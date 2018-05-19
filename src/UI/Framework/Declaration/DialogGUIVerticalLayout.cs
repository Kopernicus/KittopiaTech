using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUIVerticalLayout GUIVerticalLayout(Action optionBuilder, Modifier<DialogGUIVerticalLayout> modifier = null)
        {
            DialogGUIVerticalLayout element = new DialogGUIVerticalLayout(Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIVerticalLayout GUIVerticalLayout(Single minWidth, Single minHeight, Action optionBuilder, Modifier<DialogGUIVerticalLayout> modifier = null)
        {
            DialogGUIVerticalLayout element = new DialogGUIVerticalLayout(minWidth, minHeight, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIVerticalLayout GUIVerticalLayout(Boolean sw = false, Boolean sh = false, Modifier<DialogGUIVerticalLayout> modifier = null)
        {
            DialogGUIVerticalLayout element = new DialogGUIVerticalLayout(sw, sh);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIVerticalLayout GUIVerticalLayout(Boolean sw, Boolean sh, Single sp, RectOffset pad, TextAnchor achr, Action optionBuilder, Modifier<DialogGUIVerticalLayout> modifier = null)
        {
            DialogGUIVerticalLayout element = new DialogGUIVerticalLayout(sw, sh, sp, pad, achr, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIVerticalLayout GUIVerticalLayout(Single minWidth, Single minHeight, Single sp, RectOffset pad, TextAnchor achr, Action optionBuilder, Modifier<DialogGUIVerticalLayout> modifier = null)
        {
            DialogGUIVerticalLayout element = new DialogGUIVerticalLayout(minWidth, minHeight, sp, pad, achr, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
        
        // Kittopia Additions

        public static DialogGUIVerticalLayout GUIVerticalLayout(Boolean sw, Boolean sh, Action optionBuilder, Modifier<DialogGUIVerticalLayout> modifier = null)
        {
            DialogGUIVerticalLayout element =
                new DialogGUIVerticalLayout(Declare(optionBuilder)) {stretchWidth = sw, stretchHeight = sh};
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}