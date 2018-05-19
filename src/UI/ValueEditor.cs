using System;
using System.Reflection;
using KittopiaTech.UI.Framework;
using Kopernicus;

namespace KittopiaTech.UI
{
    public abstract class ValueEditor : Window<ValueEditor>
    {
        /// <summary>
        /// The name of the Window
        /// </summary>
        protected String _name;

        /// <summary>
        /// The Parser Target that is edited
        /// </summary>
        protected ParserTarget Target;

        /// <summary>
        /// The member that is connected to the Parser Target
        /// </summary>
        protected MemberInfo Member;

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
        protected Func<String> GetValue;

        /// <summary>
        /// Setter for the edited value
        /// </summary>
        protected Func<String, String> SetValue;

        public ValueEditor(String name, ParserTarget target, MemberInfo member, Func<Object> reference, Func<String> getValue, Func<String, String> setValue)
        {
            _name = name;
            Target = target;
            Member = member;
            _getReference = reference;
            GetValue = getValue;
            SetValue = setValue;
        }
        
        public override String GetTitle()
        {
            return "KittopiaTech - " + _name;
        }

        protected void Integrate(ValueEditor other)
        {
            other.BuildDialog();
        }
    }
}