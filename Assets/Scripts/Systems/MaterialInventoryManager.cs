using System;
using UnityEngine;

public enum ConstructionMaterialType
{
    Wood,
    Concrete,
    Cement,
    Brick
}

public class MaterialInventoryManager : MonoBehaviour
{
    public static MaterialInventoryManager Instance;

    [Header("Construction Materials")]
    public int wood;
    public int concrete;
    public int cement;
    public int brick;

    [Header("Bag Items")]
    public int mergeCoupons;
    public int speedupsMinutes;
    public int rewardChests;

    public event Action InventoryChanged;

    void Awake()
    {
        Instance = this;
    }

    public void Add(ConstructionMaterialType type, int amount)
    {
        if (amount <= 0)
            return;

        if (type == ConstructionMaterialType.Wood)
            wood += amount;
        else if (type == ConstructionMaterialType.Concrete)
            concrete += amount;
        else if (type == ConstructionMaterialType.Cement)
            cement += amount;
        else if (type == ConstructionMaterialType.Brick)
            brick += amount;

        InventoryChanged?.Invoke();
    }

    public void SetValues(
        int wood,
        int concrete,
        int cement,
        int brick,
        int mergeCoupons,
        int speedupsMinutes,
        int rewardChests)
    {
        this.wood = Mathf.Max(0, wood);
        this.concrete = Mathf.Max(0, concrete);
        this.cement = Mathf.Max(0, cement);
        this.brick = Mathf.Max(0, brick);
        this.mergeCoupons = Mathf.Max(0, mergeCoupons);
        this.speedupsMinutes = Mathf.Max(0, speedupsMinutes);
        this.rewardChests = Mathf.Max(0, rewardChests);

        InventoryChanged?.Invoke();
    }

    public string GetDisplayText()
    {
        return
            $"Malzemeler\n" +
            $"Kalas: {wood}   Beton: {concrete}   Cimento: {cement}   Tugla: {brick}\n" +
            $"Kuponlar: {mergeCoupons}   Hizlandirma: {speedupsMinutes} dk   Sandik: {rewardChests}";
    }
}

