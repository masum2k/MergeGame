using UnityEngine;
using System;

public class LevelManager : MonoBehaviour
{
    private const int LEVEL_UP_GEM_REWARD = 3;
    private const float LEVEL_XP_BASE = 130f;
    private const float LEVEL_XP_EXPONENT = 1.3f;

    public static LevelManager Instance { get; private set; }

    [Header("State")]
    public int Level = 1;
    public float CurrentXP = 0;
    public float XPForNextLevel = 100;

    public event Action<int, float, float> OnXPChanged; // Level, CurrentXP, MaxXP
    public event Action<int> OnLevelUp;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddXP(float amount)
    {
        CurrentXP += amount;
        
        while (CurrentXP >= XPForNextLevel)
        {
            LevelUp();
        }

        OnXPChanged?.Invoke(Level, CurrentXP, XPForNextLevel);
        Save();
    }

    private void LevelUp()
    {
        CurrentXP -= XPForNextLevel;
        Level++;
        XPForNextLevel = Mathf.Round(LEVEL_XP_BASE * Mathf.Pow(Level, LEVEL_XP_EXPONENT));

        // Reward is intentionally low to avoid fast gem inflation.
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGem(LEVEL_UP_GEM_REWARD);
        }

        GameMessageManager.Instance?.PushMessage("Level " + Level + " oldu. +" + LEVEL_UP_GEM_REWARD + " Gem.");

        OnLevelUp?.Invoke(Level);
        Debug.Log($"LEVEL UP! New Level: {Level}. Reward: {LEVEL_UP_GEM_REWARD} Gems.");
    }

    private void Save()
    {
        SecurePlayerPrefs.SetInt("PlayerLevel", Level);
        SecurePlayerPrefs.SetFloat("PlayerXP", CurrentXP);
        SaveCoordinator.MarkDirty();
    }

    private void Load()
    {
        Level = SecurePlayerPrefs.GetInt("PlayerLevel", 1);
        CurrentXP = SecurePlayerPrefs.GetFloat("PlayerXP", 0);
        XPForNextLevel = Mathf.Round(LEVEL_XP_BASE * Mathf.Pow(Level, LEVEL_XP_EXPONENT));
    }
}
