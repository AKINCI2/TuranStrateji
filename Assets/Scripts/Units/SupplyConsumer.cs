using UnityEngine;

public class SupplyConsumer : MonoBehaviour
{
    [Header("Supply Status")]
    public bool isSupplied = true;
    public float unsuppliedAttackPenalty = 0.5f; // 50% slower
    public float unsuppliedSpeedPenalty = 0.7f; // 30% slower

    private UnitController unit;
    private UnitCombatController combat;
    
    // Original values to restore
    private float baseMoveSpeed;
    private float baseAttackCooldown;

    void Start()
    {
        unit = GetComponent<UnitController>();
        combat = GetComponent<UnitCombatController>();

        if (unit != null) baseMoveSpeed = unit.moveSpeed;
        if (combat != null) baseAttackCooldown = combat.attackCooldown;

        SupplyManager.Instance?.RegisterConsumer(this);
    }

    void OnDestroy()
    {
        SupplyManager.Instance?.UnregisterConsumer(this);
    }

    public void SetSupplied(bool supplied)
    {
        if (isSupplied == supplied) return;

        isSupplied = supplied;
        ApplySupplyEffects();
    }

    public bool IsNearBase()
    {
        // Simple check if unit is near starting base
        // In a real scenario, this would check if the current hex is connected to base
        // For now, let's assume if it's within a large radius of the world base marker
        var baseMarker = WorldBaseMarker.FindPrimary();
        if (baseMarker != null)
        {
            return Vector3.Distance(transform.position, baseMarker.transform.position) < 20f;
        }
        return false;
    }

    private void ApplySupplyEffects()
    {
        if (unit != null)
        {
            unit.moveSpeed = isSupplied ? baseMoveSpeed : baseMoveSpeed * unsuppliedSpeedPenalty;
        }

        if (combat != null)
        {
            // Note: If supplied, cooldown is normal. If not supplied, cooldown is longer (slower attack).
            combat.attackCooldown = isSupplied ? baseAttackCooldown : baseAttackCooldown / unsuppliedAttackPenalty;
        }

        if (isSupplied)
            Debug.Log($"{name} is now SUPPLIED.");
        else
            Debug.Log($"{name} is OUT OF SUPPLY! Penalty applied.");
    }
}

