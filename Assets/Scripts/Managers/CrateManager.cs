using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages crate purchases. Handles multiple crate types, daily timer, and rewards.
/// </summary>
public class CrateManager : MonoBehaviour
{
    public static CrateManager Instance { get; private set; }

    [Header("Chest Definitions (Set by Setup)")]
    public List<CrateData> AllCrates = new List<CrateData>();

    public event Action<object> OnCrateOpened; // Passes either CropData or BoostData

    private const string DAILY_CHEST_KEY = "LastDailyChestTime";

    private void Awake()
    {
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

    public bool IsDailyChestAvailable()
    {
        if (!PlayerPrefs.HasKey(DAILY_CHEST_KEY)) return true;

        string lastTimeStr = PlayerPrefs.GetString(DAILY_CHEST_KEY);
        DateTime lastTime = DateTime.FromBinary(Convert.ToInt64(lastTimeStr));
        return (DateTime.UtcNow - lastTime).TotalHours >= 12;
    }

    public TimeSpan GetTimeUntilDailyChest()
    {
        if (!PlayerPrefs.HasKey(DAILY_CHEST_KEY)) return TimeSpan.Zero;

        string lastTimeStr = PlayerPrefs.GetString(DAILY_CHEST_KEY);
        DateTime lastTime = DateTime.FromBinary(Convert.ToInt64(lastTimeStr));
        TimeSpan elapsed = DateTime.UtcNow - lastTime;
        TimeSpan wait = TimeSpan.FromHours(12) - elapsed;
        return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
    }

    public object OpenCrate(CrateData crate)
    {
        if (crate == null) return null;

        // Special handling for Daily Chest
        if (crate.rarity == CrateRarity.Daily)
        {
            if (!IsDailyChestAvailable())
            {
                Debug.LogWarning("Daily chest not ready yet!");
                return null;
            }
            PlayerPrefs.SetString(DAILY_CHEST_KEY, DateTime.UtcNow.ToBinary().ToString());
        }
        else
        {
            // Currency Check
            if (crate.currencyType == CurrencyType.Coin)
            {
                if (!CurrencyManager.Instance.SpendCoin(crate.cost)) return null;
            }
            else
            {
                if (!CurrencyManager.Instance.SpendGem(crate.cost)) return null;
            }
        }

        // Roll Drop
        object reward = crate.RollDrop();
        if (reward == null) return null;

        // Give Reward
        if (reward is CropData crop)
        {
            InventoryManager.Instance.AddItem(crop);
            // XP Reward
            if (LevelManager.Instance != null)
            {
                // Basic XP for opening a chest
                float baseXP = (crate.rarity == CrateRarity.Daily) ? 10 : (crate.cost * 0.5f);
                LevelManager.Instance.AddXP(baseXP);
            }
        }
        else if (reward is BoostData boost)
        {
            InventoryManager.Instance.AddItem(boost);
            Debug.Log($"Dropped Boost: {boost.itemName}");
        }

        OnCrateOpened?.Invoke(reward);
        return reward;
    }
}
