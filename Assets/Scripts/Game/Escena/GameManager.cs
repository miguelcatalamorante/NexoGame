using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estado de la partida")]
    [SerializeField] private int killsThisRun = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RegisterKill()
    {
        killsThisRun++;
    }

    public int GetKillsThisRun() => killsThisRun;

    public void ResetRun()
    {
        killsThisRun = 0;
    }

    public void TriggerGameOver()
    {
        Time.timeScale = 0f;

        var ui = FindObjectOfType<GameOverUI>(true);
        if (ui != null)
        {
            ui.Show(killsThisRun);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        ResetRun();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ExitToMenu(string menuSceneName = "Menu")
    {
        Time.timeScale = 1f;
        ResetRun();
        SceneManager.LoadScene(menuSceneName);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
