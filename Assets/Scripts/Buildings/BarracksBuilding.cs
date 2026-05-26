using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BaseBuilding))]
public class BarracksBuilding : MonoBehaviour
{
    [Header("Capacity")]
    public int baseCapacity = 9;
    public int capacityPerLevel = 3;

    [Header("Units")]
    public List<UnitController> assignedUnits = new List<UnitController>();
    public bool showSingleBaseRepresentative = true;

    [Header("Army Slot")]
    public TuranUnitData assignedUnitData;
    public OfficerData assignedOfficer;
    public int trainedSoldiers;
    public bool isTraining;
    public float trainingRemainingSeconds;
    public int trainingSoldierCount;

    [Header("Base Spawn View")]
    public Transform baseUnitStagingAnchor;
    public Vector3 baseUnitStagingOffset = new Vector3(0f, 0.08f, 0.08f);
    public float baseUnitCellSpacing = 0.18f;

    private BaseBuilding baseBuilding;
    private float trainingEndTime;

    public int Capacity
    {
        get
        {
            int level = baseBuilding != null ? baseBuilding.currentLevel : 1;
            return baseCapacity + Mathf.Max(0, level - 1) * capacityPerLevel;
        }
    }

    public int UnitsInBaseCount
    {
        get
        {
            if (!HasTrainedSoldiers)
                return 0;

            int count = 0;
            foreach (UnitController unit in assignedUnits)
            {
                if (unit != null && unit.deploymentState == UnitDeploymentState.InBase)
                    count++;
            }

            return count;
        }
    }

    public int UnitsOnMapCount
    {
        get
        {
            if (!HasTrainedSoldiers)
                return 0;

            int count = 0;
            foreach (UnitController unit in assignedUnits)
            {
                if (unit != null && unit.deploymentState == UnitDeploymentState.OnWorldMap)
                    count++;
            }

            return count;
        }
    }

    public int SoldierCapacity
    {
        get
        {
            int level = baseBuilding != null ? baseBuilding.currentLevel : 1;
            return 9 + Mathf.Max(0, level - 1) * 3;
        }
    }

    public bool HasTrainedSoldiers
    {
        get { return assignedUnitData != null && trainedSoldiers > 0; }
    }

    public float TrainingRemainingSeconds
    {
        get
        {
            if (!isTraining)
                return 0f;

            if (trainingEndTime <= 0f)
                return Mathf.Max(0f, trainingRemainingSeconds);

            return Mathf.Max(0f, trainingEndTime - Time.time);
        }
    }

    void Awake()
    {
        baseBuilding = GetComponent<BaseBuilding>();
        NormalizeStagingSettings();
        CleanupAssignedUnits();
        EnsureStagingAnchor();
    }

    void Update()
    {
        RefreshTrainingProgress();
    }

    public bool CanAssignUnit(UnitController unit)
    {
        if (unit == null)
            return false;

        if (IsLogisticsUnit(unit.unitData))
            return false;

        CleanupAssignedUnits();

        if (assignedUnits.Contains(unit))
            return true;

        return assignedUnits.Count < Capacity;
    }

    public bool AssignUnit(UnitController unit)
    {
        if (!CanAssignUnit(unit))
            return false;

        if (!assignedUnits.Contains(unit))
            assignedUnits.Add(unit);

        unit.assignedBarracks = this;
        unit.usesWorldHexGrid = false;
        unit.SetDeploymentState(UnitDeploymentState.InBase);

        if (assignedUnitData != null)
            unit.SetUnitData(assignedUnitData);

        if (UnitManager.Instance != null)
            UnitManager.Instance.RefreshUnitVisibility(unit);

        return true;
    }

    public bool HasUnit(UnitController unit)
    {
        CleanupAssignedUnits();
        return unit != null && assignedUnits.Contains(unit);
    }

    public void MarkUnitOnMap(UnitController unit)
    {
        if (unit == null || IsLogisticsUnit(unit.unitData))
            return;

        if (!assignedUnits.Contains(unit))
            assignedUnits.Add(unit);

        unit.assignedBarracks = this;
        unit.usesWorldHexGrid = true;
        unit.SetDeploymentState(UnitDeploymentState.OnWorldMap);
    }

    public void MarkUnitInBase(UnitController unit)
    {
        if (unit == null || IsLogisticsUnit(unit.unitData))
            return;

        if (!assignedUnits.Contains(unit))
            assignedUnits.Add(unit);

        unit.assignedBarracks = this;
        unit.usesWorldHexGrid = false;
        unit.SetDeploymentState(UnitDeploymentState.InBase);
    }

    public string GetStatusText()
    {
        CleanupAssignedUnits();
        return $"Kapasite: {assignedUnits.Count}/{Capacity}\nUste: {UnitsInBaseCount}  Haritada: {UnitsOnMapCount}\n{GetArmySlotText()}";
    }

    public void SetAssignedUnitData(TuranUnitData unitData)
    {
        if (IsLogisticsUnit(unitData))
            return;

        assignedUnitData = unitData;
        trainedSoldiers = Mathf.Clamp(trainedSoldiers, 0, SoldierCapacity);

        foreach (UnitController unit in assignedUnits)
        {
            if (unit != null)
                unit.SetUnitData(assignedUnitData);
        }
    }

    public void SetAssignedOfficer(OfficerData officer)
    {
        assignedOfficer = officer;
    }

    public string GetArmySlotText()
    {
        string unitName = assignedUnitData != null ? assignedUnitData.displayName : "Tim yok";
        string officerName = assignedOfficer != null ? assignedOfficer.displayName : "Subay yok";
        return $"Ordu: {unitName}\nSubay: {officerName}\nAsker: {trainedSoldiers}/{SoldierCapacity}";
    }

    public bool CanTrainSoldiers(int count)
    {
        RefreshTrainingProgress();

        if (assignedUnitData == null || count <= 0 || isTraining)
            return false;

        if (IsLogisticsUnit(assignedUnitData))
            return false;

        if (trainedSoldiers + count > SoldierCapacity)
            return false;

        if (BaseManager.Instance == null)
            return false;

        return BaseManager.Instance.CanAfford(assignedUnitData.GetTrainingCost(count));
    }

    public bool StartTrainingSoldiers(int count)
    {
        if (!CanTrainSoldiers(count))
            return false;

        ResourceCost cost = assignedUnitData.GetTrainingCost(count);
        if (!BaseManager.Instance.TrySpend(cost))
            return false;

        isTraining = true;
        trainingSoldierCount = count;
        trainingRemainingSeconds = Mathf.Max(1f, assignedUnitData.secondsPerSoldier);
        trainingEndTime = Time.time + trainingRemainingSeconds;

        Debug.Log($"{assignedUnitData.displayName} egitimi basladi: {count} asker");
        return true;
    }

    public void RefreshTrainingProgress()
    {
        if (!isTraining)
            return;

        trainingRemainingSeconds = TrainingRemainingSeconds;
        if (trainingRemainingSeconds > 0f)
            return;

        int missing = Mathf.Max(0, SoldierCapacity - trainedSoldiers);
        if (missing > 0 && trainingSoldierCount > 0)
        {
            trainedSoldiers += 1;
            trainingSoldierCount -= 1;
            EnsureRepresentativeUnit();
            UpdateRepresentativeSoldiers();
            if (UnitManager.Instance != null)
                UnitManager.Instance.RefreshUnitVisibility();
        }

        if (trainingSoldierCount > 0 && trainedSoldiers < SoldierCapacity)
        {
            trainingRemainingSeconds = Mathf.Max(1f, assignedUnitData.secondsPerSoldier);
            trainingEndTime = Time.time + trainingRemainingSeconds;
            return;
        }

        isTraining = false;
        trainingRemainingSeconds = 0f;
        trainingEndTime = 0f;
        trainingSoldierCount = 0;
        EnsureRepresentativeUnit();
        UpdateRepresentativeSoldiers();
        if (UnitManager.Instance != null)
            UnitManager.Instance.RefreshUnitVisibility();

        Debug.Log("Kisla egitimi tamamlandi: " + GetArmySlotText());
    }

    public bool DeployFirstUnitToWorld()
    {
        if (!HasTrainedSoldiers)
            return false;

        EnsureRepresentativeUnit();
        UnitController unit = GetFirstUnit(UnitDeploymentState.InBase);
        if (unit == null)
            return false;

        return DeployUnitToWorld(unit);
    }

    public bool DeployUnitToWorld(UnitController unit)
    {
        if (!HasTrainedSoldiers)
            return false;

        if (unit == null || unit.deploymentState != UnitDeploymentState.InBase)
            return false;

        if (IsLogisticsUnit(unit.unitData) || IsLogisticsUnit(assignedUnitData))
            return false;

        if (!assignedUnits.Contains(unit))
            assignedUnits.Add(unit);

        unit.assignedBarracks = this;

        GameModeManager modeManager = GameModeManager.Instance;
        if (modeManager != null)
            modeManager.RequestEnterWorldMap(true);

        Transform worldParent = modeManager != null ? modeManager.worldUnitsRoot?.transform : null;
        if (worldParent != null)
            unit.transform.SetParent(worldParent);

        unit.SetUnitData(assignedUnitData);
        Vector3 spawnPosition = GetWorldSpawnPosition();
        unit.PlaceOnWorld(spawnPosition);
        MarkUnitOnMap(unit);
        unit.RefreshVisualFromData();
        ApplyVisibleSoldierCount(unit);

        Debug.Log("Kisladan haritaya birlik cikarildi: " + unit.name);

        if (UnitManager.Instance != null)
            UnitManager.Instance.RefreshUnitVisibility(unit);

        return true;
    }

    public bool RecallFirstUnitToBase()
    {
        UnitController unit = GetFirstUnit(UnitDeploymentState.OnWorldMap);
        if (unit == null)
            return false;

        return RecallUnitToBase(unit, true);
    }

    public bool RecallUnitToBase(UnitController unit, bool enterBaseView)
    {
        if (unit == null || unit.deploymentState != UnitDeploymentState.OnWorldMap || !assignedUnits.Contains(unit))
            return false;

        EnsureStagingAnchor();

        Transform baseParent = baseUnitStagingAnchor != null
            ? baseUnitStagingAnchor
            : transform;

        unit.PlaceInBase(baseParent, GetBaseUnitLocalPosition());
        MarkUnitInBase(unit);
        unit.RefreshVisualFromData();
        ApplyVisibleSoldierCount(unit);

        if (enterBaseView && GameModeManager.Instance != null)
            GameModeManager.Instance.RequestEnterBaseView(true);

        if (UnitManager.Instance != null)
            UnitManager.Instance.RefreshUnitVisibility(unit);

        return true;
    }

    public UnitController GetFirstUnit(UnitDeploymentState state)
    {
        CleanupAssignedUnits();

        foreach (UnitController unit in assignedUnits)
        {
            if (unit != null && unit.deploymentState == state && HasDeployableVisual(unit))
                return unit;
        }

        return null;
    }

    public bool ShouldShowUnitInBase(UnitController unit)
    {
        if (!HasTrainedSoldiers || unit == null || unit.deploymentState != UnitDeploymentState.InBase)
            return false;

        if (!showSingleBaseRepresentative)
            return true;

        CleanupAssignedUnits();

        foreach (UnitController assignedUnit in assignedUnits)
        {
            if (assignedUnit != null && assignedUnit.deploymentState == UnitDeploymentState.InBase && HasDeployableVisual(assignedUnit))
                return assignedUnit == unit;
        }

        return true;
    }

    void CleanupAssignedUnits()
    {
        assignedUnits.RemoveAll(unit => unit == null || IsLogisticsUnit(unit.unitData));
    }

    public UnitController EnsureRepresentativeUnit()
    {
        CleanupAssignedUnits();

        if (!HasTrainedSoldiers)
            return null;

        UnitController existing = GetFirstUnit(UnitDeploymentState.InBase);
        if (existing != null)
        {
            EnsureStagingAnchor();
            Transform existingBaseParent = baseUnitStagingAnchor != null
                ? baseUnitStagingAnchor
                : transform;
            existing.PlaceInBase(existingBaseParent, GetBaseUnitLocalPosition());
            existing.RefreshVisualFromData();
            UpdateRepresentativeSoldiers();
            return existing;
        }

        existing = GetFirstUnit(UnitDeploymentState.OnWorldMap);
        if (existing != null)
            return existing;

        GameObject prefab = assignedUnitData != null ? assignedUnitData.worldPrefab : null;
        GameObject unitObject = null;

        if (prefab != null)
        {
            unitObject = Instantiate(prefab);
        }
        else
        {
            UnitController template = FindVisualTemplate();
            if (template != null)
                unitObject = Instantiate(template.gameObject);
        }

        if (unitObject == null)
            return null;

        unitObject.name = assignedUnitData.displayName + "_Birlik";
        UnitController unit = unitObject.GetComponent<UnitController>();
        if (unit == null)
            unit = unitObject.AddComponent<UnitController>();

        unit.SetUnitData(assignedUnitData);
        unit.assignedBarracks = this;

        EnsureStagingAnchor();

        Transform baseParent = baseUnitStagingAnchor != null
            ? baseUnitStagingAnchor
            : transform;

        unit.PlaceInBase(baseParent, GetBaseUnitLocalPosition());
        unit.RefreshVisualFromData();

        if (!assignedUnits.Contains(unit))
            assignedUnits.Add(unit);

        if (UnitManager.Instance != null)
            UnitManager.Instance.RegisterUnit(unit);

        UpdateRepresentativeSoldiers();
        return unit;
    }

    private void UpdateRepresentativeSoldiers()
    {
        foreach (UnitController unit in assignedUnits)
        {
            if (unit == null || unit.deploymentState != UnitDeploymentState.InBase)
                continue;

            ApplyVisibleSoldierCount(unit);
        }
    }

    private void ApplyVisibleSoldierCount(UnitController unit)
    {
        if (unit == null)
            return;

        FormationController formation = unit.GetComponent<FormationController>();
        if (formation == null)
            return;

        formation.RebuildSoldiersFromChildren(true);

        int visible = assignedUnitData != null &&
            (assignedUnitData.kind == TuranUnitKind.Tank ||
             assignedUnitData.kind == TuranUnitKind.Artillery ||
             assignedUnitData.kind == TuranUnitKind.RocketArtillery)
            ? Mathf.Min(1, trainedSoldiers)
            : trainedSoldiers;

        formation.SetVisibleSoldierCount(visible);
    }

    private UnitController FindVisualTemplate()
    {
        if (UnitManager.Instance != null && UnitManager.Instance.units != null)
        {
            foreach (UnitController unit in UnitManager.Instance.units)
            {
                if (CanUseAsVisualTemplate(unit))
                    return unit;
            }
        }

        UnitController[] controllers =
            FindObjectsByType<UnitController>(FindObjectsInactive.Include);

        foreach (UnitController unit in controllers)
        {
            if (!CanUseAsVisualTemplate(unit))
                continue;

            FormationController formation = unit.GetComponent<FormationController>();
            if (formation != null && formation.soldiers != null && formation.soldiers.Count > 0)
                return unit;
        }

        foreach (UnitController unit in controllers)
        {
            if (CanUseAsVisualTemplate(unit))
                return unit;
        }

        return null;
    }

    private bool CanUseAsVisualTemplate(UnitController unit)
    {
        if (unit == null || IsLogisticsUnit(unit.unitData))
            return false;

        if (unit.assignedBarracks == this)
            return false;

        if (unit.GetComponentInParent<WorldBaseMarker>() != null)
            return false;

        if (unit.GetComponentInParent<HexCell>() != null)
            return false;

        return HasDeployableVisual(unit);
    }

    private Vector3 GetWorldSpawnPosition()
    {
        WorldBaseMarker marker = WorldBaseMarker.FindPrimary();
        Vector3 origin = marker != null ? marker.transform.position : transform.position;
        return origin + new Vector3(2f, 0f, 2f);
    }

    private Vector3 GetBaseUnitLocalPosition()
    {
        int index = showSingleBaseRepresentative ? 0 : Mathf.Max(0, UnitsInBaseCount);
        float x = (index % 3 - 1) * baseUnitCellSpacing;
        float z = baseUnitStagingOffset.z + (index / 3) * baseUnitCellSpacing;
        return new Vector3(x + baseUnitStagingOffset.x, baseUnitStagingOffset.y, z);
    }

    private void NormalizeStagingSettings()
    {
        baseUnitStagingOffset = new Vector3(0f, 0.08f, 0.08f);
        baseUnitCellSpacing = 0.17f;
    }

    private bool HasDeployableVisual(UnitController unit)
    {
        if (unit == null || IsLogisticsUnit(unit.unitData))
            return false;

        FormationController formation = unit.GetComponent<FormationController>();
        if (formation != null && formation.soldiers != null && formation.soldiers.Count > 0)
            return true;

        SkinnedMeshRenderer skinned = unit.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skinned != null)
            return true;

        MeshRenderer[] renderers = unit.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (renderer.GetComponentInParent<WorldBaseMarker>() != null)
                continue;

            if (renderer.GetComponentInParent<HexCell>() != null)
                continue;

            if (renderer.GetComponentInParent<Canvas>() != null ||
                renderer.GetComponentInParent<RectTransform>() != null)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsLogisticsUnit(TuranUnitData unitData)
    {
        return unitData != null && unitData.kind == TuranUnitKind.Logistics;
    }

    private void EnsureStagingAnchor()
    {
        if (baseUnitStagingAnchor != null)
            return;

        Transform root = baseBuilding != null && baseBuilding.spawnPointsRoot != null
            ? baseBuilding.spawnPointsRoot
            : transform;

        Transform existing = root.Find("BarracksUnitStaging");
        if (existing != null)
        {
            baseUnitStagingAnchor = existing;
            NormalizeStagingAnchor();
            return;
        }

        GameObject anchor = new GameObject("BarracksUnitStaging");
        anchor.transform.SetParent(root, false);
        anchor.transform.localPosition = Vector3.zero;
        anchor.transform.localRotation = Quaternion.identity;
        anchor.transform.localScale = Vector3.one;
        baseUnitStagingAnchor = anchor.transform;
        NormalizeStagingAnchor();
    }

    private void NormalizeStagingAnchor()
    {
        if (baseUnitStagingAnchor == null)
            return;

        baseUnitStagingAnchor.localPosition = Vector3.zero;
        baseUnitStagingAnchor.localRotation = Quaternion.identity;
        baseUnitStagingAnchor.localScale = Vector3.one;
    }
}

