using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceTopBarUI : MonoBehaviour
{
    public GameObject root;

    private TMP_Text goldText;
    private TMP_Text turanText;
    private TMP_Text steelText;
    private TMP_Text oilText;
    private TMP_Text borText;
    private BaseManager baseManager;

    public void SetVisible(bool visible)
    {
        if (root != null)
            root.SetActive(visible);
    }

    void Start()
    {
        EnsureUI();
        BindManager();
        Refresh();
    }

    void OnDestroy()
    {
        if (baseManager != null)
            baseManager.ResourcesChanged -= OnResourcesChanged;
    }

    void Update()
    {
        if (baseManager == null)
            BindManager();
    }

    private void BindManager()
    {
        BaseManager manager =
            BaseManager.Instance != null
                ? BaseManager.Instance
                : FindAnyObjectByType<BaseManager>();

        if (manager == null || manager == baseManager)
            return;

        if (baseManager != null)
            baseManager.ResourcesChanged -= OnResourcesChanged;

        baseManager = manager;
        baseManager.ResourcesChanged += OnResourcesChanged;
        Refresh();
    }

    private void OnResourcesChanged(ResourceCost resources)
    {
        Refresh(resources);
    }

    public void Refresh()
    {
        ResourceCost resources =
            baseManager != null
                ? baseManager.wallet
                : new ResourceCost();

        Refresh(resources);
    }

    private void Refresh(ResourceCost resources)
    {
        if (goldText != null) goldText.text = "Altin " + Format(resources.gold);
        if (turanText != null) turanText.text = "Turan " + Format(resources.turanCoin);
        if (steelText != null) steelText.text = "Celik " + Format(resources.steel);
        if (oilText != null) oilText.text = "Petrol " + Format(resources.oil);
        if (borText != null) borText.text = "Bor " + Format(resources.bor);
    }

    private void EnsureUI()
    {
        if (root != null)
            return;

        Canvas canvas =
            GetComponentInParent<Canvas>();

        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
            canvas = FindScreenCanvas();

        if (canvas == null)
            return;

        root =
            new GameObject("ResourceTopBar", typeof(RectTransform), typeof(Image));

        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect =
            root.GetComponent<RectTransform>();

        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.anchoredPosition = new Vector2(-14f, -8f);
        rootRect.sizeDelta = new Vector2(560f, 42f);

        Image rootImage =
            root.GetComponent<Image>();

        rootImage.color = new Color(0.05f, 0.06f, 0.07f, 0.82f);

        HorizontalLayoutGroup layout =
            root.AddComponent<HorizontalLayoutGroup>();

        layout.padding = new RectOffset(8, 8, 5, 5);
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        goldText = CreateResourceItem("Gold", "Altin", new Color(0.95f, 0.72f, 0.22f));
        turanText = CreateResourceItem("Turan", "Turan", new Color(0.44f, 0.80f, 1f));
        steelText = CreateResourceItem("Steel", "Celik", new Color(0.72f, 0.78f, 0.82f));
        oilText = CreateResourceItem("Oil", "Petrol", new Color(0.20f, 0.36f, 0.30f));
        borText = CreateResourceItem("Bor", "Bor", new Color(0.46f, 0.88f, 0.67f));
    }

    private TMP_Text CreateResourceItem(string objectName, string label, Color swatchColor)
    {
        GameObject item =
            new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));

        item.transform.SetParent(root.transform, false);

        RectTransform itemRect =
            item.GetComponent<RectTransform>();

        itemRect.sizeDelta = new Vector2(104f, 30f);

        LayoutElement itemLayout =
            item.GetComponent<LayoutElement>();

        itemLayout.minWidth = 96f;
        itemLayout.preferredWidth = 104f;
        itemLayout.flexibleWidth = 0f;
        itemLayout.preferredHeight = 30f;

        Image itemImage =
            item.GetComponent<Image>();

        itemImage.color = new Color(0.13f, 0.15f, 0.16f, 0.9f);

        GameObject swatch =
            new GameObject("Swatch", typeof(RectTransform), typeof(Image));

        swatch.transform.SetParent(item.transform, false);

        RectTransform swatchRect =
            swatch.GetComponent<RectTransform>();

        swatchRect.anchorMin = new Vector2(0f, 0.5f);
        swatchRect.anchorMax = new Vector2(0f, 0.5f);
        swatchRect.pivot = new Vector2(0f, 0.5f);
        swatchRect.anchoredPosition = new Vector2(8f, 0f);
        swatchRect.sizeDelta = new Vector2(8f, 18f);

        swatch.GetComponent<Image>().color = swatchColor;

        TMP_Text valueText =
            CreateText(item.transform, "Value", label + " 0", 12.5f, new Vector2(22f, 0f), new Vector2(78f, 22f));

        valueText.color = Color.white;
        valueText.alignment = TextAlignmentOptions.MidlineLeft;
        valueText.enableAutoSizing = true;
        valueText.fontSizeMin = 9f;
        valueText.fontSizeMax = 12.5f;

        return valueText;
    }

    private TMP_Text CreateText(
        Transform parent,
        string objectName,
        string text,
        float fontSize,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject textObject =
            new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));

        textObject.transform.SetParent(parent, false);

        RectTransform rect =
            textObject.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text tmp =
            textObject.GetComponent<TMP_Text>();

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    private Canvas FindScreenCanvas()
    {
        Canvas[] canvases =
            FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);

        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas;
        }

        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                return canvas;
        }

        return null;
    }

    private string Format(int value)
    {
        if (value >= 1000000)
            return (value / 1000000f).ToString("0.#") + "M";

        if (value >= 1000)
            return (value / 1000f).ToString("0.#") + "K";

        return value.ToString();
    }
}

