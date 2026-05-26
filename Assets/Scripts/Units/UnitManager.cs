using UnityEngine;
using System.Collections.Generic;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance;

    [Header("Units")]
    public List<UnitController> units = new List<UnitController>();
    private List<UnitController> playerUnits = new List<UnitController>();
    private List<UnitController> enemyUnits = new List<UnitController>();
    private List<UnitController> selectedUnits = new List<UnitController>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Discover existing units if list is empty
        if (units.Count == 0)
        {
            UnitController[] sceneUnits = Object.FindObjectsByType<UnitController>(FindObjectsInactive.Include);
            foreach (var u in sceneUnits) RegisterUnit(u);
        }

        RebuildTeamLists();

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.ModeChanged += OnGameModeChanged;

        RefreshUnitVisibility();
    }

    void OnDestroy()
    {
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.ModeChanged -= OnGameModeChanged;

        if (Instance == this)
            Instance = null;
    }

    public void UnregisterUnit(UnitController unit)
    {
        if (unit == null) return;
        units.Remove(unit);
        playerUnits.Remove(unit);
        enemyUnits.Remove(unit);
        selectedUnits.Remove(unit);
    }

    public UnitController GetNearestEnemy(Vector3 position, float range, bool isEnemySearchingForPlayer)
    {
        List<UnitController> targets = isEnemySearchingForPlayer ? playerUnits : enemyUnits;
        return GetNearestFromList(position, range, targets);
    }

    public UnitController GetNearestHostile(UnitController seeker, float range)
    {
        if (seeker == null)
            return null;

        bool seekerIsEnemy = IsEnemyUnit(seeker);
        List<UnitController> targets = seekerIsEnemy ? playerUnits : enemyUnits;
        return GetNearestFromList(seeker.transform.position, range, targets, seeker);
    }

    public bool AreHostile(UnitController first, UnitController second)
    {
        if (first == null || second == null || first == second)
            return false;

        return IsEnemyUnit(first) != IsEnemyUnit(second);
    }

    public bool IsEnemy(UnitController unit)
    {
        return IsEnemyUnit(unit);
    }

    private UnitController GetNearestFromList(
        Vector3 position,
        float range,
        List<UnitController> targets,
        UnitController ignoredUnit = null)
    {
        UnitController closest = null;
        float minSqrDistance = range * range;

        foreach (var target in targets)
        {
            if (!IsCombatTargetable(target) || target == ignoredUnit)
                continue;

            float sqrDistance = (position - target.transform.position).sqrMagnitude;
            if (sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                closest = target;
            }
        }

        return closest;
    }

    // =====================================================
    // REGISTER
    // =====================================================

    public void RegisterUnit(UnitController unit)
    {
        if (unit == null)
            return;

        bool isEnemy = IsEnemyUnit(unit);

        if (isEnemy && unit.assignedBarracks == null)
        {
            unit.usesWorldHexGrid = true;
            unit.SetDeploymentState(UnitDeploymentState.OnWorldMap);
        }
        else if (IsUnderWorldUnitsRoot(unit) &&
            unit.assignedBarracks == null &&
            unit.deploymentState != UnitDeploymentState.OnWorldMap)
        {
            unit.SetDeploymentState(UnitDeploymentState.Reserve);
            unit.gameObject.SetActive(false);
            return;
        }

        if (!units.Contains(unit))
            units.Add(unit);

        RegisterUnitTeam(unit, isEnemy);

        // Setup Supply System
        if (unit.unitData != null)
        {
            if (unit.unitData.kind == TuranUnitKind.Logistics)
            {
                if (unit.GetComponent<SupplySource>() == null)
                    unit.gameObject.AddComponent<SupplySource>();
            }
            
            // All units on world map need a consumer
            if (unit.GetComponent<SupplyConsumer>() == null)
                unit.gameObject.AddComponent<SupplyConsumer>();
        }

        EnsureSelectionVisual(unit);
        SetUnitSelected(unit, unit.isSelected);

        if (!IsLogisticsUnit(unit) && !IsEnemyUnit(unit) && unit.assignedBarracks == null)
        {
            AssignUnitToFirstAvailableBarracks(unit);
        }

        RefreshUnitVisibility(unit);
    }

    // =====================================================
    // SELECT
    // =====================================================

    public void SelectUnit(UnitController unit)
    {
        if (unit == null)
            return;

        ClearHexTarget();

        if (IsUnitSelected(unit) && selectedUnits.Count == 1)
        {
            DeselectAll();
            return;
        }

        DeselectAll();

        selectedUnits.Add(unit);
        SetUnitSelected(unit, true);
    }

    // ğŸ”¥ INDEX Ä°LE SELECT
    public void SelectUnit(int index)
    {
        List<UnitController> selectableUnits = GetSelectableUnits();

        if (index < 0 || index >= selectableUnits.Count)
            return;

        SelectUnit(selectableUnits[index]);
    }

    // ğŸ”¥ TÃœMÃœNÃœ SEÃ‡
    public void SelectAll()
    {
        ClearHexTarget();

        if (AreAllUnitsSelected())
        {
            DeselectAll();
            return;
        }

        DeselectAll();

        foreach (UnitController unit in GetSelectableUnits())
        {
            if (unit == null)
                continue;

            selectedUnits.Add(unit);
            SetUnitSelected(unit, true);
        }
    }

    // =====================================================
    // DESELECT
    // =====================================================

    public void DeselectAll()
    {
        foreach (UnitController unit in selectedUnits)
        {
            if (unit != null)
            {
                SetUnitSelected(unit, false);
            }
        }

        selectedUnits.Clear();
    }

    // =====================================================
    // GET
    // =====================================================

    public List<UnitController> GetSelectedUnits()
    {
        selectedUnits.RemoveAll(
            item => item == null || !IsUnitSelectable(item)
        );

        return selectedUnits;
    }

    public List<UnitController> GetSelectableUnits()
    {
        List<UnitController> selectableUnits =
            new List<UnitController>();

        foreach (UnitController unit in units)
        {
            if (IsUnitSelectable(unit))
                selectableUnits.Add(unit);
        }

        return selectableUnits;
    }

    public bool IsUnitSelected(UnitController unit)
    {
        if (unit == null)
            return false;

        selectedUnits.RemoveAll(item => item == null);
        return selectedUnits.Contains(unit) && unit.isSelected;
    }

    private void SetUnitSelected(UnitController unit, bool selected)
    {
        if (unit == null)
            return;

        unit.isSelected = selected;

        UnitSelectionVisual visual =
            EnsureSelectionVisual(unit);

        if (visual != null)
            visual.SetSelected(selected);
    }

    private UnitSelectionVisual EnsureSelectionVisual(UnitController unit)
    {
        if (unit == null)
            return null;

        UnitSelectionVisual visual =
            unit.GetComponent<UnitSelectionVisual>();

        if (visual == null)
            visual = unit.gameObject.AddComponent<UnitSelectionVisual>();

        return visual;
    }

    private void ClearHexTarget()
    {
        if (HexSelector.Instance != null)
            HexSelector.Instance.ClearSelectedHex();
    }

    private bool AreAllUnitsSelected()
    {
        int liveUnitCount = 0;

        foreach (UnitController unit in GetSelectableUnits())
        {
            if (unit == null)
                continue;

            liveUnitCount++;

            if (!IsUnitSelected(unit))
                return false;
        }

        return liveUnitCount > 0;
    }

    public bool EnsureAssignedBarracks(UnitController unit)
    {
        if (unit == null || IsLogisticsUnit(unit) || IsEnemyUnit(unit))
            return false;

        if (unit.assignedBarracks != null &&
            unit.assignedBarracks.HasUnit(unit))
        {
            return true;
        }

        return AssignUnitToFirstAvailableBarracks(unit);
    }

    private bool AssignUnitToFirstAvailableBarracks(UnitController unit)
    {
        if (IsLogisticsUnit(unit) || IsEnemyUnit(unit))
            return false;
        BarracksBuilding[] barracks = GetBarracksSlots();

        BarracksBuilding bestBarracks = null;
        int bestCount = int.MaxValue;

        foreach (BarracksBuilding barrack in barracks)
        {
            if (barrack == null || !barrack.CanAssignUnit(unit))
                continue;

            int count = barrack.assignedUnits != null
                ? barrack.assignedUnits.Count
                : 0;

            if (count < bestCount)
            {
                bestCount = count;
                bestBarracks = barrack;
            }
        }

        return bestBarracks != null && bestBarracks.AssignUnit(unit);
    }

    public BarracksBuilding[] GetBarracksSlots()
    {
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

        return barracks;
    }

    public UnitController GetSelectableUnitForBarracks(BarracksBuilding barrack)
    {
        if (barrack == null || barrack.assignedUnits == null)
            return null;

        if (!barrack.HasTrainedSoldiers)
            return null;

        barrack.EnsureRepresentativeUnit();

        foreach (UnitController unit in barrack.assignedUnits)
        {
            if (unit != null &&
                !IsLogisticsUnit(unit) &&
                unit.deploymentState == UnitDeploymentState.OnWorldMap &&
                IsUnitSelectable(unit))
            {
                return unit;
            }
        }

        foreach (UnitController unit in barrack.assignedUnits)
        {
            if (unit != null &&
                !IsLogisticsUnit(unit) &&
                unit.deploymentState == UnitDeploymentState.InBase &&
                IsUnitSelectable(unit))
            {
                return unit;
            }
        }

        return null;
    }

    public void SelectBarracksSlot(int index)
    {
        BarracksBuilding[] barracks = GetBarracksSlots();
        if (index < 0 || index >= barracks.Length)
            return;

        BarracksBuilding barrack = barracks[index];
        if (barrack == null || !barrack.HasTrainedSoldiers)
            return;

        UnitController unit = GetSelectableUnitForBarracks(barrack);

        if (unit == null)
            unit = AttachAvailableUnitToBarracks(barrack);

        if (unit != null)
            SelectUnit(unit);
    }

    private UnitController AttachAvailableUnitToBarracks(BarracksBuilding barrack)
    {
        if (barrack == null || barrack.assignedUnitData == null || !barrack.HasTrainedSoldiers)
            return null;

        foreach (UnitController unit in units)
        {
            if (unit == null || IsLogisticsUnit(unit))
                continue;

            if (unit.deploymentState != UnitDeploymentState.InBase)
                continue;

            if (unit.assignedBarracks != null && unit.assignedBarracks != barrack)
                continue;

            unit.SetUnitData(barrack.assignedUnitData);

            if (barrack.AssignUnit(unit))
                return unit;
        }

        foreach (UnitController unit in units)
        {
            if (unit == null || IsLogisticsUnit(unit))
                continue;

            if (unit.deploymentState != UnitDeploymentState.InBase)
                continue;

            unit.SetUnitData(barrack.assignedUnitData);

            if (barrack.AssignUnit(unit))
                return unit;
        }

        return null;
    }

    private void OnGameModeChanged(GameViewMode mode)
    {
        RefreshUnitVisibility();
    }

    public void RefreshUnitVisibility()
    {
        foreach (UnitController unit in units)
            RefreshUnitVisibility(unit);
    }

    public void RefreshUnitVisibility(UnitController unit)
    {
        if (unit == null)
            return;

        NormalizeUnitGridUsage(unit);

        GameViewMode mode =
            GameModeManager.Instance != null
                ? GameModeManager.Instance.CurrentMode
                : GameViewMode.WorldMap;

        bool shouldShow =
            (mode == GameViewMode.WorldMap &&
             unit.deploymentState == UnitDeploymentState.OnWorldMap) ||
            (mode == GameViewMode.BaseView &&
             unit.deploymentState == UnitDeploymentState.InBase &&
             ShouldShowUnitInBase(unit));

        if (unit.assignedBarracks != null && !unit.assignedBarracks.HasTrainedSoldiers)
            shouldShow = false;

        unit.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            unit.RefreshVisualFromData();

            FormationController formation = unit.GetComponent<FormationController>();
            if (formation != null)
                formation.RebuildSoldiersFromChildren(true);
        }
    }

    private void NormalizeUnitGridUsage(UnitController unit)
    {
        if (unit == null)
            return;

        if (unit.deploymentState == UnitDeploymentState.InBase)
            unit.usesWorldHexGrid = false;
        else if (unit.deploymentState == UnitDeploymentState.OnWorldMap)
            unit.usesWorldHexGrid = true;
    }

    private bool IsUnderWorldUnitsRoot(UnitController unit)
    {
        GameModeManager modeManager =
            GameModeManager.Instance != null
                ? GameModeManager.Instance
                : FindAnyObjectByType<GameModeManager>();

        if (unit == null ||
            modeManager == null ||
            modeManager.worldUnitsRoot == null)
        {
            return false;
        }

        return unit.transform.IsChildOf(modeManager.worldUnitsRoot.transform);
    }

    private bool IsUnitSelectable(UnitController unit)
    {
        if (unit == null || IsLogisticsUnit(unit) || IsEnemyUnit(unit))
            return false;

        if (unit.assignedBarracks != null && !unit.assignedBarracks.HasTrainedSoldiers)
            return false;

        GameViewMode mode =
            GameModeManager.Instance != null
                ? GameModeManager.Instance.CurrentMode
                : GameViewMode.WorldMap;

        return (mode == GameViewMode.WorldMap &&
                (unit.deploymentState == UnitDeploymentState.OnWorldMap ||
                 unit.deploymentState == UnitDeploymentState.InBase)) ||
               (mode == GameViewMode.BaseView &&
                unit.deploymentState == UnitDeploymentState.InBase &&
                ShouldShowUnitInBase(unit));
    }

    public bool HasWorldMapUnits()
    {
        foreach (UnitController unit in units)
        {
            if (unit != null &&
                !IsLogisticsUnit(unit) &&
                !IsEnemyUnit(unit) &&
                unit.deploymentState == UnitDeploymentState.OnWorldMap)
            {
                return true;
            }
        }

        return false;
    }

    public void RecallAllWorldUnitsToBase()
    {
        foreach (UnitController unit in units)
        {
            if (unit == null ||
                IsLogisticsUnit(unit) ||
                IsEnemyUnit(unit) ||
                unit.deploymentState != UnitDeploymentState.OnWorldMap)
            {
                continue;
            }

            unit.MoveToBaseAndRecall();
        }

        DeselectAll();
    }


    private bool IsLogisticsUnit(UnitController unit)
    {
        return unit != null && unit.unitData != null && unit.unitData.kind == TuranUnitKind.Logistics;
    }

    private bool IsEnemyUnit(UnitController unit)
    {
        if (unit == null || !unit.CompareTag("Enemy"))
            return false;

        return unit.assignedBarracks == null ||
               unit.deploymentState == UnitDeploymentState.OnWorldMap;
    }

    private bool IsCombatTargetable(UnitController unit)
    {
        return unit != null &&
               unit.state != UnitState.Death &&
               unit.deploymentState == UnitDeploymentState.OnWorldMap &&
               unit.gameObject.activeInHierarchy;
    }

    private void RegisterUnitTeam(UnitController unit, bool isEnemy)
    {
        playerUnits.Remove(unit);
        enemyUnits.Remove(unit);

        if (isEnemy)
            enemyUnits.Add(unit);
        else
            playerUnits.Add(unit);
    }

    private void RebuildTeamLists()
    {
        playerUnits.Clear();
        enemyUnits.Clear();
        units.RemoveAll(unit => unit == null);

        foreach (UnitController unit in units)
            RegisterUnitTeam(unit, IsEnemyUnit(unit));
    }

    private bool ShouldShowUnitInBase(UnitController unit)
    {
        if (unit == null)
            return false;

        if (unit.assignedBarracks == null)
            return true;

        return unit.assignedBarracks.ShouldShowUnitInBase(unit);
    }
}










