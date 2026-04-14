using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FactoryScreenPage : MonoBehaviour
{
    private enum RewardFilter
    {
        All,
        Research,
        Gem,
        Crop
    }

    private class OfferCardRef
    {
        public string offerId;
        public GameObject rootObject;
        public TextMeshProUGUI requirementText;
        public TextMeshProUGUI rewardText;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI timerText;
        public Button actionButton;
    }

    private class FilterButtonRef
    {
        public RewardFilter filter;
        public Image background;
        public TextMeshProUGUI label;
    }

    private readonly List<OfferCardRef> _cards = new List<OfferCardRef>();
    private readonly List<FilterButtonRef> _filterButtons = new List<FilterButtonRef>();

    private RectTransform _contentRoot;
    private TextMeshProUGUI _researchPointsText;
    private TextMeshProUGUI _feedbackText;
    private bool _built;
    private float _feedbackTimer;
    private float _stateRefreshTimer;
    private RewardFilter _activeFilter = RewardFilter.All;

    private void Start()
    {
        BuildUI();
        SubscribeEvents();
        RefreshAll();
    }

    private void OnEnable()
    {
        if (!_built) return;

        if (FactoryManager.Instance != null)
        {
            FactoryManager.Instance.EnsureOffers();
        }

        RefreshAll();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
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

        _stateRefreshTimer += Time.unscaledDeltaTime;
        if (_stateRefreshTimer < 0.25f)
            return;

        _stateRefreshTimer = 0f;
        RefreshCardStatesOnly();
    }

    private void BuildUI()
    {
        if (_built)
            return;

        RectTransform root = transform as RectTransform;

        Image bg = gameObject.GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.12f, 0.96f);
        bg.raycastTarget = true;

        GameObject panel = new GameObject("FactoryPanel", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.14f, 0.1f, 0.18f, 0.96f);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.06f, 0.08f);
        panelRt.anchorMax = new Vector2(0.94f, 0.92f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "FABRIKA / HUNI";
        title.fontSize = 38;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.96f, 0.9f, 1f);

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -16f);
        titleRt.sizeDelta = new Vector2(0f, 52f);

        GameObject subtitleObj = new GameObject("Subtitle", typeof(RectTransform));
        subtitleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI subtitle = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitle.text = "Gunluk gorev havuzu: kaydir, filtrele, teslim et.";
        subtitle.fontSize = 18;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = new Color(0.8f, 0.72f, 0.95f);

        RectTransform subtitleRt = subtitleObj.GetComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0f, 1f);
        subtitleRt.anchorMax = new Vector2(1f, 1f);
        subtitleRt.pivot = new Vector2(0.5f, 1f);
        subtitleRt.anchoredPosition = new Vector2(0f, -62f);
        subtitleRt.sizeDelta = new Vector2(0f, 32f);

        GameObject rpObj = new GameObject("ResearchPoints", typeof(RectTransform));
        rpObj.transform.SetParent(panel.transform, false);
        _researchPointsText = rpObj.AddComponent<TextMeshProUGUI>();
        _researchPointsText.fontSize = 24;
        _researchPointsText.fontStyle = FontStyles.Bold;
        _researchPointsText.alignment = TextAlignmentOptions.Center;
        _researchPointsText.color = new Color(0.76f, 1f, 0.82f);

        RectTransform rpRt = rpObj.GetComponent<RectTransform>();
        rpRt.anchorMin = new Vector2(0f, 1f);
        rpRt.anchorMax = new Vector2(1f, 1f);
        rpRt.pivot = new Vector2(0.5f, 1f);
        rpRt.anchoredPosition = new Vector2(0f, -96f);
        rpRt.sizeDelta = new Vector2(0f, 36f);

        BuildFilterBar(panel.transform);

        GameObject scrollObj = new GameObject("OfferScroll", typeof(RectTransform));
        scrollObj.transform.SetParent(panel.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.03f, 0.13f);
        scrollRt.anchorMax = new Vector2(0.97f, 0.74f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        viewportObj.AddComponent<RectMask2D>();
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        sr.viewport = viewportRt;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewportObj.transform, false);
        _contentRoot = contentObj.GetComponent<RectTransform>();
        _contentRoot.anchorMin = new Vector2(0f, 1f);
        _contentRoot.anchorMax = new Vector2(1f, 1f);
        _contentRoot.pivot = new Vector2(0.5f, 1f);
        _contentRoot.anchoredPosition = Vector2.zero;
        _contentRoot.sizeDelta = Vector2.zero;
        sr.content = _contentRoot;

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
        feedbackRt.sizeDelta = new Vector2(0f, 42f);

        _built = true;
    }

    private void BuildFilterBar(Transform parent)
    {
        GameObject filterRootObj = new GameObject("FilterBar", typeof(RectTransform));
        filterRootObj.transform.SetParent(parent, false);

        RectTransform filterRootRt = filterRootObj.GetComponent<RectTransform>();
        filterRootRt.anchorMin = new Vector2(0.03f, 1f);
        filterRootRt.anchorMax = new Vector2(0.97f, 1f);
        filterRootRt.pivot = new Vector2(0.5f, 1f);
        filterRootRt.anchoredPosition = new Vector2(0f, -134f);
        filterRootRt.sizeDelta = new Vector2(0f, 40f);

        HorizontalLayoutGroup hlg = filterRootObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        CreateFilterButton(filterRootObj.transform, "Hepsi", RewardFilter.All);
        CreateFilterButton(filterRootObj.transform, "RP", RewardFilter.Research);
        CreateFilterButton(filterRootObj.transform, "Gem", RewardFilter.Gem);
        CreateFilterButton(filterRootObj.transform, "Besin", RewardFilter.Crop);
    }

    private void CreateFilterButton(Transform parent, string labelText, RewardFilter filter)
    {
        GameObject btnObj = new GameObject("Filter_" + labelText, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.44f, 0.48f, 0.56f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() =>
        {
            _activeFilter = filter;
            RefreshFilterButtonVisuals();
            ApplyFilterVisibility();
        });

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);

        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = labelText;
        txt.fontSize = 17;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.1f, 0.1f, 0.14f);

        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        _filterButtons.Add(new FilterButtonRef
        {
            filter = filter,
            background = bg,
            label = txt
        });
    }

    private void RefreshFilterButtonVisuals()
    {
        for (int i = 0; i < _filterButtons.Count; i++)
        {
            FilterButtonRef btn = _filterButtons[i];
            if (btn == null || btn.background == null)
                continue;

            bool isActive = btn.filter == _activeFilter;
            btn.background.color = isActive
                ? new Color(1f, 0.84f, 0.3f, 1f)
                : new Color(0.44f, 0.48f, 0.56f, 1f);

            if (btn.label != null)
            {
                btn.label.color = isActive
                    ? new Color(0.16f, 0.12f, 0.06f)
                    : new Color(0.12f, 0.12f, 0.16f);
            }
        }
    }

    private void RefreshAll()
    {
        UpdateResearchPointText();
        RebuildOfferCards();
        RefreshFilterButtonVisuals();
        ApplyFilterVisibility();
        RefreshCardStatesOnly();
    }

    private void RebuildOfferCards()
    {
        if (!_built || _contentRoot == null)
            return;

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_contentRoot.GetChild(i).gameObject);
        }

        _cards.Clear();

        if (FactoryManager.Instance == null)
            return;

        IReadOnlyList<FactoryOfferData> offers = FactoryManager.Instance.CurrentOffers;
        if (offers == null)
            return;

        for (int i = 0; i < offers.Count; i++)
        {
            CreateOfferCard(offers[i]);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
    }

    private void CreateOfferCard(FactoryOfferData offer)
    {
        GameObject card = new GameObject("Offer_" + offer.offerId, typeof(RectTransform));
        card.transform.SetParent(_contentRoot, false);

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.2f, 0.12f, 0.24f, 1f);

        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(0f, 176f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = offer.cropName + " Donusumu";
        title.fontSize = 27;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.96f, 0.9f, 1f);
        title.alignment = TextAlignmentOptions.Left;

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.offsetMin = new Vector2(14f, -46f);
        titleRt.offsetMax = new Vector2(-180f, -8f);

        GameObject reqObj = new GameObject("Requirement", typeof(RectTransform));
        reqObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI req = reqObj.AddComponent<TextMeshProUGUI>();
        req.fontSize = 18;
        req.color = new Color(0.82f, 0.75f, 0.95f);
        req.alignment = TextAlignmentOptions.Left;

        RectTransform reqRt = reqObj.GetComponent<RectTransform>();
        reqRt.anchorMin = new Vector2(0f, 1f);
        reqRt.anchorMax = new Vector2(1f, 1f);
        reqRt.pivot = new Vector2(0f, 1f);
        reqRt.offsetMin = new Vector2(14f, -78f);
        reqRt.offsetMax = new Vector2(-180f, -48f);

        GameObject rewardObj = new GameObject("Reward", typeof(RectTransform));
        rewardObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI reward = rewardObj.AddComponent<TextMeshProUGUI>();
        reward.fontSize = 18;
        reward.color = new Color(0.86f, 0.92f, 0.62f);
        reward.alignment = TextAlignmentOptions.Left;

        RectTransform rewardRt = rewardObj.GetComponent<RectTransform>();
        rewardRt.anchorMin = new Vector2(0f, 1f);
        rewardRt.anchorMax = new Vector2(1f, 1f);
        rewardRt.pivot = new Vector2(0f, 1f);
        rewardRt.offsetMin = new Vector2(14f, -106f);
        rewardRt.offsetMax = new Vector2(-180f, -76f);

        GameObject progressObj = new GameObject("Progress", typeof(RectTransform));
        progressObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI progress = progressObj.AddComponent<TextMeshProUGUI>();
        progress.fontSize = 20;
        progress.fontStyle = FontStyles.Bold;
        progress.color = new Color(0.76f, 1f, 0.82f);
        progress.alignment = TextAlignmentOptions.Left;

        RectTransform progressRt = progressObj.GetComponent<RectTransform>();
        progressRt.anchorMin = new Vector2(0f, 1f);
        progressRt.anchorMax = new Vector2(1f, 1f);
        progressRt.pivot = new Vector2(0f, 1f);
        progressRt.offsetMin = new Vector2(14f, -136f);
        progressRt.offsetMax = new Vector2(-180f, -104f);

        GameObject timerObj = new GameObject("Timer", typeof(RectTransform));
        timerObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI timer = timerObj.AddComponent<TextMeshProUGUI>();
        timer.fontSize = 17;
        timer.alignment = TextAlignmentOptions.Left;
        timer.color = new Color(0.95f, 0.82f, 0.6f);

        RectTransform timerRt = timerObj.GetComponent<RectTransform>();
        timerRt.anchorMin = new Vector2(0f, 0f);
        timerRt.anchorMax = new Vector2(1f, 0f);
        timerRt.pivot = new Vector2(0f, 0f);
        timerRt.offsetMin = new Vector2(14f, 8f);
        timerRt.offsetMax = new Vector2(-180f, 34f);

        GameObject btnObj = new GameObject("DeliverButton", typeof(RectTransform));
        btnObj.transform.SetParent(card.transform, false);
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.94f, 0.7f, 0.26f, 1f);
        Button btn = btnObj.AddComponent<Button>();

        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0.5f);
        btnRt.anchorMax = new Vector2(1f, 0.5f);
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.anchoredPosition = new Vector2(-14f, 0f);
        btnRt.sizeDelta = new Vector2(155f, 84f);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "Huniye Ver";
        btnTxt.fontSize = 22;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.color = new Color(0.2f, 0.14f, 0.08f);
        btnTxt.alignment = TextAlignmentOptions.Center;

        RectTransform btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.offsetMin = Vector2.zero;
        btnTxtRt.offsetMax = Vector2.zero;

        string offerId = offer.offerId;
        btn.onClick.AddListener(() => OnOfferButtonClicked(offerId));

        _cards.Add(new OfferCardRef
        {
            offerId = offerId,
            rootObject = card,
            requirementText = req,
            rewardText = reward,
            progressText = progress,
            timerText = timer,
            actionButton = btn
        });
    }

    private void RefreshCardStatesOnly()
    {
        if (FactoryManager.Instance == null)
            return;

        IReadOnlyList<FactoryOfferData> offers = FactoryManager.Instance.CurrentOffers;

        for (int i = 0; i < _cards.Count; i++)
        {
            OfferCardRef card = _cards[i];
            if (card == null)
                continue;

            FactoryOfferData offer = FindOffer(offers, card.offerId);
            if (offer == null)
                continue;

            int count = InventoryManager.Instance != null ? InventoryManager.Instance.GetCount(offer.cropName) : 0;
            bool hasEnough = count >= offer.requiredAmount;
            bool expired = FactoryManager.Instance.GetRemainingTime(offer) <= System.TimeSpan.Zero;

            if (card.requirementText != null)
            {
                card.requirementText.text = "Gereken: " + offer.requiredAmount + " " + offer.cropName;
            }

            if (card.rewardText != null)
            {
                card.rewardText.text = "Odul: " + BuildRewardSummary(offer);
            }

            if (card.progressText != null)
            {
                card.progressText.text = "Envanter: " + count + "/" + offer.requiredAmount;
                card.progressText.color = hasEnough ? new Color(0.5f, 1f, 0.65f) : new Color(0.95f, 0.6f, 0.6f);
            }

            if (card.timerText != null)
            {
                System.TimeSpan remaining = FactoryManager.Instance.GetRemainingTime(offer);
                card.timerText.text = offer.completed
                    ? "Durum: Tamamlandi"
                    : (expired ? "Durum: Sure doldu" : "Kalan sure: " + FormatTime(remaining));
            }

            if (card.actionButton != null)
            {
                card.actionButton.interactable = !offer.completed && !expired && hasEnough;
            }
        }
    }

    private void OnOfferButtonClicked(string offerId)
    {
        if (FactoryManager.Instance == null)
            return;

        bool success = FactoryManager.Instance.TryCompleteOffer(offerId, out string feedback);
        ShowFeedback(feedback, success ? new Color(0.48f, 1f, 0.64f) : new Color(1f, 0.48f, 0.48f));

        RefreshAll();
    }

    private void ShowFeedback(string message, Color color)
    {
        if (_feedbackText == null)
            return;

        _feedbackText.text = message;
        _feedbackText.color = color;
        _feedbackTimer = 2.8f;
    }

    private string BuildRewardSummary(FactoryOfferData offer)
    {
        if (offer == null)
            return "-";

        List<string> parts = new List<string>();
        if (offer.rewardResearchPoints > 0) parts.Add("+" + offer.rewardResearchPoints + " RP");
        if (offer.rewardGems > 0) parts.Add("+" + offer.rewardGems + " Gem");
        if (offer.rewardCoins > 0) parts.Add("+" + offer.rewardCoins + " Coin");
        if (offer.rewardCropAmount > 0 && !string.IsNullOrWhiteSpace(offer.rewardCropName))
        {
            parts.Add("+" + offer.rewardCropAmount + "x " + offer.rewardCropName);
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : "Yok";
    }

    private bool OfferMatchesFilter(FactoryOfferData offer)
    {
        if (offer == null)
            return false;

        switch (_activeFilter)
        {
            case RewardFilter.Research:
                return offer.rewardResearchPoints > 0;
            case RewardFilter.Gem:
                return offer.rewardGems > 0;
            case RewardFilter.Crop:
                return offer.rewardCropAmount > 0 && !string.IsNullOrWhiteSpace(offer.rewardCropName);
            case RewardFilter.All:
            default:
                return true;
        }
    }

    private void ApplyFilterVisibility()
    {
        if (_contentRoot == null || FactoryManager.Instance == null)
            return;

        IReadOnlyList<FactoryOfferData> offers = FactoryManager.Instance.CurrentOffers;

        for (int i = 0; i < _cards.Count; i++)
        {
            OfferCardRef card = _cards[i];
            if (card == null || card.rootObject == null)
                continue;

            FactoryOfferData offer = FindOffer(offers, card.offerId);
            bool visible = offer != null && OfferMatchesFilter(offer);

            if (card.rootObject.activeSelf != visible)
            {
                card.rootObject.SetActive(visible);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
    }

    private FactoryOfferData FindOffer(IReadOnlyList<FactoryOfferData> offers, string offerId)
    {
        if (offers == null)
            return null;

        for (int i = 0; i < offers.Count; i++)
        {
            if (offers[i].offerId == offerId)
            {
                return offers[i];
            }
        }

        return null;
    }

    private string FormatTime(System.TimeSpan time)
    {
        return time.Hours.ToString("D2") + ":" + time.Minutes.ToString("D2") + ":" + time.Seconds.ToString("D2");
    }

    private void UpdateResearchPointText()
    {
        if (_researchPointsText == null)
            return;

        int points = ResearchManager.Instance != null ? ResearchManager.Instance.ResearchPoints : 0;
        _researchPointsText.text = "Arastirma Puani: " + points;
    }

    private void SubscribeEvents()
    {
        if (FactoryManager.Instance != null)
        {
            FactoryManager.Instance.OnOffersChanged += RefreshAll;
        }

        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnResearchPointsChanged += HandleResearchChanged;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshCardStatesOnly;
        }
    }

    private void UnsubscribeEvents()
    {
        if (FactoryManager.Instance != null)
        {
            FactoryManager.Instance.OnOffersChanged -= RefreshAll;
        }

        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnResearchPointsChanged -= HandleResearchChanged;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshCardStatesOnly;
        }
    }

    private void HandleResearchChanged(int _)
    {
        UpdateResearchPointText();
    }
}
