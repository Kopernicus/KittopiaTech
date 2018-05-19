using System;
using System.Collections.Generic;
using KittopiaTech.UI.Framework;
using Kopernicus.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI
{
    /// <summary>
    /// This class handles the main menu of KittopiaTech
    /// </summary>
    public class PlanetSelector : WindowSingleton<PlanetSelector>
    {
        public PlanetSelector()
        {
            Position = new Vector2(60f, 60f);
        }
        
        public override String GetTitle()
        {
            return "KittopiaTech - Planet Selector";
        }

        public override Single GetWidth()
        {
            return 300f;
        }

        /// <summary>
        /// A list of all active body editors
        /// </summary>
        public Dictionary<String, KopernicusEditor> ActiveEditors = new Dictionary<String, KopernicusEditor>();
        
        /// <summary>
        /// The thumbnails of the loaded bodies
        /// </summary>
        public Dictionary<String, Texture2D> Thumbnails = new Dictionary<String, Texture2D>();
        
        /// <summary>
        /// The loaded bodies
        /// </summary>
        public Dictionary<String, Body> Bodies = new Dictionary<String, Body>();

        /// <summary>
        /// Generate the thumbnails and create the body wrappers
        /// </summary>
        public void Init()
        {
            foreach (CelestialBody body in PSystemManager.Instance.localBodies)
            {
                Thumbnails.Add(body.transform.name, PlanetTextureExporter.GetPlanetThumbnail(body));
                Bodies.Add(body.transform.name, new Body(body));
            }
        }

        protected override void BuildDialog()
        {
            // Skin
            Skin = Tools.KittopiaSkin;

            GUIScrollList(new Vector2(290f, 500f), false, true, () =>
            {
                GUIGridLayout(new RectOffset(2, 8, 8, 8), new Vector2(84f, 114f), new Vector2(4f, 4f),
                    GridLayoutGroup.Corner.UpperLeft, GridLayoutGroup.Axis.Horizontal, TextAnchor.UpperLeft,
                    GridLayoutGroup.Constraint.Flexible, 1,
                    () =>
                    {
                        GUIContentSizer(ContentSizeFitter.FitMode.Unconstrained,
                            ContentSizeFitter.FitMode.PreferredSize, true);
                        foreach (CelestialBody body in PSystemManager.Instance.localBodies)
                        {
                            GUIToggleButton(false, "", e => TogglePlanetEditor(body, e), () =>
                            {
                                GUIVerticalLayout(true, false, 2f, new RectOffset(4, 4, 8, 8), TextAnchor.MiddleCenter,
                                    () =>
                                    {
                                        GUIImage(Thumbnails[body.transform.name], -1, -1);
                                        GUISpace(2f);
                                        GUILabel(body.transform.name,
                                            modifier: TextLabelOptions(false,
                                                    overflowMode: TextOverflowModes.Ellipsis)
                                                .And(PreferredHeight<DialogGUILabel>(18f))
                                                .And(Alignment(TextAlignmentOptions.Center)));
                                    });
                            });
                        }
                    });
            });
        }

        protected void TogglePlanetEditor(CelestialBody body, Boolean active)
        {
            if (ActiveEditors.ContainsKey(body.transform.name))
            {
                if (active)
                {
                    ActiveEditors[body.transform.name].Show();
                }
                else
                {
                    ActiveEditors[body.transform.name].Hide();
                }
            }
            else
            {
                KopernicusEditor editor = new KopernicusEditor(() => Bodies[body.transform.name], body, body.transform.name);
                editor.Open();
                ActiveEditors.Add(body.transform.name, editor);
            }
        }
    }
}