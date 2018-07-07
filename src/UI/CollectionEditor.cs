using System;
using System.Collections;
using System.Reflection;
using Kopernicus;
using Kopernicus.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;
using Object = System.Object;

namespace KittopiaTech.UI
{
    public class CollectionEditor : KopernicusEditor
    {
        /// <summary>
        /// The declaration for the collection
        /// </summary>
        public ParserTargetCollection Collection;
        
        /// <summary>
        /// The member the ParserTargetCollection is attached to
        /// </summary>
        public MemberInfo Member;

        /// <summary>
        /// The value containing the ParserTargetCollection 
        /// </summary>
        public Object Reference
        {
            get { return _getReference(); }
        }
        private Func<Object> _getReference;

        public CollectionEditor(Func<Object> value, Action<Object> setValue, CelestialBody body, String name, MemberInfo member, ParserTargetCollection collection, Func<Object> getReference) : base(value, setValue, body, name)
        {
            Member = member;
            _getReference = getReference;
            Collection = collection;
        }

        protected override void BuildDialog()
        {
            // Skin 
            Skin = KittopiaTech.Skin;
            
            // Display a scroll area
            GUIScrollList(new Vector2(390, 600), false, true, () =>
            {
                GUIVerticalLayout(true, false, 2f, new RectOffset(8, 26, 8, 8), TextAnchor.UpperLeft, () =>
                {
                    GUIContentSizer(ContentSizeFitter.FitMode.Unconstrained,
                        ContentSizeFitter.FitMode.PreferredSize, true);

                    DisplayCollection();

                    // Use a box as a seperator
                    GUISpace(5f);
                    GUIBox(-1f, 1f, () => { });
                    GUISpace(5f);
                });
            });
        }

        public void DisplayCollection()
        {
            // Get the list
            if (Info.Value is IList)
            {
                IList list = (IList) Info.Value;
                Type genericType = list.GetType().GetGenericArguments()[0];

                for (Int32 j = 0; j < list.Count; j++)
                {
                    Int32 i = j;

                    // Use a box as a seperator
                    GUISpace(5f);
                    GUIBox(-1f, 1f, () => { });
                    GUISpace(5f);

                    // Add / Remove Buttons
                    GUIHorizontalLayout(() =>
                    {
                        if (Collection.NameSignificance == NameSignificance.Type)
                        {
                            GUILabel(list[i].GetType().Name, modifier: Alignment(TextAlignmentOptions.Left));
                        }
                        else if (typeof(IPatchable).IsAssignableFrom(genericType))
                        {
                            GUILabel(((IPatchable) list[i]).name, modifier: Alignment(TextAlignmentOptions.Left));
                        }

                        GUIFlexibleSpace();
                        GUIButton("V", () =>
                            {
                                Object tmp = list[i];
                                list[i] = list[i - 1];
                                list[i - 1] = tmp;
                                Tools.SetValue(Member, Reference, list);
                                Redraw();
                            }, 25f, 25f, false, () => { },
                            Enabled<DialogGUIButton>(() => i != 0).And(Rotation<DialogGUIButton>(180, 0, 0))
                                .And(Scale<DialogGUIButton>(0.7f)));
                        
                        GUIButton("V", () =>
                            {
                                Object tmp = list[i];
                                list[i] = list[i + 1];
                                list[i + 1] = tmp;
                                Tools.SetValue(Member, Reference, list);
                                Redraw();
                            }, 25f, 25f, false, () => { },
                            Enabled<DialogGUIButton>(() => i != list.Count - 1)
                                .And(Scale<DialogGUIButton>(0.7f)));
                        
                        Boolean active = true;
                        GUIButton("+", () =>
                        {
                            if (Collection.NameSignificance == NameSignificance.Type)
                            {
                                active = false;
                                new TypeSelector(genericType, _name + " - Type", t =>
                                {
                                    list.Insert(i,
                                        Tools.Construct(t, Info.Body));
                                    Tools.SetValue(Member, Reference, list);
                                    Redraw();
                                    active = true;
                                }).Open();
                            }
                            else
                            {
                                list.Insert(i,
                                    Tools.Construct(genericType, Info.Body));
                                Tools.SetValue(Member, Reference, list);
                                Redraw();
                            }
                        }, 25f, 25f, false, () => { }, Enabled<DialogGUIButton>(() => active));
                        
                        GUIButton("x", () =>
                        {
                            Tools.Destruct(list[i]);
                            list.RemoveAt(i);
                            Tools.SetValue(Member, Reference, list);
                            Redraw();
                        }, 25f, 25f, false, () => { });
                    });

                    GUISpace(5f);

                    // Display the editor
                    ConfigType configType = Tools.GetConfigType(genericType);
                    if (configType == ConfigType.Node)
                    {
                        GUIHorizontalLayout(() =>
                        {
                            // Edit Button
                            GUIToggleButton(() => Children.ContainsKey(list[i]) && Children[list[i]].IsVisible, "Edit",
                                e => ToggleSubEditor(
                                    Collection.NameSignificance == NameSignificance.Type
                                        ? list[i].GetType().Name
                                        : i.ToString(), list[i], e), -1f, 25f);
                        });
                    }
                    else
                    {
                        GUIHorizontalLayout(() =>
                        {
                            GUITextInput("", false, Int32.MaxValue, s =>
                                {
                                    list[i] = Tools.SetValueFromString(genericType, list[i], s);
                                    Tools.SetValue(Member, Reference, list);
                                    return s;
                                },
                                () => Tools.FormatParsable(list[i]) ?? "",
                                TMP_InputField.ContentType.Standard, 25f);
                            GUIToggleButton(() => Children.ContainsKey(list[i]) && Children[list[i]].IsVisible, ">",
                                e => ToggleValueEditor(genericType, i.ToString(), () => list[i], s =>
                                {
                                    list[i] = s;
                                    Tools.SetValue(Member, Reference, list);
                                }, e),
                                25f, 25f,
                                Enabled<DialogGUIToggleButton>(() => true));
                        });
                    }
                }

                if (list.Count == 0)
                {
                    // Use a box as a seperator
                    GUISpace(5f);
                    GUIBox(-1f, 1f, () => { });
                    GUISpace(5f);
                    
                    // Add / Remove Buttons
                    GUIHorizontalLayout(true, false, () =>
                    {
                        Boolean active = true;
                        GUIButton("+", () =>
                        {
                            if (Collection.NameSignificance == NameSignificance.Type)
                            {
                                active = false;
                                new TypeSelector(genericType, _name + " - Type", t =>
                                {
                                    list.Insert(list.Count,
                                        Tools.Construct(t, Info.Body));
                                    Tools.SetValue(Member, Reference, list);
                                    Redraw();
                                    active = true;
                                }).Open();
                            }
                            else
                            {
                                list.Insert(list.Count,
                                    Tools.Construct(genericType, Info.Body));
                                Tools.SetValue(Member, Reference, list);
                                Redraw();
                            }
                        }, 25f, 25f, false, () => { }, Enabled<DialogGUIButton>(() => active));
                    });
                }
            }
        }
        
        /// <summary>
        /// Toggles a subeditor for a ParserTarget
        /// </summary>
        private void ToggleSubEditor(String name, Object value, Boolean active)
        {
            if (Children.ContainsKey(value) && Children[value].IsOpen)
            {
                if (active)
                {
                    Children[value].Show();
                }
                else
                {
                    Children[value].Hide();
                }
            }
            else if (Children.ContainsKey(value))
            {
                if (active)
                {
                    Children[value].Open();
                }
                else
                {
                    Children.Remove(value);
                }
            }
            else
            {
                KopernicusEditor editor = new KopernicusEditor(() => value, v => Tools.SetValue(Member, Reference, v), Info.Body,
                    _name + " - " + name);
                editor.Open();
                Children.Add(value, editor);
            }
        }

        /// <summary>
        /// Enabels or disables the value editor for a Parser Target
        /// </summary>
        private void ToggleValueEditor(Type memberType, String name, Func<Object> getter, Action<Object> setter, Boolean active)
        {
            if (ValueEditors.ContainsKey(getter()) && ValueEditors[getter()].IsOpen)
            {
                if (active)
                {
                    ValueEditors[getter()].Show();
                }
                else
                {
                    ValueEditors[getter()].Hide();
                }
            }
            else if (Children.ContainsKey(getter()))
            {
                if (active)
                {
                    Children[getter()].Open();
                }
                else
                {
                    Children.Remove(getter());
                }
            }
            else
            {
                Type editorType = GetValueEditor(memberType);
                if (editorType != null)
                {
                    ValueEditor editor = CreateValueEditor(editorType, name, getter, setter);
                    editor.Open();
                    ValueEditors.Add(getter(), editor);
                }
            }
        }

        /// <summary>
        /// Creates a new instance of a value editor
        /// </summary>
        private ValueEditor CreateValueEditor(Type editorType, String name, Func<Object> getter, Action<Object> setter)
        {
            return (ValueEditor) Activator.CreateInstance(editorType,
                _name + " - " + name, Info.GetValue,
                getter,
                setter);
        }
    }
}