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

    private const float ItemIconTargetWorldSize = 0.42f;
    private const float ItemBackgroundTargetWorldSize = 0.62f;
    private const float ItemIconFallbackScale = 1.5f;
    private const float ItemBackgroundFallbackScale = 15f;
    private static readonly Vector2 SlotBaseNormalizedSize = Vector2.one;

    private static bool _slotBaseSpriteLoaded;
    private static Sprite _slotBaseSprite;

    private void Awake()
    {
        _backgroundRenderer = GetComponent<SpriteRenderer>();

        Sprite baseTile = GetSlotBaseSprite();
        if (baseTile != null && _backgroundRenderer != null)
        {
            _backgroundRenderer.sprite = baseTile;
        }

        ConfigureBackgroundRenderer();

        // Create a container for the whole item (background + icon)
        // This container is what DragHandler moves.
        GameObject itemContainer = new GameObject("ItemVisualContainer");
        itemContainer.transform.SetParent(this.transform, false);
        itemContainer.transform.localPosition = Vector3.zero;
        
        // Item Background (the colored area - acts as the actual draggable base tile)
        GameObject bgObj = new GameObject("ItemBackground");
        bgObj.transform.SetParent(itemContainer.transform, false);
        bgObj.transform.localScale = new Vector3(ItemBackgroundFallbackScale, ItemBackgroundFallbackScale, 1f); // fallback before sprite normalization
        _itemBgRenderer = bgObj.AddComponent<SpriteRenderer>();
        _itemBgRenderer.sortingOrder = 1;

        // Item Icon (the actual crop)
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(itemContainer.transform, false);
        iconObj.transform.localScale = new Vector3(ItemIconFallbackScale, ItemIconFallbackScale, 1f); // fallback before sprite normalization
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
        if (_itemIconRenderer != null)
        {
            bool showIcon = show;
            if (show && CurrentCrop != null && IsCompositeCropSprite(CurrentCrop.cropSprite))
            {
                showIcon = false;
            }
            _itemIconRenderer.enabled = showIcon;
        }

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
            bool useCompositeSprite = IsCompositeCropSprite(newCrop.cropSprite);

            // Set visuals
            if (_itemBgRenderer != null)
            {
                _itemBgRenderer.sprite = newCrop.cropSprite;
                _itemBgRenderer.color = Color.white;
                _itemBgRenderer.enabled = true;
                ApplySpriteScale(_itemBgRenderer, ItemBackgroundTargetWorldSize, ItemBackgroundFallbackScale);
            }

            if (_itemIconRenderer != null)
            {
                if (useCompositeSprite)
                {
                    // Imported crop sprites already contain tile + icon composition.
                    _itemIconRenderer.sprite = null;
                    _itemIconRenderer.enabled = false;
                }
                else
                {
                    _itemIconRenderer.sprite = newCrop.cropSprite;
                    _itemIconRenderer.color = newCrop.cropColor;
                    _itemIconRenderer.enabled = true;
                    ApplySpriteScale(_itemIconRenderer, ItemIconTargetWorldSize, ItemIconFallbackScale);
                }
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
            _itemBgRenderer.transform.localScale = new Vector3(ItemBackgroundFallbackScale, ItemBackgroundFallbackScale, 1f);
        }
        if (_itemIconRenderer != null)
        {
            _itemIconRenderer.sprite = null;
            _itemIconRenderer.enabled = true;
            _itemIconRenderer.transform.localScale = new Vector3(ItemIconFallbackScale, ItemIconFallbackScale, 1f);
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

    private static Sprite GetSlotBaseSprite()
    {
        if (_slotBaseSpriteLoaded)
            return _slotBaseSprite;

        _slotBaseSpriteLoaded = true;
        _slotBaseSprite = Resources.Load<Sprite>("Slot/spr_slot_base_tile");
        return _slotBaseSprite;
    }

    private void ConfigureBackgroundRenderer()
    {
        if (_backgroundRenderer == null)
            return;

        if (!IsSlotBaseSprite(_backgroundRenderer.sprite))
            return;

        // Normalize slot visuals so very large source textures do not distort the grid.
        _backgroundRenderer.drawMode = SpriteDrawMode.Sliced;
        _backgroundRenderer.size = SlotBaseNormalizedSize;
    }

    private void ApplySpriteScale(SpriteRenderer renderer, float targetWorldSize, float fallbackScale)
    {
        if (renderer == null)
            return;

        Sprite sprite = renderer.sprite;
        if (sprite == null)
        {
            renderer.transform.localScale = new Vector3(fallbackScale, fallbackScale, 1f);
            return;
        }

        Vector2 spriteSize = sprite.bounds.size;
        float maxDim = Mathf.Max(spriteSize.x, spriteSize.y);
        if (maxDim <= 0.0001f)
        {
            renderer.transform.localScale = new Vector3(fallbackScale, fallbackScale, 1f);
            return;
        }

        float scale = targetWorldSize / maxDim;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static bool IsCompositeCropSprite(Sprite sprite)
    {
        return sprite != null
            && sprite.name.StartsWith("spr_crop_", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSlotBaseSprite(Sprite sprite)
    {
        return sprite != null
            && sprite.name.StartsWith("spr_slot_base_tile", System.StringComparison.OrdinalIgnoreCase);
    }
}
