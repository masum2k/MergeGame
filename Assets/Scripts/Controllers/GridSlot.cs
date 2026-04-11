using UnityEngine;

public class GridSlot : MonoBehaviour
{
    [Header("Slot Information")]
    public int X { get; private set; }
    public int Y { get; private set; }

    [Header("State")]
    public CropData CurrentCrop;

    /// <summary>
    /// Whether this slot is locked (cannot hold crops or be interacted with).
    /// </summary>
    public bool IsLocked { get; private set; }

    /// <summary>
    /// Whether this slot is part of the non-playable world boundary.
    /// </summary>
    public bool IsBorder { get; set; }

    private SpriteRenderer _backgroundRenderer;
    private SpriteRenderer _itemBgRenderer;
    private SpriteRenderer _itemIconRenderer;
    private Coroutine _bounceCoroutine;

    private void Awake()
    {
        _backgroundRenderer = GetComponent<SpriteRenderer>();

        // Create a container for the whole item (background + icon)
        // This container is what DragHandler moves.
        GameObject itemContainer = new GameObject("ItemVisualContainer");
        itemContainer.transform.SetParent(this.transform, false);
        itemContainer.transform.localPosition = Vector3.zero;
        
        // Item Background (the colored area - acts as the actual draggable base tile)
        GameObject bgObj = new GameObject("ItemBackground");
        bgObj.transform.SetParent(itemContainer.transform, false);
        bgObj.transform.localScale = new Vector3(15f, 15f, 1f); // Super massive halo effect
        _itemBgRenderer = bgObj.AddComponent<SpriteRenderer>();
        _itemBgRenderer.sortingOrder = 1;

        // Item Icon (the actual crop)
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(itemContainer.transform, false);
        iconObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f); // Enlarged icon to balance the massive glow
        _itemIconRenderer = iconObj.AddComponent<SpriteRenderer>();
        _itemIconRenderer.sortingOrder = 2; // In front of ItemBackground
    }

    public Transform GetVisualTransform()
    {
        // Return the container so the whole thing moves during drag
        return _itemBgRenderer.transform.parent;
    }

    public void SetDragSorting(bool isDragging)
    {
        int offset = isDragging ? 100 : -100;
        if (_itemBgRenderer != null) _itemBgRenderer.sortingOrder += offset;
        if (_itemIconRenderer != null) _itemIconRenderer.sortingOrder += offset;
    }

    // A slot is empty if it doesn't hold any crop data
    public bool IsEmpty => CurrentCrop == null;

    /// <summary>
    /// Initializes the slot's coordinates in the grid.
    /// </summary>
    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Sets the locked state of this slot.
    /// </summary>
    public void SetLocked(bool locked)
    {
        IsLocked = locked;

        // Hide crop visuals when locked
        bool show = !locked;
        if (_itemBgRenderer != null) _itemBgRenderer.enabled = show;
        if (_itemIconRenderer != null) _itemIconRenderer.enabled = show;

        // Apply fallback background color if resetting
        if (!locked && IsEmpty && _backgroundRenderer != null)
        {
            _backgroundRenderer.color = Color.white;
        }
    }

    /// <summary>
    /// Assigns a crop to this slot.
    /// </summary>
    public void SetCrop(CropData newCrop)
    {
        if (IsLocked) return; 

        CurrentCrop = newCrop;
        if (newCrop != null)
        {
            // Set visuals
            if (_itemBgRenderer != null)
            {
                _itemBgRenderer.sprite = newCrop.cropSprite;
                Color glowColor = newCrop.cropColor;
                glowColor.a = 0.65f; // Stronger tint to act as a prominent base glow
                _itemBgRenderer.color = glowColor;
            }

            if (_itemIconRenderer != null)
            {
                _itemIconRenderer.sprite = newCrop.cropSprite;
                _itemIconRenderer.color = newCrop.cropColor;
            }

            // Keep the static background pure white. The draggable container now acts as the tinted 'base tile'.
            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.color = Color.white;
            }

            PlayBounceAnimation();
        }
    }

    /// <summary>
    /// Removes the crop from this slot, making it empty.
    /// </summary>
    public void ClearSlot()
    {
        CurrentCrop = null;
        if (_itemBgRenderer != null)
        {
            _itemBgRenderer.sprite = null;
            _itemBgRenderer.color = Color.white;
        }
        if (_itemIconRenderer != null)
        {
            _itemIconRenderer.sprite = null;
        }

        if (_backgroundRenderer != null)
        {
            _backgroundRenderer.color = Color.white;
        }
    }

    private void PlayBounceAnimation()
    {
        if (_bounceCoroutine != null) StopCoroutine(_bounceCoroutine);
        _bounceCoroutine = StartCoroutine(BounceRoutine());
    }

    private System.Collections.IEnumerator BounceRoutine()
    {
        Transform t = _itemBgRenderer.transform.parent; // The container
        Vector3 baseScale = Vector3.one;
        float duration = 0.15f;
        
        // Punch up
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            t.localScale = baseScale * (1f + 0.3f * Mathf.Sin(p * Mathf.PI));
            yield return null;
        }
        
        t.localScale = baseScale;
        _bounceCoroutine = null;
    }
}
