using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Breeding Quest")]
public class BreedingQuest : Quest
{
    [Header("Breeding Quest Settings")]
    public int slimeGoal;
    public int curSlime = 0; // Sửa: bắt đầu từ 0 thay vì 3

    public void RegisterSlime()
    {
        // Sửa: chỉ tăng khi cần thiết, không tăng vô hạn
        if (curSlime < slimeGoal)
        {
            curSlime++;
        }
    }

    public override bool CheckCompletion()
    {
        return curSlime >= slimeGoal;
    }
    
    public override float GetProgressPercentage()
    {
        if (slimeGoal <= 0) return 0f;
        return Mathf.Clamp01((float)curSlime / slimeGoal) * 100f;
    }
    
    public override string GetProgressText()
    {
        return $"{curSlime} / {slimeGoal} slimes ({GetProgressPercentage():F0}%)";
    }
}
