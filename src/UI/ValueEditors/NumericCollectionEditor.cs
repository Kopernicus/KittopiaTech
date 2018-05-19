using System;
using System.Reflection;
using CommNet;
using Kopernicus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;
using Object = System.Object;

namespace KittopiaTech.UI.ValueEditors
{
    public class NumericCollectionEditor<TNum, TEd> : ValueEditor where TEd : ValueEditor
    {
        public NumericCollectionEditor(String name, ParserTarget target, MemberInfo member, Func<Object> reference, Func<String> getValue, Func<String, String> setValue) : base(name, target, member, reference, getValue, setValue)
        {
        }

        protected override void BuildDialog()
        {
            // Skin
            Skin = Tools.KittopiaSkin;
            
            // Build a list of collection elements
            NumericCollectionParser<TNum> collectionParser =
                (NumericCollectionParser<TNum>) Tools.GetValue(Member, Reference);
            
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
                            GUILabel(i + 1 + ".", modifier: Alignment(TextAlignmentOptions.Left));
                            GUIFlexibleSpace();
                            GUIButton("V", () =>
                                {
                                    TNum tmp = collectionParser.Value[i];
                                    collectionParser.Value[i] = collectionParser.Value[i - 1];
                                    collectionParser.Value[i - 1] = tmp;
                                    Tools.SetValue(Member, Reference, collectionParser);
                                }, 25f, 25f, false, () => { },
                                Enabled<DialogGUIButton>(() => i != 0).And(Rotation<DialogGUIButton>(180, 0, 0))
                                    .And(Scale<DialogGUIButton>(0.7f)));
                            GUIButton("V", () =>
                                {
                                    TNum tmp = collectionParser.Value[i];
                                    collectionParser.Value[i] = collectionParser.Value[i + 1];
                                    collectionParser.Value[i + 1] = tmp;
                                    Tools.SetValue(Member, Reference, collectionParser);
                                }, 25f, 25f, false, () => { },
                                Enabled<DialogGUIButton>(() => i != collectionParser.Value.Count - 1)
                                    .And(Scale<DialogGUIButton>(0.7f)));
                            GUIButton("+", () =>
                            {
                                collectionParser.Value.Insert(i, default(TNum));
                                Tools.SetValue(Member, Reference, collectionParser);
                                Redraw();
                            }, 25f, 25f, false, () => { });
                            GUIButton("x", () =>
                            {
                                collectionParser.Value.RemoveAt(i);
                                Tools.SetValue(Member, Reference, collectionParser);
                                Redraw();
                            }, 25f, 25f, false, () => { });
                        });

                        // Display the editor
                        Integrate((ValueEditor) Activator.CreateInstance(typeof(TEd), _name + " - I:" + i,
                            Target,
                            Member,
                            new Func<Object>(() => Reference),
                            new Func<String>(() => collectionParser.Value[i].ToString()),
                            new Func<String, String>(s =>
                            {
                                collectionParser.Value[i] = (TNum) typeof(TNum)
                                    .GetMethod("Parse", new[] {typeof(String)})?.Invoke(null, new Object[] {s});
                                Tools.SetValue(Member, Reference, collectionParser);
                                return s;
                            })
                        ));

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
            return 300;
        }
    }
}