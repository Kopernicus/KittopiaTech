using System;
using Kopernicus;
using Kopernicus.UI;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class SingleEditor : ValueEditor
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
                        () => SetValue((NumericParser<Single>) Math.Max(Single.MinValue + 1,
                            (NumericParser<Single>) GetValue() - 1)),
                        25f, 25f, false, () => { });

                    // Text Edit
                    GUITextInput("", false, Int32.MaxValue, s =>
                        {
                            SetValue(Tools.SetValueFromString(typeof(NumericParser<Single>), GetValue(), s));
                            return s;
                        }, () => Tools.FormatParsable(GetValue()), TMP_InputField.ContentType.IntegerNumber,
                        25f);

                    // Increment Button
                    GUIButton(">",
                        () => SetValue((NumericParser<Single>) Math.Min(Single.MaxValue - 1,
                            (NumericParser<Single>) GetValue() + 1)),
                        25f, 25f, false, () => { });
                });
            });
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public SingleEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}