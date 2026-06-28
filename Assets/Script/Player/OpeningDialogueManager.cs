using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OpeningDialogueManager : MonoBehaviour
{
    [Header("NPC References")]
    public MovePlayer professorNPC;
    public MovePlayer slimeNPC;
    public MovePlayer player;
    
    [Header("Position Targets")]
    public Transform professorPosition1;
    public Transform slimePosition2;
    public Transform playerPosition3;
    
    [Header("Dialogue System")]
    public DialogueSystem dialogueSystem;
    
    [Header("Dialogue Triggers")]
    public DialogueTrigger professorDialogueTrigger;
    public DialogueTrigger slimeDialogueTrigger;
    public DialogueTrigger playerDialogueTrigger;
    
    [Header("Timing Settings")]
    public float professorMoveDelay = 0.5f;
    public float slimeMoveDelay = 1f;
    public float playerMoveDelay = 1f;
    public float dialogueDelay = 0.5f;
    
    [Header("New Game Check")]
    public bool isNewGame = false;
    
    [Header("Debug Settings")]
    [Tooltip("Bỏ qua kiểm tra new game - luôn chạy opening sequence")]
    public bool forceOpeningSequence = false;
    
    private bool hasStartedOpeningSequence = false;
    private int currentDialogueStep = 0;
    
    // Events
    public System.Action OnOpeningSequenceStarted;
    public System.Action OnOpeningSequenceCompleted;
    
    void Start()
    {
        // Kiểm tra xem có phải new game không
        CheckIfNewGame();
        
        if ((isNewGame || forceOpeningSequence) && !hasStartedOpeningSequence)
        {
            StartOpeningSequence();
        }
    }
    
    private void CheckIfNewGame()
    {
        if (CloudSaveProvider.Instance != null && CloudSaveProvider.Instance.CloudCheckDone)
            isNewGame = !CloudSaveProvider.Instance.HasCloudSave;
        else
            isNewGame = true; // fallback
    }
    
    public void StartOpeningSequence()
    {
        if (hasStartedOpeningSequence) return;
        
        hasStartedOpeningSequence = true;
        OnOpeningSequenceStarted?.Invoke();
        
        // Bắt đầu sequence
        StartCoroutine(OpeningSequenceCoroutine());
    }
    
    private IEnumerator OpeningSequenceCoroutine()
    {
        // Bước 1: Giáo sư di chuyển đến pos1 và kích hoạt hội thoại
        yield return StartCoroutine(ProfessorSequence());
        
        // Bước 2: Slime di chuyển đến pos2 và kích hoạt hội thoại
        yield return StartCoroutine(SlimeSequence());
        
        // Bước 3: Player di chuyển đến pos3 và kích hoạt hội thoại
        yield return StartCoroutine(PlayerSequence());
        
        // Hoàn thành sequence
        OnOpeningSequenceCompleted?.Invoke();
    }
    
    private IEnumerator ProfessorSequence()
    {
        // Di chuyển giáo sư đến pos1
        if (professorNPC != null && professorPosition1 != null)
        {
            professorNPC.MoveToTarget(professorPosition1);
            
            // Chờ giáo sư đến vị trí
            yield return new WaitUntil(() => professorNPC.IsAtTarget());
            yield return new WaitForSeconds(professorMoveDelay);
        }
        
        // Kích hoạt hội thoại của giáo sư
        if (professorDialogueTrigger != null)
        {
            professorDialogueTrigger.TriggerDialogue();
            
            // Chờ hội thoại kết thúc
            yield return new WaitUntil(() => !dialogueSystem.IsDialogueActive());
            yield return new WaitForSeconds(dialogueDelay);
        }
        
        currentDialogueStep = 1;
    }
    
    private IEnumerator SlimeSequence()
    {
        // Di chuyển slime đến pos2
        if (slimeNPC != null && slimePosition2 != null)
        {
            slimeNPC.MoveToTarget(slimePosition2);
            
            // Chờ slime đến vị trí
            yield return new WaitUntil(() => slimeNPC.IsAtTarget());
            yield return new WaitForSeconds(slimeMoveDelay);
        }
        
        // Kích hoạt hội thoại của slime
        if (slimeDialogueTrigger != null)
        {
            slimeDialogueTrigger.TriggerDialogue();
            
            // Chờ hội thoại kết thúc
            yield return new WaitUntil(() => !dialogueSystem.IsDialogueActive());
            yield return new WaitForSeconds(dialogueDelay);
        }
        
        currentDialogueStep = 2;
    }
    
    private IEnumerator PlayerSequence()
    {
        // Di chuyển player đến pos3
        if (player != null && playerPosition3 != null)
        {
            player.MoveToTarget(playerPosition3);
            
            // Chờ player đến vị trí
            yield return new WaitUntil(() => player.IsAtTarget());
            yield return new WaitForSeconds(playerMoveDelay);
        }
        
        // Kích hoạt hội thoại của player
        if (playerDialogueTrigger != null)
        {
            playerDialogueTrigger.TriggerDialogue();
            
            // Chờ hội thoại kết thúc
            yield return new WaitUntil(() => !dialogueSystem.IsDialogueActive());
        }
        
        currentDialogueStep = 3;
    }
    
    // Phương thức để reset sequence (có thể gọi từ bên ngoài)
    public void ResetOpeningSequence()
    {
        hasStartedOpeningSequence = false;
        currentDialogueStep = 0;
        
        // Reset các dialogue triggers
        if (professorDialogueTrigger != null) professorDialogueTrigger.ResetTrigger();
        if (slimeDialogueTrigger != null) slimeDialogueTrigger.ResetTrigger();
        if (playerDialogueTrigger != null) playerDialogueTrigger.ResetTrigger();
    }
    
    // Phương thức để bắt buộc bắt đầu sequence (có thể gọi từ menu new game)
    public void ForceStartOpeningSequence()
    {
        isNewGame = true;
        StartOpeningSequence();
    }
    
    // Phương thức để kiểm tra trạng thái
    public bool IsOpeningSequenceActive()
    {
        return hasStartedOpeningSequence && currentDialogueStep < 3;
    }
    
    public int GetCurrentDialogueStep()
    {
        return currentDialogueStep;
    }
    
    // Debug methods
    [ContextMenu("Test Opening Sequence")]
    public void TestOpeningSequence()
    {
        ForceStartOpeningSequence();
    }
    
    [ContextMenu("Force Start Opening Sequence")]
    public void ForceStartOpeningSequenceDebug()
    {
        ForceStartOpeningSequence();
    }
    
    [ContextMenu("Reset Sequence")]
    public void ResetSequence()
    {
        ResetOpeningSequence();
    }
}
