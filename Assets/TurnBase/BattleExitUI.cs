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
                confirmMessageText.text = "Bạn có chắc chắn muốn bỏ cuộc và quay về bản đồ không?";
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

        // Xóa dữ liệu boss tạm nếu có
        if (BattleDataManager.Instance != null)
        {
            BattleDataManager.Instance.ClearBossData();
        }

        Debug.Log("[BattleExitUI] Người chơi bỏ cuộc, trở về firstsave scene.");
        StartCoroutine(LoadFirstSaveScene());
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
