using System;
using Kopernicus;
using TMPro;
using UnityEngine;
using Object = System.Object;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class Vector2Editor : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            NumericParser<Single> x = 0;
            NumericParser<Single> y = 0;

            // X
            Integrate(new SingleEditor("", () => Reference, () =>
            {
                Vector2Parser parser = (Vector2Parser) GetValue();
                return x = parser.Value.x;
            }, v =>
            {
                x = (NumericParser<Single>) v;
                SetValue((Vector2Parser) new Vector2(x, y));
            }));
            
            // Y
            Integrate(new SingleEditor("", () => Reference, () =>
            {
                Vector2Parser parser = (Vector2Parser) GetValue();
                return y = parser.Value.y;
            }, v =>
            {
                y = (NumericParser<Single>) v;
                SetValue((Vector2Parser) new Vector2(x, y));
            }));
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public Vector2Editor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}