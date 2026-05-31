using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingPanelUI : MonoBehaviour
{
    public static BuildingPanelUI Instance;
    [Header("Optional Scene References")]
    public GameObject panelRoot;
    public TMP_Text titleText;
    public TMP_Text statusText;
    public TMP_Text costText;
    public Button upgradeButton;
    public Button deployButton;
    public Button recallButton;
    public Button produceWoodButton;
    public Button produceConcreteButton;
    public Button produceCementButton;
    public Button collectProductionButton;
    public Button constructButton;
    public Button closeButton;

    private BaseBuilding selectedBuilding;
    private BuildingSlot selectedSlot;
    private BuildingSelector selector;
    private PlayerProfileUI playerProfileUI;
    private BuildingSelector subscribedSelector;
    private bool initialized;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        Instance = this;
        EnsurePanel();
        playerProfileUI = FindAnyObjectByType<PlayerProfileUI>();

        selector =
            BuildingSelector.Instance != null
                ? BuildingSelector.Instance
                : FindAnyObjectByType<BuildingSelector>();

        if (selector == null)
        {
            GameObject selectorObject = new GameObject("BuildingSelector");
            selector = selectorObject.AddComponent<BuildingSelector>();
        }

        SubscribeSelector(selector);

        HookButton(upgradeButton, OnUpgradePressed);
        HookButton(deployButton, OnDeployPressed);
        HookButton(recallButton, OnRecallPressed);
        HookButton(produceWoodButton, OnProduceWoodPressed);
        HookButton(produceConcreteButton, OnProduceConcretePressed);
        HookButton(produceCementButton, OnProduceCementPressed);
        HookButton(collectProductionButton, OnCollectProductionPressed);
        HookButton(constructButton, OnConstructPressed);
        HookButton(closeButton, ClosePanel);

        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        UnsubscribeSelector();
    }

    void Update()
    {
        if (selector == null || selector != BuildingSelector.Instance)
        {
            selector = BuildingSelector.Instance != null
                ? BuildingSelector.Instance
                : FindAnyObjectByType<BuildingSelector>();

            if (selector != null)
                SubscribeSelector(selector);
        }

        if (selectedSlot == null && selector != null && selector.selectedBuilding != selectedBuilding)
            OnSelectionChanged(selector.selectedBuilding);

        if (selectedSlot == null && selectedBuilding != null)
            Refresh();
        else if (selectedSlot != null)
            RefreshSlot();
    }

    void OnSelectionChanged(BaseBuilding building)
    {
        if (selectedSlot != null && building == null)
            return;

        selectedSlot = null;
        selectedBuilding = building;

        if (selectedBuilding == null)
        {
            Hide();
            return;
        }

        Show();
        Refresh();
    }

    public void ForceSelectBuilding(BaseBuilding building)
    {
        Initialize();

        if (panelRoot == null)
            EnsurePanel();

        OnSelectionChanged(building);

        if (panelRoot != null)
        {
            panelRoot.SetActive(building != null);
            panelRoot.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
        }
    }

    public void ForceSelectSlot(BuildingSlot slot)
    {
        Initialize();

        if (panelRoot == null)
            EnsurePanel();

        selectedSlot = slot;
        selectedBuilding = null;

        if (selector != null)
            selector.ClearSelection();

        if (selectedSlot == null)
        {
            Hide();
            return;
        }

        Show();
        RefreshSlot();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
        }
    }

    private void SubscribeSelector(BuildingSelector newSelector)
    {
        if (newSelector == null)
            return;

        if (subscribedSelector == newSelector)
            return;

        UnsubscribeSelector();
        subscribedSelector = newSelector;
        subscribedSelector.SelectionChanged += OnSelectionChanged;
    }

    private void UnsubscribeSelector()
    {
        if (subscribedSelector == null)
            return;

        subscribedSelector.SelectionChanged -= OnSelectionChanged;
        subscribedSelector = null;
    }

    void OnUpgradePressed()
    {
        if (selectedBuilding == null || BaseManager.Instance == null)
            return;

        BaseManager.Instance.TryUpgrade(selectedBuilding);
        Refresh();
    }

    void OnDeployPressed()
    {
        BarracksBuilding barracks = GetSelectedBarracks();
        if (barracks == null)
            return;

        barracks.DeployFirstUnitToWorld();
        ClosePanel();
        Refresh();
    }

    void OnRecallPressed()
    {
        BarracksBuilding barracks = GetSelectedBarracks();
        if (barracks == null)
            return;

        barracks.RecallFirstUnitToBase();
        Refresh();
    }

    void OnProduceWoodPressed()
    {
        StartProduction(ConstructionMaterialType.Wood);
    }

    void OnProduceConcretePressed()
    {
        StartProduction(ConstructionMaterialType.Concrete);
    }

    void OnProduceCementPressed()
    {
        StartProduction(ConstructionMaterialType.Cement);
    }

    void OnCollectProductionPressed()
    {
        if (ProductionFacilityManager.Instance != null)
            ProductionFacilityManager.Instance.CollectReadyProducts();

        Refresh();
    }

    void OnConstructPressed()
    {
        if (selectedSlot == null || BaseConstructionManager.Instance == null)
            return;

        BuildingSlot slot = selectedSlot;
        bool constructed = BaseConstructionManager.Instance.TryConstructAtSlot(slot);
        slot.RefreshVisual();

        if (constructed && slot.placedBuilding != null)
        {
            ForceSelectBuilding(slot.placedBuilding);
            return;
        }

        RefreshSlot();
    }

    void StartProduction(ConstructionMaterialType type)
    {
        if (!IsProductionBuilding() || ProductionFacilityManager.Instance == null)
            return;

        ProductionFacilityManager.Instance.StartProduction(type);
        Refresh();
    }

    void Refresh()
    {
        if (selectedBuilding == null)
            return;

        selectedSlot = null;
        SetButtonVisible(constructButton, false, false);
        RefreshBarracksButtons();
        RefreshProductionButtons();

        if (titleText != null)
            titleText.text = selectedBuilding.GetDisplayName();

        BuildingLevelData next = selectedBuilding.NextLevelData;

        if (selectedBuilding.isUpgrading)
        {
            SetUpgradeButtonVisible(true);
            SetStatus("Yukseltiliyor", BuildDetailText($"Kalan sure: {Mathf.CeilToInt(selectedBuilding.upgradeRemainingSeconds)} sn"), false);
            return;
        }

        if (next == null)
        {
            SetUpgradeButtonVisible(false);
            SetStatus("Maksimum seviye", BuildDetailText("Bu bina son seviyede."), false);
            return;
        }

        SetUpgradeButtonVisible(true);

        int headquartersLevel =
            BaseManager.Instance != null
                ? BaseManager.Instance.HeadquartersLevel
                : 1;

        ResourceCost wallet =
            BaseManager.Instance != null
                ? BaseManager.Instance.wallet
                : new ResourceCost();

        bool canUpgrade = selectedBuilding.CanStartUpgrade(headquartersLevel, wallet);

        string status = $"Sonraki seviye: Lv.{next.level}  Guc: +{next.powerReward}";
        if (headquartersLevel < next.requiredHeadquartersLevel)
            status = $"Komuta Merkezi Lv.{next.requiredHeadquartersLevel} gerekli";

        SetStatus(canUpgrade ? "Yukseltmeye hazir" : "Yukseltme yapilamaz", BuildDetailText(status), canUpgrade);
    }

    void RefreshSlot()
    {
        if (selectedSlot == null)
            return;

        HideBuildingActionButtons();

        BaseConstructionManager construction = BaseConstructionManager.Instance;
        ConstructionDefinition definition =
            construction != null ? construction.GetDefinition(selectedSlot.allowedType) : null;

        if (titleText != null)
            titleText.text = selectedSlot.displayName;

        int headquartersLevel =
            BaseManager.Instance != null
                ? BaseManager.Instance.HeadquartersLevel
                : 1;

        string reason = "";
        bool canBuild =
            construction != null &&
            construction.CanConstructAtSlot(definition, selectedSlot, out reason);

        string buildingName = definition != null ? definition.displayName : selectedSlot.allowedType.ToString();
        string prefabStatus = definition != null && definition.prefab != null ? "Hazir" : "Model/prefab bekleniyor";
        string lockStatus = selectedSlot.CanUse(headquartersLevel)
            ? "Parsel acik"
            : $"HQ Lv.{selectedSlot.requiredHeadquartersLevel} gerekli";

        string detail =
            $"Bina: {buildingName}\n" +
            $"Parsel durumu: {lockStatus}\n" +
            $"Model: {prefabStatus}\n" +
            (definition != null && !string.IsNullOrWhiteSpace(definition.modelFolder)
                ? $"Model klasoru: {definition.modelFolder}\n"
                : "");

        if (!canBuild && !string.IsNullOrWhiteSpace(reason))
            detail += "Engel: " + reason;

        SetStatus(canBuild ? "Insaata hazir" : "Insa edilemez", detail, false);

        if (costText != null)
            costText.text = definition != null ? "Maliyet  " + definition.cost.ToDisplayString() : "";

        SetButtonVisible(constructButton, selectedSlot.IsEmpty, canBuild);
    }

    string BuildDetailText(string baseDetail)
    {
        string detail = baseDetail;

        BarracksBuilding barracks = GetSelectedBarracks();
        if (barracks != null)
            detail += "\n" + barracks.GetStatusText();

        if (IsProductionBuilding() && ProductionFacilityManager.Instance != null)
            detail += "\n" + ProductionFacilityManager.Instance.GetStatusText();

        return detail;
    }

    void SetStatus(string title, string detail, bool canUpgrade)
    {
        if (statusText != null)
            statusText.text = title + "\n" + detail;

        if (upgradeButton != null)
            upgradeButton.interactable = canUpgrade;

        if (costText != null && selectedBuilding != null)
        {
            BuildingLevelData next = selectedBuilding.NextLevelData;
            costText.text = next != null ? "Maliyet  " + next.upgradeCost.ToDisplayString() : "";
        }
    }

    void Show()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }

        if (playerProfileUI == null)
            playerProfileUI = FindAnyObjectByType<PlayerProfileUI>();

        if (playerProfileUI != null)
            playerProfileUI.SetVisible(false);
    }

    void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (playerProfileUI == null)
            playerProfileUI = FindAnyObjectByType<PlayerProfileUI>();

        if (playerProfileUI != null)
            playerProfileUI.SetVisible(true);
    }

    void ClosePanel()
    {
        selectedSlot = null;
        selectedBuilding = null;

        if (selector != null)
            selector.ClearSelection();

        if (BuildingSlotSelector.Instance != null)
            BuildingSlotSelector.Instance.ClearSelection();

        Hide();
    }

    void HideBuildingActionButtons()
    {
        SetUpgradeButtonVisible(false);
        SetButtonVisible(deployButton, false, false);
        SetButtonVisible(recallButton, false, false);
        SetButtonVisible(produceWoodButton, false, false);
        SetButtonVisible(produceConcreteButton, false, false);
        SetButtonVisible(produceCementButton, false, false);
        SetButtonVisible(collectProductionButton, false, false);
    }

    void SetUpgradeButtonVisible(bool visible)
    {
        if (upgradeButton != null)
            upgradeButton.gameObject.SetActive(visible);
    }

    void RefreshBarracksButtons()
    {
        BarracksBuilding barracks = GetSelectedBarracks();
        bool isBarracks = barracks != null;

        if (deployButton != null)
        {
            deployButton.gameObject.SetActive(isBarracks);
            deployButton.interactable = isBarracks && barracks.UnitsInBaseCount > 0;
        }

        if (recallButton != null)
        {
            recallButton.gameObject.SetActive(isBarracks);
            recallButton.interactable = isBarracks && barracks.UnitsOnMapCount > 0;
        }
    }

    void RefreshProductionButtons()
    {
        bool isProduction = IsProductionBuilding();
        ProductionFacilityManager production = ProductionFacilityManager.Instance;
        bool canStartProduction =
            production != null && production.HasFreeSlot();

        SetButtonVisible(produceWoodButton, isProduction, canStartProduction);
        SetButtonVisible(produceConcreteButton, isProduction, canStartProduction);
        SetButtonVisible(produceCementButton, isProduction, canStartProduction);
        SetButtonVisible(collectProductionButton, isProduction, production != null && production.HasReadyProducts());
    }

    void SetButtonVisible(Button button, bool visible, bool interactable)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
        button.interactable = interactable;
    }

    bool IsProductionBuilding()
    {
        if (selectedBuilding == null)
            return false;

        if (selectedBuilding.data != null)
        {
            if (selectedBuilding.data.type == BuildingType.ProductionFacility ||
                selectedBuilding.data.type == BuildingType.SteelFactory)
                return true;

            string displayName = selectedBuilding.data.displayName.ToLowerInvariant();
            if (displayName.Contains("uretim") || displayName.Contains("üretim") || displayName.Contains("atolye") || displayName.Contains("fabrika"))
                return true;
        }

        string objectName = selectedBuilding.name.ToLowerInvariant();
        return selectedBuilding.GetComponent<ProductionFacilityBuilding>() != null ||
               objectName.Contains("uretim") ||
               objectName.Contains("üretim") ||
               objectName.Contains("atolye") ||
               objectName.Contains("fabrika");
    }

    BarracksBuilding GetSelectedBarracks()
    {
        if (selectedBuilding == null)
            return null;

        return selectedBuilding.GetComponent<BarracksBuilding>();
    }

    void HookButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    void EnsurePanel()
    {
        if (panelRoot != null)
            return;

        Canvas canvas = FindScreenCanvas();

        if (canvas == null)
            return;

        panelRoot = new GameObject("BuildingPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 18f);
        panelRect.sizeDelta = new Vector2(468f, 246f);

        Image panelImage = panelRoot.GetComponent<Image>();
        panelImage.color = new Color(0.07f, 0.08f, 0.09f, 0.92f);

        titleText = CreateText("Title", 20, new Vector2(18f, -16f), new Vector2(282f, 30f));
        statusText = CreateText("Status", 13, new Vector2(18f, -52f), new Vector2(288f, 124f));
        costText = CreateText("Cost", 12, new Vector2(18f, -184f), new Vector2(288f, 28f));
        upgradeButton = CreateButton("UpgradeButton", "Yukselt", new Vector2(326f, -30f), new Vector2(110f, 28f));
        deployButton = CreateButton("DeployButton", "Haritaya Cikar", new Vector2(326f, -64f), new Vector2(110f, 28f));
        recallButton = CreateButton("RecallButton", "Geri Cagir", new Vector2(326f, -98f), new Vector2(110f, 28f));
        produceWoodButton = CreateButton("ProduceWoodButton", "Kalas", new Vector2(326f, -64f), new Vector2(110f, 28f));
        produceConcreteButton = CreateButton("ProduceConcreteButton", "Beton", new Vector2(326f, -98f), new Vector2(110f, 28f));
        produceCementButton = CreateButton("ProduceCementButton", "Cimento", new Vector2(326f, -132f), new Vector2(110f, 28f));
        collectProductionButton = CreateButton("CollectProductionButton", "Topla", new Vector2(326f, -166f), new Vector2(110f, 28f));
        constructButton = CreateButton("ConstructButton", "Insa Et", new Vector2(326f, -30f), new Vector2(110f, 28f));
        closeButton = CreateButton("CloseButton", "Kapat", new Vector2(326f, -200f), new Vector2(110f, 28f));
    }

    TMP_Text CreateText(string objectName, int fontSize, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelRoot.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.text = "";
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;

        return text;
    }

    private Canvas FindScreenCanvas()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
            return parentCanvas;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);

        foreach (Canvas foundCanvas in canvases)
        {
            if (foundCanvas != null && foundCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return foundCanvas;
        }

        foreach (Canvas foundCanvas in canvases)
        {
            if (foundCanvas != null && foundCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                return foundCanvas;
        }

        return parentCanvas;
    }

    Button CreateButton(string objectName, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panelRoot.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.46f, 0.55f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.12f, 0.46f, 0.55f, 1f);
        colors.highlightedColor = new Color(0.16f, 0.56f, 0.66f, 1f);
        colors.pressedColor = new Color(0.08f, 0.34f, 0.42f, 1f);
        colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.65f);
        button.colors = colors;

        TMP_Text buttonText = CreateButtonText(buttonObject.transform, label);
        buttonText.color = Color.white;

        return button;
    }

    TMP_Text CreateButtonText(Transform parent, string label)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = label;
        text.fontSize = 13;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;

        return text;
    }
}

