using System;
using System.Reflection;
using Kopernicus;
using Smooth.Algebraics;
using TMPro;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class BooleanEditor : ValueEditor
    {
        public BooleanEditor(String name, ParserTarget target, MemberInfo member, Func<Object> reference,
            Func<String> getValue, Func<String, String> setValue) : base(name, target, member, reference, getValue,
            setValue)
        {
        }

        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            GUIToggleButton(Boolean.Parse(GetValue()), "Toggle", e => SetValue(e.ToString()), -1, 25f);
        }

        public override Single GetWidth()
        {
            return 300;
        }
    }
}