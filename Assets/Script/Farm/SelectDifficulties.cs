using UnityEngine;
using UnityEngine.UI;

public class SelectDifficulties : MonoBehaviour
{
    private FarmModeManager farmModeManager;
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    public Button extremeButton;
    public Button hellButton;
    
    void Start()
    {
        farmModeManager = FarmModeManager.Instance;
        UpdateButtonStates();
    }
    
    void OnEnable()
    {
        // Cập nhật trạng thái button mỗi khi panel được mở
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Cập nhật trạng thái của các button (unlock/lock và alpha)
    /// </summary>
    private void UpdateButtonStates()
    {
        if (farmModeManager == null) return;
        
        UpdateButtonState(easyButton, 0);
        UpdateButtonState(mediumButton, 1);
        UpdateButtonState(hardButton, 2);
        UpdateButtonState(extremeButton, 3);
        UpdateButtonState(hellButton, 4);
    }
    
    /// <summary>
    /// Cập nhật trạng thái của một button
    /// </summary>
    private void UpdateButtonState(Button button, int difficultyIndex)
    {
        if (button == null) return;
        
        bool isUnlocked = farmModeManager.IsDifficultyUnlocked(difficultyIndex);
        
        // Đặt alpha = 0.6 nếu chưa unlock, 1.0 nếu đã unlock
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = isUnlocked ? 1.0f : 0.6f;
        button.interactable = isUnlocked;
    }

    public void SelectEasy()
    {
        if (farmModeManager != null && farmModeManager.IsDifficultyUnlocked(0))
        {
            farmModeManager.SelectDifficulty(0);
        }
    }

    public void SelectMedium()
    {
        if (farmModeManager != null && farmModeManager.IsDifficultyUnlocked(1))
        {
            farmModeManager.SelectDifficulty(1);
        }
    }

    public void SelectHard()
    {
        if (farmModeManager != null && farmModeManager.IsDifficultyUnlocked(2))
        {
            farmModeManager.SelectDifficulty(2);
        }
    }

    public void SelectExtreme()
    {
        if (farmModeManager != null && farmModeManager.IsDifficultyUnlocked(3))
        {
            farmModeManager.SelectDifficulty(3);
        }
    }

    public void SelectHell()
    {
        if (farmModeManager != null && farmModeManager.IsDifficultyUnlocked(4))
        {
            farmModeManager.SelectDifficulty(4);
        }
    }
}
