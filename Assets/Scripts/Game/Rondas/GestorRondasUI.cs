using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GestorRondasUI : MonoBehaviour
{
    [Header("Referencias")]
    public GestorRondas gestor;                     // si no se asigna se busca con FindObjectOfType
    public TextMeshProUGUI textoRondaTMP;
    public TextMeshProUGUI textoEnemigosTMP;
    public TextMeshProUGUI textoIntermedioTMP;

    public Text textoRonda;
    public Text textoEnemigos;
    public Text textoIntermedio;

    void OnEnable()
    {
        if (gestor == null) gestor = FindObjectOfType<GestorRondas>();
        if (gestor == null) return;

        gestor.OnRondaCambiada += ActualizarRonda;
        gestor.OnCuentaEnemigosCambiada += ActualizarEnemigos;
        gestor.OnIntermedioTick += ActualizarIntermedio;

        // inicializar valores visibles
        ActualizarRonda(gestor.GetRondaActual());
        ActualizarEnemigos(gestor.GetVivosEstaRonda(), gestor.GetTotalEstaRonda());
        ActualizarIntermedio(0f);
    }

    void OnDisable()
    {
        if (gestor == null) return;
        gestor.OnRondaCambiada -= ActualizarRonda;
        gestor.OnCuentaEnemigosCambiada -= ActualizarEnemigos;
        gestor.OnIntermedioTick -= ActualizarIntermedio;
    }

    void ActualizarRonda(int numero)
    {
        string t = $"{numero}";
        if (textoRondaTMP != null) textoRondaTMP.text = t;
        if (textoRonda != null) textoRonda.text = t;
    }

    void ActualizarEnemigos(int vivos, int total)
    {
        string t = $"{vivos}";
        if (textoEnemigosTMP != null) textoEnemigosTMP.text = t;
        if (textoEnemigos != null) textoEnemigos.text = t;
    }

    void ActualizarIntermedio(float segundos)
    {
        bool mostrar = segundos > 0f;
        string t = mostrar ? $"Siguiente ronda en {Mathf.CeilToInt(segundos)}s" : "";

        if (textoIntermedioTMP != null)
        {
            textoIntermedioTMP.gameObject.SetActive(mostrar);
            textoIntermedioTMP.text = t;
        }
        if (textoIntermedio != null)
        {
            textoIntermedio.gameObject.SetActive(mostrar);
            textoIntermedio.text = t;
        }
    }
}
