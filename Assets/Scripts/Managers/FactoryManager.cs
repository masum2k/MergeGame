using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FactoryOfferData
{
    public string offerId;
    public string cropName;
    public int requiredAmount;
    public int rewardResearchPoints;
    public int rewardGems;
    public int rewardCoins;
    public string rewardCropName;
    public int rewardCropAmount;
    public long expiresAtTicks;
    public bool completed;
}

public class FactoryManager : MonoBehaviour
{
    public static FactoryManager Instance { get; private set; }

    public event Action OnOffersChanged;

    public IReadOnlyList<FactoryOfferData> CurrentOffers => _offers;

    private readonly List<FactoryOfferData> _offers = new List<FactoryOfferData>();

    private const string OFFERS_SAVE_KEY = "FactoryOffersData";
    private const int OFFER_COUNT = 21;
    private const int PURE_REWARD_OFFER_COUNT = 3;
    private const int OFFER_DURATION_HOURS = 24;

    private float _refreshTimer;

    [Serializable]
    private class OfferSaveWrapper
    {
        public List<FactoryOfferData> offers = new List<FactoryOfferData>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            EnsureOffers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= 1f)
        {
            _refreshTimer = 0f;
            RefreshOffersIfNeeded();
        }
    }

    public void EnsureOffers()
    {
        if (!CanGenerateOffers())
            return;

        bool needsRegenerate = _offers.Count != OFFER_COUNT || AreAllOffersResolved();
        if (!needsRegenerate)
        {
            for (int i = 0; i < _offers.Count; i++)
            {
                if (!IsOfferStructurallyValid(_offers[i]))
                {
                    needsRegenerate = true;
                    break;
                }
            }
        }

        if (needsRegenerate)
        {
            GenerateNewOffers();
        }
    }

    public bool TryCompleteOffer(string offerId, out string feedback)
    {
        FactoryOfferData offer = GetOfferById(offerId);
        if (offer == null)
        {
            feedback = "Teklif bulunamadi.";
            return false;
        }

        if (offer.completed)
        {
            feedback = "Bu teklif zaten tamamlandi.";
            return false;
        }

        if (IsExpired(offer))
        {
            feedback = "Teklifin suresi doldu. Yeni teklifler geliyor.";
            GenerateNewOffers();
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            feedback = "Envanter sistemi hazir degil.";
            return false;
        }

        int currentCount = InventoryManager.Instance.GetCount(offer.cropName);
        if (currentCount < offer.requiredAmount)
        {
            feedback = offer.cropName + " yetersiz. " + currentCount + "/" + offer.requiredAmount;
            return false;
        }

        if (!InventoryManager.Instance.TryRemoveItemByName(offer.cropName, offer.requiredAmount))
        {
            feedback = "Envanter guncellenirken bir sorun olustu.";
            return false;
        }

        int rpReward = offer.rewardResearchPoints;
        int gemReward = offer.rewardGems;
        int coinReward = offer.rewardCoins;
        int cropRewardAmount = offer.rewardCropAmount;
        string cropRewardName = offer.rewardCropName;

        float rpMultiplier = 1f;
        float gemMultiplier = 1f;
        float coinMultiplier = 1f;

        if (ResearchManager.Instance != null)
        {
            rpMultiplier *= ResearchManager.Instance.GetFactoryRpMultiplier();
            gemMultiplier *= ResearchManager.Instance.GetFactoryGemMultiplier();
        }

        if (PrestigeManager.Instance != null)
        {
            rpMultiplier *= PrestigeManager.Instance.GetFactoryRewardMultiplier();
            gemMultiplier *= PrestigeManager.Instance.GetGemRewardMultiplier();
            coinMultiplier *= PrestigeManager.Instance.GetIncomeMultiplier();
        }

        rpReward = Mathf.Max(0, rpReward);
        gemReward = Mathf.Max(0, gemReward);
        coinReward = Mathf.Max(0, coinReward);
        cropRewardAmount = Mathf.Max(0, cropRewardAmount);

        if (rpReward > 0)
        {
            rpReward = Mathf.Max(1, Mathf.RoundToInt(rpReward * rpMultiplier));
        }

        if (gemReward > 0)
        {
            gemReward = Mathf.Max(1, Mathf.RoundToInt(gemReward * gemMultiplier));
        }

        if (coinReward > 0)
        {
            coinReward = Mathf.Max(1, Mathf.RoundToInt(coinReward * coinMultiplier));
        }

        if (rpReward > 0 && ResearchManager.Instance != null)
        {
            ResearchManager.Instance.AddResearchPoints(rpReward);
        }

        if (gemReward > 0 && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGem(gemReward);
        }

        if (coinReward > 0 && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCoin(coinReward);
        }

        if (cropRewardAmount > 0 && !string.IsNullOrWhiteSpace(cropRewardName))
        {
            CropData rewardCrop = GameContentGenerator.Instance != null
                ? GameContentGenerator.Instance.GetCropByName(cropRewardName)
                : null;

            if (rewardCrop != null)
            {
                for (int i = 0; i < cropRewardAmount; i++)
                {
                    InventoryManager.Instance.AddItem(rewardCrop);
                }
            }
            else
            {
                cropRewardAmount = 0;
                cropRewardName = string.Empty;
            }
        }

        PrestigeManager.Instance?.RegisterFactoryCompletion();

        offer.completed = true;
        Save();
        OnOffersChanged?.Invoke();

        List<string> rewardParts = new List<string>();
        if (rpReward > 0) rewardParts.Add("+" + rpReward + " RP");
        if (gemReward > 0) rewardParts.Add("+" + gemReward + " Gem");
        if (coinReward > 0) rewardParts.Add("+" + coinReward + " Coin");
        if (cropRewardAmount > 0 && !string.IsNullOrWhiteSpace(cropRewardName)) rewardParts.Add("+" + cropRewardAmount + "x " + cropRewardName);

        feedback = rewardParts.Count > 0
            ? string.Join(", ", rewardParts) + " kazandin!"
            : "Odul alinmadi.";

        GameMessageManager.Instance?.PushMessage("Fabrika teklif tamamlandi: " + feedback);

        if (AreAllOffersResolved())
        {
            GenerateNewOffers();
        }

        return true;
    }

    public TimeSpan GetRemainingTime(FactoryOfferData offer)
    {
        if (offer == null)
            return TimeSpan.Zero;

        TimeSpan remaining = new DateTime(offer.expiresAtTicks, DateTimeKind.Utc) - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private FactoryOfferData GetOfferById(string offerId)
    {
        return _offers.Find(o => o.offerId == offerId);
    }

    private void RefreshOffersIfNeeded()
    {
        if (!CanGenerateOffers())
            return;

        if (_offers.Count == 0)
        {
            GenerateNewOffers();
            return;
        }

        if (AreAllOffersResolved())
        {
            GenerateNewOffers();
            return;
        }

        bool anyExpired = false;
        for (int i = 0; i < _offers.Count; i++)
        {
            if (!_offers[i].completed && IsExpired(_offers[i]))
            {
                anyExpired = true;
                break;
            }
        }

        if (anyExpired)
        {
            GenerateNewOffers();
        }
    }

    private bool CanGenerateOffers()
    {
        return GameContentGenerator.Instance != null &&
               GameContentGenerator.Instance.AllCrops != null &&
               GameContentGenerator.Instance.AllCrops.Count > 0;
    }

    private bool AreAllOffersResolved()
    {
        if (_offers.Count == 0)
            return true;

        for (int i = 0; i < _offers.Count; i++)
        {
            if (!_offers[i].completed && !IsExpired(_offers[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsExpired(FactoryOfferData offer)
    {
        return DateTime.UtcNow.Ticks >= offer.expiresAtTicks;
    }

    private bool IsOfferStructurallyValid(FactoryOfferData offer)
    {
        if (offer == null)
            return false;

        if (string.IsNullOrWhiteSpace(offer.cropName) || offer.requiredAmount <= 0)
            return false;

        bool hasResearch = offer.rewardResearchPoints > 0;
        bool hasGem = offer.rewardGems > 0;
        bool hasCoin = offer.rewardCoins > 0;
        bool hasCrop = offer.rewardCropAmount > 0 && !string.IsNullOrWhiteSpace(offer.rewardCropName);

        return hasResearch || hasGem || hasCoin || hasCrop;
    }

    private int RollRewardProfile(int tierIndex, System.Random rng)
    {
        int roll = rng.Next(100);
        int profile;

        // 0: RP, 1: Gem, 2: Crop, 3: RP+Gem, 4: RP+Crop, 5: Gem+Crop, 6: All
        if (roll < 20) profile = 0;
        else if (roll < 34) profile = 1;
        else if (roll < 48) profile = 2;
        else if (roll < 68) profile = 3;
        else if (roll < 81) profile = 4;
        else if (roll < 94) profile = 5;
        else profile = 6;

        // High tiers more often produce full reward packs.
        if (tierIndex >= 8 && rng.NextDouble() < 0.24)
        {
            profile = 6;
        }

        return profile;
    }

    private CropData PickRewardCrop(List<CropData> cropPool, int requiredTierIndex, int maxTierIndex, System.Random rng)
    {
        if (cropPool == null || cropPool.Count == 0)
            return null;

        int minRewardTier = Mathf.Clamp(requiredTierIndex + 2, 0, maxTierIndex);
        int maxRewardTier = Mathf.Clamp(requiredTierIndex + 4, minRewardTier, maxTierIndex);

        List<CropData> candidates = new List<CropData>();
        for (int i = 0; i < cropPool.Count; i++)
        {
            CropData c = cropPool[i];
            int t = (int)c.tier;
            if (t >= minRewardTier && t <= maxRewardTier)
            {
                candidates.Add(c);
            }
        }

        if (candidates.Count == 0)
        {
            int highestTier = -1;
            for (int i = 0; i < cropPool.Count; i++)
            {
                highestTier = Mathf.Max(highestTier, (int)cropPool[i].tier);
            }

            for (int i = 0; i < cropPool.Count; i++)
            {
                if ((int)cropPool[i].tier == highestTier)
                {
                    candidates.Add(cropPool[i]);
                }
            }
        }

        return candidates.Count > 0 ? candidates[rng.Next(candidates.Count)] : null;
    }

    private int RoundByStep(int value)
    {
        if (value < 300)
            return Mathf.RoundToInt(value / 10f) * 10;

        if (value < 1400)
            return Mathf.RoundToInt(value / 25f) * 25;

        return Mathf.RoundToInt(value / 50f) * 50;
    }

    private CropData PickRequiredCropForPureOffer(List<CropData> cropPool, int maxTierIndex, int minFromTop, int maxFromTop, System.Random rng)
    {
        if (cropPool == null || cropPool.Count == 0)
            return null;

        int minTier = Mathf.Clamp(maxTierIndex - maxFromTop, 0, maxTierIndex);
        int maxTier = Mathf.Clamp(maxTierIndex - minFromTop, minTier, maxTierIndex);

        List<CropData> candidates = new List<CropData>();
        for (int i = 0; i < cropPool.Count; i++)
        {
            CropData crop = cropPool[i];
            int t = (int)crop.tier;
            if (t >= minTier && t <= maxTier)
            {
                candidates.Add(crop);
            }
        }

        if (candidates.Count == 0)
            candidates.AddRange(cropPool);

        return candidates[rng.Next(candidates.Count)];
    }

    private void AddPureRewardOffer(
        List<CropData> cropPool,
        DateTime now,
        DateTime expiresAt,
        System.Random rng,
        int offerIndex,
        string offerTag,
        int rewardType,
        int maxTierIndex)
    {
        CropData requiredCrop = PickRequiredCropForPureOffer(cropPool, maxTierIndex, 1, 4, rng);
        if (requiredCrop == null)
            return;

        int tierIndex = (int)requiredCrop.tier;
        int requiredAmount = Mathf.RoundToInt((170 + rng.Next(60, 180)) * Mathf.Pow(1.16f, tierIndex));
        requiredAmount = Mathf.Max(90, RoundByStep(requiredAmount));

        int rewardPoints = 0;
        int rewardGems = 0;
        int rewardCoins = 0;

        switch (rewardType)
        {
            // Gem only
            case 0:
                rewardGems = Mathf.Max(4, Mathf.RoundToInt(6f + tierIndex * 1.4f + requiredAmount / 260f));
                break;

            // Coin only
            case 1:
                rewardCoins = Mathf.Max(800, Mathf.RoundToInt(requiredAmount * (9.5f + tierIndex * 1.35f)));
                break;

            // RP only
            default:
                rewardPoints = Mathf.Max(10, Mathf.RoundToInt((requiredAmount / 60f) * (1.25f + tierIndex * 0.14f)));
                break;
        }

        FactoryOfferData offer = new FactoryOfferData
        {
            offerId = now.Ticks + "_" + offerTag + "_" + offerIndex + "_" + requiredCrop.itemName,
            cropName = requiredCrop.itemName,
            requiredAmount = requiredAmount,
            rewardResearchPoints = rewardPoints,
            rewardGems = rewardGems,
            rewardCoins = rewardCoins,
            rewardCropName = string.Empty,
            rewardCropAmount = 0,
            expiresAtTicks = expiresAt.Ticks,
            completed = false
        };

        _offers.Add(offer);
    }

    private void GenerateNewOffers()
    {
        if (!CanGenerateOffers())
            return;

        _offers.Clear();

        List<CropData> cropPool = new List<CropData>(GameContentGenerator.Instance.AllCrops);
        if (cropPool.Count == 0)
            return;

        int maxTierIndex = 0;
        for (int i = 0; i < cropPool.Count; i++)
        {
            maxTierIndex = Mathf.Max(maxTierIndex, (int)cropPool[i].tier);
        }

        DateTime now = DateTime.UtcNow;
        DateTime expiresAt = now.AddHours(OFFER_DURATION_HOURS);

        int rotationBucket = now.Year * 1000 + now.DayOfYear;
        System.Random rng = new System.Random(rotationBucket);

        int randomOfferCount = Mathf.Max(0, OFFER_COUNT - PURE_REWARD_OFFER_COUNT);

        for (int i = 0; i < randomOfferCount; i++)
        {
            CropData crop = cropPool[rng.Next(cropPool.Count)];
            int tierIndex = (int)crop.tier;

            float requirementScale = Mathf.Pow(1.2f, tierIndex);
            int requiredAmount = Mathf.RoundToInt((130 + rng.Next(40, 190)) * requirementScale);
            requiredAmount = Mathf.Max(60, RoundByStep(requiredAmount));

            int baseRewardPoints = Mathf.Max(2, Mathf.RoundToInt((requiredAmount / 95f) * (1f + tierIndex * 0.18f)));
            int baseRewardGems = Mathf.Max(1, Mathf.RoundToInt((requiredAmount / 230f) + tierIndex * 0.5f));

            int rewardPoints = 0;
            int rewardGems = 0;
            int rewardCoins = 0;
            string rewardCropName = string.Empty;
            int rewardCropAmount = 0;

            int profile = RollRewardProfile(tierIndex, rng);

            bool includeRP = profile == 0 || profile == 3 || profile == 4 || profile == 6;
            bool includeGem = profile == 1 || profile == 3 || profile == 5 || profile == 6;
            bool includeCrop = profile == 2 || profile == 4 || profile == 5 || profile == 6;

            if (includeRP)
            {
                rewardPoints = baseRewardPoints * (profile == 6 ? 2 : 1);
            }

            if (includeGem)
            {
                rewardGems = baseRewardGems * ((profile == 5 || profile == 6) ? 2 : 1);
            }

            if (includeCrop)
            {
                CropData rewardCrop = PickRewardCrop(cropPool, tierIndex, maxTierIndex, rng);
                if (rewardCrop != null)
                {
                    rewardCropName = rewardCrop.itemName;

                    int rewardTierIndex = (int)rewardCrop.tier;
                    int baseAmount = Mathf.Clamp(6 - rewardTierIndex / 2, 1, 6);
                    if (profile == 6) baseAmount += 1;
                    rewardCropAmount = Mathf.Clamp(baseAmount + rng.Next(-1, 2), 1, 7);
                }
            }

            if (rewardPoints <= 0 && rewardGems <= 0 && rewardCropAmount <= 0)
            {
                rewardPoints = Mathf.Max(2, baseRewardPoints / 2);
            }

            FactoryOfferData offer = new FactoryOfferData
            {
                offerId = now.Ticks + "_" + i + "_" + crop.itemName,
                cropName = crop.itemName,
                requiredAmount = requiredAmount,
                rewardResearchPoints = rewardPoints,
                rewardGems = rewardGems,
                rewardCoins = rewardCoins,
                rewardCropName = rewardCropName,
                rewardCropAmount = rewardCropAmount,
                expiresAtTicks = expiresAt.Ticks,
                completed = false
            };

            _offers.Add(offer);
        }

        AddPureRewardOffer(cropPool, now, expiresAt, rng, randomOfferCount, "pure_gem", 0, maxTierIndex);
        AddPureRewardOffer(cropPool, now, expiresAt, rng, randomOfferCount + 1, "pure_coin", 1, maxTierIndex);
        AddPureRewardOffer(cropPool, now, expiresAt, rng, randomOfferCount + 2, "pure_rp", 2, maxTierIndex);

        Save();
        OnOffersChanged?.Invoke();
        GameMessageManager.Instance?.PushMessage("Yeni fabrika teklifleri geldi.");
    }

    private void Save()
    {
        OfferSaveWrapper wrapper = new OfferSaveWrapper { offers = _offers };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(OFFERS_SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        _offers.Clear();

        if (!PlayerPrefs.HasKey(OFFERS_SAVE_KEY))
            return;

        string json = PlayerPrefs.GetString(OFFERS_SAVE_KEY, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        OfferSaveWrapper wrapper = JsonUtility.FromJson<OfferSaveWrapper>(json);
        if (wrapper != null && wrapper.offers != null)
        {
            _offers.AddRange(wrapper.offers);
        }
    }
}
