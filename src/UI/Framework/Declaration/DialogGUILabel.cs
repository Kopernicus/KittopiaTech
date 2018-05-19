using System;
using System.Diagnostics.CodeAnalysis;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUILabel GUILabel(String message, Boolean expandW = false, Boolean expandH = false, Modifier<DialogGUILabel> modifier = null)
        {
            DialogGUILabel element = new DialogGUILabel(message, expandW, expandH);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUILabel GUILabel(String message, Single width, Single height = 0.0f, Modifier<DialogGUILabel> modifier = null)
        {
            DialogGUILabel element = new DialogGUILabel(message, width, height);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUILabel GUILabel(String message, UIStyle style, Boolean expandW = false, Boolean expandH = false, Modifier<DialogGUILabel> modifier = null)
        {
            DialogGUILabel element = new DialogGUILabel(message, style, expandW, expandH);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUILabel GUILabel(Func<String> getString, UIStyle style, Boolean expandW = false, Boolean expandH = false, Modifier<DialogGUILabel> modifier = null)
        {
            DialogGUILabel element = new DialogGUILabel(getString, style, expandW, expandH);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUILabel GUILabel(Func<String> getString, Boolean expandW = false, Boolean expandH = false, Modifier<DialogGUILabel> modifier = null)
        {
            DialogGUILabel element = new DialogGUILabel(getString, expandW, expandH);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUILabel GUILabel(Boolean flexH, Func<String> getString, Single width, Single height = 0.0f, Modifier<DialogGUILabel> modifier = null)
        {
            DialogGUILabel element = new DialogGUILabel(flexH, getString, width, height);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUILabel GUILabel(Func<String> getString, Single width, Single height = 0.0f, Modifier<DialogGUILabel> modifier = null)
        {
            DialogGUILabel element = new DialogGUILabel(getString, width, height);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}