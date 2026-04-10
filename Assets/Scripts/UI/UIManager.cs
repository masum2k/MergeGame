using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI coinText;
    private TextMeshProUGUI energyText;
    private TextMeshProUGUI inventoryCountText;

    private void Start()
    {
        // Hook up the event listener when the script starts
        if (CurrencyManager.Instance != null)
        {
            // Initial text update
            UpdateCoinText(CurrencyManager.Instance.Coin);
            
            // Subscribe to the event so it updates automatically
            CurrencyManager.Instance.OnCoinChanged += UpdateCoinText;
        }

        // Initialize Energy UI Zero-Touch
        InitializeEnergyUI();
        ClickerManager.OnEnergyChanged += UpdateEnergyText;

        // Initialize Inventory Count UI
        InitializeInventoryCountUI();
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryCount;
        }

        // Ensure MarketUI exists under Canvas
        EnsureUIComponent<MarketUI>("MarketUI_Auto");

        // Ensure InventoryUI exists under Canvas
        EnsureUIComponent<InventoryUI>("InventoryUI_Auto");
    }

    private void OnDestroy()
    {
        // Unsubscribe when this UI is destroyed to prevent memory leaks
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged -= UpdateCoinText;
        }
        ClickerManager.OnEnergyChanged -= UpdateEnergyText;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryCount;
        }
    }

    private void UpdateCoinText(int coinValue)
    {
        if (coinText != null)
        {
            // Update the string on the screen
            coinText.text = $"Coin: {coinValue}";
        }
    }

    private void UpdateEnergyText(int current, int max)
    {
        if (energyText != null)
        {
            energyText.text = $"Enerji: {current}/{max}";
        }
    }

    private void UpdateInventoryCount()
    {
        if (inventoryCountText != null && InventoryManager.Instance != null)
        {
            int total = InventoryManager.Instance.GetTotalItemCount();
            inventoryCountText.text = $"Envanter: {total} besin";
        }
    }

    private void InitializeEnergyUI()
    {
        // Zero-Touch UI Generation for Energy
        GameObject energyObj = new GameObject("EnergyText_Auto");
        energyObj.transform.SetParent(this.transform, false);

        energyText = energyObj.AddComponent<TextMeshProUGUI>();
        
        // Match standard settings if possible, or use defaults
        energyText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        energyText.fontSize = 36;
        energyText.color = Color.white;
        energyText.text = "Enerji: -/-";

        // Positioning: Left-Middle
        RectTransform rt = energyText.rectTransform;
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(50, 0); // 50px padding from left
        rt.sizeDelta = new Vector2(300, 100);
    }

    private void InitializeInventoryCountUI()
    {
        // Zero-Touch UI Generation for Inventory Count
        GameObject invObj = new GameObject("InventoryCountText_Auto");
        invObj.transform.SetParent(this.transform, false);

        inventoryCountText = invObj.AddComponent<TextMeshProUGUI>();
        inventoryCountText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        inventoryCountText.fontSize = 28;
        inventoryCountText.color = new Color(0.8f, 0.9f, 1f);
        inventoryCountText.text = "Envanter: 0 besin";

        // Positioning: Left, below energy
        RectTransform rt = inventoryCountText.rectTransform;
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(50, -60); // Below energy text
        rt.sizeDelta = new Vector2(300, 60);
    }

    /// <summary>
    /// Ensures a UI component exists as a child of the Canvas.
    /// </summary>
    private T EnsureUIComponent<T>(string objectName) where T : MonoBehaviour
    {
        T existing = FindObjectOfType<T>();
        if (existing == null)
        {
            GameObject obj = new GameObject(objectName);
            obj.transform.SetParent(this.transform, false);
            existing = obj.AddComponent<T>();
        }
        return existing;
    }
}
