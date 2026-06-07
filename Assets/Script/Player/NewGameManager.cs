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
    [Tooltip("Bỏ qua kiểm tra new game - luôn chạy opening sequence")]
    public bool forceOpeningSequence = false;
    [Tooltip("Bỏ qua kiểm tra new game - không chạy opening sequence")]
    public bool skipOpeningSequence = false;
    
    [Header("Tutorial Settings")]
    [Tooltip("Bỏ qua tutorial khi có save file")]
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
        // Kiểm tra xem có phải new game không
        bool isNewGame = IsNewGame();
        
        // Nếu đã có save file, bỏ qua hoàn toàn tutorial này
        if (!isNewGame && skipTutorialWhenSaveExists)
        {
            LoadMainGame(); // Nhảy ngay lập tức, bỏ qua tutorial
            return;
        }
        
        // Tự động tìm các component nếu chưa được gán
        if (openingDialogueManager == null)
        {
            openingDialogueManager = FindAnyObjectByType<OpeningDialogueManager>();
        }
        
        if (openingDialogueSetup == null)
        {
            openingDialogueSetup = FindAnyObjectByType<OpeningDialogueSetup>();
        }
        
        // Kiểm tra xem có phải new game không
        CheckAndStartNewGame();
    }
    
    private void CheckAndStartNewGame()
    {
        // Kiểm tra xem có phải new game không
        bool isNewGame = IsNewGame();
        
        // Kiểm tra debug settings trước
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
        
        // Logic bình thường
        if (isNewGame && !skipOpeningDialogue)
        {
            StartCoroutine(StartNewGameSequence());
        }
        else if (isNewGame && skipOpeningDialogue)
        {
            // Bỏ qua hội thoại mở đầu, chuyển thẳng vào game
            LoadMainGame();
        }
        // Logic này đã được xử lý trong Start() rồi
    }
    
    private bool IsNewGame()
    {
        if (CloudSaveProvider.Instance != null && CloudSaveProvider.Instance.CloudCheckDone)
            return !CloudSaveProvider.Instance.HasCloudSave;
        return true; // fallback: chưa check xong → coi như new game
    }
    
    private IEnumerator StartNewGameSequence()
    {
        // Thiết lập dialogue nếu chưa có
        if (openingDialogueSetup != null)
        {
            openingDialogueSetup.SetupOpeningDialogues();
            yield return new WaitForEndOfFrame();
        }
        
        // Bắt đầu hội thoại mở đầu
        if (openingDialogueManager != null)
        {
            openingDialogueManager.ForceStartOpeningSequence();
            
            // Chờ hội thoại hoàn thành
            yield return new WaitUntil(() => !openingDialogueManager.IsOpeningSequenceActive());
        }
        
        // Có thể thêm logic khác ở đây sau khi hội thoại kết thúc
        CompleteNewGame();
    }
    
    private void CompleteNewGame()
    {
        // Có thể thêm logic khởi tạo game mới ở đây
        // Ví dụ: Unlock quest đầu tiên, tạo slime đầu tiên, etc.
        InitializeNewGameData();
        
        // Gọi event
        OnNewGameCompleted?.Invoke();
    }
    
    private void InitializeNewGameData()
    {
        // Khởi tạo dữ liệu cho game mới
        // Ví dụ: Tạo slime đầu tiên, unlock quest tutorial, etc.
        
        // Có thể gọi từ QuestManager để unlock quest đầu tiên
        if (QuestManager.Instance != null)
        {
            // Logic để unlock quest đầu tiên
        }
        
        // Có thể gọi từ BreedingManager để tạo slime đầu tiên
        if (BreedingManager.Instance != null)
        {
            // Logic để tạo slime đầu tiên
        }
    }
    
    // Phương thức public để bắt đầu new game từ menu
    public async void StartNewGame()
    {
        if (resetSaveData)
        {
            ResetSaveData();
        }
        
        // Reload scene để bắt đầu new game
        await SceneLoader.LoadSceneWithLoading(SceneManager.GetActiveScene().name);
    }
    
    // Phương thức để reset save data
    private void ResetSaveData()
    {
        // Cloud-only: không còn local file để xóa
        // Reset PlayerPrefs nếu cần
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[NewGame] Save data đã được reset (PlayerPrefs cleared).");
    }
    
    // Phương thức để load main game
    public async void LoadMainGame()
    {
        if (!string.IsNullOrEmpty(mainGameSceneName))
        {
            await SceneLoader.LoadSceneWithLoading(mainGameSceneName);
        }
    }
    
    // Phương thức để load adventure scene
    public async void LoadAdventureScene()
    {
        if (!string.IsNullOrEmpty(adventureSceneName))
        {
            await SceneLoader.LoadSceneWithLoading(adventureSceneName);
        }
    }
    
    // Phương thức để bỏ qua hội thoại mở đầu
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
