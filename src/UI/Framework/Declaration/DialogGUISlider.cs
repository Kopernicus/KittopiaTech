using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Events;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUISlider GUISlider(Func<Single> setValue, Single min, Single max, Boolean wholeNumbers, Single width, Single height, UnityAction<Single> setCallback, Modifier<DialogGUISlider> modifier = null)
        {
            DialogGUISlider element = new DialogGUISlider(setValue, min, max, wholeNumbers, width, height, setCallback);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}