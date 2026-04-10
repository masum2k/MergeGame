using UnityEngine;

public class IncomeManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How often in seconds the income is collected.")]
    public float collectionInterval = 10f;

    [Header("References")]
    public GridManager gridManager;

    private float _timer;
    // Buffer to hold decimal remainders between ticks so we don't lose value
    private float _uncollectedDecimals = 0f;

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
        foreach (var slot in gridManager.GetAllSlots())
        {
            if (!slot.IsEmpty && slot.CurrentCrop != null)
            {
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
                Debug.Log($"Pasif gelir eklendi: {incomeAsInt}");
            }
        }
    }
}
