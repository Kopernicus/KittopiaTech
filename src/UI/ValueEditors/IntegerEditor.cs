using System;
using System.Reflection;
using Kopernicus;
using Smooth.Algebraics;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class IntegerEditor : ValueEditor
    {
        public IntegerEditor(String name, ParserTarget target, MemberInfo member, Func<Object> reference, Func<String> getValue, Func<String, String> setValue) : base(name, target, member, reference, getValue, setValue)
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
                    () => SetValue(Math.Max(Int32.MinValue + 1,
                        Int32.Parse(GetValue()) - 1).ToString()),
                    25f, 25f, false, () => { });

                // Text Edit
                GUITextInput("", false, Int32.MaxValue, SetValue, GetValue, TMP_InputField.ContentType.IntegerNumber,
                    25f);

                // Increment Button
                GUIButton(">",
                    () => SetValue(Math.Min(Int32.MaxValue - 1,
                        Int32.Parse(GetValue()) + 1).ToString()),
                    25f, 25f, false, () => { });
            });
        }

        public override Single GetWidth()
        {
            return 300;
        }
    }
}