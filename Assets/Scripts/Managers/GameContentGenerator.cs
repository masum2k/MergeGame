using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// A utility that generates all game content (Crops, Boosts, Chests) at runtime
/// to fulfill the "Zero-Touch" requirement.
/// </summary>
public class GameContentGenerator : MonoBehaviour
{
    public static GameContentGenerator Instance { get; private set; }
    public List<CropData> AllCrops = new List<CropData>();
    public List<BoostData> AllBoosts = new List<BoostData>();
    private Sprite _defaultSprite;
    private Sprite[] _resourceCropSprites;
    private readonly Dictionary<string, Sprite> _resolvedCropSpriteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GenerateContent();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    
    public CropData GetCropByTier(CropTier tier)
    {
        return AllCrops.Find(c => c.tier == tier);
    }

    public CropData GetCropByName(string cropName)
    {
        if (string.IsNullOrWhiteSpace(cropName))
            return null;

        return AllCrops.Find(c => c != null && c.itemName == cropName);
    }

    public BoostData GetBoostByName(string boostName)
    {
        if (string.IsNullOrWhiteSpace(boostName))
            return null;

        return AllBoosts.Find(b => b != null && b.itemName == boostName);
    }

    private void EnsureDefaultSprite()
    {
        if (_defaultSprite != null) return;
        
        Texture2D tex = new Texture2D(2, 2);
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        
        _defaultSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    private void GenerateContent()
    {
        EnsureDefaultSprite();

        // 1. Generate Crops (large pool across many tiers)
        AllCrops.Clear();
        string[] names =
        {
            "Havuc", "Domates", "Misir", "Patates",
            "Balkabagi", "Karpuz", "Salatalik", "Biber",
            "Patlican", "Kabak", "Sogan", "Sarimsak",
            "Ispanak", "Brokoli", "Karnabahar", "Pirasa",
            "Fasulye", "Bezelye", "Nohut", "Mercimek",
            "Bugday", "Arpa", "Yulaf", "Pirinc",
            "Kinoa", "Avokado", "Elma", "Armut",
            "Portakal", "Limon", "Muz", "Ananas",
            "Mango", "HindistanCevizi", "Nar", "Incir",
            "Ahududu", "YabanMersini", "BozMersin", "AciBiber",
            "EjderMeyvesi", "YildizMeyvesi", "Guava", "Acai",
            "AltinTruf", "KristalUzum", "AuroraErigi", "KozmikMantar"
        };

        Color[] tierPalette =
        {
            new Color(0.72f, 0.85f, 0.36f),
            new Color(0.84f, 0.78f, 0.26f),
            new Color(0.94f, 0.62f, 0.25f),
            new Color(0.88f, 0.46f, 0.24f),
            new Color(0.78f, 0.36f, 0.74f),
            new Color(0.54f, 0.44f, 0.94f),
            new Color(0.38f, 0.58f, 1f),
            new Color(0.24f, 0.75f, 0.92f),
            new Color(0.2f, 0.86f, 0.78f),
            new Color(0.36f, 0.9f, 0.54f),
            new Color(0.78f, 0.94f, 0.42f),
            new Color(1f, 0.92f, 0.58f)
        };

        int maxTierIndex = (int)CropTier.Primordial;
        int cropsPerTier = 4;

        CropData prev = null;
        int missingCropSpriteCount = 0;
        for (int i = 0; i < names.Length; i++)
        {
            CropData c = ScriptableObject.CreateInstance<CropData>();
            c.itemName = names[i];

            int tierIndex = Mathf.Clamp(i / cropsPerTier, 0, maxTierIndex);
            int localIndex = i % cropsPerTier;

            Color tierColor = tierPalette[Mathf.Clamp(tierIndex, 0, tierPalette.Length - 1)];
            c.itemColor = Color.Lerp(tierColor, Color.white, localIndex * 0.16f);

            // Prefer packaged crop art if available; fallback to generated placeholder.
            Sprite cropSprite = ResolveCropSprite(c.itemName, tierIndex, localIndex);
            if (cropSprite == null)
            {
                missingCropSpriteCount++;
            }

            c.icon = cropSprite != null ? cropSprite : _defaultSprite;
            c.tier = (CropTier)tierIndex;
            c.description = BuildAutoDescription(c.itemName, tierIndex, localIndex);

            float tierBase = 1.2f * Mathf.Pow(1.68f, tierIndex);
            float localFactor = 1f + (localIndex * 0.22f);
            c.coinPerTick = Mathf.Round((tierBase * localFactor * 0.1f) * 100f) / 100f;

            AllCrops.Add(c);

            if (prev != null) prev.nextLevelCrop = c;
            prev = c;
        }

        if (prev != null)
        {
            prev.nextLevelCrop = null;
        }

        if (missingCropSpriteCount > 0)
        {
            Debug.LogWarning("GameContentGenerator: " + missingCropSpriteCount + " crop sprite missing, default placeholders are used.");
        }

        // 2. Generate Boosts
        AllBoosts.Clear();

        BoostData incomeBoost = ScriptableObject.CreateInstance<BoostData>();
        incomeBoost.itemName = "2x Kazanc Iksiri";
        incomeBoost.description = "1 dakika boyunca kazanci ikiye katlar.";
        incomeBoost.type = BoostType.CoinMultiplier;
        incomeBoost.multiplier = 2.0f;
        incomeBoost.durationSeconds = 60f;
        incomeBoost.itemColor = Color.cyan;

        Sprite boostSprite = Resources.Load<Sprite>("Boosts/spr_boost_2x_kazanc_iksiri");
        incomeBoost.icon = boostSprite != null ? boostSprite : _defaultSprite;

        AllBoosts.Add(incomeBoost);

        // 3. Generate Chests
        if (CrateManager.Instance != null)
        {
            CrateManager.Instance.AllCrates.Clear();
        }

        CreateChestFromTierRange("Gunluk Sandik", 0, CurrencyType.Coin, CrateRarity.Daily, 0, 7, 10, false, incomeBoost, 6f);
        CreateChestFromTierRange("Bronz Sandik", 80, CurrencyType.Coin, CrateRarity.Bronze, 0, 2, 10, false);
        CreateChestFromTierRange("Gumus Sandik", 300, CurrencyType.Coin, CrateRarity.Silver, 1, 4, 10, false, incomeBoost, 4f);
        CreateChestFromTierRange("Altin Sandik", 110, CurrencyType.Gem, CrateRarity.Gold, 3, 7, 10, true, incomeBoost, 5f);
        CreateChestFromTierRange("Elmas Sandik", 260, CurrencyType.Gem, CrateRarity.Diamond, 6, 10, 9, true, incomeBoost, 6f);
        CreateChestFromTierRange("Usta Sandik", 1700, CurrencyType.Coin, CrateRarity.Gold, 5, 9, 11, true, incomeBoost, 5f);
        CreateChestFromTierRange("Kozmik Sandik", 600, CurrencyType.Gem, CrateRarity.Diamond, 8, 11, 8, true, incomeBoost, 7f);
    }

    private void CreateChestFromTierRange(
        string name,
        int cost,
        CurrencyType currencyType,
        CrateRarity rarity,
        int minTier,
        int maxTier,
        int maxCropEntries,
        bool preferHighTier,
        BoostData bonusBoost = null,
        float bonusBoostWeight = 0f)
    {
        object[] drops = BuildDropTable(minTier, maxTier, maxCropEntries, preferHighTier, bonusBoost, bonusBoostWeight);
        if (drops == null || drops.Length == 0)
            return;

        CreateChest(name, cost, currencyType, rarity, drops);
    }

    private object[] BuildDropTable(
        int minTier,
        int maxTier,
        int maxCropEntries,
        bool preferHighTier,
        BoostData bonusBoost,
        float bonusBoostWeight)
    {
        List<CropData> pool = new List<CropData>();

        int maxAllowedTier = (int)CropTier.Primordial;
        int safeMin = Mathf.Clamp(minTier, 0, maxAllowedTier);
        int safeMax = Mathf.Clamp(maxTier, safeMin, maxAllowedTier);

        for (int i = 0; i < AllCrops.Count; i++)
        {
            CropData crop = AllCrops[i];
            int tierIndex = (int)crop.tier;
            if (tierIndex >= safeMin && tierIndex <= safeMax)
            {
                pool.Add(crop);
            }
        }

        if (pool.Count == 0)
            return new object[0];

        int desiredEntries = Mathf.Clamp(maxCropEntries, 1, pool.Count);
        int stride = Mathf.Max(1, Mathf.FloorToInt((float)pool.Count / desiredEntries));

        List<object> entries = new List<object>();
        int added = 0;

        for (int i = 0; i < pool.Count && added < desiredEntries; i += stride)
        {
            CropData crop = pool[i];
            int tierIndex = (int)crop.tier;

            float weightCore = preferHighTier
                ? (3f + (tierIndex - safeMin + 1) * 2.1f)
                : (3f + (safeMax - tierIndex + 1) * 1.9f);

            float weight = Mathf.Max(1f, weightCore + (added % 3) * 0.4f);

            entries.Add(crop);
            entries.Add(weight);
            added++;
        }

        int tailIndex = pool.Count - 1;
        while (added < desiredEntries && tailIndex >= 0)
        {
            CropData crop = pool[tailIndex];
            float weight = preferHighTier ? 10f : 5f;

            entries.Add(crop);
            entries.Add(weight);

            tailIndex--;
            added++;
        }

        if (bonusBoost != null && bonusBoostWeight > 0f)
        {
            entries.Add(bonusBoost);
            entries.Add(bonusBoostWeight);
        }

        return entries.ToArray();
    }

    private void CreateChest(string name, int cost, CurrencyType cur, CrateRarity rarity, params object[] dropsAndWeights)
    {
        CrateData crate = ScriptableObject.CreateInstance<CrateData>();
        crate.crateName = name;
        crate.cost = cost;
        crate.currencyType = cur;
        crate.rarity = rarity;

        List<CrateDropEntry> entries = new List<CrateDropEntry>();
        for (int i = 0; i < dropsAndWeights.Length; i += 2)
        {
            CrateDropEntry entry = new CrateDropEntry();
            object item = dropsAndWeights[i];
            float weight = System.Convert.ToSingle(dropsAndWeights[i + 1]);
            
            if (item is CropData crop) entry.crop = crop;
            else if (item is BoostData boost) entry.boost = boost;
            
            entry.weight = weight;
            entries.Add(entry);
        }
        crate.drops = entries.ToArray();

        if (CrateManager.Instance != null)
        {
            CrateManager.Instance.AllCrates.Add(crate);
        }
    }

    private string BuildAutoDescription(string cropName, int tierIndex, int localIndex)
    {
        string[] tone =
        {
            "Baslangic seviyesinde stabil uretim saglar.",
            "Dengeli bir orta hat urunudur.",
            "Depoda tutuldugunda pasif akisi hissedilir sekilde arttirir.",
            "Yuksek verim grubunda yer alir ve uzun oyunda deger kazanir."
        };

        int tierNo = tierIndex + 1;
        string detail = tone[Mathf.Clamp(localIndex, 0, tone.Length - 1)];
        return cropName + " T" + tierNo + " besinidir. " + detail;
    }

    private Sprite ResolveCropSprite(string cropName, int tierIndex, int localIndex)
    {
        if (string.IsNullOrWhiteSpace(cropName))
            return null;

        string normalizedCropName = NormalizeAssetToken(cropName);
        if (_resolvedCropSpriteCache.TryGetValue(normalizedCropName, out Sprite cached))
            return cached;

        // Primary path: deterministic name (spr_crop_tXX_slot_name).
        string expectedPath = "Crops/spr_crop_t" + tierIndex.ToString("00") + "_" + localIndex + "_" + normalizedCropName;
        Sprite exact = Resources.Load<Sprite>(expectedPath);
        if (exact != null)
        {
            _resolvedCropSpriteCache[normalizedCropName] = exact;
            return exact;
        }

        // Secondary path: fuzzy name match over all delivered crop sprites.
        if (_resourceCropSprites == null)
        {
            _resourceCropSprites = Resources.LoadAll<Sprite>("Crops");
        }

        Sprite best = null;
        for (int i = 0; i < _resourceCropSprites.Length; i++)
        {
            Sprite candidate = _resourceCropSprites[i];
            if (candidate == null)
                continue;

            string candidateKey = NormalizeAssetToken(candidate.name);
            if (candidateKey == normalizedCropName ||
                candidateKey.EndsWith(normalizedCropName) ||
                candidateKey.Contains(normalizedCropName))
            {
                best = candidate;
                break;
            }
        }

        // Final fallback: reuse closest lower-tier sprite that shares the same local slot index.
        if (best == null)
        {
            for (int fallbackTier = tierIndex - 1; fallbackTier >= 0 && best == null; fallbackTier--)
            {
                string slotPrefix = "sprcropt" + fallbackTier.ToString("00") + localIndex;
                for (int i = 0; i < _resourceCropSprites.Length; i++)
                {
                    Sprite candidate = _resourceCropSprites[i];
                    if (candidate == null)
                        continue;

                    string candidateKey = NormalizeAssetToken(candidate.name);
                    if (candidateKey.StartsWith(slotPrefix))
                    {
                        best = candidate;
                        break;
                    }
                }
            }
        }

        _resolvedCropSpriteCache[normalizedCropName] = best;
        return best;
    }

    private string NormalizeAssetToken(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        StringBuilder sb = new StringBuilder(value.Length);
        string lower = value.ToLowerInvariant();

        for (int i = 0; i < lower.Length; i++)
        {
            char ch = lower[i];
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
