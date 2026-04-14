using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton inventory manager. Holds items (Crops/Boosts) that the player owns.
/// Uses a Dictionary internally: itemName → (BaseItemData reference, count).
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _isShuttingDown = false;
    }

    public static InventoryManager Instance
    {
        get
        {
            if (_isShuttingDown)
            {
                return _instance;
            }

            if (_instance == null)
            {
                _instance = FindAnyObjectByType<InventoryManager>();
                if (_instance == null)
                {
                    if (!Application.isPlaying)
                    {
                        return null;
                    }

                    GameObject go = new GameObject("InventoryManager_Auto");
                    _instance = go.AddComponent<InventoryManager>();
                }
            }
            return _instance;
        }
    }
    private static InventoryManager _instance;
    private static bool _isShuttingDown;

    private const string INVENTORY_SAVE_KEY = "InventoryDataV1";

    [Serializable]
    private class InventorySaveEntry
    {
        public string itemName;
        public int count;
    }

    [Serializable]
    private class InventorySaveWrapper
    {
        public List<InventorySaveEntry> entries = new List<InventorySaveEntry>();
    }

    /// <summary>
    /// Fired whenever any item is added or removed. Listeners should refresh their UI.
    /// </summary>
    public event Action OnInventoryChanged;

    // Internal storage: itemName → InventoryEntry
    private readonly Dictionary<string, InventoryEntry> _items = new Dictionary<string, InventoryEntry>();

    private class InventoryEntry
    {
        public BaseItemData itemData;
        public int count;
    }

    private void Awake()
    {
        _isShuttingDown = false;

        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _isShuttingDown = true;
            _instance = null;
        }
    }

    /// <summary>
    /// Add one unit of an item to the inventory.
    /// </summary>
    public void AddItem(BaseItemData item)
    {
        if (item == null) return;

        if (_items.TryGetValue(item.itemName, out InventoryEntry existing))
        {
            existing.count++;
            if (existing.itemData == null)
            {
                existing.itemData = item;
            }
        }
        else
        {
            _items[item.itemName] = new InventoryEntry { itemData = item, count = 1 };
        }

        Save();
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Remove one unit of an item from the inventory. Returns true if successful.
    /// </summary>
    public bool RemoveItem(BaseItemData item)
    {
        if (item == null) return false;

        if (_items.TryGetValue(item.itemName, out InventoryEntry entry) && entry.count > 0)
        {
            entry.count--;
            if (entry.count <= 0)
            {
                _items.Remove(item.itemName);
            }

            Save();
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
        if (_items.TryGetValue(itemName, out InventoryEntry entry))
        {
            return entry.count;
        }
        return 0;
    }

    /// <summary>
    /// Removes a specific amount of an item by name.
    /// Returns true if enough stock existed and removal succeeded.
    /// </summary>
    public bool TryRemoveItemByName(string itemName, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemName) || amount <= 0)
            return false;

        if (!_items.TryGetValue(itemName, out InventoryEntry entry))
            return false;

        if (entry.count < amount)
            return false;

        entry.count -= amount;
        if (entry.count <= 0)
        {
            _items.Remove(itemName);
        }

        Save();
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Returns all unique items currently in the inventory (count > 0).
    /// </summary>
    public List<BaseItemData> GetAllOwnedItems()
    {
        List<BaseItemData> result = new List<BaseItemData>();
        foreach (var kvp in _items)
        {
            TryResolveItemData(kvp.Key, kvp.Value);

            if (kvp.Value.count > 0)
            {
                if (kvp.Value.itemData != null)
                {
                    result.Add(kvp.Value.itemData);
                }
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
            TryResolveItemData(kvp.Key, kvp.Value);

            if (kvp.Value.itemData is T item && kvp.Value.count > 0)
            {
                result.Add(item);
            }
        }
        return result;
    }

    private void Save()
    {
        InventorySaveWrapper wrapper = new InventorySaveWrapper();

        foreach (var kvp in _items)
        {
            int count = Mathf.Max(0, kvp.Value.count);
            if (count <= 0)
                continue;

            wrapper.entries.Add(new InventorySaveEntry
            {
                itemName = kvp.Key,
                count = count
            });
        }

        string json = JsonUtility.ToJson(wrapper);
        SecurePlayerPrefs.SetString(INVENTORY_SAVE_KEY, json);
        SaveCoordinator.MarkDirty();
    }

    private void Load()
    {
        _items.Clear();

        string json = SecurePlayerPrefs.GetString(INVENTORY_SAVE_KEY, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return;

        InventorySaveWrapper wrapper = JsonUtility.FromJson<InventorySaveWrapper>(json);
        if (wrapper == null || wrapper.entries == null)
            return;

        for (int i = 0; i < wrapper.entries.Count; i++)
        {
            InventorySaveEntry saveEntry = wrapper.entries[i];
            if (saveEntry == null || string.IsNullOrWhiteSpace(saveEntry.itemName))
                continue;

            int safeCount = Mathf.Max(0, saveEntry.count);
            if (safeCount <= 0)
                continue;

            _items[saveEntry.itemName] = new InventoryEntry
            {
                itemData = ResolveItemDataByName(saveEntry.itemName),
                count = safeCount
            };
        }
    }

    private bool TryResolveItemData(string itemName, InventoryEntry entry)
    {
        if (entry == null)
            return false;

        if (entry.itemData != null)
            return true;

        entry.itemData = ResolveItemDataByName(itemName);
        return entry.itemData != null;
    }

    private BaseItemData ResolveItemDataByName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName) || GameContentGenerator.Instance == null)
            return null;

        CropData crop = GameContentGenerator.Instance.GetCropByName(itemName);
        if (crop != null)
            return crop;

        BoostData boost = GameContentGenerator.Instance.GetBoostByName(itemName);
        if (boost != null)
            return boost;

        return null;
    }
}
