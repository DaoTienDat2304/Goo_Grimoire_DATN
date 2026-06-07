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
        // Chỉ setup dialogue data, không kích hoạt dialogue
        SetupOpeningDialogues();
        
        // Đảm bảo dialogue panel bị ẩn
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

        // Tạo dialogue sequences
        dialogueSystem.dialogueSequences.Clear();
        CreateProfessorDialogue();
        CreateSlimeDialogue();
        CreatePlayerDialogue();
        
    }
    
    private void CreateProfessorDialogue()
    {
        // Tạo dialogue sequence cho giáo sư
        DialogueSequence professorSequence = new DialogueSequence
        {
            dialogueName = dialogueData.professorSequenceName,
            lines = new List<DialogueLine>()
        };
        
        // Thêm các dòng hội thoại mẫu cho giáo sư
        if (dialogueData.professorLines.Count == 0)
        {
            // Tạo hội thoại mẫu nếu chưa có
            professorSequence.lines.Add(new DialogueLine
            {
                text = "Xin chào! Tôi là giáo sư nghiên cứu về Slime.",
                speakerName = "Giáo sư",
                speakerPortrait = professorPortrait
            });
            
            professorSequence.lines.Add(new DialogueLine
            {
                text = "Bạn đã sẵn sàng để khám phá thế giới Slime chưa?",
                speakerName = "Giáo sư",
                speakerPortrait = professorPortrait
            });
            
            professorSequence.lines.Add(new DialogueLine
            {
                text = "Hãy cùng tôi bắt đầu cuộc phiêu lưu tuyệt vời này!",
                speakerName = "Giáo sư",
                speakerPortrait = professorPortrait
            });
        }
        else
        {
            // Sử dụng hội thoại đã thiết lập
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
        
        // Thêm vào dialogue system
        dialogueSystem.AddDialogueSequence(professorSequence);
    }
    
    private void CreateSlimeDialogue()
    {
        // Tạo dialogue sequence cho slime
        DialogueSequence slimeSequence = new DialogueSequence
        {
            dialogueName = dialogueData.slimeSequenceName,
            lines = new List<DialogueLine>()
        };
        
        // Thêm các dòng hội thoại mẫu cho slime
        if (dialogueData.slimeLines.Count == 0)
        {
            // Tạo hội thoại mẫu nếu chưa có
            slimeSequence.lines.Add(new DialogueLine
            {
                text = "Pui pui! Chào bạn mới!",
                speakerName = "Slime",
                speakerPortrait = slimePortrait
            });
            
            slimeSequence.lines.Add(new DialogueLine
            {
                text = "Tôi là slime đầu tiên của bạn. Chúng ta sẽ là bạn tốt!",
                speakerName = "Slime",
                speakerPortrait = slimePortrait
            });
            
            slimeSequence.lines.Add(new DialogueLine
            {
                text = "Pui pui pui! Hãy chăm sóc tôi thật tốt nhé!",
                speakerName = "Slime",
                speakerPortrait = slimePortrait
            });
        }
        else
        {
            // Sử dụng hội thoại đã thiết lập
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
        
        // Thêm vào dialogue system
        dialogueSystem.AddDialogueSequence(slimeSequence);
    }
    
    private void CreatePlayerDialogue()
    {
        // Tạo dialogue sequence cho player
        DialogueSequence playerSequence = new DialogueSequence
        {
            dialogueName = dialogueData.playerSequenceName,
            lines = new List<DialogueLine>()
        };
        
        // Thêm các dòng hội thoại mẫu cho player
        if (dialogueData.playerLines.Count == 0)
        {
            // Tạo hội thoại mẫu nếu chưa có
            playerSequence.lines.Add(new DialogueLine
            {
                text = "Cảm ơn giáo sư! Tôi rất vui được học hỏi về thế giới Slime.",
                speakerName = "Player",
                speakerPortrait = playerPortrait
            });
            
            playerSequence.lines.Add(new DialogueLine
            {
                text = "Chào bạn Slime! Tôi sẽ chăm sóc bạn thật tốt.",
                speakerName = "Player",
                speakerPortrait = playerPortrait
            });
            
            playerSequence.lines.Add(new DialogueLine
            {
                text = "Hãy cùng nhau khám phá thế giới Slime tuyệt vời này!",
                speakerName = "Player",
                speakerPortrait = playerPortrait
            });
        }
        else
        {
            // Sử dụng hội thoại đã thiết lập
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
        
        // Thêm vào dialogue system
        dialogueSystem.AddDialogueSequence(playerSequence);
    }
    
    // Phương thức để cập nhật dialogue data từ inspector
    [ContextMenu("Update Dialogue Data")]
    public void UpdateDialogueData()
    {
        SetupOpeningDialogues();
    }
    
    // Phương thức để xóa tất cả dialogue sequences (để reset)
    [ContextMenu("Clear All Dialogues")]
    public void ClearAllDialogues()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.dialogueSequences.Clear();
            Debug.Log("All dialogue sequences cleared!");
        }
    }
}
