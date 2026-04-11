using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Modern Grid-based Inventory UI with Tabs and Filtering.
/// All generated at runtime (Zero-Touch).
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    private enum Tab { Besin, Boost, Item }
    private enum SortMode { Tier, Count, Name }

    [Header("State")]
    private Tab _currentTab = Tab.Besin;
    private SortMode _currentSort = SortMode.Tier;

    [Header("UI Elements")]
    private GameObject inventoryPanel;
    private Transform itemContainer;
    private GridSlot targetSlot;
    private TextMeshProUGUI filterBtnText;

    private List<GameObject> itemCards = new List<GameObject>();
    private Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        BuildInventoryPanel(canvas.transform);
    }

    // =============================================
    //  BUILD: Inventory Panel
    // =============================================
    private void BuildInventoryPanel(Transform parent)
    {
        // 1. Overlay Background (Full screen dimming)
        inventoryPanel = new GameObject("InventoryPanel_Auto", typeof(RectTransform));
        inventoryPanel.transform.SetParent(parent, false);
        Image overlayImg = inventoryPanel.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.7f); // Dark dimming overlay
        RectTransform panelRt = inventoryPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;

        // 2. The Pop-up Window
        GameObject window = new GameObject("Window", typeof(RectTransform));
        window.transform.SetParent(inventoryPanel.transform, false);
        Image windowBg = window.AddComponent<Image>();
        windowBg.color = new Color(0.06f, 0.08f, 0.15f, 1f);
        RectTransform winRt = window.GetComponent<RectTransform>();
        winRt.anchorMin = new Vector2(0.1f, 0.15f); winRt.anchorMax = new Vector2(0.9f, 0.85f);
        winRt.offsetMin = Vector2.zero; winRt.offsetMax = Vector2.zero;

        // 3. Tab Bar (Inside Window)
        GameObject tabBar = new GameObject("TabBar", typeof(RectTransform));
        tabBar.transform.SetParent(window.transform, false);
        RectTransform tabRt = tabBar.GetComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0f, 1f); tabRt.anchorMax = new Vector2(1f, 1f);
        tabRt.pivot = new Vector2(0.5f, 1f);
        tabRt.anchoredPosition = new Vector2(0f, -10f);
        tabRt.sizeDelta = new Vector2(-40f, 50f);

        HorizontalLayoutGroup hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f; hlg.childForceExpandWidth = true; hlg.childControlWidth = true;

        tabButtons ??= new Dictionary<Tab, Button>();
        foreach (Tab t in System.Enum.GetValues(typeof(Tab)))
        {
            Tab capturedTab = t;
            tabButtons[t] = CreateTabButton(tabBar.transform, t.ToString(), () => SetTab(capturedTab));
        }

        // 4. Filter/Sort Button
        GameObject filterObj = new GameObject("FilterButton", typeof(RectTransform));
        filterObj.transform.SetParent(window.transform, false);
        Button fBtn = filterObj.AddComponent<Button>();
        Image fImg = filterObj.AddComponent<Image>();
        fImg.color = new Color(0.2f, 0.3f, 0.5f, 0.8f);
        RectTransform fRt = filterObj.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(1f, 1f); fRt.anchorMax = new Vector2(1f, 1f);
        fRt.pivot = new Vector2(1f, 1f);
        fRt.anchoredPosition = new Vector2(-15f, -70f);
        fRt.sizeDelta = new Vector2(120f, 35f);

        GameObject fTextObj = new GameObject("Text", typeof(RectTransform));
        fTextObj.transform.SetParent(filterObj.transform, false);
        filterBtnText = fTextObj.AddComponent<TextMeshProUGUI>();
        filterBtnText.fontSize = 14;
        filterBtnText.alignment = TextAlignmentOptions.Center;
        filterBtnText.text = "Sırala: " + _currentSort.ToString();
        RectTransform ftRt = fTextObj.GetComponent<RectTransform>();
        ftRt.anchorMin = Vector2.zero; ftRt.anchorMax = Vector2.one; 
        ftRt.offsetMin = Vector2.zero; ftRt.offsetMax = Vector2.zero;

        fBtn.onClick.AddListener(CycleSortMode);

        // 5. Close Button
        GameObject closeObj = new GameObject("CloseX", typeof(RectTransform));
        closeObj.transform.SetParent(window.transform, false);
        Button cBtn = closeObj.AddComponent<Button>();
        Image cImg = closeObj.AddComponent<Image>();
        cImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        RectTransform cRt = closeObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f); cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.anchoredPosition = new Vector2(15f, 15f);
        cRt.sizeDelta = new Vector2(35f, 35f);

        GameObject cTextObj = new GameObject("Text", typeof(RectTransform));
        cTextObj.transform.SetParent(closeObj.transform, false);
        TextMeshProUGUI cText = cTextObj.AddComponent<TextMeshProUGUI>();
        cText.text = "X"; cText.fontSize = 20; cText.alignment = TextAlignmentOptions.Center;
        RectTransform ctRt = cTextObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero; ctRt.anchorMax = Vector2.one; 
        ctRt.offsetMin = Vector2.zero; ctRt.offsetMax = Vector2.zero;

        cBtn.onClick.AddListener(Hide);

        // 6. Scrollable Grid Area
        GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform));
        scrollObj.transform.SetParent(window.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.02f, 0.02f); scrollRt.anchorMax = new Vector2(0.98f, 0.8f);
        scrollRt.offsetMin = Vector2.zero; scrollRt.offsetMax = Vector2.zero;

        ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true;
        
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        viewportObj.AddComponent<RectMask2D>();
        RectTransform viewRt = viewportObj.GetComponent<RectTransform>();
        viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one;
        viewRt.offsetMin = Vector2.zero; viewRt.offsetMax = Vector2.zero;
        sr.viewport = viewRt;

        GameObject gridObj = new GameObject("Grid", typeof(RectTransform));
        gridObj.transform.SetParent(viewportObj.transform, false);
        RectTransform gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0f, 1f); gridRt.anchorMax = new Vector2(1f, 1f);
        gridRt.pivot = new Vector2(0.5f, 1f);
        gridRt.anchoredPosition = Vector2.zero;
        gridRt.sizeDelta = new Vector2(0f, 0f);
        sr.content = gridRt;

        GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(120f, 120f);
        glg.spacing = new Vector2(10f, 10f);
        glg.padding = new RectOffset(10, 10, 10, 10);
        glg.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter csf = gridObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        itemContainer = gridObj.transform;
        itemCards ??= new List<GameObject>();

        inventoryPanel.SetActive(false);
        UpdateTabVisuals();
    }

    private Button CreateTabButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(label + "_Tab", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.3f, 0.5f, 1f);
        Button btn = obj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(obj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = 20; txt.alignment = TextAlignmentOptions.Center;
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        return btn;
    }

    // =============================================
    //  LOGIC
    // =============================================

    public void Show(GridSlot slot)
    {
        if (inventoryPanel == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null) BuildInventoryPanel(canvas.transform);
        }

        if (inventoryPanel == null) return;

        targetSlot = slot;
        PopulateItems();
        inventoryPanel.SetActive(true);
    }

    public void Hide()
    {
        targetSlot = null;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    private void SetTab(Tab tab)
    {
        _currentTab = tab;
        UpdateTabVisuals();
        PopulateItems();
    }

    private void UpdateTabVisuals()
    {
        foreach (var kvp in tabButtons)
        {
            if (kvp.Value == null || kvp.Value.targetGraphic == null) continue;
            kvp.Value.targetGraphic.color = (kvp.Key == _currentTab) 
                ? new Color(0.3f, 0.6f, 1f, 1f) 
                : new Color(0.15f, 0.2f, 0.3f, 1f);
        }
    }

    private void CycleSortMode()
    {
        _currentSort = (SortMode)(((int)_currentSort + 1) % System.Enum.GetValues(typeof(SortMode)).Length);
        if (filterBtnText != null) filterBtnText.text = "Sırala: " + _currentSort.ToString();
        PopulateItems();
    }

    private void ClearItems()
    {
        if (itemCards == null) itemCards = new List<GameObject>();
        for (int i = itemCards.Count - 1; i >= 0; i--)
        {
            if (itemCards[i] != null)
            {
                if (Application.isPlaying) Destroy(itemCards[i]);
                else DestroyImmediate(itemCards[i]);
            }
        }
        itemCards.Clear();
        
        // Final safety cleanup of any stray children in container
        if (itemContainer != null)
        {
            for (int i = itemContainer.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(itemContainer.GetChild(i).gameObject);
                else DestroyImmediate(itemContainer.GetChild(i).gameObject);
            }
        }
    }

    private void PopulateItems()
    {
        ClearItems();

        if (InventoryManager.Instance == null) return;

        // Filter and Cast based on Tab
        if (_currentTab == Tab.Besin)
        {
            List<CropData> crops = InventoryManager.Instance.GetItemsOfType<CropData>();
            
            // Sorting
            switch (_currentSort)
            {
                case SortMode.Tier:
                    crops = crops.OrderByDescending(c => c.tier).ThenBy(c => c.itemName).ToList();
                    break;
                case SortMode.Count:
                    crops = crops.OrderByDescending(c => InventoryManager.Instance.GetCount(c.itemName)).ThenBy(c => c.itemName).ToList();
                    break;
                case SortMode.Name:
                    crops = crops.OrderBy(c => c.itemName).ToList();
                    break;
            }

            foreach (var crop in crops)
            {
                CreateItemCard(crop);
            }
        }
        else if (_currentTab == Tab.Boost)
        {
            List<BoostData> boosts = InventoryManager.Instance.GetItemsOfType<BoostData>();
            
            // Sorting for boosts (mostly by name or count)
            switch (_currentSort)
            {
                case SortMode.Count:
                    boosts = boosts.OrderByDescending(b => InventoryManager.Instance.GetCount(b.itemName)).ThenBy(b => b.itemName).ToList();
                    break;
                default:
                    boosts = boosts.OrderBy(b => b.itemName).ToList();
                    break;
            }

            foreach (var boost in boosts)
            {
                CreateItemCard(boost);
            }
        }
    }

    private void CreateItemCard(BaseItemData item)
    {
        GameObject card = new GameObject("ItemCard_" + item.itemName, typeof(RectTransform));
        card.transform.SetParent(itemContainer, false);
        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);

        // Icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
        iconObj.transform.SetParent(card.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = item.icon;
        iconImg.color = item.itemColor;
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f); iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(80f, 80f);

        // Tier (Only for Crops)
        if (item is CropData cropData)
        {
            GameObject tierObj = new GameObject("Tier", typeof(RectTransform));
            tierObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI tierTxt = tierObj.AddComponent<TextMeshProUGUI>();
            tierTxt.text = "T" + ((int)cropData.tier + 1);
            tierTxt.fontSize = 20; tierTxt.fontStyle = FontStyles.Bold;
            tierTxt.color = new Color(1f, 1f, 1f, 0.5f);
            tierTxt.alignment = TextAlignmentOptions.TopRight;
            RectTransform tierRt = tierObj.GetComponent<RectTransform>();
            tierRt.anchorMin = Vector2.zero; tierRt.anchorMax = Vector2.one;
            tierRt.offsetMin = new Vector2(5, 5); tierRt.offsetMax = new Vector2(-5, -5);
        }

        // Count
        GameObject countObj = new GameObject("Count", typeof(RectTransform));
        countObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI countTxt = countObj.AddComponent<TextMeshProUGUI>();
        countTxt.text = "x" + InventoryManager.Instance.GetCount(item.itemName);
        countTxt.fontSize = 18; countTxt.fontStyle = FontStyles.Bold;
        countTxt.alignment = TextAlignmentOptions.BottomRight;
        RectTransform countRt = countObj.GetComponent<RectTransform>();
        countRt.anchorMin = Vector2.zero; countRt.anchorMax = Vector2.one;
        countRt.offsetMin = new Vector2(5, 5); countRt.offsetMax = new Vector2(-5, -5);

        // Name
        GameObject nameObj = new GameObject("Name", typeof(RectTransform));
        nameObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
        nameTxt.text = item.itemName;
        nameTxt.fontSize = 14; nameTxt.alignment = TextAlignmentOptions.Bottom;
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0); nameRt.anchorMax = new Vector2(1, 0);
        nameRt.pivot = new Vector2(0.5f, 0); nameRt.anchoredPosition = new Vector2(0, 5);
        nameRt.sizeDelta = new Vector2(0, 25);

        // Interaction
        Button btn = card.AddComponent<Button>();
        btn.onClick.AddListener(() => OnItemSelected(item));

        itemCards.Add(card);
    }

    private void OnItemSelected(BaseItemData item)
    {
        if (item is CropData crop)
        {
            if (targetSlot == null || !targetSlot.IsEmpty) { Hide(); return; }
            if (InventoryManager.Instance.RemoveItem(crop))
            {
                targetSlot.SetCrop(crop);
            }
        }
        else if (item is BoostData boost)
        {
            if (InventoryManager.Instance.RemoveItem(boost))
            {
                BoostManager.Instance.ActivateBoost(boost);
            }
        }
        
        Hide();
    }
}
