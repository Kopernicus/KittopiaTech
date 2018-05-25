using System;
using KSP.UI;
using UnityEngine;

namespace KittopiaTech.UI.Framework
{
    public class ClickThroughBlocker : MonoBehaviour
    {
        private Boolean _locked;

        private PopupDialog _dialog;

        void Start()
        {
            _dialog = GetComponent<PopupDialog>();
        }

        void Update()
        {
            Boolean inputLocked = UIMasterController.Instance &&
                                   RectTransformUtility.RectangleContainsScreenPoint(_dialog.RTrf, Input.mousePosition,
                                       UIMasterController.Instance.uiCamera);
            if (inputLocked && !_locked)
            {
                _locked = true;
                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS,
                    typeof(ClickThroughBlocker).Assembly.FullName);
            }
            else if (!inputLocked && _locked)
            {
                _locked = false;
                InputLockManager.RemoveControlLock(typeof(ClickThroughBlocker).Assembly.FullName);
            }
        }
    }
}