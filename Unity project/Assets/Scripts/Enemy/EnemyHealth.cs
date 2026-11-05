using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public Slider healthSlider;       // drag your Slider here
    public Transform healthCanvas;    // drag the Canvas (Health) here
    public float smoothSpeed = 10f;   // set to 0 for instant drops

    float currentHealth;
    Camera cam;

    void Start()
    {
        currentHealth = maxHealth;
        cam = Camera.main;

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

        if (smoothSpeed <= 0f)
            healthSlider.value = target;
        else
            healthSlider.value = Mathf.MoveTowards(healthSlider.value, target, smoothSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        // keep the bar facing the camera
        if (healthCanvas && cam)
            healthCanvas.rotation = cam.transform.rotation;
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);

        if (currentHealth <= 0f)
            Destroy(gameObject);   // enemy and its bar disappear at 0
    }
}