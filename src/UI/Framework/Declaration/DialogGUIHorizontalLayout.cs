using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUIHorizontalLayout GUIHorizontalLayout(Action optionBuilder, Modifier<DialogGUIHorizontalLayout> modifier = null)
        {
            DialogGUIHorizontalLayout element = new DialogGUIHorizontalLayout(Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIHorizontalLayout GUIHorizontalLayout(TextAnchor achr, Action optionBuilder, Modifier<DialogGUIHorizontalLayout> modifier = null)
        {
            DialogGUIHorizontalLayout element = new DialogGUIHorizontalLayout(achr, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIHorizontalLayout GUIHorizontalLayout(Single minWidth, Single minHeight, Action optionBuilder, Modifier<DialogGUIHorizontalLayout> modifier = null)
        {
            DialogGUIHorizontalLayout element = new DialogGUIHorizontalLayout(minWidth, minHeight, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIHorizontalLayout GUIHorizontalLayout(Boolean sw, Boolean sh, Action optionBuilder, Modifier<DialogGUIHorizontalLayout> modifier = null)
        {
            DialogGUIHorizontalLayout element = new DialogGUIHorizontalLayout(sw, sh, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIHorizontalLayout GUIHorizontalLayout(Boolean sw, Boolean sh, Single sp, RectOffset pad, TextAnchor achr, Action optionBuilder, Modifier<DialogGUIHorizontalLayout> modifier = null)
        {
            DialogGUIHorizontalLayout element = new DialogGUIHorizontalLayout(sw, sh, sp, pad, achr, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIHorizontalLayout GUIHorizontalLayout(Single minWidth, Single minHeight, Single sp, RectOffset pad, TextAnchor achr, Action optionBuilder, Modifier<DialogGUIHorizontalLayout> modifier = null)
        {
            DialogGUIHorizontalLayout element = new DialogGUIHorizontalLayout(minWidth, minHeight, sp, pad, achr, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}