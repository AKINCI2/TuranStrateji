using System;
using UnityEngine;

public enum BuildingType
{
    Headquarters,
    Barracks,
    ProductionFacility,
    SteelFactory,
    OilRefinery,
    BorMine,
    ResearchCenter,
    Warehouse
}

[CreateAssetMenu(
    fileName = "BuildingData_New",
    menuName = "Turan Strateji/Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identity")]
    public string buildingId = "building_id";
    public string displayName = "Bina";
    public BuildingType type;
    public Sprite icon;

    [Header("Levels")]
    public BuildingLevelData[] levels;

    public int MaxLevel =>
        levels == null ? 0 : levels.Length;

    public BuildingLevelData GetLevelData(int level)
    {
        if (levels == null || levels.Length == 0)
            return null;

        int index =
            Mathf.Clamp(level - 1, 0, levels.Length - 1);

        return levels[index];
    }

    public BuildingLevelData GetNextLevelData(int currentLevel)
    {
        if (levels == null || levels.Length == 0)
            return null;

        int nextLevel =
            currentLevel + 1;

        if (nextLevel > levels.Length)
            return null;

        return GetLevelData(nextLevel);
    }
}

[Serializable]
public class BuildingLevelData
{
    public int level = 1;
    public GameObject visualPrefab;
    public Vector3 visualScale = Vector3.one;
    public Vector3 visualRotation = Vector3.zero;
    public int requiredHeadquartersLevel = 1;
    public int powerReward;
    public int upgradeSeconds;
    public ResourceCost upgradeCost;
}

