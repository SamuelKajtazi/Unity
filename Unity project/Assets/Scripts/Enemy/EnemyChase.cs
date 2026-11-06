using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float detectRange = 30f;     // starts chasing inside this distance
    public float attackRange = 1.8f;    // stops and attacks inside this distance
    public float damage = 10f;
    public float attackCooldown = 1f;   // seconds between hits
    public float turnSpeed = 8f;

    PlayerHealth player;
    float nextAttackTime;
    float startY;

    void Start()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        startY = transform.position.y;
    }

    void Update()
    {
        if (player == null) return;
        if (PauseMenu.GameIsPaused) return;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > detectRange) return;

        if (toPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        if (distance > attackRange)
        {
            // walk toward the player
            Vector3 move = toPlayer.normalized * moveSpeed * Time.deltaTime;
            transform.position += move;
            transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        }
        else if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            player.TakeDamage(damage);
        }
    }
}