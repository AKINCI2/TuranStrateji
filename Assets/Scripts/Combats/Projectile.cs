using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public float speed = 4f;

    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);

            return;
        }

        // 🔥 Hedefe yön
        Vector3 dir =
            (target.position - transform.position)
            .normalized;

        // 🔥 Hareket
        transform.position +=
            dir * speed * Time.deltaTime;

        // 🔥 Hedefe ulaştı mı
        float dist =
            Vector3.Distance(
                transform.position,
                target.position
            );

        if (dist < 0.05f)
        {
            // 🔥 Hit effect
            if (hitEffectPrefab != null)
            {
                Instantiate(
                    hitEffectPrefab,
                    transform.position,
                    Quaternion.identity
                );
            }

            Destroy(gameObject);
        }
    }
}
