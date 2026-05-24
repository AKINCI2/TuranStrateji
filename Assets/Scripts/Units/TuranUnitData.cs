using UnityEngine;

[CreateAssetMenu(
    fileName = "UnitData_New",
    menuName = "Turan Strateji/Units/Unit Data")]
public class TuranUnitData : ScriptableObject
{
    [Header("Identity")]
    public string unitId = "unit_id";
    public string displayName = "Tim";
    public string weaponOrVehicleName = "Silah";
    public Sprite icon;
    public GameObject worldPrefab;
    public TuranNation nation = TuranNation.TuranCommon;
    public TuranRarity rarity = TuranRarity.Common;
    public WeaponData weaponData;
    public UnitVisualProfile visualProfile;

    [Header("Classification")]
    public TuranForceBranch branch = TuranForceBranch.LandForces;
    public TuranUnitKind kind = TuranUnitKind.Infantry;
    public TuranUnitEra era = TuranUnitEra.EarlyRepublic;
    public string techLine = "kara";

    [Header("Progression")]
    [Range(1, 5)]
    public int baseStars = 1;
    public int tier = 1;
    public TuranUnitData mergeResult;
    public int mergeRequiredCopies = 3;

    [Header("Stats")]
    public int power = 100;
    public int attack = 20;
    public int defense = 12;
    public int health = 100;
    public int marchSpeed = 4;

    [Header("Training")]
    public int baseSquadSize = 9;
    public float secondsPerSoldier = 1f;
    public ResourceCost trainCostPerSoldier;

    public bool CanMergeIntoNext()
    {
        return mergeResult != null && mergeRequiredCopies > 0;
    }

    public ResourceCost GetTrainingCost(int soldierCount)
    {
        return new ResourceCost
        {
            gold = trainCostPerSoldier.gold * soldierCount,
            turanCoin = trainCostPerSoldier.turanCoin * soldierCount,
            steel = trainCostPerSoldier.steel * soldierCount,
            oil = trainCostPerSoldier.oil * soldierCount,
            bor = trainCostPerSoldier.bor * soldierCount
        };
    }
}

