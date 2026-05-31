using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class GameUI : MonoBehaviour
{
    public GameObject moveButton;
    public GameObject logisticsButton;
    public GameObject enterBaseButton;
    public GameObject backToMapButton;
    public GameObject recallUnitsButton;
    public GameObject homeBaseButton;

    private HexSelector selector;
    private WorldBaseSelector worldBaseSelector;
    private GameModeManager gameMode;
    private Camera cam;

    void Start()
    {
        selector = FindAnyObjectByType<HexSelector>();
        worldBaseSelector = FindAnyObjectByType<WorldBaseSelector>();
        gameMode = FindAnyObjectByType<GameModeManager>();
        cam = Camera.main;
        EnsureEventSystem();

        if (worldBaseSelector == null)
        {
            GameObject selectorObject = new GameObject("WorldBaseSelector");
            worldBaseSelector = selectorObject.AddComponent<WorldBaseSelector>();
        }

        if (GetComponent<BuildingPanelUI>() == null)
            gameObject.AddComponent<BuildingPanelUI>();

        if (FindAnyObjectByType<BuildingSlotSelector>() == null)
            new GameObject("BuildingSlotSelector").AddComponent<BuildingSlotSelector>();

        if (GetComponent<ResourceTopBarUI>() == null)
            gameObject.AddComponent<ResourceTopBarUI>();

        if (GetComponent<MainMenuPanelUI>() == null)
            gameObject.AddComponent<MainMenuPanelUI>();

        if (GetComponent<PlayerProfileUI>() == null)
            gameObject.AddComponent<PlayerProfileUI>();

        if (GetComponent<WorldCityPanelUI>() == null)
            gameObject.AddComponent<WorldCityPanelUI>();

        if (FindAnyObjectByType<TuranMapLayerManager>() == null)
            TuranMapLayerManager.EnsureInstance();

        if (FindAnyObjectByType<WarpathZoomLayerController>() == null)
            WarpathZoomLayerController.EnsureInstance();

        if (FindAnyObjectByType<WorldMapStrategicMarkerManager>() == null)
            new GameObject("WorldMapStrategicMarkerManager").AddComponent<WorldMapStrategicMarkerManager>();

        if (FindAnyObjectByType<PlayerRosterManager>() == null)
            new GameObject("PlayerRosterManager").AddComponent<PlayerRosterManager>();

        if (FindAnyObjectByType<MaterialInventoryManager>() == null)
            new GameObject("MaterialInventoryManager").AddComponent<MaterialInventoryManager>();

        if (FindAnyObjectByType<ProductionFacilityManager>() == null)
            new GameObject("ProductionFacilityManager").AddComponent<ProductionFacilityManager>();

        if (FindAnyObjectByType<WorldResourceNodeManager>() == null)
        {
            GameObject nodeManagerObject = new GameObject("WorldResourceNodeManager");

            if (gameMode != null && gameMode.worldRoot != null)
                nodeManagerObject.transform.SetParent(gameMode.worldRoot.transform, false);

            nodeManagerObject.AddComponent<WorldResourceNodeManager>();
        }

        if (moveButton != null)
        {
            Button button = moveButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(OnMoveButtonPressed);
            }
            ConfigureButtonVisual(moveButton, "Git", new Vector2(0.5f, 0f), new Vector2(0f, 132f), new Vector2(86f, 28f));
            moveButton.SetActive(false);
        }

        logisticsButton = EnsureButton(
            logisticsButton,
            "LogisticsButton",
            "Topla",
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(86f, 28f),
            OnLogisticsButtonPressed
        );

        if (logisticsButton != null) logisticsButton.SetActive(false);

        SetupModeButtons();
        HookModeEvents();
    }

    void Update()
    {
        RefreshModeButtons();

        bool isWorldMode = gameMode == null || gameMode.CurrentMode == GameViewMode.WorldMap;

        if (!isWorldMode)
        {
            if (moveButton != null) moveButton.SetActive(false);
            if (logisticsButton != null) logisticsButton.SetActive(false);
            return;
        }

        if (selector == null) return;

        bool hasSelectedUnit =
            UnitManager.Instance != null &&
            UnitManager.Instance.GetSelectedUnits().Count > 0;

        if (hasSelectedUnit && selector.selectedHex != null)
        {
            if (moveButton != null)
            {
                moveButton.SetActive(true);
                UpdateElementPosition(moveButton.transform, 70f);
            }
        }
        else
        {
            if (moveButton != null) moveButton.SetActive(false);
        }

        // Logistics Button logic
        WorldResourceNode selectedResourceNode = selector.selectedHex != null &&
                            WorldResourceNodeManager.Instance != null
                            ? WorldResourceNodeManager.Instance.GetNodeAtHex(selector.selectedHex)
                            : null;

        bool isResourceHex = selectedResourceNode != null && !selectedResourceNode.hasGuard;
        
        if (isResourceHex && isWorldMode)
{
            if (logisticsButton != null)
            {
                logisticsButton.SetActive(true);
                UpdateElementPosition(logisticsButton.transform, -70f);
            }
        }
        else
        {
            if (logisticsButton != null) logisticsButton.SetActive(false);
        }
    }

    void UpdateElementPosition(Transform targetTransform, float xOffset)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || selector.selectedHex == null) return;

        Vector3 worldPos = selector.selectedHex.transform.position + Vector3.up * 1.2f;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        screenPos += new Vector3(xOffset, 0f, 0f);

        targetTransform.position = screenPos;
    }

    public void OnLogisticsButtonPressed()
    {
        if (selector == null || selector.selectedHex == null) return;

        UnitController logisticsUnit = FindAvailableLogisticsUnit();
        if (logisticsUnit != null)
        {
            SendUnitToHex(logisticsUnit, selector.selectedHex);
        }
        else
        {
            Debug.Log("Bosta lojistik birimi bulunamadi.");
        }

        if (logisticsButton != null) logisticsButton.SetActive(false);
    }

    private UnitController FindAvailableLogisticsUnit()
    {
        if (UnitManager.Instance == null)
        {
            Debug.Log("UnitManager.Instance bulunamadi.");
            return null;
        }
        foreach (var unit in UnitManager.Instance.units)
        {
            if (unit == null) continue;
            
            bool isLogistics = unit.unitData != null && unit.unitData.kind == TuranUnitKind.Logistics;
            bool isIdle = unit.state == UnitState.Idle;

            if (isLogistics && isIdle)
            {
                return unit;
            }
        }
        return null;
    }

    private void SendUnitToHex(UnitController unit, HexCell targetHex)
    {
        if (unit == null || targetHex == null) return;

        if (unit.deploymentState == UnitDeploymentState.InBase || unit.deploymentState == UnitDeploymentState.Reserve)
        {
            GameModeManager modeManager = GameModeManager.Instance;
            if (modeManager != null)
                modeManager.RequestEnterWorldMap(true);

            Transform worldParent = modeManager != null ? modeManager.worldUnitsRoot?.transform : null;
            if (worldParent != null)
                unit.transform.SetParent(worldParent);

            WorldBaseMarker marker = WorldBaseMarker.FindPrimary();
            Vector3 spawnPosition = marker != null ? marker.transform.position + new Vector3(2f, 0f, 2f) : targetHex.transform.position;
            unit.PlaceOnWorld(spawnPosition);
        }

        Pathfinding pathfinder = Pathfinding.Instance != null ? Pathfinding.Instance : FindAnyObjectByType<Pathfinding>();
        if (pathfinder != null && unit.currentHex != null)
        {
            List<HexCell> path = pathfinder.FindPath(unit.currentHex, targetHex);
            if (path != null && path.Count > 0)
            {
                unit.MovePath(path);

            }
        }
    }

    public void OnMoveButtonPressed()
    {
        if (selector == null) return;
        selector.MoveSelectedUnit();
        if (moveButton != null) moveButton.SetActive(false);
    }

    public void OnEnterBasePressed()
    {
        if (gameMode == null)
            gameMode = FindAnyObjectByType<GameModeManager>();

        WorldBaseMarker marker = WorldBaseMarker.FindPrimary(true);
        if (worldBaseSelector != null && marker != null)
            worldBaseSelector.SelectMarker(marker);

        if (gameMode != null)
        {
            // Doğrudan geçiş yaparak karmaşık manager takılmalarını önle
            gameMode.SetMode(GameViewMode.BaseView, true);
        }

        if (enterBaseButton != null)
            enterBaseButton.SetActive(false);
    }

    public void OnBackToMapPressed()
    {
        if (gameMode != null) gameMode.RequestEnterWorldMap(true);
    }

    public void OnRecallUnitsPressed()
    {
        if (gameMode != null)
            gameMode.RequestEnterWorldMap(true);

        if (UnitManager.Instance != null) UnitManager.Instance.RecallAllWorldUnitsToBase();
        if (worldBaseSelector != null) worldBaseSelector.ClearSelection();
    }

    public void OnHomeBasePressed()
    {
        OnEnterBasePressed();
    }

    private void SetupModeButtons()
    {
        enterBaseButton = EnsureButton(
            enterBaseButton,
            "EnterBaseButton",
            "Usse Gir",
            new Vector2(0.5f, 0f),
            new Vector2(0f, 138f),
            new Vector2(92f, 28f),
            OnEnterBasePressed
        );

        backToMapButton = EnsureButton(
            backToMapButton,
            "BackToMapButton",
            "Harita",
            new Vector2(0f, 1f),
            new Vector2(64f, -32f),
            new Vector2(92f, 30f),
            OnBackToMapPressed
        );

        recallUnitsButton = EnsureButton(
            recallUnitsButton,
            "RecallUnitsButton",
            "Birlikleri Cagir",
            new Vector2(0.5f, 0f),
            new Vector2(0f, 104f),
            new Vector2(128f, 28f),
            OnRecallUnitsPressed
        );

        homeBaseButton = EnsureButton(
            homeBaseButton,
            "HomeBaseButton",
            "Us",
            new Vector2(0f, 0.5f),
            new Vector2(16f, -74f),
            new Vector2(58f, 34f),
            OnHomeBasePressed
        );

        if (enterBaseButton != null) enterBaseButton.SetActive(false);
        if (backToMapButton != null) backToMapButton.SetActive(false);
        if (recallUnitsButton != null) recallUnitsButton.SetActive(false);
        if (homeBaseButton != null) homeBaseButton.SetActive(false);
    }

    private void HookModeEvents()
    {
        if (worldBaseSelector != null) worldBaseSelector.SelectionChanged += OnWorldBaseSelectionChanged;
        if (gameMode != null) gameMode.ModeChanged += OnModeChanged;
    }

    private void OnDestroy()
    {
        if (worldBaseSelector != null) worldBaseSelector.SelectionChanged -= OnWorldBaseSelectionChanged;
        if (gameMode != null) gameMode.ModeChanged -= OnModeChanged;
    }

    private void OnWorldBaseSelectionChanged(WorldBaseMarker marker) => RefreshModeButtons();
    private void OnModeChanged(GameViewMode mode) => RefreshModeButtons();

    private void RefreshModeButtons()
    {
        if (worldBaseSelector == null) worldBaseSelector = FindAnyObjectByType<WorldBaseSelector>();
        if (gameMode == null) gameMode = FindAnyObjectByType<GameModeManager>();

        bool isWorldMode = gameMode == null || gameMode.CurrentMode == GameViewMode.WorldMap;
        bool isBaseMode = gameMode != null && gameMode.CurrentMode == GameViewMode.BaseView;
        bool hasSelectedWorldBase = worldBaseSelector != null && worldBaseSelector.selectedMarker != null;

        if (enterBaseButton != null) enterBaseButton.SetActive(false);
        if (backToMapButton != null) backToMapButton.SetActive(isBaseMode);
        if (homeBaseButton != null) homeBaseButton.SetActive(isWorldMode);

        bool hasWorldUnits = UnitManager.Instance != null && UnitManager.Instance.HasWorldMapUnits();
        if (recallUnitsButton != null) recallUnitsButton.SetActive(isWorldMode && hasSelectedWorldBase && hasWorldUnits);
    }

    private GameObject EnsureButton(GameObject existing, string objectName, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        if (existing != null)
        {
            Button eb = existing.GetComponent<Button>();
            if (eb != null) { eb.onClick = new Button.ButtonClickedEvent(); eb.onClick.AddListener(onClick); }
            ConfigureButtonVisual(existing, label, anchor, anchoredPosition, size);
            return existing;
        }

        Canvas canvas = FindScreenCanvas();
        if (canvas == null) return null;

        GameObject buttonObj = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(canvas.transform, false);
        ConfigureButtonVisual(buttonObj, label, anchor, anchoredPosition, size);
        Button b = buttonObj.GetComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(onClick);
        return buttonObj;
    }

    private void ConfigureButtonVisual(GameObject buttonObj, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = anchor; rect.anchorMax = anchor;
        rect.pivot = anchor.x <= 0.01f ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition; rect.sizeDelta = size;

        Image image = buttonObj.GetComponent<Image>();
        image.color = new Color(0.16f, 0.44f, 0.60f, 0.95f);

        TMPro.TextMeshProUGUI text = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text == null)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textObj.transform.SetParent(buttonObj.transform, false);
            text = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        }
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        text.text = label; text.alignment = TMPro.TextAlignmentOptions.Center;
        text.fontSize = 13f; text.color = Color.white;
    }

    private Canvas FindScreenCanvas()
    {
        Canvas pc = GetComponentInParent<Canvas>();
        if (pc != null && pc.renderMode != RenderMode.WorldSpace)
        {
            EnsureGraphicRaycaster(pc);
            return pc;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        foreach (Canvas c in canvases)
        {
            if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                EnsureGraphicRaycaster(c);
                return c;
            }
        }

        foreach (Canvas c in canvases)
        {
            if (c != null && c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                EnsureGraphicRaycaster(c);
                return c;
            }
        }

        if (pc != null)
            EnsureGraphicRaycaster(pc);

        return pc;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EventSystem.current = eventSystemObject.GetComponent<EventSystem>();
    }

    private void EnsureGraphicRaycaster(Canvas targetCanvas)
    {
        if (targetCanvas == null)
            return;

        if (targetCanvas.GetComponent<GraphicRaycaster>() == null)
            targetCanvas.gameObject.AddComponent<GraphicRaycaster>();
    }
}




