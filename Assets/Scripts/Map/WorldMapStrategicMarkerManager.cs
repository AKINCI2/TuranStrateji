using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WorldMapStrategicMarkerManager : MonoBehaviour
{
    public Camera mainCamera;
    public float iconModeHeight = 22f;
    public float countryModeHeight = 74f;
    public float markerBaseScale = 1.65f;
    public float markerHeight = 5.5f;

    private readonly Dictionary<Transform, GameObject> markers = new Dictionary<Transform, GameObject>();
    private readonly List<GameObject> countryMarkers = new List<GameObject>();
    private Transform markerRoot;
    private Transform countryRoot;
    private float nextRefreshTime;

    void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + 0.75f;
            RefreshTargets();
        }

        bool showMarkers = (GameModeManager.Instance == null ||
                            GameModeManager.Instance.CurrentMode == GameViewMode.WorldMap) &&
                           mainCamera.transform.position.y >= iconModeHeight;

        foreach (KeyValuePair<Transform, GameObject> pair in markers)
        {
            Transform target = pair.Key;
            GameObject marker = pair.Value;
            if (target == null || marker == null)
                continue;

            marker.SetActive(showMarkers && target.gameObject.activeInHierarchy);
            if (!marker.activeSelf)
                continue;

            marker.transform.position = target.position + Vector3.up * markerHeight;
            marker.transform.rotation = Quaternion.LookRotation(mainCamera.transform.position - marker.transform.position, Vector3.up);

            float scale = Mathf.Clamp(mainCamera.transform.position.y / 42f, 1f, 2.6f) * markerBaseScale;
            marker.transform.localScale = Vector3.one * scale;
        }

        UpdateCountryMarkers();
    }

    private void RefreshTargets()
    {
        EnsureRoot();
        EnsureCountryMarkers();
        AddBaseMarkers();
        AddResourceMarkers();
        AddCityMarkers();
    }

    private void EnsureRoot()
    {
        if (markerRoot != null)
            return;

        GameObject root = new GameObject("WorldMapStrategicMarkers_Runtime");
        markerRoot = root.transform;
    }

    private void EnsureCountryMarkers()
    {
        if (countryRoot != null)
            return;

        GameObject root = new GameObject("WorldCountryMarkers_Runtime");
        root.transform.SetParent(markerRoot, false);
        countryRoot = root.transform;

        Bounds bounds = GetGridBounds();
        CreateCountryMarker("TURKIYE", bounds.center + new Vector3(-bounds.extents.x * 0.35f, 0f, -bounds.extents.z * 0.12f));
        CreateCountryMarker("AZERBAYCAN", bounds.center + new Vector3(bounds.extents.x * 0.22f, 0f, -bounds.extents.z * 0.28f));
        CreateCountryMarker("TURAN BOLGESI", bounds.center + new Vector3(0f, 0f, bounds.extents.z * 0.34f));
    }

    private void UpdateCountryMarkers()
    {
        bool showCountries = (GameModeManager.Instance == null ||
                              GameModeManager.Instance.CurrentMode == GameViewMode.WorldMap) &&
                             mainCamera != null &&
                             mainCamera.transform.position.y >= countryModeHeight;

        foreach (GameObject marker in countryMarkers)
        {
            if (marker == null)
                continue;

            marker.SetActive(showCountries);
            if (!showCountries)
                continue;

            marker.transform.rotation = Quaternion.LookRotation(mainCamera.transform.position - marker.transform.position, Vector3.up);
            float scale = Mathf.Clamp(mainCamera.transform.position.y / 58f, 1.4f, 3.4f);
            marker.transform.localScale = Vector3.one * scale;
        }
    }

    private void CreateCountryMarker(string label, Vector3 position)
    {
        GameObject marker = new GameObject("CountryMarker_" + label);
        marker.transform.SetParent(countryRoot, false);
        marker.transform.position = position + Vector3.up * (markerHeight + 2.5f);

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plate.name = "Plate";
        plate.transform.SetParent(marker.transform, false);
        plate.transform.localScale = new Vector3(3.2f, 0.7f, 1f);

        Collider col = plate.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer renderer = plate.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreateMaterial(new Color(0.08f, 0.12f, 0.13f, 0.82f));

        GameObject textObj = new GameObject("Label", typeof(TextMeshPro));
        textObj.transform.SetParent(marker.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        TextMeshPro text = textObj.GetComponent<TextMeshPro>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 2.7f;
        text.color = new Color(0.88f, 0.78f, 0.48f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(5.4f, 0.8f);

        marker.SetActive(false);
        countryMarkers.Add(marker);
    }

    private void AddBaseMarkers()
    {
        WorldBaseMarker[] bases = FindObjectsByType<WorldBaseMarker>(FindObjectsInactive.Exclude);
        foreach (WorldBaseMarker marker in bases)
        {
            if (marker == null || !marker.IsPlacementMarker)
                continue;

            EnsureMarker(marker.transform, "US", new Color(0.16f, 0.55f, 0.38f, 0.94f));
        }
    }

    private void AddResourceMarkers()
    {
        WorldResourceNode[] nodes = FindObjectsByType<WorldResourceNode>(FindObjectsInactive.Exclude);
        foreach (WorldResourceNode node in nodes)
        {
            if (node == null || node.isCollected)
                continue;

            EnsureMarker(node.transform, GetResourceLabel(node.resourceType), GetResourceColor(node.resourceType));
        }
    }

    private void AddCityMarkers()
    {
        WorldCityNode[] cities = FindObjectsByType<WorldCityNode>(FindObjectsInactive.Exclude);
        foreach (WorldCityNode city in cities)
        {
            if (city == null)
                continue;

            EnsureMarker(city.transform, "Sehir", new Color(0.72f, 0.60f, 0.36f, 0.94f));
        }
    }

    private void EnsureMarker(Transform target, string label, Color color)
    {
        if (target == null || markers.ContainsKey(target))
            return;

        GameObject marker = new GameObject("StrategicMarker_" + target.name, typeof(RectTransform));
        marker.transform.SetParent(markerRoot, false);

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plate.name = "Plate";
        plate.transform.SetParent(marker.transform, false);
        plate.transform.localPosition = Vector3.zero;
        plate.transform.localRotation = Quaternion.identity;
        plate.transform.localScale = new Vector3(1.45f, 0.52f, 1f);

        Collider col = plate.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer renderer = plate.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreateMaterial(color);

        GameObject textObj = new GameObject("Label", typeof(TextMeshPro));
        textObj.transform.SetParent(marker.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        textObj.transform.localRotation = Quaternion.identity;
        TextMeshPro text = textObj.GetComponent<TextMeshPro>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 2.9f;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(2.2f, 0.7f);

        markers[target] = marker;
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return material;
    }

    private string GetResourceLabel(WorldResourceType type)
    {
        switch (type)
        {
            case WorldResourceType.Gold: return "Altin";
            case WorldResourceType.TuranCoin: return "Turan";
            case WorldResourceType.Steel: return "Celik";
            case WorldResourceType.Oil: return "Petrol";
            case WorldResourceType.Bor: return "Bor";
            case WorldResourceType.Wood: return "Kalas";
            case WorldResourceType.Concrete: return "Beton";
            case WorldResourceType.Cement: return "Cimento";
            case WorldResourceType.Brick: return "Tugla";
            default: return "Kaynak";
        }
    }

    private Color GetResourceColor(WorldResourceType type)
    {
        switch (type)
        {
            case WorldResourceType.Gold: return new Color(0.95f, 0.67f, 0.18f, 0.94f);
            case WorldResourceType.TuranCoin: return new Color(0.35f, 0.74f, 0.95f, 0.94f);
            case WorldResourceType.Oil: return new Color(0.16f, 0.39f, 0.32f, 0.94f);
            case WorldResourceType.Bor: return new Color(0.42f, 0.86f, 0.64f, 0.94f);
            case WorldResourceType.Wood: return new Color(0.58f, 0.36f, 0.18f, 0.94f);
            case WorldResourceType.Concrete: return new Color(0.58f, 0.62f, 0.62f, 0.94f);
            case WorldResourceType.Cement: return new Color(0.70f, 0.70f, 0.64f, 0.94f);
            case WorldResourceType.Brick: return new Color(0.62f, 0.28f, 0.18f, 0.94f);
            default: return new Color(0.46f, 0.52f, 0.58f, 0.94f);
        }
    }

    private Bounds GetGridBounds()
    {
        HexGridManager grid = FindAnyObjectByType<HexGridManager>();
        if (grid == null || grid.allHexCells == null || grid.allHexCells.Count == 0)
            return new Bounds(Vector3.zero, new Vector3(90f, 1f, 90f));

        bool hasCells = false;
        Bounds bounds = new Bounds(grid.transform.position, Vector3.one * Mathf.Max(1f, grid.size));
        foreach (HexCell cell in grid.allHexCells)
        {
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

        return bounds;
    }
}
