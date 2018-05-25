using System;
using Kopernicus;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;
using Object = System.Object;

namespace KittopiaTech.UI.ValueEditors
{
    public class NumericCollectionEditor<TNum, TEd> : ValueEditor where TEd : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            // Build a list of collection elements
            NumericCollectionParser<TNum> collectionParser =
                (NumericCollectionParser<TNum>) GetValue();

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
                                    TNum tmp = collectionParser.Value[i];
                                    collectionParser.Value[i] = collectionParser.Value[i - 1];
                                    collectionParser.Value[i - 1] = tmp;
                                    SetValue(collectionParser);
                                }, 25f, 25f, false, () => { },
                                Enabled<DialogGUIButton>(() => i != 0).And(Rotation<DialogGUIButton>(180, 0, 0))
                                    .And(Scale<DialogGUIButton>(0.7f)));
                            GUIButton("V", () =>
                                {
                                    TNum tmp = collectionParser.Value[i];
                                    collectionParser.Value[i] = collectionParser.Value[i + 1];
                                    collectionParser.Value[i + 1] = tmp;
                                    SetValue(collectionParser);
                                }, 25f, 25f, false, () => { },
                                Enabled<DialogGUIButton>(() => i != collectionParser.Value.Count - 1)
                                    .And(Scale<DialogGUIButton>(0.7f)));
                            GUIButton("+", () =>
                            {
                                collectionParser.Value.Insert(i, default(TNum));
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
                        Integrate((ValueEditor) Activator.CreateInstance(typeof(TEd), _name + " - I:" + i,
                            new Func<Object>(() => Reference),
                            new Func<Object>(() => collectionParser.Value[i]),
                            new Action<Object>(s =>
                            {
                                collectionParser.Value[i] = (NumericParser<TNum>) s;
                                SetValue(collectionParser);
                            })
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
                                collectionParser.Value.Insert(collectionParser.Value.Count, default(TNum));
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

        public NumericCollectionEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}