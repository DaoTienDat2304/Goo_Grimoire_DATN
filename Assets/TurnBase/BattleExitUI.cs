using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Nút Thoát Trận Đấu (Exit "X" Button) trong Battle Scene.
/// Bấm nút X sẽ mở Popup xác nhận bỏ cuộc trước khi quay về scene firstsave.
/// </summary>
public class BattleExitUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button exitButton;                  // Nút X ở góc trên màn hình
    public GameObject confirmPopup;            // Popup xác nhận bỏ cuộc
    public Text confirmMessageText;            // Chữ thông báo trong popup
    public Button confirmYesButton;            // Nút Đồng Ý / Xác Nhận
    public Button confirmNoButton;             // Nút Hủy / Tiếp Tục Trận Đấu

    private void Awake()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);

        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmExit);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnCancelExit);

        if (confirmPopup != null)
            confirmPopup.SetActive(false);
    }

    public void OnExitButtonClicked()
    {
        if (confirmPopup != null)
        {
            if (confirmMessageText != null)
                confirmMessageText.text = "Are you sure you want to retreat and return to the map?";
            confirmPopup.SetActive(true);
        }
        else
        {
            OnConfirmExit();
        }
    }

    public void OnConfirmExit()
    {
        if (confirmPopup != null) confirmPopup.SetActive(false);

        Debug.Log("[BattleExitUI] Người chơi bỏ cuộc, xử lý như thua cuộc.");
        
        // Gọi xử lý thua cuộc từ TurnSystem
        TurnSystem turnSystem = FindObjectOfType<TurnSystem>();
        if (turnSystem != null)
        {
            turnSystem.TriggerDefeat();
        }
        else
        {
            // Fallback nếu không tìm thấy TurnSystem
            if (BattleDataManager.Instance != null)
            {
                BattleDataManager.Instance.ClearBossData();
            }
            StartCoroutine(LoadFirstSaveScene());
        }
    }

    public void OnCancelExit()
    {
        if (confirmPopup != null)
            confirmPopup.SetActive(false);
    }

    private IEnumerator LoadFirstSaveScene()
    {
        yield return SceneLoader.LoadSceneWithLoadingCoroutine("firstsave");
    }
}
