using System;
using KittopiaTech.UI.Framework.Declaration;
using KSP.UI.TooltipTypes;
using UnityEngine;

namespace KittopiaTech.UI.Framework
{
    /// <summary>
    /// Base class for the UI namespace
    /// </summary>
    public abstract class Window<T> where T : class
    {
        /// <summary>
        /// Whether the window is created
        /// </summary>
        public Boolean IsOpen { get; protected set; }

        /// <summary>
        /// Whether the Window is visible
        /// </summary>
        public Boolean IsVisible { get; protected set; }

        /// <summary>
        /// The skin used by the window
        /// </summary>
        public UISkinDef Skin = HighLogic.UISkin;

        /// <summary>
        /// The template for the window
        /// </summary>
        protected MultiOptionDialog Template;

        /// <summary>
        /// The dialog element that gets created
        /// </summary>
        protected PopupDialog Dialog;

        /// <summary>
        /// Returns the title of the window
        /// </summary>
        public abstract String GetTitle();

        /// <summary>
        /// Returns the dialog elements that assemble the window
        /// </summary>
        protected abstract void BuildDialog();

        /// <summary>
        /// Returns the width of the window
        /// </summary>
        public abstract Single GetWidth();

        /// <summary>
        /// The current position of the window on the screen
        /// </summary>
        protected Vector2 Position = new Vector2(400f, 60f);

        /// <summary>
        /// The base for building tooltip objects
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Tooltip_Text Prefab = AssetBase.GetPrefab<Tooltip_Text>("Tooltip_Text");
        
        protected void Integrate(Window<T> other)
        {
            other.BuildDialog();
        }

        /// <summary>
        /// Opens a new window
        /// </summary>
        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = IsVisible = true;
            Template = new MultiOptionDialog(GetTitle(), "", GetTitle(), HighLogic.UISkin, GetWidth(),
                DialogGUI.Declare(BuildDialog));
            Dialog = PopupDialog.SpawnPopupDialog(new Vector2(0f, 1f), new Vector2(0f, 1f), Template, true,
                Skin, false);
            Dialog.SetDraggable(true);
            Dialog.RTrf.anchoredPosition = new Vector2(Position.x, -Position.y);
            Dialog.gameObject.AddComponent<ClickThroughBlocker>();
        }

        /// <summary>
        /// Toggles the visibility of the window
        /// </summary>
        public void ToggleVisibility()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Hides the window
        /// </summary>
        public void Hide()
        {
            if (IsVisible)
            {
                Dialog.gameObject.SetActive(IsVisible = false);
                OnHide();
            }
        }
        
        protected virtual void OnHide() {}

        /// <summary>
        /// Shows the window
        /// </summary>
        public void Show()
        {
            if (!IsVisible)
            {
                Dialog.gameObject.SetActive(IsVisible = true);
                OnShow();
            }
        }
        
        protected virtual void OnShow() {}

        public void Close()
        {
            if (IsOpen)
            {
                Vector2 pos = Dialog.RTrf.anchoredPosition;
                Position = new Vector2(pos.x, -pos.y);
                Dialog.Dismiss();
                UnityEngine.Object.DestroyImmediate(Dialog.popupWindow);
                IsOpen = IsVisible = false;
                OnClose();
            }
        }
        
        protected virtual void OnClose() {}

        /// <summary>
        /// Updates the dialog
        /// </summary>
        public void Redraw()
        {
            if (!IsOpen)
                return;
            Vector2 pos = Dialog.RTrf.anchoredPosition;
            Template = new MultiOptionDialog(GetTitle(), "", GetTitle(), HighLogic.UISkin, GetWidth(),
                DialogGUI.Declare(BuildDialog));
            Dialog.Dismiss();
            UnityEngine.Object.Destroy(Dialog.popupWindow);
            Dialog = PopupDialog.SpawnPopupDialog(new Vector2(0f, 1f), new Vector2(0f, 1f), Template, true,
                Skin, false);
            Dialog.SetDraggable(true);
            Dialog.gameObject.SetActive(IsVisible);
            Dialog.RTrf.anchoredPosition = pos;
            foreach (RectTransform transform in Dialog.RTrf.GetComponentsInChildren<RectTransform>())
            {
                transform.gameObject.AddComponent<ClickThroughBlocker>();
            }
        }

        /// <summary>
        /// Adds a tooltip to the UI element
        /// </summary>
        protected DialogGUIBase Tooltip(DialogGUIBase dialog, String tip)
        {
            dialog.tooltipText = tip;
            if (dialog.tooltipText != "")
            {
                dialog.OnUpdate += () =>
                {
                    if (dialog.uiItem == null) return;
                    TooltipController_Text tt = dialog.uiItem.AddOrGetComponent<TooltipController_Text>();
                    if (tt == null) return;
                    tt.textString = tip;
                    tt.prefab = Prefab;
                };
            }

            return dialog;
        }

        protected DialogGUIBase OnUpdate(DialogGUIBase dialog, Action<DialogGUIBase> callback)
        {
            dialog.OnUpdate += () => callback(dialog);
            return dialog;
        }
    }
}
