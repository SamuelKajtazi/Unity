using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelExit : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load")]
    public string nextSceneName = "Level2";

    private bool triggered = false;

    void Reset()
    {
        // makes the collider a trigger automatically when you add the script
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!IsPlayer(other)) return;

        triggered = true;
        Time.timeScale = 1f;   // in case the game was slowed or paused

        SceneManager.LoadScene(nextSceneName);
    }

    // works for the Rigidbody player: checks the Rigidbody's object, its parents, then the tag
    bool IsPlayer(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;

        if (body != null && body.GetComponent<PlayerMovement>() != null) return true;
        if (other.GetComponentInParent<PlayerMovement>() != null) return true;

        return other.CompareTag("Player") || (body != null && body.CompareTag("Player"));
    }
}