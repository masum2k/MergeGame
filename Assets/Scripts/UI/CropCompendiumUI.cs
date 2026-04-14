using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CropCompendiumUI : MonoBehaviour
{
    public static CropCompendiumUI Instance { get; private set; }

    private class RowRef
    {
        public CropData crop;
        public Button button;
        public TextMeshProUGUI text;
    }

    private GameObject _panel;
    private RectTransform _listRoot;
    private readonly List<RowRef> _rows = new List<RowRef>();

    private TextMeshProUGUI _detailNameText;
    private TextMeshProUGUI _detailRateText;
    private TextMeshProUGUI _detailDescText;

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
        RebuildRows();
        Hide();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    public void Toggle()
    {
        if (!_built || _panel == null)
            return;

        bool next = !_panel.activeSelf;
        _panel.SetActive(next);
        if (next)
        {
            RebuildRows();
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

        _panel = new GameObject("CropCompendiumPanel", typeof(RectTransform));
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
        windowBg.color = new Color(0.08f, 0.13f, 0.2f, 0.98f);

        RectTransform windowRt = windowObj.GetComponent<RectTransform>();
        windowRt.anchorMin = new Vector2(0.07f, 0.08f);
        windowRt.anchorMax = new Vector2(0.93f, 0.92f);
        windowRt.offsetMin = Vector2.zero;
        windowRt.offsetMax = Vector2.zero;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(windowObj.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "TUM BESINLER";
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

        GameObject subObj = new GameObject("Subtitle", typeof(RectTransform));
        subObj.transform.SetParent(windowObj.transform, false);
        TextMeshProUGUI subtitle = subObj.AddComponent<TextMeshProUGUI>();
        subtitle.text = "Siralama: saniye basina coin (coktan aza). Sadece acilan besinler secilebilir.";
        subtitle.fontSize = 16;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = new Color(0.76f, 0.88f, 1f);

        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -54f);
        subRt.sizeDelta = new Vector2(0f, 28f);

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

        BuildListArea(windowObj.transform);
        BuildDetailArea(windowObj.transform);

        _built = true;
    }

    private void BuildListArea(Transform parent)
    {
        GameObject scrollObj = new GameObject("CropScroll", typeof(RectTransform));
        scrollObj.transform.SetParent(parent, false);

        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.02f, 0.06f);
        scrollRt.anchorMax = new Vector2(0.62f, 0.86f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

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
        _listRoot = contentObj.GetComponent<RectTransform>();
        _listRoot.anchorMin = new Vector2(0f, 1f);
        _listRoot.anchorMax = new Vector2(1f, 1f);
        _listRoot.pivot = new Vector2(0.5f, 1f);
        _listRoot.anchoredPosition = Vector2.zero;
        _listRoot.sizeDelta = Vector2.zero;
        scroll.content = _listRoot;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void BuildDetailArea(Transform parent)
    {
        GameObject detailObj = new GameObject("Detail", typeof(RectTransform));
        detailObj.transform.SetParent(parent, false);
        Image detailBg = detailObj.AddComponent<Image>();
        detailBg.color = new Color(0.12f, 0.18f, 0.28f, 1f);

        RectTransform detailRt = detailObj.GetComponent<RectTransform>();
        detailRt.anchorMin = new Vector2(0.64f, 0.06f);
        detailRt.anchorMax = new Vector2(0.98f, 0.86f);
        detailRt.offsetMin = Vector2.zero;
        detailRt.offsetMax = Vector2.zero;

        GameObject nameObj = new GameObject("Name", typeof(RectTransform));
        nameObj.transform.SetParent(detailObj.transform, false);
        _detailNameText = nameObj.AddComponent<TextMeshProUGUI>();
        _detailNameText.fontSize = 26;
        _detailNameText.fontStyle = FontStyles.Bold;
        _detailNameText.alignment = TextAlignmentOptions.Top;
        _detailNameText.color = new Color(0.96f, 0.98f, 1f);

        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -14f);
        nameRt.sizeDelta = new Vector2(-18f, 48f);

        GameObject rateObj = new GameObject("Rate", typeof(RectTransform));
        rateObj.transform.SetParent(detailObj.transform, false);
        _detailRateText = rateObj.AddComponent<TextMeshProUGUI>();
        _detailRateText.fontSize = 18;
        _detailRateText.fontStyle = FontStyles.Bold;
        _detailRateText.alignment = TextAlignmentOptions.TopLeft;
        _detailRateText.color = new Color(0.92f, 0.9f, 0.58f);

        RectTransform rateRt = rateObj.GetComponent<RectTransform>();
        rateRt.anchorMin = new Vector2(0f, 1f);
        rateRt.anchorMax = new Vector2(1f, 1f);
        rateRt.pivot = new Vector2(0.5f, 1f);
        rateRt.anchoredPosition = new Vector2(0f, -62f);
        rateRt.sizeDelta = new Vector2(-18f, 70f);

        GameObject descObj = new GameObject("Description", typeof(RectTransform));
        descObj.transform.SetParent(detailObj.transform, false);
        _detailDescText = descObj.AddComponent<TextMeshProUGUI>();
        _detailDescText.fontSize = 16;
        _detailDescText.alignment = TextAlignmentOptions.TopLeft;
        _detailDescText.color = new Color(0.84f, 0.92f, 1f);

        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 0f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.offsetMin = new Vector2(10f, 10f);
        descRt.offsetMax = new Vector2(-10f, -140f);

        ShowLockedPlaceholder();
    }

    private void RebuildRows()
    {
        if (!_built || _listRoot == null)
            return;

        for (int i = _listRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_listRoot.GetChild(i).gameObject);
        }
        _rows.Clear();

        if (GameContentGenerator.Instance == null || GameContentGenerator.Instance.AllCrops == null)
        {
            ShowLockedPlaceholder();
            return;
        }

        List<CropData> crops = new List<CropData>(GameContentGenerator.Instance.AllCrops);
        crops.RemoveAll(c => c == null);
        crops.Sort((a, b) =>
        {
            int cmp = b.coinPerTick.CompareTo(a.coinPerTick);
            if (cmp != 0) return cmp;
            return b.tier.CompareTo(a.tier);
        });

        for (int i = 0; i < crops.Count; i++)
        {
            CreateRow(crops[i], i + 1);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_listRoot);
    }

    private void CreateRow(CropData crop, int rank)
    {
        GameObject rowObj = new GameObject("Row_" + crop.itemName, typeof(RectTransform));
        rowObj.transform.SetParent(_listRoot, false);

        Image rowBg = rowObj.AddComponent<Image>();
        bool unlocked = IsCropSelectable(crop);
        rowBg.color = unlocked
            ? new Color(0.2f, 0.28f, 0.4f, 1f)
            : new Color(0.2f, 0.2f, 0.24f, 0.95f);

        RectTransform rowRt = rowObj.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0f, 52f);

        Button btn = rowObj.AddComponent<Button>();
        btn.targetGraphic = rowBg;
        btn.interactable = unlocked;
        btn.onClick.AddListener(() => ShowCropDetail(crop));

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(rowObj.transform, false);
        TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();

        string status = unlocked ? "ACIK" : "KILITLI";
        txt.text = rank + ". " + crop.itemName + "  |  " + crop.coinPerTick.ToString("0.##") + " coin/sn  |  " + status;
        txt.fontSize = 16;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.color = unlocked ? new Color(0.9f, 0.96f, 1f) : new Color(0.62f, 0.66f, 0.72f);

        RectTransform txtRt = textObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(10f, 0f);
        txtRt.offsetMax = new Vector2(-10f, 0f);

        _rows.Add(new RowRef
        {
            crop = crop,
            button = btn,
            text = txt
        });
    }

    private void ShowCropDetail(CropData crop)
    {
        if (crop == null)
            return;

        float perSecond = crop.coinPerTick;
        float perMinute = crop.coinPerTick * 60f;
        int tierNo = (int)crop.tier + 1;

        _detailNameText.text = crop.itemName + "  (T" + tierNo + ")";
        _detailRateText.text =
            "Coin/sn: " + perSecond.ToString("0.##") + "\n" +
            "Coin/dk: " + perMinute.ToString("0.##");

        _detailDescText.text =
            "Ne bu?\n" +
            BuildFlavorText(crop) + "\n\n" +
            "Not: Bu besin sandiklardan acildiginda secilebilir hale gelir.";
    }

    private void ShowLockedPlaceholder()
    {
        if (_detailNameText == null || _detailRateText == null || _detailDescText == null)
            return;

        _detailNameText.text = "Besin sec";
        _detailRateText.text = "Coin/sn: -\nCoin/dk: -";
        _detailDescText.text = "Kilitli besinler secilemez. Once sandik acip kilit ac.";
    }

    private bool IsCropSelectable(CropData crop)
    {
        if (crop == null)
            return false;

        if (CrateManager.Instance != null && CrateManager.Instance.IsCropUnlocked(crop.itemName))
            return true;

        return InventoryManager.Instance != null && InventoryManager.Instance.GetCount(crop.itemName) > 0;
    }

    private string BuildFlavorText(CropData crop)
    {
        int key = Mathf.Abs((crop.itemName.GetHashCode() * 37) + ((int)crop.tier * 97));

        string[] lines =
        {
            "Tarlanin gizli yildizi: markette gorunce herkes ikinci kez bakar.",
            "Bu urun topragi seviyor, ama oyuncunun stratejisini daha cok seviyor.",
            "Komik ama gercek: depoda durdukca moral veriyor, satildikca ekonomiyi ucuruyor.",
            "Sessiz gorunur ama coin sayacini maraton kosucusu gibi ileri tasir.",
            "Hasat zamani geldiginde kasadaki rakamlar kendiliginden gulumser.",
            "Bazi oyuncular buna 'coin motoru' diyor, haksiz da sayilmazlar."
        };

        return lines[key % lines.Length];
    }

    private void SubscribeEvents()
    {
        if (CrateManager.Instance != null)
        {
            CrateManager.Instance.OnCrateOpened += HandleCrateOpened;
            CrateManager.Instance.OnUnlockedCropsChanged += HandleUnlocksChanged;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (CrateManager.Instance != null)
        {
            CrateManager.Instance.OnCrateOpened -= HandleCrateOpened;
            CrateManager.Instance.OnUnlockedCropsChanged -= HandleUnlocksChanged;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleCrateOpened(object _)
    {
        if (_panel != null && _panel.activeSelf)
        {
            RebuildRows();
        }
    }

    private void HandleUnlocksChanged()
    {
        if (_panel != null && _panel.activeSelf)
        {
            RebuildRows();
        }
    }

    private void HandleInventoryChanged()
    {
        if (_panel != null && _panel.activeSelf)
        {
            RebuildRows();
        }
    }
}