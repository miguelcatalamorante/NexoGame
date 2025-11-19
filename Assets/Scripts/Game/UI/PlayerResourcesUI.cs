using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerResourcesUI : MonoBehaviour
{
    [Header("Target")]
    public PlayerResources playerResources;

    [Header("Ammo UI")]
    public TextMeshProUGUI ammoLabel; // e.g. "30 / 90"
    public Image reloadProgressImage; // barra/círculo de recarga

    [Header("Stamina UI")]
    public Image staminaBar;

    [Header("Hunger / Thirst UI")]
    public Image hungerBar;
    public Image thirstBar;
    public TextMeshProUGUI hungerLabel;
    public TextMeshProUGUI thirstLabel;

    void OnEnable()
    {
        if (playerResources == null) return;

        playerResources.OnAmmoChanged += HandleAmmo;
        playerResources.OnStaminaChanged += HandleStamina;
        playerResources.OnHungerChanged += HandleHunger;
        playerResources.OnThirstChanged += HandleThirst;
        playerResources.OnReloadProgress += HandleReloadProgress;
    }

    void OnDisable()
    {
        if (playerResources == null) return;

        playerResources.OnAmmoChanged -= HandleAmmo;
        playerResources.OnStaminaChanged -= HandleStamina;
        playerResources.OnHungerChanged -= HandleHunger;
        playerResources.OnThirstChanged -= HandleThirst;
        playerResources.OnReloadProgress -= HandleReloadProgress;
    }

    void Start()
    {
        if (playerResources != null)
        {
            HandleAmmo(playerResources.currentAmmoInMag, playerResources.magazineSize, playerResources.reserveAmmo);
            HandleStamina(playerResources.stamina, playerResources.staminaMax);
            HandleHunger(playerResources.hunger, playerResources.hungerMax);
            HandleThirst(playerResources.thirst, playerResources.thirstMax);
        }

        // Barra de recarga oculta al inicio
        if (reloadProgressImage != null)
        {
            reloadProgressImage.fillAmount = 0f;
            reloadProgressImage.gameObject.SetActive(false);
        }
    }

    void HandleAmmo(int current, int magSize, int reserve)
    {
        if (ammoLabel) ammoLabel.text = $"{current} / {reserve}";
    }

    void HandleStamina(float current, float max)
    {
        if (staminaBar) staminaBar.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    void HandleHunger(float current, float max)
    {
        if (hungerBar) hungerBar.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (hungerLabel) hungerLabel.text = $"{Mathf.RoundToInt(current)}";
    }

    void HandleThirst(float current, float max)
    {
        if (thirstBar) thirstBar.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (thirstLabel) thirstLabel.text = $"{Mathf.RoundToInt(current)}";
    }

    // === NUEVO: progreso de recarga ===
    void HandleReloadProgress(float progress)
    {
        if (reloadProgressImage == null) return;

        reloadProgressImage.fillAmount = Mathf.Clamp01(progress);

        // Mostrar solo mientras está recargando
        bool show = progress > 0f && progress < 1f;
        reloadProgressImage.gameObject.SetActive(show);
    }
}
