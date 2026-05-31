#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BarracksAssetBuilder
{
    private const float TargetFootprint = 1.72f;
    private const float ColliderHeight = 1.15f;

    private const string SourceFolder =
        "Assets/Models/Buildings/Barracks/Lv1";

    private const string FbxPath =
        SourceFolder + "/Barracks_Lv1.fbx";

    private const string TextureFolder =
        SourceFolder + "/textures";

    private const string BaseColorPath =
        TextureFolder + "/barracklv1_basecolor.JPEG";

    private const string NormalPath =
        TextureFolder + "/barracklv1_normal.JPEG";

    private const string MetallicPath =
        TextureFolder + "/barracklv1_metallic.JPEG";

    private const string RoughnessPath =
        TextureFolder + "/barracklv1_roughness.JPEG";

    private const string MaterialFolder =
        "Assets/Materials/Buildings/Barracks";

    private const string DataFolder =
        "Assets/ScriptableObjects/Buildings";

    private const string PrefabFolder =
        "Assets/Prefabs/Buildings/Barracks";

    [MenuItem("Turan Strateji/Assets/Build Barracks Level 1")]
    public static void Build()
    {
        EnsureFolder(MaterialFolder);
        EnsureFolder(DataFolder);
        EnsureFolder(PrefabFolder);

        GameObject sourceModel =
            AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);

        if (sourceModel == null)
        {
            Debug.LogError("Kisla FBX bulunamadi: " + FbxPath);
            return;
        }

        ConfigureTextureImports();
        ConfigureModelImport();

        Material material = CreateMaterial();
        GameObject visualPrefab = CreateVisualPrefab(sourceModel, material);
        BuildingData buildingData = CreateBuildingData(visualPrefab);
        CreateBuildingPrefab(buildingData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Kisla Level 1 hazirlandi. Prefab: " + PrefabFolder + "/Barracks_Level1.prefab");
    }

    [InitializeOnLoadMethod]
    private static void AutoBuildWhenModelExists()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!File.Exists(FbxPath))
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Barracks_Level1.prefab") != null)
                return;

            Build();
        };
    }

    private static void ConfigureModelImport()
    {
        ModelImporter importer =
            AssetImporter.GetAtPath(FbxPath) as ModelImporter;

        if (importer == null)
            return;

        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab; // Fixed obsolete warning
        importer.materialSearch = ModelImporterMaterialSearch.Local;
importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.bakeAxisConversion = false;
        importer.globalScale = 1f;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static Material CreateMaterial()
    {
        string materialPath =
            MaterialFolder + "/Barracks_Level1_Mat.mat";

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }

        Texture2D baseColor =
            AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
        Texture2D normal =
            AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);

        material.SetTexture("_BaseMap", baseColor);
        material.SetTexture("_MainTex", baseColor);
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_Metallic", 0.04f);
        material.SetFloat("_Smoothness", 0.34f);
        material.SetFloat("_WorkflowMode", 1f);
        material.enableInstancing = true;

        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 0.55f);
            material.EnableKeyword("_NORMALMAP");
        }

        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        material.DisableKeyword("_EMISSION");

        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateVisualPrefab(GameObject sourceModel, Material material)
    {
        string visualPrefabPath =
            PrefabFolder + "/Barracks_Level1_Visual.prefab";

        GameObject root =
            new GameObject("Barracks_Level1_Visual");

        GameObject contentRoot =
            new GameObject("NormalizedModel");
        contentRoot.transform.SetParent(root.transform, false);

        GameObject instance =
            (GameObject)PrefabUtility.InstantiatePrefab(sourceModel);
        instance.transform.SetParent(contentRoot.transform, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = material;

        ApplyBestBuildingOrientation(contentRoot.transform);
        FitModelToFootprint(contentRoot.transform, TargetFootprint);
        CenterModelOnGround(contentRoot.transform);

        GameObject prefab =
            PrefabUtility.SaveAsPrefabAsset(root, visualPrefabPath);
        Object.DestroyImmediate(root);

        return prefab;
    }

    private static BuildingData CreateBuildingData(GameObject visualPrefab)
    {
        string dataPath =
            DataFolder + "/BD_Kisla.asset";

        BuildingData data =
            AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BuildingData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        data.buildingId = "kisla";
        data.displayName = "Kisla";
        data.type = BuildingType.Barracks;
        data.levels = new[]
        {
            new BuildingLevelData
            {
                level = 1,
                visualPrefab = visualPrefab,
                visualScale = Vector3.one,
                visualRotation = Vector3.zero,
                requiredHeadquartersLevel = 1,
                powerReward = 45,
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
                powerReward = 95,
                upgradeSeconds = 75,
                upgradeCost = new ResourceCost { steel = 150, oil = 45, bor = 4 }
            }
        };

        EditorUtility.SetDirty(data);
        return data;
    }

    private static void CreateBuildingPrefab(BuildingData data)
    {
        string prefabPath =
            PrefabFolder + "/Barracks_Level1.prefab";

        GameObject root =
            new GameObject("Barracks_Level1");

        BaseBuilding baseBuilding =
            root.AddComponent<BaseBuilding>();
        baseBuilding.data = data;
        baseBuilding.currentLevel = 1;

        BarracksBuilding barracks =
            root.AddComponent<BarracksBuilding>();

        BoxCollider collider =
            root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, ColliderHeight * 0.5f, 0f);
        collider.size = new Vector3(1.9f, ColliderHeight, 1.45f);

        GameObject modelRoot =
            new GameObject("Model");
        modelRoot.transform.SetParent(root.transform, false);

        GameObject spawnPoints =
            new GameObject("SpawnPoints");
        spawnPoints.transform.SetParent(root.transform, false);
        spawnPoints.transform.localPosition = new Vector3(0f, 0.05f, -0.42f);

        GameObject staging =
            new GameObject("BarracksUnitStaging");
        staging.transform.SetParent(root.transform, false);
        staging.transform.localPosition = new Vector3(0f, 0.08f, 0.24f);

        GameObject uiAnchor =
            new GameObject("UIAnchor");
        uiAnchor.transform.SetParent(root.transform, false);
        uiAnchor.transform.localPosition = new Vector3(0f, 1.35f, 0f);

        baseBuilding.modelRoot = modelRoot.transform;
        baseBuilding.spawnPointsRoot = spawnPoints.transform;
        baseBuilding.uiAnchor = uiAnchor.transform;
        barracks.baseUnitStagingAnchor = staging.transform;

        GameObject visual =
            (GameObject)PrefabUtility.InstantiatePrefab(data.GetLevelData(1).visualPrefab);
        visual.transform.SetParent(modelRoot.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
    }

    private static void ConfigureTextureImports()
    {
        ConfigureTexture(BaseColorPath, TextureImporterType.Default, true, 2048, false);
        ConfigureTexture(NormalPath, TextureImporterType.NormalMap, false, 2048, true);
        ConfigureTexture(MetallicPath, TextureImporterType.Default, false, 1024, false);
        ConfigureTexture(RoughnessPath, TextureImporterType.Default, false, 1024, false);
    }

    private static void ConfigureTexture(
        string path,
        TextureImporterType type,
        bool sRgb,
        int maxSize,
        bool normalMap)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = type;
        importer.sRGBTexture = sRgb;
        importer.maxTextureSize = maxSize;
        importer.mipmapEnabled = true;
        importer.anisoLevel = 4;
        importer.filterMode = FilterMode.Trilinear;
        importer.alphaIsTransparency = false;

        if (normalMap)
            importer.convertToNormalmap = false;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
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

        Vector3 bestEuler = Vector3.zero;
        float bestScore = float.MaxValue;

        foreach (Vector3 candidate in candidates)
        {
            contentRoot.localRotation = Quaternion.Euler(candidate);

            Bounds bounds;
            if (!TryGetRendererBounds(contentRoot, out bounds))
                continue;

            float horizontal = Mathf.Max(bounds.size.x, bounds.size.z);
            float vertical = Mathf.Max(0.01f, bounds.size.y);
            float score = vertical > horizontal ? 10f : 0f;
            score += Mathf.Abs((horizontal / vertical) - 2.8f);

            if (score < bestScore)
            {
                bestScore = score;
                bestEuler = candidate;
            }
        }

        contentRoot.localRotation = Quaternion.Euler(bestEuler);
    }

    private static void FitModelToFootprint(Transform contentRoot, float targetFootprint)
    {
        Bounds bounds;
        if (!TryGetRendererBounds(contentRoot, out bounds))
            return;

        float horizontal = Mathf.Max(bounds.size.x, bounds.size.z);
        if (horizontal <= 0.001f)
            return;

        float scale = targetFootprint / horizontal;
        contentRoot.localScale *= scale;
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
