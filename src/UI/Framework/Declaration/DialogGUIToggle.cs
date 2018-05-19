using System;
using UnityEngine;

namespace KittopiaTech.UI.Framework.Declaration
{
    public static partial class DialogGUI
    {
        public static DialogGUIToggle GUIToggle(Boolean set, String lbel, Callback<Boolean> selected, Single w = -1f, Single h = -1f, Modifier<DialogGUIToggle> modifier = null)
        {
            DialogGUIToggle element = new DialogGUIToggle(set, lbel, selected, w, h);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIToggle GUIToggle(Boolean set, Func<String> lbel, Callback<Boolean> selected, Single w = -1f, Single h = -1f, Modifier<DialogGUIToggle> modifier = null)
        {
            DialogGUIToggle element = new DialogGUIToggle(set, lbel, selected, w, h);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIToggle GUIToggle(Func<Boolean> set, String lbel, Callback<Boolean> selected, Single w = -1f, Single h = -1f, Modifier<DialogGUIToggle> modifier = null)
        {
            DialogGUIToggle element = new DialogGUIToggle(set, lbel, selected, w, h);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIToggle GUIToggle(Func<Boolean> set, Func<String> lbel, Callback<Boolean> selected, Single w = -1f, Single h = -1f, Modifier<DialogGUIToggle> modifier = null)
        {
            DialogGUIToggle element = new DialogGUIToggle(set, lbel, selected, w, h);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIToggle GUIToggle(Func<Boolean> set, Func<Sprite> checkSet, Callback<Boolean> selected, Sprite overImage, Single w = -1f, Single h = -1f, Modifier<DialogGUIToggle> modifier = null)
        {
            DialogGUIToggle element = new DialogGUIToggle(set, checkSet, selected, overImage, w, h);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}