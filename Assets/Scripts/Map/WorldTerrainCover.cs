using System.Collections.Generic;
using UnityEngine;

public class WorldTerrainCover : MonoBehaviour
{
    [Header("Look")]
    public Color grassColor = new Color(0.32f, 0.47f, 0.27f, 1f);
    public Color grassDarkColor = new Color(0.22f, 0.34f, 0.20f, 1f);
    public Color waterColor = new Color(0.12f, 0.34f, 0.50f, 0.92f);
    public Color roadColor = new Color(0.47f, 0.39f, 0.25f, 0.78f);
    public Color forestColor = new Color(0.13f, 0.24f, 0.12f, 0.55f);
    public Color mountainColor = new Color(0.31f, 0.30f, 0.27f, 0.48f);
    public Color shorelineColor = new Color(0.48f, 0.42f, 0.28f, 0.34f);
    public Color treeTrunkColor = new Color(0.22f, 0.15f, 0.09f, 1f);
    public Color treeCanopyColor = new Color(0.10f, 0.24f, 0.10f, 1f);
    public Color rockPropColor = new Color(0.34f, 0.33f, 0.30f, 1f);
    public int textureSize = 256;
    public float groundTextureMeters = 7f;

    [Header("Scale")]
    public float surfaceY = 0.035f;
    public float featureY = 0.052f;
    public float padding = 4f;
    public float waterWidth = 2.4f;
    public float roadWidth = 0.55f;
    public int maxForestPropCells = 120;
    public int maxMountainPropCells = 70;

    [Header("Prefab Pipeline")]
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;
    public GameObject[] shorelineDetailPrefabs;
    public GameObject[] roadDetailPrefabs;
    public float treePrefabScale = 1f;
    public float rockPrefabScale = 1f;
    public float shorelinePrefabScale = 1f;
    public float roadPrefabScale = 1f;

    private HexGridManager grid;
    private bool built;

    public void Build(HexGridManager sourceGrid)
    {
        grid = sourceGrid;
        if (grid == null || grid.allHexCells == null || grid.allHexCells.Count == 0)
            return;

        Clear();
        CreateGroundSurface();
        CreateWaterSurface();
        CreateShorelineTint();
        CreateRoadSurfaces();
        CreateTerrainDetailTint(HexTerrainType.Forest, forestColor, 0.62f);
        CreateTerrainDetailTint(HexTerrainType.Mountain, mountainColor, 0.52f);
        CreateForestProps();
        CreateMountainProps();
        CreateRoadDetails();
        built = true;
    }

    void Start()
    {
        if (!built)
            Build(GetComponent<HexGridManager>());
    }

    private void Clear()
    {
        List<GameObject> oldChildren = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("TerrainCover_"))
                oldChildren.Add(child.gameObject);
        }

        foreach (GameObject child in oldChildren)
        {
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void CreateGroundSurface()
    {
        Bounds bounds = GetGridBounds();
        bounds.Expand(padding * 2f);

        Mesh mesh = new Mesh();
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        mesh.vertices = new Vector3[]
        {
            new Vector3(min.x, surfaceY, min.z),
            new Vector3(max.x, surfaceY, min.z),
            new Vector3(max.x, surfaceY, max.z),
            new Vector3(min.x, surfaceY, max.z)
        };
        float uvX = Mathf.Max(1f, bounds.size.x / groundTextureMeters);
        float uvZ = Mathf.Max(1f, bounds.size.z / groundTextureMeters);

        mesh.uv = new Vector2[]
        {
            Vector2.zero,
            new Vector2(uvX, 0f),
            new Vector2(uvX, uvZ),
            new Vector2(0f, uvZ)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();

        Texture2D grassTexture = CreateNoiseTexture(
            "World Grass Texture",
            grassColor,
            grassDarkColor,
            0.34f,
            7.5f
        );

        GameObject surface = CreateMeshObject(
            "TerrainCover_Grass",
            mesh,
            CreateMaterial("World Grass", grassColor, grassTexture)
        );

        AddSoftTint(surface, bounds);
    }

    private void AddSoftTint(GameObject surface, Bounds bounds)
    {
        MeshFilter meshFilter = surface.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Mesh mesh = meshFilter.sharedMesh;
        Color[] colors = new Color[mesh.vertexCount];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = i % 2 == 0 ? grassColor : Color.Lerp(grassColor, grassDarkColor, 0.22f);

        mesh.colors = colors;
    }

    private void CreateWaterSurface()
    {
        List<HexCell> waterCells = GetCells(HexTerrainType.Water);
        if (waterCells.Count < 2)
            return;

        waterCells.Sort((a, b) => a.axialCoord.y.CompareTo(b.axialCoord.y));

        List<Vector3> centers = new List<Vector3>();
        int currentRow = int.MinValue;
        Vector3 rowSum = Vector3.zero;
        int rowCount = 0;

        foreach (HexCell cell in waterCells)
        {
            if (currentRow != int.MinValue && cell.axialCoord.y != currentRow)
            {
                centers.Add(rowSum / Mathf.Max(1, rowCount));
                rowSum = Vector3.zero;
                rowCount = 0;
            }

            currentRow = cell.axialCoord.y;
            rowSum += cell.transform.position;
            rowCount++;
        }

        if (rowCount > 0)
            centers.Add(rowSum / rowCount);

        CreateRibbon("TerrainCover_Water", centers, waterWidth, waterColor, featureY, CreateNoiseTexture(
            "World Water Texture",
            waterColor,
            new Color(0.06f, 0.23f, 0.36f, waterColor.a),
            0.28f,
            11f
        ));
    }

    private void CreateRoadSurfaces()
    {
        List<HexCell> roads = GetCells(HexTerrainType.Road);
        if (roads.Count == 0)
            return;

        List<List<Vector3>> groups = new List<List<Vector3>>();
        groups.Add(new List<Vector3>());
        groups.Add(new List<Vector3>());
        groups.Add(new List<Vector3>());

        foreach (HexCell cell in roads)
        {
            float normalizedX = cell.axialCoord.x / Mathf.Max(1f, grid.width);
            int group =
                normalizedX < 0.35f ? 0 :
                normalizedX > 0.64f ? 2 :
                1;

            groups[group].Add(cell.transform.position);
        }

        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].Count < 2)
                continue;

            groups[i].Sort((a, b) => a.z.CompareTo(b.z));
            CreateRibbon("TerrainCover_Road_" + i, groups[i], roadWidth, roadColor, featureY + 0.01f, CreateNoiseTexture(
                "World Road Texture",
                roadColor,
                new Color(0.34f, 0.28f, 0.18f, roadColor.a),
                0.42f,
                9f
            ));
        }
    }

    private void CreateShorelineTint()
    {
        foreach (HexCell cell in grid.allHexCells)
        {
            if (cell == null || cell.terrainType == HexTerrainType.Water)
                continue;

            bool touchesWater = false;
            foreach (HexCell neighbor in cell.neighbors)
            {
                if (neighbor != null && neighbor.terrainType == HexTerrainType.Water)
                {
                    touchesWater = true;
                    break;
                }
            }

            if (!touchesWater)
                continue;

            Mesh mesh = CreateIrregularPatchMesh(grid.size * 0.72f, 10, featureY + 0.024f, cell.axialCoord.x * 71 + cell.axialCoord.y * 191);
            GameObject obj = CreateMeshObject(
                "TerrainCover_Shore_" + cell.axialCoord.x + "_" + cell.axialCoord.y,
                mesh,
                CreateMaterial("Shoreline", shorelineColor)
            );
            obj.transform.SetParent(transform, false);
            obj.transform.position = new Vector3(cell.transform.position.x, 0f, cell.transform.position.z);
            obj.transform.localRotation = Quaternion.Euler(0f, (cell.axialCoord.x * 19f + cell.axialCoord.y * 31f) % 360f, 0f);
            TryPlacePrefab(
                shorelineDetailPrefabs,
                cell.transform.position,
                shorelinePrefabScale,
                cell.axialCoord.x * 43 + cell.axialCoord.y * 97,
                "TerrainCover_ShoreDetail"
            );
        }
    }

    private void CreateTerrainDetailTint(HexTerrainType type, Color color, float scale)
    {
        foreach (HexCell cell in GetCells(type))
        {
            Mesh mesh = CreateIrregularPatchMesh(grid.size * scale, 9, featureY + 0.018f, cell.axialCoord.x * 37 + cell.axialCoord.y * 113);
            GameObject obj = CreateMeshObject("TerrainCover_" + type + "_" + cell.axialCoord.x + "_" + cell.axialCoord.y, mesh, CreateMaterial(type.ToString(), color));
            obj.transform.SetParent(transform, false);
            obj.transform.position = new Vector3(cell.transform.position.x, 0f, cell.transform.position.z);
            obj.transform.localRotation = Quaternion.Euler(0f, (cell.axialCoord.x * 23f + cell.axialCoord.y * 11f) % 360f, 0f);
        }
    }

    private void CreateForestProps()
    {
        Material trunkMaterial = CreateMaterial("Tree Trunk", treeTrunkColor);
        Material canopyMaterial = CreateMaterial("Tree Canopy", treeCanopyColor);
        int created = 0;

        foreach (HexCell cell in GetCells(HexTerrainType.Forest))
        {
            if (created >= maxForestPropCells)
                break;

            if (!ShouldPlaceDetail(cell, 0.55f))
                continue;

            GameObject cluster = new GameObject("TerrainCover_TreeCluster_" + cell.axialCoord.x + "_" + cell.axialCoord.y);
            cluster.transform.SetParent(transform, false);
            cluster.transform.position = cell.transform.position;

            int count = 2 + Mathf.Abs(cell.axialCoord.x + cell.axialCoord.y) % 3;
            for (int i = 0; i < count; i++)
            {
                float angle = (i * 137.5f + cell.axialCoord.x * 17f) * Mathf.Deg2Rad;
                float radius = grid.size * Mathf.Lerp(0.12f, 0.44f, Mathf.PerlinNoise(cell.axialCoord.x * 0.31f + i, cell.axialCoord.y * 0.27f));
                Vector3 localPosition = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                float scale = Mathf.Lerp(0.58f, 0.92f, Mathf.PerlinNoise(cell.axialCoord.x * 0.17f + i * 2f, cell.axialCoord.y * 0.13f));
                Vector3 worldPosition = cluster.transform.TransformPoint(localPosition);
                if (!TryPlacePrefab(treePrefabs, worldPosition, scale * treePrefabScale, cell.axialCoord.x * 131 + cell.axialCoord.y * 17 + i, "TerrainCover_TreePrefab"))
                    CreateTree(cluster.transform, localPosition, scale, trunkMaterial, canopyMaterial);
            }

            created++;
        }
    }

    private void CreateMountainProps()
    {
        Material rockMaterial = CreateMaterial("Rock Prop", rockPropColor);
        int created = 0;

        foreach (HexCell cell in GetCells(HexTerrainType.Mountain))
        {
            if (created >= maxMountainPropCells)
                break;

            if (!ShouldPlaceDetail(cell, 0.68f))
                continue;

            GameObject cluster = new GameObject("TerrainCover_RockCluster_" + cell.axialCoord.x + "_" + cell.axialCoord.y);
            cluster.transform.SetParent(transform, false);
            cluster.transform.position = cell.transform.position;

            int count = 2 + Mathf.Abs(cell.axialCoord.x * 3 + cell.axialCoord.y) % 3;
            for (int i = 0; i < count; i++)
            {
                float angle = (i * 119f + cell.axialCoord.y * 23f) * Mathf.Deg2Rad;
                float radius = grid.size * Mathf.Lerp(0.08f, 0.38f, Mathf.PerlinNoise(cell.axialCoord.x * 0.23f, cell.axialCoord.y * 0.19f + i));
                Vector3 localPosition = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                float scale = Mathf.Lerp(0.42f, 0.80f, Mathf.PerlinNoise(cell.axialCoord.x * 0.15f + i, cell.axialCoord.y * 0.22f));
                Vector3 worldPosition = cluster.transform.TransformPoint(localPosition);
                if (!TryPlacePrefab(rockPrefabs, worldPosition, scale * rockPrefabScale, cell.axialCoord.x * 151 + cell.axialCoord.y * 29 + i, "TerrainCover_RockPrefab"))
                    CreateRock(cluster.transform, localPosition, scale, rockMaterial);
            }

            created++;
        }
    }

    private bool ShouldPlaceDetail(HexCell cell, float threshold)
    {
        float value = Mathf.PerlinNoise(cell.axialCoord.x * 0.37f + 12.7f, cell.axialCoord.y * 0.37f - 4.2f);
        return value >= threshold;
    }

    private void CreateRoadDetails()
    {
        if (roadDetailPrefabs == null || roadDetailPrefabs.Length == 0)
            return;

        int index = 0;
        foreach (HexCell cell in GetCells(HexTerrainType.Road))
        {
            if (cell == null)
                continue;

            if (index % 4 == 0)
            {
                TryPlacePrefab(
                    roadDetailPrefabs,
                    cell.transform.position,
                    roadPrefabScale,
                    cell.axialCoord.x * 67 + cell.axialCoord.y * 41,
                    "TerrainCover_RoadDetail"
                );
            }

            index++;
        }
    }

    private bool TryPlacePrefab(
        GameObject[] prefabs,
        Vector3 position,
        float scale,
        int seed,
        string objectName)
    {
        if (prefabs == null || prefabs.Length == 0)
            return false;

        GameObject prefab = PickPrefab(prefabs, seed);
        if (prefab == null)
            return false;

        GameObject instance = Instantiate(prefab, transform);
        instance.name = objectName + "_" + prefab.name;
        instance.transform.position = new Vector3(position.x, featureY, position.z);
        instance.transform.rotation = Quaternion.Euler(0f, Mathf.Abs(seed * 37) % 360f, 0f);
        instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        DisableColliders(instance);
        return true;
    }

    private GameObject PickPrefab(GameObject[] prefabs, int seed)
    {
        if (prefabs == null || prefabs.Length == 0)
            return null;

        int startIndex = Mathf.Abs(seed) % prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[(startIndex + i) % prefabs.Length];
            if (prefab != null)
                return prefab;
        }

        return null;
    }

    private void DisableColliders(GameObject instance)
    {
        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null)
                collider.enabled = false;
        }
    }

    private void CreateTree(Transform parent, Vector3 localPosition, float scale, Material trunkMaterial, Material canopyMaterial)
    {
        GameObject trunk = CreateMeshObject("Trunk", CreateCylinderMesh(0.055f * scale, 0.32f * scale, 6), trunkMaterial);
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = localPosition + Vector3.up * featureY;

        GameObject canopy = CreateMeshObject("Canopy", CreateConeMesh(0.22f * scale, 0.50f * scale, 7), canopyMaterial);
        canopy.transform.SetParent(parent, false);
        canopy.transform.localPosition = localPosition + Vector3.up * (featureY + 0.24f * scale);
        canopy.transform.localRotation = Quaternion.Euler(0f, (localPosition.x * 71f + localPosition.z * 43f) % 360f, 0f);
    }

    private void CreateRock(Transform parent, Vector3 localPosition, float scale, Material rockMaterial)
    {
        GameObject rock = CreateMeshObject("Rock", CreateRockMesh(0.24f * scale, 0.20f * scale, 7), rockMaterial);
        rock.transform.SetParent(parent, false);
        rock.transform.localPosition = localPosition + Vector3.up * featureY;
        rock.transform.localRotation = Quaternion.Euler(0f, (localPosition.x * 89f + localPosition.z * 47f) % 360f, 0f);
    }

    private void CreateRibbon(string objectName, List<Vector3> points, float width, Color color, float y, Texture2D texture = null)
    {
        if (points == null || points.Count < 2)
            return;

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Count * 2];
        Vector2[] uvs = new Vector2[points.Count * 2];
        int[] triangles = new int[(points.Count - 1) * 6];
        float traveled = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 forward;
            if (i == 0)
                forward = points[i + 1] - points[i];
            else if (i == points.Count - 1)
                forward = points[i] - points[i - 1];
            else
                forward = points[i + 1] - points[i - 1];

            Vector3 right = Vector3.Cross(Vector3.up, forward.normalized);
            Vector3 center = points[i];
            vertices[i * 2] = new Vector3(center.x, y, center.z) - right * width;
            vertices[i * 2 + 1] = new Vector3(center.x, y, center.z) + right * width;

            if (i > 0)
                traveled += Vector3.Distance(points[i - 1], points[i]);

            float v = traveled / Mathf.Max(1f, groundTextureMeters);
            uvs[i * 2] = new Vector2(0f, v);
            uvs[i * 2 + 1] = new Vector2(1f, v);
        }

        int t = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;
            triangles[t++] = a;
            triangles[t++] = c;
            triangles[t++] = b;
            triangles[t++] = b;
            triangles[t++] = c;
            triangles[t++] = d;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        CreateMeshObject(objectName, mesh, CreateMaterial(objectName, color, texture));
    }

    private Mesh CreateSoftDiscMesh(float radius, int sides, float y)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[sides + 1];
        int[] triangles = new int[sides * 3];

        vertices[0] = new Vector3(0f, y, 0f);
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }

        int t = 0;
        for (int i = 0; i < sides; i++)
        {
            triangles[t++] = 0;
            triangles[t++] = i == sides - 1 ? 1 : i + 2;
            triangles[t++] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private Mesh CreateIrregularPatchMesh(float radius, int sides, float y, int seed)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[sides + 1];
        int[] triangles = new int[sides * 3];

        vertices[0] = new Vector3(0f, y, 0f);
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            float noise = Mathf.PerlinNoise(seed * 0.013f + i * 0.37f, seed * 0.021f - i * 0.19f);
            float localRadius = radius * Mathf.Lerp(0.52f, 1.08f, noise);
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * localRadius,
                y,
                Mathf.Sin(angle) * localRadius
            );
        }

        int t = 0;
        for (int i = 0; i < sides; i++)
        {
            triangles[t++] = 0;
            triangles[t++] = i == sides - 1 ? 1 : i + 2;
            triangles[t++] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private Mesh CreateCylinderMesh(float radius, float height, int sides)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[sides * 2 + 2];
        int[] triangles = new int[sides * 12];

        vertices[0] = new Vector3(0f, height, 0f);
        vertices[1] = Vector3.zero;

        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices[2 + i] = new Vector3(x, height, z);
            vertices[2 + sides + i] = new Vector3(x, 0f, z);
        }

        int t = 0;
        for (int i = 0; i < sides; i++)
        {
            int next = i == sides - 1 ? 0 : i + 1;
            int top = 2 + i;
            int topNext = 2 + next;
            int bottom = 2 + sides + i;
            int bottomNext = 2 + sides + next;

            triangles[t++] = 0;
            triangles[t++] = top;
            triangles[t++] = topNext;

            triangles[t++] = 1;
            triangles[t++] = bottomNext;
            triangles[t++] = bottom;

            triangles[t++] = top;
            triangles[t++] = bottom;
            triangles[t++] = bottomNext;

            triangles[t++] = top;
            triangles[t++] = bottomNext;
            triangles[t++] = topNext;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private Mesh CreateConeMesh(float radius, float height, int sides)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[sides + 2];
        int[] triangles = new int[sides * 6];

        vertices[0] = new Vector3(0f, height, 0f);
        vertices[1] = Vector3.zero;

        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            float jitter = Mathf.Lerp(0.82f, 1.12f, Mathf.PerlinNoise(i * 0.61f, sides * 0.17f));
            vertices[2 + i] = new Vector3(Mathf.Cos(angle) * radius * jitter, 0f, Mathf.Sin(angle) * radius * jitter);
        }

        int t = 0;
        for (int i = 0; i < sides; i++)
        {
            int next = i == sides - 1 ? 0 : i + 1;
            triangles[t++] = 0;
            triangles[t++] = 2 + i;
            triangles[t++] = 2 + next;

            triangles[t++] = 1;
            triangles[t++] = 2 + next;
            triangles[t++] = 2 + i;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private Mesh CreateRockMesh(float radius, float height, int sides)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[sides + 2];
        int[] triangles = new int[sides * 6];

        vertices[0] = new Vector3(0f, height, 0f);
        vertices[vertices.Length - 1] = Vector3.zero;

        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            float jitter = Mathf.Lerp(0.62f, 1.16f, Mathf.PerlinNoise(i * 0.43f + radius, height * 7.1f));
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius * jitter, 0f, Mathf.Sin(angle) * radius * jitter);
        }

        int t = 0;
        for (int i = 0; i < sides; i++)
        {
            int next = i == sides - 1 ? 1 : i + 2;
            triangles[t++] = 0;
            triangles[t++] = i + 1;
            triangles[t++] = next;
            triangles[t++] = vertices.Length - 1;
            triangles[t++] = next;
            triangles[t++] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private GameObject CreateMeshObject(string objectName, Mesh mesh, Material material)
    {
        GameObject obj = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(transform, false);
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        return obj;
    }

    private Material CreateMaterial(string materialName, Color color, Texture2D texture = null)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = materialName + " Runtime";
        material.color = color;
        material.SetColor("_BaseColor", color);
        if (texture != null)
        {
            material.mainTexture = texture;
            material.SetTexture("_BaseMap", texture);
        }

        if (color.a < 0.99f)
            ConfigureTransparentMaterial(material);
        return material;
    }

    private Texture2D CreateNoiseTexture(
        string textureName,
        Color baseColor,
        Color detailColor,
        float detailStrength,
        float frequency)
    {
        int safeSize = Mathf.Max(32, textureSize);
        Texture2D texture = new Texture2D(safeSize, safeSize, TextureFormat.RGBA32, true);
        texture.name = textureName;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < safeSize; y++)
        {
            for (int x = 0; x < safeSize; x++)
            {
                float nx = x / (float)safeSize;
                float ny = y / (float)safeSize;
                float broad = Mathf.PerlinNoise(nx * frequency, ny * frequency);
                float fine = Mathf.PerlinNoise((nx + 19.3f) * frequency * 3.4f, (ny - 7.1f) * frequency * 3.4f);
                float blend = Mathf.Clamp01((broad * 0.74f + fine * 0.26f) * detailStrength);
                Color color = Color.Lerp(baseColor, detailColor, blend);
                color.a = baseColor.a;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(true, false);
        return texture;
    }

    private void ConfigureTransparentMaterial(Material material)
    {
        if (material == null)
            return;

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = 3000;
    }

    private Bounds GetGridBounds()
    {
        Bounds bounds = new Bounds(grid.allHexCells[0].transform.position, Vector3.one);
        foreach (HexCell cell in grid.allHexCells)
        {
            if (cell != null)
                bounds.Encapsulate(cell.transform.position);
        }

        return bounds;
    }

    private List<HexCell> GetCells(HexTerrainType type)
    {
        List<HexCell> cells = new List<HexCell>();
        foreach (HexCell cell in grid.allHexCells)
        {
            if (cell != null && cell.terrainType == type)
                cells.Add(cell);
        }

        return cells;
    }
}
