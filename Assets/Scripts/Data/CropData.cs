using UnityEngine;

[CreateAssetMenu(fileName = "NewCropData", menuName = "IdleMerge/Crop Data")]
public class CropData : BaseItemData
{
    // Inherited from BaseItemData:
    // itemName (replaces cropName)
    // icon (replaces cropSprite)
    // itemColor (replaces cropColor)

    [Header("Progression")]
    public CropTier tier;
    
    [Tooltip("The crop this merges into.")]
    public CropData nextLevelCrop;

    [Header("Economy")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Amount of coin this crop generates per production tick/cycle.")]
    public float coinPerTick;

    // Compatibility properties for older scripts
    public string cropName => itemName;
    public Sprite cropSprite => icon;
    public Color cropColor => itemColor;
}
