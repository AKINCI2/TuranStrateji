using UnityEngine;

public class UnitAnimatorController : MonoBehaviour
{
    private Animator anim;
    private UnitController unit;
    private bool hasSpeed;
    private bool hasAttack;
    private bool hasIsMoving;
    private bool hasIsAttacking;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        unit = GetComponent<UnitController>();

        if (anim != null)
        {
            anim.applyRootMotion = false;
            CacheAnimatorParameters();
        }
    }

    void Update()
    {
        if (anim == null || unit == null) return;

        bool isMoving = unit.state == UnitState.Move;
        bool isAttacking = unit.state == UnitState.Attack;

        if (hasSpeed)
            anim.SetFloat("Speed", isMoving ? 1f : 0f);

        if (hasAttack)
            anim.SetFloat("Attack", isAttacking ? 1f : 0f);

        if (hasIsMoving)
            anim.SetBool("isMoving", isMoving);

        if (hasIsAttacking)
            anim.SetBool("isAttacking", isAttacking);

        if (isMoving)
            KeepMovingAnimationLooping();
    }

    void CacheAnimatorParameters()
    {
        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.name == "Speed")
                hasSpeed = true;
            else if (parameter.name == "Attack")
                hasAttack = true;
            else if (parameter.name == "isMoving")
                hasIsMoving = true;
            else if (parameter.name == "isAttacking")
                hasIsAttacking = true;
        }
    }

    void KeepMovingAnimationLooping()
    {
        if (anim.IsInTransition(0))
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
}

