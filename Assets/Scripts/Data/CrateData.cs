using UnityEngine;

public enum CurrencyType { Coin, Gem }
public enum CrateRarity { Bronze, Silver, Gold, Diamond, Daily }

/// <summary>
/// Represents a single entry in a crate's drop table.
/// Can drop either a Crop or a Boost.
/// </summary>
[System.Serializable]
public class CrateDropEntry
{
    public CropData crop;
    public BoostData boost;
    public float weight; // Higher weight = more likely to drop
}

/// <summary>
/// Defines a crate that can be purchased from the Market.
/// Contains a name, cost, currency type, and a weighted drop table.
/// </summary>
[CreateAssetMenu(fileName = "NewCrateData", menuName = "IdleMerge/Crate Data")]
public class CrateData : ScriptableObject
{
    public string crateName;
    public int cost;
    public CurrencyType currencyType = CurrencyType.Coin;
    public CrateRarity rarity = CrateRarity.Bronze;
    public CrateDropEntry[] drops;

    /// <summary>
    /// Picks a random item from the drop table using weighted random selection.
    /// Returns an object that can be either CropData or BoostData.
    /// </summary>
    public object RollDrop()
    {
        if (drops == null || drops.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in drops)
        {
            totalWeight += entry.weight;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in drops)
        {
            cumulative += entry.weight;
            if (roll <= cumulative)
            {
                if (entry.boost != null) return entry.boost;
                return entry.crop;
            }
        }

        // Fallback
        var last = drops[drops.Length - 1];
        if (last.boost != null) return last.boost;
        return last.crop;
    }
}
