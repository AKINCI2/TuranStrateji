using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RewardChest_New",
    menuName = "Turan Strateji/Rewards/Reward Chest")]
public class RewardChestData : ScriptableObject
{
    public string chestId = "chest_id";
    public string displayName = "Sandik";
    public TuranNation nation = TuranNation.TuranCommon;
    public List<UnitRewardEntry> unitRewards = new List<UnitRewardEntry>();
}

[Serializable]
public class UnitRewardEntry
{
    public TuranUnitData unitData;
    public int copyAmount = 1;
    public int weight = 10;
}
