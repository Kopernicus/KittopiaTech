using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KittopiaTech.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KittopiaTech.UI.Editors;

/// <summary>Finds the editor for a member, and builds and caches one prefab per editor class.</summary>
public static class FieldEditorRegistry
{
    public sealed class Registration
    {
        public Type Target { get; internal set; }
        public Type Editor { get; internal set; }
        public int Priority { get; internal set; }
        internal MethodInfo CanEditMethod;

        public bool CanEdit(MemberSchema member, Type valueType) =>
            CanEditMethod == null || (bool)CanEditMethod.Invoke(null, [member, valueType]);

        public override string ToString() =>
            $"{Editor.Name} for {Target.Name} (priority {Priority})";
    }

    public readonly struct Resolution(Type editor, ValueAdapter adapter)
    {
        public Type Editor { get; } = editor;
        public ValueAdapter Adapter { get; } = adapter;
    }

    static Dictionary<Type, List<Registration>> byTarget;
    static readonly Dictionary<(Type, MemberSchema, MemberShape), Resolution> Resolved = [];
    static readonly Dictionary<Type, FieldEditor> Prefabs = [];
    static Transform prefabRoot;

    public static IEnumerable<Registration> Registrations
    {
        get
        {
            EnsureScanned();
            return byTarget.Values.SelectMany(r => r);
        }
    }

    static void EnsureScanned()
    {
        if (byTarget != null)
            return;

        byTarget = [];
        foreach (var loaded in AssemblyLoader.loadedAssemblies)
        {
            Type[] types;
            try
            {
                types = loaded.assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            foreach (var type in types)
            {
                if (type.IsAbstract || !typeof(FieldEditor).IsAssignableFrom(type))
                    continue;

                var canEdit = type.GetMethod(
                    "CanEdit",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(MemberSchema), typeof(Type)],
                    null
                );
                foreach (var attr in type.GetCustomAttributes<FieldEditorAttribute>())
                {
                    if (!byTarget.TryGetValue(attr.Type, out var list))
                        byTarget[attr.Type] = list = [];
                    list.Add(
                        new Registration
                        {
                            Target = attr.Type,
                            Editor = type,
                            Priority = attr.Priority,
                            CanEditMethod = canEdit,
                        }
                    );
                }
            }
        }
    }

    /// <summary>
    /// Picks an editor for a value declared as <paramref name="declared"/>. Stages, first match wins:
    /// shape markers for collections; the declared type; the unwrapped ITypeParser value type;
    /// shape fallbacks; editing an IParsable as text; and finally a read-only label.
    /// </summary>
    public static Resolution Resolve(MemberSchema member, Type declared, MemberShape shape)
    {
        EnsureScanned();
        var key = (declared, member, shape);
        if (Resolved.TryGetValue(key, out var cached))
            return cached;

        var resolution = ResolveUncached(member, declared, shape);
        Resolved[key] = resolution;
        return resolution;
    }

    static Resolution ResolveUncached(MemberSchema member, Type declared, MemberShape shape)
    {
        var identity = ValueAdapter.Identity(declared);
        Registration found;

        switch (shape)
        {
            case MemberShape.KeyedCurve or MemberShape.Collection:
                var marker =
                    shape == MemberShape.KeyedCurve ? typeof(Shapes.KeyedCurve) : typeof(Shapes.Collection);
                if ((found = Best([marker], member, declared)) != null)
                    return new(found.Editor, identity);
                if (shape == MemberShape.KeyedCurve && (found = Best([typeof(Shapes.Collection)], member, declared)) != null)
                    return new(found.Editor, identity);
                if ((found = Best(Chain(declared), member, declared)) != null)
                    return new(found.Editor, identity);
                break;

            case MemberShape.Node:
                if ((found = Best(Chain(declared), member, declared)) != null)
                    return new(found.Editor, identity);
                if ((found = Best([typeof(Shapes.Node)], member, declared)) != null)
                    return new(found.Editor, identity);
                break;

            case MemberShape.Value:
                if ((found = Best(Chain(declared), member, declared)) != null)
                    return new(found.Editor, identity);

                var unwrapped = ValueAdapter.TypeParser(declared);
                if (unwrapped != null)
                {
                    found = Best(Chain(unwrapped.ValueType), member, unwrapped.ValueType);
                    if (found != null)
                        return new(found.Editor, unwrapped);
                }

                var text = ValueAdapter.ParsableText(declared);
                if (text != null && (found = Best([typeof(string)], member, typeof(string))) != null)
                    return new(found.Editor, text);
                break;
        }

        return new(typeof(ReadOnlyFieldEditor), identity);
    }

    /// <summary>The highest-priority match; ties go to the earliest (most derived) type in the chain.</summary>
    static Registration Best(IEnumerable<Type> chain, MemberSchema member, Type valueType)
    {
        Registration best = null;
        foreach (var type in chain)
        {
            if (!byTarget.TryGetValue(type, out var list))
                continue;
            foreach (var reg in list)
            {
                if ((best == null || reg.Priority > best.Priority) && reg.CanEdit(member, valueType))
                    best = reg;
            }
        }
        return best;
    }

    /// <summary>The type, its generic definition, its base classes, then its interfaces.</summary>
    static IEnumerable<Type> Chain(Type type)
    {
        for (var t = type; t != null && t != typeof(object); t = t.BaseType)
        {
            yield return t;
            if (t.IsGenericType && !t.IsGenericTypeDefinition)
                yield return t.GetGenericTypeDefinition();
        }
        foreach (var iface in type.GetInterfaces())
        {
            yield return iface;
            if (iface.IsGenericType)
                yield return iface.GetGenericTypeDefinition();
        }
    }

    /// <summary>Instantiates the editor's prefab under <paramref name="parent"/>, building it on first use.</summary>
    public static FieldEditor Instantiate(Type editorType, Transform parent)
    {
        if (!Prefabs.TryGetValue(editorType, out var prefab) || prefab == null)
        {
            prefab = BuildPrefab(editorType);
            Prefabs[editorType] = prefab;
        }

        var instance = Object.Instantiate(prefab, parent, false);
        instance.name = editorType.Name;
        return instance;
    }

    static FieldEditor BuildPrefab(Type editorType)
    {
        if (prefabRoot == null)
        {
            // Inactive, so prefabs (and the editors nested in them) never run Awake.
            var root = new GameObject("KittopiaTech_EditorPrefabs");
            root.SetActive(false);
            Object.DontDestroyOnLoad(root);
            prefabRoot = root.transform;
        }

        var go = new GameObject(editorType.Name, typeof(RectTransform));
        go.transform.SetParent(prefabRoot, false);
        var editor = (FieldEditor)go.AddComponent(editorType);
        editor.BuildPrefab(new PrefabContext());
        return editor;
    }
}
