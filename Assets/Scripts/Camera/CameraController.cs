using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 42f;
    public float dragSpeed = 0.16f;
    public float lerpSpeed = 12f;

    [Header("Zoom")]
    public float zoomSpeed = 15f;
    public float minY = 5.2f;
    public float maxY = 85f;

    [Header("View Bounds")]
    public float minX = 10f;
    public float maxX = 100f;
    public float minZ = 10f;
    public float maxZ = 80f;
    public float pitch = 75f;
    public float yaw = 45f;

    [Header("Base View")]
    public float baseMinY = 3.2f;
    public float baseMaxY = 24f;
    public float basePanRadius = 18f;
    public bool lockBasePanToCenter = false;
    public bool autoEnterBaseOnCloseZoom = false; // Restored
    public float baseEnterHeight = 11f; // Restored
    public float baseExitHeight = 14.5f; // Restored
    public float baseEnterRadius = 12f; // Restored

    private Vector3 targetPos;
    private Vector3 lastMousePosition;
    private Vector3 lastValidBaseCenter = Vector3.zero;
    private float suppressAutoBaseEnterUntil; // Restored

    void Start()
    {
        targetPos = transform.position;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        
        Camera cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = 38f;
    }

    void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, lerpSpeed * Time.deltaTime);
    }

    void Update()
    {
        if (GameModeManager.Instance != null && GameModeManager.Instance.IsCameraTransitionActive)
        {
            targetPos = transform.position;
            return;
        }

        bool isBase = GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameViewMode.BaseView;

        if (isBase)
            HandleBaseCamera();
        else
            HandleWorldCamera();
    }

    private void HandleWorldCamera()
    {
        if (!IsPointerOverUI())
        {
            HandleMovement();
            HandleDrag();
            HandleMouseWheel();
        }
        ClampWorld();
    }

    private void HandleBaseCamera()
    {
        if (!IsPointerOverUI())
        {
            HandleMovement();
            HandleDrag();
            HandleMouseWheelBase();
        }
        ClampBase();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h == 0 && v == 0) return;

        Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
        Vector3 right = transform.right; right.y = 0; right.Normalize();
        targetPos += (right * h + forward * v).normalized * moveSpeed * Time.deltaTime;
    }

    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            lastMousePosition = Input.mousePosition;

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
            Vector3 right = transform.right; right.y = 0; right.Normalize();
            targetPos -= (right * delta.x + forward * delta.y) * dragSpeed;
        }

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
                Vector3 right = transform.right; right.y = 0; right.Normalize();
                targetPos -= (right * delta.x + forward * delta.y) * dragSpeed;
            }
        }
    }

    private void HandleMouseWheel()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;
        targetPos += transform.forward * scroll * zoomSpeed;
    }

    private void HandleMouseWheelBase()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        if (lockBasePanToCenter)
        {
            Vector3 center = GetBaseCenter();
            Vector3 fromCenter = targetPos - center;
            float dist = Mathf.Max(0.1f, fromCenter.magnitude);
            float nextDist = Mathf.Clamp(dist - (scroll * zoomSpeed * 0.1f), baseMinY, baseMaxY);
            targetPos = center + fromCenter.normalized * nextDist;
        }
        else
        {
            targetPos += transform.forward * scroll * zoomSpeed;
        }
    }

    private void ClampWorld()
    {
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.z = Mathf.Clamp(targetPos.z, minZ, maxZ);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
    }

    private void ClampBase()
    {
        Vector3 center = GetBaseCenter();
        if (lockBasePanToCenter)
        {
            float rad = pitch * Mathf.Deg2Rad;
            float zOffset = -Mathf.Cos(rad) * targetPos.y * 1.3f;
            Vector3 goal = new Vector3(center.x, targetPos.y, center.z + zOffset);
            targetPos = Vector3.Lerp(targetPos, goal, lerpSpeed * Time.deltaTime);
        }
        else
        {
            targetPos.x = Mathf.Clamp(targetPos.x, center.x - basePanRadius, center.x + basePanRadius);
            targetPos.z = Mathf.Clamp(targetPos.z, center.z - basePanRadius, center.z + basePanRadius);
            targetPos.y = Mathf.Clamp(targetPos.y, baseMinY, baseMaxY);
        }
    }

    private Vector3 GetBaseCenter()
    {
        if (GameModeManager.Instance != null && GameModeManager.Instance.baseRoot != null)
        {
            GameObject baseRoot = GameModeManager.Instance.baseRoot;
            Renderer[] renderers = baseRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds bounds = new Bounds(baseRoot.transform.position, Vector3.one);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (hasBounds)
                return new Vector3(bounds.center.x, baseRoot.transform.position.y, bounds.center.z);

            return baseRoot.transform.position;
        }

        WorldBaseMarker marker = WorldBaseMarker.FindPrimary(true);
        if (marker != null) return marker.transform.position;
        return lastValidBaseCenter;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void SetWorldBounds(Bounds b, float pad)
    {
        float requestedInset = Mathf.Max(0f, pad + 15f);
        float maxInsetX = Mathf.Max(0f, b.size.x * 0.42f);
        float maxInsetZ = Mathf.Max(0f, b.size.z * 0.42f);
        float insetX = Mathf.Min(requestedInset, maxInsetX);
        float insetZ = Mathf.Min(requestedInset, maxInsetZ);

        minX = b.min.x + insetX;
        maxX = b.max.x - insetX;
        minZ = b.min.z + insetZ;
        maxZ = b.max.z - insetZ;

        if (minX > maxX)
        {
            float center = b.center.x;
            minX = center - 0.1f;
            maxX = center + 0.1f;
        }

        if (minZ > maxZ)
        {
            float center = b.center.z;
            minZ = center - 0.1f;
            maxZ = center + 0.1f;
        }
    }

    public void SuppressAutoEnterBase(float seconds = -1f)
    {
        suppressAutoBaseEnterUntil = Time.unscaledTime + (seconds > 0 ? seconds : 1.5f);
    }

    public Vector3 ClampWorldPosition(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        return pos;
    }
}
