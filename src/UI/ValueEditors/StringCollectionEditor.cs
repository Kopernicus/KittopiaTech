using System;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;
using Object = System.Object;

namespace KittopiaTech.UI.ValueEditors
{
    public class StringCollectionEditor : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            // Build a list of collection elements
            StringCollectionParser collectionParser =
                (StringCollectionParser) GetValue();

            // Display a scroll area
            GUIScrollList(new Vector2(290, 400), false, true, () =>
            {
                GUIVerticalLayout(true, false, 2f, new RectOffset(8, 26, 8, 8), TextAnchor.UpperLeft, () =>
                {
                    GUIContentSizer(ContentSizeFitter.FitMode.Unconstrained,
                        ContentSizeFitter.FitMode.PreferredSize, true);

                    // Use a box as a seperator
                    GUISpace(5f);
                    GUIBox(-1f, 1f, () => { });
                    GUISpace(5f);

                    // Display all elements
                    for (Int32 j = 0; j < collectionParser.Value.Count; j++)
                    {
                        Int32 i = j;

                        // Add / Remove Buttons
                        GUIHorizontalLayout(() =>
                        {
                            GUIFlexibleSpace();
                            GUIButton("V", () =>
                                {
                                    String tmp = collectionParser.Value[i];
                                    collectionParser.Value[i] = collectionParser.Value[i - 1];
                                    collectionParser.Value[i - 1] = tmp;
                                    SetValue(collectionParser);
                                }, 25f, 25f, false, () => { },
                                Enabled<DialogGUIButton>(() => i != 0).And(Rotation<DialogGUIButton>(180, 0, 0))
                                    .And(Scale<DialogGUIButton>(0.7f)));
                            GUIButton("V", () =>
                                {
                                    String tmp = collectionParser.Value[i];
                                    collectionParser.Value[i] = collectionParser.Value[i + 1];
                                    collectionParser.Value[i + 1] = tmp;
                                    SetValue(collectionParser);
                                }, 25f, 25f, false, () => { },
                                Enabled<DialogGUIButton>(() => i != collectionParser.Value.Count - 1)
                                    .And(Scale<DialogGUIButton>(0.7f)));
                            GUIButton("+", () =>
                            {
                                collectionParser.Value.Insert(i, "");
                                SetValue(collectionParser);
                                Redraw();
                            }, 25f, 25f, false, () => { });
                            GUIButton("x", () =>
                            {
                                collectionParser.Value.RemoveAt(i);
                                SetValue(collectionParser);
                                Redraw();
                            }, 25f, 25f, false, () => { });
                        });

                        // Display the editor
                        Integrate(new StringEditor(_name + " - I:" + i,
                            () => Reference,
                            () => collectionParser.Value[i],
                            s =>
                            {
                                collectionParser.Value[i] = (String) s;
                                SetValue(collectionParser);
                            }
                        ));

                        // Use a box as a seperator
                        GUISpace(5f);
                        GUIBox(-1f, 1f, () => { });
                        GUISpace(5f);
                    }

                    if (collectionParser.Value.Count == 0)
                    {
                        // Add / Remove Buttons
                        GUIHorizontalLayout(() =>
                        {
                            GUIButton("+", () =>
                            {
                                collectionParser.Value.Insert(collectionParser.Value.Count, "");
                                SetValue(collectionParser);
                                Redraw();
                            }, 25f, 25f, false, () => { });
                        });

                        // Use a box as a seperator
                        GUISpace(5f);
                        GUIBox(-1f, 1f, () => { });
                        GUISpace(5f);
                    }
                });
            });
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public StringCollectionEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}