using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject panelRoot;            // Panel contenedor Game Over
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;
    
    [Header("UI a desactivar cuando muere")]
    [SerializeField] private GameObject interfazJuego;  // Canvas con vida, munición, hambre, etc
    
    [Header("Escena del Menú")]
    [SerializeField] private string menuSceneName = "Menu";

    void Start()
    {
        // Panel Game Over inactivo al principio
        if (panelRoot != null)
            panelRoot.SetActive(false);

        // Conectar botones
        if (retryButton != null)
            retryButton.onClick.AddListener(Reintentar);
        if (exitButton != null)
            exitButton.onClick.AddListener(SalirAlMenu);
    }

    // Método llamado desde GameManager cuando muere el jugador
    public void Show(int kills)
    {
        // Desactivar interfaz de juego
        if (interfazJuego != null)
            interfazJuego.SetActive(false);

        // Activar panel Game Over
        if (panelRoot != null)
            panelRoot.SetActive(true);

        // Mostrar kills
        if (scoreLabel != null)
            scoreLabel.text = $"Enemigos eliminados: {kills}";

        // Desbloquear cursor para interactuar con botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Reintentar()
    {
        GameManager.Instance.RestartLevel();
    }

    void SalirAlMenu()
    {
        GameManager.Instance.ExitToMenu(menuSceneName);
    }
}
