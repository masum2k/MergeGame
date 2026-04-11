using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Zero-Touch Market UI. Creates:
/// 1. A Market panel (fullscreen overlay) with a "Sandik Ac" button
/// 2. A drop notification that briefly shows what was obtained
/// 
/// The "Markete Git" and "Envantere Git" buttons are now handled by UIManager's top bar.
/// </summary>
public class MarketUI : MonoBehaviour
{
    private GameObject marketPanel;
    private TextMeshProUGUI dropNotificationText;
    private TextMeshProUGUI crateInfoText;
    private Button openCrateButton;
    private float notificationTimer = 0f;

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }
        if (canvas == null) return;

        // Market and Inventory buttons are now in UIManager top bar.
        // Only build the market panel overlay here.
        BuildMarketPanel(canvas.transform);

        // Subscribe to crate events
        if (CrateManager.Instance != null)
        {
            CrateManager.Instance.OnCrateOpened += OnCrateOpened;
        }
    }

    private void OnDestroy()
    {
        if (CrateManager.Instance != null)
        {
            CrateManager.Instance.OnCrateOpened -= OnCrateOpened;
        }
    }

    private void Update()
    {
        // Handle notification fade-out
        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f && dropNotificationText != null)
            {
                dropNotificationText.text = "";
            }
        }
    }

    // =============================================
    //  BUILD: Market Panel (Overlay)
    // =============================================
    private void BuildMarketPanel(Transform parent)
    {
        // Panel background (fullscreen overlay)
        marketPanel = new GameObject("MarketPanel_Auto");
        marketPanel.transform.SetParent(parent, false);

        Image panelBg = marketPanel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Dark semi-transparent

        RectTransform panelRt = marketPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // ---- Title ----
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(marketPanel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "MARKET";
        titleText.fontSize = 42;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);
        titleRt.sizeDelta = new Vector2(400f, 60f);

        // ---- Crate Info ----
        GameObject crateInfoObj = new GameObject("CrateInfo");
        crateInfoObj.transform.SetParent(marketPanel.transform, false);
        crateInfoText = crateInfoObj.AddComponent<TextMeshProUGUI>();
        crateInfoText.text = "Bronz Sandik\nFiyat: 10 Coin";
        crateInfoText.fontSize = 28;
        crateInfoText.color = new Color(1f, 0.84f, 0f); // Gold text
        crateInfoText.alignment = TextAlignmentOptions.Center;

        RectTransform crateRt = crateInfoObj.GetComponent<RectTransform>();
        crateRt.anchorMin = new Vector2(0.5f, 0.5f);
        crateRt.anchorMax = new Vector2(0.5f, 0.5f);
        crateRt.pivot = new Vector2(0.5f, 0.5f);
        crateRt.anchoredPosition = new Vector2(0f, 80f);
        crateRt.sizeDelta = new Vector2(400f, 80f);

        // ---- Open Crate Button ----
        GameObject crateBtnObj = new GameObject("OpenCrateButton");
        crateBtnObj.transform.SetParent(marketPanel.transform, false);

        Image crateBtnImg = crateBtnObj.AddComponent<Image>();
        crateBtnImg.color = new Color(0.85f, 0.55f, 0.1f, 1f); // Bronze/Orange

        openCrateButton = crateBtnObj.AddComponent<Button>();
        openCrateButton.targetGraphic = crateBtnImg;

        RectTransform crateBtnRt = crateBtnObj.GetComponent<RectTransform>();
        crateBtnRt.anchorMin = new Vector2(0.5f, 0.5f);
        crateBtnRt.anchorMax = new Vector2(0.5f, 0.5f);
        crateBtnRt.pivot = new Vector2(0.5f, 0.5f);
        crateBtnRt.anchoredPosition = new Vector2(0f, -10f);
        crateBtnRt.sizeDelta = new Vector2(260f, 70f);

        // Button label
        GameObject crateLabelObj = new GameObject("Label");
        crateLabelObj.transform.SetParent(crateBtnObj.transform, false);
        TextMeshProUGUI crateLabel = crateLabelObj.AddComponent<TextMeshProUGUI>();
        crateLabel.text = "Sandik Ac";
        crateLabel.fontSize = 28;
        crateLabel.color = Color.white;
        crateLabel.alignment = TextAlignmentOptions.Center;

        RectTransform crateLabelRt = crateLabelObj.GetComponent<RectTransform>();
        crateLabelRt.anchorMin = Vector2.zero;
        crateLabelRt.anchorMax = Vector2.one;
        crateLabelRt.offsetMin = Vector2.zero;
        crateLabelRt.offsetMax = Vector2.zero;

        openCrateButton.onClick.AddListener(OnOpenCrateClicked);

        // ---- Drop Notification ----
        GameObject notifObj = new GameObject("DropNotification");
        notifObj.transform.SetParent(marketPanel.transform, false);
        dropNotificationText = notifObj.AddComponent<TextMeshProUGUI>();
        dropNotificationText.text = "";
        dropNotificationText.fontSize = 32;
        dropNotificationText.color = Color.green;
        dropNotificationText.alignment = TextAlignmentOptions.Center;

        RectTransform notifRt = notifObj.GetComponent<RectTransform>();
        notifRt.anchorMin = new Vector2(0.5f, 0.5f);
        notifRt.anchorMax = new Vector2(0.5f, 0.5f);
        notifRt.pivot = new Vector2(0.5f, 0.5f);
        notifRt.anchoredPosition = new Vector2(0f, -100f);
        notifRt.sizeDelta = new Vector2(500f, 60f);

        // ---- Close Button ----
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(marketPanel.transform, false);

        Image closeBtnImg = closeBtnObj.AddComponent<Image>();
        closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Red

        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;

        RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(0.5f, 0f);
        closeBtnRt.anchorMax = new Vector2(0.5f, 0f);
        closeBtnRt.pivot = new Vector2(0.5f, 0f);
        closeBtnRt.anchoredPosition = new Vector2(0f, 40f);
        closeBtnRt.sizeDelta = new Vector2(200f, 50f);

        // Close label
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

        closeBtn.onClick.AddListener(CloseMarket);

        // Start hidden
        marketPanel.SetActive(false);
    }

    // =============================================
    //  ACTIONS
    // =============================================

    public void OpenMarket()
    {
        if (marketPanel != null)
        {
            // Update crate info text
            if (CrateManager.Instance != null && CrateManager.Instance.currentCrate != null)
            {
                var crate = CrateManager.Instance.currentCrate;
                crateInfoText.text = $"{crate.crateName}\nFiyat: {crate.cost} Coin";
            }

            marketPanel.SetActive(true);
        }
    }

    public void CloseMarket()
    {
        if (marketPanel != null)
        {
            marketPanel.SetActive(false);
            // Clear notification
            if (dropNotificationText != null)
                dropNotificationText.text = "";
        }
    }

    private void OnOpenCrateClicked()
    {
        if (CrateManager.Instance != null)
        {
            CropData result = CrateManager.Instance.OpenCrate();
            if (result == null)
            {
                // Show error
                if (dropNotificationText != null)
                {
                    dropNotificationText.color = new Color(1f, 0.3f, 0.3f);
                    dropNotificationText.text = "Yetersiz Bakiye!";
                    notificationTimer = 2f;
                }
            }
        }
    }

    private void OnCrateOpened(CropData droppedCrop)
    {
        if (dropNotificationText != null && droppedCrop != null)
        {
            // Color the notification text with the crop's color
            Color textColor = droppedCrop.cropColor;
            // Ensure it's visible on dark background (lighten if too dark)
            textColor = Color.Lerp(textColor, Color.white, 0.3f);
            dropNotificationText.color = textColor;

            string tierText = droppedCrop.tier.ToString();
            dropNotificationText.text = $"[YENI] {droppedCrop.cropName} dustu! [{tierText}]";
            notificationTimer = 3f;
        }
    }
}
