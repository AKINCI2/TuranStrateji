using UnityEngine;
using System.Collections.Generic;

public class SupplySource : MonoBehaviour
{
    [Header("Supply Settings")]
    public float supplyRange = 3f; // Range in hex units or distance
    public float supplyRadius = 15f; // World distance for fallback

    private UnitController unit;

    void Start()
    {
        unit = GetComponent<UnitController>();
        SupplyManager.Instance?.RegisterSource(this);
    }

    void OnDestroy()
    {
        SupplyManager.Instance?.UnregisterSource(this);
    }

    public bool IsInRange(Vector3 position)
    {
        return Vector3.Distance(transform.position, position) <= supplyRadius;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, supplyRadius);
    }
}

