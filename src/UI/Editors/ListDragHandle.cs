using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KittopiaTech.UI.Editors;

/// <summary>
/// The grip on a list item row. Dragging it shows where the item will land and moves it there
/// on release; dragging near the edge of the scroll view scrolls it.
/// </summary>
public sealed class ListDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    const float EdgeSize = 30f;
    const float EdgeSpeed = 400f;
    static readonly Color IndicatorColor = new(0.45f, 0.65f, 1f);

    RectTransform items;
    RectTransform row;
    int index;
    Action<int, int> move;

    RectTransform indicator;
    CanvasGroup rowGroup;
    ScrollRect scroll;
    PointerEventData drag;
    int target;

    public void Setup(RectTransform items, RectTransform row, int index, Action<int, int> move)
    {
        this.items = items;
        this.row = row;
        this.index = index;
        this.move = move;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        drag = eventData;
        scroll = items.GetComponentInParent<ScrollRect>();
        rowGroup = row.gameObject.AddComponent<CanvasGroup>();
        rowGroup.alpha = 0.5f;

        var go = new GameObject("DropIndicator", typeof(RectTransform));
        go.transform.SetParent(items, false);
        go.AddComponent<LayoutElement>().ignoreLayout = true;
        go.AddComponent<Image>().color = IndicatorColor;
        indicator = (RectTransform)go.transform;
        indicator.anchorMin = new Vector2(0f, 0.5f);
        indicator.anchorMax = new Vector2(1f, 0.5f);
        indicator.sizeDelta = new Vector2(0f, 2f);
        UpdateTarget();
    }

    public void OnDrag(PointerEventData eventData)
    {
        drag = eventData;
        UpdateTarget();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        drag = eventData;
        UpdateTarget();
        int to = target > index ? target - 1 : target;
        Cleanup();
        if (to != index)
            move(index, to);
    }

    void OnDisable() => Cleanup();

    void Update()
    {
        if (drag == null || scroll == null)
            return;

        // Scroll while the pointer is held near the top or bottom of the viewport.
        var viewport = scroll.viewport;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, drag.position, drag.pressEventCamera, out var local))
            return;
        float fromTop = viewport.rect.yMax - local.y;
        float fromBottom = local.y - viewport.rect.yMin;
        float direction = fromTop < EdgeSize ? 1f : fromBottom < EdgeSize ? -1f : 0f;
        if (direction == 0f)
            return;

        float scrollable = scroll.content.rect.height - viewport.rect.height;
        if (scrollable <= 0f)
            return;
        scroll.verticalNormalizedPosition = Mathf.Clamp01(
            scroll.verticalNormalizedPosition + direction * EdgeSpeed * Time.unscaledDeltaTime / scrollable
        );
        UpdateTarget();
    }

    /// <summary>Finds the gap between rows nearest the pointer and moves the indicator there.</summary>
    void UpdateTarget()
    {
        if (indicator == null)
            return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(items, drag.position, drag.pressEventCamera, out var local);

        target = 0;
        float y = 0f;
        foreach (Transform child in items)
        {
            if (child == indicator || !child.gameObject.activeSelf)
                continue;
            var rect = (RectTransform)child;
            float top = rect.localPosition.y + rect.rect.yMax;
            float bottom = rect.localPosition.y + rect.rect.yMin;
            if (target == 0)
                y = top;
            if (local.y < (top + bottom) / 2f)
            {
                target++;
                y = bottom;
            }
        }
        indicator.anchoredPosition = new Vector2(0f, y - items.rect.center.y);
    }

    void Cleanup()
    {
        drag = null;
        if (indicator != null)
            Destroy(indicator.gameObject);
        if (rowGroup != null)
            Destroy(rowGroup);
        indicator = null;
        rowGroup = null;
    }
}
