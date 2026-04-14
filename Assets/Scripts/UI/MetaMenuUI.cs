using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MetaMenuUI : MonoBehaviour
{
    public static MetaMenuUI Instance { get; private set; }

    private GameObject _panel;
    private TextMeshProUGUI _prestigeHeaderText;
    private TextMeshProUGUI _prestigeReqText;
    private TextMeshProUGUI _feedbackText;
    private Button _prestigeButton;
    private RectTransform _messageContent;

    private readonly List<GameObject> _messageRows = new List<GameObject>();

    private bool _built;

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
        BuildUI();
        SubscribeEvents();
        RefreshAll();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    public void Toggle()
    {
        if (!_built || _panel == null)
            return;

        bool nextState = !_panel.activeSelf;
        _panel.SetActive(nextState);

        if (nextState)
        {
            RefreshAll();
        }
    }

    private void BuildUI()
    {
        if (_built)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        _panel = new GameObject("MetaMenuPanel", typeof(RectTransform));
        _panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 1f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.pivot = new Vector2(1f, 1f);
        panelRt.anchoredPosition = new Vector2(-10f, -100f);
        panelRt.sizeDelta = new Vector2(440f, 620f);

        Image panelBg = _panel.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.1f, 0.15f, 0.97f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(_panel.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "MENU";
        title.fontSize = 32;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -12f);
        titleRt.sizeDelta = new Vector2(0f, 42f);

        GameObject prestigeHeaderObj = new GameObject("PrestigeHeader", typeof(RectTransform));
        prestigeHeaderObj.transform.SetParent(_panel.transform, false);
        _prestigeHeaderText = prestigeHeaderObj.AddComponent<TextMeshProUGUI>();
        _prestigeHeaderText.fontSize = 19;
        _prestigeHeaderText.fontStyle = FontStyles.Bold;
        _prestigeHeaderText.alignment = TextAlignmentOptions.Left;
        _prestigeHeaderText.color = new Color(1f, 0.9f, 0.4f);

        RectTransform phRt = prestigeHeaderObj.GetComponent<RectTransform>();
        phRt.anchorMin = new Vector2(0f, 1f);
        phRt.anchorMax = new Vector2(1f, 1f);
        phRt.pivot = new Vector2(0f, 1f);
        phRt.offsetMin = new Vector2(16f, -74f);
        phRt.offsetMax = new Vector2(-16f, -44f);

        GameObject reqObj = new GameObject("PrestigeReq", typeof(RectTransform));
        reqObj.transform.SetParent(_panel.transform, false);
        _prestigeReqText = reqObj.AddComponent<TextMeshProUGUI>();
        _prestigeReqText.fontSize = 16;
        _prestigeReqText.alignment = TextAlignmentOptions.TopLeft;
        _prestigeReqText.color = new Color(0.82f, 0.9f, 1f);

        RectTransform reqRt = reqObj.GetComponent<RectTransform>();
        reqRt.anchorMin = new Vector2(0f, 1f);
        reqRt.anchorMax = new Vector2(1f, 1f);
        reqRt.pivot = new Vector2(0f, 1f);
        reqRt.offsetMin = new Vector2(16f, -260f);
        reqRt.offsetMax = new Vector2(-16f, -84f);

        GameObject prestigeBtnObj = new GameObject("PrestigeButton", typeof(RectTransform));
        prestigeBtnObj.transform.SetParent(_panel.transform, false);
        Image prestigeBtnBg = prestigeBtnObj.AddComponent<Image>();
        prestigeBtnBg.color = new Color(0.93f, 0.72f, 0.23f, 1f);
        _prestigeButton = prestigeBtnObj.AddComponent<Button>();
        _prestigeButton.onClick.AddListener(OnPrestigeClicked);

        RectTransform pbRt = prestigeBtnObj.GetComponent<RectTransform>();
        pbRt.anchorMin = new Vector2(0f, 1f);
        pbRt.anchorMax = new Vector2(1f, 1f);
        pbRt.pivot = new Vector2(0.5f, 1f);
        pbRt.offsetMin = new Vector2(16f, -312f);
        pbRt.offsetMax = new Vector2(-16f, -266f);

        GameObject pbTextObj = new GameObject("Text", typeof(RectTransform));
        pbTextObj.transform.SetParent(prestigeBtnObj.transform, false);
        TextMeshProUGUI pbText = pbTextObj.AddComponent<TextMeshProUGUI>();
        pbText.text = "Prestij Yukselt";
        pbText.fontSize = 22;
        pbText.fontStyle = FontStyles.Bold;
        pbText.alignment = TextAlignmentOptions.Center;
        pbText.color = new Color(0.15f, 0.1f, 0.05f);

        RectTransform pbtRt = pbTextObj.GetComponent<RectTransform>();
        pbtRt.anchorMin = Vector2.zero;
        pbtRt.anchorMax = Vector2.one;
        pbtRt.offsetMin = Vector2.zero;
        pbtRt.offsetMax = Vector2.zero;

        GameObject msgTitleObj = new GameObject("MessagesTitle", typeof(RectTransform));
        msgTitleObj.transform.SetParent(_panel.transform, false);
        TextMeshProUGUI msgTitle = msgTitleObj.AddComponent<TextMeshProUGUI>();
        msgTitle.text = "Mesajlar";
        msgTitle.fontSize = 20;
        msgTitle.fontStyle = FontStyles.Bold;
        msgTitle.alignment = TextAlignmentOptions.Left;
        msgTitle.color = new Color(0.7f, 0.9f, 1f);

        RectTransform mtRt = msgTitleObj.GetComponent<RectTransform>();
        mtRt.anchorMin = new Vector2(0f, 1f);
        mtRt.anchorMax = new Vector2(1f, 1f);
        mtRt.pivot = new Vector2(0f, 1f);
        mtRt.offsetMin = new Vector2(16f, -352f);
        mtRt.offsetMax = new Vector2(-16f, -324f);

        GameObject scrollObj = new GameObject("MessageScroll", typeof(RectTransform));
        scrollObj.transform.SetParent(_panel.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(16f, 48f);
        scrollRt.offsetMax = new Vector2(-16f, -362f);

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
        _messageContent = contentObj.GetComponent<RectTransform>();
        _messageContent.anchorMin = new Vector2(0f, 1f);
        _messageContent.anchorMax = new Vector2(1f, 1f);
        _messageContent.pivot = new Vector2(0.5f, 1f);
        _messageContent.anchoredPosition = Vector2.zero;
        _messageContent.sizeDelta = Vector2.zero;
        sr.content = _messageContent;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(2, 2, 2, 2);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject feedbackObj = new GameObject("Feedback", typeof(RectTransform));
        feedbackObj.transform.SetParent(_panel.transform, false);
        _feedbackText = feedbackObj.AddComponent<TextMeshProUGUI>();
        _feedbackText.fontSize = 16;
        _feedbackText.alignment = TextAlignmentOptions.Center;
        _feedbackText.color = new Color(0.95f, 0.6f, 0.6f);

        RectTransform fbRt = feedbackObj.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0f, 0f);
        fbRt.anchorMax = new Vector2(1f, 0f);
        fbRt.pivot = new Vector2(0.5f, 0f);
        fbRt.anchoredPosition = new Vector2(0f, 10f);
        fbRt.sizeDelta = new Vector2(0f, 28f);

        _panel.SetActive(false);
        _built = true;
    }

    private void SubscribeEvents()
    {
        if (GameMessageManager.Instance != null)
        {
            GameMessageManager.Instance.OnMessagesChanged += RefreshMessages;
        }

        if (PrestigeManager.Instance != null)
        {
            PrestigeManager.Instance.OnPrestigeDataChanged += RefreshPrestige;
        }

        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnResearchPointsChanged += HandleResearchPointsChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (GameMessageManager.Instance != null)
        {
            GameMessageManager.Instance.OnMessagesChanged -= RefreshMessages;
        }

        if (PrestigeManager.Instance != null)
        {
            PrestigeManager.Instance.OnPrestigeDataChanged -= RefreshPrestige;
        }

        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnResearchPointsChanged -= HandleResearchPointsChanged;
        }
    }

    private void HandleResearchPointsChanged(int _)
    {
        RefreshPrestige();
    }

    private void RefreshAll()
    {
        RefreshPrestige();
        RefreshMessages();
    }

    private void RefreshPrestige()
    {
        if (!_built)
            return;

        if (PrestigeManager.Instance == null || LevelManager.Instance == null || CurrencyManager.Instance == null || ResearchManager.Instance == null)
            return;

        PrestigeRequirementData req = PrestigeManager.Instance.GetNextRequirement();

        _prestigeHeaderText.text = "Prestij " + PrestigeManager.Instance.PrestigeLevel +
                                   "  |  Coin Cap: " + PrestigeManager.Instance.GetCoinCap();

        int level = LevelManager.Instance.Level;
        int lifeCoins = CurrencyManager.Instance.LifetimeCoinEarned;
        int spentRp = ResearchManager.Instance.TotalResearchSpent;
        int completions = PrestigeManager.Instance.FactoryCompletions;

        string reqText =
            "Hedef Prestij: " + req.targetPrestigeLevel + "\n" +
            "Level: " + level + "/" + req.requiredLevel + "\n" +
            "Toplam Coin: " + lifeCoins + "/" + req.requiredLifetimeCoins + "\n" +
            "Harcanan RP: " + spentRp + "/" + req.requiredResearchSpent + "\n" +
            "Fabrika Gorevi: " + completions + "/" + req.requiredFactoryCompletions;

        _prestigeReqText.text = reqText;

        _prestigeButton.interactable = PrestigeManager.Instance.CanPrestige(out _);
    }

    private void RefreshMessages()
    {
        if (!_built || _messageContent == null)
            return;

        for (int i = 0; i < _messageRows.Count; i++)
        {
            if (_messageRows[i] != null)
            {
                Destroy(_messageRows[i]);
            }
        }
        _messageRows.Clear();

        if (GameMessageManager.Instance == null)
            return;

        IReadOnlyList<GameMessageEntry> messages = GameMessageManager.Instance.Messages;
        for (int i = 0; i < messages.Count; i++)
        {
            CreateMessageRow(messages[i]);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_messageContent);
    }

    private void CreateMessageRow(GameMessageEntry entry)
    {
        GameObject row = new GameObject("MsgRow", typeof(RectTransform));
        row.transform.SetParent(_messageContent, false);

        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.13f, 0.16f, 0.23f, 0.95f);

        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0f, 58f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = "[" + entry.timestamp + "] " + entry.message;
        txt.fontSize = 14;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.color = new Color(0.86f, 0.92f, 1f);

        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(8f, 6f);
        txtRt.offsetMax = new Vector2(-8f, -6f);

        _messageRows.Add(row);
    }

    private void OnPrestigeClicked()
    {
        if (PrestigeManager.Instance == null)
            return;

        bool ok = PrestigeManager.Instance.TryPrestige(out string feedback);
        _feedbackText.text = feedback;
        _feedbackText.color = ok ? new Color(0.52f, 1f, 0.6f) : new Color(1f, 0.54f, 0.54f);

        RefreshPrestige();
        RefreshMessages();
    }
}
