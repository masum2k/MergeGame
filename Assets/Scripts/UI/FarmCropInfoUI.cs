using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FarmCropInfoUI : MonoBehaviour
{
    public static FarmCropInfoUI Instance { get; private set; }

    private GameObject _panel;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _rateText;
    private TextMeshProUGUI _descText;
    private TextMeshProUGUI _metaText;
    private Image _iconImage;
    private Button _sendToInventoryButton;

    private GridSlot _selectedSlot;

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
        Hide();
    }

    public void Show(GridSlot slot)
    {
        if (slot == null || slot.IsLocked || slot.IsEmpty || slot.CurrentCrop == null)
        {
            Hide();
            return;
        }

        CropData crop = slot.CurrentCrop;
        if (crop == null)
            return;

        if (!_built)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            BuildUI(canvas.transform);
        }

        int tierNo = (int)crop.tier + 1;
        float perSecond = Mathf.Max(0f, crop.coinPerTick);
        float perMinute = perSecond * 60f;

        _selectedSlot = slot;

        _nameText.text = crop.itemName + "  (T" + tierNo + ")";
        _rateText.text = "Coin/sn: " + perSecond.ToString("0.##") + "\nCoin/dk: " + perMinute.ToString("0.##");
        _descText.text = GetDescription(crop);

        string nextName = crop.nextLevelCrop != null ? crop.nextLevelCrop.itemName : "Max tier";
        _metaText.text = "Slot: (" + slot.X + ", " + slot.Y + ")\nSonraki: " + nextName;

        if (_iconImage != null)
        {
            _iconImage.sprite = crop.cropSprite;
            _iconImage.color = IsCompositeCropSprite(crop.cropSprite) ? Color.white : crop.cropColor;
        }

        _panel.SetActive(true);
    }

    public void Hide()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }

        _selectedSlot = null;
    }

    private void BuildUI(Transform parent)
    {
        if (_built)
            return;

        _panel = new GameObject("FarmCropInfoPanel", typeof(RectTransform));
        _panel.transform.SetParent(parent, false);

        Image bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.12f, 0.2f, 0.96f);

        RectTransform panelRt = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(460f, 280f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(_panel.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "BESIN BILGISI";
        title.fontSize = 24;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Left;
        title.color = new Color(0.94f, 0.98f, 1f);

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.offsetMin = new Vector2(14f, -40f);
        titleRt.offsetMax = new Vector2(-48f, -8f);

        GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform));
        closeObj.transform.SetParent(_panel.transform, false);
        Image closeBg = closeObj.AddComponent<Image>();
        closeBg.color = new Color(0.82f, 0.24f, 0.24f, 1f);
        Button closeBtn = closeObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(Hide);

        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-10f, -10f);
        closeRt.sizeDelta = new Vector2(32f, 32f);

        GameObject closeTextObj = new GameObject("Text", typeof(RectTransform));
        closeTextObj.transform.SetParent(closeObj.transform, false);
        TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.fontSize = 18;
        closeText.fontStyle = FontStyles.Bold;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;

        RectTransform closeTextRt = closeTextObj.GetComponent<RectTransform>();
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        closeTextRt.offsetMin = Vector2.zero;
        closeTextRt.offsetMax = Vector2.zero;

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
        iconObj.transform.SetParent(_panel.transform, false);
        _iconImage = iconObj.AddComponent<Image>();
        _iconImage.color = Color.white;
        _iconImage.preserveAspect = true;

        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 1f);
        iconRt.anchorMax = new Vector2(0f, 1f);
        iconRt.pivot = new Vector2(0f, 1f);
        iconRt.anchoredPosition = new Vector2(14f, -52f);
        iconRt.sizeDelta = new Vector2(68f, 68f);

        GameObject nameObj = new GameObject("Name", typeof(RectTransform));
        nameObj.transform.SetParent(_panel.transform, false);
        _nameText = nameObj.AddComponent<TextMeshProUGUI>();
        _nameText.fontSize = 22;
        _nameText.fontStyle = FontStyles.Bold;
        _nameText.alignment = TextAlignmentOptions.Left;
        _nameText.color = new Color(0.92f, 0.97f, 1f);

        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.offsetMin = new Vector2(90f, -84f);
        nameRt.offsetMax = new Vector2(-12f, -52f);

        GameObject rateObj = new GameObject("Rate", typeof(RectTransform));
        rateObj.transform.SetParent(_panel.transform, false);
        _rateText = rateObj.AddComponent<TextMeshProUGUI>();
        _rateText.fontSize = 18;
        _rateText.fontStyle = FontStyles.Bold;
        _rateText.alignment = TextAlignmentOptions.Left;
        _rateText.color = new Color(0.95f, 0.9f, 0.58f);

        RectTransform rateRt = rateObj.GetComponent<RectTransform>();
        rateRt.anchorMin = new Vector2(0f, 1f);
        rateRt.anchorMax = new Vector2(1f, 1f);
        rateRt.pivot = new Vector2(0f, 1f);
        rateRt.offsetMin = new Vector2(90f, -142f);
        rateRt.offsetMax = new Vector2(-12f, -86f);

        GameObject descObj = new GameObject("Description", typeof(RectTransform));
        descObj.transform.SetParent(_panel.transform, false);
        _descText = descObj.AddComponent<TextMeshProUGUI>();
        _descText.fontSize = 16;
        _descText.alignment = TextAlignmentOptions.TopLeft;
        _descText.color = new Color(0.82f, 0.9f, 1f);

        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 0f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.offsetMin = new Vector2(14f, 96f);
        descRt.offsetMax = new Vector2(-12f, -152f);

        GameObject metaObj = new GameObject("Meta", typeof(RectTransform));
        metaObj.transform.SetParent(_panel.transform, false);
        _metaText = metaObj.AddComponent<TextMeshProUGUI>();
        _metaText.fontSize = 14;
        _metaText.alignment = TextAlignmentOptions.BottomLeft;
        _metaText.color = new Color(0.7f, 0.82f, 0.96f);

        RectTransform metaRt = metaObj.GetComponent<RectTransform>();
        metaRt.anchorMin = new Vector2(0f, 0f);
        metaRt.anchorMax = new Vector2(1f, 0f);
        metaRt.pivot = new Vector2(0f, 0f);
        metaRt.offsetMin = new Vector2(14f, 52f);
        metaRt.offsetMax = new Vector2(-12f, 92f);

        GameObject sendBtnObj = new GameObject("SendToInventoryButton", typeof(RectTransform));
        sendBtnObj.transform.SetParent(_panel.transform, false);
        Image sendBtnBg = sendBtnObj.AddComponent<Image>();
        sendBtnBg.color = new Color(0.2f, 0.52f, 0.3f, 1f);
        _sendToInventoryButton = sendBtnObj.AddComponent<Button>();
        _sendToInventoryButton.onClick.AddListener(OnSendToInventoryClicked);

        RectTransform sendBtnRt = sendBtnObj.GetComponent<RectTransform>();
        sendBtnRt.anchorMin = new Vector2(0.5f, 0f);
        sendBtnRt.anchorMax = new Vector2(0.5f, 0f);
        sendBtnRt.pivot = new Vector2(0.5f, 0f);
        sendBtnRt.anchoredPosition = new Vector2(0f, 10f);
        sendBtnRt.sizeDelta = new Vector2(230f, 34f);

        GameObject sendBtnTextObj = new GameObject("Text", typeof(RectTransform));
        sendBtnTextObj.transform.SetParent(sendBtnObj.transform, false);
        TextMeshProUGUI sendBtnText = sendBtnTextObj.AddComponent<TextMeshProUGUI>();
        sendBtnText.text = "Envantere Geri Koy";
        sendBtnText.fontSize = 16;
        sendBtnText.fontStyle = FontStyles.Bold;
        sendBtnText.alignment = TextAlignmentOptions.Center;
        sendBtnText.color = Color.white;

        RectTransform sendBtnTextRt = sendBtnTextObj.GetComponent<RectTransform>();
        sendBtnTextRt.anchorMin = Vector2.zero;
        sendBtnTextRt.anchorMax = Vector2.one;
        sendBtnTextRt.offsetMin = Vector2.zero;
        sendBtnTextRt.offsetMax = Vector2.zero;

        _built = true;
    }

    private void OnSendToInventoryClicked()
    {
        if (_selectedSlot == null || _selectedSlot.IsLocked || _selectedSlot.IsEmpty || _selectedSlot.CurrentCrop == null)
        {
            Hide();
            return;
        }

        if (InventoryManager.Instance == null)
            return;

        CropData crop = _selectedSlot.CurrentCrop;
        InventoryManager.Instance.AddItem(crop);
        _selectedSlot.ClearSlot();

        if (GridManager.Instance != null)
        {
            GridManager.Instance.SaveGridState();
        }

        GameMessageManager.Instance?.PushMessage(crop.itemName + " envantere geri koyuldu.");
        Hide();
    }

    private string GetDescription(CropData crop)
    {
        if (!string.IsNullOrWhiteSpace(crop.description))
            return crop.description;

        int tierNo = (int)crop.tier + 1;
        return crop.itemName + " T" + tierNo + " besinidir. Pasif coin akisini destekler.";
    }

    private static bool IsCompositeCropSprite(Sprite sprite)
    {
        return sprite != null
            && sprite.name.StartsWith("spr_crop_", System.StringComparison.OrdinalIgnoreCase);
    }
}
