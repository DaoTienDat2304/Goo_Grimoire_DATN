using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Tower Quest")]
public class TowerQuest : Quest
{
    [Header("Tower Quest Settings")]
    public TowerSlimeBosses towerDatabase;
    public int targetFloor = 1;

    public override bool CheckCompletion()
    {
        if (towerDatabase == null) return false;
        return towerDatabase.highestFloorReached >= targetFloor;
    }

    public override float GetProgressPercentage()
    {
        if (towerDatabase == null || targetFloor <= 0) return 0f;
        return Mathf.Clamp01((float)towerDatabase.highestFloorReached / targetFloor) * 100f;
    }

    public override string GetProgressText()
    {
        int reached = towerDatabase != null ? towerDatabase.highestFloorReached : 0;
        return $"Floor {reached} / {targetFloor} ({GetProgressPercentage():F0}%)";
    }
}
