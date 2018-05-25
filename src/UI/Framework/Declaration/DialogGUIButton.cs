using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUIButton GUIButton(String optionText, Callback onSelected,
            Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element = new DialogGUIButtonFixed(optionText, onSelected, /* Kittopia Addition */ false);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUIButton GUIButton(String optionText, Callback onSelected, Boolean dismissOnSelect,
            Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element = new DialogGUIButtonFixed(optionText, onSelected, dismissOnSelect);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUIButton GUIButton(String optionText, Callback onSelected, Single w, Single h,
            Boolean dismissOnSelect, Action optionBuilder, Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element =
                new DialogGUIButtonFixed(optionText, onSelected, w, h, dismissOnSelect, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUIButton GUIButton(Func<String> getString, Callback onSelected, Single w, Single h,
            Boolean dismissOnSelect, Action optionBuilder, Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element =
                new DialogGUIButtonFixed(getString, onSelected, w, h, dismissOnSelect, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUIButton GUIButton(Func<String> getString, Callback onSelected,
            Func<Boolean> EnabledCondition, Single w, Single h, Boolean dismissOnSelect, Action optionBuilder,
            Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element = new DialogGUIButtonFixed(getString, onSelected, EnabledCondition, w, h,
                dismissOnSelect, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUIButton GUIButton(String optionText, Callback onSelected, Func<Boolean> EnabledCondition,
            Boolean dismissOnSelect, Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element =
                new DialogGUIButtonFixed(optionText, onSelected, EnabledCondition, dismissOnSelect);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUIButton GUIButton(String optionText, Callback onSelected, Func<Boolean> EnabledCondition,
            Single w, Single h, Boolean dismissOnSelect, UIStyle style = null,
            Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element =
                new DialogGUIButtonFixed(optionText, onSelected, EnabledCondition, w, h, dismissOnSelect, style);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUIButton GUIButton(Func<String> getString, Callback onSelected,
            Func<Boolean> EnabledCondition, Single w, Single h, Boolean dismissOnSelect, UIStyle style,
            Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element =
                new DialogGUIButtonFixed(getString, onSelected, EnabledCondition, w, h, dismissOnSelect, style);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUIButton GUIButton(Sprite image, Callback onSelected, Single w, Single h,
            Boolean dismissOnSelect = false, Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element = new DialogGUIButtonFixed(image, onSelected, w, h, dismissOnSelect);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUIButton GUIButton(Sprite image, String text, Callback onSelected, Single w, Single h,
            Boolean dismissOnSelect = false, Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element = new DialogGUIButtonFixed(image, text, onSelected, w, h, dismissOnSelect);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        // Kittopia Additions

        public static DialogGUIButton GUIButton(String optionText, Callback onSelected, Action optionBuilder,
            Modifier<DialogGUIButton> modifier = null)
        {
            DialogGUIButton element =
                new DialogGUIButtonFixed(optionText, onSelected, -1, -1, false, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        private class DialogGUIButtonFixed : DialogGUIButton
        {
            public override GameObject Create(ref Stack<Transform> layouts, UISkinDef skin)
            {
                UIStyle uistyle = guiStyle ?? skin.button;
                if (image == null)
                {
                    image = uistyle.normal.background;
                }

                return base.Create(ref layouts, skin);
            }

            public DialogGUIButtonFixed(String optionText, Callback onSelected) : base(optionText, onSelected)
            {
            }

            public DialogGUIButtonFixed(String optionText, Callback onSelected, Boolean dismissOnSelect) : base(
                optionText, onSelected, dismissOnSelect)
            {
            }

            public DialogGUIButtonFixed(String optionText, Callback onSelected, Single w, Single h,
                Boolean dismissOnSelect, params DialogGUIBase[] options) : base(optionText, onSelected, w, h,
                dismissOnSelect, options)
            {
            }

            public DialogGUIButtonFixed(Func<String> getString, Callback onSelected, Single w, Single h,
                Boolean dismissOnSelect, params DialogGUIBase[] options) : base(getString, onSelected, w, h,
                dismissOnSelect, options)
            {
            }

            public DialogGUIButtonFixed(Func<String> getString, Callback onSelected, Func<Boolean> EnabledCondition,
                Single w, Single h, Boolean dismissOnSelect, params DialogGUIBase[] options) : base(getString,
                onSelected, EnabledCondition, w, h, dismissOnSelect, options)
            {
            }

            public DialogGUIButtonFixed(String optionText, Callback onSelected, Func<Boolean> EnabledCondition,
                Boolean dismissOnSelect) : base(optionText, onSelected, EnabledCondition, dismissOnSelect)
            {
            }

            public DialogGUIButtonFixed(String optionText, Callback onSelected, Func<Boolean> EnabledCondition,
                Single w, Single h, Boolean dismissOnSelect, UIStyle style = null) : base(optionText, onSelected,
                EnabledCondition, w, h, dismissOnSelect, style)
            {
            }

            public DialogGUIButtonFixed(Func<String> getString, Callback onSelected, Func<Boolean> EnabledCondition,
                Single w, Single h, Boolean dismissOnSelect, UIStyle style) : base(getString, onSelected,
                EnabledCondition, w, h, dismissOnSelect, style)
            {
            }

            public DialogGUIButtonFixed(Sprite image, Callback onSelected, Single w, Single h,
                Boolean dismissOnSelect = false) : base(image, onSelected, w, h, dismissOnSelect)
            {
            }

            public DialogGUIButtonFixed(Sprite image, String text, Callback onSelected, Single w, Single h,
                Boolean dismissOnSelect = false) : base(image, text, onSelected, w, h, dismissOnSelect)
            {
            }
        }
    }
}