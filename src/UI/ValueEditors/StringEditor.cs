using System;
using System.Reflection;
using Kopernicus;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class StringEditor : ValueEditor
    {
        public StringEditor(String name, ParserTarget target, MemberInfo member, Func<Object> reference, Func<String> getValue, Func<String, String> setValue) : base(name, target, member, reference, getValue, setValue)
        {
        }

        protected override void BuildDialog()
        {
            // Skin
            Skin = Tools.KittopiaSkin;
            
            GUITextInput("", true, Int32.MaxValue, SetValue, GetValue, TMP_InputField.ContentType.Standard, 300f);
        }

        public override Single GetWidth()
        {
            return 300;
        }
    }
}