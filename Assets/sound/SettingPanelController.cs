using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns all behaviour of the Setting Panel prefab.</summary>
public class SettingPanelController : MonoBehaviour
{
    private const string OverallVolumeKey = "Audio.OverallVolume";
    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string SfxVolumeKey = "Audio.SfxVolume";
    private const float DefaultVolume = 0.8f;

    [Header("Overall button sprites")]
    [SerializeField] private Image overallButtonImage;
    [SerializeField] private Sprite overallOnSprite;
    [SerializeField] private Sprite overallOffSprite;

    [Header("Music button sprites")]
    [SerializeField] private Image musicButtonImage;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    [Header("SFX button sprites")]
    [SerializeField] private Image sfxButtonImage;
    [SerializeField] private Sprite sfxOnSprite;
    [SerializeField] private Sprite sfxOffSprite;

    [Header("Content restored when closing")]
    [SerializeField] private GameObject contentToRestore;
    [SerializeField] private Behaviour componentToRestore;

    private Slider overallSlider;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Button overallButton;
    private Button musicButton;
    private Button sfxButton;
    private Button applyButton;
    private Button defaultButton;
    private Button exitButton;

    private float lastOverall = 0.8f;
    private float lastMusic = 0.8f;
    private float lastSfx = 0.8f;

    public static void ApplySavedSettings(AudioManager audioManager)
    {
        if (audioManager == null) return;
        audioManager.SetOverallVolume(PlayerPrefs.GetFloat(OverallVolumeKey, DefaultVolume));
        audioManager.SetBackgroundMusicVolume(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume));
        audioManager.SetSFXVolume(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume));
    }

    private void Awake()
    {
        overallSlider = FindComponent<Slider>("SliderSFXoverall");
        musicSlider = FindComponent<Slider>("SliderMusic");
        sfxSlider = FindComponent<Slider>("SliderSFX");
        overallButton = FindComponent<Button>("volumeoverall");
        musicButton = FindComponent<Button>("volumeMusic");
        sfxButton = FindComponent<Button>("volumeSFX");
        applyButton = FindComponent<Button>("Apdung");
        defaultButton = FindComponent<Button>("Macdinh");
        exitButton = FindComponent<Button>("ExitButton");

        RememberOnSprites();
        BindEvents();
    }

    private void OnEnable()
    {
        LoadSavedValues();
    }

    private void Start()
    {
        // Start runs after every object's Awake, so AudioManager is available even
        // when this prefab was enabled before AudioManager during scene loading.
        ApplySliderValuesToAudio();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void LoadSavedValues()
    {
        SetSliderWithoutNotify(overallSlider, PlayerPrefs.GetFloat(OverallVolumeKey, DefaultVolume));
        SetSliderWithoutNotify(musicSlider, PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume));
        SetSliderWithoutNotify(sfxSlider, PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume));
        CacheNonZeroValues();
        ApplySliderValuesToAudio();
        RefreshMuteSprites();
    }

    private void BindEvents()
    {
        if (overallSlider != null) overallSlider.onValueChanged.AddListener(SetOverall);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusic);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSfx);
        if (overallButton != null) overallButton.onClick.AddListener(ToggleOverall);
        if (musicButton != null) musicButton.onClick.AddListener(ToggleMusic);
        if (sfxButton != null) sfxButton.onClick.AddListener(ToggleSfx);
        if (applyButton != null) applyButton.onClick.AddListener(Apply);
        if (defaultButton != null) defaultButton.onClick.AddListener(RestoreDefaults);
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(SaveAndClose);
        }
    }

    private void UnbindEvents()
    {
        if (overallSlider != null) overallSlider.onValueChanged.RemoveListener(SetOverall);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusic);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSfx);
        if (overallButton != null) overallButton.onClick.RemoveListener(ToggleOverall);
        if (musicButton != null) musicButton.onClick.RemoveListener(ToggleMusic);
        if (sfxButton != null) sfxButton.onClick.RemoveListener(ToggleSfx);
        if (applyButton != null) applyButton.onClick.RemoveListener(Apply);
        if (defaultButton != null) defaultButton.onClick.RemoveListener(RestoreDefaults);
        if (exitButton != null) exitButton.onClick.RemoveListener(SaveAndClose);
    }

    private void SetOverall(float value)
    {
        if (value > 0f) lastOverall = value;
        if (AudioManager.Instance != null) AudioManager.Instance.SetOverallVolume(value);
        RefreshMuteSprites();
    }

    private void SetMusic(float value)
    {
        if (value > 0f) lastMusic = value;
        if (AudioManager.Instance != null) AudioManager.Instance.SetBackgroundMusicVolume(value);
        RefreshMuteSprites();
    }

    private void SetSfx(float value)
    {
        if (value > 0f) lastSfx = value;
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value);
        RefreshMuteSprites();
    }

    private void ToggleOverall() => overallSlider.value = overallSlider.value > 0f ? 0f : lastOverall;
    private void ToggleMusic() => musicSlider.value = musicSlider.value > 0f ? 0f : lastMusic;
    private void ToggleSfx() => sfxSlider.value = sfxSlider.value > 0f ? 0f : lastSfx;

    public void Apply()
    {
        PlayerPrefs.SetFloat(OverallVolumeKey, overallSlider != null ? overallSlider.value : DefaultVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicSlider != null ? musicSlider.value : DefaultVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxSlider != null ? sfxSlider.value : DefaultVolume);
        PlayerPrefs.Save();
    }

    public void SaveAndClose()
    {
        Apply();

        if (contentToRestore != null)
            contentToRestore.SetActive(true);
        if (componentToRestore != null)
            componentToRestore.enabled = true;

        gameObject.SetActive(false);
    }

    public void RestoreDefaults()
    {
        if (overallSlider != null) overallSlider.value = DefaultVolume;
        if (musicSlider != null) musicSlider.value = DefaultVolume;
        if (sfxSlider != null) sfxSlider.value = DefaultVolume;
    }

    private void ApplySliderValuesToAudio()
    {
        if (AudioManager.Instance == null) return;
        if (overallSlider != null) AudioManager.Instance.SetOverallVolume(overallSlider.value);
        if (musicSlider != null) AudioManager.Instance.SetBackgroundMusicVolume(musicSlider.value);
        if (sfxSlider != null) AudioManager.Instance.SetSFXVolume(sfxSlider.value);
    }

    private void CacheNonZeroValues()
    {
        if (overallSlider != null && overallSlider.value > 0f) lastOverall = overallSlider.value;
        if (musicSlider != null && musicSlider.value > 0f) lastMusic = musicSlider.value;
        if (sfxSlider != null && sfxSlider.value > 0f) lastSfx = sfxSlider.value;
    }

    private void RememberOnSprites()
    {
        if (overallButtonImage == null && overallButton != null) overallButtonImage = overallButton.image;
        if (musicButtonImage == null && musicButton != null) musicButtonImage = musicButton.image;
        if (sfxButtonImage == null && sfxButton != null) sfxButtonImage = sfxButton.image;

        if (overallOnSprite == null && overallButtonImage != null) overallOnSprite = overallButtonImage.sprite;
        if (musicOnSprite == null && musicButtonImage != null) musicOnSprite = musicButtonImage.sprite;
        if (sfxOnSprite == null && sfxButtonImage != null) sfxOnSprite = sfxButtonImage.sprite;
    }

    private void RefreshMuteSprites()
    {
        SetButtonSprite(overallButtonImage, overallSlider, overallOnSprite, overallOffSprite);
        SetButtonSprite(musicButtonImage, musicSlider, musicOnSprite, musicOffSprite);
        SetButtonSprite(sfxButtonImage, sfxSlider, sfxOnSprite, sfxOffSprite);
    }

    private static void SetButtonSprite(Image targetImage, Slider slider, Sprite onSprite, Sprite offSprite)
    {
        if (targetImage == null || slider == null) return;

        Sprite targetSprite = slider.value <= 0f ? offSprite : onSprite;
        if (targetSprite != null) targetImage.sprite = targetSprite;
    }

    private T FindComponent<T>(string objectName) where T : Component
    {
        foreach (T component in GetComponentsInChildren<T>(true))
            if (component.gameObject.name == objectName) return component;

        Debug.LogWarning($"[SettingPanel] Cannot find '{objectName}' on prefab.", this);
        return null;
    }

    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
    }
}
