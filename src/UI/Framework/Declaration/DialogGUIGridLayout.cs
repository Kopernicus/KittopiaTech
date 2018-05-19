using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUIGridLayout GUIGridLayout(Action optionBuilder, Modifier<DialogGUIGridLayout> modifier = null)
        {
            DialogGUIGridLayout element = new DialogGUIGridLayout(Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
        
        public static DialogGUIGridLayout GUIGridLayout(Modifier<DialogGUIGridLayout> modifier = null)
        {
            DialogGUIGridLayout element = new DialogGUIGridLayout();
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIGridLayout GUIGridLayout(RectOffset padding, Vector2 cellSize, Vector2 spacing,
            GridLayoutGroup.Corner startCorner, GridLayoutGroup.Axis startAxis, TextAnchor childAligment,
            GridLayoutGroup.Constraint constraint, Int32 constraintCount, Action optionBuilder, Modifier<DialogGUIGridLayout> modifier = null)
        {
            DialogGUIGridLayout element = new DialogGUIGridLayout(padding, cellSize, spacing, startCorner, startAxis,
                childAligment, constraint, constraintCount, Declare(optionBuilder));
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}