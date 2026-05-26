using UnityEngine;

public class BaseBuilding : MonoBehaviour
{
    [Header("Data")]
    public BuildingData data;
    public int currentLevel = 1;

    [Header("Scene Roots")]
    public Transform modelRoot;
    public Transform spawnPointsRoot;
    public Transform uiAnchor;
    public bool useRootClickColliderOnly = true;
    public bool autoFitClickCollider;
    public bool useDedicatedClickProxy = true;

    [Header("Runtime")]
    public bool isUpgrading;
    public float upgradeRemainingSeconds;

    private GameObject activeVisual;
    private const string ClickProxyName = "ClickProxy";

    public BuildingLevelData CurrentLevelData =>
        data != null ? data.GetLevelData(currentLevel) : null;

    public BuildingLevelData NextLevelData =>
        data != null ? data.GetNextLevelData(currentLevel) : null;

    void Awake()
    {
        useDedicatedClickProxy = true;
        EnsureStructure();
        RefreshVisual();
        RebuildClickableCollider();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        UnityEditor.EditorApplication.delayCall -= RefreshEditorVisual;
        UnityEditor.EditorApplication.delayCall += RefreshEditorVisual;
    }

    private void RefreshEditorVisual()
    {
        if (this == null || Application.isPlaying)
            return;

        if (UnityEditor.EditorUtility.IsPersistent(this) ||
            UnityEditor.EditorUtility.IsPersistent(gameObject) ||
            !gameObject.scene.IsValid())
        {
            return;
        }

        useDedicatedClickProxy = true;
        EnsureStructure();
        RefreshVisual();
        RebuildClickableCollider();
    }
#endif

    public void RebuildClickableCollider()
    {
        EnsureClickableCollider(autoFitClickCollider);
    }

    public void FitVisualToFootprint(float maxFootprint)
    {
        if (maxFootprint <= 0.05f)
            return;

        if (!TryGetVisualBounds(out Bounds bounds))
            return;

        float currentFootprint = Mathf.Max(bounds.size.x, bounds.size.z);
        if (currentFootprint <= 0.001f)
            return;

        float fitMultiplier = maxFootprint / currentFootprint;

        Transform scaleTarget = modelRoot != null ? modelRoot : transform;
        float nextScale = Mathf.Clamp(scaleTarget.localScale.x * fitMultiplier, 0.025f, 6f);
        scaleTarget.localScale = Vector3.one * nextScale;
    }

    void Update()
    {
        if (!isUpgrading)
            return;

        upgradeRemainingSeconds -= Time.deltaTime;

        if (upgradeRemainingSeconds <= 0f)
            FinishUpgrade();
    }

    public bool CanStartUpgrade(int headquartersLevel, ResourceCost wallet)
    {
        BuildingLevelData next =
            NextLevelData;

        if (data == null || next == null || isUpgrading)
            return false;

        if (headquartersLevel < next.requiredHeadquartersLevel)
            return false;

        return next.upgradeCost.CanAfford(wallet);
    }

    public bool StartUpgrade(int headquartersLevel, ref ResourceCost wallet)
    {
        if (!CanStartUpgrade(headquartersLevel, wallet))
            return false;

        BuildingLevelData next =
            NextLevelData;

        wallet -= next.upgradeCost;
        isUpgrading = true;
        upgradeRemainingSeconds =
            Mathf.Max(0f, next.upgradeSeconds);

        if (upgradeRemainingSeconds <= 0f)
            FinishUpgrade();

        return true;
    }

    public void FinishUpgrade()
    {
        if (NextLevelData == null)
        {
            isUpgrading = false;
            upgradeRemainingSeconds = 0f;
            return;
        }

        currentLevel++;
        isUpgrading = false;
        upgradeRemainingSeconds = 0f;
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        EnsureStructure();

        BuildingLevelData levelData =
            CurrentLevelData;

        HideLegacyRootRenderers();
        ClearModelRoot();
        activeVisual = null;

        if (levelData == null || levelData.visualPrefab == null)
        {
            activeVisual = CreateFallbackVisual();
            return;
        }

        activeVisual =
            Instantiate(levelData.visualPrefab, modelRoot);

        activeVisual.name =
            levelData.visualPrefab.name;
        activeVisual.transform.localPosition = Vector3.zero;
        activeVisual.transform.localRotation = Quaternion.Euler(levelData.visualRotation);
        activeVisual.transform.localScale = levelData.visualScale;
    }

    private GameObject CreateFallbackVisual()
    {
        GameObject root = new GameObject("Fallback_" + (data != null ? data.type.ToString() : "Building"));
        root.transform.SetParent(modelRoot, false);

        BuildingType type = data != null ? data.type : BuildingType.Headquarters;

        if (type == BuildingType.Barracks)
        {
            CreateFallbackBox(root.transform, "Barracks_TrainingPad", new Vector3(1.15f, 0.04f, 0.78f), new Vector3(0f, 0.02f, 0f), new Color(0.24f, 0.22f, 0.19f, 1f));
            CreateFallbackBox(root.transform, "Barracks_RunLane_A", new Vector3(0.10f, 0.05f, 0.64f), new Vector3(-0.34f, 0.07f, 0f), new Color(0.42f, 0.35f, 0.22f, 1f));
            CreateFallbackBox(root.transform, "Barracks_RunLane_B", new Vector3(0.10f, 0.05f, 0.64f), new Vector3(0.34f, 0.07f, 0f), new Color(0.42f, 0.35f, 0.22f, 1f));
            CreateFallbackBox(root.transform, "Barracks_Obstacle", new Vector3(0.62f, 0.08f, 0.08f), new Vector3(0f, 0.12f, 0.23f), new Color(0.34f, 0.28f, 0.18f, 1f));
            CreateFallbackBox(root.transform, "Barracks_Sandbag", new Vector3(0.78f, 0.12f, 0.10f), new Vector3(0f, 0.11f, -0.32f), new Color(0.47f, 0.43f, 0.33f, 1f));
            return root;
        }

        if (type == BuildingType.ProductionFacility)
        {
            CreateFallbackBox(root.transform, "Facility_Core", new Vector3(1f, 0.42f, 0.74f), new Vector3(0f, 0.21f, 0f), new Color(0.22f, 0.28f, 0.30f, 1f));
            CreateFallbackBox(root.transform, "Facility_Tank", new Vector3(0.28f, 0.34f, 0.28f), new Vector3(0.36f, 0.30f, 0.15f), new Color(0.42f, 0.43f, 0.40f, 1f));
            return root;
        }

        CreateFallbackBox(root.transform, "Building_Core", new Vector3(1f, 0.45f, 1f), new Vector3(0f, 0.225f, 0f), new Color(0.24f, 0.26f, 0.27f, 1f));
        return root;
    }

    private void CreateFallbackBox(Transform parent, string objectName, Vector3 size, Vector3 localPosition, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localRotation = Quaternion.identity;
        box.transform.localScale = size;

        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = color;
            renderer.sharedMaterial = material;
        }
    }

    private void HideLegacyRootRenderers()
    {
        Renderer[] renderers = GetComponents<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }

        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in childRenderers)
        {
            if (renderer == null)
                continue;

            if (modelRoot != null && renderer.transform.IsChildOf(modelRoot))
                continue;

            if (spawnPointsRoot != null && renderer.transform.IsChildOf(spawnPointsRoot))
                continue;

            if (uiAnchor != null && renderer.transform.IsChildOf(uiAnchor))
                continue;

            renderer.enabled = false;
        }
    }

    public string GetDisplayName()
    {
        if (data == null)
            return gameObject.name;

        return $"{data.displayName} Lv.{currentLevel}";
    }

    void EnsureStructure()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (modelRoot != null && UnityEditor.EditorUtility.IsPersistent(modelRoot.gameObject))
                modelRoot = null;

            if (spawnPointsRoot != null && UnityEditor.EditorUtility.IsPersistent(spawnPointsRoot.gameObject))
                spawnPointsRoot = null;

            if (uiAnchor != null && UnityEditor.EditorUtility.IsPersistent(uiAnchor.gameObject))
                uiAnchor = null;
        }
#endif

        if (modelRoot == null)
            modelRoot = FindOrCreateChild("Model");

        if (spawnPointsRoot == null)
            spawnPointsRoot = FindOrCreateChild("SpawnPoints");

        if (uiAnchor == null)
            uiAnchor = FindOrCreateChild("UIAnchor");
    }

    void ClearModelRoot()
    {
        if (modelRoot == null)
            return;

        for (int i = modelRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = modelRoot.GetChild(i);
            if (child == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.EditorUtility.IsPersistent(child.gameObject))
                continue;
#endif

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    Transform FindOrCreateChild(string childName)
    {
        Transform child =
            transform.Find(childName);

        if (child != null)
            return child;

        GameObject childObject =
            new GameObject(childName);

        childObject.transform.SetParent(transform);
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;

        return childObject.transform;
    }

    private void EnsureClickableCollider(bool forceRebuild)
    {
        if (useDedicatedClickProxy)
        {
            EnsureProxyClickCollider(forceRebuild);
            return;
        }

        if (!forceRebuild)
        {
            BoxCollider existingRootCollider = GetComponent<BoxCollider>();
            if (existingRootCollider != null)
            {
                if (ShouldResetExistingCollider(existingRootCollider))
                {
                    if (!TryApplyRendererBoundsCollider(existingRootCollider))
                        ApplyClickColliderPreset(existingRootCollider);
                }

                ClampColliderForType(existingRootCollider);

                if (useRootClickColliderOnly)
                    DisableNestedModelColliders(existingRootCollider);

                return;
            }
        }

        BoxCollider[] rootColliders = GetComponents<BoxCollider>();
        for (int i = rootColliders.Length - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(rootColliders[i]);
            else
                DestroyImmediate(rootColliders[i]);
        }

        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        if (!TryApplyRendererBoundsCollider(collider))
            ApplyClickColliderPreset(collider);
        ClampColliderForType(collider);

        if (useRootClickColliderOnly)
            DisableNestedModelColliders(collider);
    }

    private void EnsureProxyClickCollider(bool forceRebuild)
    {
        RemoveRootBoxColliders();

        Transform proxyTransform = GetOrCreateClickProxyTransform();
        if (proxyTransform == null)
            return;

        proxyTransform.localPosition = Vector3.zero;
        proxyTransform.localRotation = Quaternion.identity;
        proxyTransform.localScale = Vector3.one;

        BuildingClickProxy proxy = proxyTransform.GetComponent<BuildingClickProxy>();
        if (proxy == null)
            proxy = proxyTransform.gameObject.AddComponent<BuildingClickProxy>();
        proxy.owner = this;

        BoxCollider collider = proxyTransform.GetComponent<BoxCollider>();
        if (collider == null)
            collider = proxyTransform.gameObject.AddComponent<BoxCollider>();

        bool applied = TryApplyRendererBoundsCollider(collider);
        if (!applied || forceRebuild)
            ApplyClickColliderPreset(collider);

        ClampColliderForType(collider);
        ApplyFootprintClamp(collider);

        if (useRootClickColliderOnly)
            DisableNestedModelColliders(collider);
    }

    private void RemoveRootBoxColliders()
    {
        BoxCollider[] rootColliders = GetComponents<BoxCollider>();
        for (int i = rootColliders.Length - 1; i >= 0; i--)
        {
            if (rootColliders[i] == null)
                continue;

            if (Application.isPlaying)
                Destroy(rootColliders[i]);
            else
                DestroyImmediate(rootColliders[i]);
        }
    }

    private Transform GetOrCreateClickProxyTransform()
    {
        Transform proxy = transform.Find(ClickProxyName);
        if (proxy != null)
            return proxy;

        GameObject proxyObject = new GameObject(ClickProxyName);
        proxyObject.transform.SetParent(transform, false);
        return proxyObject.transform;
    }

    private void ApplyFootprintClamp(BoxCollider collider)
    {
        if (collider == null)
            return;

        GameModeManager manager =
            GameModeManager.Instance != null
                ? GameModeManager.Instance
                : FindAnyObjectByType<GameModeManager>();

        float suggestedFootprint =
            manager != null && data != null
                ? manager.GetSuggestedBuildingFootprint(data.type)
                : 0.9f;

        float maxXZ = Mathf.Clamp(suggestedFootprint * 0.9f, 0.35f, 1.35f);
        float minXZ = Mathf.Clamp(maxXZ * 0.5f, 0.22f, 0.72f);

        Vector3 size = collider.size;
        size.x = Mathf.Clamp(size.x, minXZ, maxXZ);
        size.z = Mathf.Clamp(size.z, minXZ, maxXZ);
        size.y = Mathf.Clamp(size.y, 0.35f, 1.5f);
        collider.size = size;

        Vector3 center = collider.center;
        center.x = Mathf.Clamp(center.x, -0.20f, 0.20f);
        center.z = Mathf.Clamp(center.z, -0.20f, 0.20f);
        center.y = Mathf.Clamp(center.y, 0.15f, 0.85f);
        collider.center = center;
    }

    private void ApplyClickColliderPreset(BoxCollider collider)
    {
        if (collider == null)
            return;

        BuildingType type = data != null ? data.type : BuildingType.Headquarters;

        if (type == BuildingType.Headquarters)
        {
            collider.center = new Vector3(0f, 0.72f, 0f);
            collider.size = new Vector3(1.35f, 1.45f, 1.35f);
            return;
        }

        if (type == BuildingType.Barracks)
        {
            collider.center = new Vector3(0f, 0.18f, 0f);
            collider.size = new Vector3(0.92f, 0.36f, 0.72f);
            return;
        }

        if (type == BuildingType.ProductionFacility)
        {
            collider.center = new Vector3(0f, 0.52f, 0f);
            collider.size = new Vector3(0.86f, 1f, 0.86f);
            return;
        }

        collider.center = new Vector3(0f, 0.58f, 0f);
        collider.size = new Vector3(1f, 1.15f, 1f);
    }

    private bool ShouldResetExistingCollider(BoxCollider collider)
    {
        if (collider == null || data == null)
            return false;

        if (data.type != BuildingType.Headquarters)
            return false;

        bool tooLarge =
            collider.size.x > 2.2f ||
            collider.size.y > 2.4f ||
            collider.size.z > 2.2f;

        bool offCenter =
            Mathf.Abs(collider.center.x) > 1.2f ||
            Mathf.Abs(collider.center.z) > 1.2f ||
            collider.center.y < 0.2f ||
            collider.center.y > 2.6f;

        return tooLarge || offCenter;
    }

    private void ClampColliderForType(BoxCollider collider)
    {
        if (collider == null)
            return;

        BuildingType type = data != null ? data.type : BuildingType.Headquarters;

        float maxXZ = 1.2f;
        float maxY = 1.6f;
        float minXZ = 0.42f;
        float minY = 0.55f;

        if (type == BuildingType.Headquarters)
        {
            maxXZ = 1.8f;
            maxY = 1.9f;
            minXZ = 0.7f;
            minY = 0.8f;
        }
        else if (type == BuildingType.Barracks)
        {
            maxXZ = 1.05f;
            maxY = 0.72f;
            minXZ = 0.48f;
            minY = 0.25f;
        }
        else if (type == BuildingType.ProductionFacility)
        {
            maxXZ = 1.1f;
            maxY = 1.35f;
            minXZ = 0.52f;
            minY = 0.65f;
        }

        Vector3 size = collider.size;
        size.x = Mathf.Clamp(size.x, minXZ, maxXZ);
        size.y = Mathf.Clamp(size.y, minY, maxY);
        size.z = Mathf.Clamp(size.z, minXZ, maxXZ);
        collider.size = size;

        Vector3 center = collider.center;
        center.x = Mathf.Clamp(center.x, -0.35f, 0.35f);
        center.z = Mathf.Clamp(center.z, -0.35f, 0.35f);
        center.y = Mathf.Clamp(center.y, 0.18f, maxY * 0.7f);
        collider.center = center;
    }

    private bool TryApplyRendererBoundsCollider(BoxCollider collider)
    {
        Renderer[] renderers =
            modelRoot != null
                ? modelRoot.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
            return false;

        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (IsNonBuildingSurface(renderer.transform))
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

        Transform colliderTransform = collider.transform;
        Vector3 localCenter = colliderTransform.InverseTransformPoint(bounds.center);
        Vector3 scale = colliderTransform.lossyScale;
        Vector3 localSize = new Vector3(
            bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            bounds.size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z))
        );

        localSize.x = Mathf.Clamp(localSize.x * 1.08f, 0.9f, 4.2f);
        localSize.y = Mathf.Clamp(localSize.y * 1.12f, 0.8f, 3.2f);
        localSize.z = Mathf.Clamp(localSize.z * 1.08f, 0.9f, 4.2f);

        collider.center = localCenter;
        collider.size = localSize;
        return true;
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.one);

        Renderer[] renderers =
            modelRoot != null
                ? modelRoot.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
            return false;

        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (IsNonBuildingSurface(renderer.transform))
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

        return hasBounds;
    }

    private bool IsNonBuildingSurface(Transform target)
    {
        Transform current = target;
        while (current != null && current != transform)
        {
            string lowerName = current.name.ToLowerInvariant();
            if (lowerName.StartsWith("baseterrain_") ||
                lowerName.StartsWith("baseviewenvironment_") ||
                lowerName.Contains("ground") ||
                lowerName.Contains("floor") ||
                lowerName.Contains("plane") ||
                lowerName.Contains("pad"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void DisableNestedModelColliders(Collider rootCollider)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider == null || collider == rootCollider)
                continue;

            collider.enabled = false;
        }
    }
}

