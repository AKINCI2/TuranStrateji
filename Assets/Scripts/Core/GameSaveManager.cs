using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [Header("Local Debug Save")]
    public bool editorOnly = true;
    public string saveFileName = "turan_save.json";
    public bool loadOnStart = false;
    public bool saveOnQuit = false;
    public float autosaveInterval = 0f;

    private float nextAutosaveTime;
    private bool loadAttempted;

    public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    void Awake()
    {
        if (editorOnly && !Application.isEditor)
        {
            Destroy(gameObject);
            return;
        }

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

        CaptureWorldResourceNodes(data);
        CaptureWorldCities(data);
        CaptureWorldUnits(data);

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
        ApplyWorldResourceNodes(data);
        ApplyWorldCities(data);
        ApplyWorldUnits(data);
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

    private void CaptureWorldResourceNodes(GameSaveData data)
    {
        WorldResourceNodeManager manager =
            WorldResourceNodeManager.Instance != null
                ? WorldResourceNodeManager.Instance
                : FindAnyObjectByType<WorldResourceNodeManager>();

        if (manager == null)
            return;

        foreach (WorldResourceNode node in manager.GetAllNodesSnapshot())
        {
            if (node == null || node.hex == null)
                continue;

            data.worldResourceNodes.Add(new WorldResourceNodeSave
            {
                resourceType = node.resourceType,
                level = node.level,
                amount = node.amount,
                remainingAmount = node.remainingAmount,
                q = node.hex.axialCoord.x,
                r = node.hex.axialCoord.y,
                isCollected = node.isCollected,
                requiresTimedGathering = node.requiresTimedGathering,
                gatherDurationSeconds = node.gatherDurationSeconds,
                gatherPowerPerSecond = node.gatherPowerPerSecond,
                hasGuard = node.hasGuard,
                guardMaxHealth = node.guardMaxHealth,
                guardHealth = node.guardHealth,
                guardDamage = node.guardDamage,
                firstClearReward = node.firstClearReward,
                firstClearRewardGranted = node.firstClearRewardGranted
            });
        }
    }

    private void ApplyWorldResourceNodes(GameSaveData data)
    {
        if (data.worldResourceNodes == null || data.worldResourceNodes.Count == 0)
            return;

        WorldResourceNodeManager manager =
            WorldResourceNodeManager.Instance != null
                ? WorldResourceNodeManager.Instance
                : FindAnyObjectByType<WorldResourceNodeManager>();

        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        if (manager == null || grid == null)
            return;

        manager.EnsureGenerated();

        foreach (WorldResourceNodeSave save in data.worldResourceNodes)
        {
            if (save == null)
                continue;

            HexCell hex = grid.GetHexAt(save.q, save.r);
            if (hex == null)
                continue;

            WorldResourceNode node =
                manager.GetNodeAtHex(hex) ??
                manager.GetOrCreateSavedNode(
                    save.resourceType,
                    save.level,
                    save.amount,
                    hex
                );

            if (node == null)
                continue;

            node.ApplySavedState(
                save.resourceType,
                save.level,
                save.amount,
                save.remainingAmount,
                save.isCollected,
                save.requiresTimedGathering,
                save.gatherDurationSeconds,
                save.gatherPowerPerSecond,
                save.hasGuard,
                save.guardMaxHealth,
                save.guardHealth,
                save.guardDamage,
                save.firstClearReward,
                save.firstClearRewardGranted
            );
        }
    }

    private void CaptureWorldCities(GameSaveData data)
    {
        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        WorldCityNode[] cities = FindObjectsByType<WorldCityNode>(FindObjectsInactive.Include);

        foreach (WorldCityNode city in cities)
        {
            if (city == null)
                continue;

            HexCell hex = IsGridReady(grid)
                ? grid.GetClosestHex(city.transform.position)
                : null;

            data.worldCities.Add(new WorldCitySave
            {
                cityId = city.cityId,
                q = hex != null ? hex.axialCoord.x : 0,
                r = hex != null ? hex.axialCoord.y : 0,
                hasHex = hex != null,
                controllingAllianceId = city.controllingAllianceId,
                controllingColor = new SerializableColor(city.controllingColor),
                occupationState = city.occupationState,
                occupyingAllianceId = city.occupyingAllianceId,
                occupyingColor = new SerializableColor(city.occupyingColor),
                stationedMemberCount = city.stationedMemberCount,
                occupationProgress = city.occupationProgress
            });
        }
    }

    private void ApplyWorldCities(GameSaveData data)
    {
        if (data.worldCities == null || data.worldCities.Count == 0)
            return;

        WorldCityNode[] cities = FindObjectsByType<WorldCityNode>(FindObjectsInactive.Include);

        foreach (WorldCitySave save in data.worldCities)
        {
            if (save == null)
                continue;

            WorldCityNode city = FindCity(cities, save);
            if (city == null)
                continue;

            city.controllingAllianceId = save.controllingAllianceId ?? string.Empty;
            city.controllingColor = save.controllingColor.ToColor();
            city.occupationState = save.occupationState;
            city.occupyingAllianceId = save.occupyingAllianceId ?? string.Empty;
            city.occupyingColor = save.occupyingColor.ToColor();
            city.stationedMemberCount = Mathf.Max(0, save.stationedMemberCount);

            float maxProgress = Mathf.Max(0f, city.baseOccupationSeconds);
            city.occupationProgress = maxProgress > 0f
                ? Mathf.Clamp(save.occupationProgress, 0f, maxProgress)
                : Mathf.Max(0f, save.occupationProgress);

            city.RebuildInfluence();
            city.ApplyTerritoryVisuals();
        }
    }

    private void CaptureWorldUnits(GameSaveData data)
    {
        UnitManager unitManager =
            UnitManager.Instance != null
                ? UnitManager.Instance
                : FindAnyObjectByType<UnitManager>();

        List<UnitController> units =
            unitManager != null && unitManager.units != null
                ? unitManager.units
                : new List<UnitController>(FindObjectsByType<UnitController>(FindObjectsInactive.Include));

        HexGridManager grid = FindAnyObjectByType<HexGridManager>();

        foreach (UnitController unit in units)
        {
            if (unit == null ||
                unit.state == UnitState.Death ||
                unit.deploymentState != UnitDeploymentState.OnWorldMap)
            {
                continue;
            }

            HexCell hex = unit.currentHex;
            if (hex == null && IsGridReady(grid))
                hex = grid.GetClosestHex(unit.transform.position);

            Health health = unit.GetComponent<Health>();

            data.worldUnits.Add(new WorldUnitSave
            {
                key = GetUnitKey(unit),
                unitName = unit.name,
                unitId = unit.unitData != null ? unit.unitData.unitId : string.Empty,
                assignedBarracksKey = GetBarracksKey(unit.assignedBarracks),
                isEnemy = unit.CompareTag("Enemy"),
                q = hex != null ? hex.axialCoord.x : 0,
                r = hex != null ? hex.axialCoord.y : 0,
                hasHex = hex != null,
                x = unit.transform.position.x,
                y = unit.transform.position.y,
                z = unit.transform.position.z,
                currentHealth = health != null ? health.currentHealth : -1
            });
        }
    }

    private void ApplyWorldUnits(GameSaveData data)
    {
        if (data.worldUnits == null || data.worldUnits.Count == 0)
            return;

        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        BarracksBuilding[] barracksBuildings = FindObjectsByType<BarracksBuilding>(FindObjectsInactive.Include);
        UnitController[] sceneUnits = FindObjectsByType<UnitController>(FindObjectsInactive.Include);

        foreach (WorldUnitSave save in data.worldUnits)
        {
            if (save == null)
                continue;

            UnitController unit = FindUnit(sceneUnits, save);

            if (unit == null && !save.isEnemy)
            {
                BarracksBuilding barracks = FindBarracks(barracksBuildings, save.assignedBarracksKey);
                if (barracks != null)
                    unit = barracks.EnsureRepresentativeUnit();
            }

            if (unit == null)
                continue;

            BarracksBuilding assignedBarracks = FindBarracks(barracksBuildings, save.assignedBarracksKey);
            if (assignedBarracks != null)
                unit.assignedBarracks = assignedBarracks;

            Vector3 position = new Vector3(save.x, save.y, save.z);
            if (save.hasHex && grid != null)
            {
                HexCell hex = grid.GetHexAt(save.q, save.r);
                if (hex != null)
                {
                    position = grid.GetHexCenter(hex);
                    position.y = save.y > 0f ? save.y : 0.5f;
                }
            }

            Transform worldParent =
                GameModeManager.Instance != null && GameModeManager.Instance.worldUnitsRoot != null
                    ? GameModeManager.Instance.worldUnitsRoot.transform
                    : null;

            if (worldParent != null)
                unit.transform.SetParent(worldParent);

            unit.PlaceOnWorld(position);
            unit.state = UnitState.Idle;

            if (assignedBarracks != null)
                assignedBarracks.MarkUnitOnMap(unit);

            Health health = unit.GetComponent<Health>();
            if (health != null && save.currentHealth >= 0)
                health.SetCurrentHealth(Mathf.Max(1, save.currentHealth));

            if (UnitManager.Instance != null)
            {
                UnitManager.Instance.RegisterUnit(unit);
                UnitManager.Instance.RefreshUnitVisibility(unit);
            }
        }
    }

    private string GetBuildingKey(BaseBuilding building)
    {
        BuildingSlot slot = building.GetComponentInParent<BuildingSlot>();
        if (slot != null && !string.IsNullOrWhiteSpace(slot.slotId))
            return slot.slotId;

        return building.name;
    }

    private string GetBarracksKey(BarracksBuilding barracks)
    {
        if (barracks == null)
            return string.Empty;

        BaseBuilding building = barracks.GetComponent<BaseBuilding>();
        return building != null ? GetBuildingKey(building) : barracks.name;
    }

    private string GetUnitKey(UnitController unit)
    {
        if (unit == null)
            return string.Empty;

        string unitId = unit.unitData != null ? unit.unitData.unitId : string.Empty;
        string barracksKey = GetBarracksKey(unit.assignedBarracks);

        if (!string.IsNullOrWhiteSpace(barracksKey))
            return "barracks:" + barracksKey + ":" + unitId;

        string prefix = unit.CompareTag("Enemy") ? "enemy:" : "unit:";
        return prefix + unit.name;
    }

    private bool IsGridReady(HexGridManager grid)
    {
        return grid != null &&
               grid.allHexCells != null &&
               grid.allHexCells.Count > 0;
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

    private WorldCityNode FindCity(WorldCityNode[] cities, WorldCitySave save)
    {
        foreach (WorldCityNode city in cities)
        {
            if (city == null)
                continue;

            if (!string.IsNullOrWhiteSpace(save.cityId) &&
                city.cityId == save.cityId)
            {
                return city;
            }
        }

        if (!save.hasHex)
            return null;

        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        if (!IsGridReady(grid))
            return null;

        foreach (WorldCityNode city in cities)
        {
            if (city == null)
                continue;

            HexCell hex = grid.GetClosestHex(city.transform.position);
            if (hex != null &&
                hex.axialCoord.x == save.q &&
                hex.axialCoord.y == save.r)
            {
                return city;
            }
        }

        return null;
    }

    private UnitController FindUnit(UnitController[] units, WorldUnitSave save)
    {
        foreach (UnitController unit in units)
        {
            if (unit == null)
                continue;

            if (!string.IsNullOrWhiteSpace(save.key) &&
                GetUnitKey(unit) == save.key)
            {
                return unit;
            }
        }

        foreach (UnitController unit in units)
        {
            if (unit == null)
                continue;

            if (save.isEnemy)
            {
                if (unit.CompareTag("Enemy") && unit.name == save.unitName)
                    return unit;

                continue;
            }

            if (!string.IsNullOrWhiteSpace(save.assignedBarracksKey) &&
                GetBarracksKey(unit.assignedBarracks) == save.assignedBarracksKey)
            {
                string unitId = unit.unitData != null ? unit.unitData.unitId : string.Empty;
                if (string.IsNullOrWhiteSpace(save.unitId) || unitId == save.unitId)
                    return unit;
            }
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
    public int version = 2;
    public ResourceCost wallet;
    public MaterialInventorySave materials = new MaterialInventorySave();
    public int productionSlotCount;
    public List<ProductionSlotSave> productionSlots = new List<ProductionSlotSave>();
    public List<BuildingSave> buildings = new List<BuildingSave>();
    public List<BarracksSave> barracks = new List<BarracksSave>();
    public List<WorldResourceNodeSave> worldResourceNodes = new List<WorldResourceNodeSave>();
    public List<WorldCitySave> worldCities = new List<WorldCitySave>();
    public List<WorldUnitSave> worldUnits = new List<WorldUnitSave>();
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

[Serializable]
public class WorldResourceNodeSave
{
    public WorldResourceType resourceType;
    public int level;
    public int amount;
    public int remainingAmount;
    public int q;
    public int r;
    public bool isCollected;
    public bool requiresTimedGathering;
    public float gatherDurationSeconds;
    public int gatherPowerPerSecond;
    public bool hasGuard;
    public int guardMaxHealth;
    public int guardHealth;
    public int guardDamage;
    public ResourceCost firstClearReward;
    public bool firstClearRewardGranted;
}

[Serializable]
public class WorldCitySave
{
    public string cityId;
    public int q;
    public int r;
    public bool hasHex;
    public string controllingAllianceId;
    public SerializableColor controllingColor;
    public AllianceOccupationState occupationState;
    public string occupyingAllianceId;
    public SerializableColor occupyingColor;
    public int stationedMemberCount;
    public float occupationProgress;
}

[Serializable]
public class WorldUnitSave
{
    public string key;
    public string unitName;
    public string unitId;
    public string assignedBarracksKey;
    public bool isEnemy;
    public int q;
    public int r;
    public bool hasHex;
    public float x;
    public float y;
    public float z;
    public int currentHealth;
}

[Serializable]
public struct SerializableColor
{
    public float r;
    public float g;
    public float b;
    public float a;

    public SerializableColor(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }

    public Color ToColor()
    {
        return new Color(r, g, b, a);
    }
}
