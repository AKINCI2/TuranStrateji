using System.Collections.Generic;
using UnityEngine;

public class WorldMapCloudMask : MonoBehaviour
{
    [Header("Target")]
    public HexGridManager grid;
    public Transform cloudRoot;
    public float yOffset = 0.18f;
    public int initialRevealRadius = 3;
    public int frontierFadeRadius = 1;

    [Header("Visual")]
    public Color unexploredColor = new Color(0.78f, 0.84f, 0.86f, 0.74f);
    public Color frontierColor = new Color(0.86f, 0.90f, 0.92f, 0.44f);
    public float tileFillRatio = 1.02f;
    public bool rebuildOnStart = true;
    public bool hideInBaseView = true;
    public bool revealAllAtStart = true; // Başlangıçta tüm bulutları kaldır

    [Header("Compatibility")]
[HideInInspector] public float edgePadding = -1.5f;
    [HideInInspector] public float cloudBandWidth = 44f;
    [HideInInspector] public Color cloudColor = new Color(0.82f, 0.88f, 0.90f, 0.50f);
    [HideInInspector] public int cloudPuffsPerSide = 0;
    [HideInInspector] public Vector2 puffScaleRange = new Vector2(4f, 8f);
    [HideInInspector] public bool createConnectedBands = true;
    [HideInInspector] public bool createLoosePuffs = false;

    private readonly HashSet<HexCell> revealedCells = new HashSet<HexCell>();
    private readonly Dictionary<HexCell, GameObject> fogByCell = new Dictionary<HexCell, GameObject>();
    private Material unexploredMaterial;
    private Material frontierMaterial;

    void OnEnable()
    {
        HookModeEvents();
    }

    void Start()
    {
        HookModeEvents();

        if (rebuildOnStart)
            Rebuild();
    }

    void OnDisable()
    {
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.ModeChanged -= OnModeChanged;
    }

    public void Rebuild()
    {
        if (grid == null)
            grid = FindAnyObjectByType<HexGridManager>();

        if (grid == null || grid.allHexCells == null || grid.allHexCells.Count == 0)
            return;

        NormalizeRuntimeSettings();
        EnsureRoot();
        ClearRoot();
        EnsureMaterials();
        
        if (revealAllAtStart)
        {
            revealedCells.Clear();
            foreach (HexCell cell in grid.allHexCells)
            {
                if (cell != null) revealedCells.Add(cell);
            }
        }
        else
        {
            RevealInitialBaseArea();
        }

        for (int i = 0; i < grid.allHexCells.Count; i++)
{
            HexCell cell = grid.allHexCells[i];
            if (cell == null || revealedCells.Contains(cell))
                continue;

            CreateFogHex(cell);
        }

        ApplyModeVisibility();
    }

    public bool IsRevealed(HexCell cell)
    {
        return cell != null && revealedCells.Contains(cell);
    }

    public void RevealAroundWorldPosition(Vector3 worldPosition, int radius)
    {
        if (grid == null)
            grid = FindAnyObjectByType<HexGridManager>();

        if (grid == null || grid.allHexCells == null || grid.allHexCells.Count == 0)
            return;

        RevealHex(grid.GetClosestHex(worldPosition), radius);
    }

    public void RevealHex(HexCell center, int radius)
    {
        if (center == null || grid == null || grid.allHexCells == null)
            return;

        radius = Mathf.Max(0, radius);
        List<HexCell> newlyRevealed = new List<HexCell>();

        foreach (HexCell cell in grid.allHexCells)
        {
            if (cell == null || revealedCells.Contains(cell))
                continue;

            if (center.GetDistance(cell) > radius)
                continue;

            revealedCells.Add(cell);
            newlyRevealed.Add(cell);
        }

        for (int i = 0; i < newlyRevealed.Count; i++)
            RemoveFog(newlyRevealed[i]);

        RefreshFrontierMaterials();
    }

    public void RevealByScoutPlane(Vector3 scoutWorldPosition, int radius, int rewardGold = 0)
    {
        int before = revealedCells.Count;
        RevealAroundWorldPosition(scoutWorldPosition, radius);
        int discovered = Mathf.Max(0, revealedCells.Count - before);

        if (discovered <= 0 || rewardGold <= 0 || BaseManager.Instance == null)
            return;

        ResourceCost reward = new ResourceCost { gold = rewardGold * discovered };
        BaseManager.Instance.AddResources(reward);
    }

    private void NormalizeRuntimeSettings()
    {
        yOffset = Mathf.Clamp(yOffset, 0.06f, 0.85f);
        initialRevealRadius = Mathf.Clamp(initialRevealRadius, 1, 10);
        frontierFadeRadius = Mathf.Clamp(frontierFadeRadius, 0, 3);
        tileFillRatio = Mathf.Clamp(tileFillRatio, 1.05f, 1.25f); // Daha büyük karolar
        unexploredColor.a = Mathf.Clamp(unexploredColor.a, 0.5f, 0.95f);
    }

    private void RevealInitialBaseArea()
    {
        if (revealedCells.Count > 0)
            return;

        WorldBaseMarker marker = WorldBaseMarker.FindPrimary(true);
        HexCell baseHex = marker != null ? grid.GetClosestHex(marker.transform.position) : null;
        if (baseHex == null && grid.allHexCells.Count > 0)
            baseHex = grid.allHexCells[grid.allHexCells.Count / 2];

        if (baseHex == null)
            return;

        foreach (HexCell cell in grid.allHexCells)
        {
            if (cell != null && baseHex.GetDistance(cell) <= initialRevealRadius)
                revealedCells.Add(cell);
        }
    }

    private void CreateFogHex(HexCell cell)
    {
        MeshFilter sourceMesh = cell.GetComponentInChildren<MeshFilter>();
        if (sourceMesh == null || sourceMesh.sharedMesh == null)
            return;

        GameObject fog = new GameObject(
            "FogTile_" + cell.axialCoord.x + "_" + cell.axialCoord.y,
            typeof(MeshFilter),
            typeof(MeshRenderer)
        );

        fog.transform.SetParent(cloudRoot, false);
        fog.transform.position = cell.transform.position + Vector3.up * yOffset;
        fog.transform.rotation = Quaternion.identity;
        fog.GetComponent<MeshFilter>().sharedMesh = CreateSquareTileMesh(GetTileSize());

        MeshRenderer renderer = fog.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = IsFrontier(cell) ? frontierMaterial : unexploredMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        fogByCell[cell] = fog;
    }

    private Vector2 GetTileSize()
    {
        float safeSize = grid != null ? Mathf.Max(0.1f, grid.size) : 1f;
        float width = Mathf.Sqrt(3f) * safeSize * tileFillRatio;
        float height = 1.5f * safeSize * tileFillRatio;
        return new Vector2(width, height);
    }

    private Mesh CreateSquareTileMesh(Vector2 tileSize)
    {
        Mesh mesh = new Mesh();
        float halfX = tileSize.x * 0.5f;
        float halfZ = tileSize.y * 0.5f;

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
        return mesh;
    }

    private bool IsFrontier(HexCell cell)
    {
        if (frontierFadeRadius <= 0 || cell == null)
            return false;

        foreach (HexCell revealed in revealedCells)
        {
            if (revealed != null && revealed.GetDistance(cell) <= frontierFadeRadius)
                return true;
        }

        return false;
    }

    private void RefreshFrontierMaterials()
    {
        foreach (KeyValuePair<HexCell, GameObject> pair in fogByCell)
        {
            if (pair.Value == null || pair.Key == null)
                continue;

            MeshRenderer renderer = pair.Value.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = IsFrontier(pair.Key) ? frontierMaterial : unexploredMaterial;
        }
    }

    private void RemoveFog(HexCell cell)
    {
        if (cell == null || !fogByCell.TryGetValue(cell, out GameObject fog))
            return;

        fogByCell.Remove(cell);
        if (fog == null)
            return;

        if (Application.isPlaying)
            Destroy(fog);
        else
            DestroyImmediate(fog);
    }

    private void EnsureRoot()
    {
        if (cloudRoot != null)
            return;

        Transform existing = transform.Find("WorldMapHexFog_Runtime");
        if (existing != null)
        {
            cloudRoot = existing;
            return;
        }

        GameObject root = new GameObject("WorldMapHexFog_Runtime");
        root.transform.SetParent(transform, false);
        cloudRoot = root.transform;
    }

    private void ClearRoot()
    {
        fogByCell.Clear();

        if (cloudRoot == null)
            return;

        for (int i = cloudRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = cloudRoot.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void EnsureMaterials()
    {
        if (unexploredMaterial == null)
            unexploredMaterial = CreateCloudMaterial(unexploredColor, "HexFog_Unexplored");

        if (frontierMaterial == null)
            frontierMaterial = CreateCloudMaterial(frontierColor, "HexFog_Frontier");
    }

    private void HookModeEvents()
    {
        if (GameModeManager.Instance == null)
            return;

        GameModeManager.Instance.ModeChanged -= OnModeChanged;
        GameModeManager.Instance.ModeChanged += OnModeChanged;
        ApplyModeVisibility();
    }

    private void OnModeChanged(GameViewMode mode)
    {
        ApplyModeVisibility();
    }

    private void ApplyModeVisibility()
    {
        if (cloudRoot == null)
            return;

        bool visible = true;
        if (hideInBaseView &&
            GameModeManager.Instance != null &&
            GameModeManager.Instance.CurrentMode == GameViewMode.BaseView)
        {
            visible = false;
        }

        cloudRoot.gameObject.SetActive(visible);
    }

    private Material CreateCloudMaterial(Color color, string materialName)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = materialName;
        mat.color = color;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_SrcBlend"))
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return mat;
    }
}
