using System;
using UnityEngine;

/// <summary>
/// Manages crate purchases. Handles coin deduction, random drop, and inventory insertion.
/// Singleton — auto-created by GameAutoSetup.
/// </summary>
public class CrateManager : MonoBehaviour
{
    public static CrateManager Instance { get; private set; }

    /// <summary>
    /// The currently available crate in the market.
    /// Set by GameAutoSetup at runtime.
    /// </summary>
    public CrateData currentCrate;

    /// <summary>
    /// Fired when a crate is successfully opened. Passes the dropped CropData.
    /// UI can listen to show a notification.
    /// </summary>
    public event Action<CropData> OnCrateOpened;

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
    /// Attempts to open the current crate. Deducts coins and adds the drop to inventory.
    /// Returns the dropped crop, or null if purchase failed.
    /// </summary>
    public CropData OpenCrate()
    {
        if (currentCrate == null)
        {
            Debug.LogWarning("CrateManager: No crate configured!");
            return null;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("CrateManager: CurrencyManager not found!");
            return null;
        }

        // Try to spend coins
        if (!CurrencyManager.Instance.SpendCoin(currentCrate.cost))
        {
            Debug.LogWarning($"Yeterli coin yok! Sandık fiyatı: {currentCrate.cost}");
            return null;
        }

        // Roll for a drop
        CropData drop = currentCrate.RollDrop();
        if (drop == null)
        {
            Debug.LogError("CrateManager: Drop table returned null!");
            // Refund
            CurrencyManager.Instance.AddCoin(currentCrate.cost);
            return null;
        }

        // Add to inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(drop);
        }

        Debug.Log($"[SANDIK] Sandıktan düştü: {drop.cropName} ({drop.tier})");
        OnCrateOpened?.Invoke(drop);

        return drop;
    }
}
