using UnityEngine;

public sealed class MobileThermalOptimizer : MonoBehaviour
{
    private const int ActiveFrameRate = 30;
    private const int IdleFrameRate = 20;
    private const float IdleDelay = 45f;
    private const float SustainedModeDelay = 8f * 60f;

    private float lastInputTime;
    private bool isIdle;
    private bool sustainedMode;
    private float sessionStartTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!Application.isMobilePlatform || FindAnyObjectByType<MobileThermalOptimizer>() != null)
            return;

        GameObject host = new GameObject(nameof(MobileThermalOptimizer));
        DontDestroyOnLoad(host);
        host.AddComponent<MobileThermalOptimizer>();
    }

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = ActiveFrameRate;
        Time.fixedDeltaTime = 1f / ActiveFrameRate;
        lastInputTime = Time.unscaledTime;
        sessionStartTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (!sustainedMode && Time.unscaledTime - sessionStartTime >= SustainedModeDelay)
            EnableSustainedMode();

        if (HasUserInput())
        {
            lastInputTime = Time.unscaledTime;
            SetIdle(false);
        }
        else if (!isIdle && Time.unscaledTime - lastInputTime >= IdleDelay)
        {
            SetIdle(true);
        }
    }

    private static bool HasUserInput()
    {
        return Input.touchCount > 0 || Input.anyKeyDown || Input.GetMouseButtonDown(0);
    }

    private void SetIdle(bool idle)
    {
        if (isIdle == idle) return;
        isIdle = idle;
        Application.targetFrameRate = idle ? IdleFrameRate : GetActiveFrameRate();
    }

    private int GetActiveFrameRate()
    {
        return ActiveFrameRate;
    }

    private void EnableSustainedMode()
    {
        sustainedMode = true;

        SlimeSpawner[] spawners = FindObjectsByType<SlimeSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SlimeSpawner spawner in spawners)
            spawner.SetSustainedPerformanceMode(true);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Application.targetFrameRate = hasFocus
            ? (isIdle ? IdleFrameRate : GetActiveFrameRate())
            : 10;
    }
}
