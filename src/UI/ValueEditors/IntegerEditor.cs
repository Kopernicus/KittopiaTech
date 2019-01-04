using System;
using Kopernicus;
using Kopernicus.UI;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class IntegerEditor : ValueEditor
    {
        private static Int32 _multStatic = 1;
        private Int32 _mult = _multStatic;
        
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            GUIVerticalLayout(() =>
            {
                GUISpace(5f);
                GUIHorizontalLayout(() =>
                {
                    // Text Edit
                    GUITextInput("", false, Int32.MaxValue, s =>
                        {
                            SetValue(Tools.SetValueFromString(typeof(NumericParser<Int32>), GetValue(), s));
                            return s;
                        }, () => Tools.FormatParsable(GetValue()), TMP_InputField.ContentType.IntegerNumber,
                        25f);

                    // Save Button
                    GUIButton("S",
                        () =>
                        {
                            _multStatic = _mult;
                        },25f, 25f, false, () => { });

                    // Reset Button
                    GUIButton("R",
                        () =>
                        {
                            _multStatic = _mult = 1;
                            Redraw();
                        },25f, 25f, false, () => { });
                });
                GUIHorizontalLayout(true,false,() =>
                {
                    // Lower multiplier
                    GUIButton("<",
                        () =>
                        {
                            _mult = Math.Max(_mult - 1, 1);
                            Redraw();
                        },
                        25f, 25f, false, () => { }, Enabled<DialogGUIButton>(() => _mult > 1));
                    
                    // Minus 10
                    GUIButton("-" + _mult * 10,
                        () => SetValue((NumericParser<Int32>) Math.Max(Int32.MinValue + 1,
                            (NumericParser<Int32>) GetValue() - _mult * 10)),
                        -1f, 25f, false, () => { });
                    
                    // Minus 1
                    GUIButton("-" + _mult,
                        () => SetValue((NumericParser<Int32>) Math.Max(Int32.MinValue + 1,
                            (NumericParser<Int32>) GetValue() - _mult)),
                        -1f, 25f, false, () => { });
                    
                    // Plus 1
                    GUIButton("+" + _mult,
                        () => SetValue((NumericParser<Int32>) Math.Min(Int32.MaxValue - 1,
                            (NumericParser<Int32>) GetValue() + _mult)),
                        -1f, 25f, false, () => { });
                    
                    // Plus 10
                    GUIButton("+" + _mult * 10,
                        () => SetValue((NumericParser<Int32>) Math.Min(Int32.MaxValue - 1,
                            (NumericParser<Int32>) GetValue() + _mult * 10)),
                        -1f, 25f, false, () => { });
                    
                    // Higher multiplier
                    GUIButton(">",
                        () =>
                        {
                            _mult = Math.Min(_mult +1, Int32.MaxValue);
                            Redraw();
                        },
                        25f, 25f, false, () => { });
                });
            });
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public IntegerEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}