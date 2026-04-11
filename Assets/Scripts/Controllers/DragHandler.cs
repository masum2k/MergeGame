using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(GridSlot))]
[RequireComponent(typeof(BoxCollider2D))]
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    private GridSlot _slot;
    private Transform _visualTransform;
    private BoxCollider2D _collider;
    private bool _isDragging = false;
    private bool _dropHandled = false;

    private void Awake()
    {
        _slot = GetComponent<GridSlot>();
        _collider = GetComponent<BoxCollider2D>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Don't drag if empty or locked
        if (_slot.IsEmpty || _slot.IsLocked)
        {
            eventData.pointerDrag = null;
            return;
        }

        _isDragging = true;
        _dropHandled = false;

        // Disable collider so we don't raycast into ourselves while testing drop
        _collider.enabled = false;

        _visualTransform = _slot.GetVisualTransform();
        _slot.SetDragSorting(true); // Bring container to front while dragging
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_visualTransform != null)
        {
            // Use the distance between camera and plane for accurate ScreenToWorld conversion
            float camOffset = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, camOffset);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0; 
            
            // Higher lerp factor (0.8f) for more responsive but still smooth follow
            _visualTransform.position = Vector3.Lerp(_visualTransform.position, worldPos, 0.8f);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_visualTransform == null) return;

        // Reset visual state
        _slot.SetDragSorting(false);

        // Re-enable collider
        _collider.enabled = true;

        // If OnDrop didn't handle it (dropped outside a slot), try snap to nearest
        if (!_dropHandled)
        {
            TrySnapToNearestSlot();
        }

        _visualTransform.localPosition = Vector3.zero;
        _visualTransform = null;
        _isDragging = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // This is called on the TARGET slot when something is dropped on it.
        if (eventData.pointerDrag == null) return;

        // Don't allow dropping on locked slots
        if (_slot.IsLocked) return;

        DragHandler draggedItem = eventData.pointerDrag.GetComponent<DragHandler>();
        if (draggedItem != null && draggedItem != this)
        {
            GridSlot sourceSlot = draggedItem.GetComponent<GridSlot>();
            
            // Is this target empty? Move!
            if (this._slot.IsEmpty)
            {
                this._slot.SetCrop(sourceSlot.CurrentCrop);
                sourceSlot.ClearSlot();
                draggedItem._dropHandled = true;
            }
            // Is target same crop? Merge!
            else if (this._slot.CurrentCrop != null && sourceSlot.CurrentCrop != null &&
                     this._slot.CurrentCrop.cropName == sourceSlot.CurrentCrop.cropName && 
                     sourceSlot.CurrentCrop.nextLevelCrop != null)
            {
                // Cache info BEFORE clearing source slot
                CropData resultCrop = sourceSlot.CurrentCrop.nextLevelCrop;
                CropTier originalTier = sourceSlot.CurrentCrop.tier;

                this._slot.SetCrop(resultCrop);
                sourceSlot.ClearSlot();
                draggedItem._dropHandled = true;

                // Award XP using cached tier
                AwardMergeXP(originalTier);
            }
        }
    }

    /// <summary>
    /// When the user drops a crop outside any slot collider, 
    /// find the nearest grid slot and attempt to move/merge there.
    /// </summary>
    private void TrySnapToNearestSlot()
    {
        if (GridManager.Instance == null) return;
        if (_slot.IsEmpty) return; // Already cleared by something else

        Vector3 dropWorldPos = _visualTransform.position;
        GridSlot nearest = GridManager.Instance.GetNearestSlot(dropWorldPos);

        // Don't snap to self
        if (nearest == null || nearest == _slot) return;

        // Try to move or merge
        if (nearest.IsEmpty)
        {
            // Move to empty slot
            nearest.SetCrop(_slot.CurrentCrop);
            _slot.ClearSlot();
            _dropHandled = true;
        }
        else if (nearest.CurrentCrop != null && _slot.CurrentCrop != null &&
                 nearest.CurrentCrop.cropName == _slot.CurrentCrop.cropName &&
                 _slot.CurrentCrop.nextLevelCrop != null)
        {
            // Cache logic for Snap-To-Merge as well
            CropData resultCrop = _slot.CurrentCrop.nextLevelCrop;
            CropTier originalTier = _slot.CurrentCrop.tier;

            nearest.SetCrop(resultCrop);
            
            // Award XP before clearing
            AwardMergeXP(originalTier);

            _slot.ClearSlot();
            _dropHandled = true;
        }
        // If nearest slot has a different crop, just snap back to original position (do nothing)
    }

    /// <summary>
    /// Handles clicking on a slot.
    /// - If locked and adjacent to unlocked: show unlock prompt.
    /// - If empty and unlocked: open inventory panel.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Ignore if this was part of a drag operation
        if (_isDragging) return;

        // Locked slot click: attempt unlock
        if (_slot.IsLocked)
        {
            TryShowUnlockPrompt();
            return;
        }

        // Only open inventory if the slot is empty
        if (_slot.IsEmpty)
        {
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.Show(_slot);
            }
        }
    }

    /// <summary>
    /// Show unlock cost and attempt to unlock a locked slot.
    /// </summary>
    private void TryShowUnlockPrompt()
    {
        if (SlotUnlockManager.Instance == null) return;

        int x = _slot.X;
        int y = _slot.Y;

        // Only allow unlocking if adjacent to already-unlocked territory
        if (!SlotUnlockManager.Instance.IsAdjacentToUnlocked(x, y))
        {
            return; // Too far from unlocked area, do nothing
        }

        int cost = SlotUnlockManager.Instance.GetUnlockCost(x, y);

        // Attempt to unlock (spends coins automatically)
        if (SlotUnlockManager.Instance.TryUnlockSlot(x, y))
        {
            Debug.Log($"Slot ({x},{y}) acildi! Maliyet: {cost} coin");
        }
        else
        {
            Debug.Log($"Slot ({x},{y}) acilamadi. Maliyet: {cost} coin. Yetersiz bakiye.");
        }
    }

    private void AwardMergeXP(CropTier tier)
    {
        if (LevelManager.Instance == null) return;

        float xp = 10f; // T1 base
        switch (tier)
        {
            case CropTier.Common: xp = 10; break;
            case CropTier.Uncommon: xp = 25; break;
            case CropTier.Rare: xp = 60; break;
            case CropTier.Epic: xp = 150; break;
            case CropTier.Legendary: xp = 400; break;
        }

        LevelManager.Instance.AddXP(xp);
    }
}
