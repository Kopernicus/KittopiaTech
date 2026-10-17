using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Debug;

// The stock debug screens have no dropdown to clone, so build one from TMP's defaults and
// restyle it with the cloned templates. Copied from BurstPQS's DebugUIManager.
public static partial class DebugSkin
{
    static Sprite _arrowSprite;
    static bool _arrowSearched;

    public static TMP_Dropdown CreateDropdown(Transform parent)
    {
        var buttonImage = _buttonPrefab.GetComponent<Image>();
        var buttonSelectable = _buttonPrefab.GetComponent<Button>();
        var panelImage = _inputFieldPrefab.GetComponent<Image>();
        // Caption and rows sit on the button sprite, so they use the button's label colour.
        var sourceTmp = _buttonPrefab.GetComponentInChildren<TextMeshProUGUI>(true);
        var checkmarkImage = _togglePrefab.GetComponentInChildren<Toggle>(true)?.graphic as Image;
        var scrollbarTrack = _scrollbarPrefab.GetComponent<Image>();
        var scrollbarHandle = _scrollbarPrefab.GetComponent<Scrollbar>()?.targetGraphic as Image;

        var go = TMP_DefaultControls.CreateDropdown(
            new TMP_DefaultControls.Resources
            {
                standard = buttonImage?.sprite,
                background = scrollbarTrack?.sprite,
                checkmark = checkmarkImage?.sprite,
                dropdown = ArrowSprite,
            }
        );
        go.name = "Dropdown";
        go.transform.SetParent(parent, false);
        // The popup has its own Canvas, which is culled unless it's on a layer the UI camera
        // renders. Layout containers are on layer 0, so take the layer from a cloned template.
        SetLayerRecursively(go, _buttonPrefab.layer);

        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();

        CopyImageStyle(go.GetComponent<Image>(), buttonImage);
        if (buttonSelectable != null)
        {
            dropdown.transition = buttonSelectable.transition;
            dropdown.colors = buttonSelectable.colors;
            dropdown.spriteState = buttonSelectable.spriteState;
        }

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = -1f;
        layout.flexibleWidth = 1f;
        layout.minHeight = 30f;

        var labelRect = (RectTransform)go.transform.Find("Label");
        labelRect.offsetMin = new Vector2(8f, 1f);
        labelRect.offsetMax = new Vector2(-24f, -1f);
        StyleText(dropdown.captionText, sourceTmp);
        dropdown.captionText.alignment = TextAlignmentOptions.MidlineLeft;

        // An Image with no sprite draws a white box.
        var arrow = go.transform.Find("Arrow").GetComponent<Image>();
        if (ArrowSprite == null)
            arrow.gameObject.SetActive(false);
        else if (sourceTmp != null)
            arrow.color = sourceTmp.color;

        var template = (RectTransform)go.transform.Find("Template");
        template.sizeDelta = new Vector2(template.sizeDelta.x, 200f);
        template.GetComponent<ScrollRect>().scrollSensitivity = 15f;
        CopyImageStyle(template.GetComponent<Image>(), panelImage ?? buttonImage);

        // Mask clips by sprite alpha; a plain quad keeps the list corners square.
        var viewport = template.Find("Viewport").GetComponent<Image>();
        viewport.sprite = null;
        viewport.type = Image.Type.Simple;
        viewport.color = Color.white;

        // SetupTemplate would add this on first open, on the "Default" sorting layer.
        var templateCanvas = template.gameObject.AddComponent<Canvas>();
        templateCanvas.overrideSorting = true;
        templateCanvas.sortingOrder = 30000;
        template.gameObject.AddComponent<GraphicRaycaster>();
        go.AddComponent<DropdownSortingLayerFix>().template = template;

        var item = (RectTransform)template.Find("Viewport/Content/Item");
        item.sizeDelta = new Vector2(item.sizeDelta.x, 24f);

        var itemToggle = item.GetComponent<Toggle>();
        CopyImageStyle(itemToggle.targetGraphic as Image, buttonImage);
        if (buttonSelectable != null)
        {
            itemToggle.transition = buttonSelectable.transition;
            itemToggle.colors = buttonSelectable.colors;
            itemToggle.spriteState = buttonSelectable.spriteState;
        }
        var itemLabelRect = (RectTransform)item.Find("Item Label");
        itemLabelRect.offsetMin = new Vector2(20f, 1f);
        itemLabelRect.offsetMax = new Vector2(-8f, -2f);
        StyleText(dropdown.itemText, sourceTmp);
        dropdown.itemText.alignment = TextAlignmentOptions.MidlineLeft;

        var itemCheckmark = itemToggle.graphic as Image;
        if (checkmarkImage != null)
            CopyImageStyle(itemCheckmark, checkmarkImage);
        else if (itemCheckmark != null)
            itemCheckmark.gameObject.SetActive(false);

        CopyImageStyle(template.Find("Scrollbar").GetComponent<Image>(), scrollbarTrack);
        CopyImageStyle(
            template.Find("Scrollbar/Sliding Area/Handle").GetComponent<Image>(),
            scrollbarHandle
        );

        return dropdown;
    }

    /// <summary>"dropdown_arrow", the chevron stock dropdowns use; only findable by scanning.</summary>
    static Sprite ArrowSprite
    {
        get
        {
            if (!_arrowSearched)
            {
                _arrowSearched = true;
                foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (sprite.name == "dropdown_arrow")
                        _arrowSprite = sprite;
                }
            }
            return _arrowSprite;
        }
    }

    static void StyleText(TMP_Text text, TextMeshProUGUI source)
    {
        if (text == null || source == null)
            return;

        text.font = source.font;
        text.fontSize = source.fontSize;
        text.color = source.color;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    static void CopyImageStyle(Image dest, Image source)
    {
        if (dest == null || source == null)
            return;

        dest.sprite = source.sprite;
        dest.type = source.type;
        dest.color = source.color;
        dest.material = source.material;
        dest.fillCenter = source.fillCenter;
        dest.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}

/// <summary>
/// Keeps a dropdown's popup on its canvas's sorting layer. The popup canvas overrides sorting
/// and TMP_Dropdown only sets the order, so it would otherwise render behind KSP's layers.
/// </summary>
public class DropdownSortingLayerFix : MonoBehaviour
{
    public RectTransform template;

    void OnEnable()
    {
        var parentCanvas = GetComponentInParent<Canvas>();
        if (template == null || parentCanvas == null)
            return;

        var canvas = template.GetComponent<Canvas>() ?? template.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingLayerID = parentCanvas.rootCanvas.sortingLayerID;
        canvas.sortingOrder = 30000;
    }
}
