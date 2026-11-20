using UnityEngine;

public class GunHolder : MonoBehaviour
{
    [Header("Drag these in")]
    public GameObject heldGunModel;          // the Pistol under your Main Camera
    public ProjectileGun projectileGun;      // your ProjectileGun script (the object named ProjectileGun)
    public GameObject[] showWhenHeld;        // optional: ammo text, crosshair, etc. that should only show with a gun

    [Header("Start")]
    public bool startWithGun = false;        // tick this to start every scene already holding the gun

    public bool HasGun { get; private set; }

    void Start()
    {
        SetHasGun(startWithGun);
    }

    public void PickUp()
    {
        SetHasGun(true);
    }

    void SetHasGun(bool value)
    {
        HasGun = value;

        if (heldGunModel != null) heldGunModel.SetActive(value);
        if (projectileGun != null) projectileGun.enabled = value;

        foreach (GameObject g in showWhenHeld)
            if (g != null) g.SetActive(value);
    }
}