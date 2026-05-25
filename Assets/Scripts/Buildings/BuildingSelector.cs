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

        Vector2 pointerPosition;
        if (!TuranTouchInput.TryGetPrimaryPointerPosition(out pointerPosition))
            return;

        if (IsPointerOverBlockingUI(pointerPosition))
            return;

        Vector2 tapPosition;
        if (TuranTouchInput.TryGetPrimaryTapDown(out tapPosition))
            TrySelectBuilding(tapPosition);
    }

    public void ClearSelection()
    {
        selectedBuilding = null;
        SelectionChanged?.Invoke(null);
    }

    void TrySelectBuilding(Vector2 screenPosition)
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

        BaseBuilding bestBuilding = null;
        float bestScore = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            Collider col = hit.collider;
            if (col == null)
                continue;

            if (IsBaseEnvironmentCollider(col))
                continue;

            BaseBuilding building = ResolveBuildingFromCollider(col);
            if (building == null)
                continue;

            if (!IsSelectableBaseBuilding(building))
                continue;

            float score = ScoreBuildingHit(building, hit);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestBuilding = building;
        }

        if (bestBuilding == null)
        {
            ClearSelection();
            return;
        }

        selectedBuilding = bestBuilding;
        SelectionChanged?.Invoke(selectedBuilding);
        ShowBuildingPanel(selectedBuilding);

        Debug.Log("Bina secildi: " + bestBuilding.GetDisplayName());
    }

    private BaseBuilding ResolveBuildingFromCollider(Collider col)
    {
        if (col == null)
            return null;

        BuildingClickProxy proxy = col.GetComponent<BuildingClickProxy>();
        if (proxy != null && proxy.owner != null)
            return proxy.owner;

        return col.GetComponentInParent<BaseBuilding>();
    }

    private float ScoreBuildingHit(BaseBuilding building, RaycastHit hit)
    {
        if (building == null)
            return float.PositiveInfinity;

        float distanceScore = hit.distance * 0.35f;
        float centerScore = Vector2.Distance(
            new Vector2(hit.point.x, hit.point.z),
            new Vector2(building.transform.position.x, building.transform.position.z)
        );

        float proxyBonus = hit.collider != null && hit.collider.GetComponent<BuildingClickProxy>() != null
            ? -0.65f
            : 0f;

        return distanceScore + centerScore + proxyBonus;
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

    bool IsPointerOverBlockingUI(Vector2 pointerPosition)
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
