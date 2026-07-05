using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource backgroundAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Background Music Clips")]
    [SerializeField] private AudioClip sharedClip; // menu, loading, breeding
    [SerializeField] private AudioClip adventureClip; // adventure scene
    [SerializeField] private AudioClip travelClip; // travel scene

    [Header("Sound Effect Clips")]
    [SerializeField] private AudioClip catcherThrowSFX;
    [SerializeField] private AudioClip breedingSFX;
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buildingClickSFX;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float backgroundMusicVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private AudioClip generatedButtonClickSFX;
    private AudioClip generatedBuildingClickSFX;
    private float lastButtonClickTime = -1f;
    private const float ButtonClickThrottleSeconds = 0.035f;

    // Configure your scene name keywords here
    private const string SceneName_Menu = "menu";
    private const string SceneName_Loading = "loading";
    private const string SceneName_Breeding = "breed"; // any scene containing this treated as breeding
    private const string SceneName_Adventure = "adventure";
    private const string SceneName_Travel = "travel";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSourceDefaults();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Play appropriate music for initial scene
        ApplySceneMusic(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        ApplySceneMusic(next);
    }

    private void EnsureAudioSourceDefaults()
    {
        if (backgroundAudioSource == null)
        {
            backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        }
        backgroundAudioSource.loop = true;
        if (backgroundAudioSource.playOnAwake)
        {
            backgroundAudioSource.playOnAwake = false;
        }

        // Setup SFX audio source
        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }
        sfxAudioSource.loop = false;
        sfxAudioSource.playOnAwake = false;
        
        // Apply volume settings
        UpdateVolumeSettings();
    }

    private void ApplySceneMusic(Scene scene)
    {
        var targetClip = ResolveClipForScene(scene.name);
        if (targetClip == null)
        {
            return;
        }

        // Avoid restarting if already playing same clip
        if (backgroundAudioSource.clip == targetClip && backgroundAudioSource.isPlaying)
        {
            return;
        }

        backgroundAudioSource.clip = targetClip;
        backgroundAudioSource.Play();
    }

    private AudioClip ResolveClipForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return sharedClip;

        var lower = sceneName.ToLowerInvariant();
        if (lower.Contains(SceneName_Adventure)) return adventureClip != null ? adventureClip : sharedClip;
        if (lower.Contains(SceneName_Travel)) return travelClip != null ? travelClip : sharedClip;
        if (lower.Contains(SceneName_Breeding)) return sharedClip;
        if (lower.Contains(SceneName_Menu)) return sharedClip;
        if (lower.Contains(SceneName_Loading)) return sharedClip;

        // Default fallback
        return sharedClip;
    }

    // Optional manual trigger from UI or code
    public void PlayShared()
    {
        backgroundAudioSource.clip = sharedClip;
        backgroundAudioSource.Play();
    }

    public void PlayAdventure()
    {
        backgroundAudioSource.clip = adventureClip;
        backgroundAudioSource.Play();
    }

    public void PlayTravel()
    {
        backgroundAudioSource.clip = travelClip;
        backgroundAudioSource.Play();
    }

    // Volume Control Methods
    private void UpdateVolumeSettings()
    {
        if (backgroundAudioSource != null)
        {
            backgroundAudioSource.volume = backgroundMusicVolume;
        }
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = sfxVolume;
        }
    }

    public void SetBackgroundMusicVolume(float volume)
    {
        backgroundMusicVolume = Mathf.Clamp01(volume);
        if (backgroundAudioSource != null)
        {
            backgroundAudioSource.volume = backgroundMusicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = sfxVolume;
        }
    }

    public float GetBackgroundMusicVolume()
    {
        return backgroundMusicVolume;
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    // Sound Effect Methods
    public void PlayCatcherThrowSFX()
    {
        if (catcherThrowSFX != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(catcherThrowSFX, sfxVolume);
        }
    }

    public void PlayBreedingSFX()
    {
        if (breedingSFX != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(breedingSFX, sfxVolume);
        }
    }

    public void PlayButtonClickSFX()
    {
        if (Time.unscaledTime - lastButtonClickTime < ButtonClickThrottleSeconds)
            return;

        lastButtonClickTime = Time.unscaledTime;
        PlaySFX(buttonClickSFX != null ? buttonClickSFX : GetGeneratedButtonClickSFX(), 0.85f);
    }

    public void PlayBuildingClickSFX()
    {
        PlaySFX(buildingClickSFX != null ? buildingClickSFX : GetGeneratedBuildingClickSFX(), 0.9f);
    }

    // Generic method to play any sound effect
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(clip, sfxVolume);
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }
    }

    private AudioClip GetGeneratedButtonClickSFX()
    {
        if (generatedButtonClickSFX == null)
            generatedButtonClickSFX = CreateTapClip("Generated_Button_Tap", 860f, 1180f, 0.045f);
        return generatedButtonClickSFX;
    }

    private AudioClip GetGeneratedBuildingClickSFX()
    {
        if (generatedBuildingClickSFX == null)
            generatedBuildingClickSFX = CreateTapClip("Generated_Building_Tap", 620f, 920f, 0.055f);
        return generatedBuildingClickSFX;
    }

    private static AudioClip CreateTapClip(string clipName, float startFrequency, float endFrequency, float duration)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        var samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            phase += frequency / sampleRate;

            float envelope = Mathf.Exp(-t * 18f) * Mathf.Clamp01(t / 0.08f);
            float tone = Mathf.Sin(phase * Mathf.PI * 2f);
            samples[i] = tone * envelope * 0.42f;
        }

        var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
