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
    public event Action OnUnlockedCropsChanged;

    private const string DAILY_CHEST_KEY = "LastDailyChestTime";
    private const string UNLOCKED_CROPS_KEY = "UnlockedCrops";

    private readonly HashSet<string> _unlockedCropNames = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadUnlockedCrops();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        BootstrapUnlocksFromInventory();
    }

    public bool IsCropUnlocked(string cropName)
    {
        if (string.IsNullOrWhiteSpace(cropName))
            return false;

        return _unlockedCropNames.Contains(cropName);
    }

    public IReadOnlyCollection<string> GetUnlockedCropNames()
    {
        return _unlockedCropNames;
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
        List<object> rewards = OpenCrates(crate, 1);
        return rewards.Count > 0 ? rewards[0] : null;
    }

    public List<object> OpenCrates(CrateData crate, int count, int overrideUnitCost = -1)
    {
        List<object> rewards = new List<object>();
        if (crate == null || count <= 0) return rewards;

        // Special handling for Daily Chest
        if (crate.rarity == CrateRarity.Daily)
        {
            if (!IsDailyChestAvailable())
            {
                Debug.LogWarning("Daily chest not ready yet!");
                return rewards;
            }

            PlayerPrefs.SetString(DAILY_CHEST_KEY, DateTime.UtcNow.ToBinary().ToString());

            object dailyReward = crate.RollDrop();
            if (dailyReward != null)
            {
                GrantCrateReward(crate, dailyReward);
                rewards.Add(dailyReward);
            }

            return rewards;
        }

        int safeCount = Mathf.Clamp(count, 1, 100);
        int unitCost = overrideUnitCost > 0 ? overrideUnitCost : crate.cost;
        long totalCostLong = (long)unitCost * safeCount;
        if (totalCostLong > int.MaxValue)
            return rewards;

        int totalCost = (int)totalCostLong;

        if (crate.currencyType == CurrencyType.Coin)
        {
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendCoin(totalCost))
                return rewards;
        }
        else
        {
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendGem(totalCost))
                return rewards;
        }

        for (int i = 0; i < safeCount; i++)
        {
            object reward = crate.RollDrop();
            if (reward == null) continue;

            GrantCrateReward(crate, reward);
            rewards.Add(reward);
        }

        return rewards;
    }

    private void GrantCrateReward(CrateData crate, object reward)
    {
        if (reward is CropData crop)
        {
            InventoryManager.Instance.AddItem(crop);
            MarkCropUnlocked(crop.itemName);
            if (LevelManager.Instance != null)
            {
                float baseXP = (crate.rarity == CrateRarity.Daily) ? 10f : Mathf.Max(2f, crate.cost * 0.45f);
                LevelManager.Instance.AddXP(baseXP);
            }
        }
        else if (reward is BoostData boost)
        {
            InventoryManager.Instance.AddItem(boost);
            Debug.Log($"Dropped Boost: {boost.itemName}");
        }

        OnCrateOpened?.Invoke(reward);
    }

    private void MarkCropUnlocked(string cropName)
    {
        if (string.IsNullOrWhiteSpace(cropName))
            return;

        if (_unlockedCropNames.Add(cropName))
        {
            SaveUnlockedCrops();
            OnUnlockedCropsChanged?.Invoke();
        }
    }

    private void BootstrapUnlocksFromInventory()
    {
        if (InventoryManager.Instance == null)
            return;

        List<CropData> ownedCrops = InventoryManager.Instance.GetItemsOfType<CropData>();
        bool changed = false;

        for (int i = 0; i < ownedCrops.Count; i++)
        {
            CropData crop = ownedCrops[i];
            if (crop == null || string.IsNullOrWhiteSpace(crop.itemName))
                continue;

            if (_unlockedCropNames.Add(crop.itemName))
            {
                changed = true;
            }
        }

        if (changed)
        {
            SaveUnlockedCrops();
            OnUnlockedCropsChanged?.Invoke();
        }
    }

    private void SaveUnlockedCrops()
    {
        PlayerPrefs.SetString(UNLOCKED_CROPS_KEY, string.Join("|", _unlockedCropNames));
        PlayerPrefs.Save();
    }

    private void LoadUnlockedCrops()
    {
        _unlockedCropNames.Clear();

        string raw = PlayerPrefs.GetString(UNLOCKED_CROPS_KEY, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return;

        string[] parts = raw.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(parts[i]))
            {
                _unlockedCropNames.Add(parts[i]);
            }
        }
    }
}
