using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitOfficerBadge : MonoBehaviour
{
    [Header("Layout")]
    public Vector3 localOffset = new Vector3(0f, 2.35f, 0f);
    public Vector2 badgeSize = new Vector2(30f, 30f);
    public float worldScale = 0.012f;

    private UnitController unit;
    private Canvas canvas;
    private Image portraitImage;
    private Image frameImage;
    private OfficerData lastOfficer;

    void Awake()
    {
        unit = GetComponent<UnitController>();
        BuildBadge();
    }

    void LateUpdate()
    {
        Refresh();
        FaceCamera();
    }

    private void BuildBadge()
    {
        GameObject canvasObject = new GameObject("OfficerBadge", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = localOffset;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * worldScale;

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.sizeDelta = badgeSize;

        GameObject frameObject = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(root, false);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        frameImage = frameObject.GetComponent<Image>();
        frameImage.color = new Color(0.04f, 0.12f, 0.08f, 0.88f);

        GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(root, false);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
        portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRect.pivot = new Vector2(0.5f, 0.5f);
        portraitRect.anchoredPosition = Vector2.zero;
        portraitRect.sizeDelta = new Vector2(24f, 24f);
        portraitImage = portraitObject.GetComponent<Image>();
        portraitImage.color = new Color(0.78f, 0.70f, 0.52f, 1f);
    }

    private void Refresh()
    {
        if (unit == null)
            unit = GetComponent<UnitController>();

        OfficerData officer = unit != null && unit.assignedBarracks != null
            ? unit.assignedBarracks.assignedOfficer
            : null;

        bool visible =
            officer != null &&
            unit != null &&
            unit.deploymentState == UnitDeploymentState.OnWorldMap &&
            gameObject.activeInHierarchy;

        if (canvas != null)
            canvas.gameObject.SetActive(visible);

        if (!visible || officer == lastOfficer)
            return;

        lastOfficer = officer;
        if (portraitImage != null)
        {
            portraitImage.sprite = officer.portrait;
            portraitImage.color = officer.portrait != null
                ? Color.white
                : new Color(0.78f, 0.70f, 0.52f, 1f);
        }
    }

    private void FaceCamera()
    {
        if (canvas == null || Camera.main == null || !canvas.gameObject.activeSelf)
            return;

        Transform cameraTransform = Camera.main.transform;
        canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cameraTransform.position);
    }
}
