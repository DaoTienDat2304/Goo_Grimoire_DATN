using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class menu : MonoBehaviour
{
    public loading loading;
    public Animator animator;
    public Image Image;
    [SerializeField] private SlimeWorldManager slimeWorldManager;

    [Header("Scene Jump")]
    [SerializeField] private int turnBattleSceneIndex = -1;
    [SerializeField] private string turnBattleSceneName = "TurnBaseGame";

    [Header("Debug Settings")]
    [Tooltip("Bỏ qua kiểm tra save file - luôn nhảy +2")]
    public bool forceSkipTutorial = false;
    [Tooltip("Bỏ qua kiểm tra save file - luôn nhảy +1")]
    public bool forceTutorial = false;

    [Header("Tutorial Settings")]
    [Tooltip("Lần đầu tiên có save file sẽ +2, sau đó +1")]
    public bool firstTimeSkipTutorial = true;

    private void Awake()
    {

    }

    public void onactive()
    {
        animator.SetTrigger("active");
        if (slimeWorldManager != null )
        {
            slimeWorldManager.ClearWorldSlimes();
        }
    }

    public void continues()
    {
        animator.SetTrigger("remove");
        if (slimeWorldManager != null)
        {
            slimeWorldManager.CreateWorldSlimes();
        }
    }

    public async void onplay()
    {
        loading.gameObject.SetActive(true);

        if (forceSkipTutorial)
        {
            await loading.onplay(SceneManager.GetActiveScene().buildIndex + 2);
            return;
        }

        if (forceTutorial)
        {
            await loading.onplay(SceneManager.GetActiveScene().buildIndex + 1);
            return;
        }

        // Chờ CloudSaveProvider kiểm tra xong cloud save cho tài khoản hiện tại
        while (CloudSaveProvider.Instance == null || !CloudSaveProvider.Instance.CloudCheckDone)
            await System.Threading.Tasks.Task.Yield();

        // Returning player (đã có cloud save) → skip travelSence
        // New player (chưa có cloud save) → chạy travelSence
        if (CloudSaveProvider.Instance.HasCloudSave)
            await loading.onplay(SceneManager.GetActiveScene().buildIndex + 2);
        else
            await loading.onplay(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Save()
    {
        SaveAndLoadSystem.Instance.Save();
    }


    public async void SaveTeam()
    {
        if (!string.IsNullOrEmpty(turnBattleSceneName))
        {
            await SceneLoader.LoadSceneWithLoading(turnBattleSceneName);
            return;
        }

        if (turnBattleSceneIndex >= 0)
        {
            await SceneLoader.LoadSceneWithLoading(turnBattleSceneIndex);
            return;
        }

        Debug.LogError("Chưa cấu hình scene đích: đặt 'turnBattleSceneName' hoặc 'turnBattleSceneIndex' trong Inspector.");
    }

    public async void GoToTurnBaseGame()
    {
        if (BattleDataManager.Instance == null)
        {
            GameObject battleDataManagerGO = new GameObject("BattleDataManager");
            battleDataManagerGO.AddComponent<BattleDataManager>();
        }
        
        BattleDataManager.Instance.SetBattleMode(BattleMode.Tower);
        await SceneLoader.LoadSceneWithLoading("TurnBaseGame");
    }

    public async void GoToSceneByIndex(int buildIndex)
    {
        if (buildIndex < 0)
        {
            UnityEngine.Debug.LogError("Build Index không hợp lệ.");
            return;
        }

        await SceneLoader.LoadSceneWithLoading(buildIndex);
    }


    public async void GoToSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Tên scene rỗng.");
            return;
        }

        await SceneLoader.LoadSceneWithLoading(sceneName);
    }

    public void onExit()
    {
        Application.Quit();
    }

    [ContextMenu("Test Skip Tutorial (+2)")]
    public void TestSkipTutorial()
    {
        forceSkipTutorial = true;
        forceTutorial = false;
    }

    [ContextMenu("Test Tutorial (+1)")]
    public void TestTutorial()
    {
        forceTutorial = true;
        forceSkipTutorial = false;
    }

    [ContextMenu("Reset Debug Settings")]
    public void ResetDebugSettings()
    {
        forceSkipTutorial = false;
        forceTutorial = false;
    }

    [ContextMenu("Reset Tutorial Skip")]
    public void ResetTutorialSkip()
    {
        firstTimeSkipTutorial = true;
    }

    [ContextMenu("Test First Time Skip")]
    public void TestFirstTimeSkip()
    {
        firstTimeSkipTutorial = true;
        forceSkipTutorial = false;
        forceTutorial = false;
    }
}