using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class WorldCityNode : MonoBehaviour
{
    [Header("Data")]
    public WorldCityData data;

    [Header("Identity")]
    public string cityId = "city_node";
    public string displayName = "Stratejik Sehir";
    public int level = 1;
    public string regionName = "Turan Bolgesi";
    public int requiredAllianceLevel = 1;
    public int requiredPower = 5000;
    public WorldCityBonusType bonusType = WorldCityBonusType.None;
    [Range(0f, 1f)]
    public float bonusPercent = 0.05f;

    [Header("Territory")]
    public int influenceRadius = 4;
    public string controllingAllianceId;
    public Color controllingColor = new Color(0.1f, 0.55f, 0.95f, 0.85f);
    public AllianceOccupationState occupationState = AllianceOccupationState.Neutral;

    [Header("Occupation")]
    public string occupyingAllianceId;
    public Color occupyingColor = new Color(0.9f, 0.7f, 0.2f, 0.85f);
    public float baseOccupationSeconds = 180f;
    public int stationedMemberCount;
    public float occupationProgress;

    [Header("Visual")]
    public Transform visualRoot;
    public GameObject runtimeVisual;

    private readonly List<HexCell> influenceHexes = new List<HexCell>();
    private HexCell cityHex;
    private HexGridManager gridManager;

    void Awake()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 1.25f;
    }

    void Start()
    {
        ApplyData();
        RefreshVisual();
        gridManager = FindAnyObjectByType<HexGridManager>();
        RebuildInfluence();
        ApplyTerritoryVisuals();
    }

    void Update()
    {
        TickOccupation(Time.deltaTime);
    }

    public void StartOccupation(string allianceId, Color allianceColor)
    {
        if (string.IsNullOrWhiteSpace(allianceId))
            return;

        if (controllingAllianceId == allianceId && occupationState == AllianceOccupationState.Occupied)
            return;

        occupyingAllianceId = allianceId;
        occupyingColor = allianceColor;
        occupationState = AllianceOccupationState.Occupying;
        occupationProgress = 0f;
        ApplyTerritoryVisuals();
    }

    public void SetStationedMemberCount(int memberCount)
    {
        stationedMemberCount = Mathf.Max(0, memberCount);
    }

    public void TickOccupation(float deltaTime)
    {
        if (occupationState != AllianceOccupationState.Occupying)
            return;

        float memberMultiplier = 1f + Mathf.Clamp(stationedMemberCount, 0, 20) * 0.18f;
        occupationProgress += deltaTime * memberMultiplier;

        if (occupationProgress < baseOccupationSeconds)
            return;

        controllingAllianceId = occupyingAllianceId;
        controllingColor = occupyingColor;
        occupyingAllianceId = string.Empty;
        occupationProgress = baseOccupationSeconds;
        occupationState = AllianceOccupationState.Occupied;
        ApplyTerritoryVisuals();
    }

    public float GetOccupationPercent()
    {
        if (baseOccupationSeconds <= 0f)
            return 1f;

        return Mathf.Clamp01(occupationProgress / baseOccupationSeconds);
    }

    public float GetRemainingOccupationSeconds()
    {
        if (occupationState != AllianceOccupationState.Occupying)
            return 0f;

        return Mathf.Max(0f, baseOccupationSeconds - occupationProgress);
    }

    public string GetOwnerDisplayName()
    {
        if (occupationState == AllianceOccupationState.Occupied &&
            !string.IsNullOrWhiteSpace(controllingAllianceId))
        {
            return controllingAllianceId;
        }

        if (occupationState == AllianceOccupationState.Occupying &&
            !string.IsNullOrWhiteSpace(occupyingAllianceId))
        {
            return occupyingAllianceId + " isgal ediyor";
        }

        return "Tarafsiz";
    }

    public string GetOccupationStateDisplayName()
    {
        if (occupationState == AllianceOccupationState.Occupied)
            return "Isgal edildi";

        if (occupationState == AllianceOccupationState.Occupying)
            return "Isgal ediliyor";

        return "Tarafsiz";
    }

    public string GetBonusDisplayText()
    {
        if (data != null)
            return data.GetBonusDisplayText();

        if (bonusType == WorldCityBonusType.None || bonusPercent <= 0f)
            return "Bonus yok";

        string label = bonusType switch
        {
            WorldCityBonusType.SteelProduction => "Celik uretimi",
            WorldCityBonusType.OilProduction => "Petrol uretimi",
            WorldCityBonusType.BorProduction => "Bor uretimi",
            WorldCityBonusType.ResearchSpeed => "Arastirma hizi",
            WorldCityBonusType.LogisticsSpeed => "Lojistik hizi",
            WorldCityBonusType.UnitTrainingSpeed => "Egitim hizi",
            WorldCityBonusType.AllianceOccupationSpeed => "Isgal hizi",
            _ => "Bonus"
        };

        return $"{label} +%{bonusPercent * 100f:0}";
    }

    public string GetRequirementDisplayText()
    {
        if (data != null)
            return data.GetRequirementDisplayText();

        return $"Ittifak Lv.{requiredAllianceLevel} | Guc {requiredPower:N0}";
    }

    public void ApplyData()
    {
        if (data == null)
            return;

        cityId = data.cityId;
        displayName = data.displayName;
        level = data.level;
        regionName = data.regionName;
        influenceRadius = data.influenceRadius;
        baseOccupationSeconds = data.occupationSeconds;
        requiredAllianceLevel = data.requiredAllianceLevel;
        requiredPower = data.requiredPower;
        bonusType = data.bonusType;
        bonusPercent = data.bonusPercent;
    }

    public void RefreshVisual()
    {
        if (data == null || data.cityPrefab == null)
            return;

        if (visualRoot == null)
        {
            GameObject root = new GameObject("Visual");
            root.transform.SetParent(transform, false);
            visualRoot = root.transform;
        }

        if (runtimeVisual != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeVisual);
            else
                DestroyImmediate(runtimeVisual);
        }

        runtimeVisual = Instantiate(data.cityPrefab, visualRoot);
        runtimeVisual.name = data.cityPrefab.name + "_Visual";
        runtimeVisual.transform.localPosition = Vector3.zero;
        runtimeVisual.transform.localRotation = Quaternion.identity;
        DisableVisualColliders(runtimeVisual);
    }

    private void DisableVisualColliders(GameObject visual)
    {
        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null)
                collider.enabled = false;
        }
    }

    public void RebuildInfluence()
    {
        influenceHexes.Clear();

        if (gridManager == null)
            gridManager = FindAnyObjectByType<HexGridManager>();

        if (gridManager == null)
            return;

        cityHex = gridManager.GetClosestHex(transform.position);
        if (cityHex == null)
            return;

        cityHex.SetTerrain(HexTerrainType.City);

        foreach (HexCell hex in gridManager.GetAllHexes())
        {
            if (hex == null)
                continue;

            if (cityHex.GetDistance(hex) <= influenceRadius)
                influenceHexes.Add(hex);
        }
    }

    public void ApplyTerritoryVisuals()
    {
        if (influenceHexes.Count == 0)
            RebuildInfluence();

        string allianceId = occupationState == AllianceOccupationState.Occupied
            ? controllingAllianceId
            : occupyingAllianceId;

        Color color = occupationState == AllianceOccupationState.Occupied
            ? controllingColor
            : occupyingColor;

        foreach (HexCell hex in influenceHexes)
        {
            if (hex == null)
                continue;

            if (occupationState == AllianceOccupationState.Neutral)
                hex.SetTerritory(string.Empty, Color.clear);
            else
                hex.SetTerritory(allianceId, color);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = occupationState == AllianceOccupationState.Occupied
            ? controllingColor
            : occupyingColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.6f, 1.2f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.8f,
            $"{displayName}\n{occupationState} {(GetOccupationPercent() * 100f):0}%");
#endif
    }
}
