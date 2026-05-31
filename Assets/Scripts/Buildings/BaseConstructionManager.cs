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
        NormalizeKnownBuildingData();
        RefreshSlots();
        HideUnconstructedSceneBuildings();
        AttachPlacedBuildingsToSlots();
        RemoveExcessConstructedBuildings();
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

        hiddenSceneBuildings.RemoveAll(building => building == null || building.gameObject == instance);
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

    public bool TryConstructAtSlot(BuildingSlot slot)
    {
        if (slot == null)
            return false;

        ConstructionDefinition definition = GetDefinition(slot.allowedType);
        if (definition == null)
            return false;

        string reason;
        if (!CanConstructAtSlot(definition, slot, out reason))
        {
            Debug.Log("Parsele insa edilemedi: " + reason);
            return false;
        }

        if (!BaseManager.Instance.TrySpend(definition.cost))
            return false;

        GameObject instance = TryTakeHiddenSceneBuilding(definition.type);
        if (instance == null)
            instance = Instantiate(definition.prefab, slot.transform);
        else
            instance.transform.SetParent(slot.transform, false);

        hiddenSceneBuildings.RemoveAll(building => building == null || building.gameObject == instance);
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

    public bool CanConstructAtSlot(ConstructionDefinition definition, BuildingSlot slot, out string reason)
    {
        reason = "";
        if (slot == null)
        {
            reason = "Parsel yok";
            return false;
        }

        if (!IsConstructionSlotEnabled(slot))
        {
            reason = "Parsel kilitli";
            return false;
        }

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

        if (slot.allowedType != definition.type)
        {
            reason = "Bu parsel farkli bina icin ayrilmis";
            return false;
        }

        if (!slot.IsEmpty)
        {
            reason = "Parsel dolu";
            return false;
        }

        int headquartersLevel = BaseManager.Instance.HeadquartersLevel;
        if (!slot.CanUse(headquartersLevel))
        {
            reason = $"Komuta Merkezi Lv.{slot.requiredHeadquartersLevel} gerekli";
            return false;
        }

        if (headquartersLevel < definition.requiredHeadquartersLevel)
        {
            reason = $"Komuta Merkezi Lv.{definition.requiredHeadquartersLevel} gerekli";
            return false;
        }

        if (CountBuildings(definition.type) >= definition.maxCount)
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
        string modelLine = string.IsNullOrWhiteSpace(definition.modelFolder)
            ? ""
            : $"\nModel klasoru: {definition.modelFolder}";
        string briefLine = string.IsNullOrWhiteSpace(definition.modelBrief)
            ? ""
            : $"\nBrief: {definition.modelBrief}";

        return $"{definition.displayName}  {count}/{definition.maxCount}\n" +
               $"Gerekli: KM Lv.{definition.requiredHeadquartersLevel}  Maliyet: {definition.cost.ToDisplayString()}\n" +
               status +
               modelLine +
               briefLine;
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
                IsConstructionSlotEnabled(slot) &&
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
        RefreshSlotVisuals();
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

                if (hiddenSceneBuildings.Contains(building))
                    continue;

                if (building.data.type != slot.allowedType)
                    continue;

                if (!IsConstructionSlotEnabled(slot))
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

            if (building.data.type == BuildingType.Headquarters)
            {
                continue;
            }

            ConstructionDefinition definition = GetDefinition(building.data.type);

            bool forceHiddenUntilConstructed = building.data.type == BuildingType.Barracks;
            BuildingSlot occupiedSlot = FindSlotForPlacedBuilding(building);
            if (definition != null && occupiedSlot != null && !forceHiddenUntilConstructed)
                continue;

            if (!hiddenSceneBuildings.Contains(building))
                hiddenSceneBuildings.Add(building);

            if (occupiedSlot != null && occupiedSlot.placedBuilding == building)
            {
                occupiedSlot.placedBuilding = null;
                occupiedSlot.RefreshVisual();
            }

            if (BaseManager.Instance != null)
                BaseManager.Instance.UnregisterBuilding(building);

            building.gameObject.SetActive(false);
        }

        RefreshSlots();
    }

    private void NormalizeKnownBuildingData()
    {
        BaseBuilding[] sceneBuildings =
            FindObjectsByType<BaseBuilding>(FindObjectsInactive.Include);

        foreach (BaseBuilding building in sceneBuildings)
        {
            if (building == null)
                continue;

            if (!IsBarracksWithLegacyData(building))
                continue;

            BuildingData replacement = LoadBarracksData();
            if (replacement == null)
                continue;

            building.data = replacement;
            building.currentLevel = Mathf.Max(1, building.currentLevel);
            building.RefreshVisual();
            NormalizeCollider(building);
        }
    }

    private void RemoveExcessConstructedBuildings()
    {
        if (BaseManager.Instance == null)
            return;

        Dictionary<BuildingType, int> keptCounts = new Dictionary<BuildingType, int>();
        List<BaseBuilding> snapshot = new List<BaseBuilding>(BaseManager.Instance.buildings);

        foreach (BaseBuilding building in snapshot)
        {
            if (building == null || building.data == null)
                continue;

            ConstructionDefinition definition = GetDefinition(building.data.type);
            if (definition == null || definition.maxCount <= 0)
                continue;

            int kept;
            keptCounts.TryGetValue(building.data.type, out kept);
            if (kept < definition.maxCount)
            {
                keptCounts[building.data.type] = kept + 1;
                continue;
            }

            BaseManager.Instance.UnregisterBuilding(building);
            BuildingSlot slot = FindSlotForPlacedBuilding(building);
            if (slot != null && slot.placedBuilding == building)
            {
                slot.placedBuilding = null;
                slot.RefreshVisual();
            }

            building.gameObject.SetActive(false);
        }
    }

    private bool IsBarracksWithLegacyData(BaseBuilding building)
    {
        if (building == null)
            return false;

        if (building.GetComponent<BarracksBuilding>() == null)
            return false;

        if (building.data == null)
            return true;

        if (building.data.type != BuildingType.Barracks)
            return false;

        BuildingLevelData level = building.data.GetLevelData(Mathf.Max(1, building.currentLevel));
        return level == null || level.visualPrefab == null || building.data.buildingId == "building_id";
    }

    private BuildingData LoadBarracksData()
    {
        ConstructionDefinition definition = GetDefinition(BuildingType.Barracks);
        if (definition != null && definition.buildingData != null)
            return definition.buildingData;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingData>(
            "Assets/ScriptableObjects/Buildings/BD_Kisla.asset");
#else
        return null;
#endif
    }

    private BuildingSlot FindSlotForPlacedBuilding(BaseBuilding building)
    {
        if (building == null || building.data == null)
            return null;

        foreach (BuildingSlot slot in slots)
        {
            if (slot == null || slot.allowedType != building.data.type)
                continue;

            if (!IsConstructionSlotEnabled(slot))
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

        GameModeManager modeManager = GameModeManager.Instance != null
            ? GameModeManager.Instance
            : FindAnyObjectByType<GameModeManager>();

        if (building.data != null && modeManager != null)
            building.FitVisualToFootprint(modeManager.GetSuggestedBuildingFootprint(building.data.type));

        building.RebuildClickableCollider();
    }

    private void CreateDefaultSlots()
    {
        Transform root = slotRoot != null ? slotRoot : transform;
        CreateSlot(root, "slot_barracks_01", BuildingType.Barracks);
        CreateSlot(root, "slot_production_01", BuildingType.ProductionFacility);
        CreateSlot(root, "slot_warehouse_01", BuildingType.Warehouse);
        CreateSlot(root, "slot_research_01", BuildingType.ResearchCenter);
        CreateSlot(root, "slot_steel_01", BuildingType.SteelFactory);
        CreateSlot(root, "slot_oil_01", BuildingType.OilRefinery);
        CreateSlot(root, "slot_bor_01", BuildingType.BorMine);
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
            slot.displayName = GetDefaultSlotDisplayName(slot.slotId, slot.allowedType);
            slot.requiredHeadquartersLevel = GetDefaultSlotRequiredLevel(slot.slotId, slot.allowedType);
            slot.footprintSize = GetDefaultSlotFootprint(slot.slotId, slot.allowedType);
            slot.showRuntimeVisual = IsConstructionSlotEnabled(slot);
        }
    }

    private bool IsConstructionSlotEnabled(BuildingSlot slot)
    {
        if (slot == null)
            return false;

        return slot.slotId != "slot_barracks_02";
    }

    private Vector3 GetDefaultSlotPosition(string slotId, BuildingType type)
    {
        float multiplier = GetBaseLayoutMultiplier();

        switch (slotId)
        {
            case "slot_barracks_01":
                return new Vector3(-3.75f * multiplier, 0f, -2.05f * multiplier);
            case "slot_production_01":
                return new Vector3(3.75f * multiplier, 0f, -2.05f * multiplier);
            case "slot_warehouse_01":
                return new Vector3(-2.05f * multiplier, 0f, -4.35f * multiplier);
            case "slot_research_01":
                return new Vector3(2.05f * multiplier, 0f, -4.35f * multiplier);
            case "slot_steel_01":
                return new Vector3(-4.45f * multiplier, 0f, 1.55f * multiplier);
            case "slot_oil_01":
                return new Vector3(4.45f * multiplier, 0f, 1.55f * multiplier);
            case "slot_bor_01":
                return new Vector3(0f, 0f, -5.65f * multiplier);
            case "slot_barracks_02":
                return new Vector3(0f, 0f, 3.75f * multiplier);
        }

        if (type == BuildingType.Barracks)
            return new Vector3(-3.75f * multiplier, 0f, -2.05f * multiplier);

        if (type == BuildingType.ProductionFacility)
            return new Vector3(3.75f * multiplier, 0f, -2.05f * multiplier);

        return new Vector3(0f, 0f, -3f * multiplier);
    }

    private Vector2 GetDefaultSlotFootprint(string slotId, BuildingType type)
    {
        switch (type)
        {
            case BuildingType.ProductionFacility:
            case BuildingType.SteelFactory:
            case BuildingType.OilRefinery:
                return new Vector2(1.95f, 1.45f);
            case BuildingType.BorMine:
                return new Vector2(1.8f, 1.35f);
            case BuildingType.ResearchCenter:
                return new Vector2(1.65f, 1.35f);
            case BuildingType.Warehouse:
                return new Vector2(1.7f, 1.25f);
            default:
                return new Vector2(1.75f, 1.35f);
        }
    }

    private string GetDefaultSlotDisplayName(string slotId, BuildingType type)
    {
        switch (slotId)
        {
            case "slot_barracks_01":
                return "Kisla Parseli I";
            case "slot_production_01":
                return "Uretim Parseli I";
            case "slot_warehouse_01":
                return "Depo Parseli";
            case "slot_research_01":
                return "Arastirma Parseli";
            case "slot_steel_01":
                return "Celik Fabrikasi Parseli";
            case "slot_oil_01":
                return "Petrol Rafinerisi Parseli";
            case "slot_bor_01":
                return "Bor Tesisi Parseli";
            case "slot_barracks_02":
                return "Kisla Parseli II";
        }

        return type + " Parseli";
    }

    private int GetDefaultSlotRequiredLevel(string slotId, BuildingType type)
    {
        switch (slotId)
        {
            case "slot_barracks_01":
            case "slot_production_01":
                return 1;
            case "slot_warehouse_01":
                return 2;
            case "slot_research_01":
                return 3;
            case "slot_steel_01":
                return 4;
            case "slot_oil_01":
                return 5;
            case "slot_bor_01":
                return 6;
            case "slot_barracks_02":
                return 7;
        }

        return 1;
    }

    private void RefreshSlotVisuals()
    {
        foreach (BuildingSlot slot in slots)
        {
            if (slot != null)
                slot.RebuildVisual();
        }
    }

    private float GetBaseLayoutMultiplier()
    {
        GameModeManager modeManager = GameModeManager.Instance != null
            ? GameModeManager.Instance
            : FindAnyObjectByType<GameModeManager>();

        return modeManager != null
            ? Mathf.Clamp(modeManager.GetCurrentBaseLayoutMultiplier(), 0.88f, 1.08f)
            : 0.88f;
    }

    private void CreateSlot(Transform root, string id, BuildingType type)
    {
        GameObject slotObject = new GameObject(id);
        slotObject.transform.SetParent(root, false);
        slotObject.transform.localPosition = GetDefaultSlotPosition(id, type);
        BuildingSlot slot = slotObject.AddComponent<BuildingSlot>();
        slot.slotId = id;
        slot.displayName = GetDefaultSlotDisplayName(id, type);
        slot.allowedType = type;
        slot.requiredHeadquartersLevel = GetDefaultSlotRequiredLevel(id, type);
        slot.footprintSize = GetDefaultSlotFootprint(id, type);
    }

    private void EnsureDefaults()
    {
        if (definitions.Count == 0)
            AddDefaultDefinitions();
        else
            EnsureMissingDefaultDefinitions();

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
            maxCount = 1,
            requiredHeadquartersLevel = 1,
            cost = new ResourceCost { steel = 120, oil = 20 },
            modelFolder = "Assets/Models/Buildings/Barracks/Lv1/",
            modelBrief = "Askeri yatakhane, talim avlusu ve Turk/Turan esintili modern askeri detaylar."
        });

        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.ProductionFacility,
            displayName = "Uretim Tesisi",
            maxCount = 1,
            requiredHeadquartersLevel = 1,
            cost = new ResourceCost { steel = 140, oil = 35, bor = 3 },
            modelFolder = "Assets/Models/Buildings/ProductionFacility/Level1/",
            modelBrief = "Hafif sanayi atolyeleri, antenler, konteynerler ve servis borulari."
        });

        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.ResearchCenter,
            displayName = "Arastirma Ussu",
            maxCount = 1,
            requiredHeadquartersLevel = 3,
            cost = new ResourceCost { steel = 260, oil = 90, bor = 10 },
            modelFolder = "Assets/Models/Buildings/ResearchCenter/Lv1/",
            modelBrief = "Radar kubbesi, laboratuvar modulu, iletisim antenleri ve temiz teknoloji dili."
        });

        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.Warehouse,
            displayName = "Depo",
            maxCount = 1,
            requiredHeadquartersLevel = 2,
            cost = new ResourceCost { steel = 180, oil = 45 },
            modelFolder = "Assets/Models/Buildings/Warehouse/Lv1/",
            modelBrief = "Lojistik hangari, kasa/konteyner dizilimi ve kamyon yukleme rampasi."
        });

        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.SteelFactory,
            displayName = "Celik Fabrikasi",
            maxCount = 1,
            requiredHeadquartersLevel = 4,
            cost = new ResourceCost { steel = 360, oil = 120, bor = 12 },
            modelFolder = "Assets/Models/Buildings/SteelFactory/Lv1/",
            modelBrief = "Kucuk olcekli celik isleme tesisi, baca, vinclik kol ve metal stok alani."
        });

        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.OilRefinery,
            displayName = "Petrol Rafinerisi",
            maxCount = 1,
            requiredHeadquartersLevel = 5,
            cost = new ResourceCost { steel = 420, oil = 170, bor = 15 },
            modelFolder = "Assets/Models/Buildings/OilRefinery/Lv1/",
            modelBrief = "Tanklar, boru hatlari, pompa istasyonu ve guvenlik setleri."
        });

        definitions.Add(new ConstructionDefinition
        {
            type = BuildingType.BorMine,
            displayName = "Bor Tesisi",
            maxCount = 1,
            requiredHeadquartersLevel = 6,
            cost = new ResourceCost { steel = 520, oil = 210, bor = 25 },
            modelFolder = "Assets/Models/Buildings/BorMine/Lv1/",
            modelBrief = "Isleme tesisi, cevher bunkerleri, konveyor ve mavi-beyaz mineral vurgulari."
        });
    }

    private void EnsureMissingDefaultDefinitions()
    {
        List<ConstructionDefinition> existingDefinitions = new List<ConstructionDefinition>(definitions);
        List<ConstructionDefinition> generatedDefinitions = new List<ConstructionDefinition>();
        definitions = generatedDefinitions;
        AddDefaultDefinitions();
        definitions = existingDefinitions;

        foreach (ConstructionDefinition generated in generatedDefinitions)
        {
            ConstructionDefinition existing = FindDefinitionInList(existingDefinitions, generated.type);
            if (existing == null)
            {
                definitions.Add(generated);
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.modelFolder))
                existing.modelFolder = generated.modelFolder;

            if (string.IsNullOrWhiteSpace(existing.modelBrief))
                existing.modelBrief = generated.modelBrief;
        }

        ConstructionDefinition barracks = FindDefinitionInList(definitions, BuildingType.Barracks);
        if (barracks != null)
            barracks.maxCount = 1;
    }

    private ConstructionDefinition FindDefinitionInList(List<ConstructionDefinition> source, BuildingType type)
    {
        foreach (ConstructionDefinition definition in source)
        {
            if (definition != null && definition.type == type)
                return definition;
        }

        return null;
    }

    private void ResolveEditorOnlyDefaults()
    {
#if UNITY_EDITOR
        ConstructionDefinition barracks = GetDefinition(BuildingType.Barracks);
        if (barracks != null)
        {
            if (barracks.prefab == null)
                barracks.prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Buildings/Barracks/Barracks_Level1.prefab");

            if (barracks.buildingData == null)
                barracks.buildingData = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingData>(
                    "Assets/ScriptableObjects/Buildings/BD_Kisla.asset");
        }

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
    public string modelFolder;
    [TextArea(2, 4)]
    public string modelBrief;
}
