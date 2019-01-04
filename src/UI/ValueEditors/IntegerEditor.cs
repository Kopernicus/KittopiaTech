using System;
using Kopernicus;
using Kopernicus.UI;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class IntegerEditor : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            GUIVerticalLayout(() =>
            {
                GUISpace(5f);
                GUIHorizontalLayout(() =>
                {
                    // Decrement Button
                    GUIButton("<",
                        () => SetValue((NumericParser<Int32>) Math.Max(Int32.MinValue + 1,
                            (NumericParser<Int32>) GetValue() - 1)),
                        25f, 25f, false, () => { });

                    // Text Edit
                    GUITextInput("", false, Int32.MaxValue, s =>
                        {
                            SetValue(Tools.SetValueFromString(typeof(NumericParser<Int32>), GetValue(), s));
                            return s;
                        }, () => Tools.FormatParsable(GetValue()), TMP_InputField.ContentType.IntegerNumber,
                        25f);

                    // Increment Button
                    GUIButton(">",
                        () => SetValue((NumericParser<Int32>) Math.Min(Int32.MaxValue - 1,
                            (NumericParser<Int32>) GetValue() + 1)),
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