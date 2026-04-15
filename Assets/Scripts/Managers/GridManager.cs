using UnityEngine;

public class GridManager : MonoBehaviour
{
    private const string GridStateKey = "GridState";

    [Header("Grid Settings")]
    public int columns = 5;
    public int rows = 5;
    public float spacing = 0.85f;

    [Header("References")]
    public GameObject slotPrefab;
    [Tooltip("Parent transform for instantiated slots. Uses 'this' transform if left empty.")]
    public Transform gridParent;

    // 2D Array holding all slot references
    private GridSlot[,] _grid;
    private bool _useThemedFarmBackground;

    private static readonly Color SlotVisibleBorderColor = Color.black;
    private static readonly Color SlotVisibleUnlockedColor = Color.white;
    private static readonly Color SlotVisibleLockedColor = new Color(0.12f, 0.14f, 0.18f, 0.44f);

    private static readonly Color SlotThemedBorderColor = new Color(0f, 0f, 0f, 0.12f);
    private static readonly Color SlotThemedUnlockedColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color SlotThemedLockedColor = new Color(0.08f, 0.1f, 0.14f, 0.2f);

    /// <summary>
    /// Singleton-like static reference for nearest-slot lookups from DragHandler.
    /// </summary>
    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _useThemedFarmBackground = Resources.Load<Texture2D>("Farm/ANAMENU-Background") != null;

        GenerateGrid();

        // Subscribe to unlock events to refresh visuals
        if (SlotUnlockManager.Instance != null)
        {
            SlotUnlockManager.Instance.OnSlotUnlocked += OnSlotUnlocked;
        }

        LoadGridState();
    }

    private void OnDestroy()
    {
        if (SlotUnlockManager.Instance != null)
        {
            SlotUnlockManager.Instance.OnSlotUnlocked -= OnSlotUnlocked;
        }
    }

    private void GenerateGrid()
    {
        if (slotPrefab == null)
        {
            Debug.LogError("Slot Prefab is missing in GridManager! Please assign it in the Inspector.");
            return;
        }

        if (gridParent == null) 
            gridParent = this.transform;

        _grid = new GridSlot[columns, rows];

        // Offset to keep the whole grid centered based on sizes
        float offsetX = (columns - 1) * spacing / 2f;
        float offsetY = (rows - 1) * spacing / 2f;
        Vector3 origin = gridParent.position;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // Calculate position for current iteration (world-space, aligned around grid parent origin)
                Vector2 spawnPos = new Vector2(
                    origin.x + (x * spacing) - offsetX,
                    origin.y + (y * spacing) - offsetY);

                // Instantiate prefab
                GameObject newSlotObj = Instantiate(slotPrefab, spawnPos, Quaternion.identity, gridParent);
                newSlotObj.name = $"Slot_{x}_{y}";

                // Scale the slot to be slightly smaller than spacing (90%) 
                // This creates the "grid line" effect by revealing the dark background
                float visualScale = spacing * 0.9f;
                newSlotObj.transform.localScale = new Vector3(visualScale, visualScale, 1f);

                // Make sure the prefab has a GridSlot component attached
                GridSlot slotComponent = newSlotObj.GetComponent<GridSlot>();
                if (slotComponent == null)
                {
                    slotComponent = newSlotObj.AddComponent<GridSlot>();
                }
                
                // Ensure required components for Drag & Merge
                BoxCollider2D col = newSlotObj.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = newSlotObj.AddComponent<BoxCollider2D>();
                }
                // Size collider to match the full spacing area 
                // Math: visualScale * colliderSize = spacing -> colliderSize = spacing / visualScale
                col.size = new Vector2(spacing / visualScale, spacing / visualScale);

                if (newSlotObj.GetComponent<DragHandler>() == null)
                {
                    newSlotObj.AddComponent<DragHandler>();
                }

                // Initialize slot state
                slotComponent.Initialize(x, y);
                _grid[x, y] = slotComponent;

                // Keep only top/bottom rows as hard border.
                if (y == 0 || y == rows - 1)
                {
                    slotComponent.IsBorder = true;
                }

                // Apply lock/unlock visuals
                ApplySlotLockState(slotComponent, x, y);
            }
        }
    }

    /// <summary>
    /// Apply visual lock/unlock state to a slot.
    /// Locked slots are darkened and have interaction disabled.
    /// </summary>
    private void ApplySlotLockState(GridSlot slot, int x, int y)
    {
        bool isUnlocked = true;
        bool isWithinVisibleFarmArea = true;

        if (SlotUnlockManager.Instance != null)
        {
            isUnlocked = SlotUnlockManager.Instance.IsUnlocked(x, y);
            isWithinVisibleFarmArea = SlotUnlockManager.Instance.IsWithinUnlockLimits(x, y);
        }

        SpriteRenderer sr = slot.GetComponent<SpriteRenderer>();
        DragHandler dh = slot.GetComponent<DragHandler>();
        BoxCollider2D col = slot.GetComponent<BoxCollider2D>();

        // Hide all slots outside the visible farm area rectangle.
        if (!isWithinVisibleFarmArea)
        {
            if (sr != null) sr.enabled = false;
            if (dh != null) dh.enabled = false;
            if (col != null) col.enabled = false;
            slot.SetLocked(true);
            return;
        }

        // Ensure visible-area slots stay renderable / interactive according to lock state.
        if (sr != null) sr.enabled = true;
        if (col != null) col.enabled = true;

        // Handle Border visual first
        if (slot.IsBorder)
        {
            if (sr != null) sr.color = _useThemedFarmBackground ? SlotThemedBorderColor : SlotVisibleBorderColor;
            if (dh != null) dh.enabled = false;
            slot.SetLocked(true);
            return;
        }

        if (isUnlocked)
        {
            // Normal playable slot
            if (sr != null) sr.color = _useThemedFarmBackground ? SlotThemedUnlockedColor : SlotVisibleUnlockedColor;
            if (dh != null) dh.enabled = true;
            slot.SetLocked(false);
        }
        else
        {
            // All locked slots look the same (dark gray/neutral)
            // Removed the green "adjacent" highlight as requested
            if (sr != null) sr.color = _useThemedFarmBackground ? SlotThemedLockedColor : SlotVisibleLockedColor;
            
            if (dh != null) dh.enabled = false;
            slot.SetLocked(true);
        }
    }

    /// <summary>
    /// Called when a slot is unlocked — refresh that slot and its neighbors.
    /// </summary>
    private void OnSlotUnlocked(int x, int y)
    {
        if (_grid == null) return;

        // Refresh the unlocked slot
        if (x >= 0 && x < columns && y >= 0 && y < rows)
        {
            ApplySlotLockState(_grid[x, y], x, y);
        }

        // Refresh neighboring slots (they might become "adjacent to unlocked")
        RefreshNeighbors(x, y);
    }

    private void RefreshNeighbors(int cx, int cy)
    {
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = cx + dx[i];
            int ny = cy + dy[i];

            if (nx >= 0 && nx < columns && ny >= 0 && ny < rows)
            {
                ApplySlotLockState(_grid[nx, ny], nx, ny);
            }
        }
    }

    /// <summary>
    /// Helper method to find the first available empty AND unlocked slot.
    /// Useful for spawning new crops on the board.
    /// </summary>
    /// <returns>Returns an empty GridSlot, or null if the board is completely full.</returns>
    public GridSlot GetEmptySlot()
    {
        if (_grid == null) return null;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (_grid[x, y].IsEmpty && !_grid[x, y].IsLocked)
                {
                    return _grid[x, y];
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Returns all generated slots to allow iterating over the board.
    /// </summary>
    public System.Collections.Generic.IEnumerable<GridSlot> GetAllSlots()
    {
        if (_grid == null) yield break;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                yield return _grid[x, y];
            }
        }
    }

    /// <summary>
    /// Finds the nearest UNLOCKED GridSlot to a world position.
    /// Used by DragHandler to snap crops to the nearest slot on drop.
    /// </summary>
    public GridSlot GetNearestSlot(Vector3 worldPosition)
    {
        if (_grid == null) return null;

        GridSlot nearest = null;
        float minDist = float.MaxValue;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // Only consider unlocked slots for drop targets
                if (_grid[x, y].IsLocked) continue;

                float dist = Vector2.Distance(worldPosition, _grid[x, y].transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = _grid[x, y];
                }
            }
        }

        return nearest;
    }

    public void SaveGridState()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var slot in GetAllSlots())
        {
            if (!slot.IsEmpty)
            {
                // Format: x,y,tier,cropNameBase64 (name field optional for backward compatibility)
                int tierRaw = (int)slot.CurrentCrop.tier;
                string encodedCropName = EncodeGridCropName(slot.CurrentCrop.itemName);
                if (string.IsNullOrEmpty(encodedCropName))
                {
                    sb.Append($"{slot.X},{slot.Y},{tierRaw};");
                }
                else
                {
                    sb.Append($"{slot.X},{slot.Y},{tierRaw},{encodedCropName};");
                }
            }
        }
        SecurePlayerPrefs.SetString(GridStateKey, sb.ToString());
        SaveCoordinator.MarkDirty();
    }

    public void LoadGridState()
    {
        if (!SecurePlayerPrefs.HasKey(GridStateKey)) return;
        string data = SecurePlayerPrefs.GetString(GridStateKey);
        if (string.IsNullOrEmpty(data)) return;

        if (GameContentGenerator.Instance == null)
        {
            Debug.LogWarning("GridManager: GameContentGenerator is missing, GridState load skipped.");
            return;
        }

        bool containsLegacyTierOnlyEntries = false;

        string[] entries = data.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in entries)
        {
            string[] parts = entry.Split(',');
            if (parts.Length >= 3 &&
                int.TryParse(parts[0], out int x) &&
                int.TryParse(parts[1], out int y) &&
                int.TryParse(parts[2], out int tierRaw))
            {
                CropTier tier = (CropTier)tierRaw;

                if (x >= 0 && x < columns && y >= 0 && y < rows)
                {
                    CropData crop = null;

                    // New format: resolve by exact crop name first.
                    if (parts.Length >= 4 && TryDecodeGridCropName(parts[3], out string cropName))
                    {
                        crop = GameContentGenerator.Instance.GetCropByName(cropName);
                    }
                    else
                    {
                        containsLegacyTierOnlyEntries = true;
                    }

                    // Legacy fallback: resolve by tier only.
                    if (crop == null)
                    {
                        crop = GameContentGenerator.Instance.GetCropByTier(tier);
                    }

                    if (crop != null)
                    {
                        _grid[x, y].SetCrop(crop);
                    }
                }
            }
        }

        // Rewrite once so subsequent restarts use exact crop identity.
        if (containsLegacyTierOnlyEntries)
        {
            SaveGridState();
        }
    }

    private static string EncodeGridCropName(string cropName)
    {
        if (string.IsNullOrWhiteSpace(cropName))
            return string.Empty;

        try
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(cropName);
            return System.Convert.ToBase64String(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryDecodeGridCropName(string encodedCropName, out string cropName)
    {
        cropName = string.Empty;
        if (string.IsNullOrWhiteSpace(encodedCropName))
            return false;

        try
        {
            byte[] bytes = System.Convert.FromBase64String(encodedCropName);
            cropName = System.Text.Encoding.UTF8.GetString(bytes);
            return !string.IsNullOrWhiteSpace(cropName);
        }
        catch
        {
            return false;
        }
    }
}
