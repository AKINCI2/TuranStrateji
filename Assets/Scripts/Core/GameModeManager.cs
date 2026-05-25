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
    public Vector3 worldCameraOffset = new Vector3(-42f, 58f, -42f);
    public bool autoFrameBaseRoot = true;
    public bool autoNormalizeBaseLayout = true;
    public Vector3 baseCameraOffset = new Vector3(-3.2f, 4.2f, -3.2f);
    public float baseCameraHeightPadding = 1.65f;
    public float minBaseCameraDistance = 3.1f;
    public float maxBaseCameraDistance = 8.5f;
    public float baseCameraFieldOfView = 44f;
    public float worldCameraFieldOfView = 60f;

    [Header("Base Layout")]
    public Vector3 headquartersBasePosition = new Vector3(0f, 0f, 1.55f);
    public Vector3 barracksBasePosition = new Vector3(-2.65f, 0f, -2.35f);
    public Vector3 productionBasePosition = new Vector3(2.65f, 0f, -2.35f);
    public float baseInteriorHexFillRatio = 0.96f;

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
        headquartersBasePosition = new Vector3(0f, 0f, 1.55f);
        barracksBasePosition = new Vector3(-2.65f, 0f, -2.35f);
        productionBasePosition = new Vector3(2.65f, 0f, -2.35f);

        baseCameraHeightPadding = Mathf.Clamp(baseCameraHeightPadding, 1.45f, 2.15f);

        minBaseCameraDistance = Mathf.Clamp(minBaseCameraDistance, 2.6f, 4.4f);

        maxBaseCameraDistance = Mathf.Clamp(maxBaseCameraDistance, 6.5f, 12f);
        if (maxBaseCameraDistance < minBaseCameraDistance + 2.4f)
            maxBaseCameraDistance = minBaseCameraDistance + 2.4f;

        baseCameraFieldOfView = Mathf.Clamp(baseCameraFieldOfView, 40f, 48f);

        if (baseCameraOffset.magnitude < 2.6f || baseCameraOffset.magnitude > 10f)
            baseCameraOffset = new Vector3(-3.2f, 4.2f, -3.2f);
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

        Bounds bounds = GetBaseContentBounds();
        Vector3 center = bounds.center;
        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        float distance = Mathf.Clamp(
            footprint * baseCameraHeightPadding,
            minBaseCameraDistance,
            maxBaseCameraDistance
        );

        Vector3 direction = baseCameraOffset.normalized;
        if (direction == Vector3.zero)
            direction = new Vector3(-0.45f, 0.72f, -0.45f).normalized;

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

        WorldBaseMarker marker = WorldBaseMarker.FindPrimary(true);
        if (marker != null)
        {
            Vector3 baseCenter = marker.transform.position;
            Vector3 baseDirection = worldCameraOffset.normalized;
            if (baseDirection == Vector3.zero)
                baseDirection = new Vector3(-0.48f, 0.72f, -0.48f).normalized;

            float baseDistance = Mathf.Clamp(GetWorldHexFootprint() * 7.5f, 10f, 22f);
            Vector3 baseCameraPos = baseCenter + baseDirection * baseDistance;

            worldCameraAnchor.position = baseCameraPos;
            worldCameraAnchor.rotation = Quaternion.LookRotation(baseCenter - baseCameraPos, Vector3.up);
            return;
        }

        Bounds bounds = GetRootBounds(grid.gameObject);
        Vector3 center = bounds.center;

        Vector3 direction = worldCameraOffset.normalized;
        if (direction == Vector3.zero)
            direction = new Vector3(-0.48f, 0.72f, -0.48f).normalized;

        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        float distance = Mathf.Clamp(footprint * 0.62f, 34f, 92f);
        Vector3 cameraPos = center + direction * distance;

        worldCameraAnchor.position = cameraPos;
        worldCameraAnchor.rotation = Quaternion.LookRotation(center - cameraPos, Vector3.up);
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
        return Mathf.Max(0.22f, GetBaseFootprintSize() / 8.8f);
    }

    private float GetBaseFootprintSize()
    {
        float oneHexFootprint = GetWorldHexFootprint();
        return Mathf.Max(1.45f, oneHexFootprint * baseInteriorHexFillRatio * GetBaseExpansionStage());
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
            return Mathf.Clamp(footprint * 0.86f, 1.05f, 3.8f);

        if (type == BuildingType.Barracks)
            return Mathf.Clamp(footprint * 0.42f, 0.54f, 1.85f);

        if (type == BuildingType.ProductionFacility)
            return Mathf.Clamp(footprint * 0.42f, 0.54f, 1.85f);

        return Mathf.Clamp(footprint * 0.38f, 0.48f, 1.7f);
    }

    public float GetSuggestedBaseUnitScale()
    {
        return Mathf.Clamp(GetBaseFootprintSize() * 0.11f, 0.20f, 0.42f);
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

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

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

