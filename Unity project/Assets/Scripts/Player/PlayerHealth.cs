using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public Slider healthSlider;          // drag PlayerHealthBar here
    public DeathScreen deathScreen;      // drag the object that has the DeathScreen script
    public float smoothSpeed = 5f;

    float currentHealth;
    bool isDead;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }
    }

    void Update()
    {
        if (!healthSlider) return;
        float target = currentHealth / maxHealth;
        healthSlider.value = Mathf.MoveTowards(healthSlider.value, target, smoothSpeed * Time.unscaledDeltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
    }

    void Die()
    {
        isDead = true;
        if (deathScreen) deathScreen.Show();
    }
}