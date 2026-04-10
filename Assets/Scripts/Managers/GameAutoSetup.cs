using UnityEngine;

public class GameAutoSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        if (FindObjectOfType<GameAutoSetup>() == null)
        {
            GameObject obj = new GameObject("GameAutoSetup");
            obj.AddComponent<GameAutoSetup>();
        }
    }

    private void Awake()
    {
        // Execute only in Play Mode
        if (!Application.isPlaying) return;

        // =============================================
        //  STEP 1: Ensure Singletons exist in scene
        // =============================================
        EnsureSingleton<InventoryManager>("InventoryManager");
        EnsureSingleton<CrateManager>("CrateManager");

        // =============================================
        //  STEP 2: Create all 5 crop types in memory
        // =============================================
        Sprite baseSprite = Resources.Load<Sprite>("WhiteSquare");

        // --- Common: Havuç (Carrot) ---
        CropData carrot = ScriptableObject.CreateInstance<CropData>();
        carrot.cropName = "Havuç";
        carrot.tier = CropTier.Common;
        carrot.coinPerTick = 1f;
        carrot.cropSprite = baseSprite;
        carrot.cropColor = new Color(1f, 0.5f, 0f); // Orange

        // --- Uncommon: Elma (Apple) ---
        CropData apple = ScriptableObject.CreateInstance<CropData>();
        apple.cropName = "Elma";
        apple.tier = CropTier.Uncommon;
        apple.coinPerTick = 3f;
        apple.cropSprite = baseSprite;
        apple.cropColor = Color.red;

        // --- Rare: Çilek (Strawberry) ---
        CropData strawberry = ScriptableObject.CreateInstance<CropData>();
        strawberry.cropName = "Çilek";
        strawberry.tier = CropTier.Rare;
        strawberry.coinPerTick = 8f;
        strawberry.cropSprite = baseSprite;
        strawberry.cropColor = new Color(1f, 0.4f, 0.7f); // Pink

        // --- Epic: Üzüm (Grape) ---
        CropData grape = ScriptableObject.CreateInstance<CropData>();
        grape.cropName = "Üzüm";
        grape.tier = CropTier.Epic;
        grape.coinPerTick = 20f;
        grape.cropSprite = baseSprite;
        grape.cropColor = new Color(0.5f, 0f, 0.8f); // Purple

        // --- Legendary: Altın Elma (Golden Apple) ---
        CropData goldenApple = ScriptableObject.CreateInstance<CropData>();
        goldenApple.cropName = "Altın Elma";
        goldenApple.tier = CropTier.Legendary;
        goldenApple.coinPerTick = 50f;
        goldenApple.cropSprite = baseSprite;
        goldenApple.cropColor = new Color(1f, 0.84f, 0f); // Gold

        // =============================================
        //  STEP 3: Set up merge chain
        //  Havuç → Elma → Çilek → Üzüm → Altın Elma
        // =============================================
        carrot.nextLevelCrop = apple;
        apple.nextLevelCrop = strawberry;
        strawberry.nextLevelCrop = grape;
        grape.nextLevelCrop = goldenApple;
        goldenApple.nextLevelCrop = null; // Max level

        // =============================================
        //  STEP 4: Configure Crate (Bronze Crate)
        // =============================================
        CrateData bronzeCrate = new CrateData();
        bronzeCrate.crateName = "Bronz Sandık";
        bronzeCrate.cost = 10;
        bronzeCrate.drops = new CrateDropEntry[]
        {
            new CrateDropEntry { crop = carrot,      weight = 50f },  // ~56%
            new CrateDropEntry { crop = apple,       weight = 25f },  // ~28%
            new CrateDropEntry { crop = strawberry,  weight = 10f },  // ~11%
            new CrateDropEntry { crop = grape,        weight = 4f },  // ~4.5%
            new CrateDropEntry { crop = goldenApple,  weight = 1f },  // ~1.1%
        };

        // Assign crate to CrateManager
        if (CrateManager.Instance != null)
        {
            CrateManager.Instance.currentCrate = bronzeCrate;
        }

        // =============================================
        //  STEP 5: Remove legacy "Havuç Al" button
        // =============================================
        foreach (var btn in FindObjectsOfType<UnityEngine.UI.Button>(true))
        {
            var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null && tmp.text.Contains("Havuç"))
            {
                Destroy(btn.gameObject);
                break;
            }
        }

        // =============================================
        //  STEP 6: Configure Grid for tighter, smaller layout
        // =============================================
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.columns = 5;
            gridManager.rows = 5;
            gridManager.spacing = 0.85f; // Tighter spacing
        }

        // =============================================
        //  STEP 7: Set IncomeManager to 1-second ticks
        // =============================================
        IncomeManager incomeManager = FindObjectOfType<IncomeManager>();
        if (incomeManager != null)
        {
            incomeManager.collectionInterval = 1f; // 1 second ticks for real-time income
        }
    }

    /// <summary>
    /// Ensures a singleton MonoBehaviour exists in the scene. Creates it if missing.
    /// </summary>
    private T EnsureSingleton<T>(string objectName) where T : MonoBehaviour
    {
        T existing = FindObjectOfType<T>();
        if (existing == null)
        {
            GameObject obj = new GameObject(objectName);
            existing = obj.AddComponent<T>();
        }
        return existing;
    }
}
