using UnityEngine;

public class MoveCamera : MonoBehaviour {

    public Transform player;
    public Vector3 offset = Vector3.zero;   // raise the camera to head height if you want (for example 0, 0.5, 0)

    void LateUpdate() {
        if (player == null) return;
        transform.position = player.position + offset;
    }
}