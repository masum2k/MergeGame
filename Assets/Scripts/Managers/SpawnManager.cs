using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    
    [Header("Spawn Settings")]
    [Tooltip("The crop Data to spawn when buying from the UI.")]
    public CropData cropToSpawn;
    
    public int cropCost = 10;

    private void Awake()
    {
        if (cropCost <= 0) cropCost = 10;
    }

    /// <summary>
    /// This method is designed to be called directly from a UI Button's "On Click ()" event.
    /// </summary>
    public void BuyAndSpawnCrop()
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager Reference is missing in SpawnManager!");
            return;
        }

        if (cropToSpawn == null)
        {
            Debug.LogError("No CropData assigned to spawn in SpawnManager!");
            return;
        }

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.Coin >= cropCost)
        {
            // First check if there's an empty slot before taking money
            GridSlot emptySlot = gridManager.GetEmptySlot();
            if (emptySlot != null)
            {
                // Process the payment
                bool spent = CurrencyManager.Instance.SpendCoin(cropCost);
                if (spent)
                {
                    // Update slot data and visuals
                    emptySlot.SetCrop(cropToSpawn);
                    Debug.Log($"Spawned {cropToSpawn.cropName} at slot ({emptySlot.X}, {emptySlot.Y})");
                }
            }
            else
            {
                Debug.LogWarning("Cannot spawn: The grid board is completely full!");
            }
        }
        else
        {
            Debug.LogWarning($"Not enough coins to buy! Cost: {cropCost}");
        }
    }
}
