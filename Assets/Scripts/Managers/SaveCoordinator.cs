using UnityEngine;

/// <summary>
/// Centralized PlayerPrefs flush coordinator.
/// Keeps writes in memory and batches disk flushes to reduce frame spikes on mobile.
/// </summary>
public sealed class SaveCoordinator : MonoBehaviour
{
    public static SaveCoordinator Instance { get; private set; }

    [SerializeField, Min(0.25f)]
    private float autoFlushIntervalSeconds = 2.5f;

    private static bool _dirty;
    private static float _lastDirtyRealtime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!_dirty)
            return;

        if (Time.realtimeSinceStartup - _lastDirtyRealtime >= autoFlushIntervalSeconds)
        {
            FlushNow();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            FlushNow();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            FlushNow();
        }
    }

    private void OnApplicationQuit()
    {
        FlushNow();
    }

    /// <summary>
    /// Marks save data as dirty. Data will be flushed automatically soon,
    /// and immediately on lifecycle events (pause/focus loss/quit).
    /// </summary>
    public static void MarkDirty()
    {
        if (!Application.isPlaying)
        {
            PlayerPrefs.Save();
            return;
        }

        _dirty = true;
        _lastDirtyRealtime = Time.realtimeSinceStartup;

        // Fallback path if coordinator was not spawned yet.
        if (Instance == null)
        {
            PlayerPrefs.Save();
            _dirty = false;
        }
    }

    /// <summary>
    /// Forces immediate PlayerPrefs flush if there are pending changes.
    /// </summary>
    public static void FlushNow()
    {
        if (!_dirty)
            return;

        PlayerPrefs.Save();
        _dirty = false;
    }
}