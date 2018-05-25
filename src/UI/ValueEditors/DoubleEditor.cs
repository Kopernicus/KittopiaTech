using System;
using Kopernicus;
using Kopernicus.UI;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class DoubleEditor : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            GUISpace(5f);

            GUIHorizontalLayout(() =>
            {
                // Decrement Button
                GUIButton("<",
                    () => SetValue((NumericParser<Double>) Math.Max(Double.MinValue + 1,
                        (NumericParser<Double>) GetValue() - 1)),
                    25f, 25f, false, () => { });

                // Text Edit
                GUITextInput("", false, Int32.MaxValue, s =>
                    {
                        SetValue(Tools.SetValueFromString(typeof(NumericParser<Double>), GetValue(), s));
                        return s;
                    }, () => Tools.FormatParsable(GetValue()), TMP_InputField.ContentType.IntegerNumber,
                    25f);

                // Increment Button
                GUIButton(">",
                    () => SetValue((NumericParser<Double>) Math.Min(Double.MaxValue - 1,
                        (NumericParser<Double>) GetValue() + 1)),
                    25f, 25f, false, () => { });
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