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
        ApplyStatsFromData(true);

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

    public void ApplyStatsFromData(bool refill = false)
    {
        UnitController unit = GetComponent<UnitController>();
        if (unit == null || unit.unitData == null || unit.unitData.health <= 0)
        {
            if (refill || currentHealth <= 0)
                currentHealth = maxHealth;

            return;
        }

        int previousMaxHealth = Mathf.Max(1, maxHealth);
        float healthRatio = currentHealth > 0
            ? Mathf.Clamp01((float)currentHealth / previousMaxHealth)
            : 1f;

        maxHealth = unit.unitData.health;

        if (refill || currentHealth <= 0)
            currentHealth = maxHealth;
        else
            currentHealth = Mathf.Clamp(Mathf.RoundToInt(maxHealth * healthRatio), 1, maxHealth);
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
                Mathf.Clamp(
                    Mathf.CeilToInt(
                    (float)currentHealth /
                    maxHealth *
                    formation.maxSoldiers
                    ),
                    0,
                    formation.maxSoldiers
                );

            if (currentSoldierCount <
                lastSoldierCount)
            {
                while (lastSoldierCount > currentSoldierCount)
                {
                    formation.KillSoldier();
                    lastSoldierCount--;
                }
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

