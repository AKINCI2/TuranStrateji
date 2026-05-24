using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HexSelector : MonoBehaviour
{
    public static HexSelector Instance;

    public HexCell selectedHex;
    public bool debugLogs = false;

    private Camera cam;
    private HexGridManager gridManager;
    private readonly List<HexCell> revealedNeighbors = new List<HexCell>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cam = Camera.main;
        gridManager = FindAnyObjectByType<HexGridManager>();
    }

    void Update()
    {
        if (GameModeManager.Instance != null &&
            GameModeManager.Instance.CurrentMode != GameViewMode.WorldMap)
        {
            ClearSelectedHex();
            return;
        }

        // UI üstündeyken dünya raycast alma
        if (IsPointerOverUI())
        {
            return;
        }

        // Sol click → hex seç
        if (Input.GetMouseButtonDown(0))
        {
            SelectHex();
        }
    }

    void SelectHex()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            WorldCityNode cityNode =
                hit.collider.GetComponentInParent<WorldCityNode>();

            if (cityNode != null)
            {
                ClearSelectedHex();
                WorldCityPanelUI cityPanel =
                    WorldCityPanelUI.Instance != null
                        ? WorldCityPanelUI.Instance
                        : FindAnyObjectByType<WorldCityPanelUI>();

                if (cityPanel != null)
                    cityPanel.Show(cityNode);

                if (debugLogs)
                    Debug.Log("SEHIR SECILDI: " + cityNode.displayName);
                return;
            }

            WorldResourceNode resourceNode =
                hit.collider.GetComponentInParent<WorldResourceNode>();

            if (resourceNode != null && resourceNode.hex != null)
            {
                SetSelectedHex(resourceNode.hex);
                return;
            }

            if (hit.collider.GetComponentInParent<WorldBaseMarker>() != null)
                return;

            if (hit.collider.GetComponentInParent<BaseBuilding>() != null)
                return;

            HexCell hex = hit.collider.GetComponentInParent<HexCell>();

            if (hex == null && gridManager != null)
            {
                hex = gridManager.GetClosestHex(hit.point);
            }

            if (hex != null)
            {
                SetSelectedHex(hex);
            }
        }
    }

    private void SetSelectedHex(HexCell hex)
    {
        if (hex == null)
            return;

        if (hex.terrainType == HexTerrainType.Water)
        {
            ClearSelectedHex();
            return;
        }

        ClearSelectionOverlay();

        selectedHex = hex;
        selectedHex.Highlight();
        RevealSelectionRing(selectedHex);
        if (debugLogs)
            Debug.Log("HEX SECILDI: " + hex.axialCoord);
    }

    public void MoveSelectedUnit()
    {
        if (selectedHex == null)
        {
            if (debugLogs)
                Debug.Log("Selected hex null");
            return;
        }

        if (!selectedHex.IsWalkable())
        {
            if (debugLogs)
                Debug.Log("Secili hex gecilemez: " + selectedHex.terrainType);

            ClearSelectedHex();
            return;
        }

        if (UnitManager.Instance == null)
        {
            if (debugLogs)
                Debug.Log("UnitManager yok");
            return;
        }

        List<UnitController> units = UnitManager.Instance.GetSelectedUnits();

        if (units == null || units.Count == 0)
        {
            if (debugLogs)
                Debug.Log("Secili unit yok");
            return;
        }

        UnitController selectedUnit = units[0];

        if (selectedUnit == null)
        {
            if (debugLogs)
                Debug.Log("Selected unit null");
            return;
        }

        if (selectedUnit.unitData != null &&
            selectedUnit.unitData.kind == TuranUnitKind.Logistics &&
            WorldResourceNodeManager.Instance != null)
        {
            WorldResourceNode node =
                WorldResourceNodeManager.Instance.GetNodeAtHex(selectedHex);

            if (node != null && node.hasGuard)
            {
                ClearSelectedHex();
                return;
            }
        }
        if (selectedUnit.deploymentState == UnitDeploymentState.InBase)
        {
            if (UnitManager.Instance != null)
                UnitManager.Instance.EnsureAssignedBarracks(selectedUnit);

            if (selectedUnit.assignedBarracks == null)
            {
                if (debugLogs)
                    Debug.Log("Secili unit kislaya baglanamadi: " + selectedUnit.name);
                return;
            }

            if (!selectedUnit.assignedBarracks.DeployUnitToWorld(selectedUnit))
            {
                if (debugLogs)
                    Debug.Log("Secili unit haritaya cikarilamadi: " + selectedUnit.name);
                return;
            }
        }

        if (!selectedUnit.IsHexAssigned() &&
            !selectedUnit.EnsureWorldHexAssigned())
        {
            if (debugLogs)
                Debug.Log("Unit'in hex'i henuz atanmadi");
            return;
        }

        if (selectedUnit.currentHex == null)
        {
            Debug.LogError("Selected unit currentHex null!");
            return;
        }

        Pathfinding pathfinder = Pathfinding.Instance;
        if (pathfinder == null)
            pathfinder = FindAnyObjectByType<Pathfinding>();

        if (pathfinder == null)
        {
            Debug.LogError("PATHFINDING YOK!");
            return;
        }

        List<HexCell> path = pathfinder.FindPath(selectedUnit.currentHex, selectedHex);

        if (path != null && path.Count > 0)
        {
            if (debugLogs)
                Debug.Log($"PATH BULUNDU: {path.Count} adim");
            selectedUnit.MovePath(path);
        }
        else
        {
            Debug.LogError("PATH YOK!");
        }

        ClearSelectedHex();
    }

    public void ClearSelectedHex()
    {
        ClearSelectionOverlay();
        selectedHex = null;
    }

    private void RevealSelectionRing(HexCell center)
    {
        if (center == null || center.neighbors == null)
            return;

        foreach (HexCell neighbor in center.neighbors)
        {
            if (neighbor == null)
                continue;

            if (neighbor.terrainType == HexTerrainType.Water)
                continue;

            neighbor.RevealAsNeighbor();
            revealedNeighbors.Add(neighbor);
        }
    }

    private void ClearSelectionOverlay()
    {
        if (selectedHex != null)
            selectedHex.ResetColor();

        foreach (HexCell cell in revealedNeighbors)
        {
            if (cell != null)
                cell.ResetColor();
        }

        revealedNeighbors.Clear();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}


