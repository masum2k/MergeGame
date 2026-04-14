using System;
using UnityEngine;

public class StreakRewardManager : MonoBehaviour
{
    public static StreakRewardManager Instance { get; private set; }

    public event Action OnStreakChanged;

    public int CurrentStreakDays => _currentStreakDays;

    private const string STREAK_DAYS_KEY = "StreakDays";
    private const string STREAK_LAST_DAY_KEY = "StreakLastDay";
    private const string STREAK_MILESTONE_MASK_KEY = "StreakMilestoneMask";

    private const int MILESTONE_7_MASK = 1 << 0;
    private const int MILESTONE_14_MASK = 1 << 1;
    private const int MILESTONE_30_MASK = 1 << 2;

    private int _currentStreakDays;
    private string _lastCheckinDayKey;
    private int _milestoneMask;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            ProcessCheckIn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ProcessCheckIn();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            ProcessCheckIn();
        }
    }

    private void ProcessCheckIn()
    {
        string todayKey = GetUtcDayKey(DateTime.UtcNow);
        if (_lastCheckinDayKey == todayKey)
            return;

        if (string.IsNullOrWhiteSpace(_lastCheckinDayKey))
        {
            _currentStreakDays = 1;
            _milestoneMask = 0;
        }
        else
        {
            if (TryParseDayKey(_lastCheckinDayKey, out DateTime lastDate))
            {
                DateTime today = DateTime.UtcNow.Date;
                int dayDiff = (today - lastDate.Date).Days;

                if (dayDiff == 1)
                {
                    _currentStreakDays += 1;
                }
                else
                {
                    _currentStreakDays = 1;
                    _milestoneMask = 0;
                }
            }
            else
            {
                _currentStreakDays = 1;
                _milestoneMask = 0;
            }
        }

        _lastCheckinDayKey = todayKey;
        GrantMilestonesIfNeeded();

        Save();
        OnStreakChanged?.Invoke();

        GameMessageManager.Instance?.PushMessage("Gunluk giris serisi: " + _currentStreakDays + " gun.");
    }

    private void GrantMilestonesIfNeeded()
    {
        if (_currentStreakDays >= 7 && (_milestoneMask & MILESTONE_7_MASK) == 0)
        {
            GrantMilestoneReward(7, 2000, 20, 60);
            _milestoneMask |= MILESTONE_7_MASK;
        }

        if (_currentStreakDays >= 14 && (_milestoneMask & MILESTONE_14_MASK) == 0)
        {
            GrantMilestoneReward(14, 5000, 40, 140);
            _milestoneMask |= MILESTONE_14_MASK;
        }

        if (_currentStreakDays >= 30 && (_milestoneMask & MILESTONE_30_MASK) == 0)
        {
            GrantMilestoneReward(30, 15000, 110, 420);
            _milestoneMask |= MILESTONE_30_MASK;
        }
    }

    private void GrantMilestoneReward(int day, int rewardCoin, int rewardGem, int rewardRp)
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCoin(rewardCoin);
            CurrencyManager.Instance.AddGem(rewardGem);
        }

        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.AddResearchPoints(rewardRp);
        }

        GameMessageManager.Instance?.PushMessage(
            "Streak odulu (" + day + ". gun): +" + rewardCoin + " Coin, +" + rewardGem + " Gem, +" + rewardRp + " RP");
    }

    private void Save()
    {
        SecurePlayerPrefs.SetInt(STREAK_DAYS_KEY, Mathf.Max(0, _currentStreakDays));
        SecurePlayerPrefs.SetString(STREAK_LAST_DAY_KEY, _lastCheckinDayKey ?? string.Empty);
        SecurePlayerPrefs.SetInt(STREAK_MILESTONE_MASK_KEY, Mathf.Max(0, _milestoneMask));
    }

    private void Load()
    {
        _currentStreakDays = Mathf.Max(0, SecurePlayerPrefs.GetInt(STREAK_DAYS_KEY, 0));
        _lastCheckinDayKey = SecurePlayerPrefs.GetString(STREAK_LAST_DAY_KEY, string.Empty);
        _milestoneMask = Mathf.Max(0, SecurePlayerPrefs.GetInt(STREAK_MILESTONE_MASK_KEY, 0));
    }

    private static string GetUtcDayKey(DateTime utcNow)
    {
        return utcNow.Date.ToString("yyyyMMdd");
    }

    private static bool TryParseDayKey(string dayKey, out DateTime date)
    {
        if (DateTime.TryParseExact(dayKey, "yyyyMMdd", null, System.Globalization.DateTimeStyles.AssumeUniversal, out date))
        {
            date = date.Date;
            return true;
        }

        date = DateTime.MinValue;
        return false;
    }
}
