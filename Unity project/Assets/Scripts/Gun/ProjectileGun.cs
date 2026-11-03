using UnityEngine;

public class ProjectileGun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform muzzle;
    public float bulletSpeed = 100f;

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            GameObject bullet = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
            bullet.GetComponent<Rigidbody>().useGravity = false;
            bullet.GetComponent<Rigidbody>().linearVelocity = muzzle.forward * bulletSpeed;
            Destroy(bullet, 5f);
        }
    }
}