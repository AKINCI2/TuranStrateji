using UnityEngine;

public class BaseViewEnvironment : MonoBehaviour
{
    [Header("Layout")]
    public Vector2 terrainSize = new Vector2(78f, 78f);
    public Vector2 padSize = new Vector2(15.5f, 11.5f);
    public bool createBaseFoundation = true;
    public bool createTerrainBackdrop;
    public bool createPlaceholderBaseSurfaces;
    public bool hideLegacyFlatBaseSurfaces = true;

    [Header("Colors")]
    public Color grassColor = new Color(0.31f, 0.43f, 0.27f, 1f);
    public Color grassVariationColor = new Color(0.22f, 0.34f, 0.21f, 1f);
    public Color padColor = new Color(0.31f, 0.34f, 0.34f, 1f);
    public Color roadColor = new Color(0.43f, 0.38f, 0.28f, 1f);
    public Color foundationColor = new Color(0.17f, 0.20f, 0.21f, 1f);
    public Color foundationAccentColor = new Color(0.32f, 0.29f, 0.22f, 1f);
    public Color wallColor = new Color(0.34f, 0.36f, 0.35f, 1f);
    public Color towerColor = new Color(0.40f, 0.38f, 0.32f, 1f);

    private const string RootName = "BaseViewEnvironment_Runtime";

    void Awake()
    {
        ApplyWorldContext();
        Rebuild();
    }

    public void ApplyWorldContext()
    {
        HexCell markerHex = GetWorldBaseHex();
        if (markerHex == null)
            return;

        if (markerHex.terrainType == HexTerrainType.Forest)
        {
            grassColor = new Color(0.22f, 0.39f, 0.21f, 1f);
            grassVariationColor = new Color(0.14f, 0.29f, 0.16f, 1f);
            return;
        }

        bool nearWater = false;
        if (markerHex.neighbors != null)
        {
            foreach (HexCell neighbor in markerHex.neighbors)
            {
                if (neighbor != null && neighbor.terrainType == HexTerrainType.Water)
                {
                    nearWater = true;
                    break;
                }
            }
        }

        if (nearWater)
        {
            grassColor = new Color(0.25f, 0.42f, 0.27f, 1f);
            grassVariationColor = new Color(0.17f, 0.32f, 0.22f, 1f);
            return;
        }

        if (markerHex.terrainType == HexTerrainType.Road || markerHex.terrainType == HexTerrainType.City)
        {
            grassColor = new Color(0.30f, 0.41f, 0.23f, 1f);
            grassVariationColor = new Color(0.23f, 0.32f, 0.18f, 1f);
            return;
        }

        grassColor = new Color(0.27f, 0.43f, 0.24f, 1f);
        grassVariationColor = new Color(0.18f, 0.33f, 0.18f, 1f);
    }

    public void Rebuild()
    {
        createTerrainBackdrop = false;
        createPlaceholderBaseSurfaces = false;

        ClearRuntimeRoot();

        if (hideLegacyFlatBaseSurfaces)
            HideLegacySurfaces();

        GameObject root = new GameObject(RootName);
        root.transform.SetParent(transform, false);

        if (createBaseFoundation)
            CreateBaseFoundation(root.transform);

        if (createTerrainBackdrop)
            CreateQuad(root.transform, "BaseTerrain_Grass", terrainSize, Vector3.zero, -0.12f, grassColor, grassVariationColor, null, true);

        if (createPlaceholderBaseSurfaces)
        {
            CreateQuad(root.transform, "BaseTerrain_ServicePad", padSize, Vector3.zero, -0.045f, padColor, new Color(0.22f, 0.25f, 0.25f, 1f));
            CreateQuad(root.transform, "BaseTerrain_CommandPad", new Vector2(6.4f, 5.0f), new Vector3(0f, 0f, 0.15f), -0.025f, new Color(0.38f, 0.40f, 0.39f, 1f), new Color(0.24f, 0.27f, 0.27f, 1f));
        }

        CreateClickBlocker(root.transform);
    }

    private void CreateClickBlocker(Transform parent)
    {
        GameObject blocker = new GameObject("BaseViewEnvironment_ClickBlocker", typeof(BoxCollider));
        blocker.transform.SetParent(parent, false);
        blocker.transform.localPosition = new Vector3(0f, -0.16f, 0f);

        BoxCollider collider = blocker.GetComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = new Vector3(90f, 0.08f, 90f);
    }

    private void CreateBaseFoundation(Transform parent)
    {
        float size = GetCurrentFoundationSize();
        float half = size * 0.5f;
        float wallThickness = Mathf.Clamp(size * 0.055f, 0.28f, 0.48f);
        float wallHeight = Mathf.Clamp(size * 0.105f, 0.72f, 1.35f);
        float towerRadius = Mathf.Clamp(size * 0.092f, 0.62f, 1.35f);
        float towerHeight = wallHeight * 1.45f;

        CreateQuad(
            parent,
            "BasePlace_Foundation",
            new Vector2(size, size),
            Vector3.zero,
            -0.065f,
            foundationColor,
            foundationAccentColor
        );

        float wallOffset = half + wallThickness * 0.5f;
        CreateBox(parent, "BaseWall_North", new Vector3(size + wallThickness * 2f, wallHeight, wallThickness), new Vector3(0f, wallHeight * 0.5f, wallOffset), wallColor, foundationAccentColor);
        CreateBox(parent, "BaseWall_South", new Vector3(size + wallThickness * 2f, wallHeight, wallThickness), new Vector3(0f, wallHeight * 0.5f, -wallOffset), wallColor, foundationAccentColor);
        CreateBox(parent, "BaseWall_East", new Vector3(wallThickness, wallHeight, size), new Vector3(wallOffset, wallHeight * 0.5f, 0f), wallColor, foundationAccentColor);
        CreateBox(parent, "BaseWall_West", new Vector3(wallThickness, wallHeight, size), new Vector3(-wallOffset, wallHeight * 0.5f, 0f), wallColor, foundationAccentColor);

        CreateOctagonalTower(parent, "WatchTower_NE", new Vector3(wallOffset, towerHeight * 0.5f, wallOffset), towerRadius, towerHeight);
        CreateOctagonalTower(parent, "WatchTower_NW", new Vector3(-wallOffset, towerHeight * 0.5f, wallOffset), towerRadius, towerHeight);
        CreateOctagonalTower(parent, "WatchTower_SE", new Vector3(wallOffset, towerHeight * 0.5f, -wallOffset), towerRadius, towerHeight);
        CreateOctagonalTower(parent, "WatchTower_SW", new Vector3(-wallOffset, towerHeight * 0.5f, -wallOffset), towerRadius, towerHeight);
    }

    private float GetCurrentFoundationSize()
    {
        GameModeManager manager = GameModeManager.Instance != null
            ? GameModeManager.Instance
            : FindAnyObjectByType<GameModeManager>();

        if (manager != null)
            return manager.GetCurrentBaseFootprintSize();

        return 8.8f;
    }

    private HexCell GetWorldBaseHex()
    {
        WorldBaseMarker marker = WorldBaseMarker.FindPrimary(true);
        if (marker == null)
            return null;

        HexGridManager[] grids = FindObjectsByType<HexGridManager>(FindObjectsInactive.Include);
        if (grids == null || grids.Length == 0 || grids[0] == null)
            return null;

        return grids[0].GetClosestHex(marker.transform.position);
    }

    private void ClearRuntimeRoot()
    {
        Transform existing = transform.Find(RootName);
        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    private void HideLegacySurfaces()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || child.name == RootName)
                continue;

            if (child.GetComponent<BaseBuilding>() != null ||
                child.GetComponentInChildren<BaseBuilding>(true) != null ||
                child.GetComponent<UnitController>() != null ||
                child.GetComponentInChildren<UnitController>(true) != null ||
                child.GetComponent<BuildingSlot>() != null ||
                child.GetComponentInChildren<BuildingSlot>(true) != null)
            {
                continue;
            }

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Bounds bounds = renderer.bounds;
                bool looksLikeBaseSurface =
                    bounds.size.y < 0.35f &&
                    bounds.size.x > 1.8f &&
                    bounds.size.z > 1.8f;

                string lowerName = renderer.name.ToLowerInvariant();
                bool namedLikeSurface =
                    lowerName.Contains("usici") ||
                    lowerName.Contains("us ici") ||
                    lowerName.Contains("baseground") ||
                    lowerName.Contains("ground") ||
                    lowerName.Contains("plane") ||
                    lowerName.Contains("pad") ||
                    lowerName.Contains("floor");

                if (looksLikeBaseSurface || namedLikeSurface)
                {
                    renderer.enabled = false;
                    Collider collider = renderer.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = false;
                }
            }
        }
    }

    private void CreateQuad(
        Transform parent,
        string objectName,
        Vector2 size,
        Vector3 localPosition,
        float y,
        Color baseColor,
        Color variationColor,
        Quaternion? rotation = null,
        bool addGroundCollider = false)
    {
        GameObject obj = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(localPosition.x, y, localPosition.z);
        obj.transform.localRotation = rotation ?? Quaternion.identity;

        Mesh mesh = new Mesh();
        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;
        mesh.vertices = new[]
        {
            new Vector3(-halfX, 0f, -halfZ),
            new Vector3(-halfX, 0f, halfZ),
            new Vector3(halfX, 0f, halfZ),
            new Vector3(halfX, 0f, -halfZ)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        mesh.RecalculateNormals();

        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(objectName, baseColor, variationColor);

        if (addGroundCollider)
        {
            BoxCollider collider = obj.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(size.x, 0.04f, size.y);
        }
    }

    private void CreateBox(
        Transform parent,
        string objectName,
        Vector3 size,
        Vector3 localPosition,
        Color baseColor,
        Color variationColor)
    {
        GameObject obj = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;

        float x = size.x * 0.5f;
        float y = size.y * 0.5f;
        float z = size.z * 0.5f;

        Mesh mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, -y, z), new Vector3(-x, -y, z),
            new Vector3(-x, y, -z), new Vector3(x, y, -z), new Vector3(x, y, z), new Vector3(-x, y, z)
        };

        mesh.triangles = new[]
        {
            0, 4, 5, 0, 5, 1,
            1, 5, 6, 1, 6, 2,
            2, 6, 7, 2, 7, 3,
            3, 7, 4, 3, 4, 0,
            4, 7, 6, 4, 6, 5,
            0, 1, 2, 0, 2, 3
        };

        mesh.RecalculateNormals();
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(objectName, baseColor, variationColor);
    }

    private void CreateOctagonalTower(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        float radius,
        float height)
    {
        const int sideCount = 8;
        GameObject obj = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;

        Vector3[] vertices = new Vector3[sideCount * 2 + 2];
        int bottomCenter = sideCount * 2;
        int topCenter = bottomCenter + 1;

        float halfHeight = height * 0.5f;
        for (int i = 0; i < sideCount; i++)
        {
            float angle = Mathf.PI * 2f * i / sideCount + Mathf.PI / sideCount;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices[i] = new Vector3(x, -halfHeight, z);
            vertices[i + sideCount] = new Vector3(x, halfHeight, z);
        }

        vertices[bottomCenter] = new Vector3(0f, -halfHeight, 0f);
        vertices[topCenter] = new Vector3(0f, halfHeight, 0f);

        int[] triangles = new int[sideCount * 12];
        int index = 0;
        for (int i = 0; i < sideCount; i++)
        {
            int next = (i + 1) % sideCount;
            int bottomA = i;
            int bottomB = next;
            int topA = i + sideCount;
            int topB = next + sideCount;

            triangles[index++] = bottomA;
            triangles[index++] = topA;
            triangles[index++] = topB;
            triangles[index++] = bottomA;
            triangles[index++] = topB;
            triangles[index++] = bottomB;

            triangles[index++] = topCenter;
            triangles[index++] = topA;
            triangles[index++] = topB;

            triangles[index++] = bottomCenter;
            triangles[index++] = bottomB;
            triangles[index++] = bottomA;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(objectName, towerColor, foundationAccentColor);
    }

    private Material CreateMaterial(string materialName, Color baseColor, Color variationColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = materialName + "_Mat";
        material.color = baseColor;

        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, true);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.17f, y * 0.17f);
                Color color = Color.Lerp(baseColor, variationColor, noise * 0.42f);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        material.mainTexture = texture;
        return material;
    }
}
