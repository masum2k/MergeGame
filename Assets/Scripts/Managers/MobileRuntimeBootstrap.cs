using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies runtime mobile defaults and keeps UI canvases inside the device safe area.
/// </summary>
public class MobileRuntimeBootstrap : MonoBehaviour
{
    public static MobileRuntimeBootstrap Instance { get; private set; }

    [Header("Runtime Performance")]
    [SerializeField, Range(30, 120)]
    private int targetFrameRate = 60;

    [SerializeField]
    private bool disableVSync = true;

    [SerializeField]
    private bool keepScreenAwake = true;

    private float _canvasScanTimer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyRuntimeDefaults();
            EnsureSafeAreaFitters();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // UIs are generated at runtime in this project. Re-scan occasionally.
        _canvasScanTimer += Time.unscaledDeltaTime;
        if (_canvasScanTimer >= 0.2f)
        {
            _canvasScanTimer = 0f;
            EnsureSafeAreaFitters();
        }
    }

    private void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        ApplyRuntimeDefaults();
        EnsureSafeAreaFitters();
    }

    private void ApplyRuntimeDefaults()
    {
        if (!Application.isMobilePlatform)
            return;

        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }

        Application.targetFrameRate = targetFrameRate;

        if (keepScreenAwake)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        ApplyAdaptiveQualityForDevice();
    }

    private void ApplyAdaptiveQualityForDevice()
    {
        int qualityCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
        if (qualityCount == 0)
            return;

        int memoryMb = Mathf.Max(0, SystemInfo.systemMemorySize);
        int targetQuality;

        if (memoryMb <= 2500)
        {
            targetQuality = 0; // Very Low
        }
        else if (memoryMb <= 3500)
        {
            targetQuality = 1; // Low
        }
        else if (memoryMb <= 5500)
        {
            targetQuality = 2; // Medium
        }
        else if (memoryMb <= 7500)
        {
            targetQuality = 3; // High
        }
        else
        {
            targetQuality = qualityCount - 1; // Highest available tier
        }

        targetQuality = Mathf.Clamp(targetQuality, 0, qualityCount - 1);

        if (QualitySettings.GetQualityLevel() != targetQuality)
        {
            QualitySettings.SetQualityLevel(targetQuality, true);
        }
    }

    private void EnsureSafeAreaFitters()
    {
        if (!Application.isMobilePlatform)
            return;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                continue;

            RectTransform safeRoot = GetOrCreateSafeAreaRoot(canvas);
            ReparentCanvasChildrenToSafeRoot(canvas.transform, safeRoot);

            SafeAreaFitter fitter = safeRoot.GetComponent<SafeAreaFitter>();
            if (fitter == null)
            {
                fitter = safeRoot.gameObject.AddComponent<SafeAreaFitter>();
            }

            fitter.ApplySafeArea();
        }
    }

    private RectTransform GetOrCreateSafeAreaRoot(Canvas canvas)
    {
        const string safeAreaRootName = "SafeAreaRoot";

        Transform existing = canvas.transform.Find(safeAreaRootName);
        if (existing != null)
            return existing as RectTransform;

        GameObject safeRootObj = new GameObject(safeAreaRootName, typeof(RectTransform));
        RectTransform safeRoot = safeRootObj.GetComponent<RectTransform>();
        safeRoot.SetParent(canvas.transform, false);
        safeRoot.anchorMin = Vector2.zero;
        safeRoot.anchorMax = Vector2.one;
        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;
        safeRoot.SetAsLastSibling();
        return safeRoot;
    }

    private void ReparentCanvasChildrenToSafeRoot(Transform canvasRoot, Transform safeRoot)
    {
        int childCount = canvasRoot.childCount;
        if (childCount == 0)
            return;

        List<Transform> toMove = new List<Transform>(childCount);
        for (int i = 0; i < childCount; i++)
        {
            Transform child = canvasRoot.GetChild(i);
            if (child == null || child == safeRoot)
                continue;

            toMove.Add(child);
        }

        for (int i = 0; i < toMove.Count; i++)
        {
            Transform child = toMove[i];
            child.SetParent(safeRoot, false);
        }
    }
}