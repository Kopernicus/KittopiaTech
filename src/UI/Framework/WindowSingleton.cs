namespace KittopiaTech.UI.Framework
{
    /// <summary>
    /// Base class for windows that can only have one active instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class WindowSingleton<T> : Window<T> where T : class, new()
    {
        /// <summary>
        /// The internal instance variable
        /// </summary>
        private static T _instance;

        /// <summary>
        /// The currently active Instance of this window
        /// </summary>
        public static T Instance => _instance ?? (_instance = new T());
    }
}