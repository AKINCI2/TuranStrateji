using UnityEngine;

public class UnitCombatController : MonoBehaviour
{
    [Header("Combat")]
    public float attackRange = 5f;
    public float detectionRange = 8f;
    public float attackCooldown = 0.25f;
    public float damage = 10f;

    [Header("Behaviour")]
    public bool chaseTargets = true;
    public float targetRefreshInterval = 0.25f;

    [Header("Projectile")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Target")]
    public UnitController currentTarget;

    private float lastAttackTime;
    private float nextTargetRefreshTime;
    private UnitController unit;

    void Start()
    {
        unit = GetComponent<UnitController>();
        ApplyStatsFromData();
    }

    void Update()
    {
        if (unit == null)
            unit = GetComponent<UnitController>();

        if (unit == null || unit.state == UnitState.Death)
            return;

        FindTarget();
        HandleCombat();
    }

    public void ApplyStatsFromData()
    {
        if (unit == null)
            unit = GetComponent<UnitController>();

        if (unit == null || unit.unitData == null)
            return;

        if (unit.unitData.attack > 0)
            damage = unit.unitData.attack;

        WeaponData weapon = unit.unitData.weaponData;
        if (weapon == null)
            return;

        if (weapon.attack > 0)
            damage = weapon.attack;

        if (weapon.range > 0f)
            attackRange = weapon.range;

        if (weapon.fireRate > 0f)
            attackCooldown = 1f / weapon.fireRate;
    }

    void FindTarget()
    {
        if (currentTarget != null && IsValidEnemy(currentTarget))
        {
            float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (dist <= detectionRange) return;
        }

        if (Time.time < nextTargetRefreshTime)
            return;

        nextTargetRefreshTime = Time.time + Mathf.Max(0.05f, targetRefreshInterval);
        currentTarget = UnitManager.Instance != null
            ? UnitManager.Instance.GetNearestHostile(unit, detectionRange)
            : FindNearestHostileFallback();
    }

    void HandleCombat()
    {
        if (!IsValidEnemy(currentTarget))
        {
            currentTarget = null;
            if (unit.state == UnitState.Attack)
                unit.state = UnitState.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        
        if (dist <= attackRange)
        {
            unit.state = UnitState.Attack;
            Attack();
        }
        else if (chaseTargets && dist <= detectionRange)
        {
            unit.state = UnitState.Move;
            ChaseTarget();
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
            targetHealth.TakeDamage(damage);
    }

    private bool IsValidEnemy(UnitController other)
    {
        if (other == null || other == unit)
            return false;

        if (other.state == UnitState.Death ||
            other.deploymentState != UnitDeploymentState.OnWorldMap ||
            !other.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (UnitManager.Instance != null)
            return UnitManager.Instance.AreHostile(unit, other);

        return unit.CompareTag("Enemy") != other.CompareTag("Enemy");
    }

    private void ChaseTarget()
    {
        Vector3 direction = currentTarget.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Vector3 normalized = direction.normalized;
        transform.position += normalized * unit.moveSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(normalized),
            10f * Time.deltaTime
        );
    }

    private UnitController FindNearestHostileFallback()
    {
        UnitController[] allUnits = FindObjectsByType<UnitController>(FindObjectsInactive.Exclude);
        UnitController closest = null;
        float minSqrDistance = detectionRange * detectionRange;

        foreach (UnitController other in allUnits)
        {
            if (!IsValidEnemy(other))
                continue;

            float sqrDistance = (transform.position - other.transform.position).sqrMagnitude;
            if (sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                closest = other;
            }
        }

        return closest;
    }
}



