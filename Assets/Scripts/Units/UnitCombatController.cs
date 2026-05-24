using UnityEngine;

public class UnitCombatController : MonoBehaviour
{
    [Header("Combat")]
    public float attackRange = 5f;
    public float detectionRange = 8f;
    public float attackCooldown = 0.25f;

    [Header("Projectile")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Target")]
    public UnitController currentTarget;

    private float lastAttackTime;
    private UnitController unit;

    void Start()
    {
        unit = GetComponent<UnitController>();
    }

    void Update()
    {
        if (unit == null || unit.state == UnitState.Death)
            return;

        FindTarget();
        HandleCombat();
    }

    void FindTarget()
    {
        if (currentTarget != null && IsValidEnemy(currentTarget))
            return;

        currentTarget = null;

        UnitController[] allUnits =
            FindObjectsByType<UnitController>(FindObjectsInactive.Exclude);

        float closestDist = Mathf.Infinity;

        foreach (UnitController other in allUnits)
        {
            if (!IsValidEnemy(other))
                continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < detectionRange && dist < closestDist)
            {
                closestDist = dist;
                currentTarget = other;
            }
        }
    }

    void HandleCombat()
    {
        if (!IsValidEnemy(currentTarget))
        {
            currentTarget = null;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (dist <= attackRange)
        {
            unit.state = UnitState.Attack;
            Attack();
        }
    }

    void Attack()
    {
        if (!IsValidEnemy(currentTarget))
            return;

        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        FormationController formation = GetComponent<FormationController>();
        if (formation != null)
            formation.FireAllSoldiers(bulletPrefab, currentTarget.transform);

        Health targetHealth = currentTarget.GetComponent<Health>();
        if (targetHealth != null)
            targetHealth.TakeDamage(10f);
    }

    private bool IsValidEnemy(UnitController other)
    {
        if (other == null || other == unit)
            return false;

        if (other.assignedBarracks != null)
            return false;

        if (!other.CompareTag("Enemy"))
            return false;

        return other.state != UnitState.Death;
    }
}



