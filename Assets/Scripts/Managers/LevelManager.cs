using UnityEngine;
using System;

public class LevelManager : MonoBehaviour
{
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
        XPForNextLevel = Mathf.Round(100f * Mathf.Pow(Level, 1.2f));

        // Reward: 10 Gems
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGem(10);
        }

        GameMessageManager.Instance?.PushMessage("Level " + Level + " oldu. +10 Gem.");

        OnLevelUp?.Invoke(Level);
        Debug.Log($"LEVEL UP! New Level: {Level}. Reward: 10 Gems.");
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
        XPForNextLevel = Mathf.Round(100f * Mathf.Pow(Level, 1.2f));
    }
}
