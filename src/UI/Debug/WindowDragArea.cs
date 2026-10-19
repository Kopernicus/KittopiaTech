using KSP.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KittopiaTech.UI.Debug;

/// <summary>
/// Stands in for <see cref="UIWindowArea"/> on window title bars and resize handles. The stock
/// one turns off pixel-perfect on the whole canvas while dragging, which makes every text on
/// the debug canvas rebuild (seconds, with the Alt+F12 console open) when it switches back.
/// </summary>
public sealed class WindowDragArea : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public bool moveWindow;
    public bool resizeWindow;
    public UIWindow.ResizeWidth resizeWidth;
    public UIWindow.ResizeHeight resizeHeight;
    public UIWindow window;

    /// <summary>Swaps every UIWindowArea under <paramref name="root"/> for this component.</summary>
    public static void ReplaceAll(GameObject root)
    {
        foreach (var area in root.GetComponentsInChildren<UIWindowArea>(true))
        {
            var go = area.gameObject;
            var replacement = go.AddComponent<WindowDragArea>();
            replacement.moveWindow = area.moveWindow;
            replacement.resizeWindow = area.resizeWindow;
            replacement.resizeWidth = area.resizeWidth;
            replacement.resizeHeight = area.resizeHeight;
            replacement.window = area.targetWindow != null ? area.targetWindow.GetComponent<UIWindow>() : null;
            Object.DestroyImmediate(area);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (window != null && window.maxSizeIsScreen)
            window.maxSize = new Vector2(Screen.width, Screen.height);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (window == null)
            return;
        if (moveWindow)
            window.MoveWindow(eventData);
        if (resizeWindow)
            window.ResizeWindow(eventData, resizeHeight, resizeWidth);
    }
}
