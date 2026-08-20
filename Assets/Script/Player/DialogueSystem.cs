using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    [TextArea(3, 5)]
    public string text;
    public string speakerName;
    public Sprite speakerPortrait;
}

[System.Serializable]
public class DialogueSequence
{
    public string dialogueName;
    public List<DialogueLine> lines;
    public bool isCompleted = false;
}

public class DialogueSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerNameText;
    public Image speakerPortrait;
    public Button continueButton;
    public Button skipButton;
    
    [Header("Dialogue Settings")]
    public float textSpeed = 0.05f;
    public bool autoAdvance = false;
    public float autoAdvanceDelay = 2f;
    
    [Header("Scene Transition")]
    [Tooltip("Next scene name")]
    public string nextSceneName = "MainGame";
    [Tooltip("Next scene index")]
    public int nextSceneIndex = 2;
    [Tooltip("Use scene index")]
    public bool useSceneIndex = true;
    [Tooltip("Change scene after dialogue")]
    public bool enableSceneTransition = true;
    [Tooltip("Auto scene change")]
    public bool autoTransitionOnEnd = false;
    [Tooltip("Scene delay (s)")]
    public float transitionDelay = 1f;
    
    [Header("Dialogue Data")]
    public List<DialogueSequence> dialogueSequences = new List<DialogueSequence>();
    
    private int currentSequenceIndex = 0;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    
    public System.Action OnDialogueStarted;
    public System.Action OnDialogueEnded;
    public System.Action<string> OnDialogueSequenceCompleted;

    [Header("Wild Slime Tutorial")]
    public GameObject catcherCollider;
    public GameObject tutorial;
    public int c=0;
    
    void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueDialogue);
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipDialogue);
        }
    }
    
    public void StartDialogue(string sequenceName = null)
    {
        if (dialogueSequences.Count == 0)
        {
            Debug.LogWarning("No dialogue sequences available!");
            return;
        }
        
        if (!string.IsNullOrEmpty(sequenceName))
        {
            currentSequenceIndex = dialogueSequences.FindIndex(seq => seq.dialogueName == sequenceName);
            if (currentSequenceIndex == -1)
            {
                Debug.LogWarning($"Dialogue sequence '{sequenceName}' not found!");
                currentSequenceIndex = 0;
            }
        }
        else
        {
            currentSequenceIndex = 0;
        }
        
        currentLineIndex = 0;
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
        
        OnDialogueStarted?.Invoke();
        DisplayCurrentLine();
    }
    
    public void StartDialogue(int sequenceIndex)
    {
        if (sequenceIndex < 0 || sequenceIndex >= dialogueSequences.Count)
        {
            Debug.LogWarning($"Invalid dialogue sequence index: {sequenceIndex}");
            return;
        }
        
        currentSequenceIndex = sequenceIndex;
        currentLineIndex = 0;
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
        
        OnDialogueStarted?.Invoke();
        DisplayCurrentLine();
    }
    
    private void DisplayCurrentLine()
    {
        if (currentSequenceIndex >= dialogueSequences.Count || 
            currentLineIndex >= dialogueSequences[currentSequenceIndex].lines.Count)
        {
            EndDialogue();
            return;
        }
        
        DialogueLine currentLine = dialogueSequences[currentSequenceIndex].lines[currentLineIndex];
        
        if (speakerNameText != null)
        {
            speakerNameText.text = currentLine.speakerName;
        }
        
        if (speakerPortrait != null && currentLine.speakerPortrait != null)
        {
            speakerPortrait.sprite = currentLine.speakerPortrait;
        }
        
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(currentLine.text));
    }
    
    private System.Collections.IEnumerator TypeText(string text)
    {
        isTyping = true;
        
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
        else
        {
            yield break;
        }
        
        foreach (char letter in text)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
        
        isTyping = false;
        
        if (autoAdvance)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvance());
        }
    }
    
    private System.Collections.IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);
        ContinueDialogue();
    }
    
    public void ContinueDialogue()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            
            DialogueLine currentLine = dialogueSequences[currentSequenceIndex].lines[currentLineIndex];
            dialogueText.text = currentLine.text;
            isTyping = false;
            
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
            }
            return;
        }
        
        currentLineIndex++;
        DisplayCurrentLine();
    }
    
    public void SkipDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
        }
        
        EndDialogue();
    }
    
    private void EndDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        c++;
        
        if (currentSequenceIndex < dialogueSequences.Count)
        {
            dialogueSequences[currentSequenceIndex].isCompleted = true;
            OnDialogueSequenceCompleted?.Invoke(dialogueSequences[currentSequenceIndex].dialogueName);
        }
        
        OnDialogueEnded?.Invoke();
        
        if (enableSceneTransition && autoTransitionOnEnd)
        {
            StartCoroutine(TransitionToNextScene());
        }
    }
    
    private System.Collections.IEnumerator TransitionToNextScene()
    {
        yield return new WaitForSeconds(transitionDelay);
        LoadNextScene();
    }
    
    public void AddDialogueSequence(DialogueSequence sequence)
    {
        dialogueSequences.Add(sequence);
    }
    
    public bool IsDialogueActive()
    {
        return dialoguePanel != null && dialoguePanel.activeInHierarchy;
    }
    
    public async void LoadNextScene()
    {
        if (useSceneIndex)
        {
            await SceneLoader.LoadSceneWithLoading(nextSceneIndex);
        }
        else
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                await SceneLoader.LoadSceneWithLoading(nextSceneName);
            }
        }
    }
    
    [ContextMenu("Test Scene Transition")]
    public void TestSceneTransition()
    {
        if (enableSceneTransition)
        {
            LoadNextScene();
        }
    }
    
    [ContextMenu("Toggle Auto Transition")]
    public void ToggleAutoTransition()
    {
        autoTransitionOnEnd = !autoTransitionOnEnd;
    }
    
    [ContextMenu("Toggle Use Scene Index")]
    public void ToggleUseSceneIndex()
    {
        useSceneIndex = !useSceneIndex;
    }
    
    [ContextMenu("Set Scene Index to +1")]
    public void SetSceneIndexPlusOne()
    {
        nextSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
    }
    
    [ContextMenu("Set Scene Index to +2")]
    public void SetSceneIndexPlusTwo()
    {
        nextSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 2;
    }
    
    void Update()
    {
        if (IsDialogueActive() && IsContinueInputDown())
        {
            ContinueDialogue();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (enableSceneTransition)
            {
                LoadNextScene();
            }
        }
        if (c == 3)
        {
            catcherCollider.SetActive(true);
            tutorial.SetActive(true);
            c++;
        }
    }

    private bool IsContinueInputDown()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            return true;

        if (!MobileInput.TryGetPrimaryTap(out var tapPosition, false))
            return false;

        if (IsTapInsideButton(continueButton, tapPosition) || IsTapInsideButton(skipButton, tapPosition))
            return false;

        return IsTapInsideDialoguePanel(tapPosition);
    }

    private bool IsTapInsideDialoguePanel(Vector2 screenPosition)
    {
        var rect = dialoguePanel != null ? dialoguePanel.transform as RectTransform : null;
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition);
    }

    private bool IsTapInsideButton(Button button, Vector2 screenPosition)
    {
        if (button == null)
            return false;

        var rect = button.transform as RectTransform;
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition);
    }
}

