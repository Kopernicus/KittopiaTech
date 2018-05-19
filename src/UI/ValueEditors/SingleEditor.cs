using System;
using System.Globalization;
using System.Reflection;
using Kopernicus;
using Smooth.Algebraics;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class SingleEditor : ValueEditor
    {
        public SingleEditor(String name, ParserTarget target, MemberInfo member, Func<Object> reference, Func<String> getValue, Func<String, String> setValue) : base(name, target, member, reference, getValue, setValue)
        {
        }

        protected override void BuildDialog()
        {
            // Skin
            Skin = Tools.KittopiaSkin;

            GUISpace(5f);

            GUIHorizontalLayout(() =>
            {
                // Decrement Button
                GUIButton("<",
                    () => SetValue(Math.Max(Single.MinValue + 1,
                        Single.Parse(GetValue()) - 1).ToString(CultureInfo.InvariantCulture)),
                    25f, 25f, false, () => { });

                // Text Edit
                GUITextInput("", false, Int32.MaxValue, SetValue, GetValue, TMP_InputField.ContentType.IntegerNumber,
                    25f);

                // Increment Button
                GUIButton(">",
                    () => SetValue(Math.Min(Single.MaxValue - 1,
                        Single.Parse(GetValue()) + 1).ToString(CultureInfo.InvariantCulture)),
                    25f, 25f, false, () => { });
            });
        }

        public override Single GetWidth()
        {
            return 300;
        }
    }
}