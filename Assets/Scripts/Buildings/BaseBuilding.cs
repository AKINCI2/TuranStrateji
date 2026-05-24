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

    [Header("Runtime")]
    public bool isUpgrading;
    public float upgradeRemainingSeconds;

    private GameObject activeVisual;

    public BuildingLevelData CurrentLevelData =>
        data != null ? data.GetLevelData(currentLevel) : null;

    public BuildingLevelData NextLevelData =>
        data != null ? data.GetNextLevelData(currentLevel) : null;

    void Awake()
    {
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

        EnsureStructure();
        RefreshVisual();
        RebuildClickableCollider();
    }
#endif

    public void RebuildClickableCollider()
    {
        EnsureClickableCollider(autoFitClickCollider);
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

        if (levelData == null || levelData.visualPrefab == null)
            return;

        ClearModelRoot();
        activeVisual = null;

        activeVisual =
            Instantiate(levelData.visualPrefab, modelRoot);

        activeVisual.name =
            levelData.visualPrefab.name;
        activeVisual.transform.localPosition = Vector3.zero;
        activeVisual.transform.localRotation = Quaternion.Euler(levelData.visualRotation);
        activeVisual.transform.localScale = levelData.visualScale;
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
        if (data != null)
            forceRebuild = true;

        if (!forceRebuild)
        {
            BoxCollider existingRootCollider = GetComponent<BoxCollider>();
            if (existingRootCollider != null)
            {
                if (ShouldResetExistingCollider(existingRootCollider))
                    ApplyClickColliderPreset(existingRootCollider);

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
        ApplyClickColliderPreset(collider);

        if (useRootClickColliderOnly)
            DisableNestedModelColliders(collider);
    }

    private void ApplyClickColliderPreset(BoxCollider collider)
    {
        if (collider == null)
            return;

        BuildingType type = data != null ? data.type : BuildingType.Headquarters;

        if (type == BuildingType.Headquarters)
        {
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.size = new Vector3(2.65f, 1.75f, 2.65f);
            return;
        }

        if (type == BuildingType.Barracks)
        {
            collider.center = new Vector3(0f, 0.65f, 0f);
            collider.size = new Vector3(1.35f, 1.25f, 1.35f);
            return;
        }

        if (type == BuildingType.ProductionFacility)
        {
            collider.center = new Vector3(0f, 0.7f, 0f);
            collider.size = new Vector3(1.45f, 1.35f, 1.45f);
            return;
        }

        collider.center = new Vector3(0f, 0.8f, 0f);
        collider.size = new Vector3(2.2f, 1.6f, 2.2f);
    }

    private bool ShouldResetExistingCollider(BoxCollider collider)
    {
        if (collider == null || data == null)
            return false;

        if (data.type != BuildingType.Headquarters)
            return false;

        bool tooLarge =
            collider.size.x > 4.8f ||
            collider.size.y > 3.8f ||
            collider.size.z > 4.8f;

        bool offCenter =
            Mathf.Abs(collider.center.x) > 1.2f ||
            Mathf.Abs(collider.center.z) > 1.2f ||
            collider.center.y < 0.2f ||
            collider.center.y > 2.6f;

        return tooLarge || offCenter;
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

        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        Vector3 scale = transform.lossyScale;
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

