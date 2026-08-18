using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class loading : MonoBehaviour
{
    public static loading Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Thanh Slider tiến trình")]
    public Slider slider;
    [Tooltip("Ảnh Fill của thanh loading (để đổi màu theo gradient)")]
    public Image fillImage;
    [Tooltip("Text hiển thị số phần trăm (vd: 72%)")]
    public Text percentText;
    [Tooltip("Text hiển thị mẹo chơi ngẫu nhiên (nếu có, có thể để trống)")]
    public Text tipsText;
    [Tooltip("CanvasGroup của màn hình loading để tạo hiệu ứng Fade in / Fade out mượt mà")]
    public CanvasGroup canvasGroup;
    [Tooltip("(Tùy chọn) Animator nếu có clip chuyển cảnh riêng, không bắt buộc")]
    public Animator animator;

    [Header("Color Gradient theo %")]
    [Tooltip("Dải màu: 0% (Tím) -> 25% (Xanh lam) -> 50% (Cyan) -> 75% (Xanh ngọc) -> 100% (Vàng chanh)")]
    public Gradient progressGradient;

    [Header("Settings")]
    [Tooltip("Thời gian thanh loading lấp đầy từ 0% đến 100% (giây)")]
    [Range(0.5f, 5f)] public float loadingDuration = 1.5f;
    [Tooltip("Thời gian dừng lại ở mốc 100% trước khi mở Scene (giây)")]
    public float completedHoldTime = 0.25f;
    [Tooltip("Thời gian mờ dần (Fade) khi mở và đóng màn hình loading")]
    public float fadeDuration = 0.2f;

    [Header("Gameplay Tips (Tùy chọn)")]
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

            // Đảm bảo ở trạng thái ban đầu không chặn tương tác raycast của game
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
            new GradientColorKey(new Color(0.29f, 0.12f, 0.46f), 0.00f), // 0%: Tím (#4A1E75)
            new GradientColorKey(new Color(0.00f, 0.53f, 1.00f), 0.25f), // 25%: Xanh lam (#0088FF)
            new GradientColorKey(new Color(0.00f, 0.83f, 1.00f), 0.50f), // 50%: Cyan (#00D5FF)
            new GradientColorKey(new Color(0.00f, 0.92f, 0.65f), 0.75f), // 75%: Xanh ngọc (#00EAA5)
            new GradientColorKey(new Color(0.83f, 1.00f, 0.20f), 1.00f)  // 100%: Vàng chanh (#D4FF33)
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
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
        }

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
            gameObject.AddComponent<GraphicRaycaster>();
        }
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
            Debug.LogError("[Loading] Tên scene trống.");
            SceneLoader.EndSceneLoadRequest();
            return;
        }

        int sceneIndex = GetSceneIndexByName(sceneName);
        if (sceneIndex < 0)
        {
            Debug.LogError($"[Loading] Scene '{sceneName}' chưa được thêm vào Build Settings.");
            SceneLoader.EndSceneLoadRequest();
            return;
        }

        await PlayLoading(sceneIndex);
    }

    private async Task PlayLoading(int sceneIndex)
    {
        if (this == null || gameObject == null)
        {
            SceneLoader.EndSceneLoadRequest();
            return;
        }

        gameObject.SetActive(true);

        if (tipsText != null && gameplayTips != null && gameplayTips.Length > 0)
        {
            tipsText.text = gameplayTips[Random.Range(0, gameplayTips.Length)];
        }

        // Đảm bảo thanh loading reset về 0%
        UpdateProgressVisual(0f);

        // Bật chặn raycast trong suốt quá trình loading
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

        // Dọn rác bộ nhớ trước khi tải
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        if (animator != null) animator.SetBool("nextScene", true);

        // Tải scene và lấp đầy thanh tiến trình
        await LoadSceneAsync(sceneIndex);

        // Fade Out Màn hình Loading
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

        // Tắt hoàn toàn toàn bộ LoadingPanel để không chặn tương tác của scene mới
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

        // Vòng lặp nạp dữ liệu và trượt thanh tiến trình từ 0% đến 100%
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

        // Đảm bảo visual chạm đúng 100%
        UpdateProgressVisual(1f);

        // Dừng lại một khoảng ngắn ở mốc 100% để người chơi thấy trọn vẹn dải màu vàng chanh 100%
        if (completedHoldTime > 0f)
        {
            float holdElapsed = 0f;
            while (holdElapsed < completedHoldTime)
            {
                holdElapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }
        }

        // Kích hoạt chuyển sang Scene mới
        scene.allowSceneActivation = true;

        while (!scene.isDone)
        {
            await Task.Yield();
        }

        Application.backgroundLoadingPriority = ThreadPriority.Normal;
    }

    /// <summary>
    /// Cập nhật hiển thị UI thanh loading, màu gradient và số phần trăm %
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
