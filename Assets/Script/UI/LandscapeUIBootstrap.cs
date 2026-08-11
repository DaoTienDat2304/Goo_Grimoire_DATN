using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LandscapeUIBootstrap : MonoBehaviour
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float ScanInterval = 3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void ForceLandscapeBeforeSplash()
    {
        ApplyLandscapeOrientation();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        ApplyLandscapeOrientation();

        if (FindObjectsByType<LandscapeUIBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            return;

        var runner = new GameObject("LandscapeUIBootstrap");
        DontDestroyOnLoad(runner);
        runner.AddComponent<LandscapeUIBootstrap>();
    }

    private void OnEnable()
    {
        ApplyLandscapeOrientation();
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(ScanLoop());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyLandscapeOrientation();
        ConfigureAllCanvases();
    }

    private IEnumerator ScanLoop()
    {
        var wait = new WaitForSecondsRealtime(ScanInterval);
        while (enabled)
        {
            ApplyLandscapeOrientation();
            ConfigureAllCanvases();
            yield return wait;
        }
    }

    public static void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
            return;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

    }

    private static void ConfigureAllCanvases()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
            ConfigureCanvas(canvas);
    }

    private static void ApplyLandscapeOrientation()
    {
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }
}
