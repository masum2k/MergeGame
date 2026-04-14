using UnityEngine;
using System;
using System.Collections;

public class IncomeManager : MonoBehaviour
{
    public static IncomeManager Instance { get; private set; }
    private static int _pendingOfflinePopupIncome;

    private const string LAST_ACTIVE_UTC_TICKS_KEY = "IncomeLastActiveUtcTicks";
    private const string DECIMAL_CARRY_KEY = "IncomeDecimalCarry";
    private const float MAX_OFFLINE_SECONDS = 8f * 60f * 60f;
    private const float MIN_OFFLINE_SECONDS = 10f;
    private const float ECONOMY_SPEED_MULTIPLIER = 0.7f;
    private const float OFFLINE_INCOME_RATIO = 0.2f;

    [Header("Settings")]
    [Tooltip("How often in seconds the income is collected.")]
    public float collectionInterval = 1.0f;

    [Header("References")]
    public GridManager gridManager;

    // Event for UI to listen for income updates
    public static event Action<int> OnIncomeCollected;

    public static bool TryConsumePendingOfflinePopupIncome(out int amount)
    {
        amount = Mathf.Max(0, _pendingOfflinePopupIncome);
        _pendingOfflinePopupIncome = 0;
        return amount > 0;
    }

    private float _timer;
    // Buffer to hold decimal remainders between ticks so we don't lose value
    private float _uncollectedDecimals = 0f;
    private Coroutine _offlineInitCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Reset runtime carry-over for fresh session start safety.
        _pendingOfflinePopupIncome = 0;

        _uncollectedDecimals = Mathf.Clamp(SecurePlayerPrefs.GetFloat(DECIMAL_CARRY_KEY, 0f), 0f, 0.9999f);
    }

    private void Start()
    {
        _offlineInitCoroutine = StartCoroutine(ApplyOfflineProgressionWhenReady());
    }

    private void Update()
    {
        if (gridManager == null) return;

        float safeInterval = Mathf.Max(0.01f, collectionInterval);
        _timer += Time.deltaTime;
        while (_timer >= safeInterval)
        {
            CollectIncome();
            _timer -= safeInterval;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            PersistRuntimeState(DateTime.UtcNow);
        }
        else
        {
            TryStartOfflineProgressionCheck();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            TryStartOfflineProgressionCheck();
        }
        else
        {
            PersistRuntimeState(DateTime.UtcNow);
        }
    }

    private void OnApplicationQuit()
    {
        PersistRuntimeState(DateTime.UtcNow);
    }

    private void TryStartOfflineProgressionCheck()
    {
        if (_offlineInitCoroutine != null)
            return;

        if (CanApplyOfflineProgression())
        {
            TryApplyOfflineProgression();
            return;
        }

        _offlineInitCoroutine = StartCoroutine(ApplyOfflineProgressionWhenReady());
    }

    private IEnumerator ApplyOfflineProgressionWhenReady()
    {
        // Allow other startup systems to restore world state before we compute offline reward.
        yield return null;

        float timeout = 3f;
        while (timeout > 0f && !CanApplyOfflineProgression())
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        TryApplyOfflineProgression();
        _offlineInitCoroutine = null;
    }

    private bool CanApplyOfflineProgression()
    {
        return gridManager != null && CurrencyManager.Instance != null;
    }

    private void TryApplyOfflineProgression()
    {
        DateTime nowUtc = DateTime.UtcNow;

        if (!TryGetLastActiveUtc(out DateTime lastActiveUtc))
        {
            PersistRuntimeState(nowUtc);
            return;
        }

        double elapsedSecondsRaw = (nowUtc - lastActiveUtc).TotalSeconds;
        if (elapsedSecondsRaw <= MIN_OFFLINE_SECONDS)
        {
            PersistRuntimeState(nowUtc);
            return;
        }

        if (elapsedSecondsRaw < 0d)
        {
            PersistRuntimeState(nowUtc);
            return;
        }

        float elapsedSeconds = Mathf.Min((float)elapsedSecondsRaw, MAX_OFFLINE_SECONDS);
        if (elapsedSeconds <= 0f)
        {
            PersistRuntimeState(nowUtc);
            return;
        }

        // Offline income should be slower than active gameplay income.
        float incomePerSecond = CalculateIncomePerSecond(includeBoostMultiplier: true) * OFFLINE_INCOME_RATIO;
        if (incomePerSecond <= 0f)
        {
            PersistRuntimeState(nowUtc);
            return;
        }

        double totalIncome = (incomePerSecond * elapsedSeconds) + _uncollectedDecimals;
        int incomeAsInt = Mathf.FloorToInt((float)Math.Min(int.MaxValue, totalIncome));

        _uncollectedDecimals = (float)(totalIncome - incomeAsInt);
        _uncollectedDecimals = Mathf.Clamp(_uncollectedDecimals, 0f, 0.9999f);

        if (incomeAsInt > 0 && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCoin(incomeAsInt);

            if (OnIncomeCollected != null)
            {
                OnIncomeCollected.Invoke(incomeAsInt);
                _pendingOfflinePopupIncome = 0;
            }
            else
            {
                _pendingOfflinePopupIncome += incomeAsInt;
            }

            if (GameMessageManager.Instance != null)
            {
                TimeSpan duration = TimeSpan.FromSeconds(elapsedSeconds);
                string prettyDuration = duration.TotalHours >= 1d
                    ? $"{Mathf.FloorToInt((float)duration.TotalHours)}sa {duration.Minutes}dk"
                    : $"{duration.Minutes}dk {duration.Seconds}sn";

                GameMessageManager.Instance.PushMessage($"Offline gelir: +{incomeAsInt} Coin ({prettyDuration})");
            }
        }

        PersistRuntimeState(nowUtc);
    }

    private bool TryGetLastActiveUtc(out DateTime lastActiveUtc)
    {
        lastActiveUtc = DateTime.MinValue;

        if (!SecurePlayerPrefs.HasKey(LAST_ACTIVE_UTC_TICKS_KEY))
            return false;

        string rawTicks = SecurePlayerPrefs.GetString(LAST_ACTIVE_UTC_TICKS_KEY, string.Empty);
        if (!long.TryParse(rawTicks, out long ticks))
            return false;

        try
        {
            lastActiveUtc = DateTime.FromBinary(ticks);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void PersistRuntimeState(DateTime nowUtc)
    {
        SecurePlayerPrefs.SetString(LAST_ACTIVE_UTC_TICKS_KEY, nowUtc.ToBinary().ToString());
        SecurePlayerPrefs.SetFloat(DECIMAL_CARRY_KEY, Mathf.Clamp(_uncollectedDecimals, 0f, 0.9999f));
    }

    private void CollectIncome()
    {
        float totalIncome = CalculateIncomePerSecond(includeBoostMultiplier: true);

        if (totalIncome > 0)
        {
            // Add previous remainder
            totalIncome += _uncollectedDecimals;
            
            // Floor down to get integer coins to add to CurrencyManager
            int incomeAsInt = Mathf.FloorToInt(totalIncome);
            
            // Save remaining decimals for the next tick
            _uncollectedDecimals = totalIncome - incomeAsInt;

            if (incomeAsInt > 0)
            {
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.AddCoin(incomeAsInt);
                    // Trigger event for visual feedback (e.g. UIManager)
                    OnIncomeCollected?.Invoke(incomeAsInt);
                }
            }
        }
    }

    private float CalculateIncomePerSecond(bool includeBoostMultiplier)
    {
        if (gridManager == null)
            return 0f;

        float totalIncome = 0f;
        var allSlots = gridManager.GetAllSlots();
        if (allSlots == null)
            return 0f;

        foreach (var slot in allSlots)
        {
            if (slot == null || slot.IsEmpty || slot.CurrentCrop == null)
                continue;

            float slotIncome = slot.CurrentCrop.coinPerTick;

            if (ResearchManager.Instance != null)
            {
                slotIncome *= ResearchManager.Instance.GetCropIncomeMultiplier(slot.CurrentCrop);
            }

            if (PrestigeManager.Instance != null)
            {
                slotIncome *= PrestigeManager.Instance.GetIncomeMultiplier();
            }

            totalIncome += slotIncome;
        }

        if (includeBoostMultiplier && BoostManager.Instance != null)
        {
            totalIncome *= BoostManager.Instance.CoinMultiplier;
        }

        totalIncome *= ECONOMY_SPEED_MULTIPLIER;

        return Mathf.Max(0f, totalIncome);
    }
}
