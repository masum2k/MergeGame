using UnityEngine;

/// <summary>
/// Fits a RectTransform to the current safe area on mobile devices.
/// Attach to root Canvas RectTransform in screen-space UIs.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [SerializeField]
    private bool updateOnResolutionOrSafeAreaChange = true;

    private RectTransform _rectTransform;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void LateUpdate()
    {
        if (!updateOnResolutionOrSafeAreaChange)
            return;

        Rect currentSafeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

        if (currentSafeArea != _lastSafeArea || screenSize != _lastScreenSize)
        {
            ApplySafeArea();
        }
    }

    public void ApplySafeArea()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2 min = safeArea.position;
        Vector2 max = safeArea.position + safeArea.size;

        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);

        min.x /= screenWidth;
        min.y /= screenHeight;
        max.x /= screenWidth;
        max.y /= screenHeight;

        _rectTransform.anchorMin = min;
        _rectTransform.anchorMax = max;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;

        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}