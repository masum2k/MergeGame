using UnityEngine;

/// <summary>
/// Represents a single entry in a crate's drop table.
/// </summary>
[System.Serializable]
public class CrateDropEntry
{
    public CropData crop;
    public float weight; // Higher weight = more likely to drop
}

/// <summary>
/// Defines a crate that can be purchased from the Market.
/// Contains a name, cost, and a weighted drop table.
/// </summary>
public class CrateData
{
    public string crateName;
    public int cost;
    public CrateDropEntry[] drops;

    /// <summary>
    /// Picks a random crop from the drop table using weighted random selection.
    /// </summary>
    public CropData RollDrop()
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
                return entry.crop;
            }
        }

        // Fallback to last entry
        return drops[drops.Length - 1].crop;
    }
}
