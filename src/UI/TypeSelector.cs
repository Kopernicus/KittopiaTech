using System;
using System.Linq;
using KittopiaTech.UI.Framework;
using Kopernicus.ConfigParser;
using UnityEngine;
using UnityEngine.UI;
using static KittopiaTech.UI.Framework.Declaration.DialogGUI;

namespace KittopiaTech.UI
{
    public class TypeSelector : Window
    {
        /// <summary>
        /// The type the selectable types extend
        /// </summary>
        private Type _baseType;

        /// <summary>
        /// The function that gets invoked when a type was selected
        /// </summary>
        private Action<Type> _callback;

        /// <summary>
        /// The name of the object we are editing
        /// </summary>
        private String _name;
        
        public TypeSelector(Type baseType, String name, Action<Type> callback)
        {
            _baseType = baseType;
            _callback = callback;
            _name = name;
        }
        
        public override String GetTitle()
        {
            return "KittopiaTech - " + _name;
        }

        protected override void BuildDialog()
        {
            // Skin 
            Skin = KittopiaTech.Skin;
            
            // Display a scroll area
            GUIScrollList(new Vector2(390, 600), false, true, () =>
            {
                GUIVerticalLayout(true, false, 2f, new RectOffset(8, 26, 8, 8), TextAnchor.UpperLeft, () =>
                {
                    GUIContentSizer(ContentSizeFitter.FitMode.Unconstrained,
                        ContentSizeFitter.FitMode.PreferredSize, true);

                    Type[] types = Parser.ModTypes
                        .Where(t => !Equals(t.Assembly, typeof(HighLogic).Assembly) && _baseType.IsAssignableFrom(t) &&
                                    !t.IsAbstract && !t.IsInterface)
                        .ToArray();

                    for (Int32 j = 0; j < types.Length; j++)
                    {
                        Int32 i = j;

                        GUIButton(types[i].Name, () =>
                        {
                            _callback(types[i]);
                            Close();
                        });
                    }
                });
            });
        }

        public override Single GetWidth()
        {
            return 400;
        }
    }
}