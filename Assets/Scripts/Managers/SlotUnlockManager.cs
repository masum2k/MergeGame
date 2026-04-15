using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages which grid slots are unlocked (playable) vs locked.
/// Locked slots cannot hold crops and are visually darkened.
/// Players can spend coins to unlock adjacent locked slots.
/// </summary>
public class SlotUnlockManager : MonoBehaviour
{
    public static SlotUnlockManager Instance { get; private set; }

    /// <summary>
    /// Fired when any slot is unlocked. Passes the (x, y) of the newly unlocked slot.
    /// </summary>
    public event Action<int, int> OnSlotUnlocked;

    // All unlocked slot coordinates
    private HashSet<Vector2Int> _unlockedSlots = new HashSet<Vector2Int>();

    // Optional bounds for the maximum unlockable area.
    private bool _hasUnlockLimits;
    private int _minUnlockX = int.MinValue;
    private int _maxUnlockX = int.MaxValue;
    private int _minUnlockY = int.MinValue;
    private int _maxUnlockY = int.MaxValue;

    // Center of the initial unlocked region (used for distance-based pricing)
    private Vector2 _unlockCenter;

    // Base cost to unlock a slot; scales with distance from center
    private const int BASE_UNLOCK_COST = 50;
    private const float COST_PER_DISTANCE = 15f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        Load();
    }

    private void Save()
    {
        List<string> coords = new List<string>();
        foreach (var pos in _unlockedSlots)
        {
            coords.Add($"{pos.x},{pos.y}");
        }
        string data = string.Join(";", coords);
        SecurePlayerPrefs.SetString("UnlockedSlots", data);
        SaveCoordinator.MarkDirty();
    }

    private void Load()
    {
        if (!SecurePlayerPrefs.HasKey("UnlockedSlots")) return;

        string data = SecurePlayerPrefs.GetString("UnlockedSlots");
        if (string.IsNullOrEmpty(data)) return;

        string[] pairs = data.Split(';');
        foreach (string pair in pairs)
        {
            string[] parts = pair.Split(',');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    _unlockedSlots.Add(new Vector2Int(x, y));
                }
            }
        }

        RecalculateUnlockCenter();
    }

    /// <summary>
    /// Called by GameAutoSetup to define the initial unlocked rectangle.
    /// </summary>
    public void Initialize(int startCol, int startRow, int unlockCols, int unlockRows)
    {
        // If we already have loaded slots, don't clear and re-initialize the starting area
        // uniquely, but rather just ensure the starting area IS included if it's the first time.
        if (_unlockedSlots.Count > 0) return;

        _unlockedSlots.Clear();

        // Unlock the starting rectangle
        for (int x = startCol; x < startCol + unlockCols; x++)
        {
            for (int y = startRow; y < startRow + unlockRows; y++)
            {
                if (IsWithinUnlockLimits(x, y))
                {
                    _unlockedSlots.Add(new Vector2Int(x, y));
                }
            }
        }

        // Calculate the center of the unlocked patch for pricing
        _unlockCenter = new Vector2(
            startCol + unlockCols * 0.5f,
            startRow + unlockRows * 0.5f
        );

        Save();
    }

    /// <summary>
    /// Sets the maximum rectangular area where slots can be unlocked.
    /// </summary>
    public void SetUnlockLimits(int minX, int maxX, int minY, int maxY)
    {
        if (minX > maxX || minY > maxY)
        {
            _hasUnlockLimits = false;
            return;
        }

        _minUnlockX = minX;
        _maxUnlockX = maxX;
        _minUnlockY = minY;
        _maxUnlockY = maxY;
        _hasUnlockLimits = true;
    }

    public bool IsWithinUnlockLimits(int x, int y)
    {
        if (!_hasUnlockLimits)
        {
            return true;
        }

        return x >= _minUnlockX && x <= _maxUnlockX && y >= _minUnlockY && y <= _maxUnlockY;
    }

    /// <summary>
    /// Check if a slot at (x, y) is unlocked.
    /// </summary>
    public bool IsUnlocked(int x, int y)
    {
        return _unlockedSlots.Contains(new Vector2Int(x, y));
    }

    /// <summary>
    /// Returns the coin cost to unlock the slot at (x, y).
    /// Cost increases with distance from the initial unlocked center.
    /// </summary>
    public int GetUnlockCost(int x, int y)
    {
        float distance = Vector2.Distance(new Vector2(x, y), _unlockCenter);
        return Mathf.RoundToInt(BASE_UNLOCK_COST + distance * COST_PER_DISTANCE);
    }

    /// <summary>
    /// Check if a locked slot is adjacent to any unlocked slot (can only expand territory).
    /// </summary>
    public bool IsAdjacentToUnlocked(int x, int y)
    {
        // Check 4 cardinal directions
        return _unlockedSlots.Contains(new Vector2Int(x - 1, y))
            || _unlockedSlots.Contains(new Vector2Int(x + 1, y))
            || _unlockedSlots.Contains(new Vector2Int(x, y - 1))
            || _unlockedSlots.Contains(new Vector2Int(x, y + 1));
    }

    /// <summary>
    /// Attempt to unlock a slot by spending coins.
    /// Returns true if successful.
    /// </summary>
    public bool TryUnlockSlot(int x, int y)
    {
        Vector2Int coord = new Vector2Int(x, y);

        // Already unlocked
        if (_unlockedSlots.Contains(coord)) return false;

        // Respect global maximum unlockable area.
        if (!IsWithinUnlockLimits(x, y)) return false;

        // Must be adjacent to an unlocked slot
        if (!IsAdjacentToUnlocked(x, y)) return false;

        // Block unlocking border slots
        if (GridManager.Instance != null)
        {
            if (x <= 0 || y <= 0 || x >= GridManager.Instance.columns - 1 || y >= GridManager.Instance.rows - 1)
            {
                return false;
            }
        }

        int cost = GetUnlockCost(x, y);

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendCoin(cost))
        {
            _unlockedSlots.Add(coord);
            OnSlotUnlocked?.Invoke(x, y);
            Save();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns all currently unlocked slot positions.
    /// </summary>
    public HashSet<Vector2Int> GetAllUnlockedPositions()
    {
        return _unlockedSlots;
    }

    private void RecalculateUnlockCenter()
    {
        if (_unlockedSlots.Count == 0)
        {
            _unlockCenter = Vector2.zero;
            return;
        }

        Vector2 total = Vector2.zero;
        foreach (Vector2Int pos in _unlockedSlots)
        {
            total += new Vector2(pos.x, pos.y);
        }

        _unlockCenter = total / _unlockedSlots.Count;
    }
}
