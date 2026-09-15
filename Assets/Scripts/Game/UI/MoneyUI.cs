// MoneyUI.cs
using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI moneyLabel;

    void OnEnable()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
    }

    void OnDisable()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
    }

    void Start()
    {
        if (MoneyManager.Instance != null)
            UpdateMoneyDisplay(MoneyManager.Instance.GetMoney());
    }

    void UpdateMoneyDisplay(int money)
    {
        if (moneyLabel != null)
            moneyLabel.text = $"${money}";
    }
}