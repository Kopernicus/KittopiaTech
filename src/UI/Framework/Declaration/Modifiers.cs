using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Framework.Declaration
{
    public static partial class DialogGUI
    {
        public delegate T Modifier<T>(T input) where T : DialogGUIBase;

        public static Modifier<T> And<T>(this Modifier<T> first, Modifier<T> second) where T : DialogGUIBase
        {
            return input => second(first(input));
        }

        public static Modifier<DialogGUILabel> TextLabelOptions(Boolean enableWordWrapping = true,
            Boolean resizeBestFit = false, Int32 resizeMaxFontSize = 0, Int32 resizeMinFontSize = 0,
            TextOverflowModes overflowMode = default(TextOverflowModes))
        {
            return label =>
            {
                label.textLabelOptions = new DialogGUILabel.TextLabelOptions()
                {
                    enableWordWrapping = enableWordWrapping,
                    resizeBestFit = resizeBestFit,
                    resizeMaxFontSize = resizeMaxFontSize,
                    resizeMinFontSize = resizeMinFontSize,
                    OverflowMode = overflowMode
                };
                return label;
            };
        }
        
        public static Modifier<DialogGUILabel> Alignment(TextAlignmentOptions alignment)
        {
            return label =>
            {
                label.OnUpdate += () =>
                {
                    if (label.text != null)
                    {
                        label.text.alignment = alignment;
                    }
                };
                return label;
            };
        }

        public static Modifier<T> PreferredHeight<T>(Single preferredHeight) where T : DialogGUIBase
        {
            return item =>
            {
                item.OnResize += () => item.uiItem.GetComponent<LayoutElement>().preferredHeight = preferredHeight;
                return item;
            };
        }

        public static Modifier<DialogGUILabel> TextColor(Color color)
        {
            return label =>
            {
                label.OnUpdate += () =>
                {
                    if (label.text != null)
                    {
                        label.text.color = color;
                    }
                };
                return label;
            };
        }

        public static Modifier<T> Enabled<T>(Func<Boolean> check) where T : DialogGUIBase
        {
            return item =>
            {
                item.OptionInteractableCondition = check;
                return item;
            };
        }
        
        public static Modifier<T> Rotation<T>(Single x, Single y, Single z) where T : DialogGUIBase
        {
            return item =>
            {
                item.OnUpdate += () =>
                {
                    item.uiItem.GetComponentInChildren<TextMeshProUGUI>().rectTransform.localRotation =
                        Quaternion.Euler(x, y, z);
                };
                return item;
            };
        }
        
        public static Modifier<T> Scale<T>(Single s) where T : DialogGUIBase
        {
            return item =>
            {
                item.OnUpdate += () =>
                {
                    item.uiItem.GetComponentInChildren<TextMeshProUGUI>().rectTransform.localScale =
                        Vector3.one * s;
                };
                return item;
            };
        }
        
        public static Modifier<T> OnUpdate<T>(Action<T> callback) where T : DialogGUIBase
        {
            return item =>
            {
                item.OnUpdate += () => callback(item);
                return item;
            };
        }
       
    }
}