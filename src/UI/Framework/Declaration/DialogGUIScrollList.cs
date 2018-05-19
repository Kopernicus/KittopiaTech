using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUIScrollList GUIScrollList(Vector2 size, Boolean hScroll, Boolean vScroll, Action optionsBuilder, Modifier<DialogGUIScrollList> modifier = null)
        {
            DialogGUIScrollList element = new DialogGUIScollListFixed(size, hScroll, vScroll, Declare(optionsBuilder)[0] as DialogGUILayoutBase);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        public static DialogGUIScrollList GUIScrollList(Vector2 size, Vector2 contentSize, Boolean hScroll, Boolean vScroll, Action optionsBuilder, Modifier<DialogGUIScrollList> modifier = null)
        {
            DialogGUIScrollList element = new DialogGUIScollListFixed(size, contentSize, hScroll, vScroll, Declare(optionsBuilder)[0] as DialogGUILayoutBase);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }
        
        public static DialogGUIContentSizer GUIContentSizer(ContentSizeFitter.FitMode widthMode, ContentSizeFitter.FitMode heightMode, Boolean useParentSize = false, Modifier<DialogGUIContentSizer> modifier = null)
        {
            DialogGUIContentSizer element = new DialogGUIContentSizer(widthMode, heightMode, useParentSize);
            if (modifier != null)
            {
                element = modifier(element);
            }
            _elements.Add(element);
            return element;
        }

        private class DialogGUIScollListFixed : DialogGUIScrollList
        {
            public override GameObject Create(ref Stack<Transform> layouts, UISkinDef skin)
            {
                GameObject ret = base.Create(ref layouts, skin);
                scrollRect.onValueChanged.AddListener(v =>
                {
                    FixPosition();
                });
                _fixed = false;
                return ret;
            }

            private Boolean _fixed;

            private void FixPosition()
            {
                if (_fixed)
                {
                    return;
                }

                if (scrollRect != null && scrollRect.vertical)
                {
                    scrollRect.verticalNormalizedPosition = scrollRect.verticalScrollbar.value =
                        scrollRect.verticalScrollbar.direction != Scrollbar.Direction.BottomToTop ? 0f : 1f;
                }

                if (scrollRect != null && scrollRect.horizontal)
                {
                    scrollRect.horizontalNormalizedPosition = scrollRect.horizontalScrollbar.value =
                        scrollRect.horizontalScrollbar.direction != Scrollbar.Direction.LeftToRight ? 1f : 0f;
                }

                _fixed = true;
            }

            public DialogGUIScollListFixed(Vector2 size, Boolean hScroll, Boolean vScroll, DialogGUILayoutBase layout) :
                base(size, hScroll, vScroll, layout)
            {
            }

            public DialogGUIScollListFixed(Vector2 size, Vector2 contentSize, Boolean hScroll, Boolean vScroll,
                DialogGUILayoutBase layout) : base(size, contentSize, hScroll, vScroll, layout)
            {
            }
        }
    }
}