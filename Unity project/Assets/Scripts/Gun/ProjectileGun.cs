using UnityEngine;

public class ProjectileGun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform muzzle;
    public Camera cam;
    public float bulletSpeed = 100f;

    Collider[] ownColliders;
    bool fireNextFrame;

    void Start()
    {
        ownColliders = transform.root.GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            fireNextFrame = true;
    }

    void LateUpdate()
    {
        if (!fireNextFrame) return;
        fireNextFrame = false;
        Shoot();
    }

    void Shoot()
    {
        // 1. Find the point at the center of the screen
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 target = ray.GetPoint(200f);
        float closest = Mathf.Infinity;

        foreach (RaycastHit h in Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.transform.root == transform.root) continue; // skip yourself
            if (h.distance < closest)
            {
                closest = h.distance;
                target = h.point;
            }
        }

        // 2. Direction from the muzzle to that point
        Vector3 direction = (target - muzzle.position).normalized;

        // if the target is behind or beside the muzzle, shoot straight ahead
        if (Vector3.Dot(direction, cam.transform.forward) < 0.1f)
            direction = cam.transform.forward;

        // 3. Spawn and launch the bullet
        GameObject bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(direction));

        Collider bulletCol = bullet.GetComponent<Collider>();
        foreach (Collider c in ownColliders)
            Physics.IgnoreCollision(bulletCol, c);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = direction * bulletSpeed;

        Destroy(bullet, 5f);
    }
}