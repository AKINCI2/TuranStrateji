using UnityEngine;

[CreateAssetMenu(menuName = "Turan Strateji/World/City Data", fileName = "WC_NewCity")]
public class WorldCityData : ScriptableObject
{
    [Header("Identity")]
    public string cityId = "city_node";
    public string displayName = "Stratejik Sehir";
    public int level = 1;
    public string regionName = "Turan Bolgesi";

    [Header("Map")]
    public int influenceRadius = 4;
    public float occupationSeconds = 180f;
    public GameObject cityPrefab;
    public GameObject cityMapPrefab;
    public string cityMapSceneName;
    public bool useAsyncSceneLoading;

    [Header("Requirements")]
    public int requiredAllianceLevel = 1;
    public int requiredPower = 5000;

    [Header("Bonus")]
    public WorldCityBonusType bonusType = WorldCityBonusType.None;
    [Range(0f, 1f)]
    public float bonusPercent = 0.05f;

    public string GetBonusDisplayText()
    {
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
        return $"Ittifak Lv.{requiredAllianceLevel} | Guc {requiredPower:N0}";
    }
}
