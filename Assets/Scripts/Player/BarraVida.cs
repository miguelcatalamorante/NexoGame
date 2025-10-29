using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private VidaDañoPlayer target;       // Arrastra aquí el script del jugador
    [SerializeField] private Image fillImage;             // La imagen que se rellena (tipo Filled)
    [SerializeField] private TextMeshProUGUI label;       // El texto opcional (por ejemplo: "75 / 100")

    [Header("Configuración")]
    [SerializeField] private float smoothSpeed = 8f;      // Velocidad de interpolación del relleno

    private float targetFill = 1f;

    private void OnEnable()
    {
        if (target != null)
            target.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (target != null)
            target.OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        if (target != null)
            HandleHealthChanged(target.vidaActual, target.vidaMaxima);
    }

    private void HandleHealthChanged(float current, float max)
    {
        // Calcula el porcentaje de relleno
        targetFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // Actualiza el texto con valores enteros
        if (label != null)
        {
            int vidaInt = Mathf.RoundToInt(current);
            int maxInt = Mathf.RoundToInt(max);
            label.text = $"{vidaInt} / {maxInt}";
        }
    }

    private void Update()
    {
        if (!fillImage) return;

        // Interpolación suave entre el valor actual y el objetivo
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);

        // Color dinámico (verde cuando tiene vida alta, rojo cuando baja)
        fillImage.color = Color.Lerp(Color.red, Color.green, fillImage.fillAmount);
    }
}
