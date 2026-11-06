using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public Slider healthSlider;       // your Slider
    public Transform healthCanvas;    // the Health canvas
    public float smoothSpeed = 10f;   // set to 0 for instant drops

    [Header("Respawn")]
    public float respawnTime = 5f;    // seconds until the enemy comes back
    public Transform spawnPoint;      // optional: leave empty to respawn where it started

    float currentHealth;
    bool isDead;
    Camera cam;
    Vector3 startPosition;
    Quaternion startRotation;

    Renderer rend;
    Collider col;
    EnemyChase chase;

    void Start()
    {
        currentHealth = maxHealth;
        cam = Camera.main;

        startPosition = transform.position;
        startRotation = transform.rotation;

        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
        chase = GetComponent<EnemyChase>();

        if (healthSlider)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }
    }

    void Update()
    {
        if (isDead || !healthSlider) return;

        float target = currentHealth / maxHealth;

        if (smoothSpeed <= 0f)
            healthSlider.value = target;
        else
            healthSlider.value = Mathf.MoveTowards(healthSlider.value, target, smoothSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (healthCanvas && cam)
            healthCanvas.rotation = cam.transform.rotation;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);

        if (currentHealth <= 0f)
            StartCoroutine(DieAndRespawn());
    }

    IEnumerator DieAndRespawn()
    {
        isDead = true;
        SetAlive(false);

        yield return new WaitForSeconds(respawnTime);

        // move back to the spawn spot
        if (spawnPoint)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
        else
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
        }

        currentHealth = maxHealth;
        if (healthSlider) healthSlider.value = 1f;

        isDead = false;
        SetAlive(true);
    }

    void SetAlive(bool alive)
    {
        if (rend) rend.enabled = alive;
        if (col) col.enabled = alive;
        if (chase) chase.enabled = alive;
        if (healthCanvas) healthCanvas.gameObject.SetActive(alive);
    }
}