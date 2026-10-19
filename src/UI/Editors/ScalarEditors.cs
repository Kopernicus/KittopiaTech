using System;
using System.Globalization;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

[FieldEditor(typeof(double))]
[FieldEditor(typeof(float))]
[FieldEditor(typeof(int))]
[FieldEditor(typeof(uint))]
[FieldEditor(typeof(long))]
[FieldEditor(typeof(ulong))]
[FieldEditor(typeof(short))]
[FieldEditor(typeof(ushort))]
[FieldEditor(typeof(byte))]
[FieldEditor(typeof(sbyte))]
public sealed class NumberFieldEditor : FieldEditor<object>
{
    [SerializeField]
    TMP_InputField input;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        input = DebugSkin.CreateInputField(transform);
        EditorLayout.Compact(input.gameObject);
    }

    protected override void Awake() => input.onEndEdit.AddListener(OnEndEdit);

    protected override void OnBind()
    {
        // Standard rather than DecimalNumber so exponents like 5.29E+22 can be typed.
        input.contentType = NumberText.IsInteger(Source.ValueType)
            ? TMP_InputField.ContentType.IntegerNumber
            : TMP_InputField.ContentType.Standard;
    }

    void OnEndEdit(string text)
    {
        if (!NumberText.TryParse(text, Source.ValueType, out var value))
        {
            EditorLayout.MarkInvalid(input, true);
            return;
        }
        EditorLayout.MarkInvalid(input, false);
        if (!value.Equals(Value))
            Commit(value);
    }

    public override void Refresh()
    {
        input.text = NumberText.Format(Value);
        input.interactable = !Source.IsReadOnly;
        EditorLayout.MarkInvalid(input, Source.Error != null);
    }
}

/// <summary>Culture-invariant number parsing and round-trippable formatting.</summary>
public static class NumberText
{
    public static bool IsInteger(Type type) =>
        type != typeof(float) && type != typeof(double) && type != typeof(decimal);

    public static bool TryParse(string text, Type type, out object value)
    {
        value = null;
        text = text?.Trim();
        try
        {
            if (IsInteger(type))
            {
                if (!decimal.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d))
                    return false;
                value = Convert.ChangeType(d, type, CultureInfo.InvariantCulture);
            }
            else
            {
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return false;
                value = Convert.ChangeType(d, type, CultureInfo.InvariantCulture);
            }
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    public static string Format(object value) =>
        value switch
        {
            null => "",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            float f => f.ToString("R", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
}

[FieldEditor(typeof(bool))]
public sealed class ToggleFieldEditor : FieldEditor<bool>
{
    [SerializeField]
    Toggle toggle;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        toggle = DebugSkin.CreateToggle(transform, "");
        EditorLayout.Compact(toggle.transform.parent.gameObject);
    }

    protected override void Awake() => toggle.onValueChanged.AddListener(v => Commit(v));

    public override void Refresh()
    {
        toggle.SetIsOnWithoutNotify(Value);
        toggle.interactable = !Source.IsReadOnly;
    }
}

/// <summary>Plain enums as a dropdown. [Flags] enums fall back to text.</summary>
[FieldEditor(typeof(Enum))]
public sealed class EnumFieldEditor : FieldEditor<object>
{
    [SerializeField]
    TMP_Dropdown dropdown;

    string[] names = [];
    bool refreshing; // this TMP version has no SetValueWithoutNotify

    public static bool CanEdit(MemberSchema member, Type valueType) =>
        valueType.IsEnum && !valueType.IsDefined(typeof(FlagsAttribute), false);

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        dropdown = DebugSkin.CreateDropdown(transform);
        EditorLayout.Compact(dropdown.gameObject);
    }

    protected override void Awake() => dropdown.onValueChanged.AddListener(OnSelected);

    protected override void OnBind()
    {
        names = Enum.GetNames(Source.ValueType);
        dropdown.ClearOptions();
        dropdown.AddOptions([.. names]);
    }

    void OnSelected(int index)
    {
        if (!refreshing && index >= 0 && index < names.Length)
            Commit(Enum.Parse(Source.ValueType, names[index]));
    }

    public override void Refresh()
    {
        var value = Value;
        int index = value == null ? -1 : Array.IndexOf(names, Enum.GetName(Source.ValueType, value));
        refreshing = true;
        dropdown.value = index;
        refreshing = false;
        if (index < 0)
            dropdown.captionText.text = value?.ToString() ?? "";
        dropdown.interactable = !Source.IsReadOnly;
    }
}

/// <summary>A <c>[KeywordEnum]</c> shader property as a dropdown of its options.</summary>
[FieldEditor(typeof(KeywordChoice))]
public sealed class KeywordChoiceFieldEditor : FieldEditor<KeywordChoice>
{
    [SerializeField]
    TMP_Dropdown dropdown;

    string[] options = [];
    bool refreshing;

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        dropdown = DebugSkin.CreateDropdown(transform);
        EditorLayout.Compact(dropdown.gameObject);
    }

    protected override void Awake() => dropdown.onValueChanged.AddListener(OnSelected);

    void OnSelected(int index)
    {
        if (!refreshing && index >= 0 && index < options.Length)
            Commit(new KeywordChoice(options, index));
    }

    public override void Refresh()
    {
        var value = Value;
        if (value.Options != null && value.Options != options)
        {
            options = value.Options;
            dropdown.ClearOptions();
            dropdown.AddOptions([.. options]);
        }
        refreshing = true;
        dropdown.value = value.Index;
        refreshing = false;
        dropdown.interactable = !Source.IsReadOnly;
    }
}
