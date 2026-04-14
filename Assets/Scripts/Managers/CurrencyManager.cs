using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    private const string COIN_KEY = "PlayerCoin";
    private const string GEM_KEY = "PlayerGem";
    private const string LIFETIME_COIN_KEY = "PlayerLifetimeCoin";

    // Singleton instance
    public static CurrencyManager Instance { get; private set; }

    // Current coin amount
    public int Coin { get; private set; }

    // Current gem amount
    public int Gem { get; private set; }

    // Lifetime total earned coin (used for prestige requirements)
    public int LifetimeCoinEarned { get; private set; }

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
        if (!SecurePlayerPrefs.HasKey(COIN_KEY))
        {
            // Give starting values only for first run
            Debug.Log("<color=cyan>[CurrencyManager] First run detected! Giving starter 100 Coins and 10 Gems.</color>");
            AddCoin(100);
            AddGem(10);
        }
    }

    private void Save()
    {
        SecurePlayerPrefs.SetInt(COIN_KEY, Coin);
        SecurePlayerPrefs.SetInt(GEM_KEY, Gem);
        SecurePlayerPrefs.SetInt(LIFETIME_COIN_KEY, LifetimeCoinEarned);
        SaveCoordinator.MarkDirty();
    }

    private void Load()
    {
        Coin = SecurePlayerPrefs.GetInt(COIN_KEY, 0);
        Gem = SecurePlayerPrefs.GetInt(GEM_KEY, 0);
        LifetimeCoinEarned = SecurePlayerPrefs.GetInt(LIFETIME_COIN_KEY, 0);

        ApplyCoinCap();
        
        OnCoinChanged?.Invoke(Coin);
        OnGemChanged?.Invoke(Gem);
    }

    /// <summary>
    /// Adds coins to the balance.
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void AddCoin(int amount)
    {
        if (amount <= 0) return;

        long life = (long)LifetimeCoinEarned + amount;
        LifetimeCoinEarned = life >= int.MaxValue ? int.MaxValue : (int)life;

        int cap = GetCurrentCoinCap();
        long rawCoin = (long)Coin + amount;
        int clamped = (int)Mathf.Min(cap, rawCoin >= int.MaxValue ? int.MaxValue : (int)rawCoin);

        Coin = clamped;
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

    public int GetCurrentCoinCap()
    {
        if (PrestigeManager.Instance == null)
            return int.MaxValue;

        return PrestigeManager.Instance.GetCoinCap();
    }

    public void ApplyCoinCap()
    {
        int cap = GetCurrentCoinCap();
        if (Coin > cap)
        {
            Coin = cap;
        }
    }
}
