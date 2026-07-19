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

        // Đã bỏ tutorial (travelSence): luôn vào thẳng firstsave (home) — load theo TÊN
        // để không phụ thuộc thứ tự build index. firstsave tự chờ Auth/CloudSave khi khởi tạo.
        if (forceTutorial)
        {
            // Vẫn cho phép ép chạy tutorial nếu bật thủ công trong Inspector.
            await loading.LoadSceneByName("travelSence");
            return;
        }

        await loading.LoadSceneByName("firstsave");
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