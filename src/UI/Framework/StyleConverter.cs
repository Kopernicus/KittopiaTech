using UnityEngine;

namespace KittopiaTech.UI.Framework
{
    public static class StyleConverter
    {
        public static UISkinDef Convert(GUISkin skin)
        {
            UISkinDef def = new UISkinDef();
            if (skin != null)
            {
                def.box = ConvertStyle(skin.box);
                def.button = ConvertStyle(skin.button);
                def.font = skin.font;
                def.horizontalScrollbar = ConvertStyle(skin.horizontalScrollbar);
                def.horizontalScrollbarLeftButton = ConvertStyle(skin.horizontalScrollbarLeftButton);
                def.horizontalScrollbarRightButton = ConvertStyle(skin.horizontalScrollbarRightButton);
                def.horizontalScrollbarThumb = ConvertStyle(skin.horizontalScrollbarThumb);
                def.horizontalSlider = ConvertStyle(skin.horizontalSlider);
                def.horizontalSliderThumb = ConvertStyle(skin.horizontalSliderThumb);
                def.label = ConvertStyle(skin.label);
                def.name = skin.name;
                def.scrollView = ConvertStyle(skin.scrollView);
                def.textArea = ConvertStyle(skin.textArea);
                def.textField = ConvertStyle(skin.textField);
                def.toggle = ConvertStyle(skin.toggle);
                def.verticalScrollbar = ConvertStyle(skin.verticalScrollbar);
                def.verticalScrollbarDownButton = ConvertStyle(skin.verticalScrollbarDownButton);
                def.verticalScrollbarThumb = ConvertStyle(skin.verticalScrollbarThumb);
                def.verticalScrollbarUpButton = ConvertStyle(skin.verticalScrollbarUpButton);
                def.verticalSlider = ConvertStyle(skin.verticalSlider);
                def.verticalSliderThumb = ConvertStyle(skin.verticalSliderThumb);
                def.window = ConvertStyle(skin.window, new RectOffset(5, 5, 5, 5));
            }

            return def;
        }

        private static UIStyle ConvertStyle(GUIStyle guiStyle, RectOffset border = null)
        {
            UIStyle style = new UIStyle();
            if (guiStyle != null)
            {
                style.active = ConvertStyleState(guiStyle.active, border ?? guiStyle.border);
                style.alignment = guiStyle.alignment;
                style.disabled = ConvertStyleState(guiStyle.active, border ?? guiStyle.border);
                style.clipping = guiStyle.clipping;
                style.fixedHeight = guiStyle.fixedHeight;
                style.fixedWidth = guiStyle.fixedWidth;
                style.font = guiStyle.font;
                style.fontSize = guiStyle.fontSize;
                style.fontStyle = guiStyle.fontStyle;
                style.highlight = ConvertStyleState(guiStyle.focused, border ?? guiStyle.border);
                style.lineHeight = guiStyle.lineHeight;
                style.name = guiStyle.name;
                style.normal = ConvertStyleState(guiStyle.normal, border ?? guiStyle.border);
                style.richText = guiStyle.richText;
                style.stretchHeight = guiStyle.stretchHeight;
                style.stretchWidth = guiStyle.stretchWidth;
                style.wordWrap = guiStyle.wordWrap;
            }

            return style;
        }

        private static UIStyleState ConvertStyleState(GUIStyleState guiStyleState, RectOffset border)
        {
            UIStyleState state = new UIStyleState();
            if (guiStyleState != null)
            {
                if (guiStyleState.background != null)
                {
                    state.background = Sprite.Create(guiStyleState.background,
                        new Rect(0, 0, guiStyleState.background.width, guiStyleState.background.height),
                        new Vector2(0.5f, 0.5f), 100, 1, SpriteMeshType.Tight,
                        new Vector4(border.left, border.bottom, border.right, border.top));
                }

                state.textColor = guiStyleState.textColor;
            }

            return state;
        }
    }
}