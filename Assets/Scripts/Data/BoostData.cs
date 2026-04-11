using UnityEngine;

public enum BoostType { CoinMultiplier, MergeXPBoost }

[CreateAssetMenu(fileName = "NewBoostData", menuName = "IdleMerge/Boost Data")]
public class BoostData : BaseItemData
{
    // Inherited: itemName, icon, itemColor
    
    public string description;
    
    [Header("Effect")]
    public BoostType type = BoostType.CoinMultiplier;
    public float multiplier = 2.0f;
    public float durationSeconds = 60.0f;

    public string boostName => itemName;
}
