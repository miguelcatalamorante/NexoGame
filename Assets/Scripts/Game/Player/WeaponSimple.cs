using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerResources))]
public class WeaponSimple : MonoBehaviour
{
    public PlayerResources resources;
    public float fireRate = 10f; // bullets per second
    private float lastFire = 0f;

    void Start()
    {
        if (!resources) resources = GetComponent<PlayerResources>();
    }

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time - lastFire >= 1f / fireRate)
        {
            lastFire = Time.time;
            bool fired = resources.FireOne();
            if (fired)
            {
                // Aquí pondrías efectos VFX/SFX, raycast de bala, recoil, etc.
                Debug.Log("Disparo: remaining in mag = " + resources.currentAmmoInMag);
            }
            else
            {
                // Click vacío o recargando
                Debug.Log("No munición. Recargando o vacío.");
            }
        }

        // recarga manual
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(resources.ReloadCoroutine());
        }
    }
}