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
    CollectItem,      // Số lượng vật phẩm đã thu thập
    CompleteQuest,    // Số lượng nhiệm vụ đã hoàn thành
    ReachLevel,       // Cấp độ đã đạt được
    PlayTime          // Thời gian chơi game
}
