using System;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI.ValueEditors
{
    public class Vector3DEditor : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            GUIVerticalLayout(() =>
            {
                NumericParser<Double> x = 0;
                NumericParser<Double> y = 0;
                NumericParser<Double> z = 0;

                // X
                Integrate(new DoubleEditor("", () => Reference, () =>
                {
                    Vector3DParser parser = (Vector3DParser) GetValue();
                    return x = parser.Value.x;
                }, v =>
                {
                    x = (NumericParser<Double>) v;
                    SetValue((Vector3DParser) new Vector3d(x, y, z));
                }));

                // Y
                Integrate(new DoubleEditor("", () => Reference, () =>
                {
                    Vector3DParser parser = (Vector3DParser) GetValue();
                    return y = parser.Value.y;
                }, v =>
                {
                    y = (NumericParser<Double>) v;
                    SetValue((Vector3DParser) new Vector3d(x, y, z));
                }));

                // Z
                Integrate(new DoubleEditor("", () => Reference, () =>
                {
                    Vector3DParser parser = (Vector3DParser) GetValue();
                    return z = parser.Value.z;
                }, v =>
                {
                    z = (NumericParser<Double>) v;
                    SetValue((Vector3DParser) new Vector3d(x, y, z));
                }));
            });
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public Vector3DEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}