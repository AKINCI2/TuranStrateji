using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldBaseTransitionManager : MonoBehaviour
{
    public static WorldBaseTransitionManager Instance { get; private set; }

    [Header("Scene Loading")]
    public bool useAsyncSceneLoading = false; // Kapalı: Sahne bazlı değil root bazlı geçiş yapılıyor
    public string worldSceneName = "WorldMap";
public string baseSceneName = "BaseScene";
    public bool unloadWorldSceneOnBase;
    public bool unloadBaseSceneOnWorld = true;

    [Header("Fallback")]
    public bool fallbackToRootMode = true;

    [Header("Fade Loading")]
    public bool useFadeLoading;
    public CanvasGroup fadeCanvasGroup;
    public GameObject loadingRoot;
    public Text loadingText;
    public float fadeSeconds = 0.22f;
    public string baseLoadingMessage = "Usse giriliyor...";
    public string worldLoadingMessage = "Haritaya donuluyor...";

    public bool IsTransitioning => isTransitioning;

    private bool isTransitioning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (useFadeLoading)
            EnsureLoadingUI();
        else
            HideLoadingUI();
    }

    public static WorldBaseTransitionManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        WorldBaseTransitionManager existing = FindAnyObjectByType<WorldBaseTransitionManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject obj = new GameObject("WorldBaseTransitionManager");
        return obj.AddComponent<WorldBaseTransitionManager>();
    }

    public void EnterBase(WorldBaseMarker marker, bool preserveCameraFrame)
    {
        if (isTransitioning)
            return;

        StartCoroutine(EnterBaseRoutine(preserveCameraFrame));
    }

    public void ExitBase(bool preserveCameraFrame)
    {
        if (isTransitioning)
            return;

        StartCoroutine(ExitBaseRoutine(preserveCameraFrame));
    }

    private IEnumerator EnterBaseRoutine(bool preserveCameraFrame)
    {
        isTransitioning = true;
        if (useFadeLoading)
        {
            SetLoadingText(baseLoadingMessage);
            yield return FadeTo(1f);
        }

        if (useAsyncSceneLoading && CanLoadScene(baseSceneName))
        {
            yield return LoadSceneIfNeeded(baseSceneName, LoadSceneMode.Additive);

            Scene baseScene = SceneManager.GetSceneByName(baseSceneName);
            if (baseScene.IsValid())
                SceneManager.SetActiveScene(baseScene);

            if (unloadWorldSceneOnBase && CanUnloadScene(worldSceneName))
                yield return SceneManager.UnloadSceneAsync(worldSceneName);
        }

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.EnterBaseView(preserveCameraFrame);
        else if (!fallbackToRootMode)
            Debug.LogWarning("GameModeManager bulunamadi; BaseScene yuklendi ama kok gecisi yapilamadi.");

        if (useFadeLoading)
            yield return FadeTo(0f);
        else
            HideLoadingUI();

        isTransitioning = false;
    }

    private IEnumerator ExitBaseRoutine(bool preserveCameraFrame)
    {
        isTransitioning = true;
        if (useFadeLoading)
        {
            SetLoadingText(worldLoadingMessage);
            yield return FadeTo(1f);
        }

        if (useAsyncSceneLoading && CanLoadScene(worldSceneName))
        {
            yield return LoadSceneIfNeeded(worldSceneName, LoadSceneMode.Additive);

            Scene worldScene = SceneManager.GetSceneByName(worldSceneName);
            if (worldScene.IsValid())
                SceneManager.SetActiveScene(worldScene);

            if (unloadBaseSceneOnWorld && CanUnloadScene(baseSceneName))
                yield return SceneManager.UnloadSceneAsync(baseSceneName);
        }

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.EnterWorldMap(preserveCameraFrame);
        else if (!fallbackToRootMode)
            Debug.LogWarning("GameModeManager bulunamadi; WorldMap yuklendi ama kok gecisi yapilamadi.");

        if (useFadeLoading)
            yield return FadeTo(0f);
        else
            HideLoadingUI();

        isTransitioning = false;
    }

    private IEnumerator LoadSceneIfNeeded(string sceneName, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            yield break;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
            yield break;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
        if (operation == null)
            yield break;

        while (!operation.isDone)
            yield return null;
    }

    private bool CanLoadScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) &&
               Application.CanStreamedLevelBeLoaded(sceneName);
    }

    private bool CanUnloadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private void EnsureLoadingUI()
    {
        if (fadeCanvasGroup != null && loadingRoot != null)
            return;

        GameObject canvasObject = new GameObject("TransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("FadePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.88f);
        image.raycastTarget = true;

        fadeCanvasGroup = panel.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
        loadingRoot = panel;

        GameObject textObject = new GameObject("LoadingText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(520f, 80f);
        textRect.anchoredPosition = Vector2.zero;

        loadingText = textObject.GetComponent<Text>();
        loadingText.alignment = TextAnchor.MiddleCenter;
        loadingText.color = Color.white;
        loadingText.fontSize = 28;
        loadingText.text = "";

        loadingRoot.SetActive(false);
    }

    private void SetLoadingText(string message)
    {
        if (!useFadeLoading)
            return;

        EnsureLoadingUI();
        if (loadingText != null)
            loadingText.text = message;
    }

    private void HideLoadingUI()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }

        if (loadingRoot != null)
            loadingRoot.SetActive(false);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (!useFadeLoading)
        {
            HideLoadingUI();
            yield break;
        }

        EnsureLoadingUI();

        if (loadingRoot != null)
            loadingRoot.SetActive(true);

        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = true;

        float startAlpha = fadeCanvasGroup.alpha;
        float duration = Mathf.Max(0.01f, fadeSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0.001f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;

            if (loadingRoot != null)
                loadingRoot.SetActive(false);
        }
    }
}
