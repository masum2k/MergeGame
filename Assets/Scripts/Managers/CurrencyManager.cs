using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    // Singleton instance
    public static CurrencyManager Instance { get; private set; }

    // Current coin amount
    public int Coin { get; private set; }

    // Event triggered when coin amount changes
    public event Action<int> OnCoinChanged;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Give 100 coins at start for testing purposes
        AddCoin(100);
    }

    /// <summary>
    /// Adds coins to the balance.
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void AddCoin(int amount)
    {
        if (amount < 0) return;

        Coin += amount;
        OnCoinChanged?.Invoke(Coin);
    }

    /// <summary>
    /// Spends coins if there's enough balance.
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if spending was successful, False otherwise (not enough coins)</returns>
    public bool SpendCoin(int amount)
    {
        if (amount < 0) return false;

        if (Coin >= amount)
        {
            Coin -= amount;
            OnCoinChanged?.Invoke(Coin);
            return true;
        }

        return false;
    }
}
