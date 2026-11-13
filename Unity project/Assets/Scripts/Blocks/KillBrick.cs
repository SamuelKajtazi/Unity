using UnityEngine;
using UnityEngine.SceneManagement;
 
/// <summary>
/// Put this on a brick/block. When the player touches it, they die.
/// Works with 3D and 2D colliders, solid or trigger.
/// Setup: tag your player "Player", add a Collider (or Collider2D) to the brick.
/// </summary>
public class KillBrick : MonoBehaviour
{
    public enum KillAction { ReloadScene, Respawn, DestroyPlayer }
 
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KillAction action = KillAction.ReloadScene;
    [SerializeField] private float delay = 0f;
 
    [Header("Only used for Respawn")]
    [SerializeField] private Transform respawnPoint;
 
    private bool killing;
 
    // ---------- 3D ----------
    private void OnCollisionEnter(Collision c) { TryKill(c.gameObject); }
    private void OnTriggerEnter(Collider other) { TryKill(other.gameObject); }
 
    // ---------- 2D ----------
    private void OnCollisionEnter2D(Collision2D c) { TryKill(c.gameObject); }
    private void OnTriggerEnter2D(Collider2D other) { TryKill(other.gameObject); }
 
    private void TryKill(GameObject obj)
    {
        if (killing) return;
 
        // Check the object itself or its root (handles child colliders)
        GameObject target = obj.CompareTag(playerTag) ? obj : obj.transform.root.gameObject;
        if (!target.CompareTag(playerTag)) return;
 
        killing = true;
        StartCoroutine(KillRoutine(target));
    }
 
    private System.Collections.IEnumerator KillRoutine(GameObject player)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
 
        switch (action)
        {
            case KillAction.ReloadScene:
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;
 
            case KillAction.Respawn:
                if (respawnPoint != null)
                {
                    player.transform.position = respawnPoint.position;
 
                    // Reset velocity so they don't keep flying
                    var rb = player.GetComponent<Rigidbody>();
                    if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                    var rb2d = player.GetComponent<Rigidbody2D>();
                    if (rb2d != null) { rb2d.linearVelocity = Vector2.zero; rb2d.angularVelocity = 0f; }
                }
                else
                {
                    Debug.LogWarning("KillBrick: no respawnPoint assigned.", this);
                }
                killing = false;
                break;
 
            case KillAction.DestroyPlayer:
                Destroy(player);
                break;
        }
    }
}
 