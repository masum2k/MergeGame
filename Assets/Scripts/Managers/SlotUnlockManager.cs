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
    /// Re-centers only legacy "initial-only" farms where exactly the start patch size is unlocked
    /// but the unlocked set is not at the expected centered coordinates.
    /// Expanded farms are not modified.
    /// </summary>
    public void TryRecenterIfOnlyInitialArea(int startCol, int startRow, int unlockCols, int unlockRows)
    {
        if (_unlockedSlots.Count == 0)
        {
            return;
        }

        if (!_hasUnlockLimits)
        {
            return;
        }

        // Legacy save files may carry hidden coordinates outside current unlock limits.
        // Prune those first so recenter checks use only the visible farm area.
        PruneUnlockedSlotsOutsideLimits();

        if (_unlockedSlots.Count == 0)
        {
            return;
        }

        int expectedCount = unlockCols * unlockRows;
        if (_unlockedSlots.Count == expectedCount)
        {
            if (TryGetUnlockedRectangle(out int currentMinX, out int currentMinY, out int currentCols, out int currentRows))
            {
                bool sameLayout = currentMinX == startCol
                    && currentMinY == startRow
                    && currentCols == unlockCols
                    && currentRows == unlockRows;

                if (!sameLayout)
                {
                    int dx = startCol - currentMinX;
                    int dy = startRow - currentMinY;
                    ShiftSavedGridStateRectangle(currentMinX, currentMinY, currentCols, currentRows, dx, dy);
                    RebuildUnlockedRectangle(startCol, startRow, unlockCols, unlockRows);
                }
            }
            else if (!IsExactUnlockedRectangle(startCol, startRow, unlockCols, unlockRows))
            {
                RebuildUnlockedRectangle(startCol, startRow, unlockCols, unlockRows);
            }
            return;
        }

        if (!TryGetUnlockedRectangle(out int minX, out int minY, out int rectCols, out int rectRows))
        {
            return;
        }

        bool isCompactLegacyRectangle = rectCols <= 5 && rectRows <= 5 && _unlockedSlots.Count <= 25;
        if (!isCompactLegacyRectangle)
        {
            return;
        }

        int centeredStartCol = GetCenteredStart(rectCols, _minUnlockX, _maxUnlockX);
        int centeredStartRow = GetCenteredStart(rectRows, _minUnlockY, _maxUnlockY);

        if (minX == centeredStartCol && minY == centeredStartRow)
        {
            return;
        }

        int shiftX = centeredStartCol - minX;
        int shiftY = centeredStartRow - minY;
        ShiftSavedGridStateRectangle(minX, minY, rectCols, rectRows, shiftX, shiftY);
        RebuildUnlockedRectangle(centeredStartCol, centeredStartRow, rectCols, rectRows);
    }

    private void ShiftSavedGridStateRectangle(
        int sourceStartCol,
        int sourceStartRow,
        int sourceCols,
        int sourceRows,
        int shiftX,
        int shiftY)
    {
        if ((shiftX == 0 && shiftY == 0) || sourceCols <= 0 || sourceRows <= 0)
        {
            return;
        }

        if (!SecurePlayerPrefs.HasKey("GridState"))
        {
            return;
        }

        string data = SecurePlayerPrefs.GetString("GridState");
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        string[] entries = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length == 0)
        {
            return;
        }

        int sourceEndCol = sourceStartCol + sourceCols - 1;
        int sourceEndRow = sourceStartRow + sourceRows - 1;

        bool changed = false;
        List<string> rewritten = new List<string>(entries.Length);

        foreach (string entry in entries)
        {
            string[] parts = entry.Split(',');
            if (parts.Length < 3
                || !int.TryParse(parts[0], out int x)
                || !int.TryParse(parts[1], out int y))
            {
                rewritten.Add(entry);
                continue;
            }

            if (x >= sourceStartCol && x <= sourceEndCol && y >= sourceStartRow && y <= sourceEndRow)
            {
                x += shiftX;
                y += shiftY;
                changed = true;
            }

            string suffix = string.Empty;
            if (parts.Length > 3)
            {
                suffix = "," + parts[3];
                for (int i = 4; i < parts.Length; i++)
                {
                    suffix += "," + parts[i];
                }
            }

            rewritten.Add(x + "," + y + "," + parts[2] + suffix);
        }

        if (!changed)
        {
            return;
        }

        SecurePlayerPrefs.SetString("GridState", string.Join(";", rewritten));
        SaveCoordinator.MarkDirty();
    }

    private void PruneUnlockedSlotsOutsideLimits()
    {
        if (!_hasUnlockLimits || _unlockedSlots.Count == 0)
        {
            return;
        }

        bool removedAny = false;
        HashSet<Vector2Int> filtered = new HashSet<Vector2Int>();
        foreach (Vector2Int pos in _unlockedSlots)
        {
            if (IsWithinUnlockLimits(pos.x, pos.y))
            {
                filtered.Add(pos);
            }
            else
            {
                removedAny = true;
            }
        }

        if (!removedAny)
        {
            return;
        }

        _unlockedSlots = filtered;
        RecalculateUnlockCenter();
        Save();
    }

    private static int GetCenteredStart(int rectSize, int min, int max)
    {
        int maxStart = Mathf.Max(min, max - rectSize + 1);
        int centered = (min + max - rectSize + 1) / 2;
        return Mathf.Clamp(centered, min, maxStart);
    }

    private bool IsExactUnlockedRectangle(int startCol, int startRow, int unlockCols, int unlockRows)
    {
        for (int x = startCol; x < startCol + unlockCols; x++)
        {
            for (int y = startRow; y < startRow + unlockRows; y++)
            {
                if (!IsWithinUnlockLimits(x, y) || !_unlockedSlots.Contains(new Vector2Int(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool TryGetUnlockedRectangle(out int minX, out int minY, out int rectCols, out int rectRows)
    {
        minX = int.MaxValue;
        int maxX = int.MinValue;
        minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (Vector2Int pos in _unlockedSlots)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        if (minX == int.MaxValue || minY == int.MaxValue)
        {
            rectCols = 0;
            rectRows = 0;
            return false;
        }

        rectCols = maxX - minX + 1;
        rectRows = maxY - minY + 1;

        if (rectCols * rectRows != _unlockedSlots.Count)
        {
            return false;
        }

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (!_unlockedSlots.Contains(new Vector2Int(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void RebuildUnlockedRectangle(int startCol, int startRow, int unlockCols, int unlockRows)
    {
        _unlockedSlots.Clear();
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

        PruneUnlockedSlotsOutsideLimits();
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
            int borderRows = Mathf.Max(0, GridManager.Instance.borderRows);
            if (borderRows > 0 && (y < borderRows || y >= GridManager.Instance.rows - borderRows))
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
