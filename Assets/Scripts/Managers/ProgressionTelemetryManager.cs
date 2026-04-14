using UnityEngine;

public class ProgressionTelemetryManager : MonoBehaviour
{
    public static ProgressionTelemetryManager Instance { get; private set; }

    private int _lastLifetimeCoins;
    private int _lastResearchPoints;
    private int _lastFactoryCompletions;
    private int _lastUnlockedCropTypes;

    private bool _currencyBound;
    private bool _researchBound;
    private bool _prestigeBound;
    private bool _crateBound;
    private bool _slotBound;
    private bool _levelBound;

    private float _rebindTimer;

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

    private void OnEnable()
    {
        BindMissingSources();
    }

    private void Update()
    {
        if (AllSourcesBound())
            return;

        _rebindTimer += Time.unscaledDeltaTime;
        if (_rebindTimer >= 1f)
        {
            _rebindTimer = 0f;
            BindMissingSources();
        }
    }

    private void OnDisable()
    {
        UnbindAllSources();
    }

    private bool AllSourcesBound()
    {
        return _currencyBound && _researchBound && _prestigeBound && _crateBound && _slotBound && _levelBound;
    }

    private void BindMissingSources()
    {
        if (!_currencyBound && CurrencyManager.Instance != null)
        {
            _lastLifetimeCoins = CurrencyManager.Instance.LifetimeCoinEarned;
            CurrencyManager.Instance.OnCoinChanged += HandleCoinChanged;
            _currencyBound = true;
        }

        if (!_researchBound && ResearchManager.Instance != null)
        {
            _lastResearchPoints = ResearchManager.Instance.ResearchPoints;
            ResearchManager.Instance.OnResearchPointsChanged += HandleResearchPointsChanged;
            _researchBound = true;
        }

        if (!_prestigeBound && PrestigeManager.Instance != null)
        {
            _lastFactoryCompletions = PrestigeManager.Instance.FactoryCompletions;
            PrestigeManager.Instance.OnPrestigeDataChanged += HandlePrestigeDataChanged;
            _prestigeBound = true;
        }

        if (!_crateBound && CrateManager.Instance != null)
        {
            _lastUnlockedCropTypes = CrateManager.Instance.GetUnlockedCropNames().Count;
            CrateManager.Instance.OnCrateOpened += HandleCrateOpened;
            CrateManager.Instance.OnUnlockedCropsChanged += HandleUnlockedCropsChanged;
            _crateBound = true;
        }

        if (!_slotBound && SlotUnlockManager.Instance != null)
        {
            SlotUnlockManager.Instance.OnSlotUnlocked += HandleSlotUnlocked;
            _slotBound = true;
        }

        if (!_levelBound && LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelUp += HandleLevelUp;
            _levelBound = true;
        }
    }

    private void UnbindAllSources()
    {
        if (_currencyBound && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged -= HandleCoinChanged;
        }

        if (_researchBound && ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnResearchPointsChanged -= HandleResearchPointsChanged;
        }

        if (_prestigeBound && PrestigeManager.Instance != null)
        {
            PrestigeManager.Instance.OnPrestigeDataChanged -= HandlePrestigeDataChanged;
        }

        if (_crateBound && CrateManager.Instance != null)
        {
            CrateManager.Instance.OnCrateOpened -= HandleCrateOpened;
            CrateManager.Instance.OnUnlockedCropsChanged -= HandleUnlockedCropsChanged;
        }

        if (_slotBound && SlotUnlockManager.Instance != null)
        {
            SlotUnlockManager.Instance.OnSlotUnlocked -= HandleSlotUnlocked;
        }

        if (_levelBound && LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelUp -= HandleLevelUp;
        }

        _currencyBound = false;
        _researchBound = false;
        _prestigeBound = false;
        _crateBound = false;
        _slotBound = false;
        _levelBound = false;
    }

    private void HandleCoinChanged(int _)
    {
        if (CurrencyManager.Instance == null)
            return;

        int currentLifetime = CurrencyManager.Instance.LifetimeCoinEarned;
        int delta = currentLifetime - _lastLifetimeCoins;
        if (delta > 0)
        {
            ProgressionSignals.Raise(ProgressMetricType.EarnCoin, delta);
        }

        _lastLifetimeCoins = currentLifetime;
    }

    private void HandleResearchPointsChanged(int current)
    {
        int delta = current - _lastResearchPoints;
        if (delta > 0)
        {
            ProgressionSignals.Raise(ProgressMetricType.GainResearchPoint, delta);
        }

        _lastResearchPoints = current;
    }

    private void HandlePrestigeDataChanged()
    {
        if (PrestigeManager.Instance == null)
            return;

        int current = PrestigeManager.Instance.FactoryCompletions;
        int delta = current - _lastFactoryCompletions;
        if (delta > 0)
        {
            ProgressionSignals.Raise(ProgressMetricType.CompleteFactoryOffer, delta);
        }

        _lastFactoryCompletions = current;
    }

    private void HandleCrateOpened(object _)
    {
        ProgressionSignals.Raise(ProgressMetricType.OpenCrate, 1);
    }

    private void HandleUnlockedCropsChanged()
    {
        if (CrateManager.Instance == null)
            return;

        int current = CrateManager.Instance.GetUnlockedCropNames().Count;
        int delta = current - _lastUnlockedCropTypes;
        if (delta > 0)
        {
            ProgressionSignals.Raise(ProgressMetricType.UnlockCropType, delta);
        }

        _lastUnlockedCropTypes = current;
    }

    private void HandleSlotUnlocked(int _, int __)
    {
        ProgressionSignals.Raise(ProgressMetricType.UnlockSlot, 1);
    }

    private void HandleLevelUp(int _)
    {
        ProgressionSignals.Raise(ProgressMetricType.GainLevel, 1);
    }
}
