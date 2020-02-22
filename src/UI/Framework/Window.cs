using System;
using System.Collections;
using UnityEngine;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;
using Object = UnityEngine.Object;

namespace KittopiaTech.UI.Framework
{
    /// <summary>
    /// Base class for the UI namespace
    /// </summary>
    public abstract class Window
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
        /// Whether the window is currently minimized
        /// </summary>
        protected Boolean Minimized;

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

        private void BuildDialogWrapped()
        {
            GUIVerticalLayout(() =>
            {
                GUIHorizontalLayout(false, false, () =>
                {
                    GUISpace(5f);
                    if (!Minimized)
                    {
                        GUIButton("-", Minimize, 16f, 16f, false, () => { }, ClearButtonImage());
                    }
                    else
                    {
                        GUIButton("+", Maximize, 16f, 16f, false, () => { }, ClearButtonImage());
                    }

                    GUIFlexibleSpace();
                    GUILabel(GetTitle());
                    GUIFlexibleSpace();
                    GUIButton("x", Close, 16f, 16f, false, () => { }, ClearButtonImage());
                    GUISpace(5f);
                });
                if (!Minimized)
                {
                    GUIHorizontalLayout(BuildDialog);
                }
            });
        }
        
        protected void Integrate(Window other)
        {
            other.BuildDialog();
        }

        /// <summary>
        /// Opens a new window
        /// </summary>
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }
            
            IsOpen = IsVisible = true;
            Template = new MultiOptionDialog(GetTitle(), "", GetTitle(), HighLogic.UISkin, GetWidth(),
                Declare(BuildDialogWrapped));
            Template.dialogRect.height = 1;
            Dialog = PopupDialog.SpawnPopupDialog(new Vector2(0f, 1f), new Vector2(0f, 1f), Template, true,
                Skin, false);
            Dialog.RTrf.gameObject.AddComponent<ClickThroughBlocker>();
            Dialog.popupWindow.GetChild("Title").SetActive(false);
            HighLogic.fetch.StartCoroutine(Reposition());
            OnOpen();
        }

        /// <summary>
        /// Toggles the visibility of the window
        /// </summary>
        public void ToggleVisibility()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Hides the window
        /// </summary>
        public void Hide()
        {
            if (!IsVisible)
            {
                return;
            }

            if (!Dialog || !Dialog.gameObject)
            {
                IsOpen = IsVisible = false;
                OnReset();
                return;
            }
            
            Dialog.gameObject.GetComponent<ClickThroughBlocker>().Unlock();
            Dialog.gameObject.SetActive(IsVisible = false);
            OnHide();
        }

        /// <summary>
        /// Shows the window
        /// </summary>
        public void Show()
        {
            if (IsVisible)
            {
                return;
            }

            if (!Dialog || !Dialog.gameObject)
            {
                IsOpen = IsVisible = false;
                OnReset();
                return;
            }
            
            Dialog.gameObject.SetActive(IsVisible = true);
            OnShow();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (!Dialog || !Dialog.gameObject)
            {
                IsOpen = IsVisible = false;
                OnReset();
                return;
            }

            Dialog.gameObject.GetComponent<ClickThroughBlocker>().Unlock();
            Vector2 pos = Dialog.RTrf.anchoredPosition;
            Position = new Vector2(pos.x, -pos.y);
            Dialog.Dismiss();
            Object.DestroyImmediate(Dialog.popupWindow);
            IsOpen = IsVisible = false;
            OnClose();
        }

        /// <summary>
        /// Updates the dialog
        /// </summary>
        public void Redraw()
        {
            if (!IsOpen)
            {
                return;
            }

            if (!Dialog || !Dialog.gameObject)
            {
                IsOpen = IsVisible = false;
                OnReset();
                return;
            }
            
            Close();
            Open();
        }

        public void Minimize()
        {
            if (Minimized)
            {
                return;
            }

            if (!Dialog || !Dialog.gameObject)
            {
                IsOpen = IsVisible = false;
                OnReset();
                return;
            }

            Minimized = true;
            Redraw();
        }

        public void Maximize()
        {
            if (!Minimized)
            {
                return;
            }

            if (!Dialog || !Dialog.gameObject)
            {
                IsOpen = IsVisible = false;
                OnReset();
                return;
            }

            Minimized = false;
            Redraw();
        }
        
        protected virtual void OnShow() {}
        protected virtual void OnHide() {}
        protected virtual void OnOpen() {}
        protected virtual void OnClose() {}
        protected virtual void OnReset() {}

        private IEnumerator Reposition()
        {
            yield return null;
            yield return null;
            Dialog.RTrf.anchoredPosition = new Vector2(Position.x, -Position.y);
        }
    }
}
