using UnityEngine;

public class GameAutoSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        if (FindAnyObjectByType<GameAutoSetup>() == null)
        {
            GameObject obj = new GameObject("GameAutoSetup");
            obj.AddComponent<GameAutoSetup>();
        }
    }

    private void Awake()
    {
        if (!Application.isPlaying) return;

        // =============================================
        //  STEP 1: Ensure Singletons
        // =============================================
        EnsureSingleton<CurrencyManager>("CurrencyManager");
        EnsureSingleton<InventoryManager>("InventoryManager");
        EnsureSingleton<CrateManager>("CrateManager");
        EnsureSingleton<LevelManager>("LevelManager");
        EnsureSingleton<BoostManager>("BoostManager");
        EnsureSingleton<GameContentGenerator>("GameContentGenerator");
        EnsureSingleton<ResearchManager>("ResearchManager");
        EnsureSingleton<FactoryManager>("FactoryManager");
        EnsureSingleton<GameMessageManager>("GameMessageManager");
        EnsureSingleton<PrestigeManager>("PrestigeManager");

        // SlotUnlockManager must exist BEFORE GridManager.Start() runs
        SlotUnlockManager unlockMgr = EnsureSingleton<SlotUnlockManager>("SlotUnlockManager");

        // =============================================
        //  STEP 2: Grid layout — 20 columns x 25 rows
        //  Initially unlock a 6x5 patch in the center-bottom
        // =============================================
        const int columns = 20;
        const int rows    = 25;
        const float spacing = 0.7f;

        // Centered starting region: cols 7-12, rows 10-14
        const int unlockStartCol = 7;
        const int unlockStartRow = 10;
        const int unlockCols     = 6;
        const int unlockRows     = 5;

        unlockMgr.Initialize(unlockStartCol, unlockStartRow, unlockCols, unlockRows);

        GridManager gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager != null)
        {
            gridManager.columns = columns;
            gridManager.rows    = rows;
            gridManager.spacing = spacing;
        }

        // =============================================
        //  STEP 6: IncomeManager tick rate
        // =============================================
        IncomeManager inc = EnsureSingleton<IncomeManager>("IncomeManager");
        inc.gridManager = gridManager;
        inc.collectionInterval = 1f;

        // =============================================
        //  STEP 7: Camera setup
        //  Add CameraController to main camera and configure bounds.
        //  Camera starts centered on the unlocked region.
        // =============================================
        Camera cam = Camera.main;
        if (cam != null)
        {
            // Static, zoomed-out overview for farm screen (no in-farm camera panning).
            cam.orthographicSize = 8f;

            // Add CameraController if not already present
            CameraController cc = cam.GetComponent<CameraController>();
            if (cc == null) cc = cam.gameObject.AddComponent<CameraController>();

            // Calculate world extents of the full grid
            float halfW = (columns - 1) * spacing * 0.5f;
            float halfH = (rows    - 1) * spacing * 0.5f;

            // Set full grid bounds for the camera controller
            cc.SetBounds(-halfW, halfW, -halfH, halfH);

            // Keep camera centered for a stable board overview.
            cam.transform.position = new Vector3(0f, 0f, cam.transform.position.z);

            // Disable in-farm pan/zoom so horizontal swipes always change screens.
            cc.enabled = false;
        }
    }

    private T EnsureSingleton<T>(string objectName) where T : MonoBehaviour
    {
        T existing = FindAnyObjectByType<T>();
        if (existing == null)
        {
            GameObject obj = new GameObject(objectName);
            existing = obj.AddComponent<T>();
        }
        return existing;
    }
}
