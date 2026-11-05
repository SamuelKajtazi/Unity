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
        {
            enemy.TakeDamage(damage);
            Debug.Log("Hit enemy for " + damage);
        }
        else
        {
            Debug.Log("Bullet hit " + collision.gameObject.name + " (no EnemyHealth found)");
        }

        Destroy(gameObject);
    }
}