using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingSlotSelector : MonoBehaviour
{
    public static BuildingSlotSelector Instance;

    public BuildingSlot selectedSlot;
    public event Action<BuildingSlot> SelectionChanged;

    private Camera cam;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (GameModeManager.Instance != null &&
            GameModeManager.Instance.CurrentMode != GameViewMode.BaseView)
        {
            return;
        }

        Vector2 pointerPosition;
        if (!TuranTouchInput.TryGetPrimaryPointerPosition(out pointerPosition))
            return;

        if (IsPointerOverBlockingUI(pointerPosition))
            return;

        Vector2 tapPosition;
        if (TuranTouchInput.TryGetPrimaryTapDown(out tapPosition))
            TrySelectSlot(tapPosition);
    }

    public void ClearSelection()
    {
        selectedSlot = null;
        SelectionChanged?.Invoke(null);
    }

    private void TrySelectSlot(Vector2 screenPosition)
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1500f, ~0, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            ClearSelection();
            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Collider col = hit.collider;
            if (col == null)
                continue;

            if (col.GetComponentInParent<BaseBuilding>() != null)
                return;

            BuildingSlot slot = col.GetComponentInParent<BuildingSlot>();
            if (slot == null)
                continue;

            if (!slot.showRuntimeVisual || !slot.gameObject.activeInHierarchy)
                continue;

            selectedSlot = slot;
            SelectionChanged?.Invoke(selectedSlot);
            ShowSlotPanel(selectedSlot);
            return;
        }

        ClearSelection();
    }

    private void ShowSlotPanel(BuildingSlot slot)
    {
        if (slot == null)
            return;

        BuildingPanelUI panel =
            BuildingPanelUI.Instance != null
                ? BuildingPanelUI.Instance
                : FindAnyObjectByType<BuildingPanelUI>(FindObjectsInactive.Include);

        if (panel == null)
        {
            GameUI gameUi = FindAnyObjectByType<GameUI>();
            if (gameUi != null)
                panel = gameUi.GetComponent<BuildingPanelUI>();

            if (panel == null && gameUi != null)
                panel = gameUi.gameObject.AddComponent<BuildingPanelUI>();
        }

        if (panel != null)
            panel.ForceSelectSlot(slot);
    }

    private bool IsPointerOverBlockingUI(Vector2 pointerPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = pointerPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == null)
                continue;

            if (result.gameObject.GetComponentInParent<Button>() != null ||
                result.gameObject.GetComponentInParent<Toggle>() != null ||
                result.gameObject.GetComponentInParent<Slider>() != null ||
                result.gameObject.GetComponentInParent<Scrollbar>() != null ||
                result.gameObject.GetComponentInParent<ScrollRect>() != null ||
                result.gameObject.GetComponentInParent<TMPro.TMP_InputField>() != null)
            {
                return true;
            }

            Transform current = result.gameObject.transform;
            while (current != null)
            {
                string objectName = current.name;
                if (objectName.Contains("MainMenuPanel") ||
                    objectName.Contains("BuildingPanel") ||
                    objectName.Contains("WorldCityPanel"))
                {
                    return true;
                }

                current = current.parent;
            }
        }

        return false;
    }
}
