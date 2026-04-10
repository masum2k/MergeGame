using UnityEngine;

public class GridSlot : MonoBehaviour
{
    [Header("Slot Information")]
    public int X { get; private set; }
    public int Y { get; private set; }

    [Header("State")]
    public CropData CurrentCrop;

    private SpriteRenderer _cropVisualRenderer;

    private void Awake()
    {
        // Dynamically create a child object to display the crop's sprite
        // so it overlays the default background
        GameObject cropVisualObj = new GameObject("CropVisual");
        cropVisualObj.transform.SetParent(this.transform, false);
        cropVisualObj.transform.localPosition = Vector3.zero;
        
        // Scale it to fit inside the smaller, tighter grid slots
        cropVisualObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        
        _cropVisualRenderer = cropVisualObj.AddComponent<SpriteRenderer>();
        // Ensure crop is rendered in front of the background
        _cropVisualRenderer.sortingOrder = 1;
    }

    public GameObject GetVisualObject()
    {
        return _cropVisualRenderer.gameObject;
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
    /// Assigns a crop to this slot.
    /// </summary>
    public void SetCrop(CropData newCrop)
    {
        CurrentCrop = newCrop;
        if (_cropVisualRenderer != null && newCrop != null)
        {
            _cropVisualRenderer.sprite = newCrop.cropSprite;
            _cropVisualRenderer.color = newCrop.cropColor;
        }
    }

    /// <summary>
    /// Removes the crop from this slot, making it empty.
    /// </summary>
    public void ClearSlot()
    {
        CurrentCrop = null;
        if (_cropVisualRenderer != null)
        {
            _cropVisualRenderer.sprite = null;
            _cropVisualRenderer.color = Color.white;
        }
    }
}
