using UnityEngine;

public class UnitSelectionVisual : MonoBehaviour
{
    [Header("Ring")]
    public float radius = 0.85f;
    public float lineWidth = 0.05f;
    public float heightOffset = 0.12f;
    public Color selectedColor = new Color(0.15f, 0.85f, 1f, 1f);

    private LineRenderer ring;

    void Awake()
    {
        CreateRing();
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (ring == null)
            CreateRing();

        ring.enabled = selected;
    }

    void CreateRing()
    {
        Transform existing =
            transform.Find("SelectionRing");

        GameObject ringObject =
            existing != null
                ? existing.gameObject
                : new GameObject("SelectionRing");

        ringObject.transform.SetParent(transform);
        ringObject.transform.localPosition = Vector3.up * heightOffset;
        ringObject.transform.localRotation = Quaternion.identity;
        ringObject.transform.localScale = Vector3.one;

        ring =
            ringObject.GetComponent<LineRenderer>();

        if (ring == null)
            ring = ringObject.AddComponent<LineRenderer>();

        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 64;
        ring.startWidth = lineWidth;
        ring.endWidth = lineWidth;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;

        Material material =
            new Material(Shader.Find("Sprites/Default"));

        material.color = selectedColor;
        ring.material = material;
        ring.startColor = selectedColor;
        ring.endColor = selectedColor;

        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle =
                i * Mathf.PI * 2f / ring.positionCount;

            ring.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                )
            );
        }
    }
}

