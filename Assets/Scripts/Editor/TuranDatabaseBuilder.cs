#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[InitializeOnLoad]
public static class TuranDatabaseBuilder
{
    private const string Root = "Assets/Resources/Data";
    private const string UnitsRoot = Root + "/Units";
    private const string WeaponsRoot = Root + "/Weapons";
    private const string VisualProfilesRoot = Root + "/VisualProfiles";
    private const string OfficersRoot = Root + "/Officers";
    private const string ChestsRoot = Root + "/Chests";
    private const string DatabasesRoot = Root + "/Databases";

    static TuranDatabaseBuilder()
    {
        QueueAutoBuildIfMissing();
    }

    [MenuItem("Turan Strateji/Data/Build Starter Databases")]
    public static void BuildStarterDatabases()
    {
        EnsureFolder(UnitsRoot + "/Turkiye");
        EnsureFolder(UnitsRoot + "/Azerbaycan");
        EnsureFolder(UnitsRoot + "/Turan");
        EnsureFolder(UnitsRoot + "/Lojistik");
        EnsureFolder(WeaponsRoot);
        EnsureFolder(VisualProfilesRoot);
        EnsureFolder(OfficersRoot);
        EnsureFolder(ChestsRoot);
        EnsureFolder(DatabasesRoot);

        Dictionary<string, TuranUnitData> units = new Dictionary<string, TuranUnitData>();

        AddUnit(units, "bozkir_timi", "Bozkir Timi", "Egitim Piyade Tufegi", TuranNation.TuranCommon, TuranUnitKind.Infantry, TuranUnitEra.EarlyRepublic, TuranRarity.Common, 1, 1, 95, 18, 12, 95, 4, new ResourceCost { steel = 10 }, "piyade");
        AddUnit(units, "alp_timi", "Alp Timi", "SAR 56", TuranNation.Turkiye, TuranUnitKind.Infantry, TuranUnitEra.Modern, TuranRarity.Uncommon, 2, 2, 155, 30, 18, 125, 4, new ResourceCost { steel = 15, oil = 1 }, "piyade");
        AddUnit(units, "boru_timi", "Boru Timi", "MPT-55K", TuranNation.Turkiye, TuranUnitKind.Infantry, TuranUnitEra.Modern, TuranRarity.Rare, 3, 3, 225, 42, 25, 165, 4, new ResourceCost { steel = 22, oil = 2 }, "piyade");
        AddUnit(units, "akinci_timi", "Akinci Timi", "MPT-76", TuranNation.Turkiye, TuranUnitKind.Infantry, TuranUnitEra.Modern, TuranRarity.Epic, 4, 4, 315, 60, 34, 220, 4, new ResourceCost { steel = 32, oil = 4, bor = 1 }, "piyade");
        AddUnit(units, "korkut_timi", "Korkut Timi", "MPT-76MH", TuranNation.Turkiye, TuranUnitKind.Infantry, TuranUnitEra.Advanced, TuranRarity.Epic, 5, 5, 430, 78, 48, 300, 4, new ResourceCost { steel = 45, oil = 7, bor = 2 }, "piyade");
        AddUnit(units, "gokboru_timi", "Gokboru Timi", "Turan Kompozit Piyade Sistemi", TuranNation.TuranCommon, TuranUnitKind.Infantry, TuranUnitEra.Future, TuranRarity.Legendary, 5, 6, 620, 112, 72, 420, 5, new ResourceCost { steel = 70, oil = 12, bor = 5 }, "piyade");
        AddUnit(units, "istiglal_timi", "Istiglal Timi", "Istiglal Keskin Nisanci Sistemi", TuranNation.Azerbaijan, TuranUnitKind.Infantry, TuranUnitEra.Modern, TuranRarity.Epic, 4, 4, 340, 82, 24, 180, 3, new ResourceCost { steel = 36, oil = 5, bor = 1 }, "piyade_destek");
        AddUnit(units, "alatau_timi", "Alatau Timi", "Bozkir Savunma Piyade Seti", TuranNation.Kazakhstan, TuranUnitKind.Infantry, TuranUnitEra.Modern, TuranRarity.Rare, 3, 3, 235, 36, 34, 190, 4, new ResourceCost { steel = 24, oil = 3 }, "piyade_savunma");

        AddUnit(units, "bozkurt_zirhli_timi", "Bozkurt Zirhli Timi", "M60 Modernize", TuranNation.Turkiye, TuranUnitKind.Tank, TuranUnitEra.ColdWar, TuranRarity.Common, 2, 1, 260, 54, 45, 310, 3, new ResourceCost { steel = 45, oil = 12 }, "tank");
        AddUnit(units, "sabra_timi", "Sabra Timi", "M60T Sabra", TuranNation.Turkiye, TuranUnitKind.Tank, TuranUnitEra.Modern, TuranRarity.Uncommon, 3, 2, 360, 72, 62, 420, 3, new ResourceCost { steel = 62, oil = 18 }, "tank");
        AddUnit(units, "firtina_zirhli_timi", "Firtina Zirhli Timi", "Leopard 2 Modernize", TuranNation.Turkiye, TuranUnitKind.Tank, TuranUnitEra.Modern, TuranRarity.Rare, 4, 3, 480, 94, 82, 560, 3, new ResourceCost { steel = 85, oil = 26, bor = 1 }, "tank");
        AddUnit(units, "altay_timi", "Altay Timi", "Altay AMT", TuranNation.Turkiye, TuranUnitKind.Tank, TuranUnitEra.Advanced, TuranRarity.Epic, 5, 4, 660, 128, 112, 760, 3, new ResourceCost { steel = 120, oil = 38, bor = 3 }, "tank");
        AddUnit(units, "turan_altay_timi", "Turan Altay Timi", "Altay Ileri Modernizasyon", TuranNation.TuranCommon, TuranUnitKind.Tank, TuranUnitEra.Advanced, TuranRarity.Epic, 5, 5, 820, 158, 138, 930, 3, new ResourceCost { steel = 150, oil = 50, bor = 5 }, "tank");
        AddUnit(units, "gokdemir_zirhli_timi", "Gokdemir Zirhli Timi", "Gokdemir AMT", TuranNation.TuranCommon, TuranUnitKind.Tank, TuranUnitEra.Future, TuranRarity.Legendary, 5, 6, 1080, 205, 180, 1240, 4, new ResourceCost { steel = 210, oil = 75, bor = 10 }, "tank");

        AddUnit(units, "boran_bataryasi", "Boran Bataryasi", "Boran 105 mm", TuranNation.Turkiye, TuranUnitKind.Artillery, TuranUnitEra.Modern, TuranRarity.Common, 2, 1, 240, 68, 24, 190, 3, new ResourceCost { steel = 34, oil = 8 }, "topcu");
        AddUnit(units, "panter_bataryasi", "Panter Bataryasi", "Panter Obusu", TuranNation.Turkiye, TuranUnitKind.Artillery, TuranUnitEra.Modern, TuranRarity.Uncommon, 3, 2, 330, 92, 30, 245, 3, new ResourceCost { steel = 48, oil = 12 }, "topcu");
        AddUnit(units, "firtina_bataryasi", "Firtina Bataryasi", "T-155 Firtina", TuranNation.Turkiye, TuranUnitKind.Artillery, TuranUnitEra.Advanced, TuranRarity.Rare, 4, 3, 470, 132, 42, 340, 3, new ResourceCost { steel = 70, oil = 20, bor = 2 }, "topcu");
        AddUnit(units, "yeni_firtina_bataryasi", "Yeni Firtina Bataryasi", "Firtina II", TuranNation.Turkiye, TuranUnitKind.Artillery, TuranUnitEra.Advanced, TuranRarity.Epic, 5, 4, 640, 178, 56, 455, 3, new ResourceCost { steel = 98, oil = 32, bor = 4 }, "topcu");
        AddUnit(units, "demir_yay_bataryasi", "Demir Yay Bataryasi", "Turan Modern Obus Sistemi", TuranNation.TuranCommon, TuranUnitKind.Artillery, TuranUnitEra.Future, TuranRarity.Epic, 5, 5, 790, 220, 70, 560, 3, new ResourceCost { steel = 130, oil = 45, bor = 7 }, "topcu");
        AddUnit(units, "gok_topcu_bataryasi", "Gok Topcu Bataryasi", "Gok Topcu Sistemi", TuranNation.TuranCommon, TuranUnitKind.Artillery, TuranUnitEra.Future, TuranRarity.Legendary, 5, 6, 1020, 285, 88, 720, 3, new ResourceCost { steel = 180, oil = 64, bor = 12 }, "topcu");

        AddUnit(units, "sakarya_roket_timi", "Sakarya Roket Timi", "T-122 Sakarya", TuranNation.Turkiye, TuranUnitKind.RocketArtillery, TuranUnitEra.Modern, TuranRarity.Uncommon, 3, 2, 360, 115, 28, 260, 3, new ResourceCost { steel = 58, oil = 16 }, "roket");
        AddUnit(units, "kasirga_roket_timi", "Kasirga Roket Timi", "T-300 Kasirga", TuranNation.Turkiye, TuranUnitKind.RocketArtillery, TuranUnitEra.Advanced, TuranRarity.Rare, 4, 3, 520, 165, 38, 360, 3, new ResourceCost { steel = 82, oil = 28, bor = 3 }, "roket");
        AddUnit(units, "bora_fuze_timi", "Bora Fuze Timi", "Bora Taktik Fuze", TuranNation.Turkiye, TuranUnitKind.RocketArtillery, TuranUnitEra.Advanced, TuranRarity.Epic, 5, 4, 760, 245, 52, 500, 3, new ResourceCost { steel = 125, oil = 46, bor = 7 }, "roket");
        AddUnit(units, "tufan_roket_timi", "Tufan Roket Timi", "Turan-Azerbaycan Roket Sistemi", TuranNation.Azerbaijan, TuranUnitKind.RocketArtillery, TuranUnitEra.Advanced, TuranRarity.Epic, 5, 4, 735, 238, 50, 480, 3, new ResourceCost { steel = 120, oil = 44, bor = 7 }, "roket");
        AddUnit(units, "gokdogan_roket_timi", "Gokdogan Roket Timi", "Gokdogan Cok Namlulu Roket Sistemi", TuranNation.TuranCommon, TuranUnitKind.RocketArtillery, TuranUnitEra.Future, TuranRarity.Legendary, 5, 6, 1160, 370, 78, 750, 4, new ResourceCost { steel = 220, oil = 82, bor = 14 }, "roket");

        AddUnit(units, "yolcu_lojistik_kolu", "Yolcu Lojistik Kolu", "Hafif Toplayici Kamyon", TuranNation.TuranCommon, TuranUnitKind.Logistics, TuranUnitEra.Modern, TuranRarity.Common, 1, 1, 70, 6, 18, 110, 5, new ResourceCost { steel = 12, oil = 8 }, "lojistik");
        AddUnit(units, "kervan_lojistik_kolu", "Kervan Lojistik Kolu", "Zirhli Toplayici Konvoy", TuranNation.TuranCommon, TuranUnitKind.Logistics, TuranUnitEra.Advanced, TuranRarity.Rare, 3, 3, 190, 16, 48, 260, 5, new ResourceCost { steel = 34, oil = 20 }, "lojistik");

        Link(units, "bozkir_timi", "alp_timi");
        Link(units, "alp_timi", "boru_timi");
        Link(units, "boru_timi", "akinci_timi");
        Link(units, "akinci_timi", "korkut_timi");
        Link(units, "korkut_timi", "gokboru_timi");
        Link(units, "bozkurt_zirhli_timi", "sabra_timi");
        Link(units, "sabra_timi", "firtina_zirhli_timi");
        Link(units, "firtina_zirhli_timi", "altay_timi");
        Link(units, "altay_timi", "turan_altay_timi");
        Link(units, "turan_altay_timi", "gokdemir_zirhli_timi");
        Link(units, "boran_bataryasi", "panter_bataryasi");
        Link(units, "panter_bataryasi", "firtina_bataryasi");
        Link(units, "firtina_bataryasi", "yeni_firtina_bataryasi");
        Link(units, "yeni_firtina_bataryasi", "demir_yay_bataryasi");
        Link(units, "demir_yay_bataryasi", "gok_topcu_bataryasi");
        Link(units, "sakarya_roket_timi", "kasirga_roket_timi");
        Link(units, "kasirga_roket_timi", "bora_fuze_timi");
        Link(units, "bora_fuze_timi", "gokdogan_roket_timi");
        Link(units, "yolcu_lojistik_kolu", "kervan_lojistik_kolu");

        UnitCatalogData unitCatalog = LoadOrCreate<UnitCatalogData>(DatabasesRoot + "/UnitCatalog_Starter.asset");
        unitCatalog.units = new List<TuranUnitData>(units.Values);
        EditorUtility.SetDirty(unitCatalog);

        OfficerData alpArslan = CreateOfficer("alp_arslan", "Alp Arslan", OfficerLegacyType.HistoricalLeader, "Tarihi lider ruhunu temsil eden saldiri yonlu saha subayi.", TuranUnitKind.Infantry, OfficerTendency.Attack, 8, 2, 5, 0, 0);
        OfficerData tomris = CreateOfficer("tomris_hatun", "Tomris Hatun", OfficerLegacyType.HistoricalLeader, "Bozkir savas gelenegini temsil eden destek ve topcu subayi.", TuranUnitKind.Artillery, OfficerTendency.Support, 4, 5, 6, 0, 4);
        OfficerData bilge = CreateOfficer("bilge_tonyukuk", "Bilge Tonyukuk", OfficerLegacyType.HistoricalLeader, "Akil, ikmal ve ekonomi yonetimini temsil eden us subayi.", TuranUnitKind.Logistics, OfficerTendency.Economy, 0, 4, 4, 8, 6);
        OfficerData omerHalisdemir = CreateOfficer("omer_halisdemir", "Omer Halisdemir", OfficerLegacyType.Martyr, "Vatan savunmasi ve cesareti temsil eden kahraman subay karti.", TuranUnitKind.Infantry, OfficerTendency.Defense, 5, 8, 8, 0, 0);

        OfficerCatalogData officerCatalog = LoadOrCreate<OfficerCatalogData>(DatabasesRoot + "/OfficerCatalog_Starter.asset");
        officerCatalog.officers = new List<OfficerData> { alpArslan, tomris, bilge, omerHalisdemir };
        EditorUtility.SetDirty(officerCatalog);

        CreateChest("turkiye_kara_sandigi", "Turkiye Kara Sandigi", TuranNation.Turkiye, ChestsRoot + "/RC_TurkiyeKara.asset", units,
            "bozkir_timi", "alp_timi", "boru_timi", "bozkurt_zirhli_timi", "boran_bataryasi", "sakarya_roket_timi");
        CreateChest("turan_ortak_sandigi", "Turan Ortak Sandigi", TuranNation.TuranCommon, ChestsRoot + "/RC_TuranOrtak.asset", units,
            "alatau_timi", "istiglal_timi", "tufan_roket_timi", "kervan_lojistik_kolu", "gokboru_timi");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Turan starter database hazir: {unitCatalog.units.Count} tim, {officerCatalog.officers.Count} subay.");
    }

    [DidReloadScripts]
    private static void AutoBuildIfMissing()
    {
        QueueAutoBuildIfMissing();
    }

    private static void QueueAutoBuildIfMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= BuildAfterPlayMode;
                EditorApplication.playModeStateChanged += BuildAfterPlayMode;
                return;
            }

            if (NeedsStarterDatabaseBuild())
                BuildStarterDatabases();
        };
    }

    private static void BuildAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        EditorApplication.playModeStateChanged -= BuildAfterPlayMode;
        if (NeedsStarterDatabaseBuild())
            BuildStarterDatabases();
    }

    private static TuranUnitData AddUnit(
        Dictionary<string, TuranUnitData> units,
        string id,
        string displayName,
        string weapon,
        TuranNation nation,
        TuranUnitKind kind,
        TuranUnitEra era,
        TuranRarity rarity,
        int stars,
        int tier,
        int power,
        int attack,
        int defense,
        int health,
        int speed,
        ResourceCost trainCost,
        string techLine)
    {
        string folder = UnitsRoot + "/" + GetNationFolder(nation);
        EnsureFolder(folder);
        TuranUnitData unit = LoadOrCreate<TuranUnitData>(folder + "/UD_" + id + ".asset");
        unit.unitId = id;
        unit.displayName = displayName;
        unit.weaponOrVehicleName = weapon;
        unit.nation = nation;
        unit.rarity = rarity;
        unit.branch = TuranForceBranch.LandForces;
        unit.kind = kind;
        unit.era = era;
        unit.techLine = techLine;
        unit.weaponData = CreateWeaponForUnit(id, weapon, nation, kind, era, rarity, attack, tier);
        unit.visualProfile = CreateVisualProfileForUnit(id, displayName, kind, techLine);
        unit.baseStars = stars;
        unit.tier = tier;
        unit.power = power;
        unit.attack = attack;
        unit.defense = defense;
        unit.health = health;
        unit.marchSpeed = speed;
        unit.baseSquadSize = kind == TuranUnitKind.Logistics ? 1 : 9;
        unit.secondsPerSoldier = Mathf.Max(1f, 0.9f + tier * 0.55f);
        unit.trainCostPerSoldier = trainCost;
        unit.mergeRequiredCopies = 3;
        EditorUtility.SetDirty(unit);
        units[id] = unit;
        return unit;
    }

    private static WeaponData CreateWeaponForUnit(
        string unitId,
        string weaponName,
        TuranNation nation,
        TuranUnitKind kind,
        TuranUnitEra era,
        TuranRarity rarity,
        int attack,
        int tier)
    {
        EnsureFolder(WeaponsRoot + "/" + GetNationFolder(nation));
        WeaponData weapon = LoadOrCreate<WeaponData>(WeaponsRoot + "/" + GetNationFolder(nation) + "/WP_" + unitId + ".asset");
        weapon.weaponId = "wp_" + unitId;
        weapon.displayName = weaponName;
        weapon.nation = nation;
        weapon.rarity = rarity;
        weapon.category = GetWeaponCategory(kind, weaponName);
        weapon.era = era;
        weapon.attack = attack;
        weapon.range = GetWeaponRange(kind, tier);
        weapon.fireRate = GetWeaponFireRate(kind);
        weapon.armorPiercing = kind == TuranUnitKind.Tank || kind == TuranUnitKind.RocketArtillery ? tier * 8 : tier * 2;
        weapon.socketLocalPosition = Vector3.zero;
        weapon.socketLocalRotation = Vector3.zero;
        weapon.socketLocalScale = Vector3.one;
        EditorUtility.SetDirty(weapon);
        return weapon;
    }

    private static UnitVisualProfile CreateVisualProfileForUnit(
        string unitId,
        string displayName,
        TuranUnitKind kind,
        string techLine)
    {
        EnsureFolder(VisualProfilesRoot);
        UnitVisualProfile profile = LoadOrCreate<UnitVisualProfile>(VisualProfilesRoot + "/UVP_" + unitId + ".asset");
        profile.profileId = "uvp_" + unitId;
        profile.displayName = displayName + " Gorseli";
        profile.formationShape = GetFormationShape(kind, techLine);
        profile.visibleSoldierCount = kind == TuranUnitKind.Infantry ? 4 : 1;
        profile.spacing = kind == TuranUnitKind.Infantry ? 0.48f : 0.35f;
        profile.hexFillRatio = kind == TuranUnitKind.Infantry ? 0.42f : 0.34f;
        profile.weaponSocketName = "WeaponSocket";
        profile.fallbackHandBone = HumanBodyBones.RightHand;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static WeaponCategory GetWeaponCategory(TuranUnitKind kind, string weaponName)
    {
        if (kind == TuranUnitKind.Tank)
            return WeaponCategory.TankGun;
        if (kind == TuranUnitKind.Artillery)
            return WeaponCategory.Howitzer;
        if (kind == TuranUnitKind.RocketArtillery)
            return WeaponCategory.RocketLauncher;
        if (kind == TuranUnitKind.Logistics)
            return WeaponCategory.LogisticsTool;
        if (!string.IsNullOrEmpty(weaponName) && weaponName.ToLowerInvariant().Contains("istiglal"))
            return WeaponCategory.SniperRifle;
        return WeaponCategory.Rifle;
    }

    private static UnitFormationShape GetFormationShape(TuranUnitKind kind, string techLine)
    {
        if (kind == TuranUnitKind.Tank || kind == TuranUnitKind.Logistics)
            return UnitFormationShape.VehicleSingle;
        if (kind == TuranUnitKind.Artillery || kind == TuranUnitKind.RocketArtillery)
            return UnitFormationShape.ArtillerySingle;
        if (!string.IsNullOrEmpty(techLine) && techLine.Contains("destek"))
            return UnitFormationShape.Column;
        return UnitFormationShape.Fireteam;
    }

    private static float GetWeaponRange(TuranUnitKind kind, int tier)
    {
        if (kind == TuranUnitKind.Artillery)
            return 8f + tier * 1.2f;
        if (kind == TuranUnitKind.RocketArtillery)
            return 9f + tier * 1.5f;
        if (kind == TuranUnitKind.Tank)
            return 5.5f + tier * 0.4f;
        return 4f + tier * 0.25f;
    }

    private static float GetWeaponFireRate(TuranUnitKind kind)
    {
        if (kind == TuranUnitKind.Artillery || kind == TuranUnitKind.RocketArtillery)
            return 0.35f;
        if (kind == TuranUnitKind.Tank)
            return 0.55f;
        return 1.15f;
    }

    private static OfficerData CreateOfficer(
        string id,
        string displayName,
        OfficerLegacyType legacyType,
        string shortBiography,
        TuranUnitKind preferredKind,
        OfficerTendency tendency,
        int attack,
        int defense,
        int health,
        int gathering,
        int training)
    {
        OfficerData officer = LoadOrCreate<OfficerData>(OfficersRoot + "/OD_" + id + ".asset");
        officer.officerId = id;
        officer.displayName = displayName;
        officer.legacyType = legacyType;
        officer.shortBiography = shortBiography;
        officer.branch = TuranForceBranch.LandForces;
        officer.preferredUnitKind = preferredKind;
        officer.assignment = preferredKind == TuranUnitKind.Logistics ? OfficerAssignment.Base : OfficerAssignment.Field;
        officer.tendency = tendency;
        officer.attackBonusPercent = attack;
        officer.defenseBonusPercent = defense;
        officer.healthBonusPercent = health;
        officer.gatheringBonusPercent = gathering;
        officer.trainingSpeedBonusPercent = training;
        EditorUtility.SetDirty(officer);
        return officer;
    }

    private static bool NeedsStarterDatabaseBuild()
    {
        UnitCatalogData catalog = AssetDatabase.LoadAssetAtPath<UnitCatalogData>(DatabasesRoot + "/UnitCatalog_Starter.asset");
        if (catalog == null || catalog.units == null || catalog.units.Count == 0)
            return true;

        foreach (TuranUnitData unit in catalog.units)
        {
            if (unit != null && (unit.weaponData == null || unit.visualProfile == null))
                return true;
        }

        OfficerCatalogData officers = AssetDatabase.LoadAssetAtPath<OfficerCatalogData>(DatabasesRoot + "/OfficerCatalog_Starter.asset");
        return officers == null || officers.officers == null || officers.officers.Count < 4;
    }

    private static void CreateChest(
        string id,
        string displayName,
        TuranNation nation,
        string path,
        Dictionary<string, TuranUnitData> units,
        params string[] unitIds)
    {
        RewardChestData chest = LoadOrCreate<RewardChestData>(path);
        chest.chestId = id;
        chest.displayName = displayName;
        chest.nation = nation;
        chest.unitRewards.Clear();

        for (int i = 0; i < unitIds.Length; i++)
        {
            TuranUnitData unit;
            if (!units.TryGetValue(unitIds[i], out unit))
                continue;

            chest.unitRewards.Add(new UnitRewardEntry
            {
                unitData = unit,
                copyAmount = 1,
                weight = Mathf.Max(2, 20 - unit.tier * 3)
            });
        }

        EditorUtility.SetDirty(chest);
    }

    private static void Link(Dictionary<string, TuranUnitData> units, string from, string to)
    {
        if (!units.ContainsKey(from) || !units.ContainsKey(to))
            return;

        units[from].mergeResult = units[to];
        units[from].mergeRequiredCopies = 3;
        EditorUtility.SetDirty(units[from]);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/"));
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static string GetNationFolder(TuranNation nation)
    {
        if (nation == TuranNation.Turkiye) return "Turkiye";
        if (nation == TuranNation.Azerbaijan) return "Azerbaycan";
        if (nation == TuranNation.Kazakhstan) return "Kazakistan";
        if (nation == TuranNation.Kyrgyzstan) return "Kirgizistan";
        if (nation == TuranNation.Uzbekistan) return "Ozbekistan";
        return "Turan";
    }

    private static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = System.IO.Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
