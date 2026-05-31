using System.Collections.Generic;
using UnityEngine;

public class WorldResourceNodeManager : MonoBehaviour
{
    public static WorldResourceNodeManager Instance;

    [Header("Generation")]
    public bool generateOnStart = true;
    public int nodesPerResource = 3;
    public int minCoordPadding = 4;
    public int seed = 1071;

    [Header("Prefab Pipeline")]
    public GameObject goldPrefab;
    public GameObject turanCoinPrefab;
    public GameObject steelPrefab;
    public GameObject oilPrefab;
    public GameObject borPrefab;
    public GameObject woodPrefab;
    public GameObject concretePrefab;
    public GameObject cementPrefab;
    public GameObject brickPrefab;
    public float resourcePrefabScale = 1f;

    private bool generated;
    private readonly Dictionary<HexCell, WorldResourceNode> nodesByHex =
        new Dictionary<HexCell, WorldResourceNode>();

    void Awake()
    {
        Instance = this;
        LoadDefaultPrefabsIfMissing();
    }

    void Start()
    {
        TryGenerateWhenReady();
    }

    void Update()
    {
        TryGenerateWhenReady();
    }

    private void TryGenerateWhenReady()
    {
        if (generated || !generateOnStart)
            return;

        if (CanGenerate())
        {
            GenerateDefaultNodes();
            generated = true;
        }
    }

    private bool CanGenerate()
    {
        HexGridManager grid =
            FindAnyObjectByType<HexGridManager>();

        return grid != null &&
               grid.allHexCells != null &&
               grid.allHexCells.Count > 0;
    }

    public WorldResourceNode GetNodeAtHex(HexCell hex)
    {
        if (hex == null)
            return null;

        nodesByHex.TryGetValue(hex, out WorldResourceNode node);
        return node;
    }

    public bool TryCollectAtHex(HexCell hex, UnitController unit)
    {
        WorldResourceNode node = GetNodeAtHex(hex);
        return node != null && node.TryCollect(unit);
    }

    public List<WorldResourceNode> GetAllNodesSnapshot()
    {
        List<WorldResourceNode> snapshot = new List<WorldResourceNode>();

        foreach (KeyValuePair<HexCell, WorldResourceNode> pair in nodesByHex)
        {
            if (pair.Value != null)
                snapshot.Add(pair.Value);
        }

        return snapshot;
    }

    public void EnsureGenerated()
    {
        if (generated)
            return;

        if (generateOnStart && CanGenerate())
            GenerateDefaultNodes();
    }

    public WorldResourceNode GetOrCreateSavedNode(
        WorldResourceType type,
        int level,
        int amount,
        HexCell hex)
    {
        if (hex == null)
            return null;

        if (nodesByHex.TryGetValue(hex, out WorldResourceNode existing))
            return existing;

        generated = true;
        return CreateNode(
            type,
            Mathf.Max(1, level),
            Mathf.Max(1, amount),
            hex
        );
    }

    public void GenerateDefaultNodes()
    {
        HexGridManager grid =
            FindAnyObjectByType<HexGridManager>();

        if (grid == null || grid.allHexCells == null || grid.allHexCells.Count == 0)
            return;

        generated = true;

        Random.InitState(seed);

        WorldResourceType[] types =
        {
            WorldResourceType.Gold,
            WorldResourceType.TuranCoin,
            WorldResourceType.Steel,
            WorldResourceType.Oil,
            WorldResourceType.Bor,
            WorldResourceType.Wood,
            WorldResourceType.Concrete,
            WorldResourceType.Cement
        };

        foreach (WorldResourceType type in types)
        {
            int count =
                type == WorldResourceType.TuranCoin
                    ? 1
                    : nodesPerResource;

            for (int i = 0; i < count; i++)
            {
                HexCell hex = PickFreeHex(grid);
                if (hex == null)
                    continue;

                int level = 1 + i;
                int amount = GetAmount(type, level);
                CreateNode(type, level, amount, hex);
            }
        }
    }

    private HexCell PickFreeHex(HexGridManager grid)
    {
        for (int attempt = 0; attempt < 80; attempt++)
        {
            int q = Random.Range(minCoordPadding, Mathf.Max(minCoordPadding + 1, grid.width - minCoordPadding));
            int r = Random.Range(minCoordPadding, Mathf.Max(minCoordPadding + 1, grid.height - minCoordPadding));

            HexCell hex = grid.GetHexAt(q, r);
            if (hex != null &&
                !nodesByHex.ContainsKey(hex) &&
                grid.IsResourcePlacementAllowed(hex))
            {
                return hex;
            }
        }

        foreach (HexCell hex in grid.allHexCells)
        {
            if (hex != null &&
                !nodesByHex.ContainsKey(hex) &&
                grid.IsResourcePlacementAllowed(hex))
            {
                return hex;
            }
        }

        return null;
    }

    private WorldResourceNode CreateNode(
        WorldResourceType type,
        int level,
        int amount,
        HexCell hex)
    {
        GameObject nodeObject =
            new GameObject($"Resource_{type}_Lv{level}_{hex.axialCoord.x}_{hex.axialCoord.y}");

        nodeObject.transform.SetParent(transform, false);
        nodeObject.transform.position = hex.transform.position + Vector3.up * 0.16f;

        SphereCollider collider =
            nodeObject.AddComponent<SphereCollider>();

        collider.radius = 0.55f;
        collider.center = new Vector3(0f, 0.25f, 0f);

        WorldResourceNode node =
            nodeObject.AddComponent<WorldResourceNode>();

        node.resourceType = type;
        node.level = level;
        node.hex = hex;
        bool guarded = IsGuardedConstructionResource(type);
        node.requiresTimedGathering = !guarded;
        node.ConfigureCollection(amount, GetGatherDuration(type, level));

        node.ConfigureGuard(
            guarded,
            GetGuardHealth(level),
            GetGuardDamage(level),
            guarded ? GetFirstClearReward(type, level) : new ResourceCost()
        );

        CreateVisual(nodeObject.transform, type, level);
        node.SetGuardRoot(CreateGuardVisual(nodeObject.transform, level));
        node.SetCollectorRoot(CreateCollectorVisual(nodeObject.transform));
        nodesByHex[hex] = node;
        return node;
    }

    private void CreateVisual(Transform parent, WorldResourceType type, int level)
    {
        GameObject prefab = GetResourcePrefab(type);
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.name = "Visual_" + type;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, Mathf.Abs((int)type * 37 + level * 19) % 360f, 0f);
            instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, resourcePrefabScale);
            DisableColliders(instance);
            return;
        }

        Color color = GetColor(type);

        GameObject baseRock =
            CreateLowPolyRock("Main", color, 0.55f, 0.28f);

        baseRock.transform.SetParent(parent, false);
        baseRock.transform.localPosition = Vector3.zero;

        for (int i = 0; i < Mathf.Clamp(level, 1, 3); i++)
        {
            GameObject shard =
                CreateLowPolyRock("Shard", color * 1.12f, 0.24f, 0.22f);

            shard.transform.SetParent(parent, false);
            float angle = i * 120f * Mathf.Deg2Rad;
            shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.26f, 0.2f, Mathf.Sin(angle) * 0.26f);
            shard.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
        }
    }

    private GameObject GetResourcePrefab(WorldResourceType type)
    {
        switch (type)
        {
            case WorldResourceType.Gold:
                return goldPrefab;
            case WorldResourceType.TuranCoin:
                return turanCoinPrefab;
            case WorldResourceType.Steel:
                return steelPrefab;
            case WorldResourceType.Oil:
                return oilPrefab;
            case WorldResourceType.Bor:
                return borPrefab;
            case WorldResourceType.Wood:
                return woodPrefab;
            case WorldResourceType.Concrete:
                return concretePrefab;
            case WorldResourceType.Cement:
                return cementPrefab;
            case WorldResourceType.Brick:
                return brickPrefab;
            default:
                return null;
        }
    }

    private void LoadDefaultPrefabsIfMissing()
    {
        if (goldPrefab == null)
            goldPrefab = Resources.Load<GameObject>("Prefabs/World/Resources/Gold");
        if (turanCoinPrefab == null)
            turanCoinPrefab = Resources.Load<GameObject>("Prefabs/World/Resources/TuranCoin");
        if (steelPrefab == null)
            steelPrefab = Resources.Load<GameObject>("Prefabs/World/Resources/Steel");
        if (oilPrefab == null)
            oilPrefab = Resources.Load<GameObject>("Prefabs/World/Resources/Oil");
        if (borPrefab == null)
            borPrefab = Resources.Load<GameObject>("Prefabs/World/Resources/Bor");
        if (woodPrefab == null)
            woodPrefab = Resources.Load<GameObject>("Prefabs/World/Resources/Wood");
        if (concretePrefab == null)
            concretePrefab = Resources.Load<GameObject>("Prefabs/World/Resources/Concrete");
        if (cementPrefab == null)
            cementPrefab = Resources.Load<GameObject>("Prefabs/World/Resources/Cement");
        if (brickPrefab == null)
            brickPrefab = Resources.Load<GameObject>("Prefabs/World/Resources/Brick");
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

    private GameObject CreateLowPolyRock(string objectName, Color color, float radius, float height)
    {
        GameObject obj =
            new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));

        Mesh mesh = new Mesh();
        int sides = 7;
        Vector3[] vertices = new Vector3[sides + 2];
        int[] triangles = new int[sides * 6];

        vertices[0] = new Vector3(0f, height, 0f);
        vertices[vertices.Length - 1] = Vector3.zero;

        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            float jitter = 0.82f + (i % 3) * 0.08f;
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

        obj.GetComponent<MeshFilter>().sharedMesh = mesh;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material =
            new Material(shader);

        material.color = color;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        return obj;
    }

    private GameObject CreateGuardVisual(Transform parent, int level)
    {
        GameObject guard =
            new GameObject("Guard", typeof(MeshFilter), typeof(MeshRenderer));

        guard.transform.SetParent(parent, false);
        guard.transform.localPosition = new Vector3(0.38f, 0.12f, -0.28f);
        guard.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
        guard.transform.localScale = Vector3.one * (0.38f + level * 0.05f);

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(0f, 0.55f, 0f),
            new Vector3(-0.28f, 0f, -0.22f),
            new Vector3(0.28f, 0f, -0.22f),
            new Vector3(0.24f, 0f, 0.24f),
            new Vector3(-0.24f, 0f, 0.24f)
        };
        mesh.triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 1,
            1, 4, 3,
            1, 3, 2
        };
        mesh.RecalculateNormals();

        guard.GetComponent<MeshFilter>().sharedMesh = mesh;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material =
            new Material(shader);

        material.color = new Color(0.62f, 0.08f, 0.08f, 1f);
        guard.GetComponent<MeshRenderer>().sharedMaterial = material;
        return guard;
    }

    private GameObject CreateCollectorVisual(Transform parent)
    {
        GameObject collector =
            new GameObject("CollectorTruck", typeof(MeshFilter), typeof(MeshRenderer));

        collector.transform.SetParent(parent, false);
        collector.transform.localPosition = new Vector3(-0.42f, 0.1f, 0.28f);
        collector.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
        collector.transform.localScale = Vector3.one * 0.34f;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.45f, 0f, -0.25f),
            new Vector3(0.45f, 0f, -0.25f),
            new Vector3(0.45f, 0f, 0.25f),
            new Vector3(-0.45f, 0f, 0.25f),
            new Vector3(-0.45f, 0.24f, -0.25f),
            new Vector3(0.45f, 0.24f, -0.25f),
            new Vector3(0.45f, 0.24f, 0.25f),
            new Vector3(-0.45f, 0.24f, 0.25f),
            new Vector3(0.05f, 0.42f, -0.20f),
            new Vector3(0.43f, 0.42f, -0.20f),
            new Vector3(0.43f, 0.42f, 0.20f),
            new Vector3(0.05f, 0.42f, 0.20f)
        };
        mesh.triangles = new int[]
        {
            0, 4, 5, 0, 5, 1,
            1, 5, 6, 1, 6, 2,
            2, 6, 7, 2, 7, 3,
            3, 7, 4, 3, 4, 0,
            4, 7, 6, 4, 6, 5,
            5, 8, 9, 5, 9, 6,
            6, 9, 10, 6, 10, 7,
            7, 10, 11, 7, 11, 8,
            5, 4, 8, 5, 8, 9,
            8, 11, 10, 8, 10, 9
        };
        mesh.RecalculateNormals();

        collector.GetComponent<MeshFilter>().sharedMesh = mesh;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material =
            new Material(shader);

        material.color = new Color(0.18f, 0.29f, 0.34f, 1f);
        collector.GetComponent<MeshRenderer>().sharedMaterial = material;
        return collector;
    }

    private int GetAmount(WorldResourceType type, int level)
    {
        int baseAmount =
            type == WorldResourceType.TuranCoin ? 10 :
            type == WorldResourceType.Gold ? 75 :
            type == WorldResourceType.Bor ? 45 :
            type == WorldResourceType.Oil ? 90 :
            120;

        return baseAmount * Mathf.Max(1, level);
    }

    private float GetGatherDuration(WorldResourceType type, int level)
    {
        float baseDuration =
            IsGuardedConstructionResource(type) ? 12f : 18f;

        return baseDuration + Mathf.Max(0, level - 1) * 8f;
    }

    private bool IsGuardedConstructionResource(WorldResourceType type)
    {
        return type == WorldResourceType.Wood ||
               type == WorldResourceType.Concrete ||
               type == WorldResourceType.Cement ||
               type == WorldResourceType.Brick;
    }

    private ResourceCost GetFirstClearReward(WorldResourceType type, int level)
    {
        int safeLevel = Mathf.Max(1, level);

        return new ResourceCost
        {
            gold = 20 * safeLevel,
            steel = 35 * safeLevel,
            bor = type == WorldResourceType.Cement ? 5 * safeLevel : 0
        };
    }

    private int GetGuardHealth(int level)
    {
        return 25 + Mathf.Max(1, level) * 25;
    }

    private int GetGuardDamage(int level)
    {
        return 4 + Mathf.Max(1, level) * 3;
    }

    private Color GetColor(WorldResourceType type)
    {
        switch (type)
        {
            case WorldResourceType.Oil:
                return new Color(0.07f, 0.13f, 0.10f, 1f);
            case WorldResourceType.Bor:
                return new Color(0.42f, 0.92f, 0.68f, 1f);
            case WorldResourceType.Wood:
                return new Color(0.42f, 0.27f, 0.13f, 1f);
            case WorldResourceType.Concrete:
                return new Color(0.58f, 0.58f, 0.54f, 1f);
            case WorldResourceType.Cement:
                return new Color(0.72f, 0.70f, 0.64f, 1f);
            case WorldResourceType.Gold:
                return new Color(0.95f, 0.72f, 0.22f, 1f);
            case WorldResourceType.TuranCoin:
                return new Color(0.44f, 0.80f, 1f, 1f);
            default:
                return new Color(0.58f, 0.66f, 0.72f, 1f);
        }
    }
}

