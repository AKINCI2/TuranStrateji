using UnityEngine;

public class WorldMapCloudMask : MonoBehaviour
{
    [Header("Target")]
    public HexGridManager grid;
    public Transform cloudRoot;
    public float yOffset = 2.5f;
    public float edgePadding = -1.5f;
    public float cloudBandWidth = 44f;

    [Header("Visual")]
    public Color cloudColor = new Color(0.82f, 0.88f, 0.90f, 0.50f);
    public int cloudPuffsPerSide = 0;
    public Vector2 puffScaleRange = new Vector2(4f, 8f);
    public bool createConnectedBands = true;
    public bool createLoosePuffs = false;
    public bool rebuildOnStart = true;

    void Start()
    {
        if (rebuildOnStart)
            Rebuild();
    }

    public void Rebuild()
    {
        if (grid == null)
            grid = FindAnyObjectByType<HexGridManager>();

        if (grid == null)
            return;

        NormalizeRuntimeSettings();
        EnsureRoot();
        ClearRoot();

        Bounds bounds = GetGridBounds();
        if (createConnectedBands)
        {
            CreateCloudBand("NorthBand", bounds, Vector3.forward);
            CreateCloudBand("SouthBand", bounds, Vector3.back);
            CreateCloudBand("EastBand", bounds, Vector3.right);
            CreateCloudBand("WestBand", bounds, Vector3.left);
            CreateCloudCorner("NorthEastCorner", bounds, 1f, 1f);
            CreateCloudCorner("NorthWestCorner", bounds, -1f, 1f);
            CreateCloudCorner("SouthEastCorner", bounds, 1f, -1f);
            CreateCloudCorner("SouthWestCorner", bounds, -1f, -1f);
        }

        if (createLoosePuffs)
        {
            CreateCloudSide("North", bounds, Vector3.forward);
            CreateCloudSide("South", bounds, Vector3.back);
            CreateCloudSide("East", bounds, Vector3.right);
            CreateCloudSide("West", bounds, Vector3.left);
        }
    }

    private void NormalizeRuntimeSettings()
    {
        edgePadding = Mathf.Clamp(edgePadding, -2.5f, 0.5f);
        cloudBandWidth = Mathf.Clamp(cloudBandWidth, 34f, 52f);
        cloudPuffsPerSide = Mathf.Clamp(cloudPuffsPerSide, 0, 8);
        puffScaleRange = new Vector2(
            Mathf.Clamp(puffScaleRange.x, 2.5f, 5f),
            Mathf.Clamp(puffScaleRange.y, 4f, 8f));
        cloudColor = new Color(
            Mathf.Clamp01(cloudColor.r),
            Mathf.Clamp01(cloudColor.g),
            Mathf.Clamp01(cloudColor.b),
            Mathf.Clamp(cloudColor.a, 0.42f, 0.55f));
        createConnectedBands = true;
        createLoosePuffs = false;
    }

    private void EnsureRoot()
    {
        if (cloudRoot != null)
            return;

        Transform existing = transform.Find("WorldMapCloudMask_Runtime");
        if (existing != null)
        {
            cloudRoot = existing;
            return;
        }

        GameObject root = new GameObject("WorldMapCloudMask_Runtime");
        root.transform.SetParent(transform, false);
        cloudRoot = root.transform;
    }

    private void ClearRoot()
    {
        if (cloudRoot == null)
            return;

        for (int i = cloudRoot.childCount - 1; i >= 0; i--)
            Destroy(cloudRoot.GetChild(i).gameObject);
    }

    private Bounds GetGridBounds()
    {
        if (grid.allHexCells != null && grid.allHexCells.Count > 0)
        {
            bool hasCells = false;
            Bounds cellBounds = new Bounds(grid.transform.position, Vector3.one);
            for (int i = 0; i < grid.allHexCells.Count; i++)
            {
                HexCell cell = grid.allHexCells[i];
                if (cell == null)
                    continue;

                if (!hasCells)
                {
                    cellBounds = new Bounds(cell.transform.position, Vector3.one * Mathf.Max(1f, grid.size));
                    hasCells = true;
                }
                else
                {
                    cellBounds.Encapsulate(cell.transform.position);
                }
            }

            if (hasCells)
            {
                float expand = Mathf.Max(1.5f, grid.size * 1.5f);
                cellBounds.Expand(new Vector3(expand, 0f, expand));
                return cellBounds;
            }
        }

        Renderer[] renderers = grid.GetComponentsInChildren<Renderer>(false);
        if (renderers == null || renderers.Length == 0)
            return new Bounds(grid.transform.position, new Vector3(90f, 1f, 90f));

        bool hasBounds = false;
        Bounds bounds = new Bounds(grid.transform.position, new Vector3(90f, 1f, 90f));
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

        return bounds;
    }

    private void CreateCloudBand(string objectName, Bounds bounds, Vector3 outward)
    {
        bool horizontal = Mathf.Abs(outward.z) > 0.5f;
        Vector3 center = bounds.center;
        center.y += yOffset - 0.03f;

        float length = horizontal ? bounds.size.x + cloudBandWidth * 2f : bounds.size.z + cloudBandWidth * 2f;
        float width = Mathf.Max(24f, cloudBandWidth);

        if (horizontal)
            center.z += Mathf.Sign(outward.z) * (bounds.extents.z + edgePadding + width * 0.5f);
        else
            center.x += Mathf.Sign(outward.x) * (bounds.extents.x + edgePadding + width * 0.5f);

        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Quad);
        band.name = objectName;
        band.transform.SetParent(cloudRoot, true);
        band.transform.position = center;
        band.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        band.transform.localScale = horizontal
            ? new Vector3(length, width, 1f)
            : new Vector3(width, length, 1f);

        Collider col = band.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer renderer = band.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.sharedMaterial = CreateCloudMaterial(new Color(
            cloudColor.r,
            cloudColor.g,
            cloudColor.b,
            Mathf.Clamp01(cloudColor.a * 0.55f)));
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void CreateCloudCorner(string objectName, Bounds bounds, float xSign, float zSign)
    {
        Vector3 center = bounds.center;
        center.y += yOffset - 0.04f;
        center.x += xSign * (bounds.extents.x + edgePadding + cloudBandWidth * 0.5f);
        center.z += zSign * (bounds.extents.z + edgePadding + cloudBandWidth * 0.5f);

        GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Quad);
        corner.name = objectName;
        corner.transform.SetParent(cloudRoot, true);
        corner.transform.position = center;
        corner.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        corner.transform.localScale = new Vector3(cloudBandWidth * 1.35f, cloudBandWidth * 1.35f, 1f);

        Collider col = corner.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer renderer = corner.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.sharedMaterial = CreateCloudMaterial(cloudColor);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void CreateCloudSide(string sideName, Bounds bounds, Vector3 outward)
    {
        bool horizontal = Mathf.Abs(outward.z) > 0.5f;
        float length = horizontal ? bounds.size.x : bounds.size.z;
        Vector3 center = bounds.center;
        center.y += yOffset;

        if (horizontal)
            center.z += Mathf.Sign(outward.z) * (bounds.extents.z + edgePadding);
        else
            center.x += Mathf.Sign(outward.x) * (bounds.extents.x + edgePadding);

        for (int i = 0; i < cloudPuffsPerSide; i++)
        {
            float t = cloudPuffsPerSide <= 1 ? 0.5f : i / (float)(cloudPuffsPerSide - 1);
            float along = Mathf.Lerp(-length * 0.55f, length * 0.55f, t);
            float jitter = Mathf.PerlinNoise(i * 0.37f, sideName.Length * 0.71f);
            float outwardOffset = Mathf.Lerp(cloudBandWidth * 0.45f, cloudBandWidth * 1.2f, jitter);

            Vector3 pos = center;
            if (horizontal)
            {
                pos.x += along;
                pos.z += Mathf.Sign(outward.z) * outwardOffset;
            }
            else
            {
                pos.z += along;
                pos.x += Mathf.Sign(outward.x) * outwardOffset;
            }

            float scale = Mathf.Lerp(puffScaleRange.x, puffScaleRange.y, Mathf.PerlinNoise(i * 0.23f, 9.1f));
            CreateCloudPuff(sideName + "_" + i, pos, scale);
        }
    }

    private void CreateCloudPuff(string objectName, Vector3 position, float scale)
    {
        GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Quad);
        puff.name = objectName;
        puff.transform.SetParent(cloudRoot, true);
        puff.transform.position = position;
        puff.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        puff.transform.localScale = new Vector3(scale * 1.55f, scale, 1f);

        Collider col = puff.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer renderer = puff.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.sharedMaterial = CreateCloudMaterial(cloudColor);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private Material CreateCloudMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.color = cloudColor;
        mat.color = color;

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);

        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return mat;
    }
}
