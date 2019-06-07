using System;
using System.Collections.Generic;
using System.Linq;

namespace KittopiaTech.UI.Framework.Declaration
{
    /// <summary>
    /// A class to simplifly the declaration of a DialogGUI
    /// </summary>
    public static partial class DialogGUI
    {
        /// <summary>
        /// The edited elements
        /// </summary>
        private static List<DialogGUIBase> _elements = new List<DialogGUIBase>();

        /// <summary>
        /// Declares a DialogGUI
        /// </summary>
        public static DialogGUIBase[] Declare(Action builder)
        {
            // Backup the old list
            List<DialogGUIBase> old = _elements.ToList();
            _elements.Clear();
            
            // Build the dialog
            if (builder != null)
            {
                builder();
            }
            
            // Store and return it
            DialogGUIBase[] result = _elements.ToArray();
            _elements = old;
            return result;
        }
    }
}