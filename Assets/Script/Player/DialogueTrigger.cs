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
        if (dialogueSystem == null)
        {
            dialogueSystem = FindAnyObjectByType<DialogueSystem>();
        }
        
        player = FindAnyObjectByType<MovePlayer>();
        
        if (player != null)
        {
            player.OnReachedTarget += OnPlayerReachedTarget;
        }
        
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
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

    private void Update()
    {
        if (!triggerOnClick || !MobileInput.TryGetPrimaryTap(out var tapPosition, true))
            return;

        if (IsTapOnTrigger(tapPosition))
            TriggerDialogue();
    }

    private bool IsTapOnTrigger(Vector2 screenPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        var collider2d = GetComponent<Collider2D>();
        if (collider2d != null)
        {
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            return collider2d.OverlapPoint(worldPosition);
        }

        var collider3d = GetComponent<Collider>();
        if (collider3d == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        return collider3d.Raycast(ray, out _, mainCamera.farClipPlane);
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
    
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
    
    public bool HasTriggered()
    {
        return hasTriggered;
    }
    
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

