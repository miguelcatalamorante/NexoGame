using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerResourcesUI : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerResources playerResources;          // estamina / hambre / sed
    public InventarioArmasJugador inventarioArmas;  // para munición y recarga

    [Header("Ammo UI")]
    public TextMeshProUGUI ammoLabel;          // "30 / 90"
    public Image reloadProgressImage;          // barra/círculo de recarga

    [Header("Stamina UI")]
    public Image staminaBar;

    [Header("Hambre / Sed UI")]
    public Image hungerBar;
    public Image thirstBar;
    public TextMeshProUGUI hungerLabel;
    public TextMeshProUGUI thirstLabel;

    // Referencia interna al arma a la que estamos suscritos
    private ArmaDeFuego armaSuscrita;

    void OnEnable()
    {
        // Suscribir a PlayerResources (estamina, hambre, sed)
        if (playerResources != null)
        {
            playerResources.OnStaminaChanged += HandleStamina;
            playerResources.OnHungerChanged += HandleHunger;
            playerResources.OnThirstChanged += HandleThirst;
        }

        // Suscribir a cambios de arma
        if (inventarioArmas != null)
        {
            inventarioArmas.OnWeaponChanged += OnWeaponChanged;
        }

        // Suscribirnos al arma activa actual (si hay)
        SuscribirAArmaActual();
    }

    void OnDisable()
    {
        if (playerResources != null)
        {
            playerResources.OnStaminaChanged -= HandleStamina;
            playerResources.OnHungerChanged -= HandleHunger;
            playerResources.OnThirstChanged -= HandleThirst;
        }

        if (inventarioArmas != null)
        {
            inventarioArmas.OnWeaponChanged -= OnWeaponChanged;
        }

        DesuscribirDeArmaActual();
    }

    void Start()
    {
        // Inicializar UI de resources
        if (playerResources != null)
        {
            HandleStamina(playerResources.stamina, playerResources.staminaMax);
            HandleHunger(playerResources.hunger, playerResources.hungerMax);
            HandleThirst(playerResources.thirst, playerResources.thirstMax);
        }

        // Inicializar barra de recarga
        if (reloadProgressImage != null)
        {
            reloadProgressImage.fillAmount = 0f;
            reloadProgressImage.gameObject.SetActive(false);
        }

        // Inicializar ammo del arma activa
        SuscribirAArmaActual();
    }

    //  Gestión de arma / munición 

    void OnWeaponChanged()
    {
        SuscribirAArmaActual();
    }

    void SuscribirAArmaActual()
    {
        // Quitar suscripción previa
        DesuscribirDeArmaActual();

        if (inventarioArmas == null) return;

        var arma = inventarioArmas.ObtenerArmaActiva();
        if (arma == null) return;

        armaSuscrita = arma;
        armaSuscrita.OnAmmoChanged += HandleAmmo;
        armaSuscrita.OnReloadProgress += HandleReloadProgress;

        // Actualizar UI con los datos actuales del arma
        HandleAmmo(armaSuscrita.balasEnCargador, armaSuscrita.tamañoCargador, armaSuscrita.balasEnReserva);
        HandleReloadProgress(0f);
    }

    void DesuscribirDeArmaActual()
    {
        if (armaSuscrita != null)
        {
            armaSuscrita.OnAmmoChanged -= HandleAmmo;
            armaSuscrita.OnReloadProgress -= HandleReloadProgress;
            armaSuscrita = null;
        }
    }

    void HandleAmmo(int current, int magSize, int reserve)
    {
        if (ammoLabel)
            ammoLabel.text = $"{current} / {reserve}";
    }

    void HandleReloadProgress(float progress)
    {
        if (reloadProgressImage == null) return;

        reloadProgressImage.fillAmount = Mathf.Clamp01(progress);

        // Mostrar solo mientras recarga (entre 0 y 1)
        bool show = progress > 0f && progress < 1f;
        reloadProgressImage.gameObject.SetActive(show);
    }

    // Stamina / hambre / sed 

    void HandleStamina(float current, float max)
    {
        if (staminaBar)
            staminaBar.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    void HandleHunger(float current, float max)
    {
        if (hungerBar)
            hungerBar.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (hungerLabel)
            hungerLabel.text = $"{Mathf.RoundToInt(current)}";
    }

    void HandleThirst(float current, float max)
    {
        if (thirstBar)
            thirstBar.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (thirstLabel)
            thirstLabel.text = $"{Mathf.RoundToInt(current)}";
    }
}
