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
    [Tooltip("Skip new-game check: always run opening")]
    public bool forceOpeningSequence = false;
    
    private bool hasStartedOpeningSequence = false;
    private int currentDialogueStep = 0;
    
    // Events
    public System.Action OnOpeningSequenceStarted;
    public System.Action OnOpeningSequenceCompleted;
    
    void Start()
    {
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
        
        StartCoroutine(OpeningSequenceCoroutine());
    }
    
    private IEnumerator OpeningSequenceCoroutine()
    {
        yield return StartCoroutine(ProfessorSequence());
        
        yield return StartCoroutine(SlimeSequence());
        
        yield return StartCoroutine(PlayerSequence());
        
        OnOpeningSequenceCompleted?.Invoke();
    }
    
    private IEnumerator ProfessorSequence()
    {
        if (professorNPC != null && professorPosition1 != null)
        {
            professorNPC.MoveToTarget(professorPosition1);
            
            yield return new WaitUntil(() => professorNPC.IsAtTarget());
            yield return new WaitForSeconds(professorMoveDelay);
        }
        
        if (professorDialogueTrigger != null)
        {
            professorDialogueTrigger.TriggerDialogue();
            
            yield return new WaitUntil(() => !dialogueSystem.IsDialogueActive());
            yield return new WaitForSeconds(dialogueDelay);
        }
        
        currentDialogueStep = 1;
    }
    
    private IEnumerator SlimeSequence()
    {
        if (slimeNPC != null && slimePosition2 != null)
        {
            slimeNPC.MoveToTarget(slimePosition2);
            
            yield return new WaitUntil(() => slimeNPC.IsAtTarget());
            yield return new WaitForSeconds(slimeMoveDelay);
        }
        
        if (slimeDialogueTrigger != null)
        {
            slimeDialogueTrigger.TriggerDialogue();
            
            yield return new WaitUntil(() => !dialogueSystem.IsDialogueActive());
            yield return new WaitForSeconds(dialogueDelay);
        }
        
        currentDialogueStep = 2;
    }
    
    private IEnumerator PlayerSequence()
    {
        if (player != null && playerPosition3 != null)
        {
            player.MoveToTarget(playerPosition3);
            
            yield return new WaitUntil(() => player.IsAtTarget());
            yield return new WaitForSeconds(playerMoveDelay);
        }
        
        if (playerDialogueTrigger != null)
        {
            playerDialogueTrigger.TriggerDialogue();
            
            yield return new WaitUntil(() => !dialogueSystem.IsDialogueActive());
        }
        
        currentDialogueStep = 3;
    }
    
    public void ResetOpeningSequence()
    {
        hasStartedOpeningSequence = false;
        currentDialogueStep = 0;
        
        if (professorDialogueTrigger != null) professorDialogueTrigger.ResetTrigger();
        if (slimeDialogueTrigger != null) slimeDialogueTrigger.ResetTrigger();
        if (playerDialogueTrigger != null) playerDialogueTrigger.ResetTrigger();
    }
    
    public void ForceStartOpeningSequence()
    {
        isNewGame = true;
        StartOpeningSequence();
    }
    
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
