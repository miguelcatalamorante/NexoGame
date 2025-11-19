using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalUI : MonoBehaviour
{
    [Header("Escenas")]
    public string nombreEscenaJuego = "Pruebas";

    [Header("Paneles")]
    public GameObject panelPrincipal;
    public GameObject panelOpciones;
    public GameObject panelControles;   // NUEVO
    public GameObject panelCreditos;    // NUEVO

    [Header("Fundido (opcional)")]
    public FundidoEscena fundido;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MostrarSolo(panelPrincipal);
    }

    void MostrarSolo(GameObject panel)
    {
        if (panelPrincipal) panelPrincipal.SetActive(false);
        if (panelOpciones)  panelOpciones.SetActive(false);
        if (panelControles) panelControles.SetActive(false);
        if (panelCreditos)  panelCreditos.SetActive(false);
        if (panel) panel.SetActive(true);
    }

    // --- BOTONES ---
    public void AlPulsarJugar()
    {
        if (!Application.CanStreamedLevelBeLoaded(nombreEscenaJuego))
        {
            Debug.LogError($"La escena '{nombreEscenaJuego}' no está en Build Settings.");
            return;
        }
        if (fundido) fundido.FundirYCargarEscena(nombreEscenaJuego);
        else SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void AlPulsarOpciones()  => MostrarSolo(panelOpciones);
    public void AlPulsarControles() => MostrarSolo(panelControles);   // NUEVO
    public void AlPulsarCreditos()  => MostrarSolo(panelCreditos);    // NUEVO
    public void AlPulsarVolver()    => MostrarSolo(panelPrincipal);

    public void AlPulsarSalir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
