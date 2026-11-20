using UnityEngine;

public class GunPickup : MonoBehaviour
{
    public KeyCode pickupKey = KeyCode.E;
    public float pickupRange = 3f;
    public GameObject promptUI;              // optional: a "Press E" text object, can stay empty
    public bool spinAndBob = true;           // makes the gun rotate and float so it's easy to spot
    public float spinSpeed = 60f;
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;

    GunHolder holder;
    Vector3 startPos;

    void Start()
    {
        holder = FindFirstObjectByType<GunHolder>();
        startPos = transform.position;

        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        if (spinAndBob)
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        if (holder == null || holder.HasGun) return;

        bool inRange = Vector3.Distance(holder.transform.position, transform.position) <= pickupRange;

        if (promptUI != null) promptUI.SetActive(inRange);

        if (inRange && Input.GetKeyDown(pickupKey))
        {
            holder.PickUp();
            if (promptUI != null) promptUI.SetActive(false);
            Destroy(gameObject);
        }
    }
}