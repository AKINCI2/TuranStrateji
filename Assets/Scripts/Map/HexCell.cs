using UnityEngine;
using System.Collections.Generic;

public class HexCell : MonoBehaviour
{
    [Header("Grid")]
    public Vector2Int axialCoord;
    public List<HexCell> neighbors = new List<HexCell>();

    [Header("Pathfinding")]
    public int gCost;  // Başlangıçtan bu hücreye olan maliyet
    public int hCost;  // Bu hücreden hedefe olan tahmini maliyet
    public int fCost;  // gCost + hCost
    public HexCell parent;  // Pathfinding'de önceki hücre

    [Header("Unit")]
    public UnitController currentUnit;  // Bu hex'teki unit

    [Header("World Map")]
    public HexTerrainType terrainType = HexTerrainType.Plains;
    public bool blocksMovement;
    public string controllingAllianceId;
    public Color territoryColor = Color.clear;

    [Header("Visual")]
    public Material defaultMaterial;
    public Material highlightMaterial;
    public Material pathMaterial;
    public bool useHighlightMaterials = false;
    public Color highlightColor = new Color(1f, 0.82f, 0.2f, 1f);
    public Color pathColor = new Color(0.25f, 0.95f, 0.35f, 1f);
    public Color occupiedColor = new Color(0.9f, 0.25f, 0.2f, 1f);
    public Color plainsColor = new Color(0.64f, 0.55f, 0.41f, 1f);
    public Color forestColor = new Color(0.28f, 0.43f, 0.27f, 1f);
    public Color roadColor = new Color(0.50f, 0.46f, 0.38f, 1f);
    public Color waterColor = new Color(0.18f, 0.40f, 0.56f, 1f);
    public Color mountainColor = new Color(0.40f, 0.38f, 0.36f, 1f);
    public Color cityColor = new Color(0.62f, 0.56f, 0.45f, 1f);
    public bool hideWorldHexSurface = true;
    public float overlayHeightOffset = 0.08f;
    public Color neighborRevealColor = new Color(0.54f, 0.72f, 0.46f, 0.11f);
    public Color selectedRevealColor = new Color(0.78f, 0.92f, 0.58f, 0.24f);
    public Color overlayShadowColor = new Color(0f, 0f, 0f, 0.24f);

    private Renderer rend;
    private Renderer overlayRenderer;
    private Renderer overlayShadowRenderer;
    private Color originalColor;
    private Material originalMaterial;
    private MaterialPropertyBlock propertyBlock;
    private MaterialPropertyBlock overlayPropertyBlock;

    void Awake()
    {
        // Renderer'ı bul (child veya kendinde)
        rend = GetComponentInChildren<Renderer>();
        if (rend == null)
            rend = GetComponent<Renderer>();

        // Orijinal materyali kaydet
        if (rend != null && rend.material != null)
        {
            originalMaterial = rend.material;
            originalColor = rend.material.color;
        }
        else if (rend != null)
        {
            originalColor = rend.sharedMaterial.color;
        }

        // Varsayılan materyaller
        if (defaultMaterial == null && rend != null)
            defaultMaterial = rend.material;

        propertyBlock = new MaterialPropertyBlock();
        overlayPropertyBlock = new MaterialPropertyBlock();
        EnsureOverlayRenderer();
        SetWorldSurfaceVisible(!hideWorldHexSurface);
        ClearOverlay();
    }

    public void SetCoord(int q, int r)
    {
        axialCoord = new Vector2Int(q, r);
        gameObject.name = $"Hex_{q}_{r}";
    }

    // Pathfinding için FCost hesapla
    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }

    // Hex'i vurgula (seçim için)
    public void Highlight()
    {
        ShowOverlay(selectedRevealColor);
    }

    // Path hex'ini vurgula
    public void HighlightPath()
    {
        ShowOverlay(pathColor);
    }

    public void RevealAsNeighbor()
    {
        ShowOverlay(neighborRevealColor);
    }

    // Normal renge dön
    public void ResetColor()
    {
        if (hideWorldHexSurface)
        {
            ClearOverlay();
            return;
        }

        if (rend != null && originalMaterial != null)
        {
            rend.material = originalMaterial;
            SetRendererColor(GetWorldColor());
        }
        else if (rend != null) SetRendererColor(GetWorldColor());

        ClearOverlay();
    }

    // Unit geldiğinde çağrılır
    public void OnUnitEnter(UnitController unit)
    {
        currentUnit = unit;
    }

    // Unit çıktığında çağrılır
    public void OnUnitExit()
    {
        currentUnit = null;
        // Normal rengine dön
        ResetColor();
    }

    // Bu hex yürünebilir mi?
    public bool IsWalkable()
    {
        if (blocksMovement)
            return false;

        return terrainType != HexTerrainType.Water && terrainType != HexTerrainType.Mountain;
    }

    public bool IsBasePlacementAllowed()
    {
        if (!IsWalkable())
            return false;

        if (terrainType == HexTerrainType.Road || terrainType == HexTerrainType.City)
            return false;

        foreach (HexCell neighbor in neighbors)
        {
            if (neighbor != null &&
                (neighbor.terrainType == HexTerrainType.Water ||
                 neighbor.terrainType == HexTerrainType.Mountain ||
                 neighbor.terrainType == HexTerrainType.Road))
            {
                return false;
            }
        }

        return true;
    }

    public int GetMovementCost()
    {
        if (!IsWalkable())
            return int.MaxValue;

        if (terrainType == HexTerrainType.Road)
            return 6;
        if (terrainType == HexTerrainType.Forest)
            return 14;
        if (terrainType == HexTerrainType.City)
            return 9;

        return 10;
    }

    public void SetTerrain(HexTerrainType newTerrainType)
    {
        terrainType = newTerrainType;
        blocksMovement = terrainType == HexTerrainType.Water || terrainType == HexTerrainType.Mountain;
        originalColor = GetWorldColor();
        if (!hideWorldHexSurface)
            SetRendererColor(originalColor);
    }

    public void SetTerritory(string allianceId, Color color)
    {
        controllingAllianceId = allianceId;
        territoryColor = color;
        if (!hideWorldHexSurface)
            SetRendererColor(GetWorldColor());
    }

    public Color GetWorldColor()
    {
        Color baseColor = plainsColor;
        if (terrainType == HexTerrainType.Forest)
            baseColor = forestColor;
        else if (terrainType == HexTerrainType.Road)
            baseColor = roadColor;
        else if (terrainType == HexTerrainType.Water)
            baseColor = waterColor;
        else if (terrainType == HexTerrainType.Mountain)
            baseColor = mountainColor;
        else if (terrainType == HexTerrainType.City)
            baseColor = cityColor;

        if (!string.IsNullOrWhiteSpace(controllingAllianceId) && territoryColor.a > 0f)
            return Color.Lerp(baseColor, territoryColor, 0.35f);

        return baseColor;
    }

    // Komşuları temizle ve yeniden ata
    public void ClearNeighbors()
    {
        neighbors.Clear();
    }

    // Komşu ekle
    public void AddNeighbor(HexCell neighbor)
    {
        if (neighbor != null && !neighbors.Contains(neighbor))
            neighbors.Add(neighbor);
    }

    // Tüm komşuları döndür
    public List<HexCell> GetNeighbors()
    {
        return neighbors;
    }

    // Sadece yürünebilir komşuları döndür
    public List<HexCell> GetWalkableNeighbors()
    {
        List<HexCell> walkable = new List<HexCell>();
        foreach (HexCell neighbor in neighbors)
        {
            if (neighbor.IsWalkable())
                walkable.Add(neighbor);
        }
        return walkable;
    }

    // Mesafe hesapla (Manhattan veya Euclidean)
    public int GetDistance(HexCell other)
    {
        if (other == null) return int.MaxValue;

        Vector3Int a = OffsetToCube(axialCoord);
        Vector3Int b = OffsetToCube(other.axialCoord);

        return Mathf.Max(
            Mathf.Abs(a.x - b.x),
            Mathf.Abs(a.y - b.y),
            Mathf.Abs(a.z - b.z)
        );
    }

    private Vector3Int OffsetToCube(Vector2Int coord)
    {
        int row = coord.y;
        int x = coord.x - (row - (row & 1)) / 2;
        int z = row;
        int y = -x - z;

        return new Vector3Int(x, y, z);
    }

    private void SetRendererColor(Color color)
    {
        if (rend == null)
            return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        rend.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        rend.SetPropertyBlock(propertyBlock);
    }

    private void EnsureOverlayRenderer()
    {
        if (overlayRenderer != null)
            return;

        MeshFilter sourceMesh = GetComponentInChildren<MeshFilter>();
        if (sourceMesh == null || sourceMesh.sharedMesh == null)
            return;

        overlayShadowRenderer = CreateOverlayRenderer(
            "HexSelectionShadow",
            sourceMesh,
            sourceMesh.transform.localScale * 1.04f,
            overlayHeightOffset * 0.72f,
            2998
        );

        overlayRenderer = CreateOverlayRenderer(
            "HexSelectionOverlay",
            sourceMesh,
            sourceMesh.transform.localScale * 0.98f,
            overlayHeightOffset,
            3000
        );
    }

    private Renderer CreateOverlayRenderer(
        string objectName,
        MeshFilter sourceMesh,
        Vector3 localScale,
        float heightOffset,
        int renderQueue)
    {
        GameObject overlay =
            new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));

        overlay.transform.SetParent(transform, false);
        overlay.transform.localPosition = sourceMesh.transform.localPosition + Vector3.up * heightOffset;
        overlay.transform.localRotation = sourceMesh.transform.localRotation;
        overlay.transform.localScale = localScale;
        overlay.GetComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = objectName + " Runtime";
        material.color = Color.clear;
        ConfigureTransparentMaterial(material);
        material.renderQueue = renderQueue;

        Renderer renderer = overlay.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
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

    private void SetWorldSurfaceVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == overlayRenderer || renderer == overlayShadowRenderer)
                continue;

            renderer.enabled = visible;
        }
    }

    private void ShowOverlay(Color color)
    {
        EnsureOverlayRenderer();
        if (overlayRenderer == null)
            return;

        overlayRenderer.enabled = true;
        if (overlayShadowRenderer != null)
        {
            overlayShadowRenderer.enabled = true;
            SetOverlayRendererColor(overlayShadowRenderer, overlayShadowColor);
        }

        SetOverlayRendererColor(overlayRenderer, color);
    }

    private void SetOverlayRendererColor(Renderer targetRenderer, Color color)
    {
        if (targetRenderer == null)
            return;

        if (overlayPropertyBlock == null)
            overlayPropertyBlock = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(overlayPropertyBlock);
        overlayPropertyBlock.SetColor("_BaseColor", color);
        overlayPropertyBlock.SetColor("_Color", color);
        targetRenderer.SetPropertyBlock(overlayPropertyBlock);
    }

    private void ClearOverlay()
    {
        if (overlayRenderer == null)
            return;

        overlayRenderer.enabled = false;
        if (overlayShadowRenderer != null)
            overlayShadowRenderer.enabled = false;
    }

    // Editor'de görselleştirme
    void OnDrawGizmos()
    {
        // Hex merkezini göster
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        // Pozisyonu etiket olarak göster
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"{axialCoord.x},{axialCoord.y}\n({transform.position.x:F2},{transform.position.z:F2})");

        if (currentUnit != null)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, "🚫 OCCUPIED");
        }
#endif
    }

    // Debug için
    public override string ToString()
    {
        return $"HexCell [{axialCoord.x},{axialCoord.y}] - Walkable: {IsWalkable()}";
    }
}

