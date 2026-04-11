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
    }

    /// <summary>
    /// Called by GameAutoSetup to define the initial unlocked rectangle.
    /// </summary>
    public void Initialize(int startCol, int startRow, int unlockCols, int unlockRows)
    {
        _unlockedSlots.Clear();

        // Unlock the starting rectangle
        for (int x = startCol; x < startCol + unlockCols; x++)
        {
            for (int y = startRow; y < startRow + unlockRows; y++)
            {
                _unlockedSlots.Add(new Vector2Int(x, y));
            }
        }

        // Calculate the center of the unlocked patch for pricing
        _unlockCenter = new Vector2(
            startCol + unlockCols * 0.5f,
            startRow + unlockRows * 0.5f
        );
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

        // Must be adjacent to an unlocked slot
        if (!IsAdjacentToUnlocked(x, y)) return false;

        // Block unlocking border slots
        if (GridManager.Instance != null)
        {
            // We need a way to check if slot is border from manager
            // I'll add a helper to GridManager or just check coordinates here
            // But since I set border in GridManager based on coords, I can do it here too
            // Note: GridManager dims are columns/rows. 
            // A better way is to check the slot component itself.
            var allSlots = GridManager.Instance.GetAllSlots();
            foreach(var s in allSlots) {
                if (s.X == x && s.Y == y && s.IsBorder) return false;
            }
        }

        int cost = GetUnlockCost(x, y);

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendCoin(cost))
        {
            _unlockedSlots.Add(coord);
            OnSlotUnlocked?.Invoke(x, y);
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
}
