using UnityEngine;

public class RewardChestManager : MonoBehaviour
{
    public static RewardChestManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public bool OpenChest(RewardChestData chest)
    {
        if (chest == null || PlayerRosterManager.Instance == null)
            return false;

        UnitRewardEntry reward = RollUnitReward(chest);
        if (reward == null || reward.unitData == null)
            return false;

        PlayerRosterManager.Instance.AddUnitCopies(reward.unitData, Mathf.Max(1, reward.copyAmount));
        Debug.Log($"{chest.displayName} acildi: {reward.unitData.displayName} x{reward.copyAmount}");
        return true;
    }

    private UnitRewardEntry RollUnitReward(RewardChestData chest)
    {
        int totalWeight = 0;
        foreach (UnitRewardEntry entry in chest.unitRewards)
        {
            if (entry != null && entry.unitData != null && entry.weight > 0)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        int cursor = 0;

        foreach (UnitRewardEntry entry in chest.unitRewards)
        {
            if (entry == null || entry.unitData == null || entry.weight <= 0)
                continue;

            cursor += entry.weight;
            if (roll < cursor)
                return entry;
        }

        return null;
    }
}
