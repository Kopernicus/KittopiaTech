using System;
using System.Collections.Generic;
using System.Linq;
using KittopiaTech.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI
{
    public class TaskListWindow : WindowSingleton<TaskListWindow>
    {
        // The list of all windows that are currently open
        private List<Window> _windows = new List<Window>();
        
        public override String GetTitle()
        {
            return "KittopiaTech - Windows";
        }

        protected override void BuildDialog()
        {
            // Skin
            Skin = KittopiaTech.Skin;
            
            // Display a scroll area
            GUIScrollList(new Vector2(290f, 500f), false, true, () =>
            {
                GUIVerticalLayout(true, false, 2f, new RectOffset(8, 26, 8, 8), TextAnchor.UpperLeft, () =>
                {
                    GUIContentSizer(ContentSizeFitter.FitMode.Unconstrained,
                        ContentSizeFitter.FitMode.PreferredSize, true);
                    
                    for (Int32 j = 0; j < _windows.Count; j++)
                    {
                        Int32 i = j;

                        GUIHorizontalLayout(false, false, () => 
                        {
                            GUIToggleButton(() => _windows[i].IsVisible, _windows[i].GetTitle(),
                                s =>
                                {
                                    if (s)
                                    {
                                        _windows[i].Show();
                                    }
                                    else
                                    {
                                        _windows[i].Hide();
                                    }
                                }, -1, 25f);
                            GUIButton("x", () =>
                            {
                                _windows[i].Close();
                            }, 25f, 25f, false, () => { });
                        });
                    }
                });
            });
        }

        public override Single GetWidth()
        {
            return 300;
        }

        public void Add(Window window)
        {
            _windows.Add(window);
            Redraw();
        }

        public void Remove(Window window)
        {
            _windows.Remove(window);
            Redraw();
        }
    }
}