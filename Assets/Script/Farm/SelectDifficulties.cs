using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectDifficulties : MonoBehaviour
{
    private FarmModeManager farmModeManager;
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    public Button extremeButton;
    public Button hellButton;

    [Header("Reward Popup (TextMeshPro)")]
    public GameObject rewardPopup;              // Panel hiện khi claim phần thưởng
    public TMP_Text rewardPopupText;            // Text hiển thị nội dung phần thưởng
    public Button rewardPopupCloseButton;       // Nút đóng popup

    void Awake()
    {
        if (rewardPopupCloseButton != null)
            rewardPopupCloseButton.onClick.AddListener(HideRewardPopup);
        if (rewardPopup != null)
            rewardPopup.SetActive(false);
    }
    
    void Start()
    {
        farmModeManager = FarmModeManager.Instance;
        UpdateButtonStates();
    }
    
    void OnEnable()
    {
        UpdateButtonStates();
    }

    public void ShowRewardPopup(string message)
    {
        if (rewardPopupText != null) rewardPopupText.text = message;
        if (rewardPopup != null) rewardPopup.SetActive(true);
    }

    public void HideRewardPopup()
    {
        if (rewardPopup != null) rewardPopup.SetActive(false);
    }
    private void UpdateButtonStates()
    {
        if (farmModeManager == null) return;
        
        UpdateButtonState(easyButton, 0);
        UpdateButtonState(mediumButton, 1);
        UpdateButtonState(hardButton, 2);
        UpdateButtonState(extremeButton, 3);
        UpdateButtonState(hellButton, 4);
    }
    private void UpdateButtonState(Button button, int difficultyIndex)
    {
        if (button == null) return;
        
        bool isUnlocked = farmModeManager.IsDifficultyUnlocked(difficultyIndex);
        
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = isUnlocked ? 1.0f : 0.5f;
        button.interactable = true; // Cho phép bấm để hiện thông báo yêu cầu tầng tháp nếu bị khóa
    }

    public void SelectEasy()
    {
        if (farmModeManager != null)
        {
            farmModeManager.SelectDifficulty(0);
        }
    }

    public void SelectMedium()
    {
        if (farmModeManager != null)
        {
            farmModeManager.SelectDifficulty(1);
        }
    }

    public void SelectHard()
    {
        if (farmModeManager != null)
        {
            farmModeManager.SelectDifficulty(2);
        }
    }

    public void SelectExtreme()
    {
        if (farmModeManager != null)
        {
            farmModeManager.SelectDifficulty(3);
        }
    }

    public void SelectHell()
    {
        if (farmModeManager != null)
        {
            farmModeManager.SelectDifficulty(4);
        }
    }
}
