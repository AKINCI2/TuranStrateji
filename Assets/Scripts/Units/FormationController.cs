using UnityEngine;
using System.Collections.Generic;

public class FormationController : MonoBehaviour
{
    private HashSet<Transform> deadSoldiers =
    new HashSet<Transform>();
    [Header("Formation")]
    public List<Transform> soldiers =
        new List<Transform>();

    [Range(0.1f, 2f)]
    public float spacing = 0.5f;

    [Range(0.2f, 0.8f)]
    public float hexFillRatio = 0.42f;

    public float followSpeed = 10f;

    [Header("Casualty")]
    public int maxSoldiers = 4;

    [Header("Effects")]
    public GameObject muzzleFlashPrefab;

    private List<Vector3> offsets =
        new List<Vector3>();

    private UnitCombatController combat;
    private HexGridManager gridManager;
    private float safeRadius = 0.5f;

    void Start()
    {
        combat =
            GetComponent<UnitCombatController>();

        gridManager =
            FindAnyObjectByType<HexGridManager>();

        CalculateSafeRadius();
        CreateOffsets();

        // =====================================
        // ROOT MOTION OFF
        // =====================================

        foreach (Transform soldier in soldiers)
        {
            if (soldier == null)
                continue;

            Animator anim =
                soldier.GetComponentInChildren<Animator>();

            if (anim != null)
            {
                anim.applyRootMotion = false;
            }
        }
    }

    void Update()
    {
        UpdateFormation();
    }

    public void SetVisibleSoldierCount(int count)
    {
        int visibleCount = Mathf.Clamp(count, 0, soldiers.Count);

        for (int i = 0; i < soldiers.Count; i++)
        {
            Transform soldier = soldiers[i];
            if (soldier != null)
                soldier.gameObject.SetActive(i < visibleCount);
        }
    }

    // =================================================
    // OFFSETS
    // =================================================

    void CreateOffsets()
    {
        offsets.Clear();

        float radius =
            Mathf.Min(spacing, safeRadius);

        offsets.Add(Vector3.zero);
        offsets.Add(ClampInsideHex(new Vector3(-radius, 0f, -radius * 0.45f)));
        offsets.Add(ClampInsideHex(new Vector3(radius, 0f, -radius * 0.45f)));
        offsets.Add(ClampInsideHex(new Vector3(0f, 0f, radius * 0.9f)));

        int offsetCount =
            Mathf.Max(maxSoldiers, soldiers.Count);

        for (int i = offsets.Count; i < offsetCount; i++)
        {
            float angle =
                i * Mathf.PI * 2f / Mathf.Max(1, offsetCount);

            Vector3 offset =
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

            offsets.Add(ClampInsideHex(offset));
        }
    }

    void CalculateSafeRadius()
    {
        if (gridManager != null)
        {
            safeRadius =
                Mathf.Max(0.15f, gridManager.size * hexFillRatio);
            return;
        }

        safeRadius =
            Mathf.Max(0.15f, spacing * 0.75f);
    }

    Vector3 ClampInsideHex(Vector3 offset)
    {
        Vector2 flat =
            new Vector2(offset.x, offset.z);

        if (flat.magnitude <= safeRadius)
            return offset;

        flat =
            flat.normalized * safeRadius;

        return new Vector3(flat.x, offset.y, flat.y);
    }

    // =================================================
    // UPDATE FORMATION
    // =================================================

    void UpdateFormation()
    {
        UnitController leader =
            GetComponent<UnitController>();

        if (leader == null)
            return;

        bool isMoving =
            leader.state == UnitState.Move;

        bool isAttacking =
            leader.state == UnitState.Attack;

        bool isDead =
            leader.state == UnitState.Death;

        Transform combatTarget = null;

        if (combat != null &&
            combat.currentTarget != null)
        {
            combatTarget =
                combat.currentTarget.transform;
        }

        for (int i = 0; i < soldiers.Count; i++)
        {
            if (i >= offsets.Count)
                continue;

            Transform soldier =
                soldiers[i];

            if (soldier == null)
                continue;

            // =========================================
            // POSITION
            // =========================================

            Vector3 targetPos =
                ClampInsideHex(offsets[i]);

            soldier.localPosition =
                Vector3.Lerp(
                    soldier.localPosition,
                    targetPos,
                    followSpeed *
                    Time.deltaTime
                );

            // =========================================
            // ROTATION
            // =========================================

            Quaternion targetRot;

            if (isAttacking &&
                combatTarget != null)
            {
                Vector3 dir =
                    combatTarget.position -
                    soldier.position;

                dir.y = 0;

                if (dir != Vector3.zero)
                {
                    targetRot =
                        Quaternion.LookRotation(dir);
                }
                else
                {
                    targetRot =
                        transform.rotation;
                }
            }
            else
            {
                targetRot =
                    transform.rotation;
            }

            soldier.rotation =
                Quaternion.Slerp(
                    soldier.rotation,
                    targetRot,
                    10f * Time.deltaTime
                );

            // =========================================
            // ANIMATOR
            // =========================================

            Animator anim =
                soldier.GetComponentInChildren<Animator>();

            if (anim != null)
            {
                // MOVE
                anim.SetFloat(
                    "Speed",
                    isMoving ? 1f : 0f
                );

                // ATTACK
                anim.SetFloat(
                    "Attack",
                    isAttacking ? 1f : 0f
                );

                // ATTACK BOOL
                anim.SetBool(
                    "isAttacking",
                    isAttacking
                );

                if (isMoving)
                    KeepMovingAnimationLooping(anim);
            }
        }
    }

    void KeepMovingAnimationLooping(Animator anim)
    {
        if (anim == null || anim.IsInTransition(0))
            return;

        AnimatorStateInfo state =
            anim.GetCurrentAnimatorStateInfo(0);

        if (!state.IsName("Run") &&
            !state.IsName("Walk") &&
            !state.IsName("Walking"))
        {
            return;
        }

        if (state.normalizedTime >= 0.98f)
        {
            anim.Play(state.fullPathHash, 0, 0f);
        }
    }

    // =================================================
    // FIRE
    // =================================================

    public void FireAllSoldiers(
        GameObject bulletPrefab,
        Transform enemyLeader)
    {
        if (enemyLeader == null)
            return;

        FormationController enemyFormation =
            enemyLeader.GetComponent<FormationController>();

        if (enemyFormation == null)
            return;

        List<Transform> enemySoldiers =
            enemyFormation.soldiers;

        foreach (Transform soldier in soldiers)
        {
            if (soldier == null)
                continue;

            if (enemySoldiers.Count == 0)
                return;

            Transform targetSoldier =
                enemySoldiers[
                    Random.Range(
                        0,
                        enemySoldiers.Count
                    )
                ];

            if (targetSoldier == null)
                continue;

            Transform firePoint =
                soldier.Find("FirePoint");

            if (firePoint == null)
                continue;

            // =========================================
            // MUZZLE FLASH
            // =========================================

            if (muzzleFlashPrefab != null)
            {
                Instantiate(
                    muzzleFlashPrefab,
                    firePoint.position,
                    firePoint.rotation
                );
            }

            // =========================================
            // BULLET
            // =========================================

            GameObject bullet =
                Instantiate(
                    bulletPrefab,
                    firePoint.position,
                    Quaternion.identity
                );

            Projectile proj =
                bullet.GetComponent<Projectile>();

            if (proj != null)
            {
                proj.SetTarget(
                    targetSoldier
                );
            }
        }
    }

    // =================================================
    // CASUALTY
    // =================================================

    public void KillSoldier()
    {
        for (int i = soldiers.Count - 1; i >= 0; i--)
        {
            Transform deadSoldier =
                soldiers[i];

            if (deadSoldier == null)
                continue;

            // 🔥 Zaten öldü mü
            if (deadSoldiers.Contains(deadSoldier))
                continue;

            // 🔥 Listeye ekle
            deadSoldiers.Add(deadSoldier);

            Animator anim =
                deadSoldier
                .GetComponentInChildren<Animator>();

            if (anim != null)
            {
                anim.SetFloat(
                    "Speed",
                    0f
                );

                anim.SetFloat(
                    "Attack",
                    0f
                );

                anim.SetBool(
                    "isAttacking",
                    false
                );

                // 🔥 Tek sefer death
                anim.ResetTrigger(
                    "Death"
                );

                anim.SetTrigger(
                    "Death"
                );
            }

            Collider col =
                deadSoldier.GetComponent<Collider>();

            if (col != null)
            {
                col.enabled = false;
            }

            Destroy(
                deadSoldier.gameObject,
                4f
            );

            break;
        }
    }
}

