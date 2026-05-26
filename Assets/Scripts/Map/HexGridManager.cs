using UnityEngine;
using System.Collections.Generic;

public class HexGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject hexPrefab;
    public int width = 72;
    public int height = 72;
    public float size = 1f;
    public bool autoApplyMobileWorldPreset = true;
    public int mobilePresetWidth = 72;
    public int mobilePresetHeight = 72;
    public float mobilePresetHexSize = 1f;

    [Header("World Generation")]
    public bool generateWorldTerrain = true;
    public int terrainSeed = 1453;
    public int riverCenterColumn = 13;
    public int riverWidth = 2;
    public bool createSampleCities = true;
    public bool createTerrainCover = true;

    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool snapToIntegerPositions = true;  // PozisyonlarÄ± yuvarla

    // Herkese aÃ§Ä±k grid listesi (UnitController eriÅŸebilsin)
    [HideInInspector]
    public List<HexCell> allHexCells = new List<HexCell>();

    private Dictionary<Vector2Int, HexCell> grid = new Dictionary<Vector2Int, HexCell>();

    void Awake()
    {
        showDebugLogs = false;
        ApplyMobilePresetIfNeeded();
    }

    void Start()
    {
        GenerateGrid();
        AssignNeighbors();

        if (showDebugLogs)
            Debug.Log($"Grid oluÅŸturuldu: {allHexCells.Count} hex hÃ¼cresi");

        EnsureTerrainCover();
        EnsureCloudMask();
        EnsureSampleCities();
    }

    private void ApplyMobilePresetIfNeeded()
    {
        if (!autoApplyMobileWorldPreset)
            return;

        width = Mathf.Max(width, mobilePresetWidth);
        height = Mathf.Max(height, mobilePresetHeight);
        size = Mathf.Max(0.65f, mobilePresetHexSize);

        riverCenterColumn = Mathf.Clamp(Mathf.RoundToInt(width * 0.5f), 2, width - 3);
        riverWidth = Mathf.Clamp(riverWidth, 1, 3);

        if (terrainSeed == 0)
            terrainSeed = 1453;
    }

    void GenerateGrid()
    {
        // Listeleri temizle
        grid.Clear();
        allHexCells.Clear();

        float hexWidth = Mathf.Sqrt(3f) * size;
        float hexHeight = 2f * size;

        for (int r = 0; r < height; r++)
        {
            for (int q = 0; q < width; q++)
            {
                float x = hexWidth * (q + r * 0.5f);
                float z = hexHeight * (r * 0.75f);

                // Drift'i Ã¶nlemek iÃ§in pozisyonlarÄ± yuvarla
                if (snapToIntegerPositions)
                {
                    x = Mathf.Round(x * 100f) / 100f;
                    z = Mathf.Round(z * 100f) / 100f;
                }

                Vector3 pos = new Vector3(x, 0, z);

                GameObject obj = Instantiate(hexPrefab, pos, Quaternion.identity, transform);
                obj.name = $"Hex_{q}_{r}";

                HexCell cell = obj.GetComponent<HexCell>();

                // HexCell yoksa ekle
                if (cell == null)
                {
                    cell = obj.AddComponent<HexCell>();
                    if (showDebugLogs)
                        Debug.Log($"HexCell eklendi: {obj.name}");
                }

                cell.SetCoord(q, r);
                if (generateWorldTerrain)
                    cell.SetTerrain(GetProceduralTerrain(q, r));

                // Her iki listeye de ekle
                Vector2Int coord = new Vector2Int(q, r);
                grid[coord] = cell;
                allHexCells.Add(cell);

                if (showDebugLogs && (q == 0 && r == 0))
                    Debug.Log($"Ä°lk hex oluÅŸturuldu: {obj.name} pozisyon: {pos}");
            }
        }

        if (showDebugLogs)
            Debug.Log($"âœ… TOPLAM HEX SAYISI: {grid.Count}");
    }

    private HexTerrainType GetProceduralTerrain(int q, int r)
    {
        int riverWave = Mathf.RoundToInt(Mathf.Sin(r * 0.33f + terrainSeed * 0.01f) * 2f);
        int riverColumn = Mathf.Clamp(riverCenterColumn + riverWave, 0, width - 1);
        if (Mathf.Abs(q - riverColumn) < riverWidth && IsBridgeRow(r))
            return HexTerrainType.Road;

        if (Mathf.Abs(q - riverColumn) < riverWidth)
            return HexTerrainType.Water;

        if (IsRoadHex(q, r))
            return HexTerrainType.Road;

        float forestNoise = Mathf.PerlinNoise((q + terrainSeed) * 0.16f, (r - terrainSeed) * 0.16f);
        if (forestNoise > 0.72f)
            return HexTerrainType.Forest;

        float mountainNoise = Mathf.PerlinNoise((q - terrainSeed) * 0.11f, (r + terrainSeed) * 0.11f);
        if (mountainNoise > 0.82f)
            return HexTerrainType.Mountain;

        return HexTerrainType.Plains;
    }

    private bool IsBridgeRow(int r)
    {
        int firstBridge = Mathf.RoundToInt(height * 0.30f);
        int secondBridge = Mathf.RoundToInt(height * 0.58f);
        int thirdBridge = Mathf.RoundToInt(height * 0.78f);

        return Mathf.Abs(r - firstBridge) <= 1 ||
            Mathf.Abs(r - secondBridge) <= 1 ||
            Mathf.Abs(r - thirdBridge) <= 1;
    }

    private bool IsRoadHex(int q, int r)
    {
        if (q == Mathf.RoundToInt(width * 0.22f) && r > 2 && r < height - 3)
            return true;

        int diagonal = Mathf.RoundToInt(width * 0.15f + r * 0.45f);
        if (Mathf.Abs(q - diagonal) <= 0 && r > 3 && r < height - 4)
            return true;

        int secondDiagonal = Mathf.RoundToInt(width * 0.78f - r * 0.25f);
        return Mathf.Abs(q - secondDiagonal) <= 0 && r > 5 && r < height - 5;
    }

    void AssignNeighbors()
    {
        int totalNeighbors = 0;

        foreach (HexCell cell in grid.Values)
        {
            if (cell.neighbors == null)
                cell.neighbors = new List<HexCell>();
            else
                cell.neighbors.Clear();

            int q = cell.axialCoord.x;
            int r = cell.axialCoord.y;

            // Pozisyon formÃ¼lÃ¼ odd-r offset grid kullanÄ±yor.
            // KomÅŸuluklar da aynÄ± sistemde kalmalÄ±; aksi halde unitler gerÃ§ek komÅŸu olmayan hexlere yÃ¼rÃ¼r.
            Vector2Int[] evenRowDirections = new Vector2Int[]
            {
                new Vector2Int(+1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, +1),
                new Vector2Int(-1, +1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, -1)
            };

            Vector2Int[] oddRowDirections = new Vector2Int[]
            {
                new Vector2Int(+1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(+1, +1),
                new Vector2Int(0, +1),
                new Vector2Int(+1, -1),
                new Vector2Int(0, -1)
            };

            Vector2Int[] directions = (r % 2 == 0) ? evenRowDirections : oddRowDirections;

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborCoord = new Vector2Int(q + dir.x, r + dir.y);

                if (grid.ContainsKey(neighborCoord))
                {
                    HexCell neighbor = grid[neighborCoord];
                    if (!cell.neighbors.Contains(neighbor))
                    {
                        cell.neighbors.Add(neighbor);
                        totalNeighbors++;
                    }
                }
            }
        }

        if (showDebugLogs)
            Debug.Log($"âœ… KomÅŸuluklar atandÄ±: Toplam {totalNeighbors} baÄŸlantÄ±");
    }

    public HexCell GetClosestHex(Vector3 pos)
    {
        if (allHexCells == null || allHexCells.Count == 0)
        {
            Debug.LogError("âŒ HiÃ§ hex hÃ¼cresi yok! Ã–nce grid oluÅŸturulmalÄ±.");
            return null;
        }

        HexCell closest = null;
        float minDist = Mathf.Infinity;

        foreach (HexCell cell in allHexCells)
        {
            if (cell == null) continue;

            float dist = Vector3.Distance(pos, cell.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = cell;
            }
        }

        if (closest != null && showDebugLogs)
        {
            Debug.Log($"ğŸ¯ En yakÄ±n hex bulundu: {closest.name}, mesafe: {minDist:F2}");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"Hex bulunamadi. Aranan pozisyon: {pos}, toplam hex: {allHexCells.Count}");
        }

        return closest;
    }

    // Bir hex'in tam merkez pozisyonunu dÃ¶ndÃ¼r
    public Vector3 GetHexCenter(HexCell hex)
    {
        if (hex == null) return Vector3.zero;

        Vector3 center = hex.transform.position;
        if (snapToIntegerPositions)
        {
            center.x = Mathf.Round(center.x * 100f) / 100f;
            center.z = Mathf.Round(center.z * 100f) / 100f;
        }
        return center;
    }

    // Hex koordinatlarÄ±na gÃ¶re pozisyon hesapla
    public Vector3 GetPositionFromCoord(int q, int r)
    {
        float hexWidth = Mathf.Sqrt(3f) * size;
        float hexHeight = 2f * size;

        float x = hexWidth * (q + r * 0.5f);
        float z = hexHeight * (r * 0.75f);

        if (snapToIntegerPositions)
        {
            x = Mathf.Round(x * 100f) / 100f;
            z = Mathf.Round(z * 100f) / 100f;
        }

        return new Vector3(x, 0, z);
    }

    // Grid'deki tÃ¼m hex'leri dÃ¶ndÃ¼r
    public List<HexCell> GetAllHexes()
    {
        return allHexCells;
    }

    // Koordinata gÃ¶re hex bul
    public HexCell GetHexAt(int q, int r)
    {
        Vector2Int coord = new Vector2Int(q, r);
        return grid.ContainsKey(coord) ? grid[coord] : null;
    }

    public bool IsResourcePlacementAllowed(HexCell hex)
    {
        if (hex == null || !hex.IsWalkable())
            return false;

        if (hex.terrainType == HexTerrainType.Road ||
            hex.terrainType == HexTerrainType.City ||
            hex.currentUnit != null)
        {
            return false;
        }

        foreach (HexCell neighbor in hex.neighbors)
        {
            if (neighbor != null && neighbor.terrainType == HexTerrainType.Water)
                return false;
        }

        return true;
    }

    public bool IsBasePlacementAllowed(HexCell hex)
    {
        return hex != null && hex.IsBasePlacementAllowed();
    }

    public HexCell GetNearestBasePlacementHex(Vector3 position)
    {
        HexCell closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (HexCell cell in allHexCells)
        {
            if (!IsBasePlacementAllowed(cell))
                continue;

            float distance = Vector3.Distance(position, cell.transform.position);
            if (distance < closestDistance)
            {
                closest = cell;
                closestDistance = distance;
            }
        }

        return closest;
    }

    // Bir sonraki hex'e geÃ§erken pozisyon kontrolÃ¼
    public bool IsPositionAtHexCenter(Vector3 position, HexCell hex)
    {
        if (hex == null) return false;

        Vector3 hexCenter = GetHexCenter(hex);
        float dist = Vector3.Distance(position, hexCenter);
        return dist < 0.1f;
    }

    // Unit'i hex'in tam merkezine snap yap
    public void SnapUnitToHex(UnitController unit, HexCell hex)
    {
        if (unit == null || hex == null) return;

        Vector3 snapPos = GetHexCenter(hex);
        snapPos.y = 0.5f; // Maintain height
        unit.transform.position = snapPos;
        unit.currentHex = hex;

        if (showDebugLogs)
            Debug.Log($"Unit {unit.name} snap yapÄ±ldÄ±: {hex.axialCoord}");
    }

    private void EnsureSampleCities()
    {
        if (!createSampleCities)
            return;

        if (FindAnyObjectByType<WorldCityNode>() != null)
            return;

        CreateCityNode("ankara_merkez", "Ankara Merkez", "WC_AnkaraMerkez", Mathf.RoundToInt(width * 0.28f), Mathf.RoundToInt(height * 0.32f), 5);
        CreateCityNode("otuken_gecidi", "Otuken Gecidi", "WC_OtukenGecidi", Mathf.RoundToInt(width * 0.64f), Mathf.RoundToInt(height * 0.55f), 4);
        CreateCityNode("hazar_koprusu", "Hazar Koprusu", "WC_HazarKoprusu", Mathf.RoundToInt(width * 0.48f), Mathf.RoundToInt(height * 0.72f), 3);
    }

    private void CreateCityNode(string id, string displayName, string dataResourceName, int q, int r, int influenceRadius)
    {
        HexCell hex = GetHexAt(q, r);
        if (hex == null)
            return;

        GameObject cityObject = new GameObject("City_" + id, typeof(WorldCityNode));
        cityObject.transform.SetParent(transform, false);
        cityObject.transform.position = hex.transform.position + Vector3.up * 0.2f;

        WorldCityNode city = cityObject.GetComponent<WorldCityNode>();
        city.data = Resources.Load<WorldCityData>("Data/WorldCities/" + dataResourceName);
        city.cityId = id;
        city.displayName = displayName;
        city.influenceRadius = influenceRadius;
        city.ApplyData();
        city.RefreshVisual();
        city.RebuildInfluence();
        city.ApplyTerritoryVisuals();
    }

    private void EnsureTerrainCover()
    {
        if (!createTerrainCover)
            return;

        WorldTerrainCover cover = GetComponent<WorldTerrainCover>();
        if (cover == null)
            cover = gameObject.AddComponent<WorldTerrainCover>();

        cover.Build(this);
    }

    // Editor iÃ§in yardÄ±mcÄ± metod
    public void RegenerateGrid()
    {
        // Mevcut hex'leri temizle
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        grid.Clear();
        allHexCells.Clear();

        GenerateGrid();
        AssignNeighbors();
        EnsureTerrainCover();
        EnsureCloudMask();

        if (showDebugLogs)
            Debug.Log("ğŸ”„ Grid yeniden oluÅŸturuldu!");
    }

    private void EnsureCloudMask()
    {
        WorldMapCloudMask cloudMask = GetComponent<WorldMapCloudMask>();
        if (cloudMask == null)
            cloudMask = gameObject.AddComponent<WorldMapCloudMask>();

        cloudMask.grid = this;
        cloudMask.Rebuild();
    }
}

