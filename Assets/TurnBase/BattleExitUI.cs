using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// </summary>
public class BattleExitUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button exitButton;
    public GameObject confirmPopup;
    public Text confirmMessageText;
    public Button confirmYesButton;
    public Button confirmNoButton;

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

        Debug.Log("[BattleExitUI] Player quit, count as loss.");
        
        TurnSystem turnSystem = FindObjectOfType<TurnSystem>();
        if (turnSystem != null)
        {
            turnSystem.TriggerDefeat();
        }
        else
        {
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
