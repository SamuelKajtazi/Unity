using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    float currentHealth;

    void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log(name + " HP: " + currentHealth);

        if (currentHealth <= 0f)
            Destroy(gameObject);
    }
}