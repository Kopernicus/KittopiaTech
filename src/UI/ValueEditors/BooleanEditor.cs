using System;
using Kopernicus;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class BooleanEditor : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            GUIToggleButton(((NumericParser<Boolean>) GetValue()).Value, "Toggle", e => SetValue((NumericParser<Boolean>) e), -1, 25f);
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public BooleanEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}