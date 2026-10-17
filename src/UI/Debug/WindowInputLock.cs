using UnityEngine;
using UnityEngine.EventSystems;

namespace KittopiaTech.UI.Debug;

/// <summary>
/// Locks game input while the pointer is over a window. Unlike DialogMouseEnterControlLock it
/// also holds the lock while a dropdown is open: TMP covers the screen with a "Blocker" that
/// steals the hover, so the window sees a pointer exit as soon as the list appears.
/// </summary>
public sealed class WindowInputLock : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    const string BlockerName = "Blocker";

    public ControlTypes controlTypes = ControlTypes.ALLBUTCAMERAS;
    string lockName;
    bool hovered;
    bool locked;
    GameObject blocker;

    void Awake() => lockName = $"KittopiaTech_{GetInstanceID()}";

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        var target = eventData.pointerCurrentRaycast.gameObject;
        if (target != null && target.name == BlockerName)
            blocker = target;
    }

    void LateUpdate() => SetLocked(hovered || blocker != null);

    void OnDisable()
    {
        hovered = false;
        blocker = null;
        SetLocked(false);
    }

    void SetLocked(bool value)
    {
        if (value == locked)
            return;
        locked = value;
        if (value)
            InputLockManager.SetControlLock(controlTypes, lockName);
        else
            InputLockManager.RemoveControlLock(lockName);
    }
}
