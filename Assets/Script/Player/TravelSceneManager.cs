using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TravelSceneManager : MonoBehaviour
{
    [Header("Player Management")]
    public MovePlayer player;
    public Transform playerStartPosition;
    
    [Header("Dialogue System")]
    public DialogueSystem dialogueSystem;
    
    [Header("Target Points")]
    public List<Transform> targetPoints = new List<Transform>();
    public List<DialogueTrigger> dialogueTriggers = new List<DialogueTrigger>();
    
    [Header("Scene Settings")]
    public bool autoStartDialogue = true;
    public float dialogueStartDelay = 1f;
    public bool allowPlayerMovement = true;
    
    [Header("UI References")]
    public GameObject movementUI;
    public GameObject dialogueUI;
    
    private int currentTargetIndex = -1;
    private bool isSceneActive = true;
    
    public System.Action<int> OnTargetReached;
    public System.Action OnAllTargetsCompleted;
    public System.Action OnDialogueStarted;
    public System.Action OnDialogueEnded;
    
    void Start()
    {
        InitializeScene();
    }
    
    private void InitializeScene()
    {
        // Kiểm tra và khởi tạo tài nguyên cho lần đầu chạy game
        InitializeFirstTimeResources();
        
        // Tìm các component nếu chưa được gán
        if (player == null)
        {
            player = FindAnyObjectByType<MovePlayer>();
        }
        
        if (dialogueSystem == null)
        {
            dialogueSystem = FindAnyObjectByType<DialogueSystem>();
        }
        
        // Tìm tất cả DialogueTrigger trong scene
        if (dialogueTriggers.Count == 0)
        {
            dialogueTriggers.Add(FindAnyObjectByType<DialogueTrigger>());
        }
        
        // Đặt vị trí bắt đầu cho player
        if (player != null && playerStartPosition != null)
        {
            player.transform.position = playerStartPosition.position;
        }
        
        // Đăng ký sự kiện
        if (player != null)
        {
            player.OnReachedTarget += OnPlayerReachedTarget;
            player.OnStartedMoving += OnPlayerStartedMoving;
            player.OnStoppedMoving += OnPlayerStoppedMoving;
        }
        
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueStarted += OnDialogueStarted;
            dialogueSystem.OnDialogueEnded += OnDialogueEnded;
        }
        
        // Cập nhật UI
        UpdateUI();
        
        // Đảm bảo bắt đầu trước Target đầu tiên để lần nhấn đầu đi tới Target1
        currentTargetIndex = -1;
    }
    
    /// <summary>
    /// Khởi tạo tài nguyên cho lần đầu chạy game (nếu chưa có cloud save)
    /// </summary>
    private void InitializeFirstTimeResources()
    {
        bool isFirstTime = CloudSaveProvider.Instance == null
            || !CloudSaveProvider.Instance.HasCloudSave;

        if (isFirstTime)
        {
            Debug.Log("Lần đầu chạy game - Khởi tạo tài nguyên ban đầu");
            
            // Khởi tạo Currency
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.SetCurrency(CurrencyType.Coins, 50);
                CurrencyManager.Instance.SetCurrency(CurrencyType.Gems, 0);
                Debug.Log("Đã khởi tạo: 50 Coins, 0 Gems");
            }
            
            // Khởi tạo Resource
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SetResource(ResourceType.Marshmallow, 5);
                Debug.Log("Đã khởi tạo: 5 Marshmallow");
            }
        }
    }
    
    private void OnPlayerReachedTarget(Transform target)
    {
        // Tìm index của target được đến
        int targetIndex = targetPoints.IndexOf(target);
        if (targetIndex != -1)
        {
            currentTargetIndex = targetIndex;
            OnTargetReached?.Invoke(targetIndex);
            
            // Tự động kích hoạt dialogue nếu được bật
            if (autoStartDialogue)
            {
                StartCoroutine(StartDialogueAfterDelay());
            }
        }
    }
    
    private System.Collections.IEnumerator StartDialogueAfterDelay()
    {
        yield return new WaitForSeconds(dialogueStartDelay);
        
        // Tìm DialogueTrigger tương ứng với target hiện tại
        if (currentTargetIndex < dialogueTriggers.Count)
        {
            DialogueTrigger trigger = dialogueTriggers[currentTargetIndex];
            if (trigger != null)
            {
                trigger.TriggerDialogue();
            }
        }
    }
    
    private void OnPlayerStartedMoving()
    {
        // Có thể thêm logic khi player bắt đầu di chuyển
        Debug.Log("Player started moving");
    }
    
    private void OnPlayerStoppedMoving()
    {
        // Có thể thêm logic khi player dừng di chuyển
        Debug.Log("Player stopped moving");
    }
    
    
    
    public void MoveToNextTarget()
    {
        if (!allowPlayerMovement || player == null) return;
        
        int nextIndex = currentTargetIndex + 1;
        if (nextIndex < targetPoints.Count)
        {
            player.MoveToTarget(targetPoints[nextIndex]);
        }
    }
    
    public void MoveToTarget(int targetIndex)
    {
        if (!allowPlayerMovement || player == null) return;
        
        if (targetIndex >= 0 && targetIndex < targetPoints.Count)
        {
            player.MoveToTarget(targetPoints[targetIndex]);
        }
    }
    
    public void MoveToTarget(Transform target)
    {
        if (!allowPlayerMovement || player == null) return;
        
        player.MoveToTarget(target);
    }
    
    public void StartDialogueAtTarget(int targetIndex)
    {
        if (targetIndex >= 0 && targetIndex < dialogueTriggers.Count)
        {
            DialogueTrigger trigger = dialogueTriggers[targetIndex];
            if (trigger != null)
            {
                trigger.TriggerDialogue();
            }
        }
    }
    
    public void StartDialogueAtTarget(string sequenceName)
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue(sequenceName);
        }
    }
    
    public void SetSceneActive(bool active)
    {
        isSceneActive = active;
        allowPlayerMovement = active;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (movementUI != null)
        {
            movementUI.SetActive(allowPlayerMovement && isSceneActive);
        }
        
        if (dialogueUI != null)
        {
            dialogueUI.SetActive(dialogueSystem != null && dialogueSystem.IsDialogueActive());
        }
    }
    
    public void ResetScene()
    {
        currentTargetIndex = -1;
        
        if (player != null && playerStartPosition != null)
        {
            player.transform.position = playerStartPosition.position;
            player.StopMovement();
        }
        
        // Reset tất cả dialogue triggers
        foreach (DialogueTrigger trigger in dialogueTriggers)
        {
            if (trigger != null)
            {
                trigger.ResetTrigger();
            }
        }
        
        UpdateUI();
    }
    
    void Update()
    {
        // Input handling
        if (Input.GetKeyDown(KeyCode.Space) && allowPlayerMovement && isSceneActive)
        {
            MoveToNextTarget();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetScene();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            StartCoroutine(SceneLoader.LoadSceneWithLoadingCoroutine(currentIndex + 1));
        }
    }
    
    void OnDestroy()
    {
        // Hủy đăng ký sự kiện
        if (player != null)
        {
            player.OnReachedTarget -= OnPlayerReachedTarget;
            player.OnStartedMoving -= OnPlayerStartedMoving;
            player.OnStoppedMoving -= OnPlayerStoppedMoving;
        }
        
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueStarted -= OnDialogueStarted;
            dialogueSystem.OnDialogueEnded -= OnDialogueEnded;
        }
    }
}

