using UnityEngine;

public class WorldBaseMarker : MonoBehaviour
{
    [Header("Identity")]
    public string baseId = "player_main_base";

    [Header("Optional Link")]
    public BaseBuilding linkedHeadquarters;

    [Header("World Visual")]
    public bool useHeadquartersVisual = true;
    public Transform visualRoot;
    public float worldVisualScale = 1f;
    public bool autoFitWorldVisual = true;
    public float targetWorldFootprint = 1.45f;
    public float worldHexFillRatio = 0.94f;
    public float minWorldVisualScale = 0.08f;
    public float maxWorldVisualScale = 8f;
    public Vector3 worldVisualRotation = Vector3.zero;
    public bool applyMeshyWorldRotationFix = true;
    public Vector3 meshyWorldRotationFix = new Vector3(-90f, 0f, 0f);
    public Vector3 worldVisualOffset = Vector3.zero;
    public bool useLiveHeadquartersInSeamlessMode = true;
    public bool autoCorrectWorldVisualUpright = true;
    public bool showWorldFortWalls = true;
    public Color worldFortWallColor = new Color(0.19f, 0.19f, 0.2f, 1f);
    [Range(0.04f, 0.35f)]
    public float worldFortWallThickness = 0.11f;
    [Range(0.06f, 0.8f)]
    public float worldFortWallHeight = 0.2f;

    [Header("Placement Rules")]
    public bool enforcePlacementRules = true;
    public float markerHeight = 0.15f;

    private GameObject activeVisual;
    private GameObject activeVisualPrefab;
    private Transform worldFortRoot;
    private GameViewMode lastKnownMode = GameViewMode.WorldMap;

    public bool IsPlacementMarker => GetComponent<BaseBuilding>() == null;

    public static WorldBaseMarker FindPrimary(bool includeInactive = false)
    {
        WorldBaseMarker[] markers = FindObjectsByType<WorldBaseMarker>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude
        );

        WorldBaseMarker fallback = null;
        foreach (WorldBaseMarker marker in markers)
        {
            if (marker == null || !marker.IsPlacementMarker)
                continue;

            if (marker.name.Contains("PlayerBaseMarker"))
                return marker;

            fallback ??= marker;
        }

        return fallback;
    }

    void Awake()
    {
        if (!IsPlacementMarker)
            enabled = false;
    }

    void Start()
    {
        if (!IsPlacementMarker)
            return;

        EnsureLinkedHeadquarters();
        ValidatePlacement();
        if (GameModeManager.Instance != null)
            lastKnownMode = GameModeManager.Instance.CurrentMode;
        RefreshWorldVisual();
    }

    void LateUpdate()
    {
        if (!IsPlacementMarker)
            return;

        if (GameModeManager.Instance == null)
            return;

        GameViewMode currentMode = GameModeManager.Instance.CurrentMode;
        if (currentMode == lastKnownMode)
            return;

        lastKnownMode = currentMode;
        RefreshWorldVisual();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        UnityEditor.EditorApplication.delayCall -= RefreshEditorWorldVisual;
        UnityEditor.EditorApplication.delayCall += RefreshEditorWorldVisual;
    }

    private void RefreshEditorWorldVisual()
    {
        if (this == null || Application.isPlaying)
            return;

        if (!IsPlacementMarker)
            return;

        if (UnityEditor.EditorUtility.IsPersistent(this) ||
            UnityEditor.EditorUtility.IsPersistent(gameObject) ||
            !gameObject.scene.IsValid())
        {
            return;
        }

        EnsureLinkedHeadquarters();
        RefreshWorldVisual();
    }
#endif

    public bool ValidatePlacement()
    {
        if (!enforcePlacementRules)
            return true;

        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        if (grid == null)
            return true;

        HexCell currentHex = grid.GetClosestHex(transform.position);
        if (currentHex != null && grid.IsBasePlacementAllowed(currentHex))
        {
            SnapToHex(currentHex);
            return true;
        }

        HexCell safeHex = grid.GetNearestBasePlacementHex(transform.position);
        if (safeHex == null)
            return false;

        SnapToHex(safeHex);
        return true;
    }

    private void SnapToHex(HexCell hex)
    {
        if (hex == null)
            return;

        Vector3 position = hex.transform.position;
        position.y += markerHeight;
        transform.position = position;
    }

    public void RefreshWorldVisual()
    {
        if (!useHeadquartersVisual)
            return;

        worldHexFillRatio = Mathf.Clamp(worldHexFillRatio, 2.4f, 3.2f);
        targetWorldFootprint = GetLevelBasedWorldFootprint();
        minWorldVisualScale = Mathf.Min(minWorldVisualScale, 0.08f);
        maxWorldVisualScale = Mathf.Clamp(maxWorldVisualScale, 3f, 12f);
        autoCorrectWorldVisualUpright = true;
        worldVisualRotation = Vector3.zero;
        transform.rotation = Quaternion.identity;

        EnsureVisualRoot();
        HidePrimitiveMarkerRenderers();

        if (ShouldUseLiveHeadquartersVisual())
        {
            ClearVisualRoot();
            ClearWorldFortWalls();
            EnsureClickCollider();
            return;
        }

        GameObject visualPrefab = GetCurrentHeadquartersVisualPrefab();
        if (visualPrefab == null)
            return;

        if (activeVisual != null && activeVisualPrefab == visualPrefab)
        {
            ApplyVisualTransform();
            RebuildWorldFortWalls();
            EnsureClickCollider();
            return;
        }

        ClearVisualRoot();
        activeVisualPrefab = visualPrefab;
        activeVisual = Instantiate(visualPrefab, visualRoot);
        activeVisual.name = visualPrefab.name + "_World";
        ApplyVisualTransform();
        DisableVisualColliders();
        RebuildWorldFortWalls();
        EnsureClickCollider();
    }

    private void EnsureLinkedHeadquarters()
    {
        if (linkedHeadquarters != null)
            return;

        BaseBuilding[] buildings = FindObjectsByType<BaseBuilding>(FindObjectsInactive.Include);
        foreach (BaseBuilding building in buildings)
        {
            if (building == null || building.data == null)
                continue;

            if (building.data.type != BuildingType.Headquarters)
                continue;

            linkedHeadquarters = building;
            return;
        }
    }

    private GameObject GetCurrentHeadquartersVisualPrefab()
    {
        if (linkedHeadquarters == null)
            return null;

        BuildingLevelData levelData = linkedHeadquarters.CurrentLevelData;
        return levelData != null ? levelData.visualPrefab : null;
    }

    private bool ShouldUseLiveHeadquartersVisual()
    {
        if (!useLiveHeadquartersInSeamlessMode)
            return false;

        if (linkedHeadquarters == null)
            return false;

        GameModeManager manager = GameModeManager.Instance;
        return manager != null &&
               manager.seamlessWorldBaseMode &&
               manager.CurrentMode == GameViewMode.BaseView;
    }

    private void EnsureVisualRoot()
    {
        if (visualRoot != null)
            return;

        Transform existing = transform.Find("WorldBaseVisual");
        if (existing != null)
        {
            visualRoot = existing;
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
            return;
        }

        GameObject root = new GameObject("WorldBaseVisual");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        visualRoot = root.transform;
    }

    private void ApplyVisualTransform()
    {
        if (activeVisual == null)
            return;

        activeVisual.transform.localPosition = worldVisualOffset;
        Quaternion rotation = Quaternion.Euler(worldVisualRotation);
        if (applyMeshyWorldRotationFix)
            rotation *= Quaternion.Euler(meshyWorldRotationFix);

        Vector3 scale = Vector3.one * worldVisualScale;
        BuildingLevelData levelData = linkedHeadquarters != null ? linkedHeadquarters.CurrentLevelData : null;
        if (levelData != null)
        {
            rotation *= Quaternion.Euler(levelData.visualRotation);
            scale = Vector3.Scale(scale, levelData.visualScale);
        }

        activeVisual.transform.localRotation = rotation;
        activeVisual.transform.localScale = scale;

        if (autoCorrectWorldVisualUpright)
            activeVisual.transform.localRotation = GetBestUprightRotation(activeVisual.transform.localRotation);

        if (autoFitWorldVisual)
            FitVisualToWorldFootprint();
    }

    private float GetLevelBasedWorldFootprint()
    {
        return GetWorldHexFootprint() * worldHexFillRatio;
    }

    private float GetWorldHexFootprint()
    {
        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        if (grid != null)
        {
            if (grid.allHexCells != null && grid.allHexCells.Count > 0)
            {
                HexCell closestHex = grid.GetClosestHex(transform.position);
                if (closestHex != null)
                {
                    float rendererFootprint = GetRendererFootprint(closestHex.gameObject);
                    if (rendererFootprint > 0.05f)
                        return rendererFootprint;
                }
            }

            return Mathf.Max(1.2f, Mathf.Sqrt(3f) * Mathf.Max(0.1f, grid.size));
        }

        return 1.7f;
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

    private Quaternion GetBestUprightRotation(Quaternion baseRotation)
    {
        Quaternion[] candidates =
        {
            Quaternion.identity,
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(-90f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            Quaternion.Euler(0f, 0f, -90f),
            Quaternion.Euler(90f, 180f, 0f),
            Quaternion.Euler(-90f, 180f, 0f),
            Quaternion.Euler(0f, 180f, 90f),
            Quaternion.Euler(0f, 180f, -90f)
        };

        Quaternion bestRotation = baseRotation;
        float bestScore = float.NegativeInfinity;

        foreach (Quaternion candidate in candidates)
        {
            Quaternion testRotation = baseRotation * candidate;
            activeVisual.transform.localRotation = testRotation;

            if (!TryGetActiveVisualBounds(out Bounds bounds))
                continue;

            float broadFootprint = Mathf.Max(bounds.size.x, bounds.size.z);
            float narrowFootprint = Mathf.Min(bounds.size.x, bounds.size.z);
            float heightRatio = bounds.size.y / Mathf.Max(0.001f, broadFootprint);
            float squareness = narrowFootprint / Mathf.Max(0.001f, broadFootprint);
            float targetPenalty = Mathf.Abs(broadFootprint - targetWorldFootprint) / Mathf.Max(0.001f, targetWorldFootprint);
            float tooTallPenalty = Mathf.Max(0f, heightRatio - 0.72f) * 7.5f;
            float tooFlatPenalty = Mathf.Max(0f, 0.08f - heightRatio) * 2f;
            float score = squareness * 3.5f - tooTallPenalty - tooFlatPenalty - targetPenalty;
            if (heightRatio > 1.25f)
                score -= 5f;

            if (score <= bestScore)
                continue;

            bestScore = score;
            bestRotation = testRotation;
        }

        activeVisual.transform.localRotation = bestRotation;
        return bestRotation;
    }

    private void FitVisualToWorldFootprint()
    {
        if (activeVisual == null || targetWorldFootprint <= 0f)
            return;

        if (!TryGetActiveVisualBounds(out Bounds bounds))
            return;

        float currentFootprint = Mathf.Max(bounds.size.x, bounds.size.z);
        if (currentFootprint <= 0.001f)
            return;

        float fitMultiplier = targetWorldFootprint / currentFootprint;
        float fittedScale = Mathf.Clamp(
            activeVisual.transform.localScale.x * fitMultiplier,
            minWorldVisualScale,
            maxWorldVisualScale
        );

        activeVisual.transform.localScale = Vector3.one * fittedScale;
    }

    private bool TryGetActiveVisualBounds(out Bounds bounds)
    {
        bounds = new Bounds(activeVisual != null ? activeVisual.transform.position : transform.position, Vector3.one);
        if (activeVisual == null)
            return false;

        Renderer[] renderers = activeVisual.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return false;

        bool hasBounds = false;

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
            return false;

        return true;
    }

    private void ClearVisualRoot()
    {
        if (visualRoot == null)
            return;

        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = visualRoot.GetChild(i);
            if (child == null)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        activeVisual = null;
        activeVisualPrefab = null;
    }

    private void RebuildWorldFortWalls()
    {
        ClearWorldFortWalls();

        if (!showWorldFortWalls || visualRoot == null)
            return;

        float footprint = Mathf.Max(0.8f, targetWorldFootprint);
        float wallLength = footprint * 0.92f;
        float half = wallLength * 0.5f;
        float thickness = Mathf.Clamp(worldFortWallThickness, 0.04f, 0.35f);
        float wallHeight = Mathf.Clamp(worldFortWallHeight, 0.06f, 0.8f);

        GameObject root = new GameObject("WorldFortWalls");
        root.transform.SetParent(visualRoot, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        worldFortRoot = root.transform;

        CreateWorldWall("North", new Vector3(0f, wallHeight * 0.5f, half), new Vector3(wallLength, wallHeight, thickness));
        CreateWorldWall("South", new Vector3(0f, wallHeight * 0.5f, -half), new Vector3(wallLength, wallHeight, thickness));
        CreateWorldWall("East", new Vector3(half, wallHeight * 0.5f, 0f), new Vector3(thickness, wallHeight, wallLength));
        CreateWorldWall("West", new Vector3(-half, wallHeight * 0.5f, 0f), new Vector3(thickness, wallHeight, wallLength));
    }

    private void CreateWorldWall(string wallName, Vector3 localPos, Vector3 localScale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(worldFortRoot, false);
        wall.transform.localPosition = localPos;
        wall.transform.localRotation = Quaternion.identity;
        wall.transform.localScale = localScale;

        Collider col = wall.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying)
                Destroy(col);
            else
                DestroyImmediate(col);
        }

        Renderer renderer = wall.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = worldFortWallColor;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void ClearWorldFortWalls()
    {
        if (worldFortRoot == null)
            return;

        if (Application.isPlaying)
            Destroy(worldFortRoot.gameObject);
        else
            DestroyImmediate(worldFortRoot.gameObject);

        worldFortRoot = null;
    }

    private void DisableVisualColliders()
    {
        if (activeVisual == null)
            return;

        Collider[] colliders = activeVisual.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null)
                collider.enabled = false;
        }
    }

    private void HidePrimitiveMarkerRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (visualRoot != null && renderer.transform.IsChildOf(visualRoot))
                continue;

            string lowerName = renderer.name.ToLowerInvariant();
            if (lowerName.Contains("cylinder") || lowerName.Contains("marker"))
                renderer.enabled = false;
        }
    }

    private void EnsureClickCollider()
    {
        float footprint = Mathf.Max(1.8f, targetWorldFootprint);
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0f, 0.35f, 0f);
            boxCollider.size = new Vector3(footprint, 0.8f, footprint);
            return;
        }

        boxCollider.center = new Vector3(0f, 0.35f, 0f);
        boxCollider.size = new Vector3(footprint, 0.8f, footprint);
    }
}

