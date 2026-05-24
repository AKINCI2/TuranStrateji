using UnityEngine;

public class UnitCombat : MonoBehaviour
{
    public float detectionRange = 12f;
    public float attackRange = 2.5f;
    public int damage = 10;
    public float attackRate = 1f;

    private float attackTimer = 0f;

    private Transform target;
    private UnitController unit;

    void Start()
    {
        unit = GetComponent<UnitController>();
    }

    [System.Obsolete]
    void Update()
    {
        if (unit == null || unit.state == UnitState.Death) return;

        if (target == null)
        {
            FindTarget();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist > detectionRange)
        {
            target = null;
            unit.state = UnitState.Idle;
            return;
        }

        if (dist > attackRange)
        {
            // 🔥 sadece idle ise hareket başlat
            if (unit.state != UnitState.Move)
            {
                MoveToTarget();
            }
        }
        else
        {
            unit.state = UnitState.Attack;
            Attack();
        }
    }

    void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float min = Mathf.Infinity;
        Transform best = null;

        foreach (var e in enemies)
        {
            UnitController otherUnit = e.GetComponent<UnitController>();
            if (otherUnit == null || otherUnit == unit || otherUnit.assignedBarracks != null || otherUnit.state == UnitState.Death)
                continue;

            float d = Vector3.Distance(transform.position, e.transform.position);

            if (d < min && d <= detectionRange)
            {
                min = d;
                best = e.transform;
            }
        }

        if (best != null)
        {
            target = best;
        }
    }

    [System.Obsolete]
    void MoveToTarget()
    {
        if (unit.currentHex == null) return;

        var grid = FindFirstObjectByType<HexGridManager>();
        HexCell targetHex = grid.GetClosestHex(target.position);

        var path = Pathfinding.Instance.FindPath(unit.currentHex, targetHex);

        if (path != null && path.Count > 0)
        {
            unit.MovePath(path);
        }
    }

    [System.Obsolete]
    void Attack()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackRate)
        {
            var h = target.GetComponent<Health>();
            if (h != null)
                h.TakeDamage(damage);

            attackTimer = 0f;
        }
    }
}

