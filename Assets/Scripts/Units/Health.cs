using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public bool debugLogs = false;

    [Header("Feedback")]
    public bool enableHitFeedback = true;
    public float hitPulseScale = 1.06f;
    public float hitPulseSeconds = 0.08f;

    public int currentHealth;

    private int lastSoldierCount;
    private bool currentHealthRestored;
    private Coroutine hitFeedbackRoutine;

    void Start()
    {
        ApplyStatsFromData(!currentHealthRestored);

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

    public void SetCurrentHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        currentHealthRestored = true;
        SyncFormationToHealth();
    }

    private void SyncFormationToHealth()
    {
        FormationController formation =
            GetComponent<FormationController>();

        if (formation == null)
            return;

        formation.RebuildSoldiersFromChildren(true);

        int currentSoldierCount =
            Mathf.Clamp(
                Mathf.CeilToInt(
                    (float)currentHealth /
                    Mathf.Max(1, maxHealth) *
                    formation.maxSoldiers
                ),
                0,
                formation.maxSoldiers
            );

        formation.SetVisibleSoldierCount(currentSoldierCount);
        lastSoldierCount = currentSoldierCount;
    }

    // =================================================
    // DAMAGE
    // =================================================

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        currentHealth -=
            Mathf.RoundToInt(damage);

        PlayHitFeedback();

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

    private void PlayHitFeedback()
    {
        if (!enableHitFeedback || !gameObject.activeInHierarchy)
            return;

        if (hitFeedbackRoutine != null)
            StopCoroutine(hitFeedbackRoutine);

        hitFeedbackRoutine = StartCoroutine(HitFeedbackRoutine());
    }

    private System.Collections.IEnumerator HitFeedbackRoutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * Mathf.Max(1f, hitPulseScale);
        float duration = Mathf.Max(0.02f, hitPulseSeconds);
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        hitFeedbackRoutine = null;
    }
}
