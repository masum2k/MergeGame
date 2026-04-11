using UnityEngine;

public abstract class BaseItemData : ScriptableObject
{
    [Header("Base Item Info")]
    public string itemName;
    public Sprite icon;
    public Color itemColor = Color.white;
}
