using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 10f;
    bool hasHit;

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        EnemyHealth enemy = collision.gameObject.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
            enemy.TakeDamage(damage);

        Destroy(gameObject);
    }
}