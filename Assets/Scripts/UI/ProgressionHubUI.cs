using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressionHubUI : MonoBehaviour
{
    private class DayCellRef
    {
        public Image background;
        public TextMeshProUGUI label;
    }

    public static ProgressionHubUI Instance { get; private set; }

    // Optional future skin assets (Resources) for the startup popup.
    private const string POPUP_BG_SPRITE_RESOURCE = "UI/Streak/PopupBackground";
    private const string POPUP_FRAME_SPRITE_RESOURCE = "UI/Streak/PopupFrame";
    private const string POPUP_ICON_SPRITE_RESOURCE = "UI/Streak/PopupIcon";

    private const float POPUP_SHOW_SECONDS = 3.4f;
    private const float POPUP_FADE_SECONDS = 0.24f;
    private const float POPUP_SCALE_IN_START = 0.92f;
    private const float POPUP_SCALE_IN_END = 1f;
    private const float POPUP_SCALE_OUT_END = 0.96f;

    private static readonly Color PopupCardFallbackColor = new Color(0.05f, 0.09f, 0.17f, 0.97f);
    private static readonly Color PopupAccentColor = new Color(0.25f, 0.78f, 0.95f, 1f);
    private static readonly Color PopupDoneColor = new Color(0.2f, 0.75f, 0.42f, 1f);
    private static readonly Color PopupCurrentColor = new Color(1f, 0.82f, 0.28f, 1f);
    private static readonly Color PopupIdleColor = new Color(0.28f, 0.34f, 0.46f, 0.95f);

    private GameObject _panel;
    private TextMeshProUGUI _streakBody;

    private GameObject _startupPopup;
    private RectTransform _startupPopupRect;
    private CanvasGroup _startupPopupCanvasGroup;
    private Image _startupPopupBackground;
    private TextMeshProUGUI _startupPopupHeaderText;
    private TextMeshProUGUI _startupPopupText;
    private TextMeshProUGUI _startupPopupSubText;
    private readonly List<DayCellRef> _dayCells = new List<DayCellRef>();

    private Coroutine _popupCoroutine;

    private bool _skinLoaded;
    private Sprite _popupBgSprite;
    private Sprite _popupFrameSprite;
    private Sprite _popupIconSprite;

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
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        BuildUI(canvas.transform);
        SubscribeEvents();
        RefreshStreak();
        Hide();
        StartCoroutine(ShowStartupPopupWhenReady(canvas.transform));
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
            RefreshStreak();
        }
    }

    public void Hide()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    private void BuildUI(Transform parent)
    {
        if (_built)
            return;

        _panel = new GameObject("ProgressionHubPanel", typeof(RectTransform));
        _panel.transform.SetParent(parent, false);

        Image overlay = _panel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.74f);

        RectTransform panelRt = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        GameObject windowObj = new GameObject("Window", typeof(RectTransform));
        windowObj.transform.SetParent(_panel.transform, false);
        Image windowBg = windowObj.AddComponent<Image>();
        windowBg.color = new Color(0.08f, 0.11f, 0.18f, 0.98f);

        RectTransform windowRt = windowObj.GetComponent<RectTransform>();
        windowRt.anchorMin = new Vector2(0.08f, 0.08f);
        windowRt.anchorMax = new Vector2(0.92f, 0.92f);
        windowRt.offsetMin = Vector2.zero;
        windowRt.offsetMax = Vector2.zero;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(windowObj.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "ILERLEME MERKEZI";
        title.fontSize = 34;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.92f, 0.98f, 1f);

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -14f);
        titleRt.sizeDelta = new Vector2(0f, 46f);

        GameObject subtitleObj = new GameObject("Subtitle", typeof(RectTransform));
        subtitleObj.transform.SetParent(windowObj.transform, false);
        TextMeshProUGUI subtitle = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitle.text = "Sadece streak serisini takip etmek icin tasarlandi.";
        subtitle.fontSize = 16;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = new Color(0.74f, 0.88f, 1f);

        RectTransform subtitleRt = subtitleObj.GetComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0f, 1f);
        subtitleRt.anchorMax = new Vector2(1f, 1f);
        subtitleRt.pivot = new Vector2(0.5f, 1f);
        subtitleRt.anchoredPosition = new Vector2(0f, -52f);
        subtitleRt.sizeDelta = new Vector2(0f, 26f);

        GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform));
        closeObj.transform.SetParent(windowObj.transform, false);
        Image closeBg = closeObj.AddComponent<Image>();
        closeBg.color = new Color(0.85f, 0.25f, 0.25f, 1f);
        Button closeBtn = closeObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(Hide);

        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-12f, -12f);
        closeRt.sizeDelta = new Vector2(40f, 40f);

        GameObject closeTextObj = new GameObject("Text", typeof(RectTransform));
        closeTextObj.transform.SetParent(closeObj.transform, false);
        TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.fontSize = 24;
        closeText.fontStyle = FontStyles.Bold;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;

        RectTransform closeTextRt = closeTextObj.GetComponent<RectTransform>();
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        closeTextRt.offsetMin = Vector2.zero;
        closeTextRt.offsetMax = Vector2.zero;

        GameObject card = new GameObject("StreakCard", typeof(RectTransform));
        card.transform.SetParent(windowObj.transform, false);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.19f, 0.28f, 1f);

        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.04f, 0.08f);
        cardRt.anchorMax = new Vector2(0.96f, 0.84f);
        cardRt.offsetMin = Vector2.zero;
        cardRt.offsetMax = Vector2.zero;

        GameObject cardTitleObj = new GameObject("CardTitle", typeof(RectTransform));
        cardTitleObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI cardTitle = cardTitleObj.AddComponent<TextMeshProUGUI>();
        cardTitle.text = "STREAK SERISI";
        cardTitle.fontSize = 28;
        cardTitle.fontStyle = FontStyles.Bold;
        cardTitle.alignment = TextAlignmentOptions.Center;
        cardTitle.color = new Color(0.94f, 0.98f, 1f);

        RectTransform ctRt = cardTitleObj.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0f, 1f);
        ctRt.anchorMax = new Vector2(1f, 1f);
        ctRt.pivot = new Vector2(0.5f, 1f);
        ctRt.anchoredPosition = new Vector2(0f, -16f);
        ctRt.sizeDelta = new Vector2(0f, 42f);

        GameObject bodyObj = new GameObject("Body", typeof(RectTransform));
        bodyObj.transform.SetParent(card.transform, false);
        _streakBody = bodyObj.AddComponent<TextMeshProUGUI>();
        _streakBody.fontSize = 22;
        _streakBody.alignment = TextAlignmentOptions.Center;
        _streakBody.color = new Color(0.86f, 0.92f, 1f);
        _streakBody.text = "Yukleniyor...";

        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(18f, 18f);
        bodyRt.offsetMax = new Vector2(-18f, -64f);

        _built = true;
    }

    private void SubscribeEvents()
    {
        if (StreakRewardManager.Instance != null)
        {
            StreakRewardManager.Instance.OnStreakChanged += RefreshStreak;
        }
    }

    private void UnsubscribeEvents()
    {
        if (StreakRewardManager.Instance != null)
        {
            StreakRewardManager.Instance.OnStreakChanged -= RefreshStreak;
        }
    }

    private void RefreshStreak()
    {
        if (_streakBody == null)
            return;

        if (StreakRewardManager.Instance == null)
        {
            _streakBody.text = "Streak sistemi hazir degil.";
            return;
        }

        int streak = Mathf.Max(0, StreakRewardManager.Instance.CurrentStreakDays);
        int cycleDay = streak > 0 ? ((streak - 1) % 7) + 1 : 0;

        int nextMilestone = GetNextMilestone(streak);
        string milestoneText = nextMilestone > 0
            ? (nextMilestone + ". gun odulu")
            : "Tum milestone odulleri acildi";

        _streakBody.text =
            "7 gunluk seride bugun: " + cycleDay + "/7\n" +
            "Kesintisiz toplam seri: " + streak + " gun\n" +
            "Sonraki hedef: " + milestoneText + "\n" +
            "Milestone: 7 / 14 / 30";

        RefreshStartupPopupText();
    }

    private int GetNextMilestone(int streakDays)
    {
        if (streakDays < 7) return 7;
        if (streakDays < 14) return 14;
        if (streakDays < 30) return 30;
        return -1;
    }

    private IEnumerator ShowStartupPopupWhenReady(Transform parent)
    {
        float timeout = 2f;
        while (StreakRewardManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (StreakRewardManager.Instance == null)
        {
            yield break;
        }

        BuildStartupPopup(parent);
        RefreshStartupPopupText();

        if (_popupCoroutine != null)
        {
            StopCoroutine(_popupCoroutine);
        }

        _popupCoroutine = StartCoroutine(AnimateStartupPopup());
    }

    private void BuildStartupPopup(Transform parent)
    {
        if (_startupPopup != null)
        {
            return;
        }

        TryLoadPopupSkinAssets();

        _startupPopup = new GameObject("StreakStartupPopup", typeof(RectTransform));
        _startupPopup.transform.SetParent(parent, false);

        _startupPopupRect = _startupPopup.GetComponent<RectTransform>();
        _startupPopupRect.anchorMin = new Vector2(0.5f, 0.5f);
        _startupPopupRect.anchorMax = new Vector2(0.5f, 0.5f);
        _startupPopupRect.pivot = new Vector2(0.5f, 0.5f);
        _startupPopupRect.anchoredPosition = Vector2.zero;
        _startupPopupRect.sizeDelta = new Vector2(620f, 260f);

        _startupPopupCanvasGroup = _startupPopup.AddComponent<CanvasGroup>();
        _startupPopupCanvasGroup.alpha = 0f;
        _startupPopupCanvasGroup.interactable = false;
        _startupPopupCanvasGroup.blocksRaycasts = false;

        _startupPopupBackground = _startupPopup.AddComponent<Image>();
        _startupPopupBackground.color = PopupCardFallbackColor;
        ApplyOptionalBackgroundSprite(_startupPopupBackground, _popupBgSprite, PopupCardFallbackColor);

        GameObject frameObj = new GameObject("Frame", typeof(RectTransform));
        frameObj.transform.SetParent(_startupPopup.transform, false);
        Image frameImage = frameObj.AddComponent<Image>();
        frameImage.color = new Color(PopupAccentColor.r, PopupAccentColor.g, PopupAccentColor.b, 0.9f);
        ApplyOptionalBackgroundSprite(frameImage, _popupFrameSprite, frameImage.color);

        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = Vector2.zero;
        frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = new Vector2(6f, 6f);
        frameRt.offsetMax = new Vector2(-6f, -6f);

        GameObject glowObj = new GameObject("TopGlow", typeof(RectTransform));
        glowObj.transform.SetParent(_startupPopup.transform, false);
        Image glowImage = glowObj.AddComponent<Image>();
        glowImage.color = new Color(PopupAccentColor.r, PopupAccentColor.g, PopupAccentColor.b, 0.24f);

        RectTransform glowRt = glowObj.GetComponent<RectTransform>();
        glowRt.anchorMin = new Vector2(0f, 1f);
        glowRt.anchorMax = new Vector2(1f, 1f);
        glowRt.pivot = new Vector2(0.5f, 1f);
        glowRt.anchoredPosition = Vector2.zero;
        glowRt.sizeDelta = new Vector2(0f, 70f);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
        iconObj.transform.SetParent(_startupPopup.transform, false);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = _popupIconSprite != null ? Color.white : PopupCurrentColor;
        if (_popupIconSprite != null)
        {
            iconImage.sprite = _popupIconSprite;
            iconImage.preserveAspect = true;
        }

        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 1f);
        iconRt.anchorMax = new Vector2(0f, 1f);
        iconRt.pivot = new Vector2(0f, 1f);
        iconRt.anchoredPosition = new Vector2(24f, -22f);
        iconRt.sizeDelta = new Vector2(44f, 44f);

        GameObject headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(_startupPopup.transform, false);
        _startupPopupHeaderText = headerObj.AddComponent<TextMeshProUGUI>();
        _startupPopupHeaderText.text = "DAILY STREAK";
        _startupPopupHeaderText.fontSize = 24;
        _startupPopupHeaderText.fontStyle = FontStyles.Bold;
        _startupPopupHeaderText.alignment = TextAlignmentOptions.TopLeft;
        _startupPopupHeaderText.color = new Color(0.9f, 0.96f, 1f);

        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0f, 1f);
        headerRt.offsetMin = new Vector2(78f, -56f);
        headerRt.offsetMax = new Vector2(-20f, -14f);

        GameObject textObj = new GameObject("MainText", typeof(RectTransform));
        textObj.transform.SetParent(_startupPopup.transform, false);
        _startupPopupText = textObj.AddComponent<TextMeshProUGUI>();
        _startupPopupText.fontSize = 44;
        _startupPopupText.fontStyle = FontStyles.Bold;
        _startupPopupText.alignment = TextAlignmentOptions.Center;
        _startupPopupText.color = new Color(0.96f, 0.98f, 1f);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0.5f);
        textRt.anchorMax = new Vector2(1f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = new Vector2(0f, 10f);
        textRt.sizeDelta = new Vector2(-24f, 70f);

        GameObject subTextObj = new GameObject("SubText", typeof(RectTransform));
        subTextObj.transform.SetParent(_startupPopup.transform, false);
        _startupPopupSubText = subTextObj.AddComponent<TextMeshProUGUI>();
        _startupPopupSubText.fontSize = 18;
        _startupPopupSubText.alignment = TextAlignmentOptions.Center;
        _startupPopupSubText.color = new Color(0.74f, 0.88f, 1f);

        RectTransform subRt = subTextObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0.5f);
        subRt.anchorMax = new Vector2(1f, 0.5f);
        subRt.pivot = new Vector2(0.5f, 0.5f);
        subRt.anchoredPosition = new Vector2(0f, -34f);
        subRt.sizeDelta = new Vector2(-24f, 40f);

        GameObject dayRowObj = new GameObject("DayRow", typeof(RectTransform));
        dayRowObj.transform.SetParent(_startupPopup.transform, false);
        RectTransform dayRowRt = dayRowObj.GetComponent<RectTransform>();
        dayRowRt.anchorMin = new Vector2(0f, 0f);
        dayRowRt.anchorMax = new Vector2(1f, 0f);
        dayRowRt.pivot = new Vector2(0.5f, 0f);
        dayRowRt.anchoredPosition = new Vector2(0f, 16f);
        dayRowRt.sizeDelta = new Vector2(-34f, 50f);

        HorizontalLayoutGroup dayLayout = dayRowObj.AddComponent<HorizontalLayoutGroup>();
        dayLayout.spacing = 8f;
        dayLayout.childAlignment = TextAnchor.MiddleCenter;
        dayLayout.childControlWidth = true;
        dayLayout.childControlHeight = true;
        dayLayout.childForceExpandWidth = true;
        dayLayout.childForceExpandHeight = true;

        for (int i = 0; i < 7; i++)
        {
            CreateDayCell(dayRowObj.transform, i + 1);
        }
    }

    private void RefreshStartupPopupText()
    {
        if (_startupPopupText == null || _startupPopupSubText == null || StreakRewardManager.Instance == null)
        {
            return;
        }

        int streak = Mathf.Max(0, StreakRewardManager.Instance.CurrentStreakDays);
        int cycleDay = streak > 0 ? ((streak - 1) % 7) + 1 : 0;
        int nextMilestone = GetNextMilestone(streak);

        _startupPopupText.text = "STREAK " + cycleDay + "/7";

        if (nextMilestone > 0)
        {
            _startupPopupSubText.text = "Toplam seri " + streak + " gun  |  Sonraki odul " + nextMilestone + ". gun";
        }
        else
        {
            _startupPopupSubText.text = "Toplam seri " + streak + " gun  |  Tum milestone odulleri acildi";
        }

        UpdateDayCells(cycleDay);
    }

    private IEnumerator AnimateStartupPopup()
    {
        if (_startupPopup == null || _startupPopupCanvasGroup == null || _startupPopupRect == null)
        {
            _popupCoroutine = null;
            yield break;
        }

        _startupPopup.SetActive(true);
        _startupPopupCanvasGroup.alpha = 0f;
        _startupPopupRect.localScale = Vector3.one * POPUP_SCALE_IN_START;

        float t = 0f;
        while (t < POPUP_FADE_SECONDS)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / POPUP_FADE_SECONDS);
            _startupPopupCanvasGroup.alpha = p;
            float scale = Mathf.Lerp(POPUP_SCALE_IN_START, POPUP_SCALE_IN_END, p);
            _startupPopupRect.localScale = Vector3.one * scale;
            yield return null;
        }

        _startupPopupCanvasGroup.alpha = 1f;
        _startupPopupRect.localScale = Vector3.one * POPUP_SCALE_IN_END;

        float hold = POPUP_SHOW_SECONDS;
        while (hold > 0f)
        {
            hold -= Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        while (t < POPUP_FADE_SECONDS)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / POPUP_FADE_SECONDS);
            _startupPopupCanvasGroup.alpha = 1f - p;
            float scale = Mathf.Lerp(POPUP_SCALE_IN_END, POPUP_SCALE_OUT_END, p);
            _startupPopupRect.localScale = Vector3.one * scale;
            yield return null;
        }

        if (_startupPopup != null)
        {
            _startupPopup.SetActive(false);
        }

        _popupCoroutine = null;
    }

    private void CreateDayCell(Transform parent, int dayNumber)
    {
        GameObject cellObj = new GameObject("Day_" + dayNumber, typeof(RectTransform));
        cellObj.transform.SetParent(parent, false);

        Image bg = cellObj.AddComponent<Image>();
        bg.color = PopupIdleColor;

        LayoutElement le = cellObj.AddComponent<LayoutElement>();
        le.preferredWidth = 0f;
        le.preferredHeight = 40f;

        GameObject textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(cellObj.transform, false);
        TextMeshProUGUI label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = dayNumber.ToString();
        label.fontSize = 18;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.87f, 0.91f, 0.98f);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        _dayCells.Add(new DayCellRef
        {
            background = bg,
            label = label
        });
    }

    private void UpdateDayCells(int cycleDay)
    {
        for (int i = 0; i < _dayCells.Count; i++)
        {
            DayCellRef cell = _dayCells[i];
            if (cell == null || cell.background == null)
                continue;

            int day = i + 1;
            bool isCurrent = cycleDay > 0 && day == cycleDay;
            bool isPassed = cycleDay > 0 && day < cycleDay;

            if (isCurrent)
            {
                cell.background.color = PopupCurrentColor;
                if (cell.label != null) cell.label.color = new Color(0.15f, 0.13f, 0.08f);
            }
            else if (isPassed)
            {
                cell.background.color = PopupDoneColor;
                if (cell.label != null) cell.label.color = Color.white;
            }
            else
            {
                cell.background.color = PopupIdleColor;
                if (cell.label != null) cell.label.color = new Color(0.87f, 0.91f, 0.98f);
            }
        }
    }

    private void TryLoadPopupSkinAssets()
    {
        if (_skinLoaded)
            return;

        _skinLoaded = true;
        _popupBgSprite = Resources.Load<Sprite>(POPUP_BG_SPRITE_RESOURCE);
        _popupFrameSprite = Resources.Load<Sprite>(POPUP_FRAME_SPRITE_RESOURCE);
        _popupIconSprite = Resources.Load<Sprite>(POPUP_ICON_SPRITE_RESOURCE);
    }

    private void ApplyOptionalBackgroundSprite(Image target, Sprite sprite, Color fallbackColor)
    {
        if (target == null)
            return;

        if (sprite != null)
        {
            target.sprite = sprite;
            target.type = Image.Type.Sliced;
            target.color = Color.white;
            return;
        }

        target.color = fallbackColor;
    }
}
