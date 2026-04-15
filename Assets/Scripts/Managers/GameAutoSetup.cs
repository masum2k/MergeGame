using UnityEngine;

public class GameAutoSetup : MonoBehaviour
{
    private const string FarmLayoutVersionKey = "FarmLayoutVersion";
    private const int CurrentFarmLayoutVersion = 4;
    private const string FarmBackgroundResourcePath = "Farm/ANAMENU-Background";
    private const string FarmBackgroundObjectName = "FarmBackground_Auto";
    private const int FarmBackgroundSortingOrder = -500;
    private const float FarmBackgroundOffsetX = 0f;
    private const float FarmBackgroundOffsetY = 0f;
    private const string ManualCompensationKey = "ManualCompensation_20260416";
    private const int ManualCompensationCoin = 4000;
    private const int ManualCompensationGem = 200;
    // Optical center nudge: slightly right looks more centered with this artwork framing.
    private const float FarmBackgroundVisualNudgeX = 0.00f;

    private const int FarmBoardColumns = 8;
    private const int FarmBoardRows = 11;
    private const int FarmInitialUnlockColumns = 8;
    private const int FarmInitialUnlockRows = 9;
    private const float FarmGridOffsetX = 0f;
    private const float FarmGridOffsetY = 0.04f;

    // Pixel bounds (source texture coordinates, top-left origin) for the playable 8x11 marble board.
    // IMPORTANT: These were measured on the original ANAMENU-Background source size.
    // Unity may downscale imported textures at runtime (maxTextureSize), so normalize against
    // this reference size instead of the runtime texture width/height.
    private const float FarmBoardSourceTextureWidthPx = 1568f;
    private const float FarmBoardSourceTextureHeightPx = 2676f;
    private const float FarmBoardLeftPx = 156f;
    private const float FarmBoardRightPx = 1410f;
    private const float FarmBoardTopPx = 389f;
    private const float FarmBoardBottomPx = 2110f;

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

        if (GameContentGenerator.Instance != null)
        {
            GameContentGenerator.Instance.EnsureCratesGenerated();
        }

        GrantManualCompensationIfNeeded();

        // Apply farm layout migration before loading unlock / grid save data.
        MigrateFarmLayoutSaveDataIfNeeded();

        GridManager gridManager = FindAnyObjectByType<GridManager>();
        // SlotUnlockManager must exist BEFORE GridManager.Start() runs
        SlotUnlockManager unlockMgr = EnsureSingleton<SlotUnlockManager>("SlotUnlockManager");

        Camera cam = Camera.main;

        CameraController cameraController = null;
        if (cam != null)
        {
            // Keep farm focused around active area on portrait screens.
            cam.orthographicSize = 6.2f;

            cameraController = cam.GetComponent<CameraController>();
            if (cameraController == null)
            {
                cameraController = cam.gameObject.AddComponent<CameraController>();
            }

            // Keep camera centered so background framing remains consistent.
            cam.transform.position = new Vector3(0f, 0f, cam.transform.position.z);
        }

        SpriteRenderer farmBackgroundRenderer = null;
        if (cam != null)
        {
            farmBackgroundRenderer = EnsureFarmBackground(cam);
        }

        bool hasBoardRect = TryGetFarmBoardWorldRect(farmBackgroundRenderer, out Vector2 boardCenter, out float boardWidth, out float boardHeight);

        float spacing = 0.7f;
        if (hasBoardRect)
        {
            float cellWidth = boardWidth / FarmBoardColumns;
            float cellHeight = boardHeight / FarmBoardRows;
            spacing = Mathf.Min(cellWidth, cellHeight);
        }

        if (gridManager != null)
        {
            gridManager.columns = FarmBoardColumns;
            gridManager.rows = FarmBoardRows;
            gridManager.spacing = spacing;

            if (hasBoardRect)
            {
                Vector3 gridPos = gridManager.transform.position;
                gridPos.x = boardCenter.x + FarmGridOffsetX;
                gridPos.y = boardCenter.y + FarmGridOffsetY;
                gridManager.transform.position = gridPos;
            }
        }

        int unlockMinCol = 0;
        int unlockMaxCol = FarmBoardColumns - 1;
        int unlockMinRow = 1;
        int unlockMaxRow = FarmBoardRows - 2;

        CenterStartPatchWithinLimits(
            FarmInitialUnlockColumns,
            FarmInitialUnlockRows,
            unlockMinCol,
            unlockMaxCol,
            unlockMinRow,
            unlockMaxRow,
            out int unlockStartCol,
            out int unlockStartRow);

        unlockMgr.SetUnlockLimits(unlockMinCol, unlockMaxCol, unlockMinRow, unlockMaxRow);
        unlockMgr.Initialize(unlockStartCol, unlockStartRow, FarmInitialUnlockColumns, FarmInitialUnlockRows);
        unlockMgr.TryRecenterIfOnlyInitialArea(unlockStartCol, unlockStartRow, FarmInitialUnlockColumns, FarmInitialUnlockRows);

        // =============================================
        //  STEP 6: IncomeManager tick rate
        // =============================================
        IncomeManager inc = EnsureSingleton<IncomeManager>("IncomeManager");
        inc.gridManager = gridManager;
        inc.collectionInterval = 1f;

        // =============================================
        //  STEP 7: Camera setup (pan/zoom disabled in farm flow)
        // =============================================
        if (cam != null && cameraController != null)
        {
            float halfW = (FarmBoardColumns - 1) * spacing * 0.5f;
            float halfH = (FarmBoardRows - 1) * spacing * 0.5f;
            float centerX = hasBoardRect ? boardCenter.x + FarmGridOffsetX : 0f;
            float centerY = hasBoardRect ? boardCenter.y + FarmGridOffsetY : 0f;

            cameraController.SetBounds(centerX - halfW, centerX + halfW, centerY - halfH, centerY + halfH);

            // Disable in-farm pan/zoom so horizontal swipes always change screens.
            cameraController.enabled = false;
        }
    }

    private static SpriteRenderer EnsureFarmBackground(Camera cam)
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
            // Some imports expose only Sprite (e.g. multiple-sprite mode). In that case,
            // rebuild a full-texture sprite so bounds/pivot stay stable for board mapping.
            Sprite loadedSprite = Resources.Load<Sprite>(FarmBackgroundResourcePath);
            if (loadedSprite != null && loadedSprite.texture != null)
            {
                Texture2D tex = loadedSprite.texture;
                backgroundSprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
            else
            {
                backgroundSprite = loadedSprite;
            }
        }

        if (backgroundSprite == null)
        {
            Debug.LogWarning("GameAutoSetup: Farm background sprite not found at Resources/" + FarmBackgroundResourcePath);
            return null;
        }

        GameObject backgroundObj = GameObject.Find(FarmBackgroundObjectName);
        if (backgroundObj == null)
        {
            backgroundObj = new GameObject(FarmBackgroundObjectName);
        }

        if (backgroundObj.transform.parent != null)
        {
            backgroundObj.transform.SetParent(null, false);
        }

        backgroundObj.transform.position = new Vector3(FarmBackgroundOffsetX, FarmBackgroundOffsetY, 0f);
        backgroundObj.transform.rotation = Quaternion.identity;

        SpriteRenderer backgroundRenderer = backgroundObj.GetComponent<SpriteRenderer>();
        if (backgroundRenderer == null)
        {
            backgroundRenderer = backgroundObj.AddComponent<SpriteRenderer>();
        }

        backgroundRenderer.sprite = backgroundSprite;
        backgroundRenderer.sortingOrder = FarmBackgroundSortingOrder;
        backgroundRenderer.color = Color.white;

        Vector2 spriteSize = backgroundSprite.bounds.size;
        if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
        {
            return backgroundRenderer;
        }

        float visibleWorldHeight = cam.orthographicSize * 2f;
        float visibleWorldWidth = visibleWorldHeight * cam.aspect;
        float coverScale = Mathf.Max(visibleWorldWidth / spriteSize.x, visibleWorldHeight / spriteSize.y);

        backgroundObj.transform.localScale = new Vector3(coverScale, coverScale, 1f);

        // Anchor horizontal placement to safe-area center so portrait aspect differences
        // (16:9, 18:9, notches) do not introduce manual drift.
        float safeCenterScreenX = Screen.safeArea.x + (Screen.safeArea.width * 0.5f);
        float safeCenterWorldX = cam.ScreenToWorldPoint(new Vector3(safeCenterScreenX, 0f, cam.nearClipPlane)).x;
        backgroundObj.transform.position = new Vector3(
            safeCenterWorldX + FarmBackgroundOffsetX + FarmBackgroundVisualNudgeX,
            FarmBackgroundOffsetY,
            0f);
        return backgroundRenderer;
    }

    private static bool TryGetFarmBoardWorldRect(SpriteRenderer backgroundRenderer, out Vector2 center, out float width, out float height)
    {
        center = Vector2.zero;
        width = 0f;
        height = 0f;

        if (backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return false;
        }

        Texture2D texture = backgroundRenderer.sprite.texture;
        if (texture == null || texture.width <= 0 || texture.height <= 0)
        {
            return false;
        }

        Bounds bounds = backgroundRenderer.bounds;
        float texW = texture.width;
        float texH = texture.height;

        float leftNorm = FarmBoardLeftPx / FarmBoardSourceTextureWidthPx;
        float rightNorm = FarmBoardRightPx / FarmBoardSourceTextureWidthPx;
        float topNorm = FarmBoardTopPx / FarmBoardSourceTextureHeightPx;
        float bottomNorm = FarmBoardBottomPx / FarmBoardSourceTextureHeightPx;

        leftNorm = Mathf.Clamp01(leftNorm);
        rightNorm = Mathf.Clamp01(rightNorm);
        topNorm = Mathf.Clamp01(topNorm);
        bottomNorm = Mathf.Clamp01(bottomNorm);

        float left = bounds.min.x + bounds.size.x * leftNorm;
        float right = bounds.min.x + bounds.size.x * rightNorm;
        float top = bounds.max.y - bounds.size.y * topNorm;
        float bottom = bounds.max.y - bounds.size.y * bottomNorm;

        width = Mathf.Max(0.001f, right - left);
        height = Mathf.Max(0.001f, top - bottom);
        center = new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f);
        return true;
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

    private static void GrantManualCompensationIfNeeded()
    {
        if (CurrencyManager.Instance == null)
            return;

        if (SecurePlayerPrefs.GetInt(ManualCompensationKey, 0) == 1)
            return;

        CurrencyManager.Instance.AddCoin(ManualCompensationCoin);
        CurrencyManager.Instance.AddGem(ManualCompensationGem);
        SecurePlayerPrefs.SetInt(ManualCompensationKey, 1);
        SaveCoordinator.MarkDirty();
        Debug.Log("GameAutoSetup: Manual compensation granted (4000 coin, 200 gem).");
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
