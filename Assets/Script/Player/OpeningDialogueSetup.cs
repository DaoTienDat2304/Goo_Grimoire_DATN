using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OpeningDialogueData
{
    [Header("Professor Dialogue")]
    public string professorSequenceName = "Professor_Opening";
    public List<DialogueLine> professorLines = new List<DialogueLine>();
    
    [Header("Slime Dialogue")]
    public string slimeSequenceName = "Slime_Opening";
    public List<DialogueLine> slimeLines = new List<DialogueLine>();
    
    [Header("Player Dialogue")]
    public string playerSequenceName = "Player_Opening";
    public List<DialogueLine> playerLines = new List<DialogueLine>();
}

public class OpeningDialogueSetup : MonoBehaviour
{
    [Header("Dialogue System")]
    public DialogueSystem dialogueSystem;
    
    [Header("Dialogue Data")]
    public OpeningDialogueData dialogueData;
    
    [Header("NPC Sprites")]
    public Sprite professorPortrait;
    public Sprite slimePortrait;
    public Sprite playerPortrait;
    
    void Start()
    {
        SetupOpeningDialogues();
        
        if (dialogueSystem != null && dialogueSystem.dialoguePanel != null)
        {
            dialogueSystem.dialoguePanel.SetActive(false);
        }
    }
    
    [ContextMenu("Setup Opening Dialogues")]
    public void SetupOpeningDialogues()
    {
        if (dialogueSystem == null)
        {
            dialogueSystem = FindAnyObjectByType<DialogueSystem>();
            if (dialogueSystem == null)
            {
                Debug.LogError("DialogueSystem not found! Please assign it in the inspector.");
                return;
            }
        }

        dialogueSystem.dialogueSequences.Clear();
        CreateProfessorDialogue();
        CreateSlimeDialogue();
        CreatePlayerDialogue();
        
    }
    
    private void CreateProfessorDialogue()
    {
        DialogueSequence professorSequence = new DialogueSequence
        {
            dialogueName = dialogueData.professorSequenceName,
            lines = new List<DialogueLine>()
        };
        
        if (dialogueData.professorLines.Count == 0)
        {
            professorSequence.lines.Add(new DialogueLine
            {
                text = "Hi! I study Slimes.",
                speakerName = "Professor",
                speakerPortrait = professorPortrait
            });
            
            professorSequence.lines.Add(new DialogueLine
            {
                text = "Ready to explore Slime world?",
                speakerName = "Professor",
                speakerPortrait = professorPortrait
            });
            
            professorSequence.lines.Add(new DialogueLine
            {
                text = "Let us begin!",
                speakerName = "Professor",
                speakerPortrait = professorPortrait
            });
        }
        else
        {
            foreach (var line in dialogueData.professorLines)
            {
                professorSequence.lines.Add(new DialogueLine
                {
                    text = line.text,
                    speakerName = line.speakerName,
                    speakerPortrait = line.speakerPortrait ?? professorPortrait
                });
            }
        }
        
        dialogueSystem.AddDialogueSequence(professorSequence);
    }
    
    private void CreateSlimeDialogue()
    {
        DialogueSequence slimeSequence = new DialogueSequence
        {
            dialogueName = dialogueData.slimeSequenceName,
            lines = new List<DialogueLine>()
        };
        
        if (dialogueData.slimeLines.Count == 0)
        {
            slimeSequence.lines.Add(new DialogueLine
            {
                text = "Pui pui! Hi!",
                speakerName = "Slime",
                speakerPortrait = slimePortrait
            });
            
            slimeSequence.lines.Add(new DialogueLine
            {
                text = "I am your first slime. We will be friends!",
                speakerName = "Slime",
                speakerPortrait = slimePortrait
            });
            
            slimeSequence.lines.Add(new DialogueLine
            {
                text = "Pui pui! Take care of me!",
                speakerName = "Slime",
                speakerPortrait = slimePortrait
            });
        }
        else
        {
            foreach (var line in dialogueData.slimeLines)
            {
                slimeSequence.lines.Add(new DialogueLine
                {
                    text = line.text,
                    speakerName = line.speakerName,
                    speakerPortrait = line.speakerPortrait ?? slimePortrait
                });
            }
        }
        
        dialogueSystem.AddDialogueSequence(slimeSequence);
    }
    
    private void CreatePlayerDialogue()
    {
        DialogueSequence playerSequence = new DialogueSequence
        {
            dialogueName = dialogueData.playerSequenceName,
            lines = new List<DialogueLine>()
        };
        
        if (dialogueData.playerLines.Count == 0)
        {
            playerSequence.lines.Add(new DialogueLine
            {
                text = "Thanks, Professor! I want to learn.",
                speakerName = "Player",
                speakerPortrait = playerPortrait
            });
            
            playerSequence.lines.Add(new DialogueLine
            {
                text = "Hi Slime! I will care for you.",
                speakerName = "Player",
                speakerPortrait = playerPortrait
            });
            
            playerSequence.lines.Add(new DialogueLine
            {
                text = "Let us explore Slime world!",
                speakerName = "Player",
                speakerPortrait = playerPortrait
            });
        }
        else
        {
            foreach (var line in dialogueData.playerLines)
            {
                playerSequence.lines.Add(new DialogueLine
                {
                    text = line.text,
                    speakerName = line.speakerName,
                    speakerPortrait = line.speakerPortrait ?? playerPortrait
                });
            }
        }
        
        dialogueSystem.AddDialogueSequence(playerSequence);
    }
    
    [ContextMenu("Update Dialogue Data")]
    public void UpdateDialogueData()
    {
        SetupOpeningDialogues();
    }
    
    [ContextMenu("Clear All Dialogues")]
    public void ClearAllDialogues()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.dialogueSequences.Clear();
        }
    }
}
