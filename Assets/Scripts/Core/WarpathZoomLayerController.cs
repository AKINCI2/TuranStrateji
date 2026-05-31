using UnityEngine;

public class WarpathZoomLayerController : MonoBehaviour
{
    public static WarpathZoomLayerController Instance { get; private set; }

    [Header("References")]
    public Camera mainCamera;
    public CameraController cameraController;
    public GameModeManager gameModeManager;
    public WorldBaseMarker playerBaseMarker;

    [Header("Optional Roots")]
    public GameObject strategicWorldRoot;
    public GameObject regionalMapRoot;
    public GameObject baseApproachRoot;
    public GameObject baseInteriorRoot;

    [Header("Zoom Thresholds")]
    public float strategicMinHeight = 58f;
    public float regionalMinHeight = 18f;
    public float baseApproachMinHeight = 10.5f;
    public float baseEnterHeight = 8.8f;
    public float baseExitHeight = 13.5f;
    public float baseEnterRadius = 16f;
    public float baseExitRadius = 22f;
    public float baseReEnterCooldown = 1.2f;

    [Header("Camera Framing")]
    public bool centerOnPlayerBaseAtStart = false;
    public bool autoEnterBaseInterior = false;
    public bool allowBaseInteriorAutoExit = false;
    public Vector3 startCameraOffset = new Vector3(0f, 42f, -34f);
    public float startFieldOfView = 52f;
    public float layerSmoothSeconds = 0.24f;

    [Header("Runtime")]
    public WarpathZoomLayer currentLayer = WarpathZoomLayer.RegionalMap;

    private float nextBaseEnterTime;
    private WarpathZoomLayer lastAppliedLayer = (WarpathZoomLayer)(-1);

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
        AutoWire();
        TuneCameraController();
        centerOnPlayerBaseAtStart = false;
        autoEnterBaseInterior = false;
        allowBaseInteriorAutoExit = false;

        if (centerOnPlayerBaseAtStart)
            FramePlayerBase(false);

        ApplyLayer(GetDesiredLayer(), true);
    }

    void LateUpdate()
    {
        AutoWire();
        ApplyLayer(GetDesiredLayer(), false);
        HandleBaseInteriorAutoExit();
    }

    public static WarpathZoomLayerController EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        WarpathZoomLayerController existing = FindAnyObjectByType<WarpathZoomLayerController>();
        if (existing != null)
            return existing;

        GameObject obj = new GameObject("WarpathZoomLayerController");
        return obj.AddComponent<WarpathZoomLayerController>();
    }

    public void FramePlayerBase(bool smooth)
    {
        AutoWire();
        if (mainCamera == null || playerBaseMarker == null)
            return;

        Vector3 target = playerBaseMarker.transform.position;
        Vector3 pos = target + startCameraOffset;
        Quaternion rot = Quaternion.LookRotation(target - pos, Vector3.up);

        mainCamera.fieldOfView = startFieldOfView;

        if (!smooth)
        {
            mainCamera.transform.SetPositionAndRotation(pos, rot);
            return;
        }

        StopCoroutine(nameof(AnimateCamera));
        StartCoroutine(AnimateCamera(pos, rot));
    }

    private System.Collections.IEnumerator AnimateCamera(Vector3 position, Quaternion rotation)
    {
        if (mainCamera == null)
            yield break;

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float timer = 0f;
        float duration = Mathf.Max(0.05f, layerSmoothSeconds);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            mainCamera.transform.position = Vector3.Lerp(startPos, position, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, rotation, t);
            yield return null;
        }

        mainCamera.transform.SetPositionAndRotation(position, rotation);
    }

    private void AutoWire()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraController == null && mainCamera != null)
            cameraController = mainCamera.GetComponent<CameraController>();

        if (gameModeManager == null)
            gameModeManager = GameModeManager.Instance != null
                ? GameModeManager.Instance
                : FindAnyObjectByType<GameModeManager>();

        if (playerBaseMarker == null)
            playerBaseMarker = WorldBaseMarker.FindPrimary(true);

        if (gameModeManager != null)
        {
            if (regionalMapRoot == null)
                regionalMapRoot = gameModeManager.worldRoot;

            if (baseInteriorRoot == null)
                baseInteriorRoot = gameModeManager.baseRoot;
        }
    }

    private void TuneCameraController()
    {
        if (cameraController == null)
            return;

        cameraController.autoEnterBaseOnCloseZoom = false;
        cameraController.minY = Mathf.Min(cameraController.minY, 6.5f);
        cameraController.maxY = Mathf.Max(cameraController.maxY, 82f);
        cameraController.baseEnterHeight = baseEnterHeight;
        cameraController.baseExitHeight = allowBaseInteriorAutoExit ? baseExitHeight : 999f;
        cameraController.baseEnterRadius = baseEnterRadius;
    }

    private WarpathZoomLayer GetDesiredLayer()
    {
        if (gameModeManager != null && gameModeManager.CurrentMode == GameViewMode.BaseView)
            return WarpathZoomLayer.BaseInterior;

        if (mainCamera == null)
            return currentLayer;

        float height = mainCamera.transform.position.y;
        if (height >= strategicMinHeight)
            return WarpathZoomLayer.StrategicWorld;

        if (height >= regionalMinHeight)
            return WarpathZoomLayer.RegionalMap;

        if (height >= baseApproachMinHeight)
            return WarpathZoomLayer.BaseApproach;

        if (CanEnterBaseInterior())
            return WarpathZoomLayer.BaseInterior;

        return WarpathZoomLayer.BaseApproach;
    }

    private bool CanEnterBaseInterior()
    {
        if (!autoEnterBaseInterior)
            return false;

        if (Time.unscaledTime < nextBaseEnterTime ||
            mainCamera == null ||
            playerBaseMarker == null)
        {
            return false;
        }

        Vector2 cam = new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.z);
        Vector2 marker = new Vector2(playerBaseMarker.transform.position.x, playerBaseMarker.transform.position.z);
        return Vector2.Distance(cam, marker) <= baseEnterRadius;
    }

    private void ApplyLayer(WarpathZoomLayer layer, bool force)
    {
        currentLayer = layer;
        if (!force && layer == lastAppliedLayer)
            return;

        lastAppliedLayer = layer;

        SetRootActive(strategicWorldRoot, layer == WarpathZoomLayer.StrategicWorld);
        SetRootActive(regionalMapRoot, layer != WarpathZoomLayer.BaseInterior);
        SetRootActive(baseApproachRoot, layer == WarpathZoomLayer.BaseApproach);

        if (layer == WarpathZoomLayer.BaseInterior)
        {
            if (gameModeManager != null && gameModeManager.CurrentMode != GameViewMode.BaseView)
                gameModeManager.RequestEnterBaseView(false);
        }
        else if (gameModeManager != null && gameModeManager.CurrentMode == GameViewMode.BaseView)
        {
            nextBaseEnterTime = Time.unscaledTime + baseReEnterCooldown;
            gameModeManager.RequestEnterWorldMap(true);
        }

        WarpathZoomVisibility[] visibilityRules =
            FindObjectsByType<WarpathZoomVisibility>(FindObjectsInactive.Include);

        foreach (WarpathZoomVisibility rule in visibilityRules)
        {
            if (rule != null)
                rule.ApplyLayer(layer);
        }
    }

    private void HandleBaseInteriorAutoExit()
    {
        if (!allowBaseInteriorAutoExit)
            return;

        if (gameModeManager == null ||
            gameModeManager.CurrentMode != GameViewMode.BaseView ||
            mainCamera == null)
        {
            return;
        }

        bool highEnough = mainCamera.transform.position.y >= baseExitHeight;
        bool farEnough = false;
        if (playerBaseMarker != null)
        {
            Vector2 cam = new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.z);
            Vector2 marker = new Vector2(playerBaseMarker.transform.position.x, playerBaseMarker.transform.position.z);
            farEnough = Vector2.Distance(cam, marker) >= baseExitRadius;
        }

        if (!highEnough && !farEnough)
            return;

        nextBaseEnterTime = Time.unscaledTime + baseReEnterCooldown;
        gameModeManager.RequestEnterWorldMap(true);
    }

    private void SetRootActive(GameObject root, bool active)
    {
        if (root == null || root.activeSelf == active)
            return;

        root.SetActive(active);
    }
}
