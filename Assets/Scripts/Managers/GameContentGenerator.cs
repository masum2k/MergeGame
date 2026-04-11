using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A utility that generates all game content (Crops, Boosts, Chests) at runtime
/// to fulfill the "Zero-Touch" requirement.
/// </summary>
public class GameContentGenerator : MonoBehaviour
{
    public static GameContentGenerator Instance { get; private set; }
    private Sprite _defaultSprite;

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
        
        // 1. Generate Crops (T1 to T5)
        List<CropData> crops = new List<CropData>();
        string[] names = { "Havuç", "Domates", "Mısır", "Balkabağı", "Karpuz" };
        Color[] colors = { Color.orange, Color.red, Color.yellow, new Color(1, 0.5f, 0), Color.green };

        CropData prev = null;
        for (int i = 0; i < 5; i++)
        {
            CropData c = ScriptableObject.CreateInstance<CropData>();
            c.itemName = names[i];
            c.itemColor = colors[i];
            c.icon = _defaultSprite;
            c.tier = (CropTier)i;
            c.coinPerTick = (i + 1) * 2f;
            crops.Add(c);
            
            if (prev != null) prev.nextLevelCrop = c;
            prev = c;
        }

        // 2. Generate Boosts
        BoostData incomeBoost = ScriptableObject.CreateInstance<BoostData>();
        incomeBoost.itemName = "2x Kazanç İksiri";
        incomeBoost.description = "1 dakika boyunca kazancı ikiye katlar.";
        incomeBoost.type = BoostType.CoinMultiplier;
        incomeBoost.multiplier = 2.0f;
        incomeBoost.durationSeconds = 60f;
        incomeBoost.itemColor = Color.cyan;
        incomeBoost.icon = _defaultSprite;

        // 3. Generate Chests
        CreateChest("Günlük Sandık", 0, CurrencyType.Coin, CrateRarity.Daily, crops[0], 90, incomeBoost, 10);
        CreateChest("Bronz Sandık", 10, CurrencyType.Coin, CrateRarity.Bronze, crops[0], 100);
        CreateChest("Gümüş Sandık", 50, CurrencyType.Coin, CrateRarity.Silver, crops[0], 70, crops[1], 30);
        CreateChest("Altın Sandık", 20, CurrencyType.Gem, CrateRarity.Gold, crops[1], 60, crops[2], 30, incomeBoost, 10);
        CreateChest("Elmas Sandık", 100, CurrencyType.Gem, CrateRarity.Diamond, crops[2], 50, crops[3], 40, crops[4], 10);
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
}
