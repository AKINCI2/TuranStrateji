using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseConstructionManager : MonoBehaviour
{
    public static BaseConstructionManager Instance;

    [Header("Roots")]
    public Transform slotRoot;

    [Header("Definitions")]
    public List<ConstructionDefinition> definitions = new List<ConstructionDefinition>();

    [Header("Runtime")]
    public List<BuildingSlot> slots = new List<BuildingSlot>();

    private readonly List<BaseBuilding> hiddenSceneBuildings = new List<BaseBuilding>();

    public event Action ConstructionChanged;

    void Awake()
    {
        Instance = this;
        EnsureDefaults();
    }

    void Start()
    {
        EnsureSlotRoot();
        RefreshSlots();
        HideUnconstructedSceneBuildings();
        AttachPlacedBuildingsToSlots();
        ConstructionChanged?.Invoke();
    }

    public IReadOnlyList<ConstructionDefinition> Definitions => definitions;

    public bool TryConstruct(BuildingType type)
    {
        ConstructionDefinition definition = GetDefinition(type);
        if (definition == null)
            return false;

        string reason;
        if (!CanConstruct(definition, out reason))
        {
            Debug.Log("Insa edilemedi: " + reason);
            return false;
        }

        BuildingSlot slot = GetFreeSlot(definition.type);
        if (slot == null || definition.prefab == null || BaseManager.Instance == null)
            return false;

        if (!BaseManager.Instance.TrySpend(definition.cost))
            return false;

        GameObject instance = TryTakeHiddenSceneBuilding(definition.type);
        if (instance == null)
            instance = Instantiate(definition.prefab, slot.transform);
        else
            instance.transform.SetParent(slot.transform, false);

        instance.name = definition.displayName;
        instance.SetActive(true);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        BaseBuilding building = instance.GetComponent<BaseBuilding>();
        if (building == null)
            building = instance.AddComponent<BaseBuilding>();

        if (building.data == null)
            building.data = definition.buildingData;

        slot.Place(building);
        NormalizeCollider(building);
        BaseManager.Instance.RegisterBuilding(building);
        ConstructionChanged?.Invoke();
        return true;
    }

    public bool CanConstruct(ConstructionDefinition definition, out string reason)
    {
        reason = "";
        if (definition == null)
        {
            reason = "Tanim yok";
            return false;
        }

        if (BaseManager.Instance == null)
        {
            reason = "BaseManager yok";
            return false;
        }

        if (BaseManager.Instance.HeadquartersLevel < definition.requiredHeadquartersLevel)
        {
            reason = $"Komuta Merkezi Lv.{definition.requiredHeadquartersLevel} gerekli";
            return false;
        }

        int existingCount = CountBuildings(definition.type);
        if (existingCount >= definition.maxCount)
        {
            reason = "Limit dolu";
            return false;
        }

        if (definition.prefab == null)
        {
            reason = "Prefab hazir degil";
            return false;
        }

        if (!BaseManager.Instance.CanAfford(definition.cost))
        {
            reason = "Kaynak yetersiz";
            return false;
        }

        if (GetFreeSlot(definition.type) == null)
        {
            reason = "Bos slot yok";
            return false;
        }

        return true;
    }

    public string GetBuildStatus(ConstructionDefinition definition)
    {
        if (definition == null)
            return "";

        string reason;
        bool canBuild = CanConstruct(definition, out reason);
        int count = CountBuildings(definition.type);
        string status = canBuild ? "Hazir" : reason;

        return $"{definition.displayName}  {count}/{definition.maxCount}\n" +
               $"Gerekli: KM Lv.{definition.requiredHeadquartersLevel}  Maliyet: {definition.cost.ToDisplayString()}\n" +
               status;
    }

    public ConstructionDefinition GetDefinition(BuildingType type)
    {
        foreach (ConstructionDefinition definition in definitions)
        {
            if (definition != null && definition.type == type)
                return definition;
        }

        return null;
    }

    private int CountBuildings(BuildingType type)
    {
        int count = 0;

        if (BaseManager.Instance == null)
            return count;

        foreach (BaseBuilding building in BaseManager.Instance.buildings)
        {
            if (building != null && building.data != null && building.data.type == type)
                count++;
        }

        return count;
    }

    private BuildingSlot GetFreeSlot(BuildingType type)
    {
        RefreshSlots();
        int headquartersLevel = BaseManager.Instance != null ? BaseManager.Instance.HeadquartersLevel : 1;

        foreach (BuildingSlot slot in slots)
        {
            if (slot != null &&
                slot.allowedType == type &&
                slot.IsEmpty &&
                slot.CanUse(headquartersLevel))
            {
                return slot;
            }
        }

        return null;
    }

    private void RefreshSlots()
    {
        slots.Clear();
        slots.AddRange(FindObjectsByType<BuildingSlot>(FindObjectsInactive.Include));

        if (slots.Count == 0)
            CreateDefaultSlots();

        NormalizeSlotPositions();
    }

    private void AttachPlacedBuildingsToSlots()
    {
        if (BaseManager.Instance == null)
            return;

        foreach (BuildingSlot slot in slots)
        {
            if (slot == null || !slot.IsEmpty)
                continue;

            foreach (BaseBuilding building in BaseManager.Instance.buildings)
            {
                if (building == null || building.data == null)
                    continue;

                if (building.data.type != slot.allowedType)
                    continue;

                if (Vector3.Distance(building.transform.position, slot.transform.position) <= 1.75f)
                {
                    slot.Place(building);
                    NormalizeCollider(building);
                    break;
                }
            }
        }
    }

    private void HideUnconstructedSceneBuildings()
    {
        hiddenSceneBuildings.Clear();

        BaseBuilding[] sceneBuildings =
            FindObjectsByType<BaseBuilding>(FindObjectsInactive.Include);

        foreach (BaseBuilding building in sceneBuildings)
        {
            if (building == null || building.data == null)
                continue;

            if (building.data.type == BuildingType.Headquarters ||
                building.data.type == BuildingType.Barracks)
            {
                continue;
            }

            ConstructionDefinition definition = GetDefinition(building.data.type);
            if (definition == null)
                continue;

            BuildingSlot occupiedSlot = FindSlotForPlacedBuilding(building);
            if (occupiedSlot != null)
                continue;

            if (!hiddenSceneBuildings.Contains(building))
                hiddenSceneBuildings.Add(building);

            if (BaseManager.Instance != null)
                BaseManager.Instance.UnregisterBuilding(building);

            building.gameObject.SetActive(false);
        }
    }

    private BuildingSlot FindSlotForPlacedBuilding(BaseBuilding building)
    {
        if (building == null || building.data == null)
            return null;

        foreach (BuildingSlot slot in slots)
        {
            if (slot == null || slot.allowedType != building.data.type)
                continue;

            if (slot.placedBuilding == building)
                return slot;

            if (Vector3.Distance(building.transform.position, slot.transform.position) <= 1.75f)
                return slot;
        }

        return null;
    }

    private GameObject TryTakeHiddenSceneBuilding(BuildingType type)
    {
        for (int i = hiddenSceneBuildings.Count - 1; i >= 0; i--)
        {
            BaseBuilding building = hiddenSceneBuildings[i];
            if (building == null || building.data == null)
            {
                hiddenSceneBuildings.RemoveAt(i);
                continue;
            }

            if (building.data.type != type)
                continue;

            hiddenSceneBuildings.RemoveAt(i);
            return building.gameObject;
        }

        return null;
    }

    private void NormalizeCollider(BaseBuilding building)
    {
        if (building == null)
            return;

        building.RebuildClickableCollider();
    }

    private void CreateDefaultSlots()
    {
        Transform root = slotRoot != null ? slotRoot : transform;
        CreateSlot(root, "slot_barracks_01", BuildingType.Barracks, GetDefaultSlotPosition("slot_barracks_01", BuildingType.Barracks), 1);
        CreateSlot(root, "slot_production_01", BuildingType.ProductionFacility, GetDefaultSlotPosition("slot_production_01", BuildingType.ProductionFacility), 1);
        CreateSlot(root, "slot_barracks_02", BuildingType.Barracks, GetDefaultSlotPosition("slot_barracks_02", BuildingType.Barracks), 10);
        CreateSlot(root, "slot_research_01", BuildingType.ResearchCenter, GetDefaultSlotPosition("slot_research_01", BuildingType.ResearchCenter), 10);
        CreateSlot(root, "slot_warehouse_01", BuildingType.Warehouse, GetDefaultSlotPosition("slot_warehouse_01", BuildingType.Warehouse), 10);
        slots.AddRange(FindObjectsByType<BuildingSlot>(FindObjectsInactive.Include));
    }

    private void NormalizeSlotPositions()
    {
        foreach (BuildingSlot slot in slots)
        {
            if (slot == null)
                continue;

            slot.transform.localPosition = GetDefaultSlotPosition(slot.slotId, slot.allowedType);
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = Vector3.one;

            if (slot.slotId == "slot_barracks_02" ||
                slot.slotId == "slot_research_01" ||
                slot.slotId == "slot_warehouse_01")
            {
                slot.requiredHeadquartersLevel = Mathf.Max(slot.requiredHeadquartersLevel, 10);
            }
        }
    }

    private Vector3 GetDefaultSlotPosition(string slotId, BuildingType type)
    {
        switch (slotId)
        {
            case "slot_barracks_01":
                return new Vector3(-2.65f, 0f, -2.35f);
            case "slot_production_01":
                return new Vector3(2.65f, 0f, -2.35f);
            case "slot_barracks_02":
                return new Vector3(-4.2f, 0f, -3.9f);
            case "slot_research_01":
                return new Vector3(4.2f, 0f, -3.9f);
            case "slot_warehouse_01":
                return new Vector3(0f, 0f, -4.45f);
        }

        if (type == BuildingType.Barracks)
            return new Vector3(-2.65f, 0f, -2.35f);

        if (type == BuildingType.ProductionFacility)
            return new Vector3(2.65f, 0f, -2.35f);

        return new Vector3(0f, 0f, -3f);
    }

    private void CreateSlot(Transform root, string id, BuildingType type, Vector3 localPosition, int requiredLevel)
    {
        GameObject slotObject = new GameObject(id);
        slotObject.transform.SetParent(root, false);
        slotObject.transform.localPosition = localPosition;
        BuildingSlot slot = slotObject.AddComponent<BuildingSlot>();
        slot.slotId = id;
        slot.allowedType = type;
        slot.requiredHeadquartersLevel = requiredLevel;
    }

    private void EnsureDefaults()
    {
        if (definitions.Count == 0)
            AddDefaultDefinitions();

        ResolveEditorOnlyDefaults();
    }

    private void EnsureSlotRoot()
    {
        if (slotRoot != null)
            return;

        GameModeManager modeManager = GameModeManager.Instance != null
            ? GameModeManager.Instance
            : FindAnyObjectByType<GameModeManager>();

        if (modeManager != null && modeManager.baseRoot != null)
        {
            Transform existing = modeManager.baseRoot.transform.Find("ConstructionSlots");
            if (existing != null)
            {
                slotRoot = existing;
                return;
            }

            GameObject slotRootObject = new GameObject("ConstructionSlots");
            slotRootObject.transform.SetParent(modeManager.baseRoot.transform, false);
            slotRootObject.transform.localPosition = Vector3.zero;
            slotRootObject.transform.localRotation = Quaternion.identity;
            slotRootObject.transform.localScale = Vector3.one;
            slotRoot = slotRootObject.transform;
        }
    }

    private void AddDefaultDefinitions()
    {
        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.Barracks,
            displayName = "Kisla",
            maxCount = 5,
            requiredHeadquartersLevel = 1,
            cost = new ResourceCost { steel = 120, oil = 20 }
        });

        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.ProductionFacility,
            displayName = "Uretim Tesisi",
            maxCount = 1,
            requiredHeadquartersLevel = 1,
            cost = new ResourceCost { steel = 140, oil = 35, bor = 3 }
        });

        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.ResearchCenter,
            displayName = "Arastirma Ussu",
            maxCount = 1,
            requiredHeadquartersLevel = 2,
            cost = new ResourceCost { steel = 260, oil = 90, bor = 10 }
        });

        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.Warehouse,
            displayName = "Depo",
            maxCount = 1,
            requiredHeadquartersLevel = 2,
            cost = new ResourceCost { steel = 180, oil = 45 }
        });
    }

    private void ResolveEditorOnlyDefaults()
    {
#if UNITY_EDITOR
        ConstructionDefinition production = GetDefinition(BuildingType.ProductionFacility);
        if (production != null)
        {
            if (production.prefab == null)
                production.prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Buildings/ProductionFacility/UretimTesisi_Level1.prefab");

            if (production.buildingData == null)
                production.buildingData = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingData>(
                    "Assets/ScriptableObjects/Buildings/BD_UretimTesisi.asset");
        }
#endif
    }
}

[Serializable]
public class ConstructionDefinition
{
    public BuildingType type;
    public string displayName = "Bina";
    public int maxCount = 1;
    public int requiredHeadquartersLevel = 1;
    public ResourceCost cost;
    public BuildingData buildingData;
    public GameObject prefab;
}
