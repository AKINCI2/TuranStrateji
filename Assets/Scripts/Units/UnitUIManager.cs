using UnityEngine;
using System.Collections.Generic;

public class UnitUIManager : MonoBehaviour
{
    public List<UnitSlotUI> slots;
    public UnityEngine.UI.Button selectAllButton;

    private float refreshTimer;

    void Start()
    {
        ApplyWarpathLayout();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
                slots[i].index = i;
        }

        if (selectAllButton != null)
        {
            selectAllButton.onClick.RemoveAllListeners();
            selectAllButton.onClick.AddListener(() =>
            {
                if (UnitManager.Instance == null)
                    return;

                UnitManager.Instance.SelectAll();
                Refresh();
            });
        }

        Refresh();
    }

    private void ApplyWarpathLayout()
    {
        if (slots == null)
            return;

        float slotSize = 40f;
        float spacing = 6f;
        float startX = -246f;
        float y = 54f;

        for (int i = 0; i < slots.Count; i++)
        {
            UnitSlotUI slot = slots[i];
            if (slot == null)
                continue;

            RectTransform rect = slot.GetComponent<RectTransform>();
            if (rect == null)
                continue;

            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(startX + i * (slotSize + spacing), y);
            rect.sizeDelta = new Vector2(slotSize, slotSize);
        }

        if (selectAllButton != null)
        {
            RectTransform allRect = selectAllButton.GetComponent<RectTransform>();
            if (allRect != null)
            {
                allRect.anchorMin = new Vector2(1f, 0f);
                allRect.anchorMax = new Vector2(1f, 0f);
                allRect.pivot = new Vector2(0f, 0f);
                allRect.anchoredPosition = new Vector2(-62f, y);
                allRect.sizeDelta = new Vector2(58f, slotSize);
            }
        }
    }

    void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = 0.25f;
        Refresh();
    }

    public void Refresh()
    {
        bool showWorldSlots =
            GameModeManager.Instance == null ||
            GameModeManager.Instance.CurrentMode == GameViewMode.WorldMap;

        SetWorldSlotControlsVisible(showWorldSlots);
        if (!showWorldSlots)
            return;

        BarracksBuilding[] barracks = UnitManager.Instance != null
            ? UnitManager.Instance.GetBarracksSlots()
            : null;

        for (int i = 0; i < slots.Count; i++)
        {
            UnitSlotUI slot = slots[i];
            if (slot == null)
                continue;

            slot.index = i;

            BarracksBuilding barrack =
                barracks != null && i < barracks.Length
                    ? barracks[i]
                    : null;

            if (barrack != null)
                barrack.RefreshTrainingProgress();

            UnitController slotUnit =
                UnitManager.Instance != null
                    ? UnitManager.Instance.GetSelectableUnitForBarracks(barrack)
                    : null;

            bool selected =
                slotUnit != null &&
                UnitManager.Instance != null &&
                UnitManager.Instance.IsUnitSelected(slotUnit);

            slot.SetBarracksSlot(barrack, selected);
        }
    }

    private void SetWorldSlotControlsVisible(bool visible)
    {
        if (slots != null)
        {
            foreach (UnitSlotUI slot in slots)
            {
                if (slot != null && slot.gameObject.activeSelf != visible)
                    slot.gameObject.SetActive(visible);
            }
        }

        if (selectAllButton != null && selectAllButton.gameObject.activeSelf != visible)
            selectAllButton.gameObject.SetActive(visible);
    }
}


