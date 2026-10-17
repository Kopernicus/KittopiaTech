using KittopiaTech.UI.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KittopiaTech.UI;

/// <summary>Shows <see cref="text"/> in a shared panel after the pointer rests on this object.</summary>
public sealed class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    const float Delay = 0.4f;
    const float MaxWidth = 320f;
    const float Padding = 6f;

    static RectTransform panel;
    static TextMeshProUGUI label;
    static Tooltip owner;

    public string text;
    float shownAt = -1f;

    public static void Attach(GameObject target, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        target.AddComponent<Tooltip>().text = text;
        // Cloned labels don't take raycasts, so they'd never see the pointer.
        foreach (var graphic in target.GetComponentsInChildren<TMP_Text>())
            graphic.raycastTarget = true;
    }

    public void OnPointerEnter(PointerEventData eventData) => shownAt = Time.unscaledTime + Delay;

    public void OnPointerExit(PointerEventData eventData) => Hide();

    void OnDisable() => Hide();

    void Update()
    {
        if (shownAt < 0f || Time.unscaledTime < shownAt || owner == this)
            return;
        Show();
    }

    void Hide()
    {
        shownAt = -1f;
        if (owner == this && panel != null)
            panel.gameObject.SetActive(false);
        if (owner == this)
            owner = null;
    }

    void Show()
    {
        owner = this;
        if (panel == null)
            Build();

        label.text = text;
        var size = label.GetPreferredValues(text, MaxWidth, 0f);
        float width = Mathf.Min(size.x, MaxWidth);
        float height = label.GetPreferredValues(text, width, 0f).y;
        panel.sizeDelta = new Vector2(width + 2f * Padding, height + 2f * Padding);

        // Below and to the right of the pointer, kept inside the canvas.
        var canvas = (RectTransform)panel.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas, Input.mousePosition, canvas.GetComponentInParent<Canvas>().worldCamera, out var local);
        var bounds = canvas.rect;
        var position = local + new Vector2(14f, -18f);
        position.x = Mathf.Min(position.x, bounds.xMax - panel.sizeDelta.x);
        position.y = Mathf.Max(position.y, bounds.yMin + panel.sizeDelta.y);
        panel.anchoredPosition = position;

        panel.SetAsLastSibling();
        panel.gameObject.SetActive(true);
    }

    static void Build()
    {
        var go = new GameObject($"{HotReloadSupport.ObjectPrefix}Tooltip", typeof(RectTransform));
        go.layer = DebugSkin.Canvas.gameObject.layer;
        go.transform.SetParent(DebugSkin.Canvas, false);
        panel = (RectTransform)go.transform;
        // Anchored at the canvas centre, pivot top-left, so positions are canvas-local.
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0f, 1f);

        var background = go.AddComponent<Image>();
        background.color = new Color(0.1f, 0.1f, 0.11f, 0.96f);
        background.raycastTarget = false;
        go.AddComponent<Outline>().effectColor = new Color(0.45f, 0.46f, 0.48f);

        label = DebugSkin.CreateLabel(go.transform, "");
        var container = (RectTransform)label.transform.parent;
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = new Vector2(Padding, Padding);
        container.offsetMax = new Vector2(-Padding, -Padding);
        label.enableWordWrapping = true;
        label.alignment = TextAlignmentOptions.TopLeft;
        foreach (var graphic in go.GetComponentsInChildren<Graphic>())
            graphic.raycastTarget = false;
    }
}
