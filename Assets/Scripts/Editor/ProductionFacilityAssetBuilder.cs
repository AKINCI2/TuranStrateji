#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProductionFacilityAssetBuilder
{
    private const float TargetFootprint = 5.2f;
    private const float TargetHeight = 3.6f;
    private const float SceneVisualScale = 9f;
    private static readonly Vector3 SourceUprightEuler = new Vector3(-90f, 0f, 0f);
    private static readonly Vector3 ColliderCenter = new Vector3(0f, 1.55f, 0f);
    private static readonly Vector3 ColliderSize = new Vector3(4.6f, 3.1f, 4.6f);

    private const string SourceFolder =
        "Assets/Models/Buildings/ProductionFacility/Level1/Meshy_AI_Domefront_Outpost_0520095140_texture_fbx";

    private const string FbxPath =
        SourceFolder + "/Meshy_AI_Domefront_Outpost_0520095140_texture.fbx";

    private const string TexturePath =
        SourceFolder + "/Meshy_AI_Domefront_Outpost_0520095140_texture.png";

    private const string NormalPath =
        SourceFolder + "/Meshy_AI_Domefront_Outpost_0520095140_texture_normal.png";

    private const string MetallicPath =
        SourceFolder + "/Meshy_AI_Domefront_Outpost_0520095140_texture_metallic.png";

    private const string EmissionPath =
        SourceFolder + "/Meshy_AI_Domefront_Outpost_0520095140_texture_emission.png";

    private const string MaterialFolder =
        "Assets/Materials/Buildings/ProductionFacility";

    private const string DataFolder =
        "Assets/ScriptableObjects/Buildings";

    private const string PrefabFolder =
        "Assets/Prefabs/Buildings/ProductionFacility";

    [MenuItem("Turan Strateji/Assets/Build Production Facility Level 1")]
    public static void Build()
    {
        EnsureFolder(MaterialFolder);
        EnsureFolder(DataFolder);
        EnsureFolder(PrefabFolder);

        GameObject sourceModel =
            AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);

        if (sourceModel == null)
        {
            Debug.LogError("Uretim tesisi FBX bulunamadi: " + FbxPath);
            return;
        }

        ConfigureTextureImports();

        Material material = CreateMaterial();
        GameObject visualPrefab =
            CreateVisualPrefab(sourceModel, material);

        BuildingData buildingData =
            CreateBuildingData(visualPrefab);

        CreateBuildingPrefab(buildingData);
        FixProductionFacilityInstances(false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Uretim Tesisi Level 1 hazirlandi. Prefab: " + PrefabFolder + "/UretimTesisi_Level1.prefab");
    }

    [DidReloadScripts]
    private static void NormalizeOpenSceneProductionFacilities()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            FixProductionFacilityInstances(false);
        };
    }

    [MenuItem("Turan Strateji/Assets/Fix Production Facility Instances")]
    public static void FixProductionFacilityInstances()
    {
        FixProductionFacilityInstances(true);
    }

    private static void FixProductionFacilityInstances(bool saveAssets)
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/UretimTesisi_Level1.prefab");

        if (prefab != null)
        {
            NormalizeProductionRoot(prefab.transform);
            EditorUtility.SetDirty(prefab);
        }

        ProductionFacilityBuilding[] facilities =
            Object.FindObjectsByType<ProductionFacilityBuilding>(FindObjectsInactive.Include);

        foreach (ProductionFacilityBuilding facility in facilities)
        {
            if (facility == null)
                continue;

            NormalizeProductionRoot(facility.transform);
            BaseBuilding baseBuilding = facility.GetComponent<BaseBuilding>();
            if (baseBuilding != null)
                baseBuilding.RefreshVisual();

            EditorUtility.SetDirty(facility.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (saveAssets)
            AssetDatabase.SaveAssets();

        Debug.Log("Uretim tesisi prefab ve sahne instance ayarlari sifirlandi.");
    }

    private static Material CreateMaterial()
    {
        string materialPath =
            MaterialFolder + "/UretimTesisi_Level1_Mat.mat";

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }

        Texture2D mainTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        Texture2D normalTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        Texture2D metallicTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicPath);
        Texture2D emissionTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionPath);

        material.SetTexture("_BaseMap", mainTexture);
        material.SetTexture("_BumpMap", normalTexture);
        material.SetTexture("_MetallicGlossMap", metallicTexture);
        material.SetTexture("_EmissionMap", emissionTexture);
        material.SetFloat("_Metallic", 0.12f);
        material.SetFloat("_Smoothness", 0.58f);
        material.enableInstancing = true;

        if (normalTexture != null)
            material.EnableKeyword("_NORMALMAP");

        if (metallicTexture != null)
            material.EnableKeyword("_METALLICSPECGLOSSMAP");

        if (emissionTexture != null)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.03f, 0.12f, 0.14f));
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateVisualPrefab(GameObject sourceModel, Material material)
    {
        string visualPrefabPath =
            PrefabFolder + "/UretimTesisi_Level1_Visual.prefab";

        GameObject root =
            new GameObject("UretimTesisi_Level1_Visual");

        GameObject contentRoot =
            new GameObject("NormalizedModel");
        contentRoot.transform.SetParent(root.transform, false);

        GameObject instance =
            (GameObject)PrefabUtility.InstantiatePrefab(sourceModel);
        instance.transform.SetParent(contentRoot.transform, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(SourceUprightEuler);
        instance.transform.localScale = Vector3.one * SceneVisualScale;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = material;

        CenterModelOnGround(contentRoot.transform);

        GameObject prefab =
            PrefabUtility.SaveAsPrefabAsset(root, visualPrefabPath);
        Object.DestroyImmediate(root);

        return prefab;
    }

    private static void ConfigureTextureImports()
    {
        ConfigureTexture(TexturePath, TextureImporterType.Default, 4096, false);
        ConfigureTexture(MetallicPath, TextureImporterType.Default, 2048, false);
        ConfigureTexture(EmissionPath, TextureImporterType.Default, 1024, false);
        ConfigureTexture(NormalPath, TextureImporterType.NormalMap, 4096, true);
    }

    private static void ConfigureTexture(string path, TextureImporterType type, int maxSize, bool normalMap)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = type;
        importer.maxTextureSize = maxSize;
        importer.mipmapEnabled = true;
        importer.anisoLevel = 8;
        importer.filterMode = FilterMode.Trilinear;
        importer.alphaIsTransparency = false;

        if (normalMap)
            importer.convertToNormalmap = false;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static void CenterModelOnGround(Transform contentRoot)
    {
        Bounds bounds;
        if (!TryGetRendererBounds(contentRoot, out bounds))
            return;

        Vector3 offset =
            new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

        contentRoot.position += offset;
    }

    private static void ApplyBestBuildingOrientation(Transform contentRoot)
    {
        Vector3[] candidates =
        {
            Vector3.zero,
            new Vector3(90f, 0f, 0f),
            new Vector3(-90f, 0f, 0f),
            new Vector3(0f, 0f, 90f),
            new Vector3(0f, 0f, -90f),
            new Vector3(180f, 0f, 0f)
        };

        float targetRatio =
            TargetFootprint / TargetHeight;

        Vector3 bestEuler =
            Vector3.zero;
        float bestScore =
            float.MaxValue;

        foreach (Vector3 candidate in candidates)
        {
            contentRoot.localRotation =
                Quaternion.Euler(candidate);

            Bounds bounds;
            if (!TryGetRendererBounds(contentRoot, out bounds))
                continue;

            float horizontal =
                Mathf.Max(bounds.size.x, bounds.size.z);
            float vertical =
                Mathf.Max(0.01f, bounds.size.y);
            float ratio =
                horizontal / vertical;

            float score =
                Mathf.Abs(ratio - targetRatio);

            if (vertical > horizontal)
                score += 2f;

            if (score < bestScore)
            {
                bestScore = score;
                bestEuler = candidate;
            }
        }

        contentRoot.localRotation =
            Quaternion.Euler(bestEuler);
    }

    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        bounds = new Bounds(root.position, Vector3.zero);

        bool hasRenderer = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!hasRenderer)
            {
                bounds = renderer.bounds;
                hasRenderer = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasRenderer;
    }

    private static BuildingData CreateBuildingData(GameObject visualPrefab)
    {
        string dataPath =
            DataFolder + "/BD_UretimTesisi.asset";

        BuildingData data =
            AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BuildingData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        data.buildingId = "uretim_tesisi";
        data.displayName = "Uretim Tesisi";
        data.type = BuildingType.ProductionFacility;
        data.levels = new[]
        {
            new BuildingLevelData
            {
                level = 1,
                visualPrefab = visualPrefab,
                visualScale = Vector3.one,
                visualRotation = Vector3.zero,
                requiredHeadquartersLevel = 1,
                powerReward = 60,
                upgradeSeconds = 0,
                upgradeCost = new ResourceCost()
            },
            new BuildingLevelData
            {
                level = 2,
                visualPrefab = visualPrefab,
                visualScale = Vector3.one,
                visualRotation = Vector3.zero,
                requiredHeadquartersLevel = 2,
                powerReward = 120,
                upgradeSeconds = 90,
                upgradeCost = new ResourceCost { steel = 180, oil = 60, bor = 8 }
            }
        };

        EditorUtility.SetDirty(data);
        return data;
    }

    private static void CreateBuildingPrefab(BuildingData data)
    {
        string prefabPath =
            PrefabFolder + "/UretimTesisi_Level1.prefab";

        GameObject root =
            new GameObject("UretimTesisi_Level1");

        BaseBuilding baseBuilding =
            root.AddComponent<BaseBuilding>();
        baseBuilding.data = data;
        baseBuilding.currentLevel = 1;

        root.AddComponent<ProductionFacilityBuilding>();

        BoxCollider collider =
            root.AddComponent<BoxCollider>();
        collider.center = ColliderCenter;
        collider.size = ColliderSize;

        GameObject modelRoot =
            new GameObject("Model");
        modelRoot.transform.SetParent(root.transform, false);

        GameObject spawnPoints =
            new GameObject("SpawnPoints");
        spawnPoints.transform.SetParent(root.transform, false);
        spawnPoints.transform.localPosition = new Vector3(0f, 0f, -1.55f);

        GameObject uiAnchor =
            new GameObject("UIAnchor");
        uiAnchor.transform.SetParent(root.transform, false);
        uiAnchor.transform.localPosition = new Vector3(0f, 2.8f, 0f);

        baseBuilding.modelRoot = modelRoot.transform;
        baseBuilding.spawnPointsRoot = spawnPoints.transform;
        baseBuilding.uiAnchor = uiAnchor.transform;

        GameObject visual =
            (GameObject)PrefabUtility.InstantiatePrefab(data.GetLevelData(1).visualPrefab);
        visual.transform.SetParent(modelRoot.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
    }

    private static void NormalizeProductionRoot(Transform root)
    {
        if (root == null)
            return;

        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        Transform model = root.Find("Model");
        if (model != null)
        {
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            model.localScale = Vector3.one;
        }

        BoxCollider collider = root.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.center = ColliderCenter;
            collider.size = ColliderSize;
            EditorUtility.SetDirty(collider);
        }

        Transform spawnPoints = root.Find("SpawnPoints");
        if (spawnPoints != null)
            spawnPoints.localPosition = new Vector3(0f, 0f, -1.55f);

        Transform uiAnchor = root.Find("UIAnchor");
        if (uiAnchor != null)
            uiAnchor.localPosition = new Vector3(0f, 2.8f, 0f);

        BaseBuilding baseBuilding = root.GetComponent<BaseBuilding>();
        if (baseBuilding == null || baseBuilding.data == null || baseBuilding.data.levels == null)
            return;

        foreach (BuildingLevelData level in baseBuilding.data.levels)
        {
            if (level == null)
                continue;

            level.visualScale = Vector3.one;
            level.visualRotation = Vector3.zero;
        }

        EditorUtility.SetDirty(baseBuilding.data);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent =
            Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder =
            Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif

