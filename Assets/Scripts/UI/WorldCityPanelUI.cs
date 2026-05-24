using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldCityPanelUI : MonoBehaviour
{
    public static WorldCityPanelUI Instance;

    private const string PlayerAllianceId = "Turan Birligi";
    private static readonly Color PlayerAllianceColor = new Color(0.08f, 0.48f, 0.92f, 0.85f);

    private RectTransform panel;
    private TMP_Text titleText;
    private TMP_Text infoText;
    private TMP_Text progressText;
    private Image progressFill;
    private Button occupyButton;
    private Button supportButton;
    private Button closeButton;
    private WorldCityNode selectedCity;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        BuildIfNeeded();
        Hide();
    }

    void Update()
    {
        if (selectedCity != null && panel != null && panel.gameObject.activeSelf)
            Refresh();
    }

    public void Show(WorldCityNode city)
    {
        if (city == null)
            return;

        BuildIfNeeded();
        selectedCity = city;
        panel.gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        selectedCity = null;
        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    private void BuildIfNeeded()
    {
        if (panel != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject panelObject = new GameObject("WorldCityPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 104f);
        panel.sizeDelta = new Vector2(620f, 184f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.07f, 0.10f, 0.11f, 0.88f);

        titleText = CreateText("Title", panel, new Vector2(18f, -14f), new Vector2(330f, 30f), 22, TextAlignmentOptions.Left);
        infoText = CreateText("Info", panel, new Vector2(18f, -46f), new Vector2(390f, 90f), 14, TextAlignmentOptions.TopLeft);
        progressText = CreateText("ProgressText", panel, new Vector2(18f, -142f), new Vector2(310f, 22f), 14, TextAlignmentOptions.Left);

        GameObject progressBackObject = new GameObject("ProgressBack", typeof(RectTransform), typeof(Image));
        progressBackObject.transform.SetParent(panel, false);
        RectTransform progressBack = progressBackObject.GetComponent<RectTransform>();
        progressBack.anchorMin = new Vector2(0f, 1f);
        progressBack.anchorMax = new Vector2(0f, 1f);
        progressBack.pivot = new Vector2(0f, 1f);
        progressBack.anchoredPosition = new Vector2(18f, -164f);
        progressBack.sizeDelta = new Vector2(390f, 8f);
        progressBackObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.42f);

        GameObject progressFillObject = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        progressFillObject.transform.SetParent(progressBack, false);
        RectTransform fillRect = progressFillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 0f);
        progressFill = progressFillObject.GetComponent<Image>();
        progressFill.color = new Color(0.90f, 0.70f, 0.22f, 0.95f);

        occupyButton = CreateButton("OccupyButton", "Isgal Et", panel, new Vector2(446f, -30f), new Vector2(142f, 36f), OnOccupyPressed);
        supportButton = CreateButton("SupportButton", "Destek", panel, new Vector2(446f, -78f), new Vector2(142f, 36f), OnSupportPressed);
        closeButton = CreateButton("CloseButton", "Kapat", panel, new Vector2(446f, -126f), new Vector2(142f, 30f), Hide);
    }

    private void Refresh()
    {
        if (selectedCity == null)
            return;

        titleText.text = $"{selectedCity.displayName} Lv.{selectedCity.level}";
        infoText.text =
            $"Durum: {selectedCity.GetOccupationStateDisplayName()}\n" +
            $"Sahip: {selectedCity.GetOwnerDisplayName()}\n" +
            $"Bolge: {selectedCity.regionName} | Etki: {selectedCity.influenceRadius} hex\n" +
            $"Sart: {selectedCity.GetRequirementDisplayText()}\n" +
            $"Bonus: {selectedCity.GetBonusDisplayText()}\n" +
            $"Destek: {selectedCity.stationedMemberCount} birlik";

        float percent = selectedCity.GetOccupationPercent();
        progressText.text =
            selectedCity.occupationState == AllianceOccupationState.Occupying
                ? $"Isgal ilerlemesi %{percent * 100f:0} | Kalan {FormatTime(selectedCity.GetRemainingOccupationSeconds())}"
                : $"Isgal ilerlemesi %{percent * 100f:0}";

        if (progressFill != null)
        {
            RectTransform fillRect = progressFill.rectTransform;
            fillRect.anchorMax = new Vector2(percent, 1f);
            fillRect.sizeDelta = Vector2.zero;
        }

        occupyButton.interactable =
            selectedCity.occupationState != AllianceOccupationState.Occupied ||
            selectedCity.controllingAllianceId != PlayerAllianceId;
    }

    private void OnOccupyPressed()
    {
        if (selectedCity == null)
            return;

        selectedCity.SetStationedMemberCount(Mathf.Max(1, selectedCity.stationedMemberCount));
        selectedCity.StartOccupation(PlayerAllianceId, PlayerAllianceColor);
        Refresh();
    }

    private void OnSupportPressed()
    {
        if (selectedCity == null)
            return;

        selectedCity.SetStationedMemberCount(selectedCity.stationedMemberCount + 1);
        Refresh();
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private Button CreateButton(
        string objectName,
        string label,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.38f, 0.52f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateText("Label", buttonObject.transform, new Vector2(0f, 0f), size, 15, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        text.text = label;
        return button;
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
