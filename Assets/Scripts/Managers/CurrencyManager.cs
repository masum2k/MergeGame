using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    // Singleton instance
    public static CurrencyManager Instance { get; private set; }

    // Current coin amount
    public int Coin { get; private set; }

    // Current gem amount
    public int Gem { get; private set; }

    // Event triggered when coin amount changes
    public event Action<int> OnCoinChanged;

    // Event triggered when gem amount changes
    public event Action<int> OnGemChanged;

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
        // Give 10 gems at start for testing
        AddGem(10);
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

    /// <summary>
    /// Adds gems to the balance.
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void AddGem(int amount)
    {
        if (amount < 0) return;

        Gem += amount;
        OnGemChanged?.Invoke(Gem);
    }

    /// <summary>
    /// Spends gems if there's enough balance.
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if spending was successful, False otherwise</returns>
    public bool SpendGem(int amount)
    {
        if (amount < 0) return false;

        if (Gem >= amount)
        {
            Gem -= amount;
            OnGemChanged?.Invoke(Gem);
            return true;
        }

        return false;
    }
}
