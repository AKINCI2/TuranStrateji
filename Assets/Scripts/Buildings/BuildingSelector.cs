using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingSelector : MonoBehaviour
{
    public static BuildingSelector Instance;

    public BaseBuilding selectedBuilding;
    public event Action<BaseBuilding> SelectionChanged;

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

        if (IsPointerOverBlockingUI())
            return;

        if (Input.GetMouseButtonDown(0))
            TrySelectBuilding();
    }

    public void ClearSelection()
    {
        selectedBuilding = null;
        SelectionChanged?.Invoke(null);
    }

    void TrySelectBuilding()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
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

            BaseBuilding building = col.GetComponentInParent<BaseBuilding>();
            if (building == null)
            {
                ClearSelection();
                return;
            }

            if (!IsSelectableBaseBuilding(building))
            {
                ClearSelection();
                return;
            }

            selectedBuilding = building;
            SelectionChanged?.Invoke(selectedBuilding);
            ShowBuildingPanel(selectedBuilding);

            Debug.Log("Bina secildi: " + building.GetDisplayName());
            return;
        }

        ClearSelection();
    }

    private bool IsSelectableBaseBuilding(BaseBuilding building)
    {
        if (building == null || !building.gameObject.activeInHierarchy)
            return false;

        GameModeManager mode = GameModeManager.Instance;
        if (mode != null && mode.baseRoot != null &&
            !building.transform.IsChildOf(mode.baseRoot.transform))
        {
            return false;
        }

        return true;
    }

    private bool IsBaseEnvironmentCollider(Collider col)
    {
        if (col == null)
            return false;

        Transform current = col.transform;
        while (current != null)
        {
            string objectName = current.name;
            if (objectName.StartsWith("BaseTerrain_") ||
                objectName.StartsWith("BaseViewEnvironment_"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void ShowBuildingPanel(BaseBuilding building)
    {
        if (building == null)
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
            panel.ForceSelectBuilding(building);
    }

    bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        if (Input.touchCount > 0)
        {
            pointerData.position = Input.GetTouch(0).position;
        }
        else
        {
            pointerData.position = Input.mousePosition;
        }

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
