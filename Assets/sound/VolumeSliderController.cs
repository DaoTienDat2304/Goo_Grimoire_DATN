using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderController : MonoBehaviour
{
    public enum VolumeType { BackgroundMusic, SFX }

    [SerializeField] private VolumeType volumeType = VolumeType.BackgroundMusic;
    [SerializeField] private Slider slider;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();
    }

    private void Start()
    {
        if (AudioManager.Instance == null || slider == null) return;

        // Khởi tạo giá trị slider từ AudioManager
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = volumeType == VolumeType.BackgroundMusic
            ? AudioManager.Instance.GetBackgroundMusicVolume()
            : AudioManager.Instance.GetSFXVolume();

        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        if (AudioManager.Instance == null) return;

        if (volumeType == VolumeType.BackgroundMusic)
            AudioManager.Instance.SetBackgroundMusicVolume(value);
        else
            AudioManager.Instance.SetSFXVolume(value);
    }
}
