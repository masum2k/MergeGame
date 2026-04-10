using UnityEngine;

[CreateAssetMenu(fileName = "NewCropData", menuName = "IdleMerge/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Crop Information")]
    public string cropName;
    
    [Tooltip("The visual representation of this crop.")]
    public Sprite cropSprite;

    [Tooltip("Visual color tint for the crop.")]
    public Color cropColor = Color.white;

    [Header("Progression")]
    public CropTier tier;
    
    [Tooltip("The crop this merges into.")]
    public CropData nextLevelCrop;

    [Header("Economy")]
    [Tooltip("Amount of coin this crop generates per production tick/cycle.")]
    public float coinPerTick;
}
