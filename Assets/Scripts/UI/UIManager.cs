using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIManager — Builds and manages the main HUD:
///   - Top bar with Coin, Gem counters and Market/Inventory buttons
///   - Bottom-center Clicker button (round, yellow)
///   - No energy/inventory text clutter
/// All generated at runtime (Zero-Touch).
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI coinText;

    // Auto-created references
    private TextMeshProUGUI gemText;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI screenTitleText;
    private Image xpBarFill;
    private GameObject topBar;
    private Button clickButtonRef;
    private TextMeshProUGUI clickButtonText;

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Build the top bar HUD
        BuildTopBar(canvas.transform);

        // Build the bottom-center clicker button
        BuildClickerButton(canvas.transform);

        // Hook up coin event
        if (CurrencyManager.Instance != null)
        {
            UpdateCoinText(CurrencyManager.Instance.Coin);
            CurrencyManager.Instance.OnCoinChanged += UpdateCoinText;

            UpdateGemText(CurrencyManager.Instance.Gem);
            CurrencyManager.Instance.OnGemChanged += UpdateGemText;
        }

        // Hook up level events
        if (LevelManager.Instance != null)
        {
            UpdateLevelUI(LevelManager.Instance.Level, LevelManager.Instance.CurrentXP, LevelManager.Instance.XPForNextLevel);
            LevelManager.Instance.OnXPChanged += UpdateLevelUI;
            LevelManager.Instance.OnLevelUp += HandleLevelUp;
        }

        // Hook up passive income feedback
        IncomeManager.OnIncomeCollected += HandleIncomeCollected;

        // Ensure InventoryUI exists under Canvas
        EnsureUIComponent<InventoryUI>("InventoryUI_Auto");

        // Ensure 4-screen carousel UI exists under Canvas
        EnsureUIComponent<ScreenCarouselUI>("ScreenCarouselUI_Auto");

        // Ensure right-side meta menu exists (hamburger button target)
        EnsureUIComponent<MetaMenuUI>("MetaMenuUI_Auto");

        // Ensure progression hub exists (streak only)
        EnsureUIComponent<ProgressionHubUI>("ProgressionHubUI_Auto");

        // Ensure crop compendium exists (triggered from farm page button)
        EnsureUIComponent<CropCompendiumUI>("CropCompendiumUI_Auto");
    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged -= UpdateCoinText;
            CurrencyManager.Instance.OnGemChanged -= UpdateGemText;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnXPChanged -= UpdateLevelUI;
            LevelManager.Instance.OnLevelUp -= HandleLevelUp;
        }

        ClickerManager.OnEnergyChanged -= UpdateClickButtonEnergy;
        
        IncomeManager.OnIncomeCollected -= HandleIncomeCollected;
    }

    // =============================================
    //  TOP BAR
    // =============================================

    private void BuildTopBar(Transform parent)
    {
        // --- Top Bar Panel ---
        topBar = new GameObject("TopBar");
        topBar.transform.SetParent(parent, false);

        Image barBg = topBar.AddComponent<Image>();
        barBg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

        RectTransform barRt = topBar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(0f, 90f);

        // --- Coin Icon (yellow circle) ---
        GameObject coinIcon = CreateCircleIcon(topBar.transform, new Color(1f, 0.85f, 0.1f), "CoinIcon");
        RectTransform coinIconRt = coinIcon.GetComponent<RectTransform>();
        coinIconRt.anchorMin = new Vector2(0f, 0.5f);
        coinIconRt.anchorMax = new Vector2(0f, 0.5f);
        coinIconRt.pivot = new Vector2(0f, 0.5f);
        coinIconRt.anchoredPosition = new Vector2(20f, 0f);
        coinIconRt.sizeDelta = new Vector2(40f, 40f);

        // Coin letter on icon
        AddIconLabel(coinIcon.transform, "C", Color.black);

        // --- Coin Text ---
        GameObject coinObj = new GameObject("CoinText_TopBar");
        coinObj.transform.SetParent(topBar.transform, false);
        coinText = coinObj.AddComponent<TextMeshProUGUI>();
        coinText.text = "0";
        coinText.fontSize = 28;
        coinText.fontStyle = FontStyles.Bold;
        coinText.color = new Color(1f, 0.9f, 0.3f);
        coinText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;

        RectTransform coinRt = coinObj.GetComponent<RectTransform>();
        coinRt.anchorMin = new Vector2(0f, 0.5f);
        coinRt.anchorMax = new Vector2(0f, 0.5f);
        coinRt.pivot = new Vector2(0f, 0.5f);
        coinRt.anchoredPosition = new Vector2(68f, 0f);
        coinRt.sizeDelta = new Vector2(120f, 50f);

        // --- Gem Icon (cyan diamond shape) ---
        GameObject gemIcon = CreateDiamondIcon(topBar.transform, new Color(0.2f, 0.9f, 1f), "GemIcon");
        RectTransform gemIconRt = gemIcon.GetComponent<RectTransform>();
        gemIconRt.anchorMin = new Vector2(0f, 0.5f);
        gemIconRt.anchorMax = new Vector2(0f, 0.5f);
        gemIconRt.pivot = new Vector2(0f, 0.5f);
        gemIconRt.anchoredPosition = new Vector2(200f, 0f);
        gemIconRt.sizeDelta = new Vector2(40f, 40f);

        // Gem letter on icon
        AddIconLabel(gemIcon.transform, "G", Color.black);

        // --- Gem Text ---
        GameObject gemObj = new GameObject("GemText_TopBar");
        gemObj.transform.SetParent(topBar.transform, false);
        gemText = gemObj.AddComponent<TextMeshProUGUI>();
        gemText.text = "0";
        gemText.fontSize = 28;
        gemText.fontStyle = FontStyles.Bold;
        gemText.color = new Color(0.3f, 0.95f, 1f);
        gemText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;

        RectTransform gemRt = gemObj.GetComponent<RectTransform>();
        gemRt.anchorMin = new Vector2(0f, 0.5f);
        gemRt.anchorMax = new Vector2(0f, 0.5f);
        gemRt.pivot = new Vector2(0f, 0.5f);
        gemRt.anchoredPosition = new Vector2(248f, 0f); // Positioned to the right of GemIcon (200 + offset)
        gemRt.sizeDelta = new Vector2(100f, 50f);

        // --- XP Bar (Bottom of TopBar) ---
        GameObject xpBarBg = new GameObject("XPBar_Bg");
        xpBarBg.transform.SetParent(topBar.transform, false);
        Image xpBgImg = xpBarBg.AddComponent<Image>();
        xpBgImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        RectTransform xpBgRt = xpBarBg.GetComponent<RectTransform>();
        xpBgRt.anchorMin = new Vector2(0f, 0f);
        xpBgRt.anchorMax = new Vector2(1f, 0f);
        xpBgRt.pivot = new Vector2(0.5f, 0f);
        xpBgRt.anchoredPosition = Vector2.zero;
        xpBgRt.sizeDelta = new Vector2(0f, 10f);

        GameObject xpBarFillObj = new GameObject("XPBar_Fill");
        xpBarFillObj.transform.SetParent(xpBarBg.transform, false);
        xpBarFill = xpBarFillObj.AddComponent<Image>();
        xpBarFill.color = new Color(0.2f, 0.6f, 1f);
        RectTransform xpFillRt = xpBarFillObj.GetComponent<RectTransform>();
        xpFillRt.anchorMin = new Vector2(0f, 0f);
        xpFillRt.anchorMax = new Vector2(0f, 1f); // Width controlled by code
        xpFillRt.pivot = new Vector2(0f, 0.5f);
        xpFillRt.anchoredPosition = Vector2.zero;
        xpFillRt.sizeDelta = Vector2.zero;

        // --- Level Text ---
        GameObject levelObj = new GameObject("LevelText_TopBar");
        levelObj.transform.SetParent(topBar.transform, false);
        levelText = levelObj.AddComponent<TextMeshProUGUI>();
        levelText.text = "LVL 1";
        levelText.fontSize = 20;
        levelText.fontStyle = FontStyles.Bold;
        levelText.color = Color.white;
        levelText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        RectTransform levelRt = levelObj.GetComponent<RectTransform>();
        levelRt.anchorMin = new Vector2(0f, 0.5f);
        levelRt.anchorMax = new Vector2(0f, 0.5f);
        levelRt.anchoredPosition = new Vector2(400f, 0f);
        levelRt.sizeDelta = new Vector2(100f, 50f);

        // --- Screen Title (center) ---
        GameObject titleObj = new GameObject("ScreenTitle_TopBar");
        titleObj.transform.SetParent(topBar.transform, false);
        screenTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        screenTitleText.text = "TARLA";
        screenTitleText.fontSize = 24;
        screenTitleText.fontStyle = FontStyles.Bold;
        screenTitleText.color = new Color(0.9f, 0.95f, 1f);
        screenTitleText.alignment = TextAlignmentOptions.Center;

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = Vector2.zero;
        titleRt.sizeDelta = new Vector2(320f, 50f);

        // --- Inventory Button (right side, always visible) ---
        BuildTopBarButton(topBar.transform, "Envanter", new Color(0.2f, 0.4f, 0.85f),
            new Vector2(-170f, 0f), () => {
                if (InventoryUI.Instance != null) InventoryUI.Instance.Show(null);
            });

        // --- Progression Hub Button (streak) ---
        BuildTopBarButton(topBar.transform, "Ilerleme", new Color(0.28f, 0.56f, 0.32f),
            new Vector2(-320f, 0f), () => {
                if (ProgressionHubUI.Instance != null) ProgressionHubUI.Instance.Toggle();
            }, 0f, 150f);

        // --- Hamburger Menu Button (far right, always visible) ---
        BuildTopBarButton(topBar.transform, "|||", new Color(0.32f, 0.36f, 0.42f),
            new Vector2(-20f, 0f), () => {
                if (MetaMenuUI.Instance != null) MetaMenuUI.Instance.Toggle();
            }, 90f);
    }

    private void BuildTopBarButton(Transform parent, string label, Color bgColor, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick, float labelRotationZ = 0f, float width = 130f)
    {
        GameObject btnObj = new GameObject(label + "Button_TopBar");
        btnObj.transform.SetParent(parent, false);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = bgColor;

        // Rounded corners effect via slight transparency at edges (simple approach)
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        // Set up color transition for hover/press
        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.2f;
        cb.pressedColor = bgColor * 0.7f;
        cb.selectedColor = bgColor;
        btn.colors = cb;

        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0.5f);
        btnRt.anchorMax = new Vector2(1f, 0.5f);
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.anchoredPosition = anchoredPos;
        btnRt.sizeDelta = new Vector2(width, 50f);

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 20;
        labelTmp.color = Color.white;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;

        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        labelRt.localRotation = Quaternion.Euler(0f, 0f, labelRotationZ);

        btn.onClick.AddListener(onClick);
    }

    // =============================================
    //  CLICKER BUTTON (Bottom Center)
    // =============================================

    private void BuildClickerButton(Transform parent)
    {
        // Remove the old ClickButton if it exists in the scene
        Transform oldBtn = FindByNameRecursive(parent, "ClickButton");
        if (oldBtn != null)
        {
            Object.Destroy(oldBtn.gameObject);
        }

        // --- Round yellow button ---
        GameObject btnObj = new GameObject("ClickButton");
        btnObj.transform.SetParent(parent, false);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(1f, 0.82f, 0.1f, 1f);
        // Make it look round by using a generated circle sprite 
        btnImg.sprite = CreateCircleSprite();
        btnImg.type = Image.Type.Simple;
        btnImg.preserveAspect = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        // Color transitions for feedback
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(1f, 0.82f, 0.1f);
        cb.highlightedColor = new Color(1f, 0.9f, 0.3f);
        cb.pressedColor = new Color(0.85f, 0.7f, 0.05f);
        cb.disabledColor = new Color(0.5f, 0.45f, 0.2f);
        cb.selectedColor = new Color(1f, 0.82f, 0.1f);
        btn.colors = cb;

        // Position: bottom-center
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 120f);
        btnRt.sizeDelta = new Vector2(120f, 120f);

        // Button text (energy count)
        GameObject labelObj = new GameObject("Text");
        labelObj.transform.SetParent(btnObj.transform, false);
        clickButtonText = labelObj.AddComponent<TextMeshProUGUI>();
        clickButtonText.text = "Tikla";
        clickButtonText.fontSize = 22;
        clickButtonText.fontStyle = FontStyles.Bold;
        clickButtonText.color = new Color(0.15f, 0.1f, 0f);
        clickButtonText.alignment = TextAlignmentOptions.Center;

        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        // Wire up click event to ClickerManager
        ClickerManager clicker = GetComponent<ClickerManager>();
        if (clicker == null) clicker = GetComponentInParent<ClickerManager>();
        if (clicker == null) clicker = FindAnyObjectByType<ClickerManager>();

        if (clicker != null)
        {
            btn.onClick.AddListener(clicker.OnClickCoinButton);
        }

        clickButtonRef = btn;

        // Subscribe to energy changes for button text
        ClickerManager.OnEnergyChanged += UpdateClickButtonEnergy;
    }

    private static Transform FindByNameRecursive(Transform root, string nameToFind)
    {
        if (root == null || string.IsNullOrWhiteSpace(nameToFind))
            return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == nameToFind)
                return all[i];
        }

        return null;
    }

    private void UpdateClickButtonEnergy(int current, int max)
    {
        if (clickButtonText == null) return;

        if (current >= max)
        {
            clickButtonText.text = "MAX";
        }
        else
        {
            clickButtonText.text = current.ToString();
        }

        // Disable button when no energy
        if (clickButtonRef != null)
        {
            clickButtonRef.interactable = current > 0;
        }
    }

    // =============================================
    //  ICON HELPERS
    // =============================================

    /// <summary>
    /// Create a circle icon (colored circle) as a UI Image.
    /// </summary>
    private GameObject CreateCircleIcon(Transform parent, Color color, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.sprite = CreateCircleSprite();
        img.type = Image.Type.Simple;
        img.preserveAspect = true;

        return obj;
    }

    /// <summary>
    /// Create a diamond-shaped icon as a rotated square.
    /// </summary>
    private GameObject CreateDiamondIcon(Transform parent, Color color, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.sprite = CreateCircleSprite();
        img.type = Image.Type.Simple;
        img.preserveAspect = true;

        // Rotate 45 degrees for diamond effect
        obj.transform.localRotation = Quaternion.Euler(0, 0, 45f);

        return obj;
    }

    /// <summary>
    /// Add a centered text label on top of an icon.
    /// </summary>
    private void AddIconLabel(Transform parent, string text, Color color)
    {
        GameObject labelObj = new GameObject("IconLabel");
        labelObj.transform.SetParent(parent, false);

        // Counter-rotate text if parent is rotated (diamond icon)
        labelObj.transform.localRotation = Quaternion.Inverse(parent.localRotation);

        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Generate a simple circle sprite at runtime (32x32 white circle).
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = center - 1f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    // Slight anti-aliasing at edges
                    float alpha = Mathf.Clamp01((radius - dist) * 2f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // =============================================
    //  CURRENCY UPDATES
    // =============================================

    private void UpdateCoinText(int coinValue)
    {
        if (coinText != null)
        {
            coinText.text = coinValue.ToString();
        }
    }

    private void UpdateGemText(int gemValue)
    {
        if (gemText != null)
        {
            gemText.text = gemValue.ToString();
        }
    }

    public void SetScreenTitle(string title)
    {
        if (screenTitleText == null) return;
        screenTitleText.text = string.IsNullOrWhiteSpace(title) ? "TARLA" : title.ToUpperInvariant();
    }

    public void SetClickerVisible(bool isVisible)
    {
        if (clickButtonRef == null) return;
        clickButtonRef.gameObject.SetActive(isVisible);
    }

    private void UpdateLevelUI(int level, float currentXP, float maxXP)
    {
        if (levelText != null) levelText.text = "LVL " + level;
        if (xpBarFill != null)
        {
            float ratio = Mathf.Clamp01(currentXP / maxXP);
            xpBarFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        // Simple Level Up feedback
        Debug.Log("UIManager: Level Up Animation Triggered!");
    }

    // =============================================
    //  PASSIVE INCOME FEEDBACK
    // =============================================

    private void HandleIncomeCollected(int amount)
    {
        if (amount <= 0) return;
        ShowIncomePopup(amount);
    }

    private void ShowIncomePopup(int amount)
    {
        // 1. Create the popup text
        GameObject popupObj = new GameObject("IncomePopup");
        popupObj.transform.SetParent(topBar.transform, false);

        TextMeshProUGUI tmp = popupObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "+" + amount;
        tmp.fontSize = 24;
        tmp.color = new Color(1f, 1f, 0.4f, 1f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform rt = popupObj.GetComponent<RectTransform>();
        // Position it right next to the coin counter
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(140f, 0f); // Slightly to the right of the coin text
        rt.sizeDelta = new Vector2(100f, 50f);

        // 2. Animate and Destroy
        StartCoroutine(AnimatePopup(popupObj, rt, tmp));
        
        // 3. Pulse the coin counter
        StartCoroutine(PulseCoinCounter());
    }

    private System.Collections.IEnumerator AnimatePopup(GameObject obj, RectTransform rt, TextMeshProUGUI tmp)
    {
        float duration = 1.2f;
        float elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, 40f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            tmp.alpha = 1f - t; // Fade out

            yield return null;
        }

        Destroy(obj);
    }

    private System.Collections.IEnumerator PulseCoinCounter()
    {
        if (coinText == null) yield break;

        float duration = 0.2f;
        Vector3 startScale = Vector3.one;
        Vector3 peakScale = new Vector3(1.2f, 1.2f, 1f);

        // Scale up
        float elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            coinText.transform.localScale = Vector3.Lerp(startScale, peakScale, elapsed / (duration * 0.5f));
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            coinText.transform.localScale = Vector3.Lerp(peakScale, startScale, elapsed / (duration * 0.5f));
            yield return null;
        }

        coinText.transform.localScale = startScale;
    }

    // =============================================
    //  HELPERS
    // =============================================

    /// <summary>
    /// Ensures a UI component exists as a child of the Canvas.
    /// </summary>
    private T EnsureUIComponent<T>(string objectName) where T : MonoBehaviour
    {
        T existing = FindAnyObjectByType<T>();
        if (existing == null)
        {
            GameObject obj = new GameObject(objectName);
            obj.transform.SetParent(this.transform, false);
            existing = obj.AddComponent<T>();
        }
        return existing;
    }
}
