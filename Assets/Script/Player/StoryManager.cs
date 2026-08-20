using UnityEngine;
using System.Collections.Generic;

public class StoryManager : MonoBehaviour
{
    [Header("Story Points")]
    public List<Transform> storyPoints = new List<Transform>();
    public MovePlayer player;
    
    [Header("Story Settings")]
    public bool autoAdvance = false;
    public float autoAdvanceDelay = 2f;
    
    private int currentStoryIndex = 0;
    private TravelSceneManager travelManager;
    
    public System.Action<int> OnStoryPointReached;
    public System.Action OnAllStoryPointsCompleted;
    
    void Start()
    {
        travelManager = FindAnyObjectByType<TravelSceneManager>();
        
        if (player != null)
        {
            player.OnReachedTarget += OnPlayerReachedStoryPoint;
        }
    }
    
    private void OnPlayerReachedStoryPoint(Transform target)
    {
        int storyIndex = storyPoints.IndexOf(target);
        if (storyIndex != -1)
        {
            currentStoryIndex = storyIndex;
            OnStoryPointReached?.Invoke(storyIndex);
            
            if (autoAdvance)
            {
                Invoke(nameof(MoveToNextStoryPoint), autoAdvanceDelay);
            }
            
            if (currentStoryIndex >= storyPoints.Count - 1)
            {
                OnAllStoryPointsCompleted?.Invoke();
            }
        }
    }
    
    public void MoveToNextStoryPoint()
    {
        if (player == null || currentStoryIndex >= storyPoints.Count) return;
        
        player.MoveToTarget(storyPoints[currentStoryIndex]);
        currentStoryIndex++;
    }
    
    public void MoveToStoryPoint(int index)
    {
        if (player == null || index < 0 || index >= storyPoints.Count) return;
        
        currentStoryIndex = index;
        player.MoveToTarget(storyPoints[index]);
    }
    
    public void ResetStory()
    {
        currentStoryIndex = 0;
    }
    
    public int GetCurrentStoryIndex()
    {
        return currentStoryIndex;
    }
    
    public bool IsStoryCompleted()
    {
        return currentStoryIndex >= storyPoints.Count;
    }

    void Update()
    {

    }
    
    void OnDestroy()
    {
        if (player != null)
        {
            player.OnReachedTarget -= OnPlayerReachedStoryPoint;
        }
    }
}
