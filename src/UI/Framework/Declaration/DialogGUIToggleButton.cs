using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UniLinq;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUIToggleButton GUIToggleButton(Boolean set, String lbel, Callback<Boolean> selected, Single w = -1f, Single h = 1f, Modifier<DialogGUIToggleButton> modifier = null)
        {
            DialogGUIToggleButton element = new DialogGUIToggleButton(set, lbel, selected, w, h);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIToggleButton GUIToggleButton(Func<Boolean> set, String lbel, Callback<Boolean> selected, Single w = -1f, Single h = 1f, Modifier<DialogGUIToggleButton> modifier = null)
        {
            DialogGUIToggleButton element = new DialogGUIToggleButton(set, lbel, selected, w, h);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
        
        public static DialogGUIToggleGroup GUIToggleGroup(Action optionBuilder, Modifier<DialogGUIToggleGroup> modifier = null)
        {
            DialogGUIToggleGroup element = new DialogGUIToggleGroup(Declare(optionBuilder).Select(e => e as DialogGUIToggle).ToArray());
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
        
        // Kittopia Additions
        
        public static DialogGUIToggleButton GUIToggleButton(Boolean set, String lbel, Callback<Boolean> selected, Action optionBuilder, Modifier<DialogGUIToggleButton> modifier = null)
        {
            DialogGUIToggleButton element = new DialogGUIToggleButton(set, lbel, selected);
            element.AddChildren(Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
        
        public static DialogGUIToggleButton GUIToggleButton(Boolean set, String lbel, Callback<Boolean> selected, Single w, Single h, Action optionBuilder, Modifier<DialogGUIToggleButton> modifier = null)
        {
            DialogGUIToggleButton element = new DialogGUIToggleButton(set, lbel, selected, w, h);
            element.AddChildren(Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIToggleButton GUIToggleButton(Func<Boolean> set, String lbel, Callback<Boolean> selected, Action optionBuilder, Modifier<DialogGUIToggleButton> modifier = null)
        {
            DialogGUIToggleButton element = new DialogGUIToggleButton(set, lbel, selected);
            element.AddChildren(Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIToggleButton GUIToggleButton(Func<Boolean> set, String lbel, Callback<Boolean> selected, Single w, Single h, Action optionBuilder, Modifier<DialogGUIToggleButton> modifier = null)
        {
            DialogGUIToggleButton element = new DialogGUIToggleButton(set, lbel, selected, w, h);
            element.AddChildren(Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}