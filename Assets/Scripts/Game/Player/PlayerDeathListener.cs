using UnityEngine;

[RequireComponent(typeof(VidaDañoPlayer))]
public class PlayerDeathListener : MonoBehaviour
{
    private VidaDañoPlayer vida;

    void Awake()
    {
        vida = GetComponent<VidaDañoPlayer>();
    }

    void OnEnable()
    {
        if (vida != null)
            vida.OnDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        if (vida != null)
            vida.OnDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();
    }
}
