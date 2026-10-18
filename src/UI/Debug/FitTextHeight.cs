using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Debug;

/// <summary>
/// Makes a label as tall as its wrapped text. Labels are a fixed-height wrapper around a
/// stretched text, so text that wraps would otherwise spill over whatever comes next.
/// </summary>
public sealed class FitTextHeight : MonoBehaviour, ILayoutElement
{
    TextMeshProUGUI text;
    float height;

    public static void Attach(TextMeshProUGUI label)
    {
        label.enableWordWrapping = true;
        label.transform.parent.gameObject.AddComponent<FitTextHeight>().text = label;
    }

    public void CalculateLayoutInputHorizontal() { }

    // Widths are already set when heights are calculated.
    public void CalculateLayoutInputVertical()
    {
        float width = ((RectTransform)transform).rect.width;
        height = width > 0f ? text.GetPreferredValues(text.text, width, 0f).y : -1f;
    }

    public float minWidth => -1f;
    public float preferredWidth => -1f;
    public float flexibleWidth => -1f;
    public float minHeight => height;
    public float preferredHeight => height;
    public float flexibleHeight => -1f;

    // Above the wrapper's own LayoutElement.
    public int layoutPriority => 2;
}
