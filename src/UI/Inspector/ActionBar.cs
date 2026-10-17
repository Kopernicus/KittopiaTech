using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kopernicus.Configuration;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using KittopiaTech.UI.Editors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Inspector;

/// <summary>A row of buttons for an object's KittopiaAction methods.</summary>
public static class ActionBar
{
    /// <summary>Actions KittopiaTech replaces with its own features, besides those apply hooks replace.</summary>
    static readonly HashSet<(Type, string)> Hidden = [(typeof(Body), nameof(Body.ExportConfig))];

    /// <summary>Actions that can't be undone but aren't marked Destructive upstream.</summary>
    static readonly HashSet<(Type, string)> Confirm = [(typeof(Body), nameof(Body.CreateBarycenter))];

    internal static bool NeedsConfirm(ActionSchema action) =>
        action.Destructive || Confirm.Contains((action.Method.DeclaringType, action.Method.Name));

    public static void Build(Transform parent, IEditScope scope, object owner)
    {
        var actions = TypeSchema
            .Get(owner.GetType())
            .Actions.Where(a => a.Method.GetParameters().Length == 0)
            .Where(a => !Hidden.Contains((a.Method.DeclaringType, a.Method.Name)))
            .Where(a => !ApplyHooks.Replaces(a.Method))
            .ToList();
        if (actions.Count == 0)
            return;

        var row = DebugSkin.CreateHorizontalLayout(parent, 4f, controlWidth: true);
        row.name = "Actions";
        row.padding = new RectOffset(4, 0, 2, 2);
        foreach (var action in actions)
        {
            var button = DebugSkin.CreateButton(row.transform, action.Name);
            EditorLayout.Compact(button.gameObject);
            button.gameObject.AddComponent<ActionButton>().Setup(button, action, scope, owner);
        }
    }
}

/// <summary>Runs one action. Destructive actions need a second click within a few seconds.</summary>
public sealed class ActionButton : MonoBehaviour
{
    const float ConfirmSeconds = 3f;

    Button button;
    TextMeshProUGUI label;
    ActionSchema action;
    IEditScope scope;
    object owner;
    float armedUntil;
    bool running;

    public void Setup(Button button, ActionSchema action, IEditScope scope, object owner)
    {
        this.button = button;
        this.action = action;
        this.scope = scope;
        this.owner = owner;
        label = button.GetComponentInChildren<TextMeshProUGUI>();
        button.onClick.AddListener(OnClick);
    }

    void Update()
    {
        if (armedUntil > 0f && Time.unscaledTime > armedUntil)
        {
            armedUntil = 0f;
            label.text = action.Name;
        }
    }

    void OnClick()
    {
        if (running)
            return;

        if (ActionBar.NeedsConfirm(action) && armedUntil == 0f)
        {
            armedUntil = Time.unscaledTime + ConfirmSeconds;
            label.text = $"Confirm {action.Name}?";
            return;
        }
        armedUntil = 0f;
        label.text = action.Name;

        try
        {
            // Actions act on the live body, so apply what's pending beneath them first.
            scope.ApplyWithin(owner);
            object result;
            using (ParseStateScope.For(scope.Context))
                result = action.Method.Invoke(owner, null);

            // Run coroutines on the window so they outlive a collapsed foldout.
            if (result is IEnumerator routine)
                GetComponentInParent<InspectorWindow>().StartCoroutine(RunCoroutine(routine));
            else
                scope.Invalidate();
        }
        catch (Exception e)
        {
            var inner = e is TargetInvocationException { InnerException: { } i } ? i : e;
            UnityEngine.Debug.LogError($"[KittopiaTech] Action {action} failed: {inner}");
        }
    }

    IEnumerator RunCoroutine(IEnumerator routine)
    {
        running = true;
        button.interactable = false;
        label.text = $"{action.Name}...";
        yield return routine;
        running = false;
        if (this != null)
        {
            button.interactable = true;
            label.text = action.Name;
        }
        scope.Invalidate();
    }
}
