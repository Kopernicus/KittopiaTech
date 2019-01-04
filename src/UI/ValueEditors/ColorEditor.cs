using System;
using System.Globalization;
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
        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;
            
            GUIVerticalLayout(() =>
            {
                // Color channels
                Single red = 0;
                Single green = 0;
                Single blue = 0;
                Single alpha = 0;

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
                        ColorParser value = (ColorParser) GetValue();
                        return red = value.Value.r;
                    }, 0, 1, false, -1, 25f, v =>
                    {
                        red = v;
                        SetValue((ColorParser) new Color(red, green, blue, alpha));
                    });
                    GUITextInput("", false, Int32.MaxValue, s =>
                        {
                            red = Single.Parse(s);
                            SetValue((ColorParser) new Color(red, green, blue, alpha));
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
                        ColorParser value = (ColorParser) GetValue();
                        return green = value.Value.g;
                    }, 0, 1, false, -1, 25f, v =>
                    {
                        green = v;
                        SetValue((ColorParser) new Color(red, green, blue, alpha));
                    });
                    GUITextInput("", false, Int32.MaxValue, s =>
                        {
                            green = Single.Parse(s);
                            SetValue((ColorParser) new Color(red, green, blue, alpha));
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
                        ColorParser value = (ColorParser) GetValue();
                        return blue = value.Value.b;
                    }, 0, 1, false, -1, 25f, v =>
                    {
                        blue = v;
                        SetValue((ColorParser) new Color(red, green, blue, alpha));
                    });
                    GUITextInput("", false, Int32.MaxValue, s =>
                        {
                            blue = Single.Parse(s);
                            SetValue((ColorParser) new Color(red, green, blue, alpha));
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
                        ColorParser value = (ColorParser) GetValue();
                        return alpha = value.Value.a;
                    }, 0, 1, false, -1, 25f, v =>
                    {
                        alpha = v;
                        SetValue((ColorParser) new Color(red, green, blue, alpha));
                    });
                    GUITextInput("", false, Int32.MaxValue, s =>
                        {
                            alpha = Single.Parse(s);
                            SetValue((ColorParser) new Color(red, green, blue, alpha));
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
                        OnUpdate<DialogGUIImage>(
                            e => e.uiItem.GetComponent<RawImage>().color = (ColorParser) GetValue()));
                });
            });
        }

        public override Single GetWidth()
        {
            return 400;
        }

        public ColorEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue) : base(name, reference, getValue, setValue)
        {
        }
    }
}