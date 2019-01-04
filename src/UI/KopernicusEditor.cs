using System;
using System.Collections.Generic;
using System.Reflection;
using KittopiaTech.UI.Framework;
using KittopiaTech.UI.ValueEditors;
using Kopernicus;
using Kopernicus.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;
using Object = System.Object;

namespace KittopiaTech.UI
{
    public class KopernicusEditor : Window
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
        
        public Action<Object> SetValue;

        /// <summary>
        /// The Kopernicus Parser object that is displayed
        /// </summary>
        public EditorInfo Info;

        /// <summary>
        /// The name of the object we are editing
        /// </summary>
        protected String _name;

        /// <summary>
        /// All sub-windows that were spawned by this window
        /// </summary>
        protected readonly Dictionary<Object, KopernicusEditor> Children;

        /// <summary>
        /// All value editors that were spawned by this window
        /// </summary>
        protected readonly Dictionary<Object, ValueEditor> ValueEditors;

        public KopernicusEditor(Func<Object> value, Action<Object> setValue, CelestialBody body, String name)
        {
            Info = new EditorInfo {GetValue = value, Body = body};
            SetValue = setValue;
            _name = name;
            Children = new Dictionary<Object, KopernicusEditor>();
            ValueEditors = new Dictionary<Object, ValueEditor>();
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
            if (Tools.HasAttribute<KittopiaHideOption>(member) &&
                !Tools.GetAttributes<KittopiaHideOption>(member)[0].show)
            {
                return;
            }

            // Check if the object is a list or a single element
            if (!Tools.IsCollection(target))
            {
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

                // If the element is loaded from a config node, it needs a new window
                // Simply values can be edited with an inline textfield
                ConfigType configType =
                    Tools.GetConfigType(Tools.GetValue(member, Info.Value)?.GetType() ?? Tools.MemberType(member));

                if (configType == ConfigType.Node)
                {
                    GUIHorizontalLayout(() =>
                    {
                        // Edit Button
                        GUIToggleButton(
                            () => Children.ContainsKey(target.FieldName) && Children[target.FieldName].IsVisible,
                            "Edit", e => ToggleSubEditor(target, member, e), -1f, 25f,
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
                                        SetValue(null);
                                    }
                                    else
                                    {
                                        Object v = Tools.Construct(Tools.MemberType(member), Info.Body);
                                        Tools.SetValue(member, Info.Value, v);
                                        SetValue(v);
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
                        GUIToggleButton(
                            () => ValueEditors.ContainsKey(target.FieldName) &&
                                  ValueEditors[target.FieldName].IsVisible, ">",
                            e => ToggleValueEditor(target, member, e), 25f, 25f,
                            Enabled<DialogGUIToggleButton>(() => HasValueEditor(member)));
                    });
                }
            }
            else
            {
                ParserTargetCollection collection = (ParserTargetCollection) target;
                
                // Is the collection parsing a subnode, or this one?
                if (collection.FieldName != "self")
                {
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

                    GUIHorizontalLayout(() =>
                    {
                        // Edit Button
                        GUIToggleButton(() => Children.ContainsKey(target.FieldName) && Children[target.FieldName].IsVisible, "Edit", e => ToggleCollectionEditor(target, member, e), -1f, 25f,
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
                                        SetValue(null);
                                    }
                                    else
                                    {
                                        Object v = Tools.Construct(Tools.MemberType(member), Info.Body);
                                        Tools.SetValue(member, Info.Value, v);
                                        SetValue(v);
                                    }
                                }, 25f, 25f, false,
                                () => { });
                        }
                    });
                }
                else
                {
                    new CollectionEditor(() => Tools.GetValue(member, Info.Value),
                        v => Tools.SetValue(member, Info.Value, v), Info.Body,
                        Info.Body.transform.name + " - " + target.FieldName, member, (ParserTargetCollection) target,
                        () => Info.Value).DisplayCollection();
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
            if (Children.ContainsKey(target.FieldName) && Children[target.FieldName].IsOpen)
            {
                if (active)
                {
                    Children[target.FieldName].Show();
                }
                else
                {
                    Children[target.FieldName].Hide();
                }
            }
            else if (Children.ContainsKey(target.FieldName))
            {
                if (active)
                {
                    Children[target.FieldName].Open();
                }
                else
                {
                    Children.Remove(target.FieldName);
                }
            }
            else
            {
                KopernicusEditor editor = new KopernicusEditor(() => Tools.GetValue(member, Info.Value),
                    v => Tools.SetValue(member, Info.Value, v), Info.Body,
                    Info.Body.transform.name + " - " + target.FieldName);
                editor.Open();
                Children.Add(target.FieldName, editor);
            }
        }

        /// <summary>
        /// Toggles a subeditor for a ParserTargetCollection
        /// </summary>
        private void ToggleCollectionEditor(ParserTarget target, MemberInfo member, Boolean active)
        {
            if (Children.ContainsKey(target.FieldName) && Children[target.FieldName].IsOpen)
            {
                if (active)
                {
                    Children[target.FieldName].Show();
                }
                else
                {
                    Children[target.FieldName].Hide();
                }
            }
            else if (Children.ContainsKey(target.FieldName))
            {
                if (active)
                {
                    Children[target.FieldName].Open();
                }
                else
                {
                    Children.Remove(target.FieldName);
                }
            }
            else
            {
                KopernicusEditor editor = new CollectionEditor(() => Tools.GetValue(member, Info.Value),
                    v => Tools.SetValue(member, Info.Value, v), Info.Body,
                    Info.Body.transform.name + " - " + target.FieldName, member, target as ParserTargetCollection,
                    () => Info.Value);
                editor.Open();
                Children.Add(target.FieldName, editor);
            }
        }

        /// <summary>
        /// Enabels or disables the value editor for a Parser Target
        /// </summary>
        private void ToggleValueEditor(ParserTarget target, MemberInfo member, Boolean active)
        {
            if (ValueEditors.ContainsKey(target.FieldName) && ValueEditors[target.FieldName].IsOpen)
            {
                if (active)
                {
                    ValueEditors[target.FieldName].Show();
                }
                else
                {
                    ValueEditors[target.FieldName].Hide();
                }
            }
            else if (ValueEditors.ContainsKey(target.FieldName))
            {
                if (active)
                {
                    ValueEditors[target.FieldName].Open();
                }
                else
                {
                    ValueEditors.Remove(target.FieldName);
                }
            }
            else
            {
                Type editorType = GetValueEditor(Tools.MemberType(member));
                if (editorType != null)
                {
                    ValueEditor editor = CreateValueEditor(editorType, target, member);
                    editor.Open();
                    ValueEditors.Add(target.FieldName, editor);
                }
            }
        }

        /// <summary>
        /// Creates a new value editor for the type of the supplied member
        /// </summary>
        protected static Type GetValueEditor(Type memberType)
        {
            if (memberType == typeof(String))
            {
                return typeof(StringEditor);
            }

            if (memberType == typeof(NumericParser<Int32>))
            {
                return typeof(IntegerEditor);
            }

            if (memberType == typeof(NumericParser<Single>))
            {
                return typeof(SingleEditor);
            }

            if (memberType == typeof(NumericParser<Double>))
            {
                return typeof(DoubleEditor);
            }

            if (memberType == typeof(NumericParser<Boolean>))
            {
                return typeof(BooleanEditor);
            }

            if (memberType == typeof(StringCollectionParser))
            {
                return typeof(StringCollectionEditor);
            }

            if (memberType == typeof(NumericCollectionParser<Int32>))
            {
                return typeof(NumericCollectionEditor<Int32, IntegerEditor>);
            }

            if (memberType == typeof(NumericCollectionParser<Single>))
            {
                return typeof(NumericCollectionEditor<Single, SingleEditor>);
            }

            if (memberType == typeof(NumericCollectionParser<Double>))
            {
                return typeof(NumericCollectionEditor<Double, DoubleEditor>);
            }

            if (memberType == typeof(NumericCollectionParser<Boolean>))
            {
                return typeof(NumericCollectionEditor<Boolean, BooleanEditor>);
            }

            if (memberType == typeof(ColorParser))
            {
                return typeof(ColorEditor);
            }

            if (memberType == typeof(Vector3Parser))
            {
                return typeof(Vector3Editor);
            }

            if (memberType == typeof(Vector3DParser))
            {
                return typeof(Vector3DEditor);
            }

            if (memberType == typeof(Vector2Parser))
            {
                return typeof(Vector2Editor);
            }

            if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(EnumParser<>))
            {
                return typeof(EnumEditor);
            }

            return null;
        }

        /// <summary>
        /// Creates a new instance of a value editor
        /// </summary>
        private ValueEditor CreateValueEditor(Type editorType, ParserTarget target, MemberInfo member)
        {
            return (ValueEditor) Activator.CreateInstance(editorType,
                Info.Body.transform.name + " - " + target.FieldName, Info.GetValue,
                new Func<Object>(() => Tools.GetValue(member, Info.Value)),
                new Action<Object>(s => Tools.SetValue(member, Info.Value, s)));
        }

        /// <summary>
        /// Returns whether the supplied member can be edited in a value editor
        /// </summary>
        protected static Boolean HasValueEditor(MemberInfo member)
        {
            return HasValueEditor(Tools.MemberType(member));
        }

        /// <summary>
        /// Returns whether the supplied member can be edited in a value editor
        /// </summary>
        protected static Boolean HasValueEditor(Type memberType)
        {
            return GetValueEditor(memberType) != null;
        }

        public override Single GetWidth()
        {
            return 400;
        }

        protected override void OnOpen()
        {
            TaskListWindow.Instance.Add(this);
        }

        protected override void OnClose()
        {
            TaskListWindow.Instance.Remove(this);
        }
    }
}