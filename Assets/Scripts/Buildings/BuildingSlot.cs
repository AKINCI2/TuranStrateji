using UnityEngine;

public class BuildingSlot : MonoBehaviour
{
    public string slotId = "slot_01";
    public string displayName = "Insaat Parseli";
    public BuildingType allowedType = BuildingType.Barracks;
    public int requiredHeadquartersLevel = 1;
    public Vector2 footprintSize = new Vector2(1.8f, 1.55f);
    public BaseBuilding placedBuilding;

    [Header("Visual")]
    public bool showRuntimeVisual = true;
    public Color availableColor = new Color(0.14f, 0.50f, 0.58f, 0.46f);
    public Color lockedColor = new Color(0.24f, 0.24f, 0.24f, 0.55f);
    public Color occupiedColor = new Color(0.18f, 0.58f, 0.24f, 0.26f);

    private GameObject visualRoot;
    private Renderer padRenderer;
    private BoxCollider slotCollider;

    public bool IsEmpty =>
        placedBuilding == null;

    void Awake()
    {
        EnsureVisual();
        EnsureCollider();
    }

    void Start()
    {
        EnsureCollider();
        RefreshVisual();
    }

    public bool CanPlace(BuildingData buildingData)
    {
        if (buildingData == null || !IsEmpty)
            return false;

        return buildingData.type == allowedType;
    }

    public bool Place(BaseBuilding building)
    {
        if (building == null || building.data == null)
            return false;

        if (!IsEmpty && placedBuilding != building)
            return false;

        if (building.data.type != allowedType)
        {
            return false;
        }

        placedBuilding = building;
        building.transform.SetParent(transform);
        building.transform.localPosition = Vector3.zero;
        building.transform.localRotation = Quaternion.identity;

        RefreshVisual();
        return true;
    }

    public bool CanUse(int headquartersLevel)
    {
        return headquartersLevel >= requiredHeadquartersLevel;
    }

    public void RefreshVisual()
    {
        if (!showRuntimeVisual)
        {
            if (visualRoot != null)
                visualRoot.SetActive(false);

            if (slotCollider != null)
                slotCollider.enabled = false;

            return;
        }

        EnsureVisual();
        EnsureCollider();
        if (slotCollider != null)
            slotCollider.enabled = true;

        if (visualRoot == null || padRenderer == null)
            return;

        visualRoot.SetActive(true);

        int headquartersLevel =
            BaseManager.Instance != null
                ? BaseManager.Instance.HeadquartersLevel
                : 1;

        Color color = IsEmpty
            ? (CanUse(headquartersLevel) ? availableColor : lockedColor)
            : occupiedColor;

        padRenderer.sharedMaterial = CreateMaterial("SlotPad_" + slotId, color);
    }

    public void RebuildVisual()
    {
        if (visualRoot != null)
        {
            if (Application.isPlaying)
                Destroy(visualRoot);
            else
                DestroyImmediate(visualRoot);

            visualRoot = null;
            padRenderer = null;
        }

        EnsureVisual();
        EnsureCollider();
        RefreshVisual();
    }

    private void EnsureVisual()
    {
        if (!showRuntimeVisual)
            return;

        if (visualRoot != null)
            return;

        visualRoot = new GameObject("SlotVisual");
        visualRoot.transform.SetParent(transform, false);
        visualRoot.transform.localPosition = new Vector3(0f, -0.035f, 0f);
        visualRoot.transform.localRotation = Quaternion.identity;

        GameObject pad = new GameObject("Pad", typeof(MeshFilter), typeof(MeshRenderer));
        pad.transform.SetParent(visualRoot.transform, false);
        pad.transform.localPosition = Vector3.zero;
        pad.transform.localRotation = Quaternion.identity;

        Mesh mesh = new Mesh();
        float halfX = Mathf.Max(0.25f, footprintSize.x) * 0.5f;
        float halfZ = Mathf.Max(0.25f, footprintSize.y) * 0.5f;
        mesh.vertices = new[]
        {
            new Vector3(-halfX, 0f, -halfZ),
            new Vector3(-halfX, 0f, halfZ),
            new Vector3(halfX, 0f, halfZ),
            new Vector3(halfX, 0f, -halfZ)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        mesh.RecalculateNormals();

        pad.GetComponent<MeshFilter>().sharedMesh = mesh;
        padRenderer = pad.GetComponent<MeshRenderer>();
        padRenderer.sharedMaterial = CreateMaterial("SlotPad_" + slotId, availableColor);

        CreateBorder(visualRoot.transform, halfX, halfZ);
    }

    private void EnsureCollider()
    {
        slotCollider = GetComponent<BoxCollider>();
        if (slotCollider == null)
            slotCollider = gameObject.AddComponent<BoxCollider>();

        slotCollider.isTrigger = false;
        slotCollider.center = new Vector3(0f, 0.02f, 0f);
        slotCollider.size = new Vector3(
            Mathf.Max(0.25f, footprintSize.x),
            0.08f,
            Mathf.Max(0.25f, footprintSize.y)
        );
        slotCollider.enabled = showRuntimeVisual;
    }

    private void CreateBorder(Transform parent, float halfX, float halfZ)
    {
        float thickness = 0.045f;
        float height = 0.018f;
        CreateBorderBox(parent, "Border_N", new Vector3(halfX * 2f, height, thickness), new Vector3(0f, 0.012f, halfZ));
        CreateBorderBox(parent, "Border_S", new Vector3(halfX * 2f, height, thickness), new Vector3(0f, 0.012f, -halfZ));
        CreateBorderBox(parent, "Border_E", new Vector3(thickness, height, halfZ * 2f), new Vector3(halfX, 0.012f, 0f));
        CreateBorderBox(parent, "Border_W", new Vector3(thickness, height, halfZ * 2f), new Vector3(-halfX, 0.012f, 0f));
    }

    private void CreateBorderBox(Transform parent, string objectName, Vector3 size, Vector3 localPosition)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;

        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreateMaterial(objectName, new Color(0.70f, 0.64f, 0.44f, 0.85f));
    }

    private Material CreateMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        return material;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsEmpty
            ? new Color(0.15f, 0.75f, 0.95f, 0.45f)
            : new Color(0.25f, 0.9f, 0.35f, 0.35f);

        Gizmos.DrawWireCube(
            transform.position + Vector3.up * 0.05f,
            new Vector3(Mathf.Max(0.25f, footprintSize.x), 0.1f, Mathf.Max(0.25f, footprintSize.y))
        );
    }
}

