using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalUI : MonoBehaviour
{
    [Header("Escenas")]
    public string nombreEscenaJuego = "Pruebas";

    [Header("Paneles")]
    public GameObject panelPrincipal;
    public GameObject panelOpciones;
    public GameObject panelControles;
    public GameObject panelCreditos;

    [Header("Fundido (opcional)")]
    public FundidoEscena fundido;

    [Header("Sonido")]
    // Referencia al componente Audio Source en este mismo objeto
    public AudioSource audioSource;
    // Referencia al Audio Clip que suena al hacer click
    public AudioClip sonidoClick; 

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

    // Método privado para reproducir el sonido de clic
    void ReproducirClick()
    {
        // Solo reproduce si tenemos ambos componentes asignados
        if (audioSource != null && sonidoClick != null)
        {
            // PlayOneShot evita que se corte el sonido si se pulsa rápidamente
            audioSource.PlayOneShot(sonidoClick);
        }
    }

    // --- BOTONES ---
    
    public void AlPulsarJugar()
    {
        ReproducirClick(); // Llama al sonido
        
        if (!Application.CanStreamedLevelBeLoaded(nombreEscenaJuego))
        {
            Debug.LogError($"La escena '{nombreEscenaJuego}' no está en Build Settings.");
            return;
        }
        
        if (fundido) fundido.FundirYCargarEscena(nombreEscenaJuego);
        else SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void AlPulsarOpciones()
    {
        ReproducirClick(); // Llama al sonido
        MostrarSolo(panelOpciones);
    }

    public void AlPulsarControles()
    {
        ReproducirClick(); // Llama al sonido
        MostrarSolo(panelControles);
    }
    
    public void AlPulsarCreditos()
    {
        ReproducirClick(); // Llama al sonido
        MostrarSolo(panelCreditos);
    }
    
    public void AlPulsarVolver()
    {
        ReproducirClick(); // Llama al sonido
        MostrarSolo(panelPrincipal);
    }

    public void AlPulsarSalir()
    {
        ReproducirClick(); // Llama al sonido
        
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}