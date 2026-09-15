// MoneyManager.cs
using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Configuración")]
    public int moneyPerKill = 10;

    private int currentMoney = 0;

    // Evento para notificar a la UI
    public event Action<int> OnMoneyChanged; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // La persistencia de DontDestroyOnLoad ya está en GameManager.
    }

    void Start()
    {
        currentMoney = 0;
        // Suscribirse al GameManager después de que se inicialice.
        
        // 💡 SUSCRIPCIÓN CLAVE
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnKillRegistered += HandleKillRegistered;
        }
        else
        {
            Debug.LogError("MoneyManager: No se encontró la instancia de GameManager para suscribirse.");
        }
        
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // Desuscribirse al destruirse para evitar errores
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnKillRegistered -= HandleKillRegistered;
        }
    }

    // Método que se llama cuando el GameManager registra una muerte
    private void HandleKillRegistered()
    {
        AddMoney(moneyPerKill);
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        Debug.Log($"💰 [MoneyManager] Dinero Añadido: +{amount}. Total Actual: ${currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public int GetMoney() => currentMoney;

    public void ResetMoney()
    {
        currentMoney = 0;
        OnMoneyChanged?.Invoke(currentMoney);
    }
}