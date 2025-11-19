using System;
using System.Collections;
using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    // ---- Salud (ya tienes VidaDañoPlayer) ----
    // Este script no sobrescribe vida, pero puede aplicar daño por hambre/sed.

    [Header("Munición (arma)")]
    public int magazineSize = 30;
    public int currentAmmoInMag = 30;
    public int reserveAmmo = 90;
    public float reloadTime = 1.8f;
    public bool infiniteAmmo = false;

    public bool isReloading { get; private set; } = false;

    public event Action<int, int, int> OnAmmoChanged; // currentInMag, magazineSize, reserveAmmo

    // NUEVO: evento para progreso de recarga (0–1)
    public event Action<float> OnReloadProgress;

    // ---- Resistencia / Cansancio ----
    [Header("Stamina / Cansancio")]
    public float staminaMax = 100f;
    public float stamina = 100f;
    public float runDrainPerSecond = 18f;
    public float walkDrainPerSecond = 0f; // (no usado en este setup, lo dejamos por si lo quieres)
    public float staminaRegenPerSecond = 20f;
    public float staminaRegenDelay = 1.0f; // tiempo tras dejar de correr para regenerar

    // NUEVO: umbral mínimo que debe alcanzar para salir del agotamiento
    public float exhaustedRecoverThreshold = 20f;

    private float lastRunTime = -999f;
    private bool isExhausted = false;

    public event Action<float, float> OnStaminaChanged; // current, max

    // ---- Hambre y Sed ----
    [Header("Hambre y Sed")]
    public float hungerMax = 100f;
    public float hunger = 100f;
    public float thirstMax = 100f;
    public float thirst = 100f;
    public float hungerDecayPerSecond = 0.5f;
    public float thirstDecayPerSecond = 0.9f;
    public float starvationDamagePerSecond = 2f; // daño si hunger<=0
    public float dehydrationDamagePerSecond = 3f; // daño si thirst<=0

    public event Action<float, float> OnHungerChanged; // current, max
    public event Action<float, float> OnThirstChanged; // current, max

    [Header("Opciones")]
    public bool applyDamageWhenStarving = true;

    private VidaDañoPlayer vidaPlayer;

    void Start()
    {
        currentAmmoInMag = Mathf.Clamp(currentAmmoInMag, 0, magazineSize);
        stamina = Mathf.Clamp(stamina, 0f, staminaMax);
        hunger = Mathf.Clamp(hunger, 0f, hungerMax);
        thirst = Mathf.Clamp(thirst, 0f, thirstMax);

        vidaPlayer = GetComponent<VidaDañoPlayer>();

        // Notificar estado inicial
        OnAmmoChanged?.Invoke(currentAmmoInMag, magazineSize, reserveAmmo);
        OnStaminaChanged?.Invoke(stamina, staminaMax);
        OnHungerChanged?.Invoke(hunger, hungerMax);
        OnThirstChanged?.Invoke(thirst, thirstMax);

        // Reset barra de recarga al inicio
        OnReloadProgress?.Invoke(0f);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ---- Hambre y sed decaen con el tiempo ----
        hunger = Mathf.Max(0f, hunger - hungerDecayPerSecond * dt);
        thirst = Mathf.Max(0f, thirst - thirstDecayPerSecond * dt);

        OnHungerChanged?.Invoke(hunger, hungerMax);
        OnThirstChanged?.Invoke(thirst, thirstMax);

        // ---- Daño por starvation / dehydration ----
        if (applyDamageWhenStarving && vidaPlayer != null)
        {
            if (hunger <= 0f)
            {
                vidaPlayer.RecibirDaño(starvationDamagePerSecond * dt, gameObject);
            }
            if (thirst <= 0f)
            {
                vidaPlayer.RecibirDaño(dehydrationDamagePerSecond * dt, gameObject);
            }
        }

        // Recarga manual
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(ReloadCoroutine());
        }
    }

    // === Estamina gestionada desde aquí, llamada desde MovimientoPlayer ===
    public void TickStamina(bool isRunning, bool isWalking, float dt)
    {
        // Drenaje si corriendo y no agotado
        if (isRunning && !isExhausted)
        {
            float drain = runDrainPerSecond * dt;
            stamina = Mathf.Max(0f, stamina - drain);
            lastRunTime = Time.time;

            if (stamina <= 0f)
            {
                isExhausted = true; // entra en agotamiento
            }

            OnStaminaChanged?.Invoke(stamina, staminaMax);
            return;
        }

        // Regeneración si NO corriendo (caminando o quieto), tras el delay
        if (Time.time - lastRunTime >= staminaRegenDelay)
        {
            float regen = staminaRegenPerSecond * dt;

            // Si quisieras que andando regenere un poco más lento, descomenta:
            // if (isWalking) regen *= 0.75f;

            if (stamina < staminaMax)
            {
                stamina = Mathf.Min(staminaMax, stamina + regen);
                OnStaminaChanged?.Invoke(stamina, staminaMax);
            }

            // Salimos del agotamiento cuando supera el umbral
            if (isExhausted && stamina >= exhaustedRecoverThreshold)
            {
                isExhausted = false;
            }
        }
    }

    // Exponer estado de agotamiento
    public bool IsExhausted() => isExhausted;

    // ------------------ Munición ------------------
    public bool FireOne()
    {
        if (isReloading) return false;
        if (currentAmmoInMag > 0)
        {
            currentAmmoInMag--;
            OnAmmoChanged?.Invoke(currentAmmoInMag, magazineSize, reserveAmmo);
            return true;
        }
        else
        {
            // auto reload si hay reserva
            if (reserveAmmo > 0 || infiniteAmmo)
            {
                StartCoroutine(ReloadCoroutine());
            }
            return false;
        }
    }

    // === RECARGA CON PROGRESO ===
    public IEnumerator ReloadCoroutine()
    {
        if (isReloading) yield break;
        if (currentAmmoInMag >= magazineSize) yield break;
        if (reserveAmmo <= 0 && !infiniteAmmo) yield break;

        isReloading = true;

        // Al empezar, barra a 0
        OnReloadProgress?.Invoke(0f);

        float timer = 0f;

        // Bucle de recarga con progreso
        while (timer < reloadTime)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / reloadTime);

            OnReloadProgress?.Invoke(progress);

            yield return null;
        }

        // Recarga efectiva
        int needed = magazineSize - currentAmmoInMag;
        int take = infiniteAmmo ? needed : Mathf.Min(needed, reserveAmmo);
        currentAmmoInMag += take;
        if (!infiniteAmmo) reserveAmmo -= take;

        isReloading = false;

        OnAmmoChanged?.Invoke(currentAmmoInMag, magazineSize, reserveAmmo);

        // Al final dejamos la barra llena (1). El UI decidirá si la oculta.
        OnReloadProgress?.Invoke(1f);
    }

    public void AddAmmoToReserve(int amount)
    {
        reserveAmmo = Mathf.Max(0, reserveAmmo + amount);
        OnAmmoChanged?.Invoke(currentAmmoInMag, magazineSize, reserveAmmo);
    }

    // ---- Consumibles: comida y bebida ----
    public void Eat(float amount) // amount suma a hunger
    {
        hunger = Mathf.Clamp(hunger + amount, 0f, hungerMax);
        OnHungerChanged?.Invoke(hunger, hungerMax);
    }

    public void Drink(float amount) // amount suma a thirst
    {
        thirst = Mathf.Clamp(thirst + amount, 0f, thirstMax);
        OnThirstChanged?.Invoke(thirst, thirstMax);
    }
}
