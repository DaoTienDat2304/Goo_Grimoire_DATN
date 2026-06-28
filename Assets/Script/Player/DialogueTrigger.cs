using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public DialogueSystem dialogueSystem;
    public string dialogueSequenceName = "";
    public int dialogueSequenceIndex = 0;
    public bool useSequenceName = true;
    
    [Header("Trigger Settings")]
    public bool triggerOnPlayerReach = true;
    public bool triggerOnClick = false;
    public bool triggerOnce = true;
    public float triggerDelay = 0f;
    
    [Header("Visual Feedback")]
    public GameObject highlightObject;
    public bool showHighlightOnHover = true;
    
    private bool hasTriggered = false;
    private bool isPlayerNearby = false;
    private MovePlayer player;
    
    void Start()
    {
        // Tìm DialogueSystem nếu chưa được gán
        if (dialogueSystem == null)
        {
            dialogueSystem = FindAnyObjectByType<DialogueSystem>();
        }
        
        // Tìm Player
        player = FindAnyObjectByType<MovePlayer>();
        
        // Đăng ký sự kiện từ Player
        if (player != null)
        {
            player.OnReachedTarget += OnPlayerReachedTarget;
        }
        
        // Ẩn highlight ban đầu
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        // Hủy đăng ký sự kiện
        if (player != null)
        {
            player.OnReachedTarget -= OnPlayerReachedTarget;
        }
    }
    
    private void OnPlayerReachedTarget(Transform target)
    {
        if (triggerOnPlayerReach && target == transform)
        {
            TriggerDialogue();
        }
    }
    
    public void TriggerDialogue()
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }
        
        if (dialogueSystem == null)
        {
            Debug.LogWarning("DialogueSystem not found!");
            return;
        }
        
        if (triggerDelay > 0)
        {
            Invoke(nameof(StartDialogue), triggerDelay);
        }
        else
        {
            StartDialogue();
        }
        
        if (triggerOnce)
        {
            hasTriggered = true;
        }
    }
    
    private void StartDialogue()
    {
        if (useSequenceName && !string.IsNullOrEmpty(dialogueSequenceName))
        {
            dialogueSystem.StartDialogue(dialogueSequenceName);
        }
        else
        {
            dialogueSystem.StartDialogue(dialogueSequenceIndex);
        }
    }
    
    void OnMouseDown()
    {
        if (triggerOnClick)
        {
            TriggerDialogue();
        }
    }
    
    void OnMouseEnter()
    {
        if (showHighlightOnHover && highlightObject != null)
        {
            highlightObject.SetActive(true);
        }
    }
    
    void OnMouseExit()
    {
        if (showHighlightOnHover && highlightObject != null)
        {
            highlightObject.SetActive(false);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (triggerOnPlayerReach)
            {
                TriggerDialogue();
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }
    
    // Phương thức để reset trigger (có thể gọi từ bên ngoài)
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
    
    // Phương thức để kiểm tra xem đã trigger chưa
    public bool HasTriggered()
    {
        return hasTriggered;
    }
    
    // Phương thức để thiết lập dialogue sequence mới
    public void SetDialogueSequence(string sequenceName)
    {
        dialogueSequenceName = sequenceName;
        useSequenceName = true;
    }
    
    public void SetDialogueSequence(int sequenceIndex)
    {
        dialogueSequenceIndex = sequenceIndex;
        useSequenceName = false;
    }
}

