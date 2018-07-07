using System;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class StringEditor : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            GUITextInput("", true, Int32.MaxValue, s =>
            {
                SetValue(s);
                return s;
            }, () => (String) GetValue() ?? "", TMP_InputField.ContentType.Standard, 300f);
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public StringEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}