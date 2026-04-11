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
        EnsureSingleton<InventoryManager>("InventoryManager");
        EnsureSingleton<CrateManager>("CrateManager");
        EnsureSingleton<LevelManager>("LevelManager");
        EnsureSingleton<BoostManager>("BoostManager");
        EnsureSingleton<GameContentGenerator>("GameContentGenerator");

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
            // Slightly smaller ortho size so the farm feels large
            cam.orthographicSize = 4f;

            // Add CameraController if not already present
            CameraController cc = cam.GetComponent<CameraController>();
            if (cc == null) cc = cam.gameObject.AddComponent<CameraController>();

            // Calculate world extents of the full grid
            float halfW = (columns - 1) * spacing * 0.5f;
            float halfH = (rows    - 1) * spacing * 0.5f;

            // Set full grid bounds for the camera controller
            cc.SetBounds(-halfW, halfW, -halfH, halfH);

            // Position camera to show the center of the unlocked patch
            float unlockedCenterX = (unlockStartCol + unlockCols  * 0.5f - columns * 0.5f) * spacing;
            float unlockedCenterY = (unlockStartRow + unlockRows  * 0.5f - rows    * 0.5f) * spacing;

            float orthoH = cam.orthographicSize;
            float orthoW = orthoH * cam.aspect;

            // Clamp to within bounds
            float minX = -halfW + orthoW;
            float maxX =  halfW - orthoW;
            float minY = -halfH + orthoH;
            float maxY =  halfH - orthoH;

            if (minX > maxX) minX = maxX = 0f;
            if (minY > maxY) minY = maxY = 0f;

            Vector3 startPos = new Vector3(
                Mathf.Clamp(unlockedCenterX, minX, maxX),
                Mathf.Clamp(unlockedCenterY, minY, maxY),
                cam.transform.position.z
            );
            cam.transform.position = startPos;
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
