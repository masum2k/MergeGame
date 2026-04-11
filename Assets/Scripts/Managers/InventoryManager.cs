using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton inventory manager. Holds crops that the player owns but hasn't placed on the grid yet.
/// Uses a Dictionary internally: cropName → (CropData reference, count).
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

    // Internal storage: cropName → InventoryEntry
    private Dictionary<string, InventoryEntry> _items = new Dictionary<string, InventoryEntry>();

    private class InventoryEntry
    {
        public CropData cropData;
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
    /// Add one unit of a crop to the inventory.
    /// </summary>
    public void AddItem(CropData crop)
    {
        if (crop == null) return;

        if (_items.ContainsKey(crop.cropName))
        {
            _items[crop.cropName].count++;
        }
        else
        {
            _items[crop.cropName] = new InventoryEntry { cropData = crop, count = 1 };
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Remove one unit of a crop from the inventory. Returns true if successful.
    /// </summary>
    public bool RemoveItem(CropData crop)
    {
        if (crop == null) return false;

        if (_items.ContainsKey(crop.cropName) && _items[crop.cropName].count > 0)
        {
            _items[crop.cropName].count--;
            if (_items[crop.cropName].count <= 0)
            {
                _items.Remove(crop.cropName);
            }
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get the count of a specific crop in the inventory.
    /// </summary>
    public int GetCount(string cropName)
    {
        if (_items.ContainsKey(cropName))
        {
            return _items[cropName].count;
        }
        return 0;
    }

    /// <summary>
    /// Returns all unique crops currently in the inventory (count > 0).
    /// </summary>
    public List<CropData> GetAllOwnedCrops()
    {
        List<CropData> result = new List<CropData>();
        foreach (var kvp in _items)
        {
            if (kvp.Value.count > 0)
            {
                result.Add(kvp.Value.cropData);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns total number of items in inventory.
    /// </summary>
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var kvp in _items)
        {
            total += kvp.Value.count;
        }
        return total;
    }
}
