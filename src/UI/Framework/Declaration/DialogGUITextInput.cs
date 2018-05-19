using System;
using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine.UI;

namespace KittopiaTech.UI.Framework.Declaration
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static partial class DialogGUI
    {
        public static DialogGUITextInput GUITextInput(String txt, Boolean multiline, Int32 maxlength,
            Func<String, String> textSetFunc, Single hght = -1f, Modifier<DialogGUITextInput> modifier = null)
        {
            DialogGUITextInput element = new DialogGUITextInput(txt, multiline, maxlength, textSetFunc, hght);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUITextInput GUITextInput(String txt, Boolean multiline, Int32 maxlength,
            Func<String, String> textSetFunc, Single w, Single hght, Modifier<DialogGUITextInput> modifier = null)
        {
            DialogGUITextInput element = new DialogGUITextInput(txt, multiline, maxlength, textSetFunc, w, hght);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUITextInput GUITextInput(String txt, String placeHlder, Boolean multiline, Int32 maxlength,
            Func<String, String> textSetFunc, Single hght = -1f, Modifier<DialogGUITextInput> modifier = null)
        {
            DialogGUITextInput element =
                new DialogGUITextInput(txt, placeHlder, multiline, maxlength, textSetFunc, hght);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUITextInput GUITextInput(String txt, String placeHlder, Boolean multiline, Int32 maxlength,
            Func<String, String> textSetFunc, Single w, Single hght, Modifier<DialogGUITextInput> modifier = null)
        {
            DialogGUITextInput element =
                new DialogGUITextInput(txt, placeHlder, multiline, maxlength, textSetFunc, w, hght);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }

        public static DialogGUITextInput GUITextInput(String txt, Boolean multiline, Int32 maxlength,
            Func<String, String> textSetFunc, Func<String> getString, TMP_InputField.ContentType contentType,
            Single hght = -1f, Modifier<DialogGUITextInput> modifier = null)
        {
            DialogGUITextInput element =
                new DialogGUITextInput(txt, multiline, maxlength, textSetFunc, getString, contentType, hght);
            if (modifier != null)
            {
                element = modifier(element);
            }
            element.OnUpdate += () =>
            {
                if (element.uiItem == null) return;
                if (element.uiItem.GetComponent<FixScrollRect>() != null) return;
                element.uiItem.AddComponent<FixScrollRect>().MainScroll =
                    Part.GetComponentUpwards<ScrollRect>(element.uiItem);
            };
            _elements.Add(element);
            return element;
        }
    }
}