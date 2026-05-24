using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 34f;
    public float dragSpeed = 0.11f;

    [Header("Zoom")]
    public float zoomSpeed = 11f;
    public float minY = 14f;
    public float maxY = 86f;
    public bool autoEnterBaseOnCloseZoom = true;
    public float baseEnterHeight = 18f;
    public float baseEnterRadius = 28f;
    public float baseExitScrollThreshold = -0.15f;

    [Header("View")]
    public float pitch = 58f;
    public float yaw = 45f;
    public float minX = -12f;
    public float maxX = 118f;
    public float minZ = -12f;
    public float maxZ = 92f;

    [Header("Base View Camera")]
    public float baseMoveSpeed = 18f;
    public float baseDragSpeed = 0.07f;
    public float baseZoomSpeed = 7.5f;
    public float baseMinY = 7f;
    public float baseMaxY = 34f;
    public float basePanRadius = 34f;
    public float baseExitHeight = 30f;

    private Vector3 lastMousePosition;

    void Start()
    {
        ApplyWarpathStyleZoomTuning();
        ApplyStrategicRotation();
    }

    void Update()
    {
        if (GameModeManager.Instance != null &&
            GameModeManager.Instance.CurrentMode == GameViewMode.BaseView)
        {
            HandleBaseViewCamera();
            return;
        }

        if (GameModeManager.Instance != null &&
            GameModeManager.Instance.CurrentMode != GameViewMode.WorldMap)
            return;

        if (GameModeManager.Instance != null &&
            GameModeManager.Instance.IsCameraTransitionActive)
        {
            return;
        }

        HandleMovement();
        HandleDrag();
        HandleZoom();
        ClampPosition();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    void HandleBaseViewCamera()
    {
        if (GameModeManager.Instance == null ||
            GameModeManager.Instance.CurrentMode != GameViewMode.BaseView ||
            GameModeManager.Instance.IsCameraTransitionActive)
        {
            return;
        }

        if (!IsPointerOverUI())
        {
            HandleMovement(baseMoveSpeed);
            HandleDrag(baseDragSpeed);
            HandleBaseZoom();
            ClampBasePosition();
        }
    }

    void HandleBaseZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        Vector3 pos = transform.position + transform.forward * scroll * baseZoomSpeed;
        pos.y = Mathf.Clamp(pos.y, baseMinY, baseMaxY);
        transform.position = pos;

        if (scroll < baseExitScrollThreshold && transform.position.y >= baseExitHeight)
            GameModeManager.Instance.EnterWorldMap(true);
    }

    void HandleMovement()
    {
        HandleMovement(moveSpeed);
    }

    void HandleMovement(float speed)
    {
        if (IsPointerOverUI())
            return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 dir = (right * h + forward * v).normalized;

        transform.position +=
            dir * speed * Time.deltaTime;
    }

    void HandleDrag()
    {
        HandleDrag(dragSpeed);
    }

    void HandleDrag(float speed)
    {
        if (IsPointerOverUI())
            return;

        if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1))
            lastMousePosition = Input.mousePosition;

        if (!Input.GetMouseButton(2) && !Input.GetMouseButton(1))
            return;

        Vector3 delta = Input.mousePosition - lastMousePosition;
        lastMousePosition = Input.mousePosition;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        transform.position -=
            (right * delta.x + forward * delta.y) * speed;
    }

    void HandleZoom()
    {
        if (IsPointerOverUI())
            return;

        float scroll =
            Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        Vector3 pos =
            transform.position + transform.forward * scroll * zoomSpeed;

        pos.y =
            Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;

        TryAutoEnterBaseView();
    }

    private void TryAutoEnterBaseView()
    {
        if (!autoEnterBaseOnCloseZoom)
            return;

        if (GameModeManager.Instance == null ||
            GameModeManager.Instance.CurrentMode != GameViewMode.WorldMap ||
            GameModeManager.Instance.IsCameraTransitionActive)
        {
            return;
        }

        if (transform.position.y > baseEnterHeight)
            return;

        WorldBaseMarker closestMarker = FindClosestBaseMarker();
        if (closestMarker == null)
            return;

        Vector2 cameraXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 baseXZ = new Vector2(closestMarker.transform.position.x, closestMarker.transform.position.z);
        float distance = Vector2.Distance(cameraXZ, baseXZ);

        if (distance > baseEnterRadius)
            return;

        GameModeManager.Instance.EnterBaseView();
    }

    private WorldBaseMarker FindClosestBaseMarker()
    {
        WorldBaseMarker[] markers =
            FindObjectsByType<WorldBaseMarker>(FindObjectsInactive.Exclude);

        if (markers == null || markers.Length == 0)
            return null;

        WorldBaseMarker closest = null;
        float closestDistance = float.MaxValue;
        Vector2 cameraXZ = new Vector2(transform.position.x, transform.position.z);

        foreach (WorldBaseMarker marker in markers)
        {
            if (marker == null || !marker.gameObject.activeInHierarchy)
                continue;

            if (!marker.IsPlacementMarker)
                continue;

            Vector2 markerXZ = new Vector2(marker.transform.position.x, marker.transform.position.z);
            float distance = Vector2.Distance(cameraXZ, markerXZ);

            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closest = marker;
        }

        return closest;
    }

    void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }

    void ClampBasePosition()
    {
        Vector3 center = Vector3.zero;
        if (GameModeManager.Instance != null && GameModeManager.Instance.baseRoot != null)
            center = GameModeManager.Instance.baseRoot.transform.position;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, center.x - basePanRadius, center.x + basePanRadius);
        pos.y = Mathf.Clamp(pos.y, baseMinY, baseMaxY);
        pos.z = Mathf.Clamp(pos.z, center.z - basePanRadius, center.z + basePanRadius);
        transform.position = pos;
    }

    void ApplyStrategicRotation()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void ApplyWarpathStyleZoomTuning()
    {
        autoEnterBaseOnCloseZoom = true;
        baseEnterHeight = Mathf.Clamp(baseEnterHeight, 16f, 20f);
        baseEnterRadius = Mathf.Clamp(baseEnterRadius, 22f, 34f);
        baseExitScrollThreshold = Mathf.Min(baseExitScrollThreshold, -0.1f);
        baseMinY = Mathf.Clamp(baseMinY, 6f, 9f);
        baseMaxY = Mathf.Clamp(baseMaxY, 28f, 40f);
        baseExitHeight = Mathf.Clamp(baseExitHeight, 24f, baseMaxY);
    }
}
