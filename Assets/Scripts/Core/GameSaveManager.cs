using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [Header("Save")]
    public string saveFileName = "turan_save.json";
    public bool loadOnStart = true;
    public bool saveOnQuit = true;
    public float autosaveInterval = 30f;

    private float nextAutosaveTime;
    private bool loadAttempted;

    public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        nextAutosaveTime = Time.time + Mathf.Max(5f, autosaveInterval);

        if (loadOnStart)
            StartCoroutine(LoadWhenManagersAreReady());
    }

    void Update()
    {
        if (autosaveInterval <= 0f || Time.time < nextAutosaveTime)
            return;

        SaveGame();
        nextAutosaveTime = Time.time + Mathf.Max(5f, autosaveInterval);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && saveOnQuit)
            SaveGame();
    }

    void OnApplicationQuit()
    {
        if (saveOnQuit)
            SaveGame();
    }

    public void SaveGame()
    {
        GameSaveData data = CaptureSaveData();
        string json = JsonUtility.ToJson(data, true);

        Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
        File.WriteAllText(SavePath, json);
    }

    public bool LoadGame()
    {
        loadAttempted = true;

        if (!File.Exists(SavePath))
            return false;

        string json = File.ReadAllText(SavePath);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null)
            return false;

        ApplySaveData(data);
        return true;
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    private IEnumerator LoadWhenManagersAreReady()
    {
        for (int i = 0; i < 5; i++)
            yield return null;

        if (!loadAttempted)
            LoadGame();
    }

    private GameSaveData CaptureSaveData()
    {
        GameSaveData data = new GameSaveData();

        if (BaseManager.Instance != null)
            data.wallet = BaseManager.Instance.wallet;

        MaterialInventoryManager inventory = MaterialInventoryManager.Instance;
        if (inventory != null)
        {
            data.materials = new MaterialInventorySave
            {
                wood = inventory.wood,
                concrete = inventory.concrete,
                cement = inventory.cement,
                brick = inventory.brick,
                mergeCoupons = inventory.mergeCoupons,
                speedupsMinutes = inventory.speedupsMinutes,
                rewardChests = inventory.rewardChests
            };
        }

        ProductionFacilityManager production = ProductionFacilityManager.Instance;
        if (production != null)
        {
            data.productionSlotCount = production.slotCount;
            foreach (ProductionSlot slot in production.slots)
            {
                if (slot == null)
                    continue;

                data.productionSlots.Add(new ProductionSlotSave
                {
                    isProducing = slot.isProducing,
                    isReadyToCollect = slot.isReadyToCollect,
                    recipeType = slot.recipeType,
                    remainingSeconds = slot.remainingSeconds,
                    totalSeconds = slot.totalSeconds
                });
            }
        }

        BaseBuilding[] buildings = FindObjectsByType<BaseBuilding>(FindObjectsInactive.Include);
        foreach (BaseBuilding building in buildings)
        {
            if (building == null || building.data == null)
                continue;

            data.buildings.Add(new BuildingSave
            {
                key = GetBuildingKey(building),
                type = building.data.type,
                currentLevel = building.currentLevel,
                isUpgrading = building.isUpgrading,
                upgradeRemainingSeconds = building.upgradeRemainingSeconds,
                active = building.gameObject.activeSelf
            });

            BarracksBuilding barracks = building.GetComponent<BarracksBuilding>();
            if (barracks == null)
                continue;

            data.barracks.Add(new BarracksSave
            {
                key = GetBuildingKey(building),
                assignedUnitId = barracks.assignedUnitData != null ? barracks.assignedUnitData.unitId : "",
                assignedOfficerId = barracks.assignedOfficer != null ? barracks.assignedOfficer.officerId : "",
                trainedSoldiers = barracks.trainedSoldiers,
                isTraining = barracks.isTraining,
                trainingRemainingSeconds = barracks.trainingRemainingSeconds,
                trainingSoldierCount = barracks.trainingSoldierCount
            });
        }

        return data;
    }

    private void ApplySaveData(GameSaveData data)
    {
        if (BaseManager.Instance != null)
        {
            BaseManager.Instance.wallet = data.wallet;
            BaseManager.Instance.NotifyResourcesChanged();
        }

        MaterialInventoryManager inventory = MaterialInventoryManager.Instance;
        if (inventory != null && data.materials != null)
        {
            inventory.SetValues(
                data.materials.wood,
                data.materials.concrete,
                data.materials.cement,
                data.materials.brick,
                data.materials.mergeCoupons,
                data.materials.speedupsMinutes,
                data.materials.rewardChests
            );
        }

        ApplyProduction(data);
        ApplyBuildings(data);
        ApplyBarracks(data);
    }

    private void ApplyProduction(GameSaveData data)
    {
        ProductionFacilityManager production = ProductionFacilityManager.Instance;
        if (production == null || data.productionSlots == null)
            return;

        production.EnsureSlotCount(Mathf.Max(data.productionSlotCount, data.productionSlots.Count));

        for (int i = 0; i < production.slots.Count && i < data.productionSlots.Count; i++)
        {
            ProductionSlot slot = production.slots[i];
            ProductionSlotSave save = data.productionSlots[i];

            if (slot == null || save == null)
                continue;

            slot.isProducing = save.isProducing;
            slot.isReadyToCollect = save.isReadyToCollect;
            slot.recipeType = save.recipeType;
            slot.remainingSeconds = Mathf.Max(0f, save.remainingSeconds);
            slot.totalSeconds = Mathf.Max(0f, save.totalSeconds);
        }

        production.NotifyChanged();
    }

    private void ApplyBuildings(GameSaveData data)
    {
        if (data.buildings == null)
            return;

        BaseBuilding[] sceneBuildings = FindObjectsByType<BaseBuilding>(FindObjectsInactive.Include);
        foreach (BuildingSave save in data.buildings)
        {
            BaseBuilding building = FindBuilding(sceneBuildings, save.key, save.type);
            if (building == null)
                continue;

            building.currentLevel = Mathf.Max(1, save.currentLevel);
            building.isUpgrading = save.isUpgrading;
            building.upgradeRemainingSeconds = Mathf.Max(0f, save.upgradeRemainingSeconds);
            building.gameObject.SetActive(save.active);
            building.RefreshVisual();
            building.RebuildClickableCollider();

            if (BaseManager.Instance != null)
                BaseManager.Instance.RegisterBuilding(building);
        }
    }

    private void ApplyBarracks(GameSaveData data)
    {
        if (data.barracks == null)
            return;

        TuranUnitData[] units = Resources.LoadAll<TuranUnitData>("Data/Units");
        OfficerData[] officers = Resources.LoadAll<OfficerData>("Data/Officers");
        BarracksBuilding[] barracksBuildings = FindObjectsByType<BarracksBuilding>(FindObjectsInactive.Include);

        foreach (BarracksSave save in data.barracks)
        {
            BarracksBuilding barracks = FindBarracks(barracksBuildings, save.key);
            if (barracks == null)
                continue;

            barracks.assignedUnitData = FindUnitById(units, save.assignedUnitId);
            barracks.assignedOfficer = FindOfficerById(officers, save.assignedOfficerId);
            barracks.trainedSoldiers = Mathf.Max(0, save.trainedSoldiers);
            barracks.isTraining = save.isTraining;
            barracks.trainingRemainingSeconds = Mathf.Max(0f, save.trainingRemainingSeconds);
            barracks.trainingSoldierCount = Mathf.Max(0, save.trainingSoldierCount);
            barracks.EnsureRepresentativeUnit();
        }
    }

    private string GetBuildingKey(BaseBuilding building)
    {
        BuildingSlot slot = building.GetComponentInParent<BuildingSlot>();
        if (slot != null && !string.IsNullOrWhiteSpace(slot.slotId))
            return slot.slotId;

        return building.name;
    }

    private BaseBuilding FindBuilding(BaseBuilding[] buildings, string key, BuildingType type)
    {
        foreach (BaseBuilding building in buildings)
        {
            if (building == null || building.data == null || building.data.type != type)
                continue;

            if (GetBuildingKey(building) == key)
                return building;
        }

        return null;
    }

    private BarracksBuilding FindBarracks(BarracksBuilding[] barracksBuildings, string key)
    {
        foreach (BarracksBuilding barracks in barracksBuildings)
        {
            if (barracks == null)
                continue;

            BaseBuilding building = barracks.GetComponent<BaseBuilding>();
            if (building != null && GetBuildingKey(building) == key)
                return barracks;
        }

        return null;
    }

    private TuranUnitData FindUnitById(TuranUnitData[] units, string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
            return null;

        foreach (TuranUnitData unit in units)
        {
            if (unit != null && unit.unitId == unitId)
                return unit;
        }

        return null;
    }

    private OfficerData FindOfficerById(OfficerData[] officers, string officerId)
    {
        if (string.IsNullOrWhiteSpace(officerId))
            return null;

        foreach (OfficerData officer in officers)
        {
            if (officer != null && officer.officerId == officerId)
                return officer;
        }

        return null;
    }
}

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public ResourceCost wallet;
    public MaterialInventorySave materials = new MaterialInventorySave();
    public int productionSlotCount;
    public List<ProductionSlotSave> productionSlots = new List<ProductionSlotSave>();
    public List<BuildingSave> buildings = new List<BuildingSave>();
    public List<BarracksSave> barracks = new List<BarracksSave>();
}

[Serializable]
public class MaterialInventorySave
{
    public int wood;
    public int concrete;
    public int cement;
    public int brick;
    public int mergeCoupons;
    public int speedupsMinutes;
    public int rewardChests;
}

[Serializable]
public class ProductionSlotSave
{
    public bool isProducing;
    public bool isReadyToCollect;
    public ConstructionMaterialType recipeType;
    public float remainingSeconds;
    public float totalSeconds;
}

[Serializable]
public class BuildingSave
{
    public string key;
    public BuildingType type;
    public int currentLevel;
    public bool isUpgrading;
    public float upgradeRemainingSeconds;
    public bool active;
}

[Serializable]
public class BarracksSave
{
    public string key;
    public string assignedUnitId;
    public string assignedOfficerId;
    public int trainedSoldiers;
    public bool isTraining;
    public float trainingRemainingSeconds;
    public int trainingSoldierCount;
}
