using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Time Quest")]
public class TimeQuest : Quest
{
    [Header("Time Quest Settings")]
    public int required;
    public float current = 0;

    public void RegisterTime()
    {
        if (state == QuestState.InProgress)
        {
            current += 1 * Time.deltaTime;
        }
    }

    public override bool CheckCompletion()
    {
        return current >= required;
    }
    
    public override float GetProgressPercentage()
    {
        if (required <= 0) return 0f;
        return Mathf.Clamp01(current / required) * 100f;
    }
    
    public override string GetProgressText()
    {
        return $"{current:F1}s / {required}s ({GetProgressPercentage():F0}%)";
    }
}
