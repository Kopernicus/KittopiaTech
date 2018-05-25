using System;
using Kopernicus;
using TMPro;
using UnityEngine;
using Object = System.Object;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class Vector3Editor : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            NumericParser<Single> x = 0;
            NumericParser<Single> y = 0;
            NumericParser<Single> z = 0;

            // X
            Integrate(new SingleEditor("", () => Reference, () =>
            {
                Vector3Parser parser = (Vector3Parser) GetValue();
                return x = parser.Value.x;
            }, v =>
            {
                x = (NumericParser<Single>) v;
                SetValue((Vector3Parser) new Vector3(x, y, z));
            }));
            
            // Y
            Integrate(new SingleEditor("", () => Reference, () =>
            {
                Vector3Parser parser = (Vector3Parser) GetValue();
                return y = parser.Value.y;
            }, v =>
            {
                y = (NumericParser<Single>) v;
                SetValue((Vector3Parser) new Vector3(x, y, z));
            }));
            
            // Z
            Integrate(new SingleEditor("", () => Reference, () =>
            {
                Vector3Parser parser = (Vector3Parser) GetValue();
                return z = parser.Value.z;
            }, v =>
            {
                z = (NumericParser<Single>) v;
                SetValue((Vector3Parser) new Vector3(x, y, z));
            }));
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public Vector3Editor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}