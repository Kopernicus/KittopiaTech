using System;
using System.Diagnostics.CodeAnalysis;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUIBox GUIBox(String message, Single w, Single h, Func<Boolean> EnabledCondition,
            Action optionBuilder, Modifier<DialogGUIBox> modifier = null)
        {
            DialogGUIBox element = new DialogGUIBox(message, w, h, EnabledCondition, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIBox GUIBox(String message, UIStyle style, Single w, Single h,
            Func<Boolean> EnabledCondition, Action optionBuilder, Modifier<DialogGUIBox> modifier = null)
        {
            DialogGUIBox element = new DialogGUIBox(message, style, w, h, EnabledCondition, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        // Kittopia Additions

        public static DialogGUIBox GUIBox(Single w, Single h, Action optionBuilder,
            Modifier<DialogGUIBox> modifier = null)
        {
            DialogGUIBox element = new DialogGUIBox("", w, h, null, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIBox GUIBox(Action optionBuilder, Modifier<DialogGUIBox> modifier = null)
        {
            DialogGUIBox element = new DialogGUIBox("", -1, -1, null, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}