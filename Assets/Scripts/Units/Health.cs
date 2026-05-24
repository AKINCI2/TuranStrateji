using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public bool debugLogs = false;

    public int currentHealth;

    private int lastSoldierCount;

    void Start()
    {
        currentHealth = maxHealth;

        FormationController formation =
            GetComponent<FormationController>();

        if (formation != null)
        {
            lastSoldierCount =
                formation.soldiers.Count;
        }

        if (debugLogs)
        {
            Debug.Log(
                gameObject.name +
                " Health başlatıldı"
            );
        }
    }

    // =================================================
    // DAMAGE
    // =================================================

    public void TakeDamage(float damage)
    {
        currentHealth -=
            Mathf.RoundToInt(damage);

        if (debugLogs)
        {
            Debug.Log(
                gameObject.name +
                " hasar aldı: " +
                damage +
                " | Kalan HP: " +
                currentHealth
            );
        }

        // =================================================
        // CASUALTY SYSTEM
        // =================================================

        FormationController formation =
            GetComponent<FormationController>();

        if (formation != null)
        {
            int currentSoldierCount =
                Mathf.CeilToInt(
                    (float)currentHealth /
                    maxHealth *
                    formation.maxSoldiers
                );

            if (currentSoldierCount <
                lastSoldierCount)
            {
                formation.KillSoldier();

                lastSoldierCount =
                    currentSoldierCount;
            }
        }

        // =================================================
        // Death
        // =================================================

        if (currentHealth <= 0)
        {
            UnitController unit =
                GetComponent<UnitController>();

            if (unit != null)
            {
                unit.state =
                    UnitState.Death;
            }

            Destroy(gameObject, 4f);
        }
    }
}

