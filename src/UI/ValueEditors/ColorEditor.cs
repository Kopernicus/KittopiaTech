using System;
using System.Globalization;
using System.Reflection;
using Kopernicus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;
using Object = System.Object;

namespace KittopiaTech.UI.ValueEditors
{
    public class ColorEditor : ValueEditor
    {
        public ColorEditor(String name, ParserTarget target, MemberInfo member, Func<Object> reference,
            Func<String> getValue, Func<String, String> setValue) : base(name, target, member, reference, getValue,
            setValue)
        {
        }

        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;

            // Color channels
            Single red = 0;
            Single green = 0;
            Single blue = 0;
            Single alpha = 0;
            String colorCache = "";
            Color cache = default(Color);

            GUISpace(2f);

            // Red channel
            GUIHorizontalLayout(() =>
            {
                GUIBox(25f, 25f, () =>
                {
                    GUIHorizontalLayout(true, true, 2f, new RectOffset(8, 0, 4, 0), TextAnchor.MiddleCenter,
                        () => { GUILabel("R"); });
                });
                GUISlider(() =>
                {
                    String value = GetValue();
                    if (colorCache != value)
                    {
                        cache = ParseColor(value);
                        colorCache = value;
                    }

                    return red = cache.r;
                }, 0, 1, false, -1, 25f, v =>
                {
                    red = v;
                    SetValue(FormatColor(new Color(red, green, blue, alpha)));
                });
                GUITextInput("", false, Int32.MaxValue, s =>
                    {
                        red = Single.Parse(s);
                        SetValue(FormatColor(new Color(red, green, blue, alpha)));
                        return s;
                    }, () => red.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.DecimalNumber,
                    modifier: e =>
                    {
                        e.size = new Vector2(75f, 25f);
                        return e;
                    });
            });

            // Green channel
            GUIHorizontalLayout(() =>
            {
                GUIBox(25f, 25f, () =>
                {
                    GUIHorizontalLayout(true, true, 2f, new RectOffset(8, 0, 4, 0), TextAnchor.MiddleCenter,
                        () => { GUILabel("G"); });
                });
                GUISlider(() =>
                {
                    String value = GetValue();
                    if (colorCache != value)
                    {
                        cache = ParseColor(value);
                        colorCache = value;
                    }

                    return green = cache.g;
                }, 0, 1, false, -1, 25f, v =>
                {
                    green = v;
                    SetValue(FormatColor(new Color(red, green, blue, alpha)));
                });
                GUITextInput("", false, Int32.MaxValue, s =>
                    {
                        green = Single.Parse(s);
                        SetValue(FormatColor(new Color(red, green, blue, alpha)));
                        return s;
                    }, () => green.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.DecimalNumber,
                    modifier: e =>
                    {
                        e.size = new Vector2(75f, 25f);
                        return e;
                    });
            });

            // Blue channel
            GUIHorizontalLayout(() =>
            {
                GUIBox(25f, 25f, () =>
                {
                    GUIHorizontalLayout(true, true, 2f, new RectOffset(8, 0, 4, 0), TextAnchor.MiddleCenter,
                        () => { GUILabel("B"); });
                });
                GUISlider(() =>
                {
                    String value = GetValue();
                    if (colorCache != value)
                    {
                        cache = ParseColor(value);
                        colorCache = value;
                    }

                    return blue = cache.b;
                }, 0, 1, false, -1, 25f, v =>
                {
                    blue = v;
                    SetValue(FormatColor(new Color(red, green, blue, alpha)));
                });
                GUITextInput("", false, Int32.MaxValue, s =>
                    {
                        blue = Single.Parse(s);
                        SetValue(FormatColor(new Color(red, green, blue, alpha)));
                        return s;
                    }, () => blue.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.DecimalNumber,
                    modifier: e =>
                    {
                        e.size = new Vector2(75f, 25f);
                        return e;
                    });
            });

            // Alpha channel
            GUIHorizontalLayout(() =>
            {
                GUIBox(25f, 25f, () =>
                {
                    GUIHorizontalLayout(true, true, 2f, new RectOffset(8, 0, 4, 0), TextAnchor.MiddleCenter,
                        () => { GUILabel("A"); });
                });
                GUISlider(() =>
                {
                    String value = GetValue();
                    if (colorCache != value)
                    {
                        cache = ParseColor(value);
                        colorCache = value;
                    }

                    return alpha = cache.a;
                }, 0, 1, false, -1, 25f, v =>
                {
                    alpha = v;
                    SetValue(FormatColor(new Color(red, green, blue, alpha)));
                });
                GUITextInput("", false, Int32.MaxValue, s =>
                    {
                        alpha = Single.Parse(s);
                        SetValue(FormatColor(new Color(red, green, blue, alpha)));
                        return s;
                    }, () => alpha.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.DecimalNumber,
                    modifier: e =>
                    {
                        e.size = new Vector2(75f, 25f);
                        return e;
                    });
            });

            // Use a box as a seperator
            GUIBox(-1f, 1f, () => { });

            GUIHorizontalLayout(true, false, 2f, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter, () =>
            {
                GUIImage(Texture2D.whiteTexture, -1, 75f,
                    OnUpdate<DialogGUIImage>(e => e.uiItem.GetComponent<RawImage>().color = cache));
            });
        }

        private static Color ParseColor(String s)
        {
            ColorParser parser = new ColorParser();
            parser.SetFromString(s);
            return parser.Value;
        }

        private static String FormatColor(Color c)
        {
            return c.r + ", " + c.g + ", " + c.b + ", " + c.a;
        }

        public override Single GetWidth()
        {
            return 300;
        }
    }
}