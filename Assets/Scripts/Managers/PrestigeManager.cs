using System;
using UnityEngine;

[Serializable]
public class PrestigeRequirementData
{
    public int targetPrestigeLevel;
    public int requiredLevel;
    public int requiredLifetimeCoins;
    public int requiredResearchSpent;
    public int requiredFactoryCompletions;
}

public class PrestigeManager : MonoBehaviour
{
    public static PrestigeManager Instance { get; private set; }

    public int PrestigeLevel { get; private set; } = 1;
    public int FactoryCompletions { get; private set; }

    public event Action OnPrestigeDataChanged;

    private const string PRESTIGE_LEVEL_KEY = "PrestigeLevel";
    private const string PRESTIGE_FACTORY_COMPLETIONS_KEY = "PrestigeFactoryCompletions";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            CurrencyManager.Instance?.ApplyCoinCap();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetCoinCap()
    {
        return GetCoinCapForLevel(PrestigeLevel);
    }

    public int GetCoinCapForLevel(int level)
    {
        int safeLevel = Mathf.Max(1, level);
        long cap = 5000;

        for (int i = 1; i < safeLevel; i++)
        {
            cap *= 4;
            if (cap >= int.MaxValue)
                return int.MaxValue;
        }

        return (int)cap;
    }

    public float GetIncomeMultiplier()
    {
        return 1f + 0.12f * Mathf.Max(0, PrestigeLevel - 1);
    }

    public float GetFactoryRewardMultiplier()
    {
        return 1f + 0.10f * Mathf.Max(0, PrestigeLevel - 1);
    }

    public float GetGemRewardMultiplier()
    {
        return 1f + 0.08f * Mathf.Max(0, PrestigeLevel - 1);
    }

    public PrestigeRequirementData GetNextRequirement()
    {
        int target = PrestigeLevel + 1;

        return new PrestigeRequirementData
        {
            targetPrestigeLevel = target,
            requiredLevel = 8 + target * 6,
            requiredLifetimeCoins = GetCoinCapForLevel(target - 1),
            requiredResearchSpent = 30 * target * target,
            requiredFactoryCompletions = 4 + target * 4
        };
    }

    public bool CanPrestige(out string reason)
    {
        PrestigeRequirementData req = GetNextRequirement();

        if (LevelManager.Instance == null || CurrencyManager.Instance == null || ResearchManager.Instance == null)
        {
            reason = "Sistemler hazir degil.";
            return false;
        }

        if (LevelManager.Instance.Level < req.requiredLevel)
        {
            reason = "Seviye yetersiz.";
            return false;
        }

        if (CurrencyManager.Instance.LifetimeCoinEarned < req.requiredLifetimeCoins)
        {
            reason = "Toplam coin kazanci yetersiz.";
            return false;
        }

        if (ResearchManager.Instance.TotalResearchSpent < req.requiredResearchSpent)
        {
            reason = "Harcanan RP yetersiz.";
            return false;
        }

        if (FactoryCompletions < req.requiredFactoryCompletions)
        {
            reason = "Fabrika gorev tamamlama sayisi yetersiz.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool TryPrestige(out string feedback)
    {
        if (!CanPrestige(out feedback))
            return false;

        PrestigeLevel++;
        int gemReward = 10 + 5 * PrestigeLevel;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGem(gemReward);
            CurrencyManager.Instance.ApplyCoinCap();
        }

        GameMessageManager.Instance?.PushMessage("Prestij " + PrestigeLevel + " oldu. +" + gemReward + " Gem odulu.");

        Save();
        OnPrestigeDataChanged?.Invoke();

        feedback = "Prestij yukseltti! Yeni Coin cap: " + GetCoinCap();
        return true;
    }

    public void RegisterFactoryCompletion()
    {
        FactoryCompletions++;
        Save();
        OnPrestigeDataChanged?.Invoke();
    }

    private void Save()
    {
        SecurePlayerPrefs.SetInt(PRESTIGE_LEVEL_KEY, Mathf.Max(1, PrestigeLevel));
        SecurePlayerPrefs.SetInt(PRESTIGE_FACTORY_COMPLETIONS_KEY, Mathf.Max(0, FactoryCompletions));
        SaveCoordinator.MarkDirty();
    }

    private void Load()
    {
        PrestigeLevel = Mathf.Max(1, SecurePlayerPrefs.GetInt(PRESTIGE_LEVEL_KEY, 1));
        FactoryCompletions = Mathf.Max(0, SecurePlayerPrefs.GetInt(PRESTIGE_FACTORY_COMPLETIONS_KEY, 0));
        OnPrestigeDataChanged?.Invoke();
    }
}
