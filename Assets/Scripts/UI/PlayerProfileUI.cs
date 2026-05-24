using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerProfileUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform root;
    private TMP_Text nameText;
    private TMP_Text powerText;
    private Image portrait;

    private float refreshTimer;

    public void SetVisible(bool visible)
    {
        if (root != null)
            root.gameObject.SetActive(visible);
    }

    void Start()
    {
        canvas = FindScreenCanvas();
        if (canvas == null)
            return;

        BuildUI();
        Refresh();
    }

    void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = 0.5f;
        Refresh();
    }

    private void BuildUI()
    {
        GameObject rootObject = new GameObject("PlayerProfileRoot", typeof(RectTransform), typeof(Image));
        rootObject.transform.SetParent(canvas.transform, false);

        root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(14f, -56f);
        root.sizeDelta = new Vector2(214f, 56f);

        Image rootImage = rootObject.GetComponent<Image>();
        rootImage.color = new Color(0.04f, 0.06f, 0.07f, 0.74f);

        GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(root, false);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(7f, 0f);
        portraitRect.sizeDelta = new Vector2(42f, 42f);
        portrait = portraitObject.GetComponent<Image>();
        portrait.color = new Color(0.12f, 0.20f, 0.24f, 0.96f);

        nameText = CreateText("Name", new Vector2(58f, -8f), new Vector2(146f, 18f), 11.5f);
        powerText = CreateText("Power", new Vector2(58f, -29f), new Vector2(146f, 20f), 12.5f);
    }

    private void Refresh()
    {
        if (root == null)
            return;

        string officerName = GetProfileOfficerName();
        int power = CalculatePlayerPower();

        if (nameText != null)
            nameText.text = officerName;

        if (powerText != null)
            powerText.text = "Guc " + FormatNumber(power);
    }

    private string GetProfileOfficerName()
    {
        if (PlayerRosterManager.Instance == null)
            return "Komutan";

        foreach (OwnedOfficerEntry entry in PlayerRosterManager.Instance.ownedOfficers)
        {
            if (entry != null && entry.unlocked && entry.officer != null)
                return entry.officer.displayName;
        }

        return "Komutan";
    }

    private int CalculatePlayerPower()
    {
        int total = 0;

        BarracksBuilding[] barracks =
            FindObjectsByType<BarracksBuilding>(FindObjectsInactive.Include);

        foreach (BarracksBuilding barrack in barracks)
        {
            if (barrack == null || barrack.assignedUnitData == null)
                continue;

            if (barrack.assignedUnitData.kind == TuranUnitKind.Logistics)
                continue;

            if (barrack.trainedSoldiers <= 0)
                continue;

            int soldierCount = barrack.trainedSoldiers;
            int unitPower = barrack.assignedUnitData.power + soldierCount * Mathf.Max(1, barrack.assignedUnitData.attack + barrack.assignedUnitData.defense);

            if (barrack.assignedOfficer != null)
            {
                int bonus = barrack.assignedOfficer.attackBonusPercent + barrack.assignedOfficer.defenseBonusPercent + barrack.assignedOfficer.healthBonusPercent;
                unitPower += Mathf.RoundToInt(unitPower * bonus / 100f);
            }

            total += unitPower;
        }

        return total;
    }

    private TMP_Text CreateText(string objectName, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;

        return text;
    }

    private string FormatNumber(int value)
    {
        if (value >= 1000000)
            return (value / 1000000f).ToString("0.#") + "M";

        if (value >= 1000)
            return (value / 1000f).ToString("0.#") + "K";

        return value.ToString();
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
}


