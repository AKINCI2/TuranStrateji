using UnityEngine;
using System.Collections.Generic;

public class UnitController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 10f;
    public float arrivalTolerance = 0.05f;

    [Header("Selection")]
    public bool isSelected = false;

    [Header("State")]
    public UnitState state = UnitState.Idle;
    public UnitDeploymentState deploymentState = UnitDeploymentState.InBase;
    public bool usesWorldHexGrid = true;
    public bool debugLogs = false;

    [Header("Base")]
    public BarracksBuilding assignedBarracks;
    public TuranUnitData unitData;

    [HideInInspector]
public HexCell currentHex;

    private List<HexCell> path = new List<HexCell>();
    private int targetWaypointIndex = 0;
    private HexGridManager gridManager;
    private bool isHexAssigned = false;
    private bool isInitialized = false;
    private bool gridAssignRoutineStarted;

    private Vector3 targetPosition;
    private HexCell targetDestinationHex;
    private bool recallToBaseOnArrival;

    void OnEnable()
    {
        if (!isInitialized && ShouldUseWorldGrid())
            TryStartGridAssignment();
    }

    void Start()
    {
        RefreshVisualFromData();
        EnsureOfficerBadge();
        ApplyRuntimeStatsFromData();

        if (UnitManager.Instance != null)
        {
            UnitManager.Instance.RegisterUnit(this);
        }

        if (!ShouldUseWorldGrid())
        {
            isInitialized = true;
            return;
        }

        TryStartGridAssignment();
    }

    public void SetUnitData(TuranUnitData newUnitData, bool rebuildVisual = true)
    {
        unitData = newUnitData;
        ApplyRuntimeStatsFromData();

        if (rebuildVisual)
            RefreshVisualFromData();
    }

    void OnDestroy()
    {
        if (currentHex != null)
            currentHex.OnUnitExit();

        if (UnitManager.Instance != null)
            UnitManager.Instance.UnregisterUnit(this);
    }

    private void ApplyRuntimeStatsFromData()
    {
        if (unitData == null)
            return;

        if (unitData.marchSpeed > 0)
            moveSpeed = unitData.marchSpeed;

        Health health = GetComponent<Health>();
        if (health != null)
            health.ApplyStatsFromData();

        UnitCombatController combat = GetComponent<UnitCombatController>();
        if (combat != null)
            combat.ApplyStatsFromData();
    }

    public void RefreshVisualFromData()
    {
        UnitVisualAssembler assembler = GetComponent<UnitVisualAssembler>();
        if (assembler == null)
            assembler = gameObject.AddComponent<UnitVisualAssembler>();

        assembler.unitController = this;
        assembler.formationController = GetComponent<FormationController>();

        if (unitData == null || unitData.visualProfile == null)
        {
            SetChildrenActive(true);
            assembler.EnsureExistingChildrenVisible();
            return;
        }

        assembler.Assemble(unitData);
    }

    private void EnsureOfficerBadge()
    {
        if (GetComponent<UnitOfficerBadge>() == null)
            gameObject.AddComponent<UnitOfficerBadge>();
    }

    System.Collections.IEnumerator WaitForGridAndAssignHex()
    {
        float timeout = 2f;
        while ((gridManager.allHexCells == null || gridManager.allHexCells.Count == 0) && timeout > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }

        if (gridManager.allHexCells != null && gridManager.allHexCells.Count > 0)
        {
            AssignCurrentHex();
            isInitialized = true;
        }
        else
        {
            Debug.LogError("Grid oluşmadı!");
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        if (state == UnitState.Move)
        {
            MoveAlongPath();
        }
    }

    public void MovePath(List<HexCell> newPath)
    {
        usesWorldHexGrid = true;
        deploymentState = UnitDeploymentState.OnWorldMap;

        if (gridManager == null)
            gridManager = FindAnyObjectByType<HexGridManager>();

        if (!IsHexAssigned() && !EnsureWorldHexAssigned())
        {
            if (debugLogs)
                Debug.Log("Hareket baslatilamadi, unit hex atamasi hazir degil: " + name);
            return;
        }

        if (newPath == null || newPath.Count == 0)
        {
            if (debugLogs)
                Debug.Log("Bos path: " + name);
            return;
        }

        // Hedef hex'i kaydet
        targetDestinationHex = newPath[newPath.Count - 1];
        recallToBaseOnArrival = false;

        // Path'i temizle (ilk hex current ise kaldır)
        path = new List<HexCell>(newPath);
        if (path.Count > 0 && path[0] == currentHex)
        {
            path.RemoveAt(0);
        }

        if (path.Count == 0)
        {
            Debug.Log("Zaten hedefte!");
            state = UnitState.Idle;
            return;
        }

        targetWaypointIndex = 0;
        SetNextTarget();
        state = UnitState.Move;
        SetDeploymentState(UnitDeploymentState.OnWorldMap);

        if (assignedBarracks != null)
            assignedBarracks.MarkUnitOnMap(this);

        Debug.Log($"Hareket başladı. Hedef hex: {targetDestinationHex.axialCoord}, Toplam waypoint: {path.Count}");
    }

    public void SetDeploymentState(UnitDeploymentState newState)
    {
        deploymentState = newState;
    }

    public void PlaceOnWorld(Vector3 worldPosition)
    {
        gameObject.SetActive(true);
        SetChildrenActive(true);
        usesWorldHexGrid = true;
        deploymentState = UnitDeploymentState.OnWorldMap;
        state = UnitState.Idle;
        path.Clear();
        targetWaypointIndex = 0;
        targetDestinationHex = null;
        recallToBaseOnArrival = false;

        transform.position = worldPosition;
        transform.localScale = Vector3.one;
        RefreshVisualFromData();

        isInitialized = false;
        isHexAssigned = false;
        gridAssignRoutineStarted = false;

        if (!TryAssignCurrentHexImmediate())
            TryStartGridAssignment();
    }

    public bool EnsureWorldHexAssigned()
    {
        usesWorldHexGrid = true;
        deploymentState = UnitDeploymentState.OnWorldMap;

        if (IsHexAssigned() && currentHex != null)
            return true;

        if (gridManager == null)
            gridManager = FindAnyObjectByType<HexGridManager>();

        return TryAssignCurrentHexImmediate();
    }

    public void PlaceInBase(Transform baseParent, Vector3 localPosition)
    {
        if (currentHex != null)
            currentHex.OnUnitExit();

        currentHex = null;
        path.Clear();
        targetWaypointIndex = 0;
        targetDestinationHex = null;
        recallToBaseOnArrival = false;
        state = UnitState.Idle;
        deploymentState = UnitDeploymentState.InBase;
        usesWorldHexGrid = false;
        isHexAssigned = false;
        isInitialized = true;

        if (baseParent != null)
            transform.SetParent(baseParent);

        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.identity;
        float baseScale = GameModeManager.Instance != null
            ? GameModeManager.Instance.GetSuggestedBaseUnitScale()
            : 0.28f;
        transform.localScale = Vector3.one * baseScale;
        gameObject.SetActive(true);
        SetChildrenActive(true);
        RefreshVisualFromData();
    }

    public void MoveToBaseAndRecall()
    {
        if (deploymentState != UnitDeploymentState.OnWorldMap)
            return;

        if (!EnsureWorldHexAssigned())
        {
            if (debugLogs)
                Debug.Log("Usse cagirma baslatilamadi, birlik hex'e bagli degil: " + name);
            return;
        }

        if (gridManager == null)
            gridManager = FindAnyObjectByType<HexGridManager>();

        Pathfinding pathfinder = Pathfinding.Instance != null
            ? Pathfinding.Instance
            : FindAnyObjectByType<Pathfinding>();

        WorldBaseMarker marker = WorldBaseMarker.FindPrimary();

        if (gridManager == null || pathfinder == null || marker == null)
        {
            if (debugLogs)
                Debug.Log("Usse cagirma icin gerekli sistem bulunamadi: " + name);
            return;
        }

        HexCell baseHex = gridManager.GetClosestHex(marker.transform.position);
        List<HexCell> basePath = pathfinder.FindPath(currentHex, baseHex);

        if (basePath == null)
        {
            if (debugLogs)
                Debug.Log("Usse cagirma yolu bulunamadi: " + name);
            return;
        }

        if (basePath.Count == 0)
        {
            RecallToAssignedBarracks();
            return;
        }

        MovePath(basePath);
        recallToBaseOnArrival = true;
    }

    private void SetChildrenActive(bool active)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            SetHierarchyActive(child, active);
        }
    }

    private void SetHierarchyActive(Transform root, bool active)
    {
        if (root == null)
            return;

        root.gameObject.SetActive(active);
        for (int i = 0; i < root.childCount; i++)
            SetHierarchyActive(root.GetChild(i), active);
    }

    void SetNextTarget()
    {
        if (targetWaypointIndex >= path.Count)
        {
            state = UnitState.Idle;
            return;
        }

        HexCell targetHex = path[targetWaypointIndex];
        targetPosition = gridManager != null
            ? gridManager.GetHexCenter(targetHex)
            : targetHex.transform.position;
        targetPosition.y = 0.5f; // Set a clear height above the hexes

        Debug.Log($"Waypoint {targetWaypointIndex + 1}/{path.Count}: {targetHex.axialCoord} -> Pozisyon: {targetPosition}");
    }

    void MoveAlongPath()
    {
        if (path == null || path.Count == 0)
        {
            state = UnitState.Idle;
            return;
        }

        if (targetWaypointIndex >= path.Count)
        {
            // Hedefe varıldı
            if (targetDestinationHex != null)
            {
                Vector3 finalPos = gridManager != null
                    ? gridManager.GetHexCenter(targetDestinationHex)
                    : targetDestinationHex.transform.position;
                finalPos.y = transform.position.y;
                transform.position = finalPos;
                currentHex = targetDestinationHex;

                Debug.Log($"✅ HEDEFE VARILDI: {currentHex.axialCoord}");
                if (!TryRecallToBaseAfterArrival())
                    TryCollectResourceOnCurrentHex();
            }

            path.Clear();
            state = UnitState.Idle;
            targetDestinationHex = null;
            targetWaypointIndex = 0;
            return;
        }

        Vector3 moveDirection = targetPosition - transform.position;
        moveDirection.y = 0f;

        // Rotasyon
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRot,
                rotationSpeed * Time.deltaTime
            );
        }

        // Hareket
        Vector3 newPosition = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        transform.position = newPosition;

        // Waypoint'e ulaştı mı?
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance < arrivalTolerance)
        {
            // Bu waypoint'e tam olarak snap yap
            transform.position = targetPosition;

            if (currentHex != null)
            {
                currentHex.OnUnitExit();
            }

            // Current hex'i güncelle
            currentHex = path[targetWaypointIndex];
            currentHex.OnUnitEnter(this);

            targetWaypointIndex++;

            if (targetWaypointIndex < path.Count)
            {
                SetNextTarget();
            }
            else
            {
                // Son waypoint'e ulaştık, bir sonraki frame'de hedefe varış kontrolü yapılacak
                Debug.Log($"Son waypoint'e ulaşıldı: {currentHex.axialCoord}");
            }
        }
    }

    private bool TryRecallToBaseAfterArrival()
    {
        if (!recallToBaseOnArrival)
            return false;

        recallToBaseOnArrival = false;
        return RecallToAssignedBarracks();
    }

    private bool RecallToAssignedBarracks()
    {
        BarracksBuilding barracks = assignedBarracks;

        if (barracks == null && UnitManager.Instance != null)
        {
            UnitManager.Instance.EnsureAssignedBarracks(this);
            barracks = assignedBarracks;
        }

        if (barracks == null)
        {
            if (debugLogs)
                Debug.Log("Birlik usse dondu ama atanmis kisla bulunamadi: " + name);
            return false;
        }

        return barracks.RecallUnitToBase(this, false);
    }

    private void TryCollectResourceOnCurrentHex()
    {
        if (deploymentState != UnitDeploymentState.OnWorldMap ||
            currentHex == null ||
            WorldResourceNodeManager.Instance == null)
        {
            return;
        }

        WorldResourceNodeManager.Instance.TryCollectAtHex(currentHex, this);
    }

    void AssignCurrentHex()
    {
        if (gridManager == null)
        {
            Debug.LogError("❌ GridManager NULL!");
            return;
        }

        HexCell nearestHex = gridManager.GetClosestHex(transform.position);

        if (nearestHex != null)
        {
            if (currentHex != null && currentHex != nearestHex)
                currentHex.OnUnitExit();

            currentHex = nearestHex;
            isHexAssigned = true;

            Vector3 snappedPos = gridManager.GetHexCenter(currentHex);
            snappedPos.y = 0.5f; // Maintain height
            transform.position = snappedPos;
            currentHex.OnUnitEnter(this);

            Debug.Log($"✅ Hex atandı: {currentHex.axialCoord} -> Pozisyon: {transform.position}");
        }
        else
{
            Debug.LogError($"❌ UNIT {name} YAKININDA HEX BULUNAMADI!");
        }
    }

    public bool IsHexAssigned()
    {
        return isHexAssigned && isInitialized;
    }

    private void TryStartGridAssignment()
    {
        if (!ShouldUseWorldGrid())
        {
            isInitialized = true;
            return;
        }

        if (!isActiveAndEnabled)
            return;

        if (gridAssignRoutineStarted)
            return;

        gridManager = FindAnyObjectByType<HexGridManager>();

        if (gridManager == null)
        {
            if (debugLogs)
                Debug.Log("HexGridManager henuz bulunamadi, unit aktif olunca tekrar denenecek: " + name);
            return;
        }

        if (TryAssignCurrentHexImmediate())
            return;

        gridAssignRoutineStarted = true;
        StartCoroutine(WaitForGridAndAssignHex());
    }

    private bool TryAssignCurrentHexImmediate()
    {
        if (!ShouldUseWorldGrid())
            return false;

        if (gridManager == null)
            gridManager = FindAnyObjectByType<HexGridManager>();

        if (gridManager == null ||
            gridManager.allHexCells == null ||
            gridManager.allHexCells.Count == 0)
        {
            return false;
        }

        AssignCurrentHex();
        isInitialized = true;
        gridAssignRoutineStarted = false;
        return true;
    }

    private bool ShouldUseWorldGrid()
    {
        return usesWorldHexGrid &&
               deploymentState == UnitDeploymentState.OnWorldMap;
    }

    // Debug için
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (path != null && path.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (path[i] != null && path[i + 1] != null)
                {
                    Gizmos.DrawLine(
                        path[i].transform.position + Vector3.up * 0.5f,
                        path[i + 1].transform.position + Vector3.up * 0.5f
                    );
                }
            }

            if (targetDestinationHex != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetDestinationHex.transform.position + Vector3.up * 0.5f, 0.5f);
            }
        }
    }
}

public enum UnitState
{
    Idle,
    Move,
    Attack,
    Death
}

