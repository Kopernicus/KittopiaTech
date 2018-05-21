using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUIImage GUIImage(Vector2 s, Vector2 p, Color t, Texture i, Modifier<DialogGUIImage> modifier = null)
        {
            DialogGUIImage element = new DialogGUIImage(s, p, t, i);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIImage GUIImage(Vector2 s, Vector2 p, Color t, Texture i, Rect uv, Modifier<DialogGUIImage> modifier = null)
        {
            DialogGUIImage element = new DialogGUIImage(s, p, t, i, uv);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
        
        // Kittopia Additions

        public static DialogGUIImage GUIImage(Texture i, Single x, Single y, Modifier<DialogGUIImage> modifier = null)
        {
            DialogGUIImage element = new DialogGUIImage(new Vector2(x, y), Vector2.zero, Color.white, i);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIImage GUIImage(Texture i, Single x, Single y, Color t, Modifier<DialogGUIImage> modifier = null)
        {
            DialogGUIImage element = new DialogGUIImage(new Vector2(x, y), Vector2.zero, t, i);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
    }
}