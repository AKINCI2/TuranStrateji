using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitSlotUI : MonoBehaviour
{
    public int index;
    public Image selectionBorder;
    public TMP_Text labelText;
    public TMP_Text statusText;
    public Image iconImage;

    private Button btn;
    private Image background;

    void Awake()
    {
        btn = GetComponent<Button>();
        background = GetComponent<Image>();
        EnsureTexts();

        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClick);
            btn.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        if (UnitManager.Instance == null)
            return;

        UnitManager.Instance.SelectBarracksSlot(index);

        UnitUIManager uiManager =
            FindAnyObjectByType<UnitUIManager>();

        if (uiManager != null)
            uiManager.Refresh();
    }

    public void SetBarracksSlot(BarracksBuilding barrack, bool selected)
    {
        EnsureTexts();

        bool hasBarrack = barrack != null;
        bool hasUnitData = hasBarrack && barrack.assignedUnitData != null && barrack.assignedUnitData.kind != TuranUnitKind.Logistics;
        bool hasTrainedSoldiers = hasBarrack && barrack.HasTrainedSoldiers;
        UnitController visualUnit = hasBarrack && UnitManager.Instance != null
            ? UnitManager.Instance.GetSelectableUnitForBarracks(barrack)
            : null;

        if (btn != null)
            btn.interactable = hasBarrack && hasTrainedSoldiers && (hasUnitData || visualUnit != null);

        if (background != null)
        {
            background.color = hasTrainedSoldiers && (hasUnitData || visualUnit != null)
                ? new Color(0.18f, 0.26f, 0.30f, 0.96f)
                : new Color(0.28f, 0.28f, 0.28f, 0.72f);
        }

        TuranUnitData slotData = hasUnitData
            ? barrack.assignedUnitData
            : (visualUnit != null ? visualUnit.unitData : null);

        if (iconImage != null)
        {
            Sprite icon = slotData != null
                ? (slotData.icon != null
                    ? slotData.icon
                    : (slotData.weaponData != null ? slotData.weaponData.weaponIcon : null))
                : null;

            iconImage.sprite = icon;
            bool showIcon = icon != null;
            iconImage.enabled = showIcon;
            iconImage.color = showIcon ? Color.white : Color.clear;
        }

        if (labelText != null)
        {
            if (!hasBarrack)
                labelText.text = "-";
            else if (hasUnitData)
                labelText.text = ShortName(barrack.assignedUnitData.displayName);
            else if (visualUnit != null && visualUnit.unitData != null)
                labelText.text = ShortName(visualUnit.unitData.displayName);
            else if (visualUnit != null)
                labelText.text = "Birlik";
            else
                labelText.text = "Bos";
        }

        if (statusText != null)
        {
            if (!hasBarrack)
                statusText.text = string.Empty;
            else if (barrack.isTraining)
                statusText.text = barrack.TrainingRemainingSeconds.ToString("0") + "s";
            else if (hasUnitData)
                statusText.text = barrack.trainedSoldiers + "/" + barrack.SoldierCapacity;
            else
                statusText.text = string.Empty;
        }

        SetSelected(selected);
    }

    public void SetSelected(bool state)
    {
        if (selectionBorder != null)
            selectionBorder.enabled = state;
    }

    private void EnsureTexts()
    {
        if (labelText == null)
            labelText = CreateText("Label", 8.5f, new Vector2(2f, 2f), new Vector2(-2f, -18f));

        if (statusText == null)
            statusText = CreateText("Status", 9f, new Vector2(2f, 18f), new Vector2(-2f, -2f));

        if (iconImage == null)
            iconImage = CreateIcon();
    }

    private TMP_Text CreateText(string objectName, float fontSize, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;

        return text;
    }

    private string ShortName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Tim";

        value = value.Replace(" Timi", string.Empty);
        value = value.Replace(" Bataryasi", string.Empty);

        return value.Length <= 9 ? value : value.Substring(0, 9);
    }

    private Image CreateIcon()
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(transform, false);

        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -2f);
        rect.sizeDelta = new Vector2(22f, 22f);

        Image img = iconObject.GetComponent<Image>();
        img.color = Color.clear;
        img.enabled = false;
        img.raycastTarget = false;
        return img;
    }
}





