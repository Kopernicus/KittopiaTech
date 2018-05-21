using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using KittopiaTech.UI.Framework;
using KittopiaTech.UI.ValueEditors;
using Kopernicus;
using Kopernicus.Configuration;
using Kopernicus.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;
using Object = System.Object;

namespace KittopiaTech.UI
{
    public class KopernicusEditor : Window<KopernicusEditor>
    {
        /// <summary>
        /// Information about the current state of the editor
        /// </summary>
        public struct EditorInfo
        {
            public CelestialBody Body;

            public Object Value
            {
                get { return GetValue(); }
            }

            public Func<Object> GetValue;
        }

        /// <summary>
        /// The Kopernicus Parser object that is displayed
        /// </summary>
        public EditorInfo Info;

        /// <summary>
        /// The name of the object we are editing
        /// </summary>
        private String _name;

        /// <summary>
        /// All sub-windows that were spawned by this window
        /// </summary>
        private Dictionary<String, KopernicusEditor> _children;

        /// <summary>
        /// All value editors that were spawned by this window
        /// </summary>
        private Dictionary<String, ValueEditor> _valueEditors;

        public KopernicusEditor(Func<Object> value, CelestialBody body, String name)
        {
            Info = new EditorInfo {GetValue = value, Body = body};
            _name = name;
            _children = new Dictionary<String, KopernicusEditor>();
            _valueEditors = new Dictionary<String, ValueEditor>();
        }

        public override String GetTitle()
        {
            return "KittopiaTech - " + _name;
        }

        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            // Build a list of parser targets
            GUIScrollList(new Vector2(390, 600), false, true, () =>
            {
                GUIVerticalLayout(true, false, 2f, new RectOffset(8, 26, 8, 8), TextAnchor.UpperLeft, () =>
                {
                    GUIContentSizer(ContentSizeFitter.FitMode.Unconstrained,
                        ContentSizeFitter.FitMode.PreferredSize, true);
                    Dictionary<ParserTarget, MemberInfo> parserTargets = Tools.GetParserTargets(Info.Value.GetType());

                    // Use a box as a seperator
                    GUISpace(5f);
                    GUIBox(-1f, 1f, () => { });
                    GUISpace(5f);

                    foreach (KeyValuePair<ParserTarget, MemberInfo> target in parserTargets)
                    {
                        DisplayParserTarget(target.Key, target.Value);
                    }

                    // Display KittopiaActions
                    Dictionary<KittopiaAction, MethodInfo> actions = Tools.GetKittopiaActions(Info.Value.GetType());
                    foreach (KeyValuePair<KittopiaAction, MethodInfo> action in actions)
                    {
                        DisplayKittopiaAction(action.Key, action.Value);
                    }
                });
            });
        }

        /// <summary>
        /// Displays a parser target in the window
        /// </summary>
        private void DisplayParserTarget(ParserTarget target, MemberInfo member)
        {
            // Don't display hidden options
            if (Tools.HasAttribute<KittopiaHideOption>(member))
            {
                return;
            }

            GUIHorizontalLayout(() =>
            {
                GUILabel(target.FieldName, modifier: Alignment(TextAlignmentOptions.Left));
                GUIFlexibleSpace();
                GUILabel(target.Optional ? "Optional" : "Required",
                    modifier: Alignment(TextAlignmentOptions.Right)
                        .And(TextColor(Color.gray)));
            });

            // Display a KittopiaDescription
            String description = Tools.GetDescription(member);
            if (!String.IsNullOrEmpty(description))
            {
                GUISpace(2f);
                GUILabel(description,
                    modifier: TextColor(Color.gray));
            }

            GUISpace(5f);

            // Check if the object is a list or a single element
            if (!Tools.IsCollection(target))
            {
                // If the element is loaded from a config node, it needs a new window
                // Simply values can be edited with an inline textfield
                ConfigType configType =
                    Tools.GetConfigType(Tools.GetValue(member, Info.Value)?.GetType() ?? Tools.MemberType(member));
                if (configType == ConfigType.Node)
                {
                    GUIHorizontalLayout(() =>
                    {
                        // Edit Button
                        GUIToggleButton(false, "Edit", e => ToggleSubEditor(target, member, e), -1f, 25f,
                            Enabled<DialogGUIToggleButton>(() => Tools.GetValue(member, Info.Value) != null));

                        // Button to create or destroy the element
                        if (Tools.HasAttribute<KittopiaUntouchable>(member))
                        {
                            GUIButton(Tools.GetValue(member, Info.Value) != null ? "x" : "+", () => { }, 25f, 25f,
                                false, () => { }, Enabled<DialogGUIButton>(() => false));
                        }
                        else
                        {
                            GUIButton(() => Tools.GetValue(member, Info.Value) != null ? "x" : "+", () =>
                                {
                                    Object value = Tools.GetValue(member, Info.Value);
                                    if (value != null)
                                    {
                                        Tools.Destruct(value);
                                        Tools.SetValue(member, Info.Value, null);
                                    }
                                    else
                                    {
                                        Tools.SetValue(member, Info.Value,
                                            Tools.Construct(Tools.MemberType(member), Info.Body));
                                    }
                                }, 25f, 25f, false,
                                () => { });
                        }
                    });
                }
                else
                {
                    GUIHorizontalLayout(() =>
                    {
                        GUITextInput("", false, Int32.MaxValue, s => Tools.ApplyInput(member, s, Info.Value),
                            () => Tools.FormatParsable(Tools.GetValue(member, Info.Value)) ?? "",
                            TMP_InputField.ContentType.Standard, 25f);
                        GUIToggleButton(false, ">", e => ToggleValueEditor(target, member, e), 25f, 25f,
                            Enabled<DialogGUIToggleButton>(() => HasValueEditor(member)));
                    });
                }
            }

            // Use a box as a seperator
            GUISpace(5f);
            GUIBox(-1f, 1f, () => { });
            GUISpace(5f);
        }

        /// <summary>
        /// Displays a Kittopia Action
        /// </summary>
        private void DisplayKittopiaAction(KittopiaAction action, MethodInfo info)
        {
            // Display a KittopiaDescription
            String description = Tools.GetDescription(info);
            if (!String.IsNullOrEmpty(description))
            {
                GUILabel(description, modifier: TextColor(Color.gray)); 
                GUISpace(2f);
            }

            // Display the button
            GUIHorizontalLayout(true, false,
                () =>
                {
                    Boolean active = true;
                    GUIButton(action.name, () =>
                        {
                            active = false;
                            Tools.InvokeKittopiaAction(info, Info.Value, () => active = true);
                        }, -1f, 25f, false, () => { },
                        Scale<DialogGUIButton>(0.8f).And(Enabled<DialogGUIButton>(() => active)));
                });

            // Use a box as a seperator
            GUISpace(5f);
            GUIBox(-1f, 1f, () => { });
            GUISpace(5f);
        }

        /// <summary>
        /// Toggles a subeditor for a ParserTarget
        /// </summary>
        private void ToggleSubEditor(ParserTarget target, MemberInfo member, Boolean active)
        {
            if (_children.ContainsKey(target.FieldName))
            {
                if (active)
                {
                    _children[target.FieldName].Show();
                }
                else
                {
                    _children[target.FieldName].Hide();
                }
            }
            else
            {
                KopernicusEditor editor = new KopernicusEditor(() => Tools.GetValue(member, Info.Value), Info.Body,
                    Info.Body.transform.name + " - " + target.FieldName);
                editor.Open();
                _children.Add(target.FieldName, editor);
            }
        }

        /// <summary>
        /// Enabels or disables the value editor for a Parser Target
        /// </summary>
        private void ToggleValueEditor(ParserTarget target, MemberInfo member, Boolean active)
        {
            if (_valueEditors.ContainsKey(target.FieldName))
            {
                if (active)
                {
                    _valueEditors[target.FieldName].Show();
                }
                else
                {
                    _valueEditors[target.FieldName].Hide();
                }
            }
            else
            {
                ValueEditor editor = GetValueEditor(target, member);
                if (editor != null)
                {
                    editor.Open();
                    _valueEditors.Add(target.FieldName, editor);
                }
            }
        }

        /// <summary>
        /// Creates a new value editor for the type of the supplied member
        /// </summary>
        private ValueEditor GetValueEditor(ParserTarget target, MemberInfo member)
        {
            Type memberType = Tools.MemberType(member);
            if (memberType == typeof(String))
            {
                return CreateValueEditor<StringEditor>(target, member);
            }

            if (memberType == typeof(NumericParser<Int32>))
            {
                return CreateValueEditor<IntegerEditor>(target, member);
            }

            if (memberType == typeof(NumericParser<Single>))
            {
                return CreateValueEditor<SingleEditor>(target, member);
            }

            if (memberType == typeof(NumericParser<Boolean>))
            {
                return CreateValueEditor<BooleanEditor>(target, member);
            }

            if (memberType == typeof(StringCollectionParser))
            {
                return CreateValueEditor<StringCollectionEditor>(target, member);
            }

            if (memberType == typeof(NumericCollectionParser<Int32>))
            {
                return CreateValueEditor<NumericCollectionEditor<Int32, IntegerEditor>>(target, member);
            }

            if (memberType == typeof(NumericCollectionParser<Single>))
            {
                return CreateValueEditor<NumericCollectionEditor<Single, SingleEditor>>(target, member);
            }

            if (memberType == typeof(NumericCollectionParser<Boolean>))
            {
                return CreateValueEditor<NumericCollectionEditor<Boolean, BooleanEditor>>(target, member);
            }

            if (memberType == typeof(ColorParser))
            {
                return CreateValueEditor<ColorEditor>(target, member);
            }

            return null;
        }

        /// <summary>
        /// Creates a new instance of a value editor
        /// </summary>
        private T CreateValueEditor<T>(ParserTarget target, MemberInfo member) where T : ValueEditor
        {
            return (T) Activator.CreateInstance(typeof(T),
                Info.Body.transform.name + " - " + target.FieldName, target, member,
                Info.GetValue, new Func<String>(() => Tools.FormatParsable(Tools.GetValue(member, Info.Value)) ?? ""),
                new Func<String, String>(s => Tools.ApplyInput(member, s, Info.Value)));
        }

        /// <summary>
        /// Returns whether the supplied member can be edited in a value editor
        /// </summary>
        private static Boolean HasValueEditor(MemberInfo member)
        {
            Type memberType = Tools.MemberType(member);
            if (memberType == typeof(String))
            {
                return true;
            }

            if (memberType == typeof(NumericParser<Int32>))
            {
                return true;
            }

            if (memberType == typeof(NumericParser<Single>))
            {
                return true;
            }

            if (memberType == typeof(NumericParser<Boolean>))
            {
                return true;
            }

            if (memberType == typeof(StringCollectionParser))
            {
                return true;
            }

            if (memberType == typeof(NumericCollectionParser<Int32>))
            {
                return true;
            }

            if (memberType == typeof(NumericCollectionParser<Single>))
            {
                return true;
            }

            if (memberType == typeof(NumericCollectionParser<Boolean>))
            {
                return true;
            }

            if (memberType == typeof(ColorParser))
            {
                return true;
            }

            return false;
        }

        public override Single GetWidth()
        {
            return 400;
        }

#if FALSE // Not sure if this is good or bad

        protected override void OnHide()
        {
            foreach (KopernicusEditor window in _children.Values)
            {
                window.Hide();
            }
        }

        protected override void OnShow()
        {
            foreach (KopernicusEditor window in _children.Values)
            {
                window.Show();
            }
        }
#endif

        protected override void OnClose()
        {
            foreach (KopernicusEditor window in _children.Values)
            {
                window.Close();
            }
        }
    }
}