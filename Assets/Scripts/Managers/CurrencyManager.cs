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

        Load();
    }

    private void Start()
    {
        if (!PlayerPrefs.HasKey("PlayerCoin"))
        {
            // Give starting values only for first run
            Debug.Log("<color=cyan>[CurrencyManager] First run detected! Giving starter 100 Coins and 10 Gems.</color>");
            AddCoin(100);
            AddGem(10);
        }
    }

    private void Save()
    {
        PlayerPrefs.SetInt("PlayerCoin", Coin);
        PlayerPrefs.SetInt("PlayerGem", Gem);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        Coin = PlayerPrefs.GetInt("PlayerCoin", 0);
        Gem = PlayerPrefs.GetInt("PlayerGem", 0);
        
        OnCoinChanged?.Invoke(Coin);
        OnGemChanged?.Invoke(Gem);
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
        Save();
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
            Save();
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
        Save();
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
            Save();
            return true;
        }

        return false;
    }
}
