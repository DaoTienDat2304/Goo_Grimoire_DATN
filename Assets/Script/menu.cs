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
    [Tooltip("Skip save check: +2")]
    public bool forceSkipTutorial = false;
    [Tooltip("Skip save check: +1")]
    public bool forceTutorial = false;

    [Header("Tutorial Settings")]
    [Tooltip("First save gets +2, then +1")]
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

        if (forceTutorial)
        {
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

        Debug.LogError("Target scene missing: set turnBattleSceneName or turnBattleSceneIndex.");
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
            UnityEngine.Debug.LogError("Invalid build index.");
            return;
        }

        await SceneLoader.LoadSceneWithLoading(buildIndex);
    }


    public async void GoToSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name empty.");
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
