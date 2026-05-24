using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanelUI : MonoBehaviour
{
    private enum PanelMode
    {
        None,
        Officers,
        Units,
        Supply,
        Alliance,
        Army,
        Build
    }
    private enum SupplyCategory
    {
        Materials,
        Coupons,
        Speedups,
        Chests
    }

    private readonly List<GameObject> contentRows = new List<GameObject>();
    private readonly List<TMP_Text> armyStatusTexts = new List<TMP_Text>();
    private readonly List<BarracksBuilding> armyStatusBarracks = new List<BarracksBuilding>();
    private float armyRefreshTimer;

    private Canvas canvas;
    private RectTransform menuRoot;
    private RectTransform panelRoot;
    private RectTransform contentRoot;
    private TMP_Text titleText;
    private PanelMode currentMode = PanelMode.None;
    private PlayerProfileUI playerProfileUI;
    private Button buildMenuButton;
    private TuranUnitKind selectedUnitsKind = TuranUnitKind.Infantry;
    private TuranUnitData selectedUnitDetail;
    private OfficerData selectedOfficerDetail;
    private SupplyCategory selectedSupplyCategory = SupplyCategory.Materials;
    private bool isBaseMode = true;
    private RectTransform activeArmyUnitPicker;
    private RectTransform activeArmyOfficerPicker;

    void Start()
    {
        canvas = FindScreenCanvas();
        if (canvas == null)
            return;

        EnsureConstructionManager();
        playerProfileUI = FindAnyObjectByType<PlayerProfileUI>();
        BuildMenu();

        if (PlayerRosterManager.Instance != null)
            PlayerRosterManager.Instance.RosterChanged += RefreshPanel;

        if (BaseConstructionManager.Instance != null)
            BaseConstructionManager.Instance.ConstructionChanged += RefreshPanel;

        GameModeManager modeManager = FindAnyObjectByType<GameModeManager>();
        if (modeManager != null)
        {
            isBaseMode = modeManager.CurrentMode == GameViewMode.BaseView;
            modeManager.ModeChanged += OnModeChanged;
        }

        UpdateMenuButtonsForMode();
    }

    void Update()
    {
        if (panelRoot == null || !panelRoot.gameObject.activeSelf)
            return;

        if (currentMode != PanelMode.Army)
            return;

        armyRefreshTimer -= Time.deltaTime;
        if (armyRefreshTimer > 0f)
            return;

        armyRefreshTimer = 0.2f;
        UpdateArmyPanelTexts();
    }
    void OnDestroy()
    {
        if (PlayerRosterManager.Instance != null)
            PlayerRosterManager.Instance.RosterChanged -= RefreshPanel;

        if (BaseConstructionManager.Instance != null)
            BaseConstructionManager.Instance.ConstructionChanged -= RefreshPanel;

        GameModeManager modeManager = FindAnyObjectByType<GameModeManager>();
        if (modeManager != null)
            modeManager.ModeChanged -= OnModeChanged;
    }

    private void BuildMenu()
    {
        GameObject rootObject = new GameObject("MainMenuRoot", typeof(RectTransform));
        rootObject.transform.SetParent(canvas.transform, false);
        menuRoot = rootObject.GetComponent<RectTransform>();
        menuRoot.anchorMin = new Vector2(1f, 0f);
        menuRoot.anchorMax = new Vector2(1f, 0f);
        menuRoot.pivot = new Vector2(1f, 0f);
        menuRoot.anchoredPosition = new Vector2(-12f, 6f);
        menuRoot.sizeDelta = new Vector2(360f, 24f);

        buildMenuButton = CreateButton(menuRoot, "InsaButton", "Insa", new Vector2(0f, 0f), new Vector2(44f, 22f), () => TogglePanel(PanelMode.Build));

        CreateButton(menuRoot, "OfficersButton", "Subaylar", new Vector2(48f, 0f), new Vector2(56f, 22f), () => TogglePanel(PanelMode.Officers));
        CreateButton(menuRoot, "UnitsButton", "Birimler", new Vector2(108f, 0f), new Vector2(56f, 22f), () => TogglePanel(PanelMode.Units));
        CreateButton(menuRoot, "SupplyButton", "Ikmal", new Vector2(168f, 0f), new Vector2(48f, 22f), () => TogglePanel(PanelMode.Supply));
        CreateButton(menuRoot, "AllianceButton", "Ittifak", new Vector2(220f, 0f), new Vector2(50f, 22f), () => TogglePanel(PanelMode.Alliance));
        CreateButton(menuRoot, "ArmyButton", "Ordu", new Vector2(274f, 0f), new Vector2(46f, 22f), () => TogglePanel(PanelMode.Army));

        BuildPanel();
    }

    private void BuildPanel()
    {
        GameObject panelObject =
            new GameObject("MainMenuPanel", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        panelObject.transform.SetParent(canvas.transform, false);

        panelRoot = panelObject.GetComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0.08f, 0.26f);
        panelRoot.anchorMax = new Vector2(0.92f, 0.86f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.anchoredPosition = Vector2.zero;
        panelRoot.sizeDelta = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.11f, 0.92f);

        titleText = CreateText(panelRoot, "Title", "", 16f, new Vector2(16f, -12f), new Vector2(420f, 24f));

        Button closeButton = CreateButton(panelRoot, "CloseButton", "Kapat", new Vector2(-78f, -10f), new Vector2(62f, 24f), ClosePanel, true);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(0f, 1f);

        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(panelRoot, false);
        contentRoot = contentObject.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 0f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.offsetMin = new Vector2(12f, 12f);
        contentRoot.offsetMax = new Vector2(-12f, -44f);

        panelObject.SetActive(false);
    }

    private void TogglePanel(PanelMode mode)
    {
        if (currentMode == mode && panelRoot.gameObject.activeSelf)
        {
            ClosePanel();
            return;
        }

        currentMode = mode;
        panelRoot.gameObject.SetActive(true);
        panelRoot.SetAsLastSibling();
        menuRoot.SetAsLastSibling();

        if (playerProfileUI == null)
            playerProfileUI = FindAnyObjectByType<PlayerProfileUI>();

        if (playerProfileUI != null)
            playerProfileUI.SetVisible(false);

        // Ust kaynak cubugu panel acikken de gorunur kalsin.

        RefreshPanel();
    }

    private void ClosePanel()
    {
        currentMode = PanelMode.None;
        if (activeArmyUnitPicker != null)
            Destroy(activeArmyUnitPicker.gameObject);
        if (activeArmyOfficerPicker != null)
            Destroy(activeArmyOfficerPicker.gameObject);

        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);

        if (playerProfileUI == null)
            playerProfileUI = FindAnyObjectByType<PlayerProfileUI>();

        if (playerProfileUI != null)
            playerProfileUI.SetVisible(true);

        // Ust kaynak cubugu her zaman acik.
    }

    private void RefreshPanel()
    {
        if (panelRoot == null || !panelRoot.gameObject.activeSelf)
            return;

        ClearRows();
        armyStatusTexts.Clear();
        armyStatusBarracks.Clear();

        if (currentMode == PanelMode.Officers)
            BuildOfficersPanel();
        else if (currentMode == PanelMode.Units)
            BuildUnitsPanel();
        else if (currentMode == PanelMode.Army)
            BuildArmyPanel();
        else if (currentMode == PanelMode.Build)
            BuildConstructionPanel();
        else if (currentMode == PanelMode.Supply)
            BuildSupplyPanel();
        else if (currentMode == PanelMode.Alliance)
            BuildInfoPanel("Ittifak", "Ittifak uyeleri, yetkiler ve toprak sistemi burada acilacak.");
    }

    private void BuildOfficersPanel()
    {
        titleText.text = "Subaylar";

        PlayerRosterManager roster = PlayerRosterManager.Instance;
        if (roster == null || roster.ownedOfficers.Count == 0)
        {
            AddTextRow("Henuz subay yok.");
            return;
        }

        List<OwnedOfficerEntry> unlockedOfficers = new List<OwnedOfficerEntry>();
        foreach (OwnedOfficerEntry entry in roster.ownedOfficers)
        {
            if (entry == null || entry.officer == null || !entry.unlocked)
                continue;

            unlockedOfficers.Add(entry);
        }

        if (unlockedOfficers.Count == 0)
        {
            AddTextRow("Acilmis subay yok.");
            return;
        }

        if (selectedOfficerDetail == null || !HasUnlockedOfficer(unlockedOfficers, selectedOfficerDetail))
            selectedOfficerDetail = unlockedOfficers[0].officer;

        RectTransform portraitListRoot = CreatePanelBox(contentRoot, "OfficerPortraitList", new Color(0.09f, 0.10f, 0.11f, 0.96f));
        portraitListRoot.anchorMin = new Vector2(0f, 0f);
        portraitListRoot.anchorMax = new Vector2(0f, 1f);
        portraitListRoot.pivot = new Vector2(0f, 0.5f);
        portraitListRoot.anchoredPosition = Vector2.zero;
        portraitListRoot.sizeDelta = new Vector2(116f, 0f);

        RectTransform heroRoot = CreatePanelBox(contentRoot, "OfficerHero", new Color(0.10f, 0.08f, 0.08f, 0.72f));
        heroRoot.anchorMin = new Vector2(0f, 0f);
        heroRoot.anchorMax = new Vector2(1f, 1f);
        heroRoot.offsetMin = new Vector2(124f, 0f);
        heroRoot.offsetMax = new Vector2(-184f, 0f);

        RectTransform detailRoot = CreatePanelBox(contentRoot, "OfficerDetail", new Color(0.13f, 0.09f, 0.09f, 0.96f));
        detailRoot.anchorMin = new Vector2(1f, 0f);
        detailRoot.anchorMax = new Vector2(1f, 1f);
        detailRoot.pivot = new Vector2(1f, 0.5f);
        detailRoot.anchoredPosition = Vector2.zero;
        detailRoot.sizeDelta = new Vector2(174f, 0f);

        int rowCount = Mathf.CeilToInt(unlockedOfficers.Count / 2f);
        float portraitContentHeight = Mathf.Max(220f, 10f + rowCount * 56f);
        RectTransform portraitContent = CreateVerticalScrollContent(
            portraitListRoot,
            "OfficerPortraitScroll",
            new Vector2(6f, 6f),
            new Vector2(-6f, -6f),
            portraitContentHeight);

        for (int i = 0; i < unlockedOfficers.Count; i++)
            AddOfficerPortraitCard(portraitContent, unlockedOfficers[i], i);

        OwnedOfficerEntry selectedEntry = GetOfficerEntry(unlockedOfficers, selectedOfficerDetail);
        BuildOfficerHeroPanel(heroRoot, selectedEntry);
        BuildOfficerDetailPanel(detailRoot, selectedEntry);
    }

    private bool HasUnlockedOfficer(List<OwnedOfficerEntry> entries, OfficerData officer)
    {
        return GetOfficerEntry(entries, officer) != null;
    }

    private OwnedOfficerEntry GetOfficerEntry(List<OwnedOfficerEntry> entries, OfficerData officer)
    {
        if (entries == null || officer == null)
            return null;

        foreach (OwnedOfficerEntry entry in entries)
        {
            if (entry != null && entry.officer == officer)
                return entry;
        }

        return null;
    }

    private void AddOfficerPortraitCard(RectTransform parent, OwnedOfficerEntry entry, int index)
    {
        if (entry == null || entry.officer == null)
            return;

        OfficerData officer = entry.officer;
        int column = index % 2;
        int row = index / 2;

        GameObject cardObject = new GameObject("OfficerCard_" + officer.officerId, typeof(RectTransform), typeof(Image), typeof(Button));
        cardObject.transform.SetParent(parent, false);

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(6f + column * 48f, -8f - row * 56f);
        rect.sizeDelta = new Vector2(42f, 50f);

        Image image = cardObject.GetComponent<Image>();
        image.color = officer == selectedOfficerDetail
            ? new Color(0.72f, 0.58f, 0.30f, 0.98f)
            : new Color(0.18f, 0.19f, 0.20f, 0.98f);

        Button button = cardObject.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            selectedOfficerDetail = officer;
            RefreshPanel();
        });

        GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(rect, false);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 1f);
        portraitRect.anchorMax = new Vector2(0f, 1f);
        portraitRect.pivot = new Vector2(0f, 1f);
        portraitRect.anchoredPosition = new Vector2(3f, -3f);
        portraitRect.sizeDelta = new Vector2(36f, 30f);

        Image portraitImage = portraitObject.GetComponent<Image>();
        portraitImage.sprite = officer.portrait;
        portraitImage.color = officer.portrait != null
            ? Color.white
            : GetOfficerColor(officer);

        TMP_Text levelText = CreateText(rect, "Level", "Lv " + entry.level, 7f, new Vector2(2f, -34f), new Vector2(38f, 10f));
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void BuildOfficerHeroPanel(RectTransform parent, OwnedOfficerEntry entry)
    {
        if (entry == null || entry.officer == null)
            return;

        OfficerData officer = entry.officer;

        GameObject portraitObject = new GameObject("LargePortrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(parent, false);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
        portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRect.pivot = new Vector2(0.5f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(0f, 8f);
        portraitRect.sizeDelta = new Vector2(118f, 138f);

        Image portraitImage = portraitObject.GetComponent<Image>();
        portraitImage.sprite = officer.portrait;
        portraitImage.color = officer.portrait != null
            ? Color.white
            : GetOfficerColor(officer);

        TMP_Text nameText = CreateText(parent, "Name", officer.displayName, 14f, new Vector2(12f, -10f), new Vector2(180f, 22f));
        nameText.fontStyle = FontStyles.Bold;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text roleText = CreateText(
            parent,
            "Role",
            $"{FormatBranch(officer.branch)} / {FormatKind(officer.preferredUnitKind)} / {FormatAssignment(officer.assignment)}",
            9f,
            new Vector2(12f, -32f),
            new Vector2(200f, 18f));
        roleText.textWrappingMode = TextWrappingModes.NoWrap;
        roleText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void BuildOfficerDetailPanel(RectTransform parent, OwnedOfficerEntry entry)
    {
        if (entry == null || entry.officer == null)
            return;

        OfficerData officer = entry.officer;
        string biography = string.IsNullOrWhiteSpace(officer.shortBiography)
            ? "Biyografi metni daha sonra doldurulacak."
            : officer.shortBiography;

        TMP_Text powerText = CreateText(parent, "Power", $"Guc {GetOfficerPower(officer, entry.level):N0}", 12.5f, new Vector2(8f, -8f), new Vector2(156f, 20f));
        powerText.fontStyle = FontStyles.Bold;

        TMP_Text roleText = CreateText(
            parent,
            "Role",
            $"{FormatTendency(officer.tendency)} Subayi\nLv.{entry.level} / Max Lv.{officer.maxLevel}",
            8.8f,
            new Vector2(8f, -34f),
            new Vector2(156f, 34f));
        roleText.alignment = TextAlignmentOptions.TopLeft;

        TMP_Text bonusText = CreateText(
            parent,
            "Bonuses",
            $"Saldiri %{officer.attackBonusPercent}\nSavunma %{officer.defenseBonusPercent}\nCan %{officer.healthBonusPercent}\nToplama %{officer.gatheringBonusPercent}\nEgitim %{officer.trainingSpeedBonusPercent}",
            8.8f,
            new Vector2(8f, -74f),
            new Vector2(156f, 68f));
        bonusText.alignment = TextAlignmentOptions.TopLeft;

        TMP_Text skillsTitle = CreateText(parent, "SkillsTitle", "Beceriler", 9.6f, new Vector2(8f, -146f), new Vector2(120f, 16f));
        skillsTitle.fontStyle = FontStyles.Bold;

        for (int i = 0; i < 4; i++)
            AddOfficerSkillBox(parent, officer, entry.level, i);

        TMP_Text bioText = CreateText(parent, "Bio", biography, 8.2f, new Vector2(8f, -192f), new Vector2(156f, 66f));
        bioText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void AddOfficerSkillBox(RectTransform parent, OfficerData officer, int level, int index)
    {
        bool unlocked = officer != null && officer.IsSkillUnlocked(index + 1, level);

        GameObject skillObject = new GameObject("Skill_" + (index + 1), typeof(RectTransform), typeof(Image));
        skillObject.transform.SetParent(parent, false);
        RectTransform rect = skillObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(8f + index * 38f, -162f);
        rect.sizeDelta = new Vector2(30f, 22f);

        Image image = skillObject.GetComponent<Image>();
        image.color = unlocked
            ? new Color(0.65f, 0.48f, 0.22f, 0.98f)
            : new Color(0.18f, 0.18f, 0.18f, 0.98f);

        TMP_Text text = CreateText(rect, "Text", unlocked ? "Acik" : "Kilit", 6.4f, Vector2.zero, rect.sizeDelta);
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private int GetOfficerPower(OfficerData officer, int level)
    {
        if (officer == null)
            return 0;

        int bonusTotal =
            officer.attackBonusPercent +
            officer.defenseBonusPercent +
            officer.healthBonusPercent +
            officer.gatheringBonusPercent +
            officer.trainingSpeedBonusPercent;

        return Mathf.Max(1, level) * 120 + bonusTotal * 85;
    }

    private Color GetOfficerColor(OfficerData officer)
    {
        if (officer == null)
            return new Color(0.22f, 0.27f, 0.30f, 1f);

        if (officer.legacyType == OfficerLegacyType.Martyr)
            return new Color(0.42f, 0.30f, 0.28f, 1f);
        if (officer.tendency == OfficerTendency.Defense)
            return new Color(0.23f, 0.34f, 0.42f, 1f);
        if (officer.tendency == OfficerTendency.Support)
            return new Color(0.36f, 0.32f, 0.44f, 1f);
        if (officer.tendency == OfficerTendency.Economy)
            return new Color(0.28f, 0.40f, 0.32f, 1f);

        return new Color(0.42f, 0.32f, 0.24f, 1f);
    }

    private void BuildUnitsPanel()
    {
        titleText.text = "Birimler";

        PlayerRosterManager roster = PlayerRosterManager.Instance;
        if (roster == null || roster.ownedUnits.Count == 0)
        {
            AddTextRow("Henuz birim yok.");
            return;
        }

        RectTransform categoryRoot = CreatePanelBox(contentRoot, "UnitCategories", new Color(0.10f, 0.12f, 0.13f, 0.96f));
        categoryRoot.anchorMin = new Vector2(0f, 0f);
        categoryRoot.anchorMax = new Vector2(0f, 1f);
        categoryRoot.pivot = new Vector2(0f, 0.5f);
        categoryRoot.anchoredPosition = Vector2.zero;
        categoryRoot.sizeDelta = new Vector2(104f, 0f);

        CreateUnitCategoryButton(categoryRoot, "Piyade", TuranUnitKind.Infantry, 0);
        CreateUnitCategoryButton(categoryRoot, "Tank", TuranUnitKind.Tank, 1);
        CreateUnitCategoryButton(categoryRoot, "Topcu", TuranUnitKind.Artillery, 2);
        CreateUnitCategoryButton(categoryRoot, "Roket", TuranUnitKind.RocketArtillery, 3);

        RectTransform gridRoot = CreatePanelBox(contentRoot, "UnitGrid", new Color(0.07f, 0.08f, 0.09f, 0.35f));
        gridRoot.anchorMin = new Vector2(0f, 0f);
        gridRoot.anchorMax = new Vector2(1f, 1f);
        gridRoot.offsetMin = new Vector2(112f, 0f);
        gridRoot.offsetMax = new Vector2(-166f, 0f);

        RectTransform detailRoot = CreatePanelBox(contentRoot, "UnitDetail", new Color(0.12f, 0.14f, 0.16f, 0.96f));
        detailRoot.anchorMin = new Vector2(1f, 0f);
        detailRoot.anchorMax = new Vector2(1f, 1f);
        detailRoot.pivot = new Vector2(1f, 0.5f);
        detailRoot.anchoredPosition = Vector2.zero;
        detailRoot.sizeDelta = new Vector2(142f, 0f);

        List<OwnedUnitEntry> filtered = new List<OwnedUnitEntry>();
        foreach (OwnedUnitEntry entry in roster.ownedUnits)
        {
            if (entry == null || entry.unitData == null || entry.count <= 0 || entry.unitData.kind == TuranUnitKind.Logistics)
                continue;

            if (entry.unitData.kind == selectedUnitsKind)
                filtered.Add(entry);
        }

        int rowCount = Mathf.Max(1, filtered.Count);
        float contentHeight = Mathf.Max(220f, 10f + rowCount * 62f);
        RectTransform gridContent = CreateVerticalScrollContent(
            gridRoot,
            "UnitGridScroll",
            new Vector2(6f, 6f),
            new Vector2(-6f, -6f),
            contentHeight);

        if (filtered.Count > 0 &&
            (selectedUnitDetail == null || selectedUnitDetail.kind != selectedUnitsKind))
        {
            selectedUnitDetail = filtered[0].unitData;
        }

        if (filtered.Count == 0)
        {
            TMP_Text emptyText = CreateText(gridContent, "Empty", "Bu kategoride acilmis tim yok.", 10f, new Vector2(8f, -8f), new Vector2(240f, 22f));
            emptyText.alignment = TextAlignmentOptions.TopLeft;
            selectedUnitDetail = null;
        }

        for (int i = 0; i < filtered.Count; i++)
        {
            int index = i;
            AddUnitCard(gridContent, filtered[index], index);
        }

        BuildUnitDetailPanel(detailRoot, selectedUnitDetail, roster);
    }

    private void CreateUnitCategoryButton(RectTransform parent, string label, TuranUnitKind kind, int index)
    {
        Button button = CreateButton(
            parent,
            label + "Category",
            label,
            new Vector2(6f, -8f - index * 36f),
            new Vector2(90f, 30f),
            () =>
            {
                selectedUnitsKind = kind;
                selectedUnitDetail = null;
                RefreshPanel();
            },
            true);

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selectedUnitsKind == kind
                ? new Color(0.72f, 0.60f, 0.34f, 0.98f)
                : new Color(0.20f, 0.22f, 0.23f, 0.98f);
        }
    }

    private void AddUnitCard(RectTransform parent, OwnedUnitEntry entry, int index)
    {
        if (entry == null || entry.unitData == null)
            return;

        TuranUnitData unit = entry.unitData;
        int row = index;

        GameObject cardObject = new GameObject("UnitCard_" + unit.unitId, typeof(RectTransform), typeof(Image), typeof(Button));
        cardObject.transform.SetParent(parent, false);

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(8f, -8f - row * 62f);
        rect.sizeDelta = new Vector2(-16f, 56f);

        Image image = cardObject.GetComponent<Image>();
        image.color = unit == selectedUnitDetail
            ? new Color(0.22f, 0.30f, 0.34f, 0.98f)
            : new Color(0.13f, 0.16f, 0.18f, 0.96f);

        Button button = cardObject.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            selectedUnitDetail = unit;
            RefreshPanel();
        });

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(rect, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(4f, -4f);
        iconRect.sizeDelta = new Vector2(30f, 26f);
        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = unit.icon;
        iconImage.color = unit.icon != null ? Color.white : GetUnitCardColor(unit.kind);

        TMP_Text nameText = CreateText(rect, "Name", unit.displayName, 7.7f, new Vector2(38f, -4f), new Vector2(132f, 12f));
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text metaText = CreateText(
            rect,
            "Meta",
            $"x{entry.count}  {unit.tier}.Tier\n{Stars(unit.baseStars)}  Guc {unit.power}",
            7.1f,
            new Vector2(38f, -18f),
            new Vector2(132f, 30f));
        metaText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void BuildUnitDetailPanel(RectTransform parent, TuranUnitData unit, PlayerRosterManager roster)
    {
        if (unit == null)
        {
            TMP_Text empty = CreateText(parent, "EmptyDetail", "Tim sec.", 10.5f, new Vector2(8f, -10f), new Vector2(138f, 24f));
            empty.alignment = TextAlignmentOptions.TopLeft;
            return;
        }

        TMP_Text title = CreateText(parent, "DetailTitle", unit.displayName, 11f, new Vector2(8f, -8f), new Vector2(138f, 20f));
        title.fontStyle = FontStyles.Bold;
        title.overflowMode = TextOverflowModes.Ellipsis;

        string weaponName = unit.weaponData != null ? unit.weaponData.displayName : unit.weaponOrVehicleName;
        string detail =
            $"{FormatKind(unit.kind)}\n" +
            $"{weaponName}\n" +
            $"{Stars(unit.baseStars)} / Tier {unit.tier}\n\n" +
            $"Guc {unit.power}\n" +
            $"Saldiri {unit.attack}\n" +
            $"Savunma {unit.defense}\n" +
            $"Can {unit.health}\n" +
            $"Hiz {unit.marchSpeed}";

        TMP_Text body = CreateText(parent, "DetailBody", detail, 8.8f, new Vector2(8f, -32f), new Vector2(138f, 132f));
        body.alignment = TextAlignmentOptions.TopLeft;

        Button mergeButton = CreateButton(
            parent,
            "DetailMerge",
            "Birlestir",
            new Vector2(8f, -172f),
            new Vector2(122f, 22f),
            () =>
            {
                if (PlayerRosterManager.Instance != null)
                    PlayerRosterManager.Instance.TryMerge(unit);

                RefreshPanel();
            },
            true);

        mergeButton.interactable = roster != null && roster.CanMerge(unit);
    }

    private RectTransform CreatePanelBox(RectTransform parent, string objectName, Color color)
    {
        GameObject boxObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        boxObject.transform.SetParent(parent, false);
        Image image = boxObject.GetComponent<Image>();
        image.color = color;
        return boxObject.GetComponent<RectTransform>();
    }

    private RectTransform CreateVerticalScrollContent(
        RectTransform parent,
        string objectName,
        Vector2 offsetMin,
        Vector2 offsetMax,
        float contentHeight)
    {
        GameObject viewportObject = new GameObject(
            objectName + "_Viewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask),
            typeof(ScrollRect));
        viewportObject.transform.SetParent(parent, false);

        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = offsetMin;
        viewportRect.offsetMax = offsetMax;

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        Mask mask = viewportObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = new GameObject(objectName, typeof(RectTransform));
        contentObject.transform.SetParent(viewportRect, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, Mathf.Max(10f, contentHeight));

        ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.elasticity = 0.1f;
        scroll.scrollSensitivity = 18f;
        scroll.verticalNormalizedPosition = 1f;

        return contentRect;
    }

    private Color GetUnitCardColor(TuranUnitKind kind)
    {
        if (kind == TuranUnitKind.Tank)
            return new Color(0.42f, 0.52f, 0.38f, 1f);
        if (kind == TuranUnitKind.Artillery)
            return new Color(0.52f, 0.46f, 0.34f, 1f);
        if (kind == TuranUnitKind.RocketArtillery)
            return new Color(0.45f, 0.36f, 0.52f, 1f);
        return new Color(0.32f, 0.42f, 0.34f, 1f);
    }

    private void BuildArmyPanel()
    {
        titleText.text = "Ordu";

        BarracksBuilding[] barracks =
            FindObjectsByType<BarracksBuilding>(FindObjectsInactive.Include);
        System.Array.Sort(
            barracks,
            (a, b) => string.Compare(
                a != null ? a.name : string.Empty,
                b != null ? b.name : string.Empty,
                System.StringComparison.Ordinal
            )
        );

        RectTransform listRoot = CreatePanelBox(contentRoot, "ArmySlotList", new Color(0.07f, 0.08f, 0.09f, 0.30f));
        listRoot.anchorMin = new Vector2(0f, 0f);
        listRoot.anchorMax = new Vector2(1f, 1f);
        listRoot.offsetMin = Vector2.zero;
        listRoot.offsetMax = Vector2.zero;

        RectTransform scrollContent = CreateVerticalScrollContent(
            listRoot,
            "ArmySlotsScroll",
            new Vector2(6f, 6f),
            new Vector2(-6f, -6f),
            Mathf.Max(220f, 8f + 5 * 50f));

        List<BarracksBuilding> existingBarracks = new List<BarracksBuilding>();
        if (barracks != null)
        {
            for (int i = 0; i < barracks.Length; i++)
            {
                if (barracks[i] != null)
                    existingBarracks.Add(barracks[i]);
            }
        }
        int unlockedSlots = Mathf.Clamp(existingBarracks.Count, 0, 5);

        for (int i = 0; i < 5; i++)
        {
            BarracksBuilding barrack = i < unlockedSlots ? existingBarracks[i] : null;
            AddArmySlotCard(scrollContent, i, barrack, i < unlockedSlots);
        }
    }

    private void AddArmySlotCard(RectTransform parent, int index, BarracksBuilding barrack, bool isUnlocked)
    {
        GameObject cardObject = new GameObject("ArmySlot_" + (index + 1), typeof(RectTransform), typeof(Image));
        cardObject.transform.SetParent(parent, false);

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * 50f);
        rect.sizeDelta = new Vector2(0f, 46f);

        Image image = cardObject.GetComponent<Image>();
        image.color = new Color(0.13f, 0.16f, 0.18f, 0.96f);

        GameObject numberObject = new GameObject("Number", typeof(RectTransform), typeof(Image));
        numberObject.transform.SetParent(rect, false);
        RectTransform numberRect = numberObject.GetComponent<RectTransform>();
        numberRect.anchorMin = new Vector2(0f, 0.5f);
        numberRect.anchorMax = new Vector2(0f, 0.5f);
        numberRect.pivot = new Vector2(0f, 0.5f);
        numberRect.anchoredPosition = new Vector2(8f, 0f);
        numberRect.sizeDelta = new Vector2(34f, 34f);
        numberObject.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.22f, 0.96f);

        TMP_Text numberText = CreateText(numberRect, "Text", (index + 1).ToString(), 15f, Vector2.zero, numberRect.sizeDelta);
        numberText.alignment = TextAlignmentOptions.Center;
        numberText.fontStyle = FontStyles.Bold;
        RectTransform numberTextRect = numberText.GetComponent<RectTransform>();
        numberTextRect.anchorMin = Vector2.zero;
        numberTextRect.anchorMax = Vector2.one;
        numberTextRect.offsetMin = Vector2.zero;
        numberTextRect.offsetMax = Vector2.zero;

        string rowLabel = isUnlocked
            ? GetArmyRowText(index, barrack)
            : $"Kisla {index + 1}: Kilitli\nKisla insa ederek bu slotu ac";

        TMP_Text rowText = CreateText(rect, "Status", rowLabel, 8.3f, new Vector2(50f, -6f), new Vector2(360f, 36f));
        rowText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform rowTextRect = rowText.GetComponent<RectTransform>();
        rowTextRect.anchorMin = new Vector2(0f, 0f);
        rowTextRect.anchorMax = new Vector2(1f, 1f);
        rowTextRect.offsetMin = new Vector2(50f, 5f);
        rowTextRect.offsetMax = new Vector2(-246f, -5f);
        if (isUnlocked && barrack != null)
        {
            armyStatusTexts.Add(rowText);
            armyStatusBarracks.Add(barrack);
        }

        Button unitButton = CreateButton(rect, "AssignUnit", "Tim", new Vector2(-252f, -12f), new Vector2(40f, 20f), () => ShowUnitPickerForBarracks(rect, barrack), true);
        Button officerButton = CreateButton(rect, "AssignOfficer", "Subay", new Vector2(-206f, -12f), new Vector2(48f, 20f), () => ShowOfficerPickerForBarracks(rect, barrack), true);
        Button trainButton = CreateButton(rect, "TrainOne", "+1", new Vector2(-152f, -12f), new Vector2(34f, 20f), () => TrainSoldiers(barrack, 1), true);
        Button trainMaxButton = CreateButton(rect, "TrainMax", "Max", new Vector2(-112f, -12f), new Vector2(38f, 20f), () => TrainMax(barrack), true);

        AnchorButtonRight(unitButton, 188f);
        AnchorButtonRight(officerButton, 136f);
        AnchorButtonRight(trainButton, 94f);
        AnchorButtonRight(trainMaxButton, 50f);

        bool canTrain = isUnlocked &&
            barrack != null &&
            barrack.assignedUnitData != null &&
            barrack.trainedSoldiers < barrack.SoldierCapacity &&
            !barrack.isTraining;
        trainButton.interactable = canTrain;
        trainMaxButton.interactable = canTrain;
        unitButton.interactable = isUnlocked && barrack != null;
        officerButton.interactable = isUnlocked && barrack != null;
        if (!isUnlocked)
        {
            unitButton.gameObject.SetActive(false);
            officerButton.gameObject.SetActive(false);
            trainButton.gameObject.SetActive(false);
            trainMaxButton.gameObject.SetActive(false);
        }
    }

    private void ShowUnitPickerForBarracks(RectTransform rowRect, BarracksBuilding barrack)
    {
        if (barrack == null || panelRoot == null)
            return;

        PlayerRosterManager roster = PlayerRosterManager.Instance;
        if (roster == null)
            return;

        if (activeArmyUnitPicker != null)
            Destroy(activeArmyUnitPicker.gameObject);
        if (activeArmyOfficerPicker != null)
            Destroy(activeArmyOfficerPicker.gameObject);

        if (barrack.assignedUnitData != null)
        {
            RectTransform lockedPicker = CreatePanelBox(panelRoot, "ArmyUnitPickerLocked", new Color(0.09f, 0.11f, 0.13f, 0.98f));
            lockedPicker.anchorMin = new Vector2(0.5f, 0.5f);
            lockedPicker.anchorMax = new Vector2(0.5f, 0.5f);
            lockedPicker.pivot = new Vector2(0.5f, 0.5f);
            lockedPicker.anchoredPosition = new Vector2(0f, -8f);
            lockedPicker.sizeDelta = new Vector2(300f, 110f);
            activeArmyUnitPicker = lockedPicker;

            CreateText(
                lockedPicker,
                "LockedInfo",
                "Bu slota tim atanmis.\nDegistirmek icin once kaldir.",
                9f,
                new Vector2(10f, -12f),
                new Vector2(220f, 36f));

            Button removeBtn = CreateButton(
                lockedPicker,
                "RemoveAssignedUnit",
                "Timi Kaldir",
                new Vector2(10f, -58f),
                new Vector2(98f, 24f),
                () =>
                {
                    if (PlayerRosterManager.Instance != null)
                        PlayerRosterManager.Instance.AssignUnitToBarracks(barrack, null);

                    if (activeArmyUnitPicker != null)
                        Destroy(activeArmyUnitPicker.gameObject);

                    RefreshPanel();
                },
                true);
            removeBtn.GetComponent<Image>().color = new Color(0.48f, 0.21f, 0.20f, 0.98f);

            Button closeLocked = CreateButton(lockedPicker, "ClosePicker", "X", new Vector2(266f, -8f), new Vector2(24f, 20f), () =>
            {
                if (activeArmyUnitPicker != null)
                    Destroy(activeArmyUnitPicker.gameObject);
            }, true);
            closeLocked.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f, 1f);
            return;
        }

        List<TuranUnitData> candidates = new List<TuranUnitData>();
        foreach (OwnedUnitEntry entry in roster.ownedUnits)
        {
            if (entry == null || entry.unitData == null || entry.count <= 0)
                continue;

            if (entry.unitData.kind == TuranUnitKind.Logistics)
                continue;

            candidates.Add(entry.unitData);
        }

        if (candidates.Count == 0)
            return;

        RectTransform picker = CreatePanelBox(panelRoot, "ArmyUnitPicker", new Color(0.09f, 0.11f, 0.13f, 0.98f));
        picker.anchorMin = new Vector2(0.5f, 0.5f);
        picker.anchorMax = new Vector2(0.5f, 0.5f);
        picker.pivot = new Vector2(0.5f, 0.5f);
        picker.anchoredPosition = new Vector2(0f, -8f);
        picker.sizeDelta = new Vector2(300f, Mathf.Min(180f, 14f + candidates.Count * 24f));
        activeArmyUnitPicker = picker;

        Button close = CreateButton(picker, "ClosePicker", "X", new Vector2(266f, -8f), new Vector2(24f, 20f), () =>
        {
            if (activeArmyUnitPicker != null)
                Destroy(activeArmyUnitPicker.gameObject);
        }, true);
        close.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f, 1f);

        RectTransform listContent = CreateVerticalScrollContent(
            picker,
            "UnitPickerScroll",
            new Vector2(8f, 8f),
            new Vector2(-30f, -30f),
            Mathf.Max(80f, 6f + candidates.Count * 24f));

        for (int i = 0; i < candidates.Count; i++)
        {
            TuranUnitData unit = candidates[i];
            Button unitBtn = CreateButton(
                listContent,
                "PickUnit_" + i,
                unit.displayName,
                new Vector2(4f, -4f - i * 24f),
                new Vector2(240f, 20f),
                () =>
                {
                    if (PlayerRosterManager.Instance != null)
                        PlayerRosterManager.Instance.AssignUnitToBarracks(barrack, unit);

                    if (activeArmyUnitPicker != null)
                        Destroy(activeArmyUnitPicker.gameObject);

                    RefreshPanel();
                },
                true);

            TMP_Text text = unitBtn.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.alignment = TextAlignmentOptions.Left;
                text.margin = new Vector4(8f, 0f, 0f, 0f);
                text.fontSize = 8.2f;
            }
        }
    }

    private void ShowOfficerPickerForBarracks(RectTransform rowRect, BarracksBuilding barrack)
    {
        if (barrack == null || panelRoot == null)
            return;

        PlayerRosterManager roster = PlayerRosterManager.Instance;
        if (roster == null)
            return;

        roster.EnsureStarterOfficers();

        List<OfficerData> candidates = new List<OfficerData>();
        foreach (OwnedOfficerEntry entry in roster.ownedOfficers)
        {
            if (entry == null || entry.officer == null || !entry.unlocked)
                continue;

            candidates.Add(entry.officer);
        }

        if (candidates.Count == 0)
            return;

        if (activeArmyOfficerPicker != null)
            Destroy(activeArmyOfficerPicker.gameObject);
        if (activeArmyUnitPicker != null)
            Destroy(activeArmyUnitPicker.gameObject);

        RectTransform picker = CreatePanelBox(panelRoot, "ArmyOfficerPicker", new Color(0.09f, 0.11f, 0.13f, 0.98f));
        picker.anchorMin = new Vector2(0.5f, 0.5f);
        picker.anchorMax = new Vector2(0.5f, 0.5f);
        picker.pivot = new Vector2(0.5f, 0.5f);
        picker.anchoredPosition = new Vector2(0f, -8f);
        picker.sizeDelta = new Vector2(300f, Mathf.Min(190f, 18f + candidates.Count * 24f));
        activeArmyOfficerPicker = picker;

        Button close = CreateButton(picker, "CloseOfficerPicker", "X", new Vector2(266f, -8f), new Vector2(24f, 20f), () =>
        {
            if (activeArmyOfficerPicker != null)
                Destroy(activeArmyOfficerPicker.gameObject);
        }, true);
        close.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f, 1f);

        RectTransform listContent = CreateVerticalScrollContent(
            picker,
            "OfficerPickerScroll",
            new Vector2(8f, 8f),
            new Vector2(-30f, -30f),
            Mathf.Max(80f, 6f + candidates.Count * 24f));

        for (int i = 0; i < candidates.Count; i++)
        {
            OfficerData officer = candidates[i];
            string officerName = string.IsNullOrWhiteSpace(officer.displayName) ? "Subay" : officer.displayName;
            Button officerBtn = CreateButton(
                listContent,
                "PickOfficer_" + i,
                officerName,
                new Vector2(4f, -4f - i * 24f),
                new Vector2(240f, 20f),
                () =>
                {
                    if (PlayerRosterManager.Instance != null)
                        PlayerRosterManager.Instance.AssignOfficerToBarracks(barrack, officer);

                    if (activeArmyOfficerPicker != null)
                        Destroy(activeArmyOfficerPicker.gameObject);

                    RefreshPanel();
                },
                true);

            TMP_Text text = officerBtn.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.alignment = TextAlignmentOptions.Left;
                text.margin = new Vector4(8f, 0f, 0f, 0f);
                text.fontSize = 8.2f;
            }

            RectTransform buttonRect = officerBtn.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f);
                buttonRect.anchoredPosition = new Vector2(0f, -4f - i * 24f);
                buttonRect.sizeDelta = new Vector2(-8f, 20f);
            }
        }
    }


    private void UpdateArmyPanelTexts()
    {
        int count = Mathf.Min(armyStatusTexts.Count, armyStatusBarracks.Count);
        for (int i = 0; i < count; i++)
        {
            TMP_Text text = armyStatusTexts[i];
            BarracksBuilding barrack = armyStatusBarracks[i];
            if (text == null || barrack == null)
                continue;

            barrack.RefreshTrainingProgress();
            text.text = GetArmyRowText(i, barrack);
        }
    }

    private string GetArmyRowText(int index, BarracksBuilding barrack)
    {
        string unitName = barrack.assignedUnitData != null
            ? barrack.assignedUnitData.displayName
            : "Tim yok";

        string officerName = barrack.assignedOfficer != null
            ? barrack.assignedOfficer.displayName
            : "Subay yok";

        string training = barrack.isTraining
            ? $"Egitim: {barrack.TrainingRemainingSeconds:0}s"
            : "Hazir";

        return $"Kisla {index + 1}: {unitName}\n{officerName} | Asker {barrack.trainedSoldiers}/{barrack.SoldierCapacity} | {training}";
    }

    private void BuildConstructionPanel()
    {
        titleText.text = "Insa";

        BaseConstructionManager construction = BaseConstructionManager.Instance;
        if (construction == null)
        {
            AddTextRow("Insa sistemi hazirlaniyor.", 46f);
            return;
        }

        foreach (ConstructionDefinition definition in construction.Definitions)
            AddConstructionRow(definition);
    }

    private void AddConstructionRow(ConstructionDefinition definition)
    {
        BaseConstructionManager construction = BaseConstructionManager.Instance;
        if (definition == null || construction == null)
            return;

        GameObject row = AddTextRow(construction.GetBuildStatus(definition), 48f);
        if (row == null)
            return;

        RectTransform rowRect = row.GetComponent<RectTransform>();
        Button buildButton =
            CreateButton(rowRect, "BuildButton", "Kur", new Vector2(-86f, -13f), new Vector2(72f, 24f), () =>
            {
                if (BaseConstructionManager.Instance != null)
                    BaseConstructionManager.Instance.TryConstruct(definition.type);

                RefreshPanel();
            }, true);
        AnchorButtonRight(buildButton, 14f);

        string reason;
        buildButton.interactable = construction.CanConstruct(definition, out reason);
    }

    private void BuildSupplyPanel()
    {
        titleText.text = "Ikmal Cantasi";

        MaterialInventoryManager inventory = MaterialInventoryManager.Instance;
        if (inventory == null)
        {
            AddTextRow("Canta hazirlaniyor.", 48f);
            return;
        }

        RectTransform categoryRoot = CreatePanelBox(contentRoot, "SupplyCategories", new Color(0.10f, 0.12f, 0.13f, 0.96f));
        categoryRoot.anchorMin = new Vector2(0f, 0f);
        categoryRoot.anchorMax = new Vector2(0f, 1f);
        categoryRoot.pivot = new Vector2(0f, 0.5f);
        categoryRoot.anchoredPosition = Vector2.zero;
        categoryRoot.sizeDelta = new Vector2(102f, 0f);

        AddSupplyCategory(categoryRoot, "Malzeme", SupplyCategory.Materials, 0);
        AddSupplyCategory(categoryRoot, "Kupon", SupplyCategory.Coupons, 1);
        AddSupplyCategory(categoryRoot, "Hizlandir", SupplyCategory.Speedups, 2);
        AddSupplyCategory(categoryRoot, "Sandik", SupplyCategory.Chests, 3);

        RectTransform gridRoot = CreatePanelBox(contentRoot, "SupplyGrid", new Color(0.07f, 0.08f, 0.09f, 0.35f));
        gridRoot.anchorMin = new Vector2(0f, 0f);
        gridRoot.anchorMax = new Vector2(1f, 1f);
        gridRoot.offsetMin = new Vector2(110f, 0f);
        gridRoot.offsetMax = new Vector2(-168f, 0f);

        RectTransform detailRoot = CreatePanelBox(contentRoot, "SupplyDetail", new Color(0.12f, 0.14f, 0.16f, 0.96f));
        detailRoot.anchorMin = new Vector2(1f, 0f);
        detailRoot.anchorMax = new Vector2(1f, 1f);
        detailRoot.pivot = new Vector2(1f, 0.5f);
        detailRoot.anchoredPosition = Vector2.zero;
        detailRoot.sizeDelta = new Vector2(148f, 0f);

        int cardCount =
            selectedSupplyCategory == SupplyCategory.Materials ? 4 :
            selectedSupplyCategory == SupplyCategory.Coupons ? 1 :
            selectedSupplyCategory == SupplyCategory.Speedups ? 1 : 2;

        int supplyColumns = 2;
        float supplyHeight = Mathf.Max(210f, 10f + Mathf.CeilToInt(cardCount / (float)supplyColumns) * 62f);
        RectTransform supplyContent = CreateVerticalScrollContent(
            gridRoot,
            "SupplyGridScroll",
            new Vector2(6f, 6f),
            new Vector2(-6f, -6f),
            supplyHeight);

        string detailTitleValue = "Depo";
        string detailBodyValue;
        int index = 0;

        if (selectedSupplyCategory == SupplyCategory.Materials)
        {
            AddSupplyCard(supplyContent, "Kalas", inventory.wood, new Color(0.55f, 0.34f, 0.18f, 1f), index++, supplyColumns);
            AddSupplyCard(supplyContent, "Beton", inventory.concrete, new Color(0.56f, 0.58f, 0.58f, 1f), index++, supplyColumns);
            AddSupplyCard(supplyContent, "Cimento", inventory.cement, new Color(0.72f, 0.72f, 0.68f, 1f), index++, supplyColumns);
            AddSupplyCard(supplyContent, "Tugla", inventory.brick, new Color(0.56f, 0.26f, 0.18f, 1f), index++, supplyColumns);
            detailBodyValue = "Uretim tesisi ve harita kaynaklarindan gelen ana malzemeler burada tutulur.";
        }
        else if (selectedSupplyCategory == SupplyCategory.Coupons)
        {
            AddSupplyCard(supplyContent, "Birlestirme Kuponu", inventory.mergeCoupons, new Color(0.70f, 0.56f, 0.26f, 1f), index++, supplyColumns);
            detailTitleValue = "Kuponlar";
            detailBodyValue = "Birlestirme ve ozel tim islemlerinde kullanilan kuponlar bu sekmede tutulur.";
        }
        else if (selectedSupplyCategory == SupplyCategory.Speedups)
        {
            AddSupplyCard(supplyContent, "Hizlandirma", inventory.speedupsMinutes, new Color(0.35f, 0.55f, 0.78f, 1f), index++, supplyColumns);
            detailTitleValue = "Hizlandirma";
            detailBodyValue = "Egitim, arastirma ve uretim surelerini azaltmak icin kullanilir.";
        }
        else
        {
            AddSupplyCard(supplyContent, "Turan Ortak Sandigi", inventory.rewardChests, new Color(0.48f, 0.34f, 0.62f, 1f), index++, supplyColumns);
            AddSupplyCard(supplyContent, "Turkiye Kara Sandigi", inventory.rewardChests, new Color(0.30f, 0.44f, 0.62f, 1f), index++, supplyColumns);
            detailTitleValue = "Sandiklar";
            detailBodyValue = "Acilabilir odul sandiklari burada birikir. Acma sistemi sonraki adimda eklenecek.";
        }

        TMP_Text detailTitle = CreateText(detailRoot, "DetailTitle", detailTitleValue, 12f, new Vector2(8f, -8f), new Vector2(138f, 20f));
        detailTitle.fontStyle = FontStyles.Bold;

        TMP_Text detailText = CreateText(
            detailRoot,
            "Detail",
            detailBodyValue + "\n\nKullanim aksiyonlari siradaki asamada buraya eklenecek.",
            8.8f,
            new Vector2(8f, -30f),
            new Vector2(138f, 146f));
        detailText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void AddSupplyCategory(RectTransform parent, string label, SupplyCategory category, int index)
    {
        Button button = CreateButton(
            parent,
            "SupplyCategory_" + label,
            label,
            new Vector2(6f, -8f - index * 36f),
            new Vector2(90f, 30f),
            () =>
            {
                selectedSupplyCategory = category;
                RefreshPanel();
            },
            true);

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selectedSupplyCategory == category
                ? new Color(0.72f, 0.60f, 0.34f, 0.98f)
                : new Color(0.20f, 0.22f, 0.23f, 0.98f);
        }
    }

    private void OnModeChanged(GameViewMode mode)
    {
        isBaseMode = mode == GameViewMode.BaseView;
        UpdateMenuButtonsForMode();
    }

    private void UpdateMenuButtonsForMode()
    {
        if (buildMenuButton != null)
            buildMenuButton.gameObject.SetActive(isBaseMode);

        if (!isBaseMode && currentMode == PanelMode.Build)
            ClosePanel();
    }

    private void AddSupplyCard(RectTransform parent, string label, int amount, Color color, int index, int columns)
    {
        int safeColumns = Mathf.Max(1, columns);
        int column = index % safeColumns;
        int row = index / safeColumns;

        GameObject cardObject = new GameObject("SupplyCard_" + label, typeof(RectTransform), typeof(Image));
        cardObject.transform.SetParent(parent, false);

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(8f + column * 80f, -8f - row * 62f);
        rect.sizeDelta = new Vector2(72f, 54f);

        Image image = cardObject.GetComponent<Image>();
        image.color = new Color(0.13f, 0.16f, 0.18f, 0.96f);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(rect, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -6f);
        iconRect.sizeDelta = new Vector2(24f, 18f);
        iconObject.GetComponent<Image>().color = color;

        TMP_Text amountText = CreateText(rect, "Amount", amount.ToString(), 7.6f, new Vector2(3f, -28f), new Vector2(64f, 12f));
        amountText.alignment = TextAlignmentOptions.Center;
        amountText.fontStyle = FontStyles.Bold;
        amountText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text labelText = CreateText(rect, "Label", label, 6.2f, new Vector2(3f, -40f), new Vector2(64f, 12f));
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void ClampPickerToContent(RectTransform picker)
    {
        if (picker == null || contentRoot == null)
            return;

        float width = contentRoot.rect.width;
        float height = contentRoot.rect.height;
        Vector2 size = picker.sizeDelta;
        Vector2 pos = picker.anchoredPosition;

        float minX = 6f;
        float maxX = Mathf.Max(minX, width - size.x - 6f);
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        float minY = -height + size.y + 6f;
        float maxY = -6f;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        picker.anchoredPosition = pos;
        picker.SetAsLastSibling();
    }

    private void BuildInfoPanel(string title, string body)
    {
        titleText.text = title;
        AddTextRow(body, 86f);
    }

    private void TrainSoldiers(BarracksBuilding barrack, int count)
    {
        if (barrack != null)
            barrack.StartTrainingSoldiers(count);

        RefreshPanel();
    }

    private void TrainMax(BarracksBuilding barrack)
    {
        if (barrack == null)
            return;

        int missing = barrack.SoldierCapacity - barrack.trainedSoldiers;
        if (missing <= 0)
            return;

        barrack.StartTrainingSoldiers(missing);
        RefreshPanel();
    }

    private GameObject AddTextRow(string text, float height = 62f)
    {
        float availableHeight =
            contentRoot.rect.height > 1f
                ? contentRoot.rect.height
                : panelRoot.sizeDelta.y - 54f;

        float nextY = contentRows.Count * (height + 5f);
        if (nextY + height > availableHeight)
            return null;

        GameObject rowObject =
            new GameObject("Row", typeof(RectTransform), typeof(Image));
        rowObject.transform.SetParent(contentRoot, false);

        RectTransform rect = rowObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -nextY);
        rect.sizeDelta = new Vector2(0f, height);

        Image image = rowObject.GetComponent<Image>();
        image.color = new Color(0.13f, 0.16f, 0.18f, 0.96f);

        TMP_Text rowText =
            CreateText(rect, "Text", text, 10.8f, new Vector2(12f, -6f), new Vector2(540f, height - 8f));
        rowText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform textRect = rowText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(12f, 5f);
        textRect.offsetMax = new Vector2(-154f, -5f);

        contentRows.Add(rowObject);
        return rowObject;
    }

    private void AnchorButtonRight(Button button, float rightPadding)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(-(rightPadding + rect.sizeDelta.x), rect.anchoredPosition.y);
    }

    private void EnsureConstructionManager()
    {
        if (BaseConstructionManager.Instance != null)
            return;

        BaseConstructionManager manager = FindAnyObjectByType<BaseConstructionManager>();
        if (manager != null)
            return;

        GameObject managerObject = new GameObject("BaseConstructionManager");
        managerObject.AddComponent<BaseConstructionManager>();
    }

    private void ClearRows()
    {
        contentRows.Clear();

        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    private Button CreateButton(
        RectTransform parent,
        string objectName,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick,
        bool topLeftPivot = false)
    {
        GameObject buttonObject =
            new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = topLeftPivot ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        rect.anchorMax = topLeftPivot ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        rect.pivot = topLeftPivot ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.43f, 0.55f, 0.98f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateText(rect, "Text", label, 10f, Vector2.zero, size);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private TMP_Text CreateText(
        RectTransform parent,
        string objectName,
        string text,
        float fontSize,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject textObject =
            new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    private Canvas FindScreenCanvas()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
            return parentCanvas;

        Canvas[] canvases =
            FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);

        foreach (Canvas foundCanvas in canvases)
        {
            if (foundCanvas != null && foundCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return foundCanvas;
        }

        foreach (Canvas foundCanvas in canvases)
        {
            if (foundCanvas != null && foundCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                return foundCanvas;
        }

        return parentCanvas;
    }

    private string Stars(int count)
    {
        return count + " Yildiz";
    }

    private string FormatBranch(TuranForceBranch branch)
    {
        return branch == TuranForceBranch.LandForces ? "Kara Kuvvetleri" : "Hava Kuvvetleri";
    }

    private string FormatKind(TuranUnitKind kind)
    {
        if (kind == TuranUnitKind.Infantry) return "Piyade";
        if (kind == TuranUnitKind.Tank) return "Tank";
        if (kind == TuranUnitKind.Artillery) return "Topcu";
        if (kind == TuranUnitKind.RocketArtillery) return "Roket Topcu";
        return "Lojistik";
    }

    private string FormatAssignment(OfficerAssignment assignment)
    {
        return assignment == OfficerAssignment.Field ? "Saha" : "Us";
    }

    private string FormatTendency(OfficerTendency tendency)
    {
        if (tendency == OfficerTendency.Attack) return "Saldiri";
        if (tendency == OfficerTendency.Defense) return "Savunma";
        if (tendency == OfficerTendency.Support) return "Destek";
        return "Ekonomi";
    }
}
















