using UnityEngine;
using UnityEngine.UI;

public class MenuOpciones : MonoBehaviour
{
    [Header("Referencias UI")]
    public Slider deslizadorVolumen;
    public Slider deslizadorSensibilidad;
    public Toggle togglePantallaCompleta;
    public Dropdown listaResoluciones;

    [Header("Valores por defecto")]
    public float volumenPorDefecto = 0.8f;
    public float sensibilidadPorDefecto = 300f;

    private Resolution[] resoluciones;

    // Claves para guardar los valores
    private const string VOL_CLAVE = "op_volumen";
    private const string SENS_CLAVE = "op_sensibilidad";
    private const string FULL_CLAVE = "op_pantalla_completa";
    private const string RESX_CLAVE = "op_res_x";
    private const string RESY_CLAVE = "op_res_y";
    private const string RESF_CLAVE = "op_res_f";

    private void Awake()
    {
        // Poblar la lista de resoluciones
        if (listaResoluciones)
        {
            resoluciones = Screen.resolutions;
            listaResoluciones.ClearOptions();
            int indiceActual = 0;

            var opciones = new System.Collections.Generic.List<string>();
            for (int i = 0; i < resoluciones.Length; i++)
            {
                string texto = $"{resoluciones[i].width} x {resoluciones[i].height} @ {resoluciones[i].refreshRate}Hz";
                opciones.Add(texto);

                if (resoluciones[i].width == Screen.currentResolution.width &&
                    resoluciones[i].height == Screen.currentResolution.height &&
                    resoluciones[i].refreshRate == Screen.currentResolution.refreshRate)
                {
                    indiceActual = i;
                }
            }
            listaResoluciones.AddOptions(opciones);

            // Cargar la resolución guardada
            int guardadaX = PlayerPrefs.GetInt(RESX_CLAVE, Screen.currentResolution.width);
            int guardadaY = PlayerPrefs.GetInt(RESY_CLAVE, Screen.currentResolution.height);
            int guardadaF = PlayerPrefs.GetInt(RESF_CLAVE, Screen.currentResolution.refreshRate);
            int indiceGuardado = indiceActual;

            for (int i = 0; i < resoluciones.Length; i++)
            {
                if (resoluciones[i].width == guardadaX &&
                    resoluciones[i].height == guardadaY &&
                    resoluciones[i].refreshRate == guardadaF)
                {
                    indiceGuardado = i;
                    break;
                }
            }

            listaResoluciones.value = indiceGuardado;
            listaResoluciones.RefreshShownValue();
        }
    }

    private void Start()
    {
        // Volumen
        float vol = PlayerPrefs.GetFloat(VOL_CLAVE, volumenPorDefecto);
        if (deslizadorVolumen) deslizadorVolumen.value = vol;
        AudioListener.volume = vol;

        // Sensibilidad
        float sens = PlayerPrefs.GetFloat(SENS_CLAVE, sensibilidadPorDefecto);
        if (deslizadorSensibilidad) deslizadorSensibilidad.value = sens;

        // Pantalla completa
        bool fs = PlayerPrefs.GetInt(FULL_CLAVE, Screen.fullScreen ? 1 : 0) == 1;
        if (togglePantallaCompleta) togglePantallaCompleta.isOn = fs;
        Screen.fullScreen = fs;
    }

    // ---- Callbacks de UI ----
    public void AlCambiarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(VOL_CLAVE, valor);
    }

    public void AlCambiarSensibilidad(float valor)
    {
        PlayerPrefs.SetFloat(SENS_CLAVE, valor);
        // Tu script MovimientoPlayer puede leerlo al iniciar.
    }

    public void AlCambiarPantallaCompleta(bool activo)
    {
        Screen.fullScreen = activo;
        PlayerPrefs.SetInt(FULL_CLAVE, activo ? 1 : 0);
    }

    public void AlCambiarResolucion(int indice)
    {
        if (resoluciones == null || resoluciones.Length == 0) return;
        var r = resoluciones[indice];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen, r.refreshRate);

        PlayerPrefs.SetInt(RESX_CLAVE, r.width);
        PlayerPrefs.SetInt(RESY_CLAVE, r.height);
        PlayerPrefs.SetInt(RESF_CLAVE, r.refreshRate);
    }
}
