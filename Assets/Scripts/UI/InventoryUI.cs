using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Zero-Touch Inventory UI. Shows a popup when the player clicks on an empty grid slot.
/// Displays all owned crops as buttons. Clicking one places it on the target slot.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    /// <summary>
    /// Singleton-like static reference so GridSlot can call InventoryUI.Show(slot)
    /// </summary>
    public static InventoryUI Instance { get; private set; }

    private GameObject inventoryPanel;
    private Transform itemContainer;
    private TextMeshProUGUI titleText;
    private GridSlot targetSlot;

    // Keep references to dynamically created buttons so we can clean them up
    private List<GameObject> itemButtons = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }
        if (canvas == null) return;

        BuildInventoryPanel(canvas.transform);
    }

    // =============================================
    //  BUILD: Inventory Panel
    // =============================================
    private void BuildInventoryPanel(Transform parent)
    {
        // Panel background (fullscreen overlay)
        inventoryPanel = new GameObject("InventoryPanel_Auto");
        inventoryPanel.transform.SetParent(parent, false);

        Image panelBg = inventoryPanel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.1f, 0.2f, 0.95f); // Dark blue

        RectTransform panelRt = inventoryPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // ---- Title ----
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(inventoryPanel.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Envanter — Besin Seç";
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -30f);
        titleRt.sizeDelta = new Vector2(500f, 60f);

        // ---- Item Container (vertical list area) ----
        GameObject containerObj = new GameObject("ItemContainer");
        containerObj.transform.SetParent(inventoryPanel.transform, false);

        RectTransform containerRt = containerObj.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.1f, 0.15f);
        containerRt.anchorMax = new Vector2(0.9f, 0.85f);
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;

        // Add a VerticalLayoutGroup for clean layout
        VerticalLayoutGroup vlg = containerObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Add ContentSizeFitter to allow scrolling if needed
        ContentSizeFitter csf = containerObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        itemContainer = containerObj.transform;

        // ---- Empty Inventory Message (hidden by default, shown when empty) ----
        // Will be managed in PopulateItems()

        // ---- Close Button ----
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(inventoryPanel.transform, false);

        Image closeBtnImg = closeBtnObj.AddComponent<Image>();
        closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;

        RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(0.5f, 0f);
        closeBtnRt.anchorMax = new Vector2(0.5f, 0f);
        closeBtnRt.pivot = new Vector2(0.5f, 0f);
        closeBtnRt.anchoredPosition = new Vector2(0f, 20f);
        closeBtnRt.sizeDelta = new Vector2(200f, 50f);

        GameObject closeLabelObj = new GameObject("Label");
        closeLabelObj.transform.SetParent(closeBtnObj.transform, false);
        TextMeshProUGUI closeLabel = closeLabelObj.AddComponent<TextMeshProUGUI>();
        closeLabel.text = "X Kapat";
        closeLabel.fontSize = 24;
        closeLabel.color = Color.white;
        closeLabel.alignment = TextAlignmentOptions.Center;

        RectTransform closeLabelRt = closeLabelObj.GetComponent<RectTransform>();
        closeLabelRt.anchorMin = Vector2.zero;
        closeLabelRt.anchorMax = Vector2.one;
        closeLabelRt.offsetMin = Vector2.zero;
        closeLabelRt.offsetMax = Vector2.zero;

        closeBtn.onClick.AddListener(Hide);

        // Start hidden
        inventoryPanel.SetActive(false);
    }

    // =============================================
    //  PUBLIC API
    // =============================================

    /// <summary>
    /// Shows the inventory panel, targeting a specific grid slot for placement.
    /// </summary>
    /// <summary>
    /// Shows the inventory panel.
    /// If slot is provided, clicking an item places it on that slot.
    /// If slot is null, opens in view-only mode (just browsing).
    /// </summary>
    public void Show(GridSlot slot)
    {
        targetSlot = slot;

        // Update title based on mode
        if (titleText != null)
        {
            titleText.text = slot != null 
                ? "Envanter — Besin Seç" 
                : "Envanter";
        }

        PopulateItems();

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the inventory panel.
    /// </summary>
    public void Hide()
    {
        targetSlot = null;
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    // =============================================
    //  ITEM POPULATION
    // =============================================

    private void PopulateItems()
    {
        // Clear previous buttons
        foreach (var btn in itemButtons)
        {
            Destroy(btn);
        }
        itemButtons.Clear();

        if (InventoryManager.Instance == null) return;

        List<CropData> ownedCrops = InventoryManager.Instance.GetAllOwnedCrops();

        if (ownedCrops.Count == 0)
        {
            // Show "empty" message
            CreateItemRow("Envanterin boş! Marketten sandık aç.", Color.gray, null);
            return;
        }

        foreach (var crop in ownedCrops)
        {
            int count = InventoryManager.Instance.GetCount(crop.cropName);
            string tierTag = crop.tier.ToString();
            string label = $"{crop.cropName} [{tierTag}] x{count}";

            // Use the crop's color for the text
            Color textColor = Color.Lerp(crop.cropColor, Color.white, 0.3f);

            CreateItemRow(label, textColor, crop);
        }
    }

    private void CreateItemRow(string label, Color textColor, CropData cropToPlace)
    {
        GameObject rowObj = new GameObject("InventoryItem");
        rowObj.transform.SetParent(itemContainer, false);

        // Row background
        Image rowBg = rowObj.AddComponent<Image>();
        rowBg.color = new Color(0.15f, 0.2f, 0.3f, 0.8f);

        // LayoutElement for consistent sizing
        LayoutElement le = rowObj.AddComponent<LayoutElement>();
        le.preferredHeight = 60f;
        le.minHeight = 60f;

        // Color swatch (small square showing crop color)
        if (cropToPlace != null)
        {
            GameObject swatchObj = new GameObject("ColorSwatch");
            swatchObj.transform.SetParent(rowObj.transform, false);
            Image swatch = swatchObj.AddComponent<Image>();
            swatch.color = cropToPlace.cropColor;

            RectTransform swatchRt = swatchObj.GetComponent<RectTransform>();
            swatchRt.anchorMin = new Vector2(0f, 0.5f);
            swatchRt.anchorMax = new Vector2(0f, 0.5f);
            swatchRt.pivot = new Vector2(0f, 0.5f);
            swatchRt.anchoredPosition = new Vector2(15f, 0f);
            swatchRt.sizeDelta = new Vector2(40f, 40f);
        }

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(rowObj.transform, false);
        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 24;
        labelTmp.color = textColor;
        labelTmp.alignment = TextAlignmentOptions.Left;

        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(65f, 0f); // Left padding after swatch
        labelRt.offsetMax = new Vector2(0f, 0f);

        // Make it clickable if there's a crop to place AND we have a target slot
        if (cropToPlace != null && targetSlot != null)
        {
            Button btn = rowObj.AddComponent<Button>();
            btn.targetGraphic = rowBg;

            // Capture crop reference for the lambda
            CropData capturedCrop = cropToPlace;
            btn.onClick.AddListener(() => OnItemSelected(capturedCrop));
        }

        itemButtons.Add(rowObj);
    }

    private void OnItemSelected(CropData crop)
    {
        if (targetSlot == null || crop == null) return;

        // Verify slot is still empty
        if (!targetSlot.IsEmpty)
        {
            Debug.LogWarning("Slot artık dolu!");
            Hide();
            return;
        }

        // Remove from inventory
        if (InventoryManager.Instance != null && InventoryManager.Instance.RemoveItem(crop))
        {
            // Place on slot
            targetSlot.SetCrop(crop);
            Debug.Log($"Envanter → Tarla: {crop.cropName} yerleştirildi.");
        }

        Hide();
    }
}
