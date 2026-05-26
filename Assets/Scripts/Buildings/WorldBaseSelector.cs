using System;
using System.Collections.Generic;
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

        Vector2 pointerPosition;
        if (!TuranTouchInput.TryGetPrimaryPointerPosition(out pointerPosition))
            return;

        if (IsPointerOverUI(pointerPosition))
            return;

        Vector2 tapPosition;
        if (TuranTouchInput.TryGetPrimaryTapDown(out tapPosition))
            TrySelectMarker(tapPosition);
    }

    public void ClearSelection()
    {
        selectedMarker = null;
        SelectionChanged?.Invoke(null);
    }

    public void SelectMarker(WorldBaseMarker marker)
    {
        if (marker == null || !marker.IsPlacementMarker)
        {
            ClearSelection();
            return;
        }

        selectedMarker = marker;
        SelectionChanged?.Invoke(selectedMarker);
    }

    private void TrySelectMarker(Vector2 screenPosition)
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, ~0, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            if (debugLogs)
                Debug.Log("WorldBaseSelector: Raycast hit yok.");

            ClearSelection();
            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            WorldBaseMarker marker = hits[i].collider != null
                ? hits[i].collider.GetComponentInParent<WorldBaseMarker>()
                : null;

            if (marker == null || !marker.IsPlacementMarker)
                continue;

            selectedMarker = marker;
            if (debugLogs)
                Debug.Log("WorldBaseSelector: Marker secildi -> " + marker.name);

            SelectionChanged?.Invoke(selectedMarker);
            return;
        }

        if (debugLogs)
            Debug.Log("WorldBaseSelector: Raycast var ama WorldBaseMarker yok.");

        ClearSelection();
    }

    private bool IsPointerOverUI(Vector2 pointerPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = pointerPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results.Count > 0;
    }
}
