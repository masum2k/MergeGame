using UnityEngine;

public class GameAutoSetup : MonoBehaviour
{
    private const string FarmLayoutVersionKey = "FarmLayoutVersion";
    private const int CurrentFarmLayoutVersion = 2;
    private const string FarmBackgroundResourcePath = "Farm/ANAMENU-Background";
    private const string FarmBackgroundObjectName = "FarmBackground_Auto";
    private const int FarmBackgroundSortingOrder = -500;

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
        EnsureSingleton<SaveCoordinator>("SaveCoordinator");
        EnsureSingleton<MobileRuntimeBootstrap>("MobileRuntimeBootstrap");

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
        EnsureSingleton<ProgressionTelemetryManager>("ProgressionTelemetryManager");
        EnsureSingleton<QuestManager>("QuestManager");
        EnsureSingleton<StreakRewardManager>("StreakRewardManager");

        // Apply farm layout migration before loading unlock / grid save data.
        MigrateFarmLayoutSaveDataIfNeeded();

        // SlotUnlockManager must exist BEFORE GridManager.Start() runs
        SlotUnlockManager unlockMgr = EnsureSingleton<SlotUnlockManager>("SlotUnlockManager");

        Camera cam = Camera.main;

        // =============================================
        //  STEP 2: Grid layout — 20 columns x 25 rows
        //  Initially unlock a 4x5 patch in the visible farm area
        // =============================================
        const int columns = 20;
        const int rows    = 25;
        const float spacing = 0.7f;
        const int unlockCols = 4;
        const int unlockRows = 5;

        int unlockMinCol = 1;
        int unlockMaxCol = columns - 2;
        int unlockMinRow = 1;
        int unlockMaxRow = rows - 2;

        if (cam != null)
        {
            CalculateUnlockAreaFromFarmViewport(
                cam,
                columns,
                rows,
                spacing,
                out unlockMinCol,
                out unlockMaxCol,
                out unlockMinRow,
                out unlockMaxRow);
        }

        CenterStartPatchWithinLimits(
            unlockCols,
            unlockRows,
            unlockMinCol,
            unlockMaxCol,
            unlockMinRow,
            unlockMaxRow,
            out int unlockStartCol,
            out int unlockStartRow);

        unlockMgr.SetUnlockLimits(unlockMinCol, unlockMaxCol, unlockMinRow, unlockMaxRow);
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
        if (cam != null)
        {
            // Keep farm focused around active area on portrait screens.
            cam.orthographicSize = 6.2f;

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

            EnsureFarmBackground(cam, gridManager != null ? gridManager.transform : null);
        }
    }

    private static void EnsureFarmBackground(Camera cam, Transform parent)
    {
        Sprite backgroundSprite = null;
        Texture2D backgroundTexture = Resources.Load<Texture2D>(FarmBackgroundResourcePath);
        if (backgroundTexture != null)
        {
            // Build a centered sprite so world placement is stable even if importer pivots vary.
            backgroundSprite = Sprite.Create(
                backgroundTexture,
                new Rect(0f, 0f, backgroundTexture.width, backgroundTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
        else
        {
            backgroundSprite = Resources.Load<Sprite>(FarmBackgroundResourcePath);
        }

        if (backgroundSprite == null)
        {
            Debug.LogWarning("GameAutoSetup: Farm background sprite not found at Resources/" + FarmBackgroundResourcePath);
            return;
        }

        GameObject backgroundObj = GameObject.Find(FarmBackgroundObjectName);
        if (backgroundObj == null)
        {
            backgroundObj = new GameObject(FarmBackgroundObjectName);
        }

        if (parent != null && backgroundObj.transform.parent != parent)
        {
            backgroundObj.transform.SetParent(parent, false);
        }

        backgroundObj.transform.localPosition = Vector3.zero;
        backgroundObj.transform.localRotation = Quaternion.identity;

        SpriteRenderer backgroundRenderer = backgroundObj.GetComponent<SpriteRenderer>();
        if (backgroundRenderer == null)
        {
            backgroundRenderer = backgroundObj.AddComponent<SpriteRenderer>();
        }

        backgroundRenderer.sprite = backgroundSprite;
        backgroundRenderer.sortingOrder = FarmBackgroundSortingOrder;
        backgroundRenderer.color = new Color(1f, 1f, 1f, 0.9f);

        Vector2 spriteSize = backgroundSprite.bounds.size;
        if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
        {
            return;
        }

        float visibleWorldHeight = cam.orthographicSize * 2f;
        float visibleWorldWidth = visibleWorldHeight * cam.aspect;
        float coverScale = Mathf.Max(visibleWorldWidth / spriteSize.x, visibleWorldHeight / spriteSize.y);

        backgroundObj.transform.localScale = new Vector3(coverScale, coverScale, 1f);
    }

    private static void MigrateFarmLayoutSaveDataIfNeeded()
    {
        int savedVersion = SecurePlayerPrefs.GetInt(FarmLayoutVersionKey, 0);
        if (savedVersion >= CurrentFarmLayoutVersion)
        {
            return;
        }

        // Force a fresh farm board when layout rules change (e.g. 5x5 -> 4x5 start area).
        SecurePlayerPrefs.DeleteKey("UnlockedSlots");
        SecurePlayerPrefs.DeleteKey("GridState");
        SecurePlayerPrefs.SetInt(FarmLayoutVersionKey, CurrentFarmLayoutVersion);
    }

    private static void CalculateUnlockAreaFromFarmViewport(
        Camera cam,
        int columns,
        int rows,
        float spacing,
        out int minCol,
        out int maxCol,
        out int minRow,
        out int maxRow)
    {
        // Matches the runtime UI layout in UIManager + ScreenCarouselUI.
        const float topHudHeightPx = 90f;
        const float clickButtonBottomPx = 120f;
        const float clickButtonSizePx = 120f;

        float topScreenY = Mathf.Clamp(Screen.height - topHudHeightPx, 0f, Screen.height);
        float bottomScreenY = Mathf.Clamp(clickButtonBottomPx + clickButtonSizePx, 0f, Screen.height);
        if (bottomScreenY >= topScreenY)
        {
            bottomScreenY = 0f;
            topScreenY = Screen.height;
        }

        float leftWorldX = cam.ScreenToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane)).x;
        float rightWorldX = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0f, cam.nearClipPlane)).x;
        float bottomWorldY = cam.ScreenToWorldPoint(new Vector3(0f, bottomScreenY, cam.nearClipPlane)).y;
        float topWorldY = cam.ScreenToWorldPoint(new Vector3(0f, topScreenY, cam.nearClipPlane)).y;

        float offsetX = (columns - 1) * spacing * 0.5f;
        float offsetY = (rows - 1) * spacing * 0.5f;

        minCol = Mathf.CeilToInt((leftWorldX + offsetX) / spacing);
        maxCol = Mathf.FloorToInt((rightWorldX + offsetX) / spacing);
        minRow = Mathf.CeilToInt((bottomWorldY + offsetY) / spacing);
        maxRow = Mathf.FloorToInt((topWorldY + offsetY) / spacing);

        // Keep unlockable area away from the hard border slots.
        minCol = Mathf.Clamp(minCol, 1, columns - 2);
        maxCol = Mathf.Clamp(maxCol, 1, columns - 2);
        minRow = Mathf.Clamp(minRow, 1, rows - 2);
        maxRow = Mathf.Clamp(maxRow, 1, rows - 2);

        if (minCol > maxCol)
        {
            minCol = 1;
            maxCol = columns - 2;
        }
        if (minRow > maxRow)
        {
            minRow = 1;
            maxRow = rows - 2;
        }
    }

    private static void CenterStartPatchWithinLimits(
        int patchCols,
        int patchRows,
        int minCol,
        int maxCol,
        int minRow,
        int maxRow,
        out int startCol,
        out int startRow)
    {
        int maxStartCol = Mathf.Max(minCol, maxCol - patchCols + 1);
        int maxStartRow = Mathf.Max(minRow, maxRow - patchRows + 1);

        int centeredCol = (minCol + maxCol - patchCols + 1) / 2;
        int centeredRow = (minRow + maxRow - patchRows + 1) / 2;

        startCol = Mathf.Clamp(centeredCol, minCol, maxStartCol);
        startRow = Mathf.Clamp(centeredRow, minRow, maxStartRow);
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
