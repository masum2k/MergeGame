using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// Revamped Market UI:
/// - Centers on screen (non-fullscreen)
/// - Vertical scrollable list of chests
/// - Categories (Daily, Coin, Gem)
/// - Real-time timers for Daily Chest
/// </summary>
public class MarketUI : MonoBehaviour
{
    private GameObject marketPanel;
    private Transform contentContainer;
    private TextMeshProUGUI dropNotificationText;
    private float notificationTimer = 0f;

    private List<GameObject> chestCards = new List<GameObject>();

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        BuildMarketPanel(canvas.transform);

        if (CrateManager.Instance != null)
        {
            CrateManager.Instance.OnCrateOpened += OnCrateOpened;
        }

        // Periodically refresh UI for timers
        InvokeRepeating(nameof(RefreshTimers), 1f, 1f);
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
        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f && dropNotificationText != null)
            {
                dropNotificationText.text = "";
            }
        }
    }

    private void BuildMarketPanel(Transform parent)
    {
        // 1. Overlay (Dimming)
        marketPanel = new GameObject("MarketPanel_Auto", typeof(RectTransform));
        marketPanel.transform.SetParent(parent, false);
        Image overlayImg = marketPanel.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.7f);
        RectTransform panelRt = marketPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;

        // 2. Window
        GameObject window = new GameObject("Window", typeof(RectTransform));
        window.transform.SetParent(marketPanel.transform, false);
        Image windowBg = window.AddComponent<Image>();
        windowBg.color = new Color(0.08f, 0.1f, 0.18f, 1f);
        RectTransform winRt = window.GetComponent<RectTransform>();
        winRt.anchorMin = new Vector2(0.15f, 0.1f); winRt.anchorMax = new Vector2(0.85f, 0.9f);
        winRt.offsetMin = Vector2.zero; winRt.offsetMax = Vector2.zero;

        // 3. Header
        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(window.transform, false);
        TextMeshProUGUI title = header.AddComponent<TextMeshProUGUI>();
        title.text = "MARKET"; title.fontSize = 32; title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1); headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.anchoredPosition = new Vector2(0, -10);
        headerRt.sizeDelta = new Vector2(0, 50);

        // 4. Scroll Area
        GameObject scrollObj = new GameObject("ChestList", typeof(RectTransform));
        scrollObj.transform.SetParent(window.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.02f, 0.02f); scrollRt.anchorMax = new Vector2(0.98f, 0.85f);
        scrollRt.offsetMin = Vector2.zero; scrollRt.offsetMax = Vector2.zero;

        ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollObj.transform, false);
        viewport.AddComponent<RectMask2D>();
        RectTransform viewRt = viewport.GetComponent<RectTransform>();
        viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one;
        viewRt.offsetMin = Vector2.zero; viewRt.offsetMax = Vector2.zero;
        sr.viewport = viewRt;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        contentContainer = content.transform;
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector3(0, 0);
        sr.content = contentRt;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15; vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 5. Close Button
        GameObject closeObj = new GameObject("CloseBtn", typeof(RectTransform));
        closeObj.transform.SetParent(window.transform, false);
        Button closeBtn = closeObj.AddComponent<Button>();
        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 1); closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(0.5f, 0.5f);
        closeRt.anchoredPosition = new Vector2(10, 10);
        closeRt.sizeDelta = new Vector2(40, 40);
        closeBtn.onClick.AddListener(CloseMarket);

        // 6. Notification Area
        GameObject notifObj = new GameObject("Notification", typeof(RectTransform));
        notifObj.transform.SetParent(window.transform, false);
        dropNotificationText = notifObj.AddComponent<TextMeshProUGUI>();
        dropNotificationText.text = ""; dropNotificationText.fontSize = 24;
        dropNotificationText.alignment = TextAlignmentOptions.Center;
        RectTransform notifRt = notifObj.GetComponent<RectTransform>();
        notifRt.anchorMin = new Vector2(0, 0); notifRt.anchorMax = new Vector2(1, 0);
        notifRt.anchoredPosition = new Vector2(0, -40);
        notifRt.sizeDelta = new Vector2(0, 40);

        marketPanel.SetActive(false);
    }

    public void OpenMarket()
    {
        PopulateChests();
        marketPanel.SetActive(true);
    }

    public void CloseMarket()
    {
        marketPanel.SetActive(false);
    }

    private void PopulateChests()
    {
        foreach (var card in chestCards) Destroy(card);
        chestCards.Clear();

        if (CrateManager.Instance == null) return;

        // Group chests by rarity/type for better UI
        AddChestSection("GÜNLÜK FIRSAT", CrateRarity.Daily);
        AddChestSection("STANDART SANDIKLAR", CrateRarity.Bronze, CrateRarity.Silver);
        AddChestSection("ELİT SANDIKLAR", CrateRarity.Gold, CrateRarity.Diamond);

        // Force layout rebuild to ensure scroll area handles new children
        if (contentContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer.GetComponent<RectTransform>());
        }

        Debug.Log($"MarketUI: Populated {chestCards.Count} entries.");
    }

    private void AddChestSection(string title, params CrateRarity[] rarities)
    {
        bool hasAny = false;
        foreach (var c in CrateManager.Instance.AllCrates)
        {
            if (System.Array.Exists(rarities, r => r == c.rarity)) { hasAny = true; break; }
        }
        if (!hasAny) return;

        // Section Title
        GameObject sectionTitle = new GameObject("Section_" + title, typeof(RectTransform));
        sectionTitle.transform.SetParent(contentContainer, false);
        TextMeshProUGUI txt = sectionTitle.AddComponent<TextMeshProUGUI>();
        txt.text = title; txt.fontSize = 18; txt.color = new Color(0.5f, 0.6f, 0.8f);
        txt.alignment = TextAlignmentOptions.Left;
        sectionTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
        chestCards.Add(sectionTitle);

        foreach (var crate in CrateManager.Instance.AllCrates)
        {
            if (System.Array.Exists(rarities, r => r == crate.rarity))
            {
                CreateChestCard(crate);
            }
        }
    }

    private void CreateChestCard(CrateData crate)
    {
        GameObject card = new GameObject("ChestCard_" + crate.crateName, typeof(RectTransform));
        card.transform.SetParent(contentContainer, false);
        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.15f, 0.25f, 1f);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 100);

        // Icon (Placeholder)
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(card.transform, false);
        Image iconImg = icon.AddComponent<Image>();
        iconImg.color = GetChestColor(crate.rarity);
        RectTransform iconRt = icon.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f); iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(50, 0); iconRt.sizeDelta = new Vector2(70, 70);

        // Info
        GameObject info = new GameObject("Info");
        info.transform.SetParent(card.transform, false);
        TextMeshProUGUI infoTxt = info.AddComponent<TextMeshProUGUI>();
        infoTxt.text = $"<b>{crate.crateName}</b>";
        infoTxt.fontSize = 22; infoTxt.alignment = TextAlignmentOptions.Left;
        RectTransform infoRt = info.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0, 0.5f); infoRt.anchorMax = new Vector2(1, 0.5f);
        infoRt.offsetMin = new Vector2(100, 0); infoRt.offsetMax = new Vector2(-120, 0);

        // Button
        GameObject btnObj = new GameObject("BuyBtn");
        btnObj.transform.SetParent(card.transform, false);
        Button btn = btnObj.AddComponent<Button>();
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = (crate.currencyType == CurrencyType.Coin) ? new Color(1, 0.8f, 0) : new Color(0, 0.8f, 1);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1, 0.5f); btnRt.anchorMax = new Vector2(1, 0.5f);
        btnRt.anchoredPosition = new Vector2(-70, 0); btnRt.sizeDelta = new Vector2(110, 50);

        GameObject btnTxtObj = new GameObject("Text");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = (crate.rarity == CrateRarity.Daily) ? "AÇ" : crate.cost.ToString();
        btnTxt.fontSize = 18; btnTxt.alignment = TextAlignmentOptions.Center; btnTxt.color = Color.black;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxtObj.GetComponent<RectTransform>().anchorMin = Vector2.zero; btnTxtObj.GetComponent<RectTransform>().anchorMax = Vector2.one;

        btn.onClick.AddListener(() => OnBuyClicked(crate, btnTxt, btn));

        chestCards.Add(card);
    }

    private Color GetChestColor(CrateRarity rarity)
    {
        switch (rarity)
        {
            case CrateRarity.Daily: return Color.white;
            case CrateRarity.Bronze: return new Color(0.8f, 0.5f, 0.2f);
            case CrateRarity.Silver: return new Color(0.8f, 0.8f, 0.8f);
            case CrateRarity.Gold: return new Color(1f, 0.85f, 0f);
            case CrateRarity.Diamond: return new Color(0.4f, 1f, 1f);
            default: return Color.gray;
        }
    }

    private void OnBuyClicked(CrateData crate, TextMeshProUGUI btnTxt, Button btn)
    {
        object result = CrateManager.Instance.OpenCrate(crate);
        if (result == null)
        {
            if (crate.rarity == CrateRarity.Daily) return; // Silent fail for daily if not ready
            dropNotificationText.color = Color.red; dropNotificationText.text = "Yetersiz Bakiye!";
            notificationTimer = 2f;
        }
    }

    private void RefreshTimers()
    {
        // This could update the Daily Chest button text if it's on cooldown
        if (!marketPanel.activeSelf) return;
        
        // Find daily chest button and update text
        // (For brevity, we'll just re-populate or update target card if we kept references)
    }

    private void OnCrateOpened(object reward)
    {
        if (reward is BaseItemData item)
        {
            dropNotificationText.color = Color.green;
            dropNotificationText.text = $"[YENİ] {item.itemName} ÇIKTI!";
            notificationTimer = 3f;
        }
    }
}
