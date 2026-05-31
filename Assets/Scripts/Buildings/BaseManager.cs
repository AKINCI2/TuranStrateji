using System.Collections.Generic;
using System;
using UnityEngine;

public class BaseManager : MonoBehaviour
{
    public static BaseManager Instance;

    [Header("Resources")]
    public ResourceCost wallet =
        new ResourceCost
        {
            gold = 1000,
            turanCoin = 0,
            steel = 1000,
            oil = 1000,
            bor = 250
        };

    [Header("Buildings")]
    public BaseBuilding headquarters;
    public List<BaseBuilding> buildings = new List<BaseBuilding>();

    public int HeadquartersLevel =>
        headquarters != null ? headquarters.currentLevel : 1;

    public event Action<ResourceCost> ResourcesChanged;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        RegisterSceneBuildings();
        NotifyResourcesChanged();
    }

    public void RegisterBuilding(BaseBuilding building)
    {
        if (building == null)
            return;

        if (!buildings.Contains(building))
            buildings.Add(building);

        if (building.data != null &&
            building.data.type == BuildingType.Headquarters)
        {
            headquarters = building;
        }
    }

    public void UnregisterBuilding(BaseBuilding building)
    {
        if (building == null)
            return;

        buildings.Remove(building);

        if (headquarters == building)
            headquarters = null;
    }

    public bool TryUpgrade(BaseBuilding building)
    {
        if (building == null)
            return false;

        bool started =
            building.StartUpgrade(HeadquartersLevel, ref wallet);

        if (started)
            NotifyResourcesChanged();

        return started;
    }

    public bool CanAfford(ResourceCost cost)
    {
        return cost.CanAfford(wallet);
    }

    public void AddResources(ResourceCost amount)
    {
        wallet += amount;
        NotifyResourcesChanged();
    }

    public bool TrySpend(ResourceCost cost)
    {
        if (!CanAfford(cost))
            return false;

        wallet -= cost;
        NotifyResourcesChanged();
        return true;
    }

    public void NotifyResourcesChanged()
    {
        ResourcesChanged?.Invoke(wallet);
    }

    void RegisterSceneBuildings()
    {
        buildings.Clear();

        BaseBuilding[] sceneBuildings =
            FindObjectsByType<BaseBuilding>(FindObjectsInactive.Exclude);

        foreach (BaseBuilding building in sceneBuildings)
        {
            if (building == null || building.data == null)
                continue;

            if (building.data.type != BuildingType.Headquarters)
                continue;

            RegisterBuilding(building);
        }
    }
}

