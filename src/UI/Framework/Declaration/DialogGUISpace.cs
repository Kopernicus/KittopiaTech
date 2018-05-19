using System;
using System.Diagnostics.CodeAnalysis;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUISpace GUISpace(Single v, Modifier<DialogGUISpace> modifier = null)
        {
            DialogGUISpace element = new DialogGUISpace(v);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIFlexibleSpace GUIFlexibleSpace(Modifier<DialogGUIFlexibleSpace> modifier = null)
        {
            DialogGUIFlexibleSpace element = new DialogGUIFlexibleSpace();
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}