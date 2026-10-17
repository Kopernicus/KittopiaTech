using KSP.UI;
using KSP.UI.Screens.DebugToolbar;
using KSP.UI.Screens.DebugToolbar.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KittopiaTech.UI.Debug;

/// <summary>
/// Widget templates cloned from the stock Alt+F12 debug screens, so our windows
/// share their look. Copied from KSPTextureLoader's DebugUIManager.
/// </summary>
public static class DebugSkin
{
    static GameObject _labelPrefab;
    static GameObject _buttonPrefab;
    static GameObject _togglePrefab;
    static GameObject _inputFieldPrefab;
    static GameObject _scrollbarPrefab;
    static GameObject _windowPrefab;

    public static bool Initialized { get; private set; }

    /// <summary>Must run after DebugScreenSpawner has started (main menu or later).</summary>
    public static bool Initialize()
    {
        if (Initialized)
            return true;

        var spawner = DebugScreenSpawner.Instance;
        var screens = spawner?.debugScreens?.screens;
        if (screens == null)
        {
            UnityEngine.Debug.LogWarning("[KittopiaTech] DebugSkin: debug screens not available");
            return false;
        }

        foreach (var wrapper in screens)
        {
            if (wrapper.screen == null)
                continue;

            var root = wrapper.screen.transform;
            switch (wrapper.name)
            {
                case "Debug":
                    var bottomBar = root.Find("BottomBar");
                    _inputFieldPrefab ??= Clone(bottomBar?.Find("InputField"), "InputField");
                    _buttonPrefab ??= Clone(bottomBar?.Find("Button"), "Button");
                    break;
                case "Database":
                    _labelPrefab ??= Clone(root.Find("TotalLabel"), "Label");
                    break;
                case "Debugging":
                    if (_togglePrefab == null)
                    {
                        _togglePrefab = Clone(root.Find("PrintErrorsToScreen"), "Toggle");
                        var debugToggle = _togglePrefab?.GetComponent<DebugScreenToggle>();
                        if (debugToggle != null)
                            Object.DestroyImmediate(debugToggle);
                    }
                    break;
            }
        }

        _scrollbarPrefab ??= Clone(
            spawner.screenPrefab?.transform.Find(
                "VerticalLayout/HorizontalLayout/Contents/Contents Scroll View/Scrollbar"
            ),
            "Scrollbar"
        );

        Initialized =
            _labelPrefab != null
            && _buttonPrefab != null
            && _togglePrefab != null
            && _inputFieldPrefab != null
            && _scrollbarPrefab != null;

        if (!Initialized)
        {
            UnityEngine.Debug.LogWarning(
                $"[KittopiaTech] DebugSkin: missing templates. label={_labelPrefab != null}, "
                    + $"button={_buttonPrefab != null}, toggle={_togglePrefab != null}, "
                    + $"input={_inputFieldPrefab != null}, scrollbar={_scrollbarPrefab != null}"
            );
            return false;
        }

        BuildWindowPrefab(spawner);
        return _windowPrefab != null;
    }

    /// <summary>The canvas the debug screen lives on. It persists across scenes.</summary>
    public static Transform Canvas =>
        DebugScreenSpawner.Instance?.GetComponentInParent<Canvas>()?.transform;

    static GameObject Clone(Transform source, string name)
    {
        if (source == null)
            return null;

        var clone = Object.Instantiate(source.gameObject);
        clone.name = $"KittopiaTech_{name}Template";
        clone.SetActive(false);
        Object.DontDestroyOnLoad(clone);
        return clone;
    }

    static GameObject Spawn(GameObject template, Transform parent, string name)
    {
        var go = Object.Instantiate(template, parent, false);
        go.name = name;
        go.SetActive(true);
        return go;
    }

    static void FillWidth(GameObject go, float minHeight = -1)
    {
        var layout = go.GetComponent<LayoutElement>();
        if (layout == null)
            return;

        layout.preferredWidth = -1;
        layout.flexibleWidth = 1;
        if (minHeight >= 0)
            layout.minHeight = minHeight;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // -- Window --

    static void BuildWindowPrefab(DebugScreenSpawner spawner)
    {
        var go = Object.Instantiate(spawner.screenPrefab.gameObject);
        go.name = "KittopiaTech_WindowTemplate";
        go.SetActive(false);
        Object.DontDestroyOnLoad(go);

        // DestroyImmediate runs OnDestroy, which undoes DebugScreen's static registration.
        Object.DestroyImmediate(go.GetComponent<DebugScreen>());
        Object.DestroyImmediate(go.GetComponent<AddDebugScreens>());
        Object.DestroyImmediate(go.GetComponent<UIWindowArea>());

        var titleBar = go.transform.Find("VerticalLayout/TitleBar");
        var toggleButton = titleBar.Find("ToggleContentsButton");
        if (toggleButton != null)
            Object.DestroyImmediate(toggleButton.gameObject);

        // Wrapping TMP inside a HorizontalLayoutGroup inflates the title bar height.
        var title = titleBar.Find("TitleText").GetComponent<TextMeshProUGUI>();
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Ellipsis;

        var verticalLayout = go.transform.Find("VerticalLayout");
        Object.DestroyImmediate(verticalLayout.Find("HorizontalLayout").gameObject);
        verticalLayout.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(verticalLayout, false);
        Stretch((RectTransform)content.transform);

        var contentLayout = content.AddComponent<LayoutElement>();
        contentLayout.flexibleHeight = 1f;
        contentLayout.flexibleWidth = 1f;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 4f;

        _windowPrefab = go;
    }

    public struct WindowParts
    {
        public GameObject Window;
        public TextMeshProUGUI Title;
        public Transform Content;
        public Button CloseButton;
    }

    /// <summary>Instantiates an inactive window so the caller can finish building it.</summary>
    public static WindowParts CreateWindow(string title, Transform parent, Vector2 size)
    {
        var go = Object.Instantiate(_windowPrefab, parent, false);
        go.name = $"KittopiaTech_Window({title})";

        var parts = new WindowParts
        {
            Window = go,
            Title = go
                .transform.Find("VerticalLayout/TitleBar/TitleText")
                .GetComponent<TextMeshProUGUI>(),
            Content = go.transform.Find("VerticalLayout/Content"),
            CloseButton = go
                .transform.Find("VerticalLayout/TitleBar/ExitButton")
                .GetComponent<Button>(),
        };
        parts.Title.text = title;
        parts.CloseButton.onClick.RemoveAllListeners();

        var uiWindow = go.GetComponent<UIWindow>();
        uiWindow.minSize = new Vector2(240f, 160f);
        uiWindow.maxSizeIsScreen = true;

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        go.AddComponent<DialogMouseEnterControlLock>()
            .Setup(ControlTypes.ALLBUTCAMERAS, $"KittopiaTech_{go.GetInstanceID()}");

        return parts;
    }

    // -- Widgets --

    public static TextMeshProUGUI CreateLabel(Transform parent, string text)
    {
        var go = Spawn(_labelPrefab, parent, "Label");
        FillWidth(go);

        // The cloned text child has a fixed 200px width.
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        Stretch(tmp.rectTransform);
        tmp.text = text;
        return tmp;
    }

    public static TextMeshProUGUI CreateHeader(Transform parent, string text)
    {
        var tmp = CreateLabel(parent, text);
        tmp.fontStyle = FontStyles.Bold;
        tmp.fontSize *= 1.2f;
        return tmp;
    }

    public static Button CreateButton(
        Transform parent,
        string text,
        UnityEngine.Events.UnityAction onClick = null
    )
    {
        var go = Spawn(_buttonPrefab, parent, "Button");
        FillWidth(go, 30f);

        var button = go.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        if (onClick != null)
            button.onClick.AddListener(onClick);

        go.GetComponentInChildren<TextMeshProUGUI>().text = text;
        return button;
    }

    public static Toggle CreateToggle(Transform parent, string label)
    {
        var go = Spawn(_togglePrefab, parent, "Toggle");
        FillWidth(go);

        // The inner toggle has a fixed width; stretch it over the wrapper.
        var toggle = go.GetComponentInChildren<Toggle>();
        Stretch((RectTransform)toggle.transform);
        toggle.onValueChanged.RemoveAllListeners();

        var text = toggle.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = label;
        return toggle;
    }

    public static TMP_InputField CreateInputField(Transform parent)
    {
        var go = Spawn(_inputFieldPrefab, parent, "InputField");
        FillWidth(go, 30f);

        var input = go.GetComponent<TMP_InputField>();
        input.onEndEdit.RemoveAllListeners();
        input.onSubmit.RemoveAllListeners();
        input.onValueChanged.RemoveAllListeners();
        input.textComponent.alignment = TextAlignmentOptions.Left;
        input.text = "";
        if (input.placeholder is TMP_Text placeholder)
            placeholder.text = "";
        return input;
    }

    public static LayoutElement CreateSpacer(Transform parent, float height = 8f)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        return layout;
    }

    public static HorizontalLayoutGroup CreateHorizontalLayout(
        Transform parent,
        float spacing = 8f,
        bool controlWidth = false
    )
    {
        var go = new GameObject("HorizontalLayout", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = controlWidth;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        return hlg;
    }

    public static VerticalLayoutGroup CreateVerticalLayout(Transform parent, float spacing = 4f)
    {
        var go = new GameObject("VerticalLayout", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        return vlg;
    }

    /// <summary>
    /// A vertically scrolling view with the stock scrollbar. Children go under the
    /// returned content transform, which grows to fit them.
    /// </summary>
    public static ScrollRect CreateScrollView(Transform parent, out Transform content)
    {
        var root = new GameObject("ScrollView", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rootLayout = root.AddComponent<LayoutElement>();
        rootLayout.flexibleWidth = 1f;
        rootLayout.flexibleHeight = 1f;

        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(root.transform, false);
        var viewportRect = (RectTransform)viewport.transform;
        Stretch(viewportRect);
        // A stencil mask: RectMask2D doesn't clip the TMP text inside cloned input fields.
        viewport.AddComponent<Image>().color = Color.white;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var contentVlg = CreateVerticalLayout(viewport.transform, 2f);
        contentVlg.name = "Content";
        var contentRect = (RectTransform)contentVlg.transform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;
        contentVlg.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter
            .FitMode
            .PreferredSize;

        var scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 15f;

        var scrollbar = Spawn(_scrollbarPrefab, root.transform, "Scrollbar")
            .GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar;
        // AutoHideAndExpandViewport under-reserves for the cloned scrollbar, so keep a fixed
        // gutter instead and let content stop short of the bar.
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        viewportRect.offsetMax = new Vector2(-(((RectTransform)scrollbar.transform).sizeDelta.x + 4f), 0f);

        content = contentRect;
        return scroll;
    }
}
