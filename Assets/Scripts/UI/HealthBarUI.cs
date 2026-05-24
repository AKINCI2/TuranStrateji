using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Image fillImage;

    private Health health;

    void Start()
    {
        health =
            GetComponentInParent<Health>();
    }

    void Update()
    {
        if (health == null)
            return;

        fillImage.fillAmount =
            (float)health.currentHealth /
            health.maxHealth;

        // 🔥 Kamera yönüne bak
        transform.forward =
            Camera.main.transform.forward;
    }
}
