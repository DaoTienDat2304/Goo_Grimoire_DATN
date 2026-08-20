using UnityEngine;

[CreateAssetMenu(fileName = "ArchievementPre", menuName = "Scriptable Objects/ArchievementPre")]
public class ArchievementPre : ScriptableObject
{
    [Header("Achievement Info")]
    public int achievementID;
    public string achievementName;
    [TextArea(2, 4)]
    public string title;
    public string description;

    [Header("Achievement Requirements")]
    public AchievementType type;
    public int targetValue;

    [Header("Display")]
    public Sprite sprite;
    
    [Header("Currency Rewards")]
    public AchievementCurrencyReward currencyReward;
}

public enum AchievementType
{
    CollectItem,
    CompleteQuest,
    ReachLevel,
    PlayTime
}
