using UnityEngine;

public class GridManager : MonoBehaviour
{
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

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // Calculate position for current iteration
                Vector2 spawnPos = new Vector2((x * spacing) - offsetX, (y * spacing) - offsetY);

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

                // Mark world boundary (col 0/19 and row 0/24)
                if (x == 0 || x == columns - 1 || y == 0 || y == rows - 1)
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

        if (SlotUnlockManager.Instance != null)
        {
            isUnlocked = SlotUnlockManager.Instance.IsUnlocked(x, y);
        }

        SpriteRenderer sr = slot.GetComponent<SpriteRenderer>();
        DragHandler dh = slot.GetComponent<DragHandler>();

        // Handle Border visual first
        if (slot.IsBorder)
        {
            if (sr != null) sr.color = Color.black;
            if (dh != null) dh.enabled = false;
            slot.SetLocked(true);
            return;
        }

        if (isUnlocked)
        {
            // Normal playable slot
            if (sr != null) sr.color = Color.white;
            if (dh != null) dh.enabled = true;
            slot.SetLocked(false);
        }
        else
        {
            // All locked slots look the same (dark gray/neutral)
            // Removed the green "adjacent" highlight as requested
            if (sr != null) sr.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);
            
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
                // Format: x,y,tier
                sb.Append($"{slot.X},{slot.Y},{(int)slot.CurrentCrop.tier};");
            }
        }
        SecurePlayerPrefs.SetString("GridState", sb.ToString());
        SaveCoordinator.MarkDirty();
    }

    public void LoadGridState()
    {
        if (!SecurePlayerPrefs.HasKey("GridState")) return;
        string data = SecurePlayerPrefs.GetString("GridState");
        if (string.IsNullOrEmpty(data)) return;

        string[] entries = data.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in entries)
        {
            string[] parts = entry.Split(',');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int x) &&
                int.TryParse(parts[1], out int y) &&
                int.TryParse(parts[2], out int tierRaw))
            {
                CropTier tier = (CropTier)tierRaw;

                if (x >= 0 && x < columns && y >= 0 && y < rows)
                {
                    CropData crop = GameContentGenerator.Instance.GetCropByTier(tier);
                    if (crop != null)
                    {
                        _grid[x, y].SetCrop(crop);
                    }
                }
            }
        }
    }
}
