using System;
using Kopernicus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;
using Object = System.Object;

namespace KittopiaTech.UI.ValueEditors
{
    public class EnumEditor : ValueEditor
    {
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;
            
            // Get the enum type
            Object value = GetValue();
            Type enumParser = value.GetType();
            Type enumType = enumParser.GetGenericArguments()[0];
            Int32 count = Enum.GetNames(enumType).Length;

            GUIScrollList(new Vector2(390, Math.Min(count * 25f + 20f, 400)), false, true, () =>
            {
                GUIVerticalLayout(true, false, 2f, new RectOffset(8, 26, 8, 8), TextAnchor.UpperLeft, () =>
                {
                    GUIContentSizer(ContentSizeFitter.FitMode.Unconstrained,
                        ContentSizeFitter.FitMode.PreferredSize, true);

                    String[] names = Enum.GetNames(enumType);
                    for (Int32 i = 0; i < names.Length; i++)
                    {
                        Int32 index = i;
                        GUIToggleButton(() => (GetValue() as IWritable)?.ValueToString() == names[index], names[index],
                            s =>
                            {
                                if (s)
                                {
                                    IParsable parsable = GetValue() as IParsable;
                                    parsable?.SetFromString(names[index]);
                                    SetValue(parsable);
                                }
                            }, -1, 25f);
                    }

                });
            });
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public EnumEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}