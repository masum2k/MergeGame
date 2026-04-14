using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketScreenPage : MonoBehaviour
{
    private class ChestCardRef
    {
        public CrateData crate;
        public TextMeshProUGUI costText;
        public Button actionButton;
    }

    private readonly List<ChestCardRef> _cardRefs = new List<ChestCardRef>();
    private readonly List<Button> _quantityButtons = new List<Button>();

    private RectTransform _contentRoot;
    private TextMeshProUGUI _feedbackText;
    private float _feedbackTimer;
    private bool _built;
    private int _openMultiplier = 1;

    private void Start()
    {
        BuildUI();
        RebuildChestList();
    }

    private void OnEnable()
    {
        if (!_built) return;
        RebuildChestList();
    }

    private void Update()
    {
        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0f && _feedbackText != null)
            {
                _feedbackText.text = string.Empty;
            }
        }

        if (!_built || !gameObject.activeInHierarchy)
            return;

        RefreshCardStates();
    }

    private void BuildUI()
    {
        if (_built)
            return;

        RectTransform root = transform as RectTransform;

        Image bg = gameObject.GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.12f, 0.96f);
        bg.raycastTarget = true;

        GameObject panel = new GameObject("MarketPanel", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.09f, 0.13f, 0.2f, 0.96f);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.07f, 0.08f);
        panelRt.anchorMax = new Vector2(0.93f, 0.92f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "SANDIK MARKET";
        title.fontSize = 38;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.95f, 0.98f, 1f);

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -18f);
        titleRt.sizeDelta = new Vector2(0f, 52f);

        GameObject subtitleObj = new GameObject("Subtitle", typeof(RectTransform));
        subtitleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI subtitle = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitle.text = "Coin/Gem ile sandik ac, urun ve boost kazan.";
        subtitle.fontSize = 19;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = new Color(0.72f, 0.82f, 0.95f);

        RectTransform subtitleRt = subtitleObj.GetComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0f, 1f);
        subtitleRt.anchorMax = new Vector2(1f, 1f);
        subtitleRt.pivot = new Vector2(0.5f, 1f);
        subtitleRt.anchoredPosition = new Vector2(0f, -64f);
        subtitleRt.sizeDelta = new Vector2(0f, 32f);

        BuildQuantitySelector(panel.transform);

        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform));
        scrollObj.transform.SetParent(panel.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.03f, 0.12f);
        scrollRt.anchorMax = new Vector2(0.97f, 0.84f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = new Vector2(0f, -44f);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        viewportObj.AddComponent<RectMask2D>();
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        scroll.viewport = viewportRt;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewportObj.transform, false);
        _contentRoot = contentObj.GetComponent<RectTransform>();
        _contentRoot.anchorMin = new Vector2(0f, 1f);
        _contentRoot.anchorMax = new Vector2(1f, 1f);
        _contentRoot.pivot = new Vector2(0.5f, 1f);
        _contentRoot.anchoredPosition = Vector2.zero;
        _contentRoot.sizeDelta = Vector2.zero;
        scroll.content = _contentRoot;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject feedbackObj = new GameObject("Feedback", typeof(RectTransform));
        feedbackObj.transform.SetParent(panel.transform, false);
        _feedbackText = feedbackObj.AddComponent<TextMeshProUGUI>();
        _feedbackText.fontSize = 22;
        _feedbackText.alignment = TextAlignmentOptions.Center;
        _feedbackText.color = Color.white;

        RectTransform feedbackRt = feedbackObj.GetComponent<RectTransform>();
        feedbackRt.anchorMin = new Vector2(0f, 0f);
        feedbackRt.anchorMax = new Vector2(1f, 0f);
        feedbackRt.pivot = new Vector2(0.5f, 0f);
        feedbackRt.anchoredPosition = new Vector2(0f, 10f);
        feedbackRt.sizeDelta = new Vector2(0f, 38f);

        _built = true;
    }

    private void BuildQuantitySelector(Transform parent)
    {
        GameObject qtyRoot = new GameObject("QuantitySelector", typeof(RectTransform));
        qtyRoot.transform.SetParent(parent, false);
        RectTransform qtyRt = qtyRoot.GetComponent<RectTransform>();
        qtyRt.anchorMin = new Vector2(0.03f, 1f);
        qtyRt.anchorMax = new Vector2(0.97f, 1f);
        qtyRt.pivot = new Vector2(0.5f, 1f);
        qtyRt.anchoredPosition = new Vector2(0f, -102f);
        qtyRt.sizeDelta = new Vector2(0f, 38f);

        HorizontalLayoutGroup hlg = qtyRoot.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        CreateQtyButton(qtyRoot.transform, 1, "x1");
        CreateQtyButton(qtyRoot.transform, 10, "x10");
        CreateQtyButton(qtyRoot.transform, 50, "x50");
        CreateQtyButton(qtyRoot.transform, 100, "x100");

        RefreshQuantityButtonVisuals();
    }

    private void CreateQtyButton(Transform parent, int value, string label)
    {
        GameObject btnObj = new GameObject("Qty_" + label, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        Image bg = btnObj.AddComponent<Image>();
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() =>
        {
            _openMultiplier = value;
            RefreshQuantityButtonVisuals();
            RefreshCardStates();
        });

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 18;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.1f, 0.1f, 0.12f);

        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        _quantityButtons.Add(btn);
    }

    private void RefreshQuantityButtonVisuals()
    {
        for (int i = 0; i < _quantityButtons.Count; i++)
        {
            Button btn = _quantityButtons[i];
            if (btn == null || btn.targetGraphic == null)
                continue;

            int value = 1;
            if (i == 1) value = 10;
            else if (i == 2) value = 50;
            else if (i == 3) value = 100;

            btn.targetGraphic.color = (value == _openMultiplier)
                ? new Color(1f, 0.86f, 0.36f, 1f)
                : new Color(0.54f, 0.61f, 0.72f, 1f);
        }
    }

    private void RebuildChestList()
    {
        if (!_built || _contentRoot == null)
            return;

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_contentRoot.GetChild(i).gameObject);
        }

        _cardRefs.Clear();

        if (CrateManager.Instance == null || CrateManager.Instance.AllCrates == null)
            return;

        List<CrateData> crates = new List<CrateData>(CrateManager.Instance.AllCrates);
        crates.Sort((a, b) => GetRarityOrder(a.rarity).CompareTo(GetRarityOrder(b.rarity)));

        for (int i = 0; i < crates.Count; i++)
        {
            CreateCrateCard(crates[i]);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        RefreshCardStates();
    }

    private void CreateCrateCard(CrateData crate)
    {
        GameObject card = new GameObject("Card_" + crate.crateName, typeof(RectTransform));
        card.transform.SetParent(_contentRoot, false);

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.14f, 0.19f, 0.28f, 1f);

        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(0f, 118f);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
        iconObj.transform.SetParent(card.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.color = GetRarityColor(crate.rarity);

        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(16f, 0f);
        iconRt.sizeDelta = new Vector2(84f, 84f);

        GameObject nameObj = new GameObject("Name", typeof(RectTransform));
        nameObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = crate.crateName;
        nameText.fontSize = 27;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.95f, 0.98f, 1f);
        nameText.alignment = TextAlignmentOptions.Left;

        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0.5f);
        nameRt.anchorMax = new Vector2(1f, 0.5f);
        nameRt.pivot = new Vector2(0f, 0.5f);
        nameRt.offsetMin = new Vector2(116f, 10f);
        nameRt.offsetMax = new Vector2(-190f, 36f);

        GameObject detailObj = new GameObject("Detail", typeof(RectTransform));
        detailObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI detailText = detailObj.AddComponent<TextMeshProUGUI>();
        detailText.text = GetChestDescription(crate);
        detailText.fontSize = 18;
        detailText.color = new Color(0.72f, 0.82f, 0.95f);
        detailText.alignment = TextAlignmentOptions.Left;

        RectTransform detailRt = detailObj.GetComponent<RectTransform>();
        detailRt.anchorMin = new Vector2(0f, 0.5f);
        detailRt.anchorMax = new Vector2(1f, 0.5f);
        detailRt.pivot = new Vector2(0f, 0.5f);
        detailRt.offsetMin = new Vector2(116f, -42f);
        detailRt.offsetMax = new Vector2(-190f, 8f);

        GameObject buttonObj = new GameObject("ActionButton", typeof(RectTransform));
        buttonObj.transform.SetParent(card.transform, false);
        Image buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.color = GetCurrencyColor(crate.currencyType, crate.rarity == CrateRarity.Daily);
        Button button = buttonObj.AddComponent<Button>();

        RectTransform buttonRt = buttonObj.GetComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(1f, 0.5f);
        buttonRt.anchorMax = new Vector2(1f, 0.5f);
        buttonRt.pivot = new Vector2(1f, 0.5f);
        buttonRt.anchoredPosition = new Vector2(-16f, 0f);
        buttonRt.sizeDelta = new Vector2(165f, 62f);

        GameObject buttonTextObj = new GameObject("ButtonText", typeof(RectTransform));
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.fontSize = 18;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = new Color(0.08f, 0.08f, 0.1f);
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform buttonTextRt = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRt.anchorMin = Vector2.zero;
        buttonTextRt.anchorMax = Vector2.one;
        buttonTextRt.offsetMin = Vector2.zero;
        buttonTextRt.offsetMax = Vector2.zero;

        button.onClick.AddListener(() => OnChestClicked(crate));

        _cardRefs.Add(new ChestCardRef
        {
            crate = crate,
            actionButton = button,
            costText = buttonText
        });
    }

    private void RefreshCardStates()
    {
        if (CrateManager.Instance == null || CurrencyManager.Instance == null)
            return;

        float costMultiplier = ResearchManager.Instance != null ? ResearchManager.Instance.GetChestCostMultiplier() : 1f;

        for (int i = 0; i < _cardRefs.Count; i++)
        {
            ChestCardRef card = _cardRefs[i];
            if (card == null || card.crate == null || card.costText == null || card.actionButton == null)
                continue;

            bool interactable = true;
            if (card.crate.rarity == CrateRarity.Daily)
            {
                if (CrateManager.Instance.IsDailyChestAvailable())
                {
                    card.costText.text = "Ucretsiz Ac";
                    interactable = true;
                }
                else
                {
                    System.TimeSpan wait = CrateManager.Instance.GetTimeUntilDailyChest();
                    card.costText.text = wait.Hours.ToString("D2") + ":" + wait.Minutes.ToString("D2") + ":" + wait.Seconds.ToString("D2");
                    interactable = false;
                }
            }
            else if (card.crate.currencyType == CurrencyType.Coin)
            {
                int unitCost = Mathf.Max(1, Mathf.RoundToInt(card.crate.cost * costMultiplier));
                int totalCost = unitCost * Mathf.Max(1, _openMultiplier);
                card.costText.text = "x" + _openMultiplier + "  " + totalCost + " Coin";
                interactable = CurrencyManager.Instance.Coin >= totalCost;
            }
            else
            {
                int unitCost = Mathf.Max(1, Mathf.RoundToInt(card.crate.cost * costMultiplier));
                int totalCost = unitCost * Mathf.Max(1, _openMultiplier);
                card.costText.text = "x" + _openMultiplier + "  " + totalCost + " Gem";
                interactable = CurrencyManager.Instance.Gem >= totalCost;
            }

            card.actionButton.interactable = interactable;
        }
    }

    private void OnChestClicked(CrateData crate)
    {
        if (CrateManager.Instance == null)
            return;

        float costMultiplier = ResearchManager.Instance != null ? ResearchManager.Instance.GetChestCostMultiplier() : 1f;
        int count = crate.rarity == CrateRarity.Daily ? 1 : Mathf.Max(1, _openMultiplier);
        int unitCost = Mathf.Max(1, Mathf.RoundToInt(crate.cost * costMultiplier));

        List<object> rewards = CrateManager.Instance.OpenCrates(crate, count, unitCost);

        if (rewards == null || rewards.Count == 0)
        {
            ShowFeedback("Yetersiz bakiye veya sandik hazir degil.", Color.red);
            return;
        }

        int cropCount = 0;
        int boostCount = 0;
        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is CropData) cropCount++;
            else if (rewards[i] is BoostData) boostCount++;
        }

        string summary = "Acildi x" + rewards.Count + "  |  Besin: " + cropCount + "  Boost: " + boostCount;
        ShowFeedback(summary, new Color(0.4f, 1f, 0.6f));

        RefreshCardStates();
    }

    private void ShowFeedback(string message, Color color)
    {
        if (_feedbackText == null)
            return;

        _feedbackText.text = message;
        _feedbackText.color = color;
        _feedbackTimer = 2.5f;
    }

    private int GetRarityOrder(CrateRarity rarity)
    {
        switch (rarity)
        {
            case CrateRarity.Daily: return 0;
            case CrateRarity.Bronze: return 1;
            case CrateRarity.Silver: return 2;
            case CrateRarity.Gold: return 3;
            case CrateRarity.Diamond: return 4;
            default: return 99;
        }
    }

    private Color GetCurrencyColor(CurrencyType currencyType, bool isDaily)
    {
        if (isDaily)
            return new Color(0.75f, 0.92f, 0.35f, 1f);

        return currencyType == CurrencyType.Coin
            ? new Color(1f, 0.82f, 0.24f, 1f)
            : new Color(0.2f, 0.9f, 1f, 1f);
    }

    private Color GetRarityColor(CrateRarity rarity)
    {
        switch (rarity)
        {
            case CrateRarity.Daily: return new Color(0.82f, 0.95f, 0.95f);
            case CrateRarity.Bronze: return new Color(0.78f, 0.49f, 0.26f);
            case CrateRarity.Silver: return new Color(0.78f, 0.82f, 0.88f);
            case CrateRarity.Gold: return new Color(1f, 0.82f, 0.24f);
            case CrateRarity.Diamond: return new Color(0.25f, 1f, 0.95f);
            default: return Color.gray;
        }
    }

    private string GetChestDescription(CrateData crate)
    {
        if (crate.rarity == CrateRarity.Daily)
            return "Gunluk odul: 12 saatte bir acilir.";

        if (crate.currencyType == CurrencyType.Coin)
            return "Coin sandigi: urun ve boost sansi.";

        return "Gem sandigi: yuksek tier odul sansi.";
    }
}
