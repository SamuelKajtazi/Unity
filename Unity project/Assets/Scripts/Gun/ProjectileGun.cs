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
    public TextMeshProUGUI ammoText;   // optional screen text, can stay empty

    [Header("Gun Screen (3D text on the gun)")]
    public TMP_Text gunScreenText;     // drag the 3D TextMeshPro that sits on the gun's screen
    public Color gunScreenColor = new Color(0.2f, 0.6f, 1f);   // normal color (blue)
    public Color gunScreenLowColor = Color.red;                // color when ammo is low
    public int lowAmmoAt = 2;
    public string reloadingText = "...";                       // shown on the gun screen while reloading

    [Header("Recoil")]
    public Transform gunModel;          // the part that kicks (leave empty to use this object)
    public Vector3 recoilKickPosition = new Vector3(0f, 0.01f, -0.06f);   // how far the gun kicks back
    public Vector3 recoilKickRotation = new Vector3(-6f, 0f, 0f);         // how much the gun tilts up
    public float recoilReturnSpeed = 14f;                                 // how fast it returns to normal

    int currentAmmo;
    bool isReloading;
    Collider[] ownColliders;
    bool fireNextFrame;

    Vector3 gunStartPos;
    Quaternion gunStartRot;
    Vector3 recoilPos;
    Vector3 recoilRot;

    void Start()
    {
        if (cam == null) cam = Camera.main;

        // the player and the camera (with the weapons under it) are separate roots now, ignore both
        var list = new System.Collections.Generic.List<Collider>(transform.root.GetComponentsInChildren<Collider>());
        if (cam != null && cam.transform.root != transform.root)
            list.AddRange(cam.transform.root.GetComponentsInChildren<Collider>());
        ownColliders = list.ToArray();

        currentAmmo = magazineSize;
        UpdateAmmoUI();

        if (gunModel == null) gunModel = transform;
        gunStartPos = gunModel.localPosition;
        gunStartRot = gunModel.localRotation;
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
        if (fireNextFrame)
        {
            fireNextFrame = false;

            if (!(PauseMenu.GameIsPaused || Time.timeScale == 0f || isReloading || currentAmmo <= 0))
                Shoot();
        }

        ApplyRecoil();
    }

    void ApplyRecoil()
    {
        if (Time.timeScale == 0f) return;

        float k = 1f - Mathf.Exp(-recoilReturnSpeed * Time.deltaTime);
        recoilPos = Vector3.Lerp(recoilPos, Vector3.zero, k);
        recoilRot = Vector3.Lerp(recoilRot, Vector3.zero, k);

        gunModel.localPosition = gunStartPos + recoilPos;
        gunModel.localRotation = gunStartRot * Quaternion.Euler(recoilRot);
    }

    IEnumerator Reload()
    {
        isReloading = true;
        if (ammoText)
        {
            ammoText.text = "Reloading...";
            ammoText.color = Color.white;
        }
        if (gunScreenText)
        {
            gunScreenText.text = reloadingText;
            gunScreenText.color = gunScreenColor;
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;
        UpdateAmmoUI();
    }

    // true if this object belongs to the player or the camera/weapons
    bool IsOwn(Transform t)
    {
        Transform root = t.root;
        if (root == transform.root) return true;
        if (cam != null && root == cam.transform.root) return true;
        return false;
    }

    void Shoot()
    {
        currentAmmo--;
        UpdateAmmoUI();

        // recoil only happens here, so it never plays while reloading
        recoilPos += recoilKickPosition;
        recoilRot += recoilKickRotation;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 target = ray.GetPoint(200f);
        float closest = Mathf.Infinity;

        foreach (RaycastHit h in Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (IsOwn(h.transform)) continue;
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
        if (bulletCol != null)
        {
            foreach (Collider c in ownColliders)
                if (c != null) Physics.IgnoreCollision(bulletCol, c);
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = direction * bulletSpeed;

        Destroy(bullet, 5f);
    }

    void UpdateAmmoUI()
    {
        if (ammoText)
        {
            ammoText.text = currentAmmo + " / " + magazineSize;
            ammoText.color = currentAmmo <= lowAmmoAt ? Color.red : Color.white;
        }

        if (gunScreenText)
        {
            gunScreenText.text = currentAmmo.ToString();
            gunScreenText.color = currentAmmo <= lowAmmoAt ? gunScreenLowColor : gunScreenColor;
        }
    }
}