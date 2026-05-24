using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class WorldBaseSelector : MonoBehaviour
{
    public static WorldBaseSelector Instance;

    public bool debugLogs = false;
    public WorldBaseMarker selectedMarker;
    public event Action<WorldBaseMarker> SelectionChanged;

    private Camera cam;

    void Awake()
    {
        Instance = this;
        debugLogs = false;
    }

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (GameModeManager.Instance != null &&
            GameModeManager.Instance.CurrentMode != GameViewMode.WorldMap)
        {
            return;
        }

        if (IsPointerOverUI())
            return;

        if (Input.GetMouseButtonDown(0))
            TrySelectMarker();
    }

    public void ClearSelection()
    {
        selectedMarker = null;
        SelectionChanged?.Invoke(null);
    }

    private void TrySelectMarker()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

        if (hits == null || hits.Length == 0)
        {
            if (debugLogs)
                Debug.Log("WorldBaseSelector: Raycast hit yok.");

            ClearSelection();
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            WorldBaseMarker marker =
                hits[i].collider.GetComponentInParent<WorldBaseMarker>();

            if (marker == null)
                continue;

            if (!marker.IsPlacementMarker)
                continue;

            selectedMarker = marker;
            if (debugLogs)
                Debug.Log("WorldBaseSelector: Marker secildi -> " + marker.name);

            SelectionChanged?.Invoke(selectedMarker);
            return;
        }

        if (debugLogs)
            Debug.Log("WorldBaseSelector: Raycast var ama WorldBaseMarker yok. Ilk hit: " + hits[0].collider.name);

        ClearSelection();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }
}

