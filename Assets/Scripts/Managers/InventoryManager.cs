using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton inventory manager. Holds items (Crops/Boosts) that the player owns.
/// Uses a Dictionary internally: itemName → (BaseItemData reference, count).
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<InventoryManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("InventoryManager_Auto");
                    _instance = go.AddComponent<InventoryManager>();
                }
            }
            return _instance;
        }
    }
    private static InventoryManager _instance;

    /// <summary>
    /// Fired whenever any item is added or removed. Listeners should refresh their UI.
    /// </summary>
    public event Action OnInventoryChanged;

    // Internal storage: itemName → InventoryEntry
    private Dictionary<string, InventoryEntry> _items = new Dictionary<string, InventoryEntry>();

    private class InventoryEntry
    {
        public BaseItemData itemData;
        public int count;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Add one unit of an item to the inventory.
    /// </summary>
    public void AddItem(BaseItemData item)
    {
        if (item == null) return;

        if (_items.ContainsKey(item.itemName))
        {
            _items[item.itemName].count++;
        }
        else
        {
            _items[item.itemName] = new InventoryEntry { itemData = item, count = 1 };
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Remove one unit of an item from the inventory. Returns true if successful.
    /// </summary>
    public bool RemoveItem(BaseItemData item)
    {
        if (item == null) return false;

        if (_items.ContainsKey(item.itemName) && _items[item.itemName].count > 0)
        {
            _items[item.itemName].count--;
            if (_items[item.itemName].count <= 0)
            {
                _items.Remove(item.itemName);
            }
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get the count of a specific item in the inventory.
    /// </summary>
    public int GetCount(string itemName)
    {
        if (_items.ContainsKey(itemName))
        {
            return _items[itemName].count;
        }
        return 0;
    }

    /// <summary>
    /// Returns all unique items currently in the inventory (count > 0).
    /// </summary>
    public List<BaseItemData> GetAllOwnedItems()
    {
        List<BaseItemData> result = new List<BaseItemData>();
        foreach (var kvp in _items)
        {
            if (kvp.Value.count > 0)
            {
                result.Add(kvp.Value.itemData);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns all items of a specific type.
    /// </summary>
    public List<T> GetItemsOfType<T>() where T : BaseItemData
    {
        List<T> result = new List<T>();
        foreach (var kvp in _items)
        {
            if (kvp.Value.itemData is T item && kvp.Value.count > 0)
            {
                result.Add(item);
            }
        }
        return result;
    }
}
