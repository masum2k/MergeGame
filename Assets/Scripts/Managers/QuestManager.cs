using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestState
{
    public string id;
    public string title;
    public string description;
    public ProgressMetricType metric;
    public int target;
    public int progress;
    public int rewardCoin;
    public int rewardGem;
    public int rewardRp;
    public bool claimed;
}

public class QuestManager : MonoBehaviour
{
    private class QuestTemplate
    {
        public string id;
        public string title;
        public string description;
        public ProgressMetricType metric;
        public int minTarget;
        public int maxTarget;
        public int rewardCoin;
        public int rewardGem;
        public int rewardRp;
    }

    [Serializable]
    private class QuestSaveWrapper
    {
        public string dailyResetKey;
        public string weeklyResetKey;
        public List<QuestState> dailyQuests = new List<QuestState>();
        public QuestState weeklyQuest;
    }

    public static QuestManager Instance { get; private set; }

    public event Action OnQuestStateChanged;

    public IReadOnlyList<QuestState> DailyQuests => _dailyQuests;
    public QuestState WeeklyQuest => _weeklyQuest;

    private readonly List<QuestState> _dailyQuests = new List<QuestState>();
    private QuestState _weeklyQuest;

    private const string QUESTS_SAVE_KEY = "QuestSystemDataV1";
    private const int DAILY_QUEST_COUNT = 3;

    private string _dailyResetKey;
    private string _weeklyResetKey;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            EnsureQuestState();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        ProgressionSignals.OnMetricProgress += HandleProgressMetric;
    }

    private void OnDisable()
    {
        ProgressionSignals.OnMetricProgress -= HandleProgressMetric;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            EnsureQuestState();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            EnsureQuestState();
        }
    }

    private void HandleProgressMetric(ProgressMetricType metric, int amount)
    {
        if (amount <= 0)
            return;

        bool changed = false;

        for (int i = 0; i < _dailyQuests.Count; i++)
        {
            if (ApplyProgress(_dailyQuests[i], metric, amount))
            {
                changed = true;
            }
        }

        if (_weeklyQuest != null && ApplyProgress(_weeklyQuest, metric, amount))
        {
            changed = true;
        }

        if (changed)
        {
            Save();
            OnQuestStateChanged?.Invoke();
        }
    }

    private bool ApplyProgress(QuestState quest, ProgressMetricType metric, int amount)
    {
        if (quest == null || quest.claimed || quest.metric != metric)
            return false;

        int before = quest.progress;
        quest.progress = Mathf.Clamp(quest.progress + amount, 0, quest.target);
        if (quest.progress == before)
            return false;

        if (quest.progress >= quest.target)
        {
            AutoClaimQuest(quest, IsWeeklyQuest(quest));
        }

        return true;
    }

    private bool IsWeeklyQuest(QuestState quest)
    {
        return _weeklyQuest != null && quest == _weeklyQuest;
    }

    private void AutoClaimQuest(QuestState quest, bool weekly)
    {
        if (quest == null || quest.claimed || quest.progress < quest.target)
            return;

        quest.claimed = true;
        GrantRewards(quest.rewardCoin, quest.rewardGem, quest.rewardRp);

        string scope = weekly ? "Haftalik mega gorev" : "Gunluk gorev";
        GameMessageManager.Instance?.PushMessage(scope + " tamamlandi: " + quest.title + BuildRewardSuffix(quest));
    }

    private void GrantRewards(int rewardCoin, int rewardGem, int rewardRp)
    {
        if (rewardCoin > 0 && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCoin(rewardCoin);
        }

        if (rewardGem > 0 && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGem(rewardGem);
        }

        if (rewardRp > 0 && ResearchManager.Instance != null)
        {
            ResearchManager.Instance.AddResearchPoints(rewardRp);
        }
    }

    private string BuildRewardSuffix(QuestState quest)
    {
        List<string> rewardParts = new List<string>();
        if (quest.rewardCoin > 0) rewardParts.Add("+" + quest.rewardCoin + " Coin");
        if (quest.rewardGem > 0) rewardParts.Add("+" + quest.rewardGem + " Gem");
        if (quest.rewardRp > 0) rewardParts.Add("+" + quest.rewardRp + " RP");

        if (rewardParts.Count == 0)
            return string.Empty;

        return " (" + string.Join(", ", rewardParts) + ")";
    }

    private void EnsureQuestState()
    {
        string todayKey = GetUtcDayKey(DateTime.UtcNow);
        string weekKey = GetUtcWeekKey(DateTime.UtcNow);

        if (_dailyResetKey != todayKey || _dailyQuests.Count != DAILY_QUEST_COUNT || HasObsoleteMergeQuest() || HasLegacyLevelCoinDailyQuest())
        {
            GenerateDailyQuests(todayKey);
            GameMessageManager.Instance?.PushMessage("Yeni gunluk gorevler hazir.");
        }

        if (_weeklyResetKey != weekKey || _weeklyQuest == null || (_weeklyQuest != null && _weeklyQuest.metric == ProgressMetricType.MergeCrop) || HasLegacyLevelCoinWeeklyQuest())
        {
            GenerateWeeklyQuest(weekKey);
            GameMessageManager.Instance?.PushMessage("Yeni haftalik mega gorev acildi.");
        }

        AutoClaimReadyQuests();
        Save();
        OnQuestStateChanged?.Invoke();
    }

    private void AutoClaimReadyQuests()
    {
        for (int i = 0; i < _dailyQuests.Count; i++)
        {
            AutoClaimQuest(_dailyQuests[i], false);
        }

        if (_weeklyQuest != null)
        {
            AutoClaimQuest(_weeklyQuest, true);
        }
    }

    private void GenerateDailyQuests(string resetKey)
    {
        _dailyResetKey = resetKey;
        _dailyQuests.Clear();

        List<QuestTemplate> templates = BuildDailyTemplates();
        int seed = ParseSeedFromKey(resetKey) + 197;
        System.Random rng = new System.Random(seed);

        HashSet<int> usedIndices = new HashSet<int>();
        while (_dailyQuests.Count < DAILY_QUEST_COUNT && usedIndices.Count < templates.Count)
        {
            int idx = rng.Next(templates.Count);
            if (!usedIndices.Add(idx))
                continue;

            _dailyQuests.Add(CreateQuestFromTemplate(templates[idx], rng, "daily"));
        }
    }

    private void GenerateWeeklyQuest(string resetKey)
    {
        _weeklyResetKey = resetKey;

        List<QuestTemplate> templates = BuildWeeklyTemplates();
        int seed = ParseSeedFromKey(resetKey) + 719;
        System.Random rng = new System.Random(seed);

        int idx = rng.Next(templates.Count);
        _weeklyQuest = CreateQuestFromTemplate(templates[idx], rng, "weekly");
    }

    private QuestState CreateQuestFromTemplate(QuestTemplate template, System.Random rng, string scope)
    {
        int target = template.minTarget;
        if (template.maxTarget > template.minTarget)
        {
            target = rng.Next(template.minTarget, template.maxTarget + 1);
        }

        return new QuestState
        {
            id = scope + "_" + template.id,
            title = template.title,
            description = template.description,
            metric = template.metric,
            target = Mathf.Max(1, target),
            progress = 0,
            rewardCoin = Mathf.Max(0, template.rewardCoin),
            rewardGem = Mathf.Max(0, template.rewardGem),
            rewardRp = Mathf.Max(0, template.rewardRp),
            claimed = false
        };
    }

    private List<QuestTemplate> BuildDailyTemplates()
    {
        return new List<QuestTemplate>
        {
            new QuestTemplate { id = "crate", title = "Sandik Avcisi", description = "Sandik ac.", metric = ProgressMetricType.OpenCrate, minTarget = 4, maxTarget = 8, rewardCoin = 800, rewardGem = 8, rewardRp = 25 },
            new QuestTemplate { id = "factory", title = "Fabrika Vardiyasi", description = "Fabrika teklifi tamamla.", metric = ProgressMetricType.CompleteFactoryOffer, minTarget = 2, maxTarget = 5, rewardCoin = 1500, rewardGem = 7, rewardRp = 40 },
            new QuestTemplate { id = "earn_coin", title = "Kasayi Doldur", description = "Coin kazan.", metric = ProgressMetricType.EarnCoin, minTarget = 3000, maxTarget = 7000, rewardCoin = 2200, rewardGem = 10, rewardRp = 35 },
            new QuestTemplate { id = "research", title = "Arastirma Hamlesi", description = "Arastirma puani topla.", metric = ProgressMetricType.GainResearchPoint, minTarget = 70, maxTarget = 160, rewardCoin = 1000, rewardGem = 5, rewardRp = 45 },
            new QuestTemplate { id = "slot", title = "Alan Genislet", description = "Kilitli slot ac.", metric = ProgressMetricType.UnlockSlot, minTarget = 1, maxTarget = 3, rewardCoin = 900, rewardGem = 6, rewardRp = 30 },
            new QuestTemplate { id = "level", title = "Seviye Atla", description = "Seviye atla.", metric = ProgressMetricType.GainLevel, minTarget = 1, maxTarget = 1, rewardCoin = 0, rewardGem = 5, rewardRp = 24 }
        };
    }

    private List<QuestTemplate> BuildWeeklyTemplates()
    {
        return new List<QuestTemplate>
        {
            new QuestTemplate { id = "mega_coin", title = "Mega Gelir", description = "Hafta boyunca buyuk coin hedefine ulas.", metric = ProgressMetricType.EarnCoin, minTarget = 30000, maxTarget = 50000, rewardCoin = 10000, rewardGem = 55, rewardRp = 220 },
            new QuestTemplate { id = "mega_factory", title = "Mega Uretim", description = "Cok sayida fabrika teklifi tamamla.", metric = ProgressMetricType.CompleteFactoryOffer, minTarget = 12, maxTarget = 20, rewardCoin = 8000, rewardGem = 60, rewardRp = 240 },
            new QuestTemplate { id = "mega_crate", title = "Mega Sandik", description = "Yuksek sayida sandik ac.", metric = ProgressMetricType.OpenCrate, minTarget = 25, maxTarget = 40, rewardCoin = 7500, rewardGem = 65, rewardRp = 260 },
            new QuestTemplate { id = "mega_level", title = "Mega Gelisim", description = "Hafta boyunca birden fazla seviye atla.", metric = ProgressMetricType.GainLevel, minTarget = 4, maxTarget = 7, rewardCoin = 0, rewardGem = 24, rewardRp = 180 }
        };
    }

    private bool HasObsoleteMergeQuest()
    {
        for (int i = 0; i < _dailyQuests.Count; i++)
        {
            if (_dailyQuests[i] != null && _dailyQuests[i].metric == ProgressMetricType.MergeCrop)
                return true;
        }

        return false;
    }

    private bool HasLegacyLevelCoinDailyQuest()
    {
        for (int i = 0; i < _dailyQuests.Count; i++)
        {
            QuestState q = _dailyQuests[i];
            if (q != null && q.metric == ProgressMetricType.GainLevel && q.rewardCoin > 0)
                return true;
        }

        return false;
    }

    private bool HasLegacyLevelCoinWeeklyQuest()
    {
        return _weeklyQuest != null && _weeklyQuest.metric == ProgressMetricType.GainLevel && _weeklyQuest.rewardCoin > 0;
    }

    private void Save()
    {
        QuestSaveWrapper wrapper = new QuestSaveWrapper
        {
            dailyResetKey = _dailyResetKey,
            weeklyResetKey = _weeklyResetKey,
            dailyQuests = _dailyQuests,
            weeklyQuest = _weeklyQuest
        };

        string json = JsonUtility.ToJson(wrapper);
        SecurePlayerPrefs.SetString(QUESTS_SAVE_KEY, json);
    }

    private void Load()
    {
        _dailyQuests.Clear();
        _weeklyQuest = null;

        string json = SecurePlayerPrefs.GetString(QUESTS_SAVE_KEY, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return;

        QuestSaveWrapper wrapper = JsonUtility.FromJson<QuestSaveWrapper>(json);
        if (wrapper == null)
            return;

        _dailyResetKey = wrapper.dailyResetKey;
        _weeklyResetKey = wrapper.weeklyResetKey;

        if (wrapper.dailyQuests != null)
        {
            _dailyQuests.AddRange(wrapper.dailyQuests);
        }

        _weeklyQuest = wrapper.weeklyQuest;
    }

    private static string GetUtcDayKey(DateTime utcNow)
    {
        return utcNow.Date.ToString("yyyyMMdd");
    }

    private static string GetUtcWeekKey(DateTime utcNow)
    {
        DateTime day = utcNow.Date;
        int offset = ((int)day.DayOfWeek + 6) % 7; // Monday = 0
        DateTime monday = day.AddDays(-offset);
        return monday.ToString("yyyyMMdd");
    }

    private static int ParseSeedFromKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return 1337;

        int seed = 0;
        for (int i = 0; i < key.Length; i++)
        {
            seed = (seed * 31) + key[i];
        }

        return Mathf.Abs(seed);
    }
}
