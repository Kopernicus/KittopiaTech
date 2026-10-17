using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kopernicus.ConfigParser;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.Interfaces;
using Kopernicus.UI;

namespace KittopiaTech.Model;

/// <summary>A KittopiaAction method on a loader type.</summary>
public sealed class ActionSchema(MethodInfo method, KittopiaAction attr)
{
    public MethodInfo Method { get; } = method;
    public string Name { get; } = attr.Name;
    public bool Destructive { get; } = attr.Destructive;
    public string Description { get; } =
        method.GetCustomAttribute<KittopiaDescription>()?.Description;

    public override string ToString() => $"{Method.DeclaringType?.Name}.{Method.Name} ({Name})";
}

/// <summary>Cached reflection over a loader type: its ParserTargets, actions and constructors.</summary>
public sealed class TypeSchema
{
    const BindingFlags MemberFlags =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    static readonly Dictionary<Type, TypeSchema> Cache = [];
    static readonly Dictionary<Type, Type[]> ConcreteTypeCache = [];

    public Type Type { get; }
    public IReadOnlyList<MemberSchema> Members { get; }
    public IReadOnlyList<ActionSchema> Actions { get; }
    public MethodInfo Destructor { get; }

    readonly Func<CelestialBody, object> constructor;

    public bool CanConstruct => constructor != null;
    public bool IsAbstract => Type.IsAbstract || Type.IsInterface;

    public static TypeSchema Get(Type type)
    {
        if (!Cache.TryGetValue(type, out var schema))
        {
            schema = new TypeSchema(type);
            Cache.Add(type, schema);
        }
        return schema;
    }

    TypeSchema(Type type)
    {
        Type = type;

        // Walk from the base class down so inherited members come first, each level
        // in declaration order. Overrides replace the base member in place.
        var members = new List<MemberSchema>();
        var index = new Dictionary<string, int>();
        foreach (var level in Hierarchy(type))
        {
            var declared = level
                .GetMembers(MemberFlags)
                .Where(m => m is FieldInfo or PropertyInfo)
                .OrderBy(m => m.MetadataToken);
            foreach (var member in declared)
            {
                var targets = (ParserTarget[])
                    member.GetCustomAttributes(typeof(ParserTarget), true);
                if (targets.Length == 0)
                    continue;

                var schema = new MemberSchema(this, member, targets);
                if (index.TryGetValue(member.Name, out int i))
                    members[i] = schema;
                else
                {
                    index[member.Name] = members.Count;
                    members.Add(schema);
                }
            }
        }
        Members = members;

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        Actions = methods
            .Select(m => (m, attr: m.GetCustomAttribute<KittopiaAction>()))
            .Where(p => p.attr != null)
            .OrderBy(p => p.m.MetadataToken)
            .Select(p => new ActionSchema(p.m, p.attr))
            .ToList();
        Destructor = methods.FirstOrDefault(m => m.IsDefined(typeof(KittopiaDestructor), true));

        if (!IsAbstract)
            constructor = FindConstructor(type);
    }

    static IEnumerable<Type> Hierarchy(Type type)
    {
        var chain = new Stack<Type>();
        for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            chain.Push(t);
        return chain;
    }

    static Func<CelestialBody, object> FindConstructor(Type type)
    {
        foreach (var ctor in type.GetConstructors())
        {
            var attr = ctor.GetCustomAttribute<KittopiaConstructor>();
            if (attr == null)
                continue;
            if (attr.Parameter == KittopiaConstructor.ParameterType.CelestialBody)
                return body => ctor.Invoke([body]);
            return _ => ctor.Invoke([]);
        }

        if (type.GetConstructor(Type.EmptyTypes) == null)
            return null;

        if (typeof(ICreatable<CelestialBody>).IsAssignableFrom(type))
        {
            return body =>
            {
                var value = (ICreatable<CelestialBody>)Activator.CreateInstance(type);
                value.Create(body);
                return value;
            };
        }
        if (typeof(ICreatable).IsAssignableFrom(type))
        {
            return _ =>
            {
                var value = (ICreatable)Activator.CreateInstance(type);
                value.Create();
                return value;
            };
        }
        return _ => Activator.CreateInstance(type);
    }

    /// <summary>Creates a new instance attached to <paramref name="body"/>.</summary>
    public object Construct(CelestialBody body)
    {
        if (constructor == null)
            throw new InvalidOperationException($"{Type.Name} can't be constructed");

        try
        {
            return constructor(body);
        }
        catch (TargetInvocationException e) when (e.InnerException != null)
        {
            throw e.InnerException;
        }
    }

    /// <summary>
    /// Constructible types that can stand in for <paramref name="baseType"/>, found the same
    /// way the parser resolves type names. Excludes stock types like the parser does.
    /// </summary>
    public static Type[] GetConcreteTypes(Type baseType)
    {
        if (ConcreteTypeCache.TryGetValue(baseType, out var types))
            return types;

        var stock = typeof(HighLogic).Assembly;
        types = Parser
            .ModTypes.Where(t =>
                !t.IsAbstract
                && !t.IsInterface
                && !t.ContainsGenericParameters
                && t.Assembly != stock
                && baseType.IsAssignableFrom(t)
            )
            .Where(t => Get(t).CanConstruct)
            .OrderBy(t => t.Name)
            .ToArray();
        ConcreteTypeCache.Add(baseType, types);
        return types;
    }

    public override string ToString() => Type.Name;
}
