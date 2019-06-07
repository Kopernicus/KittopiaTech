using System;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.UI;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class DoubleEditor : ValueEditor
    {
        private static Int32 _multStatic = 1;
        private Int32 _mult = _multStatic;

        private Double RealMult
        {
            get { return _mult > 0 ? _mult : 1f / -_mult; }
        }
        
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
                            SetValue(Tools.SetValueFromString(typeof(NumericParser<Double>), GetValue(), s));
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
                            _mult = Math.Max(_mult - 1, Int32.MinValue + 1);
                            if (_mult == 0)
                            {
                                _mult = Math.Max(_mult - 2, Int32.MinValue + 1);
                            }
                            Redraw();
                        },
                        25f, 25f, false, () => { });
                    
                    // Minus 10
                    GUIButton("-" + RealMult * 10,
                        () => SetValue((NumericParser<Double>) Math.Max(Double.MinValue + 1,
                            (NumericParser<Double>) GetValue() - RealMult * 10)),
                        -1f, 25f, false, () => { });
                    
                    // Minus 1
                    GUIButton("-" + RealMult,
                        () => SetValue((NumericParser<Double>) Math.Max(Double.MinValue + 1,
                            (NumericParser<Double>) GetValue() - RealMult)),
                        -1f, 25f, false, () => { });
                    
                    // Plus 1
                    GUIButton("+" + RealMult,
                        () => SetValue((NumericParser<Double>) Math.Min(Double.MaxValue - 1,
                            (NumericParser<Double>) GetValue() + RealMult)),
                        -1f, 25f, false, () => { });
                    
                    // Plus 10
                    GUIButton("+" + RealMult * 10,
                        () => SetValue((NumericParser<Double>) Math.Min(Double.MaxValue - 1,
                            (NumericParser<Double>) GetValue() + RealMult * 10)),
                        -1f, 25f, false, () => { });
                    
                    // Higher multiplier
                    GUIButton(">",
                        () =>
                        {
                            _mult = Math.Min(_mult + 1, Int32.MaxValue);
                            if (_mult == -1)
                            {
                                _mult = Math.Min(_mult + 2, Int32.MaxValue);
                            }
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

        public DoubleEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}