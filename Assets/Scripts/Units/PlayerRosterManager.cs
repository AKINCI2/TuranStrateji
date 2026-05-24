using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRosterManager : MonoBehaviour
{
    public static PlayerRosterManager Instance;

    [Header("Starter Data")]
    public bool createStarterRosterIfEmpty = true;
    public UnitCatalogData starterUnitCatalog;
    public OfficerCatalogData starterOfficerCatalog;

    [Header("Owned Units")]
    public List<OwnedUnitEntry> ownedUnits = new List<OwnedUnitEntry>();

    [Header("Owned Officers")]
    public List<OwnedOfficerEntry> ownedOfficers = new List<OwnedOfficerEntry>();

    public event Action RosterChanged;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (createStarterRosterIfEmpty)
            CreateStarterRosterIfEmpty();

        EnsureStarterOfficers();
    }

    public int GetOwnedCount(TuranUnitData unitData)
    {
        OwnedUnitEntry entry = GetUnitEntry(unitData);
        return entry != null ? entry.count : 0;
    }

    public bool CanMerge(TuranUnitData unitData)
    {
        if (unitData == null || !unitData.CanMergeIntoNext())
            return false;

        return GetOwnedCount(unitData) >= unitData.mergeRequiredCopies;
    }

    public bool TryMerge(TuranUnitData unitData)
    {
        if (!CanMerge(unitData))
            return false;

        OwnedUnitEntry source = GetUnitEntry(unitData);
        source.count -= unitData.mergeRequiredCopies;

        AddUnitCopies(unitData.mergeResult, 1);
        Cleanup();
        RosterChanged?.Invoke();

        Debug.Log($"{unitData.displayName} birlestirildi -> {unitData.mergeResult.displayName}");
        return true;
    }

    public void AddUnitCopies(TuranUnitData unitData, int amount)
    {
        if (unitData == null || amount <= 0)
            return;

        OwnedUnitEntry entry = GetOrCreateUnitEntry(unitData);
        entry.count += amount;
        RosterChanged?.Invoke();
    }

    public void UnlockOfficer(OfficerData officer)
    {
        if (officer == null || GetOfficerEntry(officer) != null)
            return;

        ownedOfficers.Add(new OwnedOfficerEntry
        {
            officer = officer,
            level = 1,
            unlocked = true
        });

        RosterChanged?.Invoke();
    }

    public bool AssignUnitToBarracks(BarracksBuilding barracks, TuranUnitData unitData)
    {
        if (barracks == null)
            return false;

        if (unitData == null)
        {
            barracks.SetAssignedUnitData(null);
            RosterChanged?.Invoke();
            return true;
        }

        if (unitData.kind == TuranUnitKind.Logistics || GetOwnedCount(unitData) <= 0)
            return false;

        barracks.SetAssignedUnitData(unitData);
        RosterChanged?.Invoke();
        return true;
    }

    public bool AssignOfficerToBarracks(BarracksBuilding barracks, OfficerData officer)
    {
        if (barracks == null || officer == null)
            return false;

        OwnedOfficerEntry entry = GetOfficerEntry(officer);
        if (entry == null || !entry.unlocked)
            return false;

        barracks.SetAssignedOfficer(officer);
        RosterChanged?.Invoke();
        return true;
    }

    public void EnsureStarterOfficers()
    {
        Cleanup();
        LoadStarterCatalogsIfNeeded();

        if (starterOfficerCatalog != null && starterOfficerCatalog.officers.Count > 0)
        {
            bool addedAny = false;
            foreach (OfficerData officer in starterOfficerCatalog.officers)
            {
                if (officer == null)
                    continue;

                OwnedOfficerEntry existing = GetOfficerEntry(officer);
                if (existing != null)
                {
                    if (!existing.unlocked)
                    {
                        existing.unlocked = true;
                        existing.level = Mathf.Max(1, existing.level);
                        addedAny = true;
                    }
                    continue;
                }

                ownedOfficers.Add(new OwnedOfficerEntry
                {
                    officer = officer,
                    level = 1,
                    unlocked = true
                });
                addedAny = true;
            }

            if (addedAny)
                RosterChanged?.Invoke();

            return;
        }

        if (ownedOfficers.Count == 0)
            AddRuntimeFallbackOfficer();
    }

    public int GetOfficerLevel(OfficerData officer)
    {
        OwnedOfficerEntry entry = GetOfficerEntry(officer);
        return entry != null ? Mathf.Max(1, entry.level) : 1;
    }

    private OwnedUnitEntry GetUnitEntry(TuranUnitData unitData)
    {
        if (unitData == null)
            return null;

        foreach (OwnedUnitEntry entry in ownedUnits)
        {
            if (entry != null && entry.unitData == unitData)
                return entry;
        }

        return null;
    }

    private OwnedUnitEntry GetOrCreateUnitEntry(TuranUnitData unitData)
    {
        OwnedUnitEntry entry = GetUnitEntry(unitData);
        if (entry != null)
            return entry;

        entry = new OwnedUnitEntry
        {
            unitData = unitData,
            count = 0
        };

        ownedUnits.Add(entry);
        return entry;
    }

    private OwnedOfficerEntry GetOfficerEntry(OfficerData officer)
    {
        if (officer == null)
            return null;

        foreach (OwnedOfficerEntry entry in ownedOfficers)
        {
            if (entry != null && entry.officer == officer)
                return entry;
        }

        return null;
    }

    private void Cleanup()
    {
        ownedUnits.RemoveAll(entry => entry == null || entry.unitData == null || entry.count <= 0);
        ownedOfficers.RemoveAll(entry => entry == null || entry.officer == null);
    }

    private void CreateStarterRosterIfEmpty()
    {
        Cleanup();

        if (ownedUnits.Count > 0)
        {
            EnsureStarterOfficers();
            return;
        }

        if (ownedOfficers.Count > 0)
            return;

        LoadStarterCatalogsIfNeeded();
        if (CreateStarterRosterFromCatalogs())
            return;

        TuranUnitData kirikkale =
            CreateRuntimeUnit(
                "bozkir_timi",
                "Bozkir Timi",
                "Egitim Piyade Tufegi",
                TuranUnitKind.Infantry,
                TuranUnitEra.EarlyRepublic,
                1,
                110,
                22,
                12,
                100,
                4,
                new ResourceCost { steel = 12 }
            );

        TuranUnitData mpt55 =
            CreateRuntimeUnit(
                "mpt55_timi",
                "MPT-55 Timi",
                "MPT-55",
                TuranUnitKind.Infantry,
                TuranUnitEra.Modern,
                2,
                185,
                34,
                20,
                145,
                4,
                new ResourceCost { steel = 18, oil = 2 }
            );

        TuranUnitData firtina =
            CreateRuntimeUnit(
                "t155_firtina_bataryasi",
                "T-155 Firtina Bataryasi",
                "T-155 Firtina",
                TuranUnitKind.Artillery,
                TuranUnitEra.Modern,
                2,
                260,
                58,
                24,
                170,
                3,
                new ResourceCost { steel = 34, oil = 8 }
            );

        TuranUnitData lojistik = Resources.Load<TuranUnitData>("Data/Units/LojistikDestek");
        if (lojistik == null)
        {
            lojistik = CreateRuntimeUnit(
                "lojistik_destek",
                "Lojistik Destek Birimi",
                "Lojistik Araci",
                TuranUnitKind.Logistics,
                TuranUnitEra.Modern,
                1,
                50,
                5,
                10,
                80,
                5,
                new ResourceCost { steel = 10, oil = 10 }
            );
        #if UNITY_EDITOR
            // Load Truck Prefab if exists
            GameObject truckPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/LogisticsTruck 1.prefab");
            if (truckPrefab != null) lojistik.worldPrefab = truckPrefab;
        #endif
        }

        kirikkale.mergeResult = mpt55;
        kirikkale.mergeRequiredCopies = 3;

        ownedUnits.Add(new OwnedUnitEntry { unitData = kirikkale, count = 3 });
        ownedUnits.Add(new OwnedUnitEntry { unitData = mpt55, count = 1 });
        ownedUnits.Add(new OwnedUnitEntry { unitData = firtina, count = 1 });
        ownedUnits.Add(new OwnedUnitEntry { unitData = lojistik, count = 1 });

        AddRuntimeFallbackOfficer();

        RosterChanged?.Invoke();
    }

    private void LoadStarterCatalogsIfNeeded()
    {
        if (starterUnitCatalog == null)
            starterUnitCatalog = Resources.Load<UnitCatalogData>("Data/Databases/UnitCatalog_Starter");

        if (starterOfficerCatalog == null)
            starterOfficerCatalog = Resources.Load<OfficerCatalogData>("Data/Databases/OfficerCatalog_Starter");
    }

    private bool CreateStarterRosterFromCatalogs()
    {
        if (starterUnitCatalog == null || starterUnitCatalog.units.Count == 0)
            return false;

        AddStarterUnit("bozkir_timi", 3);
        AddStarterUnit("alp_timi", 1);
        AddStarterUnit("boran_bataryasi", 1);
        AddStarterUnit("yolcu_lojistik_kolu", 1);

        if (ownedUnits.Count == 0)
            return false;

        if (starterOfficerCatalog != null && starterOfficerCatalog.officers.Count > 0)
        {
            foreach (OfficerData officer in starterOfficerCatalog.officers)
            {
                if (officer == null)
                    continue;

                ownedOfficers.Add(new OwnedOfficerEntry
                {
                    officer = officer,
                    level = 1,
                    unlocked = true
                });
                break;
            }
        }

        RosterChanged?.Invoke();
        return true;
    }

    private void AddRuntimeFallbackOfficer()
    {
        OfficerData alp = ScriptableObject.CreateInstance<OfficerData>();
        alp.officerId = "alp_arslan";
        alp.displayName = "Alp Arslan";
        alp.branch = TuranForceBranch.LandForces;
        alp.preferredUnitKind = TuranUnitKind.Infantry;
        alp.assignment = OfficerAssignment.Field;
        alp.tendency = OfficerTendency.Attack;
        alp.attackBonusPercent = 8;
        alp.healthBonusPercent = 5;

        ownedOfficers.Add(new OwnedOfficerEntry
        {
            officer = alp,
            level = 1,
            unlocked = true
        });

        RosterChanged?.Invoke();
    }

    private void AddStarterUnit(string unitId, int count)
    {
        TuranUnitData unit = starterUnitCatalog != null ? starterUnitCatalog.FindById(unitId) : null;
        if (unit != null)
            ownedUnits.Add(new OwnedUnitEntry { unitData = unit, count = count });
    }

    private TuranUnitData CreateRuntimeUnit(
        string id,
        string displayName,
        string weaponName,
        TuranUnitKind kind,
        TuranUnitEra era,
        int stars,
        int power,
        int attack,
        int defense,
        int health,
        int speed,
        ResourceCost trainCost)
    {
        TuranUnitData unit =
            ScriptableObject.CreateInstance<TuranUnitData>();

        unit.unitId = id;
        unit.displayName = displayName;
        unit.weaponOrVehicleName = weaponName;
        unit.branch = TuranForceBranch.LandForces;
        unit.kind = kind;
        unit.era = era;
        unit.nation = TuranNation.TuranCommon;
        unit.rarity = TuranRarity.Common;
        unit.baseStars = stars;
        unit.tier = stars;
        unit.power = power;
        unit.attack = attack;
        unit.defense = defense;
        unit.health = health;
        unit.marchSpeed = speed;
        unit.baseSquadSize = 9;
        unit.secondsPerSoldier = 1.2f + stars * 0.4f;
        unit.trainCostPerSoldier = trainCost;

        return unit;
    }
}

[Serializable]
public class OwnedUnitEntry
{
    public TuranUnitData unitData;
    public int count;
}

[Serializable]
public class OwnedOfficerEntry
{
    public OfficerData officer;
    public int level = 1;
    public int experience;
    public bool unlocked;
}


