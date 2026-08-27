using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class NewGameManager : MonoBehaviour
{
    [Header("Opening Dialogue")]
    public OpeningDialogueManager openingDialogueManager;
    public OpeningDialogueSetup openingDialogueSetup;
    
    [Header("Scene Management")]
    public string mainGameSceneName = "firstsave";
    public string adventureSceneName = "adventureSence";
    
    [Header("New Game Settings")]
    public bool skipOpeningDialogue = false;
    public bool resetSaveData = true;
    
    [Header("Debug Settings")]
    [Tooltip("Skip new-game check: always run opening")]
    public bool forceOpeningSequence = false;
    [Tooltip("Skip new-game check: never run opening")]
    public bool skipOpeningSequence = false;
    
    [Header("Tutorial Settings")]
    [Tooltip("Skip tutorial if save exists")]
    public bool skipTutorialWhenSaveExists = true;
    
    private static NewGameManager instance;
    public static NewGameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<NewGameManager>();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        bool isNewGame = IsNewGame();
        
        if (!isNewGame && skipTutorialWhenSaveExists)
        {
            LoadMainGame();
            return;
        }
        
        if (openingDialogueManager == null)
        {
            openingDialogueManager = FindAnyObjectByType<OpeningDialogueManager>();
        }
        
        if (openingDialogueSetup == null)
        {
            openingDialogueSetup = FindAnyObjectByType<OpeningDialogueSetup>();
        }
        
        CheckAndStartNewGame();
    }
    
    private void CheckAndStartNewGame()
    {
        bool isNewGame = IsNewGame();
        
        if (forceOpeningSequence)
        {
            StartCoroutine(StartNewGameSequence());
            return;
        }
        
        if (skipOpeningSequence)
        {
            LoadMainGame();
            return;
        }
        
        if (isNewGame && !skipOpeningDialogue)
        {
            StartCoroutine(StartNewGameSequence());
        }
        else if (isNewGame && skipOpeningDialogue)
        {
            LoadMainGame();
        }
    }
    
    private bool IsNewGame()
    {
        if (AuthManager.Instance != null)
        {
            string localJson = LocalSaveStore.Load(AuthManager.Instance.LocalSaveId);
            if (!string.IsNullOrEmpty(localJson))
            {
                return false;
            }
        }

        string guestJson = LocalSaveStore.Load("guest");
        if (!string.IsNullOrEmpty(guestJson)) return false;

        if (CloudSaveProvider.Instance != null && CloudSaveProvider.Instance.HasCloudSave)
            return false;

        if (CloudSaveProvider.Instance != null && !CloudSaveProvider.Instance.CloudCheckDone)
            return false;
        return true;
    }
    
    private IEnumerator StartNewGameSequence()
    {
        if (openingDialogueSetup != null)
        {
            openingDialogueSetup.SetupOpeningDialogues();
            yield return new WaitForEndOfFrame();
        }
        
        if (openingDialogueManager != null)
        {
            openingDialogueManager.ForceStartOpeningSequence();
            
            yield return new WaitUntil(() => !openingDialogueManager.IsOpeningSequenceActive());
        }
        
        CompleteNewGame();
    }
    
    private void CompleteNewGame()
    {
        InitializeNewGameData();
        
        OnNewGameCompleted?.Invoke();
    }
    
    private void InitializeNewGameData()
    {
        
        if (QuestManager.Instance != null)
        {
        }
        
        if (BreedingManager.Instance != null)
        {
        }
    }
    
    public async void StartNewGame()
    {
        if (resetSaveData)
        {
            ResetSaveData();
        }
        
        await SceneLoader.LoadSceneWithLoading(SceneManager.GetActiveScene().name);
    }
    
    private void ResetSaveData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
    
    public async void LoadMainGame()
    {
        if (!string.IsNullOrEmpty(mainGameSceneName))
        {
            await SceneLoader.LoadSceneWithLoading(mainGameSceneName);
        }
    }
    
    public async void LoadAdventureScene()
    {
        if (!string.IsNullOrEmpty(adventureSceneName))
        {
            await SceneLoader.LoadSceneWithLoading(adventureSceneName);
        }
    }
    
    public void SkipOpeningDialogue()
    {
        skipOpeningDialogue = true;
        
        if (openingDialogueManager != null)
        {
            openingDialogueManager.ResetOpeningSequence();
        }
        
        LoadMainGame();
    }
    
    // Debug methods
    [ContextMenu("Test New Game")]
    public void TestNewGame()
    {
        StartNewGame();
    }
    
    [ContextMenu("Force New Game (Delete Save)")]
    public void ForceNewGame()
    {
        ResetSaveData();
        StartNewGame();
    }
    
    [ContextMenu("Skip Opening Dialogue")]
    public void TestSkipOpening()
    {
        SkipOpeningDialogue();
    }
    
    [ContextMenu("Force Opening Sequence (Ignore Save)")]
    public void ForceOpeningSequence()
    {
        forceOpeningSequence = true;
        skipOpeningSequence = false;
        StartCoroutine(StartNewGameSequence());
    }
    
    [ContextMenu("Skip Opening Sequence (Ignore Save)")]
    public void SkipOpeningSequence()
    {
        skipOpeningSequence = true;
        forceOpeningSequence = false;
        LoadMainGame();
    }
    
    [ContextMenu("Reset Debug Settings")]
    public void ResetDebugSettings()
    {
        forceOpeningSequence = false;
        skipOpeningSequence = false;
    }
    
    [ContextMenu("Toggle Skip Tutorial When Save Exists")]
    public void ToggleSkipTutorialWhenSaveExists()
    {
        skipTutorialWhenSaveExists = !skipTutorialWhenSaveExists;
    }
    
    [ContextMenu("Force Skip Tutorial (Load Now)")]
    public void ForceSkipTutorial()
    {
        LoadMainGame();
    }
    
    
    
    // Events
    public System.Action OnNewGameStarted;
    public System.Action OnNewGameCompleted;
}
