using UnityEngine;
using System;

public class IncomeManager : MonoBehaviour
{
    public static IncomeManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("How often in seconds the income is collected.")]
    public float collectionInterval = 1.0f;

    [Header("References")]
    public GridManager gridManager;

    // Event for UI to listen for income updates
    public static event Action<int> OnIncomeCollected;

    private float _timer;
    // Buffer to hold decimal remainders between ticks so we don't lose value
    private float _uncollectedDecimals = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (gridManager == null) return;

        _timer += Time.deltaTime;
        if (_timer >= collectionInterval)
        {
            CollectIncome();
            _timer = 0f;
        }
    }

    private void CollectIncome()
    {
        float totalIncome = 0f;

        // Iterate through all slots on the board
        var allSlots = gridManager.GetAllSlots();
        if (allSlots == null) return;

        foreach (var slot in allSlots)
        {
            if (slot != null && !slot.IsEmpty && slot.CurrentCrop != null)
            {
                // In our math: coinPerTick is interpreted as "income per second"
                totalIncome += slot.CurrentCrop.coinPerTick;
            }
        }

        if (totalIncome > 0)
        {
            // Add previous remainder
            totalIncome += _uncollectedDecimals;
            
            // Floor down to get integer coins to add to CurrencyManager
            int incomeAsInt = Mathf.FloorToInt(totalIncome);
            
            // Save remaining decimals for the next tick
            _uncollectedDecimals = totalIncome - incomeAsInt;

            if (incomeAsInt > 0)
            {
                CurrencyManager.Instance.AddCoin(incomeAsInt);
                // Trigger event for visual feedback (e.g. UIManager)
                OnIncomeCollected?.Invoke(incomeAsInt);
                Debug.Log($"Pasif gelir eklendi: {incomeAsInt}");
            }
        }
    }
}
