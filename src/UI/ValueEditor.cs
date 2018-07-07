using System;
using KittopiaTech.UI.Framework;

namespace KittopiaTech.UI
{
    public abstract class ValueEditor : Window
    {
        /// <summary>
        /// The name of the Window
        /// </summary>
        protected String _name;

        /// <summary>
        /// The reference object for setting the value of the member
        /// </summary>
        protected Object Reference
        {
            get { return _getReference(); }
        }
        private Func<Object> _getReference;

        /// <summary>
        /// Getter for the edited value
        /// </summary>
        protected Func<Object> GetValue;

        /// <summary>
        /// Setter for the edited value
        /// </summary>
        protected Action<Object> SetValue;

        public ValueEditor(String name, Func<Object> reference, Func<Object> getValue, Action<Object> setValue)
        {
            _name = name;
            _getReference = reference;
            GetValue = getValue;
            SetValue = setValue;
        }
        
        public override String GetTitle()
        {
            return "KittopiaTech - " + _name;
        }

        protected override void OnOpen()
        {
            TaskListWindow.Instance.Add(this);
        }

        protected override void OnClose()
        {
            TaskListWindow.Instance.Remove(this);
        }
    }
}