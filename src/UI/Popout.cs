using System;
using KittopiaTech.UI.Debug;
using UnityEngine;

namespace KittopiaTech.UI;

/// <summary>
/// A small child window owned by a field editor, for detail views that don't fit in a row
/// (curve tables, previews). It closes when its owner goes away.
/// </summary>
public sealed class Popout : MonoBehaviour
{
    Component owner;

    public Transform Content { get; private set; }

    /// <summary>Raised when the pop-out is destroyed, however it was closed.</summary>
    public event Action Closed;

    public static Popout Open(Component owner, string title, Vector2 size)
    {
        var parts = DebugSkin.CreateWindow(title, DebugSkin.Canvas, size);
        parts.Window.name = $"{HotReloadSupport.ObjectPrefix}Popout({title})";
        // Titles are field or asset paths, which can outgrow the title bar.
        parts.Title.enableWordWrapping = false;
        parts.Title.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        var popout = parts.Window.AddComponent<Popout>();
        popout.owner = owner;
        popout.Content = parts.Content;
        parts.CloseButton.onClick.AddListener(popout.Close);

        // Open beside the owning window rather than on top of it.
        var ownerWindow = owner.GetComponentInParent<InspectorWindow>();
        if (ownerWindow != null)
        {
            var rect = (RectTransform)parts.Window.transform;
            var ownerRect = (RectTransform)ownerWindow.transform;
            rect.anchoredPosition =
                ownerRect.anchoredPosition + new Vector2((ownerRect.rect.width + size.x) / 2f + 8f, 0f);
        }

        parts.Window.SetActive(true);
        return popout;
    }

    public void Close()
    {
        if (this != null)
            Destroy(gameObject);
    }

    void OnDestroy() => Closed?.Invoke();

    void Update()
    {
        // Owner destroyed (row rebuilt, window closed, hot reload): follow it.
        if (owner == null)
            Close();
    }
}
