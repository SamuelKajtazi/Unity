using UnityEngine;
using System.Collections;
using TMPro;

public class ProjectileGun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform muzzle;
    public Camera cam;
    public float bulletSpeed = 100f;

    [Header("Ammo")]
    public int magazineSize = 7;
    public float reloadTime = 1.5f;
    public KeyCode reloadKey = KeyCode.R;
    public TextMeshProUGUI ammoText;   // optional, can stay empty

    int currentAmmo;
    bool isReloading;
    Collider[] ownColliders;
    bool fireNextFrame;

    void Start()
    {
        ownColliders = transform.root.GetComponentsInChildren<Collider>();
        currentAmmo = magazineSize;
        UpdateAmmoUI();
    }

    void Update()
    {
        if (PauseMenu.GameIsPaused || Time.timeScale == 0f) return;
        if (isReloading) return;

        if ((Input.GetKeyDown(reloadKey) && currentAmmo < magazineSize) || currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Input.GetMouseButtonDown(0))
            fireNextFrame = true;
    }

    void LateUpdate()
    {
        if (!fireNextFrame) return;
        fireNextFrame = false;

        if (PauseMenu.GameIsPaused || Time.timeScale == 0f || isReloading || currentAmmo <= 0) return;

        Shoot();
    }

    IEnumerator Reload()
    {
        isReloading = true;
        if (ammoText)
        {
            ammoText.text = "Reloading...";
            ammoText.color = Color.white;
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;
        UpdateAmmoUI();
    }

    void Shoot()
    {
        currentAmmo--;
        UpdateAmmoUI();

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 target = ray.GetPoint(200f);
        float closest = Mathf.Infinity;

        foreach (RaycastHit h in Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.transform.root == transform.root) continue;
            if (h.distance < closest)
            {
                closest = h.distance;
                target = h.point;
            }
        }

        Vector3 direction = (target - muzzle.position).normalized;
        if (Vector3.Dot(direction, cam.transform.forward) < 0.1f)
            direction = cam.transform.forward;

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

    void UpdateAmmoUI()
    {
        if (!ammoText) return;
        ammoText.text = currentAmmo + " / " + magazineSize;
        ammoText.color = currentAmmo <= 2 ? Color.red : Color.white;
    }
}