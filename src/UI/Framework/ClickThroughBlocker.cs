using UnityEngine;

namespace KittopiaTech.UI.Framework
{
    public class ClickThroughBlocker : MonoBehaviour
    {
        void OnMouseEnter()
        {
            EditorLogic.fetch.Lock(true, true, true, typeof(ClickThroughBlocker).Assembly.FullName);
            InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, typeof(ClickThroughBlocker).Assembly.FullName);
        }

        void OnMouseExit()
        {
            EditorLogic.fetch.Unlock(typeof(ClickThroughBlocker).Assembly.FullName);
            InputLockManager.RemoveControlLock(typeof(ClickThroughBlocker).Assembly.FullName);
        }
    }
}