using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenCarouselUI : MonoBehaviour
{
    private enum ScreenId
    {
        Market = 0,
        Farm = 1,
        Factory = 2,
        SkillTree = 3
    }

    [Header("Swipe")]
    [SerializeField] private float transitionDuration = 0.32f;
    [SerializeField] private float dragStartDeadzone = 14f;
    [SerializeField, Range(0.1f, 0.9f)] private float completeThresholdNormalized = 0.30f;

    private readonly string[] _screenTitles = { "Market", "Tarla", "Fabrika", "Yetenek Agaci" };

    private RectTransform _root;
    private RectTransform[] _pages;

    private int _currentIndex = (int)ScreenId.Farm;
    private bool _isAnimating;

    private bool _pointerDown;
    private bool _isDraggingPages;
    private bool _gestureLockedVertical;
    private Vector2 _pressStart;
    private float _dragOffsetX;

    private float _cachedPageWidth;
    private UIManager _uiManager;
    private Coroutine _snapCoroutine;
    private Camera _mainCamera;
    private Vector3 _baseCameraPosition;
    private bool _cameraBaseCached;
    private bool _clickButtonAttached;
    private RectTransform _bottomNavRoot;
    private readonly Image[] _bottomNavButtonBackgrounds = new Image[4];
    private readonly TMPro.TextMeshProUGUI[] _bottomNavButtonLabels = new TMPro.TextMeshProUGUI[4];
    private Sprite _bottomNavMenuSprite;

    private readonly Color _bottomNavActiveColor = new Color(0.24f, 0.56f, 0.96f, 1f);
    private readonly Color _bottomNavIdleColor = new Color(0.22f, 0.24f, 0.32f, 0.94f);
    private readonly Color _bottomNavLabelActiveColor = Color.white;
    private readonly Color _bottomNavLabelIdleColor = new Color(1f, 1f, 1f, 0.94f);
    private const string BottomNavMenuResourcePath = "BottomNav/ALTMENU-Full-Cropped";
    private const string FarmAllCropsButtonResourcePath = "Farm/ANAMENU-TumBesinler";
    private const float BottomNavHeight = 244f;
    private const float BottomNavEdgePadding = 36f;
    private const float BottomNavSlotGap = 12f;
    private const float BottomNavSlotVerticalPadding = 16f;
    private const float BottomNavLabelFontSize = 25f;
    private const float BottomNavLabelFontSizeMin = 15f;
    private const float BottomNavLabelOutlineWidth = 0.14f;
    private const float BottomNavLabelBottomOffset = -2f;
    private const float BottomNavLabelHeight = 54f;
    private const float BottomNavLabelEdgeNudge = 10f;
    private Sprite _farmAllCropsButtonSprite;

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        _uiManager = FindAnyObjectByType<UIManager>();
        _mainCamera = Camera.main;
        CacheCameraBaseIfNeeded();
        LoadBottomNavIcons();
        LoadFarmOverlaySprites();

        BuildRoot(canvas.transform);
        BuildBottomNavigation(canvas.transform);
        BuildPages();
        SetupIdlePositions();
        ApplyScreenState();
        TryAttachClickButtonToFarmPage();
    }

    private void Update()
    {
        if (_root == null)
            return;

        if (DragHandler.IsAnyCropDragging)
        {
            CancelGestureForCropDrag();
            return;
        }

        if (!_clickButtonAttached)
        {
            TryAttachClickButtonToFarmPage();
        }

        float pageWidth = GetPageWidth();
        if (!Mathf.Approximately(_cachedPageWidth, pageWidth) && !_isDraggingPages && !_isAnimating)
        {
            _cachedPageWidth = pageWidth;
            SetupIdlePositions();
        }

        if (_isAnimating)
            return;

        HandlePointerInput();
    }

    private void CancelGestureForCropDrag()
    {
        _pointerDown = false;
        _gestureLockedVertical = false;

        if (_snapCoroutine != null)
        {
            StopCoroutine(_snapCoroutine);
            _snapCoroutine = null;
            _isAnimating = false;
        }

        if (_isDraggingPages)
        {
            _isDraggingPages = false;
        }

        _dragOffsetX = 0f;
        SetupIdlePositions();
        SyncFarmWorldToFarmPage();
    }

    private void BuildRoot(Transform parent)
    {
        if (_root != null)
            return;

        GameObject rootObj = new GameObject("ScreenCarouselRoot", typeof(RectTransform));
        rootObj.transform.SetParent(parent, false);
        _root = rootObj.GetComponent<RectTransform>();

        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = new Vector2(0f, -90f); // Leave room for top HUD bar.

        // Keep pages under top bar and above world objects.
        _root.SetAsFirstSibling();
    }

    private void BuildBottomNavigation(Transform parent)
    {
        if (_bottomNavRoot != null)
            return;

        GameObject navObj = new GameObject("BottomNavigationPane", typeof(RectTransform));
        navObj.transform.SetParent(parent, false);
        _bottomNavRoot = navObj.GetComponent<RectTransform>();

        _bottomNavRoot.anchorMin = new Vector2(0f, 0f);
        _bottomNavRoot.anchorMax = new Vector2(1f, 0f);
        _bottomNavRoot.pivot = new Vector2(0.5f, 0f);
        _bottomNavRoot.anchoredPosition = Vector2.zero;
        _bottomNavRoot.sizeDelta = new Vector2(0f, BottomNavHeight);

        Image navBg = navObj.AddComponent<Image>();
        if (_bottomNavMenuSprite != null)
        {
            navBg.sprite = _bottomNavMenuSprite;
            navBg.type = Image.Type.Simple;
            navBg.color = Color.white;
        }
        else
        {
            navBg.color = new Color(0.05f, 0.06f, 0.1f, 0.98f);
        }

        // Keep pane above page content but below pop-up overlays.
        _bottomNavRoot.SetSiblingIndex(1);

        GameObject rowObj = new GameObject("NavRow", typeof(RectTransform));
        rowObj.transform.SetParent(_bottomNavRoot, false);

        RectTransform rowRt = rowObj.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.offsetMin = new Vector2(0f, 0f);
        rowRt.offsetMax = new Vector2(0f, 0f);

        CreateBottomNavButton(rowObj.transform, "Market", (int)ScreenId.Market);
        CreateBottomNavButton(rowObj.transform, "Tarla", (int)ScreenId.Farm);
        CreateBottomNavButton(rowObj.transform, "Fabrika", (int)ScreenId.Factory);
        CreateBottomNavButton(rowObj.transform, "Yetenek", (int)ScreenId.SkillTree);

        UpdateBottomNavigationVisuals();
    }

    private void LoadBottomNavIcons()
    {
        _bottomNavMenuSprite = Resources.Load<Sprite>(BottomNavMenuResourcePath);
        if (_bottomNavMenuSprite == null)
        {
            Texture2D menuTexture = Resources.Load<Texture2D>(BottomNavMenuResourcePath);
            if (menuTexture != null)
            {
                _bottomNavMenuSprite = Sprite.Create(
                    menuTexture,
                    new Rect(0f, 0f, menuTexture.width, menuTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }
    }

    private void LoadFarmOverlaySprites()
    {
        _farmAllCropsButtonSprite = LoadSpriteResource(FarmAllCropsButtonResourcePath);
    }

    private static Sprite LoadSpriteResource(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private void CreateBottomNavButton(Transform parent, string label, int targetIndex)
    {
        GameObject btnObj = new GameObject("Nav_" + label, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        float minX = targetIndex / 4f;
        float maxX = (targetIndex + 1) / 4f;
        btnRt.anchorMin = new Vector2(minX, 0f);
        btnRt.anchorMax = new Vector2(maxX, 1f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        float halfGap = BottomNavSlotGap * 0.5f;
        float leftPad = targetIndex == 0 ? BottomNavEdgePadding : halfGap;
        float rightPad = targetIndex == 3 ? BottomNavEdgePadding : halfGap;
        btnRt.offsetMin = new Vector2(leftPad, BottomNavSlotVerticalPadding);
        btnRt.offsetMax = new Vector2(-rightPad, -BottomNavSlotVerticalPadding);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = _bottomNavMenuSprite != null ? Color.clear : _bottomNavIdleColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => OnBottomNavPressed(targetIndex));

        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(btnObj.transform, false);

        TMPro.TextMeshProUGUI labelText = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = BottomNavLabelFontSize;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = BottomNavLabelFontSizeMin;
        labelText.fontSizeMax = BottomNavLabelFontSize;
        labelText.fontStyle = TMPro.FontStyles.Bold;
        labelText.alignment = TMPro.TextAlignmentOptions.Center;
        labelText.enableWordWrapping = false;
        labelText.raycastTarget = false;
        labelText.color = _bottomNavLabelIdleColor;

        // Use a material instance so outline changes stay local to this label.
        if (labelText.fontSharedMaterial != null)
        {
            labelText.fontMaterial = new Material(labelText.fontSharedMaterial);
        }

        labelText.outlineColor = Color.black;
        labelText.outlineWidth = BottomNavLabelOutlineWidth;

        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        float labelOffsetX = 0f;
        if (targetIndex == (int)ScreenId.Market)
        {
            labelOffsetX = -BottomNavLabelEdgeNudge;
        }
        else if (targetIndex == (int)ScreenId.SkillTree)
        {
            labelOffsetX = BottomNavLabelEdgeNudge;
        }

        labelRt.anchoredPosition = new Vector2(labelOffsetX, BottomNavLabelBottomOffset);
        labelRt.sizeDelta = new Vector2(0f, BottomNavLabelHeight);

        _bottomNavButtonBackgrounds[targetIndex] = bg;
        _bottomNavButtonLabels[targetIndex] = labelText;
    }

    private void OnBottomNavPressed(int targetIndex)
    {
        if (_isAnimating || _pages == null || _pages.Length == 0)
            return;

        int safeTarget = Wrap(targetIndex);
        if (safeTarget == _currentIndex)
            return;

        if (_snapCoroutine != null)
        {
            StopCoroutine(_snapCoroutine);
            _snapCoroutine = null;
        }

        _pointerDown = false;
        _isDraggingPages = false;
        _gestureLockedVertical = false;
        _dragOffsetX = 0f;

        _snapCoroutine = StartCoroutine(NavigateToIndexRoutine(safeTarget));
    }

    private IEnumerator NavigateToIndexRoutine(int targetIndex)
    {
        _isAnimating = true;

        while (_currentIndex != targetIndex)
        {
            int direction = GetShortestStepDirection(_currentIndex, targetIndex);
            if (direction == 0)
                break;

            SetupIdlePositions();

            float width = GetPageWidth();
            float startOffset = 0f;
            float targetOffset = direction > 0 ? -width : width;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                t = Mathf.SmoothStep(0f, 1f, t);

                float currentOffset = Mathf.Lerp(startOffset, targetOffset, t);
                ApplyDragOffset(currentOffset);

                yield return null;
            }

            ApplyDragOffset(targetOffset);
            _currentIndex = Wrap(_currentIndex + direction);
            _dragOffsetX = 0f;

            SetupIdlePositions();
            ApplyScreenState();
        }

        _isAnimating = false;
        _snapCoroutine = null;
    }

    private int GetShortestStepDirection(int fromIndex, int toIndex)
    {
        int count = _pages != null && _pages.Length > 0 ? _pages.Length : 4;
        int forwardSteps = (toIndex - fromIndex + count) % count;
        int backwardSteps = (fromIndex - toIndex + count) % count;

        if (forwardSteps == 0)
            return 0;

        return forwardSteps <= backwardSteps ? 1 : -1;
    }

    private void UpdateBottomNavigationVisuals()
    {
        for (int i = 0; i < _bottomNavButtonBackgrounds.Length; i++)
        {
            Image bg = _bottomNavButtonBackgrounds[i];
            if (bg == null)
                continue;

            bool isActive = i == _currentIndex;
            if (_bottomNavMenuSprite != null)
            {
                bg.color = Color.clear;
            }
            else
            {
                bg.color = isActive ? _bottomNavActiveColor : _bottomNavIdleColor;
            }

            TMPro.TextMeshProUGUI label = _bottomNavButtonLabels[i];
            if (label != null)
            {
                label.color = isActive ? _bottomNavLabelActiveColor : _bottomNavLabelIdleColor;
            }
        }
    }

    private bool IsPointerOnBottomNavigation(Vector2 screenPosition)
    {
        return _bottomNavRoot != null && RectTransformUtility.RectangleContainsScreenPoint(_bottomNavRoot, screenPosition, null);
    }

    private void BuildPages()
    {
        _pages = new RectTransform[4];

        _pages[(int)ScreenId.Market] = CreatePage("Page_Market", true, new Color(0.05f, 0.08f, 0.12f, 0.96f));
        _pages[(int)ScreenId.Farm] = CreatePage("Page_Farm", false, Color.clear);
        _pages[(int)ScreenId.Factory] = CreatePage("Page_Factory", true, new Color(0.08f, 0.07f, 0.12f, 0.96f));
        _pages[(int)ScreenId.SkillTree] = CreatePage("Page_SkillTree", true, new Color(0.06f, 0.11f, 0.11f, 0.96f));

        _pages[(int)ScreenId.Market].gameObject.AddComponent<MarketScreenPage>();
        _pages[(int)ScreenId.Factory].gameObject.AddComponent<FactoryScreenPage>();
        _pages[(int)ScreenId.SkillTree].gameObject.AddComponent<SkillTreeScreenPage>();

        BuildFarmOverlay(_pages[(int)ScreenId.Farm]);
        SyncFarmWorldToFarmPage();
        TryAttachClickButtonToFarmPage();
    }

    private RectTransform CreatePage(string name, bool blocksInput, Color bgColor)
    {
        GameObject pageObj = new GameObject(name, typeof(RectTransform));
        pageObj.transform.SetParent(_root, false);

        RectTransform rt = pageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = pageObj.AddComponent<Image>();
        bg.color = bgColor;
        bg.raycastTarget = blocksInput;

        return rt;
    }

    private void BuildFarmOverlay(RectTransform farmPage)
    {
        GameObject panel = new GameObject("FarmHintPanel", typeof(RectTransform));
        panel.transform.SetParent(farmPage, false);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.03f, 0.04f, 0.06f, 0.58f);
        panelBg.raycastTarget = false;

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 16f);
        panelRt.sizeDelta = new Vector2(680f, 72f);

        GameObject textObj = new GameObject("HintText", typeof(RectTransform));
        textObj.transform.SetParent(panel.transform, false);
        TMPro.TextMeshProUGUI hint = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        hint.text = "Bos slota dokun: Envanter acilir. Slotlara urun yerlestirip birlestir.";
        hint.fontSize = 20;
        hint.alignment = TMPro.TextAlignmentOptions.Center;
        hint.color = new Color(0.86f, 0.94f, 1f);

        RectTransform hintRt = textObj.GetComponent<RectTransform>();
        hintRt.anchorMin = Vector2.zero;
        hintRt.anchorMax = Vector2.one;
        hintRt.offsetMin = Vector2.zero;
        hintRt.offsetMax = Vector2.zero;

        GameObject cropListBtnObj = new GameObject("AllCropsButton", typeof(RectTransform));
        cropListBtnObj.transform.SetParent(farmPage, false);
        Image cropListBtnBg = cropListBtnObj.AddComponent<Image>();
        if (_farmAllCropsButtonSprite != null)
        {
            cropListBtnBg.sprite = _farmAllCropsButtonSprite;
            cropListBtnBg.type = Image.Type.Simple;
            cropListBtnBg.preserveAspect = true;
            cropListBtnBg.color = Color.white;
        }
        else
        {
            cropListBtnBg.color = new Color(0.2f, 0.52f, 0.9f, 0.95f);
        }

        Button cropListBtn = cropListBtnObj.AddComponent<Button>();
        cropListBtn.targetGraphic = cropListBtnBg;
        cropListBtn.onClick.AddListener(() =>
        {
            if (CropCompendiumUI.Instance != null)
            {
                CropCompendiumUI.Instance.Toggle();
            }
        });

        RectTransform cropListBtnRt = cropListBtnObj.GetComponent<RectTransform>();
        cropListBtnRt.anchorMin = new Vector2(0f, 0f);
        cropListBtnRt.anchorMax = new Vector2(0f, 0f);
        cropListBtnRt.pivot = new Vector2(0f, 0f);
        cropListBtnRt.anchoredPosition = new Vector2(20f, 256f);
        cropListBtnRt.sizeDelta = _farmAllCropsButtonSprite != null
            ? new Vector2(188f, 156f)
            : new Vector2(170f, 56f);

        GameObject cropListBtnTextObj = new GameObject("Text", typeof(RectTransform));
        cropListBtnTextObj.transform.SetParent(cropListBtnObj.transform, false);
        TMPro.TextMeshProUGUI cropListBtnText = cropListBtnTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        cropListBtnText.text = "Tum Besinler";
        cropListBtnText.fontSize = _farmAllCropsButtonSprite != null ? 18 : 20;
        cropListBtnText.fontStyle = TMPro.FontStyles.Bold;
        cropListBtnText.alignment = TMPro.TextAlignmentOptions.Center;
        cropListBtnText.color = _farmAllCropsButtonSprite != null
            ? new Color(0.96f, 0.93f, 0.78f, 1f)
            : Color.white;

        if (_farmAllCropsButtonSprite != null)
        {
            cropListBtnText.outlineColor = new Color(0.05f, 0.08f, 0.12f, 0.95f);
            cropListBtnText.outlineWidth = 0.18f;
        }

        RectTransform cropListTextRt = cropListBtnTextObj.GetComponent<RectTransform>();
        if (_farmAllCropsButtonSprite != null)
        {
            cropListTextRt.anchorMin = new Vector2(0.5f, 0f);
            cropListTextRt.anchorMax = new Vector2(0.5f, 0f);
            cropListTextRt.pivot = new Vector2(0.5f, 0f);
            cropListTextRt.anchoredPosition = new Vector2(0f, 10f);
            cropListTextRt.sizeDelta = new Vector2(146f, 36f);
        }
        else
        {
            cropListTextRt.anchorMin = Vector2.zero;
            cropListTextRt.anchorMax = Vector2.one;
            cropListTextRt.offsetMin = Vector2.zero;
            cropListTextRt.offsetMax = Vector2.zero;
        }
    }

    private void HandlePointerInput()
    {
        if (Input.touchCount > 0)
        {
            if (Input.touchCount > 1)
            {
                if (_pointerDown)
                {
                    EndPointer(Input.GetTouch(0).position);
                }
                return;
            }

            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                BeginPointer(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                MovePointer(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                EndPointer(touch.position);
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            BeginPointer(Input.mousePosition);
        }
        else if (_pointerDown && Input.GetMouseButton(0))
        {
            MovePointer(Input.mousePosition);
        }
        else if (_pointerDown && Input.GetMouseButtonUp(0))
        {
            EndPointer(Input.mousePosition);
        }
    }

    private void BeginPointer(Vector2 pointerPosition)
    {
        if (_isAnimating)
            return;

        if (IsPointerOnBottomNavigation(pointerPosition))
            return;

        _pointerDown = true;
        _isDraggingPages = false;
        _gestureLockedVertical = false;
        _pressStart = pointerPosition;
        _dragOffsetX = 0f;

        if (_snapCoroutine != null)
        {
            StopCoroutine(_snapCoroutine);
            _snapCoroutine = null;
        }
    }

    private void MovePointer(Vector2 pointerPosition)
    {
        if (!_pointerDown || _gestureLockedVertical)
            return;

        Vector2 delta = pointerPosition - _pressStart;
        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        if (!_isDraggingPages)
        {
            if (absX < dragStartDeadzone && absY < dragStartDeadzone)
                return;

            // Let vertical list scrolls win if gesture is mostly vertical.
            if (absY > absX * 1.1f)
            {
                _gestureLockedVertical = true;
                return;
            }

            BeginPageDrag();
        }

        float width = Mathf.Max(1f, GetPageWidth());
        _dragOffsetX = Mathf.Clamp(delta.x, -width, width);
        ApplyDragOffset(_dragOffsetX);
    }

    private void EndPointer(Vector2 pointerPosition)
    {
        if (!_pointerDown)
            return;

        _pointerDown = false;

        if (!_isDraggingPages)
        {
            _gestureLockedVertical = false;
            return;
        }

        float width = Mathf.Max(1f, GetPageWidth());
        float dragAmountNormalized = Mathf.Abs(_dragOffsetX) / width;

        int snapDirection = 0;
        if (dragAmountNormalized >= completeThresholdNormalized)
        {
            // Left drag => next page, right drag => previous page.
            snapDirection = _dragOffsetX < 0f ? 1 : -1;
        }

        _isDraggingPages = false;
        _gestureLockedVertical = false;
        StartSnapAnimation(snapDirection);
    }

    private void BeginPageDrag()
    {
        _isDraggingPages = true;

        float width = GetPageWidth();
        int prev = Wrap(_currentIndex - 1);
        int next = Wrap(_currentIndex + 1);
        int hidden = Wrap(_currentIndex + 2);

        ActivatePage(prev, -width);
        ActivatePage(_currentIndex, 0f);
        ActivatePage(next, width);

        _pages[hidden].gameObject.SetActive(false);
        SetPageX(hidden, width * 2f);
    }

    private void ApplyDragOffset(float offsetX)
    {
        float width = GetPageWidth();

        int prev = Wrap(_currentIndex - 1);
        int next = Wrap(_currentIndex + 1);

        ActivatePage(prev, -width + offsetX);
        ActivatePage(_currentIndex, offsetX);
        ActivatePage(next, width + offsetX);
        SyncFarmWorldToFarmPage();
    }

    private void StartSnapAnimation(int snapDirection)
    {
        if (_snapCoroutine != null)
        {
            StopCoroutine(_snapCoroutine);
        }

        _snapCoroutine = StartCoroutine(SnapRoutine(snapDirection));
    }

    private IEnumerator SnapRoutine(int snapDirection)
    {
        _isAnimating = true;

        float width = GetPageWidth();
        float startOffset = _dragOffsetX;
        float targetOffset = 0f;

        if (snapDirection > 0)
        {
            targetOffset = -width;
        }
        else if (snapDirection < 0)
        {
            targetOffset = width;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float currentOffset = Mathf.Lerp(startOffset, targetOffset, t);
            ApplyDragOffset(currentOffset);

            yield return null;
        }

        ApplyDragOffset(targetOffset);

        if (snapDirection != 0)
        {
            _currentIndex = Wrap(_currentIndex + snapDirection);
        }

        _dragOffsetX = 0f;
        SetupIdlePositions();
        ApplyScreenState();

        _isAnimating = false;
        _snapCoroutine = null;
    }

    private void SetupIdlePositions()
    {
        if (_pages == null || _pages.Length == 0)
            return;

        float width = GetPageWidth();

        int prev = Wrap(_currentIndex - 1);
        int next = Wrap(_currentIndex + 1);
        int hidden = Wrap(_currentIndex + 2);

        ActivatePage(prev, -width);
        ActivatePage(_currentIndex, 0f);
        ActivatePage(next, width);

        // Keep one page disabled for performance and clear raycast clutter.
        _pages[hidden].gameObject.SetActive(false);
        SetPageX(hidden, width * 2f);
        SyncFarmWorldToFarmPage();
    }

    private void ApplyScreenState()
    {
        if (_uiManager != null)
        {
            _uiManager.SetScreenTitle(_screenTitles[_currentIndex]);
            _uiManager.SetClickerVisible(_currentIndex == (int)ScreenId.Farm);
        }

        if (_currentIndex != (int)ScreenId.Farm && InventoryUI.Instance != null)
        {
            InventoryUI.Instance.Hide();
        }

        if (_currentIndex != (int)ScreenId.Farm && CropCompendiumUI.Instance != null)
        {
            CropCompendiumUI.Instance.Hide();
        }

        if (_currentIndex != (int)ScreenId.Farm && FarmCropInfoUI.Instance != null)
        {
            FarmCropInfoUI.Instance.Hide();
        }

        UpdateBottomNavigationVisuals();
    }

    private void ActivatePage(int index, float x)
    {
        if (_pages[index] == null)
            return;

        _pages[index].gameObject.SetActive(true);
        SetPageX(index, x);
    }

    private void SetPageX(int index, float x)
    {
        RectTransform rt = _pages[index];
        Vector2 pos = rt.anchoredPosition;
        pos.x = x;
        pos.y = 0f;
        rt.anchoredPosition = pos;
    }

    private int Wrap(int index)
    {
        int count = _pages != null ? _pages.Length : 4;
        int wrapped = index % count;
        if (wrapped < 0) wrapped += count;
        return wrapped;
    }

    private float GetPageWidth()
    {
        if (_root == null)
            return Screen.width;

        float width = _root.rect.width;
        if (width <= 1f)
        {
            width = Screen.width;
        }

        return width;
    }

    private void CacheCameraBaseIfNeeded()
    {
        if (_cameraBaseCached)
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return;

        _baseCameraPosition = _mainCamera.transform.position;
        _cameraBaseCached = true;
    }

    private void SyncFarmWorldToFarmPage()
    {
        CacheCameraBaseIfNeeded();

        if (!_cameraBaseCached || _pages == null || _pages.Length == 0)
            return;

        RectTransform farmPage = _pages[(int)ScreenId.Farm];
        if (farmPage == null)
            return;

        float pageWidth = Mathf.Max(1f, GetPageWidth());
        float visibleWorldWidth = _mainCamera.orthographicSize * 2f * _mainCamera.aspect;
        float worldPerPageUnit = visibleWorldWidth / pageWidth;

        float farmPageOffset = farmPage.anchoredPosition.x;

        Vector3 camPos = _mainCamera.transform.position;
        camPos.x = _baseCameraPosition.x - (farmPageOffset * worldPerPageUnit);
        camPos.y = _baseCameraPosition.y;
        camPos.z = _baseCameraPosition.z;
        _mainCamera.transform.position = camPos;
    }

    private void TryAttachClickButtonToFarmPage()
    {
        if (_pages == null || _pages.Length == 0)
            return;

        RectTransform farmPage = _pages[(int)ScreenId.Farm];
        if (farmPage == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        Transform clickButton = FindByNameRecursive(canvas.transform, "ClickButton");
        if (clickButton == null)
            return;

        if (clickButton.parent != farmPage)
        {
            clickButton.SetParent(farmPage, false);
        }

        RectTransform buttonRt = clickButton as RectTransform;
        if (buttonRt != null)
        {
            buttonRt.anchorMin = new Vector2(0.5f, 0f);
            buttonRt.anchorMax = new Vector2(0.5f, 0f);
            buttonRt.pivot = new Vector2(0.5f, 0f);
            buttonRt.anchoredPosition = new Vector2(0f, 284f);
        }

        _clickButtonAttached = true;
    }

    private static Transform FindByNameRecursive(Transform root, string nameToFind)
    {
        if (root == null || string.IsNullOrWhiteSpace(nameToFind))
            return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == nameToFind)
                return all[i];
        }

        return null;
    }
}
