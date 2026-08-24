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
        InitializeFirstTimeResources();
        
        if (player == null)
        {
            player = FindAnyObjectByType<MovePlayer>();
        }
        
        if (dialogueSystem == null)
        {
            dialogueSystem = FindAnyObjectByType<DialogueSystem>();
        }
        
        if (dialogueTriggers.Count == 0)
        {
            dialogueTriggers.Add(FindAnyObjectByType<DialogueTrigger>());
        }
        
        if (player != null && playerStartPosition != null)
        {
            player.transform.position = playerStartPosition.position;
        }
        
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
        
        UpdateUI();
        
        currentTargetIndex = -1;
    }
    private void InitializeFirstTimeResources()
    {
        bool isFirstTime = CloudSaveProvider.Instance == null
            || !CloudSaveProvider.Instance.HasCloudSave;

        if (isFirstTime)
        {
            Debug.Log("First run: init resources");
            
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.SetCurrency(CurrencyType.Coins, 50);
                CurrencyManager.Instance.SetCurrency(CurrencyType.Gems, 0);
                Debug.Log("Initialized: 50 Coins, 0 Gems");
            }
            
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SetResource(ResourceType.Marshmallow, 5);
                Debug.Log("Initialized: 5 Marshmallow");
            }
        }
    }
    
    private void OnPlayerReachedTarget(Transform target)
    {
        int targetIndex = targetPoints.IndexOf(target);
        if (targetIndex != -1)
        {
            currentTargetIndex = targetIndex;
            OnTargetReached?.Invoke(targetIndex);
            
            if (autoStartDialogue)
            {
                StartCoroutine(StartDialogueAfterDelay());
            }
        }
    }
    
    private System.Collections.IEnumerator StartDialogueAfterDelay()
    {
        yield return new WaitForSeconds(dialogueStartDelay);
        
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
        Debug.Log("Player started moving");
    }
    
    private void OnPlayerStoppedMoving()
    {
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
        if (IsMoveToNextInputDown() && allowPlayerMovement && isSceneActive)
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

    private bool IsMoveToNextInputDown()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || MobileInput.TryGetPrimaryTap(out _, true);
    }
    
    void OnDestroy()
    {
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

