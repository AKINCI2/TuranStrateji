using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TuranMapLayerManager : MonoBehaviour
{
    public static TuranMapLayerManager Instance { get; private set; }

    [Header("Roots")]
    public GameObject globalMapRoot;
    public GameObject cityMapRoot;
    public GameObject baseRoot;
    public Transform cityMapRuntimeRoot;
    public bool hideGlobalMapWhenCityIsOpen = true;

    [Header("Camera")]
    public Camera mainCamera;
    public Vector3 globalCameraOffset = new Vector3(0f, 90f, -64f);
    public Vector3 cityCameraOffset = new Vector3(0f, 48f, -38f);
    public float globalFieldOfView = 48f;
    public float cityFieldOfView = 52f;
    public float cameraSnapSeconds = 0.42f;

    [Header("Fade")]
    public bool useFade = true;
    public CanvasGroup fadeCanvasGroup;
    public float fadeSeconds = 0.18f;

    public TuranMapLayer CurrentLayer { get; private set; } = TuranMapLayer.CityMap;
    public WorldCityNode ActiveCity { get; private set; }
    public bool IsTransitioning => isTransitioning;

    private bool isTransitioning;
    private GameObject activeCityMapInstance;

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
        AutoWireFromScene();
        EnsureFadeCanvas();

        if (globalMapRoot != null)
            SetLayerImmediate(TuranMapLayer.GlobalMap);
        else
            SetLayerImmediate(TuranMapLayer.CityMap);
    }

    public static TuranMapLayerManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        TuranMapLayerManager existing = FindAnyObjectByType<TuranMapLayerManager>();
        if (existing != null)
            return existing;

        GameObject obj = new GameObject("TuranMapLayerManager");
        return obj.AddComponent<TuranMapLayerManager>();
    }

    public void EnterGlobalMap()
    {
        if (isTransitioning)
            return;

        StartCoroutine(EnterGlobalMapRoutine());
    }

    public void EnterCityMap(WorldCityNode city)
    {
        if (city == null || isTransitioning)
            return;

        StartCoroutine(EnterCityMapRoutine(city));
    }

    public void EnterBaseView()
    {
        if (isTransitioning)
            return;

        StartCoroutine(EnterBaseViewRoutine());
    }

    private IEnumerator EnterGlobalMapRoutine()
    {
        isTransitioning = true;
        yield return FadeTo(1f);

        ActiveCity = null;
        ClearCityMapInstance();
        SetLayerImmediate(TuranMapLayer.GlobalMap);
        FrameGlobalMap();

        yield return FadeTo(0f);
        isTransitioning = false;
    }

    private IEnumerator EnterCityMapRoutine(WorldCityNode city)
    {
        isTransitioning = true;
        yield return FadeTo(1f);

        ActiveCity = city;

        if (city.useAsyncSceneLoading && !string.IsNullOrWhiteSpace(city.cityMapSceneName) &&
            Application.CanStreamedLevelBeLoaded(city.cityMapSceneName))
        {
            Scene scene = SceneManager.GetSceneByName(city.cityMapSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                yield return SceneManager.LoadSceneAsync(city.cityMapSceneName, LoadSceneMode.Additive);
        }

        BuildCityMapInstance(city);
        SetLayerImmediate(TuranMapLayer.CityMap);
        FrameCityMap(city);

        yield return FadeTo(0f);
        isTransitioning = false;
    }

    private IEnumerator EnterBaseViewRoutine()
    {
        isTransitioning = true;
        yield return FadeTo(1f);

        SetLayerImmediate(TuranMapLayer.BaseView);
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.EnterBaseView(false);

        yield return FadeTo(0f);
        isTransitioning = false;
    }

    private void SetLayerImmediate(TuranMapLayer layer)
    {
        CurrentLayer = layer;

        if (globalMapRoot != null)
            globalMapRoot.SetActive(layer == TuranMapLayer.GlobalMap || !hideGlobalMapWhenCityIsOpen);

        if (cityMapRoot != null)
            cityMapRoot.SetActive(layer == TuranMapLayer.CityMap || layer == TuranMapLayer.BaseView);

        if (baseRoot != null)
            baseRoot.SetActive(layer == TuranMapLayer.BaseView);

        if (layer != TuranMapLayer.BaseView && GameModeManager.Instance != null)
            GameModeManager.Instance.EnterWorldMap(true);
    }

    private void AutoWireFromScene()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        GameModeManager mode = GameModeManager.Instance != null
            ? GameModeManager.Instance
            : FindAnyObjectByType<GameModeManager>();

        if (mode != null)
        {
            if (cityMapRoot == null)
                cityMapRoot = mode.worldRoot;

            if (baseRoot == null)
                baseRoot = mode.baseRoot;
        }

        if (cityMapRuntimeRoot == null)
        {
            GameObject root = new GameObject("CityMap_RuntimeRoot");
            cityMapRuntimeRoot = root.transform;
            if (cityMapRoot != null)
                cityMapRuntimeRoot.SetParent(cityMapRoot.transform, false);
        }
    }

    private void BuildCityMapInstance(WorldCityNode city)
    {
        ClearCityMapInstance();

        GameObject prefab = city.GetCityMapPrefab();
        if (prefab == null || cityMapRuntimeRoot == null)
            return;

        activeCityMapInstance = Instantiate(prefab, cityMapRuntimeRoot);
        activeCityMapInstance.name = city.displayName + "_CityMap";
        activeCityMapInstance.transform.localPosition = Vector3.zero;
        activeCityMapInstance.transform.localRotation = Quaternion.identity;
        activeCityMapInstance.transform.localScale = Vector3.one;
    }

    private void ClearCityMapInstance()
    {
        if (activeCityMapInstance == null)
            return;

        Destroy(activeCityMapInstance);
        activeCityMapInstance = null;
    }

    private void FrameGlobalMap()
    {
        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        if (grid == null || mainCamera == null)
            return;

        Bounds bounds = GetHexGridBounds(grid);
        Vector3 target = bounds.center;
        Vector3 position = target + globalCameraOffset;
        ApplyCameraFrame(position, target, globalFieldOfView);
    }

    private void FrameCityMap(WorldCityNode city)
    {
        if (mainCamera == null || city == null)
            return;

        Transform cameraAnchor = city.cityMapCameraAnchor;
        if (cameraAnchor != null)
        {
            ApplyCameraFrame(cameraAnchor.position, cameraAnchor.position + cameraAnchor.forward * 12f, cityFieldOfView);
            return;
        }

        Transform focus = city.cityMapFocusPoint != null ? city.cityMapFocusPoint : city.transform;
        Vector3 target = focus.position;
        Vector3 position = target + cityCameraOffset;
        ApplyCameraFrame(position, target, cityFieldOfView);
    }

    private void ApplyCameraFrame(Vector3 position, Vector3 target, float fov)
    {
        if (mainCamera == null)
            return;

        mainCamera.fieldOfView = fov;
        StopCoroutine(nameof(AnimateCameraTo));
        StartCoroutine(AnimateCameraTo(position, Quaternion.LookRotation(target - position, Vector3.up)));
    }

    private IEnumerator AnimateCameraTo(Vector3 position, Quaternion rotation)
    {
        if (mainCamera == null)
            yield break;

        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        float duration = Mathf.Max(0.01f, cameraSnapSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            mainCamera.transform.position = Vector3.Lerp(startPosition, position, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, rotation, t);
            yield return null;
        }

        mainCamera.transform.position = position;
        mainCamera.transform.rotation = rotation;
    }

    private Bounds GetRenderBounds(GameObject root)
    {
        if (root == null)
            return new Bounds(Vector3.zero, Vector3.one * 20f);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
        if (renderers == null || renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one * 20f);

        bool hasBounds = false;
        Bounds bounds = new Bounds(root.transform.position, Vector3.one * 20f);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null ||
                renderers[i].GetComponentInParent<WorldMapCloudMask>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderers[i].bounds);
        }

        if (!hasBounds)
            return new Bounds(root.transform.position, Vector3.one * 20f);

        return bounds;
    }

    private Bounds GetHexGridBounds(HexGridManager grid)
    {
        if (grid == null || grid.allHexCells == null || grid.allHexCells.Count == 0)
            return grid != null ? GetRenderBounds(grid.gameObject) : new Bounds(Vector3.zero, Vector3.one * 20f);

        bool hasCells = false;
        Bounds bounds = new Bounds(grid.transform.position, Vector3.one * Mathf.Max(1f, grid.size));

        for (int i = 0; i < grid.allHexCells.Count; i++)
        {
            HexCell cell = grid.allHexCells[i];
            if (cell == null)
                continue;

            if (!hasCells)
            {
                bounds = new Bounds(cell.transform.position, Vector3.one * Mathf.Max(1f, grid.size));
                hasCells = true;
            }
            else
            {
                bounds.Encapsulate(cell.transform.position);
            }
        }

        if (!hasCells)
            return GetRenderBounds(grid.gameObject);

        float expand = Mathf.Max(4f, grid.size * 3f);
        bounds.Expand(new Vector3(expand, 0f, expand));
        return bounds;
    }

    private void EnsureFadeCanvas()
    {
        if (!useFade || fadeCanvasGroup != null)
            return;

        GameObject canvasObject = new GameObject("TuranMapFadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = new GameObject("Fade", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.88f);
        fadeCanvasGroup = panel.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        panel.SetActive(false);
    }

    private IEnumerator FadeTo(float target)
    {
        if (!useFade || fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;
        float start = fadeCanvasGroup.alpha;
        float duration = Mathf.Max(0.01f, fadeSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = target;
        fadeCanvasGroup.blocksRaycasts = target > 0.01f;
        if (target <= 0.01f)
            fadeCanvasGroup.gameObject.SetActive(false);
    }
}
