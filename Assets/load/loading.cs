using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class loading : MonoBehaviour
{
    public static loading Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Progress slider")]
    public Slider slider;
    [Tooltip("Progress fill")]
    public Image fillImage;
    [Tooltip("Percent text")]
    public Text percentText;
    [Tooltip("Tip text")]
    public Text tipsText;
    [Tooltip("Loading fade group")]
    public CanvasGroup canvasGroup;
    [Tooltip("Optional transition animator")]
    public Animator animator;

    [Header("Color Gradient theo %")]
    [Tooltip("Progress colors")]
    public Gradient progressGradient;

    [Header("Settings")]
    [Tooltip("Load fill time (s)")]
    [Range(0.5f, 5f)] public float loadingDuration = 1.5f;
    [Tooltip("Hold at 100% (s)")]
    public float completedHoldTime = 0.25f;
    [Tooltip("Fade time")]
    public float fadeDuration = 0.2f;

    [Header("Gameplay Tips")]
    public string[] gameplayTips = new string[]
    {
        "Weapon Skills of (Rare) rarity or higher unlock an Ultimate ability upon accumulating 100 Energy!",
        "Basic Attacks restore 1 Skill Point (SP) for the entire team.",
        "Sacrificing high-level Slimes yields more Fusion points for summoning Secret Slimes.",
        "Each Slime is limited to gaining a maximum of +2 additional Skill Points per turn from all sources combined."
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            EnsureCanvas();
            EnsureDefaultGradient();
            EnsureCanvasGroup();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            gameObject.SetActive(false);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    [ContextMenu("Reset Default Gradient")]
    public void ResetDefaultGradient()
    {
        progressGradient = new Gradient();
        var colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(new Color(0.29f, 0.12f, 0.46f), 0.00f),
            new GradientColorKey(new Color(0.00f, 0.53f, 1.00f), 0.25f), // 25%: Xanh lam (#0088FF)
            new GradientColorKey(new Color(0.00f, 0.83f, 1.00f), 0.50f), // 50%: Cyan (#00D5FF)
            new GradientColorKey(new Color(0.00f, 0.92f, 0.65f), 0.75f),
            new GradientColorKey(new Color(0.83f, 1.00f, 0.20f), 1.00f)
        };
        var alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };
        progressGradient.SetKeys(colorKeys, alphaKeys);
    }

    private void EnsureDefaultGradient()
    {
        if (progressGradient == null || progressGradient.colorKeys == null || progressGradient.colorKeys.Length < 3)
        {
            ResetDefaultGradient();
        }
    }

    private void EnsureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    public async Task onplay(int scene)
    {
        if (!SceneLoader.TryBeginSceneLoad()) return;
        await PlayLoading(scene);
    }

    public async Task LoadSceneByName(string sceneName)
    {
        if (!SceneLoader.TryBeginSceneLoad()) return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[Loading] Scene name empty, using 'firstsave'.");
            sceneName = "firstsave";
        }

        int sceneIndex = GetSceneIndexByName(sceneName);
        if (sceneIndex < 0)
        {
            Debug.LogWarning($"[Loading] Scene '{sceneName}' not in Build Settings. Using 'firstsave'...");
            sceneIndex = GetSceneIndexByName("firstsave");
            if (sceneIndex < 0)
            {
                Debug.LogError("[Loading] Scene not found 'firstsave' trong Build Settings.");
                SceneLoader.EndSceneLoadRequest();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                }
                gameObject.SetActive(false);
                return;
            }
        }

        await PlayLoading(sceneIndex);
    }

    private async Task PlayLoading(int sceneIndex)
    {
        EnsureCanvas();
        EnsureCanvasGroup();
        transform.SetParent(null, false);
        transform.SetAsLastSibling();

        Canvas startCanvas = GetComponent<Canvas>();
        if (startCanvas != null)
        {
            startCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            startCanvas.overrideSorting = true;
            startCanvas.sortingOrder = 32767;
        }

        gameObject.SetActive(true);

        if (tipsText != null && gameplayTips != null && gameplayTips.Length > 0)
        {
            tipsText.text = gameplayTips[Random.Range(0, gameplayTips.Length)];
        }

        UpdateProgressVisual(0f);

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < fadeDuration && canvasGroup != null)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
                await Task.Yield();
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        if (animator != null) animator.SetBool("nextScene", true);

        await LoadSceneAsync(sceneIndex);

        if (canvasGroup != null)
        {
            float t = 0f;
            while (t < fadeDuration && canvasGroup != null)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - (t / fadeDuration));
                await Task.Yield();
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        gameObject.SetActive(false);

        SceneLoader.EndSceneLoadRequest();
    }

    private async Task LoadSceneAsync(int index)
    {
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        AsyncOperation scene = SceneManager.LoadSceneAsync(index);
        if (scene == null)
        {
            Application.backgroundLoadingPriority = ThreadPriority.Normal;
            return;
        }

        scene.allowSceneActivation = false;

        float visualProgress = 0f;
        float elapsed = 0f;
        float duration = Mathf.Max(0.5f, loadingDuration);

        while (visualProgress < 1f)
        {
            if (this == null || gameObject == null) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            elapsed += dt;

            visualProgress = Mathf.Clamp01(elapsed / duration);

            if (scene.progress < 0.9f && visualProgress >= 0.9f)
            {
                visualProgress = 0.9f;
                elapsed = 0.9f * duration;
            }

            UpdateProgressVisual(visualProgress);

            if (visualProgress >= 0.9f && scene.progress < 0.9f)
            {
                await Task.Yield();
                continue;
            }

            if (visualProgress >= 1f && scene.progress >= 0.9f)
            {
                break;
            }

            await Task.Yield();
        }

        UpdateProgressVisual(1f);

        if (completedHoldTime > 0f)
        {
            float holdElapsed = 0f;
            while (holdElapsed < completedHoldTime)
            {
                holdElapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }
        }

        scene.allowSceneActivation = true;

        while (!scene.isDone)
        {
            await Task.Yield();
        }

        // Re-enforce topmost status after incoming scene canvases/joysticks have initialized
        EnsureCanvas();
        transform.SetParent(null, false);
        transform.SetAsLastSibling();
        Canvas postCanvas = GetComponent<Canvas>();
        if (postCanvas != null)
        {
            postCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            postCanvas.overrideSorting = true;
            postCanvas.sortingOrder = 32767;
        }

        Application.backgroundLoadingPriority = ThreadPriority.Normal;
    }

    /// <summary>
    /// </summary>
    public void UpdateProgressVisual(float progress01)
    {
        if (this == null || gameObject == null) return;

        progress01 = Mathf.Clamp01(progress01);

        if (slider != null)
        {
            slider.value = progress01;
        }

        Color currentColor = progressGradient != null 
            ? progressGradient.Evaluate(progress01) 
            : Color.cyan;

        if (fillImage != null)
        {
            fillImage.color = currentColor;
        }

        if (percentText != null)
        {
            percentText.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
            percentText.color = currentColor;
        }
    }

    private int GetSceneIndexByName(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
                return i;
        }
        return -1;
    }
}
