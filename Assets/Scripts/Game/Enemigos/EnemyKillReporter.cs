using UnityEngine;

[RequireComponent(typeof(DañoVidaEnemigo))]
public class EnemyKillReporter : MonoBehaviour
{
    private DañoVidaEnemigo enemy;

    private void Awake()
    {
        enemy = GetComponent<DañoVidaEnemigo>();
    }

    private void OnEnable()
    {
        if (enemy != null)
            enemy.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (enemy != null)
            enemy.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterKill();
    }
}
