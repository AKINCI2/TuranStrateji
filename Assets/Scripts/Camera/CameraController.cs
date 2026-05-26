using UnityEngine;
using UnityEngine.EventSystems;
using LegacyTouchPhase = UnityEngine.TouchPhase;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 42f;
    public float dragSpeed = 0.16f;
    public float touchPanWorldMultiplier = 0.024f;
    public float touchPanBaseMultiplier = 0.011f;

    [Header("Zoom")]
    public float zoomSpeed = 15f;
    public float minY = 5.8f;
    public float maxY = 110f;
    public bool autoEnterBaseOnCloseZoom = false;
    public float baseEnterHeight = 10.5f;
    public float baseEnterRadius = 13f;
    public float baseExitPinchThreshold = -1.5f;
    public float baseAutoEnterCooldownAfterExit = 1.25f;

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
    public float baseMinY = 2.4f;
    public float baseMaxY = 13f;
    public float basePanRadius = 6f;
    public float baseExitHeight = 11.5f;

    private Vector3 lastMousePosition;
    private float suppressAutoBaseEnterUntil;
    private bool requireBaseZoneExitBeforeAutoEnter;
    private bool hasWorldBounds;

    void Start()
    {
        ApplyWarpathStyleZoomTuning();
        if (GameModeManager.Instance == null)
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
        {
            return;
        }

        if (GameModeManager.Instance != null &&
            GameModeManager.Instance.IsCameraTransitionActive)
        {
            return;
        }

        HandleWorldCamera();
    }

    private void HandleWorldCamera()
    {
        if (!IsAnyPointerOverUI())
        {
            bool handledTouchPan = HandleTouchPan(touchPanWorldMultiplier);
            bool handledTouchZoom = HandlePinchZoom(zoomSpeed, minY, maxY, out _);

            if (!handledTouchPan)
            {
                HandleMovement();
                HandleDrag();
            }

            if (!handledTouchZoom)
            {
                HandleMouseWheelZoom(zoomSpeed, minY, maxY);
                TryAutoEnterBaseView();
            }

            if (handledTouchZoom)
                TryAutoEnterBaseView();
        }

        ClampPosition();
    }

    private void HandleBaseViewCamera()
    {
        if (GameModeManager.Instance == null ||
            GameModeManager.Instance.CurrentMode != GameViewMode.BaseView ||
            GameModeManager.Instance.IsCameraTransitionActive)
        {
            return;
        }

        if (!IsAnyPointerOverUI())
        {
            bool handledTouchPan = HandleTouchPan(touchPanBaseMultiplier);
            bool handledTouchZoom = HandlePinchZoom(baseZoomSpeed, baseMinY, baseMaxY, out float pinchDelta);

            if (!handledTouchPan)
            {
                HandleMovement(baseMoveSpeed);
                HandleDrag(baseDragSpeed);
            }

            if (!handledTouchZoom)
            {
                HandleMouseWheelZoom(baseZoomSpeed, baseMinY, baseMaxY);
                if (Input.mouseScrollDelta.y < -0.01f && transform.position.y >= baseExitHeight)
                    GameModeManager.Instance.RequestEnterWorldMap(true);
            }

            if (handledTouchZoom &&
                pinchDelta < baseExitPinchThreshold &&
                transform.position.y >= baseExitHeight)
            {
                GameModeManager.Instance.RequestEnterWorldMap(true);
            }
        }

        ClampBasePosition();
    }

    private bool HandleTouchPan(float multiplier)
    {
        if (Input.touchCount != 1)
            return false;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != LegacyTouchPhase.Moved)
            return false;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector2 delta = touch.deltaPosition;
        Vector3 worldDelta = (-right * delta.x + -forward * delta.y) * multiplier;
        transform.position += worldDelta;
        return true;
    }

    private bool HandlePinchZoom(float speed, float minHeight, float maxHeight, out float zoomDelta)
    {
        zoomDelta = 0f;
        if (!TuranTouchInput.TryGetPinchOrScrollDelta(out float rawDelta))
            return false;

        if (Input.touchCount < 2)
            return false;

        zoomDelta = rawDelta;
        Vector3 pos = transform.position + transform.forward * rawDelta * speed * 0.01f;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;
        return true;
    }

    private void HandleMouseWheelZoom(float speed, float minHeight, float maxHeight)
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        Vector3 pos = transform.position + transform.forward * scroll * speed;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;
    }

    private bool IsAnyPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    void HandleMovement()
    {
        HandleMovement(moveSpeed);
    }

    void HandleMovement(float speed)
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 dir = (right * h + forward * v).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

    void HandleDrag()
    {
        HandleDrag(dragSpeed);
    }

    void HandleDrag(float speed)
    {
        if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0))
            lastMousePosition = Input.mousePosition;

        if (!Input.GetMouseButton(2) && !Input.GetMouseButton(1) && !Input.GetMouseButton(0))
            return;

        Vector3 delta = Input.mousePosition - lastMousePosition;
        lastMousePosition = Input.mousePosition;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        transform.position -= (right * delta.x + forward * delta.y) * speed;
    }

    private void TryAutoEnterBaseView()
    {
        if (!autoEnterBaseOnCloseZoom)
            return;

        if (Time.unscaledTime < suppressAutoBaseEnterUntil)
            return;

        if (GameModeManager.Instance == null ||
            GameModeManager.Instance.CurrentMode != GameViewMode.WorldMap ||
            GameModeManager.Instance.IsCameraTransitionActive)
        {
            return;
        }

        if (requireBaseZoneExitBeforeAutoEnter)
        {
            if (IsInsideBaseAutoEnterZone())
                return;

            requireBaseZoneExitBeforeAutoEnter = false;
        }

        if (!IsInsideBaseAutoEnterZone())
            return;

        GameModeManager.Instance.RequestEnterBaseView();
    }

    private bool IsInsideBaseAutoEnterZone()
    {
        if (transform.position.y > baseEnterHeight)
        {
            return false;
        }

        WorldBaseMarker closestMarker = FindClosestBaseMarker();
        if (closestMarker == null)
            return false;

        Vector2 cameraXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 baseXZ = new Vector2(closestMarker.transform.position.x, closestMarker.transform.position.z);
        float distance = Vector2.Distance(cameraXZ, baseXZ);

        return distance <= baseEnterRadius;
    }

    public void SuppressAutoEnterBase(float seconds = -1f)
    {
        float duration = seconds > 0f ? seconds : baseAutoEnterCooldownAfterExit;
        suppressAutoBaseEnterUntil = Mathf.Max(suppressAutoBaseEnterUntil, Time.unscaledTime + duration);
        requireBaseZoneExitBeforeAutoEnter = true;
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
            if (marker == null || !marker.gameObject.activeInHierarchy || !marker.IsPlacementMarker)
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
        Vector3 pos = ClampWorldPosition(transform.position);
        transform.position = pos;
    }

    public void SetWorldBounds(Bounds bounds, float padding)
    {
        if (bounds.size.x <= 0f || bounds.size.z <= 0f)
            return;

        minX = bounds.min.x + padding;
        maxX = bounds.max.x - padding;
        minZ = bounds.min.z + padding;
        maxZ = bounds.max.z - padding;

        if (minX > maxX)
        {
            float mid = bounds.center.x;
            minX = mid;
            maxX = mid;
        }

        if (minZ > maxZ)
        {
            float mid = bounds.center.z;
            minZ = mid;
            maxZ = mid;
        }

        hasWorldBounds = true;
    }

    public Vector3 ClampWorldPosition(Vector3 position)
    {
        Vector3 pos = position;
        if (!hasWorldBounds)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            return pos;
        }

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        return pos;
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
        autoEnterBaseOnCloseZoom = false;
        moveSpeed = Mathf.Clamp(moveSpeed, 38f, 56f);
        dragSpeed = Mathf.Clamp(dragSpeed, 0.13f, 0.22f);
        zoomSpeed = Mathf.Clamp(zoomSpeed, 13f, 22f);
        minY = Mathf.Clamp(minY, 5.4f, 8f);
        maxY = Mathf.Clamp(maxY, 96f, 128f);
        baseEnterHeight = Mathf.Clamp(baseEnterHeight, 9f, 13f);
        baseEnterRadius = Mathf.Clamp(baseEnterRadius, 9f, 16f);
        baseExitPinchThreshold = Mathf.Clamp(baseExitPinchThreshold, -4f, -0.4f);
        baseMinY = Mathf.Clamp(baseMinY, 2f, 3.8f);
        baseMaxY = Mathf.Clamp(baseMaxY, 10f, 16f);
        basePanRadius = Mathf.Clamp(basePanRadius, 5f, 11f);
        baseExitHeight = Mathf.Clamp(baseExitHeight, 8.5f, baseMaxY);
    }
}
