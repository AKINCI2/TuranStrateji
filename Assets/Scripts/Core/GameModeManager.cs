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
    public Vector3 baseCameraOffset = new Vector3(-9.5f, 11.5f, -9.5f);
    public float baseCameraHeightPadding = 0.72f;
    public float minBaseCameraDistance = 12f;
    public float maxBaseCameraDistance = 24f;
    public float baseCameraFieldOfView = 48f;
    public float worldCameraFieldOfView = 60f;

    [Header("Base Layout")]
    public Vector3 headquartersBasePosition = new Vector3(0f, 0f, 1.55f);
    public Vector3 barracksBasePosition = new Vector3(-2.65f, 0f, -2.35f);
    public Vector3 productionBasePosition = new Vector3(2.65f, 0f, -2.35f);

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
        if (mainCamera == null)
            mainCamera = Camera.main;

        ApplyProfessionalCameraTuning();
        DiscoverAutoWorldObjects();
        EnsureCameraAnchors();
        AlignBaseRootToWorldMarker();
        SetMode(GameViewMode.WorldMap, true);
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

    public void EnterWorldMap(bool preserveCameraFrame)
    {
        if (preserveCameraFrame && CurrentMode == GameViewMode.WorldMap)
            return;

        if (preserveCameraFrame)
            preserveCurrentCameraOnNextWorldEnter = true;

        SetMode(GameViewMode.WorldMap, false);
    }

    public void SetMode(GameViewMode mode, bool instantCamera)
    {
        CurrentMode = mode;

        bool worldOn = mode == GameViewMode.WorldMap;
        bool baseOn = mode == GameViewMode.BaseView;

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

        baseCameraHeightPadding = Mathf.Clamp(baseCameraHeightPadding, 0.64f, 0.82f);

        minBaseCameraDistance = Mathf.Clamp(minBaseCameraDistance, 10f, 14f);

        maxBaseCameraDistance = Mathf.Clamp(maxBaseCameraDistance, 20f, 28f);

        baseCameraFieldOfView = Mathf.Clamp(baseCameraFieldOfView, 46f, 52f);

        if (baseCameraOffset.magnitude < 14f || baseCameraOffset.magnitude > 28f)
            baseCameraOffset = new Vector3(-9.5f, 11.5f, -9.5f);
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
        return new Vector3(
            Mathf.Clamp(headquartersBasePosition.x, -0.8f, 0.8f),
            headquartersBasePosition.y,
            Mathf.Clamp(headquartersBasePosition.z, 0.6f, 1.7f)
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
        Vector3 originalLocalScale = building.transform.localScale;
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
        building.transform.localScale = originalLocalScale;
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

        Renderer[] environmentRenderers = baseRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in environmentRenderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (renderer.GetComponentInParent<BaseBuilding>() != null)
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

        float baseFootprintSize = GetBaseFootprintSize();
        Bounds baseFootprint = new Bounds(baseRoot.transform.position, new Vector3(baseFootprintSize, 2f, baseFootprintSize));
        bounds.Encapsulate(baseFootprint);
        bounds.Expand(new Vector3(1.4f, 0f, 1.4f));
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
        return 1f + (GetBaseExpansionStage() - 1) * 0.55f;
    }

    private float GetBaseFootprintSize()
    {
        switch (GetBaseExpansionStage())
        {
            case 3:
                return 16.5f;
            case 2:
                return 12.8f;
            default:
                return 8.8f;
        }
    }

    public float GetCurrentBaseFootprintSize()
    {
        return GetBaseFootprintSize();
    }

    public int GetCurrentBaseExpansionStage()
    {
        return GetBaseExpansionStage();
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

