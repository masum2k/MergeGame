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

                // Scale the slot prefab to match tighter grid
                newSlotObj.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

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
                // Size collider to match spacing so there are no gaps between slots
                col.size = new Vector2(spacing / newSlotObj.transform.localScale.x, spacing / newSlotObj.transform.localScale.y);

                if (newSlotObj.GetComponent<DragHandler>() == null)
                {
                    newSlotObj.AddComponent<DragHandler>();
                }

                // Initialize slot state
                slotComponent.Initialize(x, y);
                _grid[x, y] = slotComponent;
            }
        }
    }

    /// <summary>
    /// Helper method to find the first available empty slot.
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
                if (_grid[x, y].IsEmpty)
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
    /// Finds the nearest GridSlot to a world position.
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
}
