using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameViewMode
{
    WorldMap,
    BaseView
}

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance;

    [Header("Roots")]
    public GameObject worldRoot;
    public GameObject baseRoot;
    public GameObject worldUnitsRoot;
    public GameObject baseUnitsRoot;
    public bool autoManageWorldObjects = true;
    public bool seamlessWorldBaseMode = true;
    public bool placeBaseRootAtWorldMarker = true;
    public float baseRootWorldYOffset = 0.05f;

    [Header("Scene Transition")]
    public bool useWorldBaseTransitionManager = false;

    [Header("Camera Targets")]
    public Transform worldCameraAnchor;
    public Transform baseCameraAnchor;

    [Header("Camera")]
    public Camera mainCamera;
    public float cameraLerpSpeed = 8f;
    public float maxCameraTransitionSeconds = 1.2f;
    public bool autoFrameWorldGrid = true;
    public Vector3 worldCameraOffset = new Vector3(0f, 42f, -34f);
    public bool autoFrameBaseRoot = true;
    public bool autoNormalizeBaseLayout = true;
    public Vector3 baseCameraOffset = new Vector3(0f, 5.2f, -4.7f);
    public float baseCameraHeightPadding = 2.05f;
    public float minBaseCameraDistance = 3.1f;
    public float maxBaseCameraDistance = 8.5f;
    public float baseCameraFieldOfView = 44f;
    public float worldCameraFieldOfView = 52f;

    [Header("Base Layout")]
    public Vector3 headquartersBasePosition = new Vector3(0f, 0f, 1.35f);
    public Vector3 barracksBasePosition = new Vector3(-1.9f, 0f, -1.45f);
    public Vector3 productionBasePosition = new Vector3(1.9f, 0f, -1.45f);
    public float baseInteriorHexFillRatio = 1.28f;

    public GameViewMode CurrentMode { get; private set; } = GameViewMode.WorldMap;
    public bool IsCameraTransitionActive => cameraTransitionActive;
    public event Action<GameViewMode> ModeChanged;

    private Vector3 targetCamPos;
    private Quaternion targetCamRot;
    private bool hasCameraTarget;
    private bool cameraTransitionActive;
    private float cameraTransitionTimer;
    private bool preserveCurrentCameraOnNextWorldEnter;
    private bool preserveCurrentCameraOnNextBaseEnter;
    private readonly List<GameObject> autoWorldObjects = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnsureMobileOptimizer();

        if (mainCamera == null)
            mainCamera = Camera.main;

        ApplyProfessionalCameraTuning();
        DiscoverAutoWorldObjects();
        EnsureCameraAnchors();
        AlignBaseRootToWorldMarker();
        SetMode(GameViewMode.WorldMap, true);
    }

    private void EnsureMobileOptimizer()
    {
        MobileRuntimeOptimizer optimizer = FindAnyObjectByType<MobileRuntimeOptimizer>();
        if (optimizer != null)
        {
            optimizer.Apply();
            return;
        }

        GameObject optimizerObject = new GameObject("MobileRuntimeOptimizer");
        optimizer = optimizerObject.AddComponent<MobileRuntimeOptimizer>();
        optimizer.Apply();
    }

    void LateUpdate()
    {
        if (mainCamera == null || !hasCameraTarget || !cameraTransitionActive)
            return;

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetCamPos,
            cameraLerpSpeed * Time.deltaTime
        );

        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            targetCamRot,
            cameraLerpSpeed * Time.deltaTime
        );

        cameraTransitionTimer += Time.deltaTime;

        float posDist =
            Vector3.Distance(mainCamera.transform.position, targetCamPos);

        float rotDist =
            Quaternion.Angle(mainCamera.transform.rotation, targetCamRot);

        if (posDist < 0.05f ||
            rotDist < 0.5f ||
            cameraTransitionTimer >= maxCameraTransitionSeconds)
        {
            mainCamera.transform.position = targetCamPos;
            mainCamera.transform.rotation = targetCamRot;
            cameraTransitionActive = false;
            cameraTransitionTimer = 0f;
        }
    }

    public void EnterBaseView()
    {
        SetMode(GameViewMode.BaseView, false);
    }

    public void RequestEnterBaseView(bool preserveCameraFrame = false)
    {
        if (useWorldBaseTransitionManager)
        {
            WorldBaseTransitionManager transition = WorldBaseTransitionManager.EnsureInstance();
            if (transition != null)
            {
                transition.EnterBase(WorldBaseMarker.FindPrimary(true), preserveCameraFrame);
                return;
            }
        }

        EnterBaseView(preserveCameraFrame);
    }

    public void EnterBaseView(bool preserveCameraFrame)
    {
        if (preserveCameraFrame && CurrentMode == GameViewMode.BaseView)
            return;

        if (preserveCameraFrame)
            preserveCurrentCameraOnNextBaseEnter = true;

        SetMode(GameViewMode.BaseView, false);
    }

    public void EnterWorldMap()
    {
        SetMode(GameViewMode.WorldMap, false);
    }

    public void RequestEnterWorldMap(bool preserveCameraFrame = false)
    {
        if (useWorldBaseTransitionManager)
        {
            WorldBaseTransitionManager transition = WorldBaseTransitionManager.EnsureInstance();
            if (transition != null)
            {
                transition.ExitBase(preserveCameraFrame);
                return;
            }
        }

        EnterWorldMap(preserveCameraFrame);
    }

    public void FocusWorldCameraOnPlayerBase(bool closeView = true)
    {
        WorldBaseMarker marker = WorldBaseMarker.FindPrimary(true);
        if (marker == null || mainCamera == null)
            return;

        if (CurrentMode != GameViewMode.WorldMap)
            SetMode(GameViewMode.WorldMap, false);

        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        Vector3 target = marker.transform.position;
        Vector3 direction = worldCameraOffset.normalized;
        if (direction == Vector3.zero)
            direction = new Vector3(0f, 0.72f, -0.68f).normalized;

        float footprint = grid != null ? Mathf.Max(GetWorldHexFootprint(), grid.size) : 1.7f;
        float distance = closeView
            ? Mathf.Clamp(footprint * 11f, 15f, 22f)
            : Mathf.Clamp(footprint * 22f, 32f, 48f);

        Vector3 cameraPosition = target + direction * distance;
        
        // Kamera hedefini doğrudan ata
        targetCamPos = cameraPosition;
        targetCamRot = Quaternion.LookRotation(target - cameraPosition, Vector3.up);
        hasCameraTarget = true;
        cameraTransitionActive = true;
        cameraTransitionTimer = 0f;
    }

    public void EnterWorldMap(bool preserveCameraFrame)
    {
        if (preserveCameraFrame && CurrentMode == GameViewMode.WorldMap)
            return;

        SuppressCameraAutoBaseEntry();

        if (preserveCameraFrame)
            preserveCurrentCameraOnNextWorldEnter = true;

        SetMode(GameViewMode.WorldMap, false);
    }

    public void SetMode(GameViewMode mode, bool instantCamera)
    {
        CurrentMode = mode;

        bool worldOn = mode == GameViewMode.WorldMap;
        bool baseOn = mode == GameViewMode.BaseView;

        if (worldOn)
            SuppressCameraAutoBaseEntry();

        bool hasModeRoots = worldRoot != null || baseRoot != null;
        bool hasUnitRoots = worldUnitsRoot != null || baseUnitsRoot != null;

        if (hasModeRoots && !seamlessWorldBaseMode)
        {
            if (worldRoot != null) worldRoot.SetActive(worldOn);
            if (baseRoot != null) baseRoot.SetActive(baseOn);
        }
        else if (seamlessWorldBaseMode)
        {
            if (worldRoot != null) worldRoot.SetActive(true);
            if (baseRoot != null) baseRoot.SetActive(baseOn);
        }

        if (hasUnitRoots)
        {
            if (worldUnitsRoot != null) worldUnitsRoot.SetActive(worldOn);
            if (baseUnitsRoot != null) baseUnitsRoot.SetActive(baseOn);
        }

        if (autoManageWorldObjects && !seamlessWorldBaseMode)
            SetAutoWorldObjectsActive(worldOn);

        bool skipWorldFrame = worldOn && preserveCurrentCameraOnNextWorldEnter;
        bool skipBaseFrame = baseOn && preserveCurrentCameraOnNextBaseEnter;
        preserveCurrentCameraOnNextWorldEnter = false;
        preserveCurrentCameraOnNextBaseEnter = false;

        if (worldOn && autoFrameWorldGrid && !skipWorldFrame)
            ApplyWorldAnchorFromGrid();

        if (baseOn && baseRoot != null && autoNormalizeBaseLayout)
        {
            EnsureBaseEnvironment();
            NormalizeBaseLayout();
            AlignBaseRootToWorldMarker();
        }

        if (baseOn && autoFrameBaseRoot && baseRoot != null && !skipBaseFrame)
            ApplyBaseAnchorFromRoot();

        Transform anchor = worldOn ? worldCameraAnchor : baseCameraAnchor;

        if (anchor == null)
            anchor = worldOn ? baseCameraAnchor : worldCameraAnchor;

        if ((worldOn && skipWorldFrame || baseOn && skipBaseFrame) && mainCamera != null)
        {
            targetCamPos = mainCamera.transform.position;
            targetCamRot = mainCamera.transform.rotation;
            hasCameraTarget = true;
            cameraTransitionActive = false;
            cameraTransitionTimer = 0f;
        }
        else if (anchor != null)
        {
            targetCamPos = anchor.position;
            targetCamRot = anchor.rotation;
            hasCameraTarget = true;
            cameraTransitionActive = !instantCamera;
            cameraTransitionTimer = 0f;

            if (instantCamera && mainCamera != null)
            {
                mainCamera.transform.position = targetCamPos;
                mainCamera.transform.rotation = targetCamRot;
                cameraTransitionActive = false;
                cameraTransitionTimer = 0f;

                // Sync CameraController's targetPos to prevent snap-back shake
                CameraController camCtrl = mainCamera.GetComponent<CameraController>();
                if (camCtrl != null)
                {
                    if (baseOn)
                    {
                        camCtrl.lockBasePanToCenter = false;
                        camCtrl.baseMinY = Mathf.Min(camCtrl.baseMinY, 3.2f);
                        camCtrl.baseMaxY = Mathf.Max(camCtrl.baseMaxY, 24f);
                        camCtrl.basePanRadius = Mathf.Max(camCtrl.basePanRadius, GetBaseFootprintSize() * 0.55f);
                    }

                    // Accessing private field via reflection safely
                    var field = typeof(CameraController).GetField("targetPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) field.SetValue(camCtrl, targetCamPos);
                }
            }
}
        else
        {
            // Anchor yoksa kamerayÄ± olduÄŸu yerde bÄ±rak.
            hasCameraTarget = false;
            cameraTransitionActive = false;
            cameraTransitionTimer = 0f;
        }

        ModeChanged?.Invoke(CurrentMode);

        ApplyCameraFieldOfView(mode);

        if (UnitManager.Instance != null)
            UnitManager.Instance.RefreshUnitVisibility();
    }

    private void SuppressCameraAutoBaseEntry()
    {
        CameraController controller = mainCamera != null
            ? mainCamera.GetComponent<CameraController>()
            : FindAnyObjectByType<CameraController>();

        if (controller != null)
            controller.SuppressAutoEnterBase();
    }

    private void AlignBaseRootToWorldMarker()
    {
        if (!placeBaseRootAtWorldMarker || baseRoot == null)
            return;

        WorldBaseMarker marker = WorldBaseMarker.FindPrimary(true);
        if (marker == null)
            return;

        Vector3 position = marker.transform.position;
        position.y += baseRootWorldYOffset;
        baseRoot.transform.position = position;
    }

    private void ApplyCameraFieldOfView(GameViewMode mode)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        mainCamera.allowHDR = true;
        mainCamera.allowMSAA = true;
        mainCamera.fieldOfView =
            mode == GameViewMode.BaseView
                ? baseCameraFieldOfView
                : worldCameraFieldOfView;
    }

    private void ApplyProfessionalCameraTuning()
    {
        headquartersBasePosition = new Vector3(0f, 0f, 1.35f);
        barracksBasePosition = new Vector3(-1.9f, 0f, -1.45f);
        productionBasePosition = new Vector3(1.9f, 0f, -1.45f);
        baseInteriorHexFillRatio = Mathf.Clamp(baseInteriorHexFillRatio, 7.8f, 9.6f);

        baseCameraHeightPadding = Mathf.Clamp(baseCameraHeightPadding, 1.25f, 1.65f);

        minBaseCameraDistance = Mathf.Clamp(minBaseCameraDistance, 14f, 18f);

        maxBaseCameraDistance = Mathf.Clamp(maxBaseCameraDistance, 24f, 34f);
        if (maxBaseCameraDistance < minBaseCameraDistance + 6f)
            maxBaseCameraDistance = minBaseCameraDistance + 6f;

        baseCameraFieldOfView = Mathf.Clamp(baseCameraFieldOfView, 44f, 50f);
        worldCameraFieldOfView = Mathf.Clamp(worldCameraFieldOfView, 50f, 56f);

        if (worldCameraOffset.magnitude < 20f || worldCameraOffset.magnitude > 70f)
            worldCameraOffset = new Vector3(0f, 42f, -34f);

        if (baseCameraOffset.magnitude < 18f || baseCameraOffset.magnitude > 40f)
            baseCameraOffset = new Vector3(0f, 22f, -24f);
    }

    private void EnsureCameraAnchors()
    {
        if (mainCamera == null)
            return;

        if (worldCameraAnchor == null)
            worldCameraAnchor = CreateAnchor("WorldCameraAnchor_Auto", mainCamera.transform.position, mainCamera.transform.rotation);

        if (baseCameraAnchor == null)
        {
            Vector3 basePos = mainCamera.transform.position + new Vector3(-8f, -6f, -10f);
            Quaternion baseRot = Quaternion.Euler(28f, 45f, 0f);
            baseCameraAnchor = CreateAnchor("BaseCameraAnchor_Auto", basePos, baseRot);
        }
    }

    private void DiscoverAutoWorldObjects()
    {
        autoWorldObjects.Clear();

        if (!autoManageWorldObjects)
            return;

        HexGridManager[] grids =
            FindObjectsByType<HexGridManager>(FindObjectsInactive.Include);

        foreach (HexGridManager grid in grids)
            AddAutoWorldObject(grid.gameObject);

        WorldBaseMarker[] markers =
            FindObjectsByType<WorldBaseMarker>(FindObjectsInactive.Include);

        foreach (WorldBaseMarker marker in markers)
        {
            if (marker == null || !marker.IsPlacementMarker)
                continue;

            AddAutoWorldObject(marker.gameObject);
        }

        WorldResourceNodeManager[] resourceManagers =
            FindObjectsByType<WorldResourceNodeManager>(FindObjectsInactive.Include);

        foreach (WorldResourceNodeManager resourceManager in resourceManagers)
            AddAutoWorldObject(resourceManager.gameObject);
    }

    private void AddAutoWorldObject(GameObject obj)
    {
        if (obj == null)
            return;

        if (baseRoot != null && obj.transform.IsChildOf(baseRoot.transform))
            return;

        if (worldRoot != null && obj == worldRoot)
            return;

        if (!autoWorldObjects.Contains(obj))
            autoWorldObjects.Add(obj);
    }

    private void SetAutoWorldObjectsActive(bool active)
    {
        foreach (GameObject obj in autoWorldObjects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void ApplyBaseAnchorFromRoot()
    {
        if (baseCameraAnchor == null)
            baseCameraAnchor = CreateAnchor("BaseCameraAnchor_Auto", Vector3.zero, Quaternion.identity);

        Bounds contentBounds = GetBaseContentBounds();
        Vector3 center = contentBounds.center;
        if (baseRoot != null)
            center.y = baseRoot.transform.position.y + 0.2f;

        float footprint = Mathf.Max(GetBaseFootprintSize(), 12f);
        float distance = Mathf.Clamp(
            footprint * 1.26f,
            minBaseCameraDistance,
            maxBaseCameraDistance
        );

        Vector3 direction = new Vector3(0f, 0.78f, -0.62f).normalized;
        if (direction == Vector3.zero)
            direction = new Vector3(0f, 0.68f, -0.74f).normalized;

        Vector3 cameraPos = center + direction * distance;

        baseCameraAnchor.position = cameraPos;
        baseCameraAnchor.rotation = Quaternion.LookRotation(center - cameraPos, Vector3.up);
    }

    private void NormalizeBaseLayout()
    {
        if (baseRoot == null)
            return;

        BaseBuilding[] buildings =
            FindObjectsByType<BaseBuilding>(FindObjectsInactive.Include);

        foreach (BaseBuilding building in buildings)
        {
            if (building == null || building.data == null)
                continue;

            if (building.data.type == BuildingType.Headquarters)
            {
                SnapBuildingIntoBase(building, GetSafeHeadquartersPosition(), true);
                continue;
            }

            if (building.data.type == BuildingType.Barracks)
            {
                if (IsInsideBuildingSlot(building))
                    continue;

                SnapBuildingIntoBase(building, GetScaledBasePosition(barracksBasePosition), true);
                continue;
            }

            if (building.data.type == BuildingType.ProductionFacility && building.gameObject.activeSelf)
            {
                if (IsInsideBuildingSlot(building))
                    continue;

                SnapBuildingIntoBase(building, GetScaledBasePosition(productionBasePosition), true);
            }
        }
    }

    private void EnsureBaseEnvironment()
    {
        if (baseRoot == null)
            return;

        BaseViewEnvironment environment = baseRoot.GetComponent<BaseViewEnvironment>();
        if (environment == null)
            environment = baseRoot.AddComponent<BaseViewEnvironment>();

        environment.ApplyWorldContext();
        environment.Rebuild();
    }

    private bool IsInsideBuildingSlot(BaseBuilding building)
    {
        if (building == null)
            return false;

        return building.GetComponentInParent<BuildingSlot>() != null;
    }

    private Vector3 GetSafeHeadquartersPosition()
    {
        float halfSize = GetBaseFootprintSize() * 0.5f;
        return new Vector3(
            Mathf.Clamp(headquartersBasePosition.x * GetBaseLayoutMultiplier(), -halfSize * 0.18f, halfSize * 0.18f),
            headquartersBasePosition.y,
            Mathf.Clamp(halfSize * 0.22f, -halfSize * 0.22f, halfSize * 0.28f)
        );
    }

    private Vector3 GetScaledBasePosition(Vector3 localPosition)
    {
        float multiplier = GetBaseLayoutMultiplier();
        return new Vector3(localPosition.x * multiplier, localPosition.y, localPosition.z * multiplier);
    }

    private void SnapBuildingIntoBase(BaseBuilding building, Vector3 localPosition, bool forcePosition)
    {
        if (building == null || baseRoot == null)
            return;

        bool wasInsideBase = building.transform.IsChildOf(baseRoot.transform);
        Quaternion originalLocalRotation = building.transform.localRotation;

        if (!wasInsideBase)
        {
            building.transform.SetParent(baseRoot.transform, true);
            building.transform.localPosition = localPosition;
        }
        else if (forcePosition || IsOutsideBaseFootprint(building.transform.localPosition))
        {
            building.transform.localPosition = localPosition;
        }

        building.transform.localRotation = originalLocalRotation;
        building.transform.localScale = Vector3.one;
        building.FitVisualToFootprint(GetSuggestedBuildingFootprint(building.data.type));
        building.gameObject.SetActive(true);
        building.RebuildClickableCollider();
    }

    private bool IsOutsideBaseFootprint(Vector3 localPosition)
    {
        float halfSize = GetBaseFootprintSize() * 0.5f;
        return Mathf.Abs(localPosition.x) > halfSize || Mathf.Abs(localPosition.z) > halfSize;
    }

    private void ApplyWorldAnchorFromGrid()
    {
        HexGridManager grid =
            FindAnyObjectByType<HexGridManager>();

        if (grid == null)
            return;

        if (worldCameraAnchor == null)
            worldCameraAnchor = CreateAnchor("WorldCameraAnchor_Auto", Vector3.zero, Quaternion.identity);

        Bounds bounds = GetHexGridBounds(grid);
        Vector3 center = bounds.center;
        center.y = 0f;

        Vector3 direction = worldCameraOffset.normalized;
        if (direction == Vector3.zero)
            direction = new Vector3(-0.48f, 0.72f, -0.48f).normalized;

        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        float distance = Mathf.Clamp(footprint * 0.58f, 36f, 96f);
        Vector3 cameraPos = center + direction * distance;

        ConfigureCameraWorldBounds(grid, cameraPos);
        cameraPos = worldCameraAnchor.position;
        worldCameraAnchor.rotation = Quaternion.LookRotation(center - cameraPos, Vector3.up);
    }

    private void ConfigureCameraWorldBounds(HexGridManager grid, Vector3 cameraPosition)
    {
        if (grid == null || mainCamera == null)
            return;

        CameraController controller = mainCamera.GetComponent<CameraController>();
        if (controller == null)
            return;

        Bounds bounds = GetHexGridBounds(grid);
        float pad = 28f; // Use a larger inward padding to strictly hide map edges
        controller.SetWorldBounds(bounds, pad);

        Vector3 clamped = controller.ClampWorldPosition(cameraPosition);
        worldCameraAnchor.position = clamped;
    }

    private Bounds GetHexGridBounds(HexGridManager grid)
    {
        if (grid == null)
            return new Bounds(Vector3.zero, Vector3.one * 8f);

        if (grid.allHexCells == null || grid.allHexCells.Count == 0)
            return GetRootBounds(grid.gameObject);

        bool hasCells = false;
        Bounds bounds = new Bounds(grid.transform.position, Vector3.one * Mathf.Max(1f, grid.size));

        for (int i = 0; i < grid.allHexCells.Count; i++)
        {
            HexCell cell = grid.allHexCells[i];
            if (cell == null)
                continue;

            if (!hasCells)
            {
                bounds = new Bounds(cell.transform.position, Vector3.one * Mathf.Max(1f, grid.size));
                hasCells = true;
            }
            else
            {
                bounds.Encapsulate(cell.transform.position);
            }
        }

        if (!hasCells)
            return GetRootBounds(grid.gameObject);

        return bounds;
        }

    private Bounds GetBaseContentBounds()
    {
        if (baseRoot == null)
            return new Bounds(Vector3.zero, Vector3.one * 8f);

        BaseBuilding[] buildings = baseRoot.GetComponentsInChildren<BaseBuilding>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(baseRoot.transform.position, new Vector3(12f, 2f, 12f));

        foreach (BaseBuilding building in buildings)
        {
            if (building == null || !building.gameObject.activeInHierarchy)
                continue;

            Renderer[] renderers = building.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        float baseFootprintSize = GetBaseFootprintSize();
        float maxEnvSpan = Mathf.Max(2.2f, baseFootprintSize * 2.4f);

        Renderer[] environmentRenderers = baseRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in environmentRenderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (renderer.GetComponentInParent<BaseBuilding>() != null)
                continue;

            if (renderer.name.StartsWith("BaseTerrain_") ||
                renderer.name.StartsWith("BaseViewEnvironment_"))
            {
                continue;
            }

            Vector3 envSize = renderer.bounds.size;
            if (envSize.x > maxEnvSpan || envSize.z > maxEnvSpan)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            bounds = new Bounds(baseRoot.transform.position, new Vector3(12f, 3f, 12f));

        Bounds baseFootprint = new Bounds(baseRoot.transform.position, new Vector3(baseFootprintSize, 2f, baseFootprintSize));
        bounds.Encapsulate(baseFootprint);
        float padding = Mathf.Clamp(baseFootprintSize * 0.18f, 0.22f, 0.85f);
        bounds.Expand(new Vector3(padding, 0f, padding));
        return bounds;
    }

    private int GetHeadquartersLevel()
    {
        BaseBuilding[] buildings = FindObjectsByType<BaseBuilding>(FindObjectsInactive.Include);
        foreach (BaseBuilding building in buildings)
        {
            if (building == null || building.data == null)
                continue;

            if (building.data.type == BuildingType.Headquarters)
                return Mathf.Max(1, building.currentLevel);
        }

        return 1;
    }

    private int GetBaseExpansionStage()
    {
        int level = GetHeadquartersLevel();
        if (level >= 20)
            return 3;

        if (level >= 10)
            return 2;

        return 1;
    }

    private float GetBaseLayoutMultiplier()
    {
        return Mathf.Max(0.42f, GetBaseFootprintSize() / 7.2f);
    }

    private float GetBaseFootprintSize()
    {
        float oneHexFootprint = GetWorldHexFootprint();
        return Mathf.Max(16.5f, oneHexFootprint * baseInteriorHexFillRatio * GetBaseExpansionStage());
    }

    public float GetCurrentBaseFootprintSize()
    {
        return GetBaseFootprintSize();
    }

    public int GetCurrentBaseExpansionStage()
    {
        return GetBaseExpansionStage();
    }

    public float GetCurrentBaseLayoutMultiplier()
    {
        return GetBaseLayoutMultiplier();
    }

    public float GetSuggestedBuildingFootprint(BuildingType type)
    {
        float footprint = GetBaseFootprintSize();

        if (type == BuildingType.Headquarters)
            return Mathf.Clamp(footprint * 0.58f, 8.5f, 12.0f);

        if (type == BuildingType.Barracks)
            return Mathf.Clamp(footprint * 0.22f, 3.0f, 4.2f);

        if (type == BuildingType.ProductionFacility)
            return Mathf.Clamp(footprint * 0.23f, 3.0f, 4.4f);

        return Mathf.Clamp(footprint * 0.22f, 2.4f, 4.0f);
    }

    public float GetSuggestedBaseUnitScale()
    {
        return Mathf.Clamp(GetBaseFootprintSize() * 0.14f, 0.38f, 0.72f);
    }

    private float GetWorldHexFootprint()
    {
        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        if (grid == null)
            return 1.7f;

        WorldBaseMarker marker = WorldBaseMarker.FindPrimary(true);
        if (marker != null && grid.allHexCells != null && grid.allHexCells.Count > 0)
        {
            HexCell markerHex = grid.GetClosestHex(marker.transform.position);
            float rendererFootprint = GetRendererFootprint(markerHex != null ? markerHex.gameObject : null);
            if (rendererFootprint > 0.05f)
                return rendererFootprint;
        }

        return Mathf.Max(1.55f, Mathf.Sqrt(3f) * Mathf.Max(0.1f, grid.size));
    }

    private float GetRendererFootprint(GameObject source)
    {
        if (source == null)
            return 0f;

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return 0f;

        bool hasBounds = false;
        Bounds bounds = new Bounds(source.transform.position, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return 0f;

        return Mathf.Max(bounds.size.x, bounds.size.z);
    }

    private Vector3 GetRootCenter(GameObject root)
    {
        if (root == null)
            return Vector3.zero;

        return GetRootBounds(root).center;
    }

    private Bounds GetRootBounds(GameObject root)
    {
        if (root == null)
            return new Bounds(Vector3.zero, Vector3.one);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
        if (renderers == null || renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one * 8f);

        bool hasBounds = false;
        Bounds bounds = new Bounds(root.transform.position, Vector3.one);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null ||
                renderers[i].GetComponentInParent<WorldMapCloudMask>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderers[i].bounds);
        }

        if (!hasBounds)
            return new Bounds(root.transform.position, Vector3.one * 8f);

        return bounds;
    }

    private Transform CreateAnchor(string name, Vector3 pos, Quaternion rot)
    {
        GameObject anchor = new GameObject(name);
        anchor.transform.position = pos;
        anchor.transform.rotation = rot;
        return anchor.transform;
    }
}

