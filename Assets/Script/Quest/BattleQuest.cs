using UnityEngine;

public enum BattleModeFilter { All, Adventure, Farm, Tower }

[CreateAssetMenu(menuName = "Quests/Battle Quest")]
public class BattleQuest : Quest
{
    [Header("Battle Quest Settings")]
    public int battleGoal;
    public BattleModeFilter modeFilter = BattleModeFilter.All;
    public int curBattles = 0;

    public void RegisterBattleWin(BattleMode mode)
    {
        if (state != QuestState.InProgress) return;
        if (curBattles >= battleGoal) return;

        bool matches = modeFilter == BattleModeFilter.All
            || (modeFilter == BattleModeFilter.Adventure && mode == BattleMode.Adventure)
            || (modeFilter == BattleModeFilter.Farm      && mode == BattleMode.Farm)
            || (modeFilter == BattleModeFilter.Tower     && mode == BattleMode.Tower);

        if (matches) curBattles++;
    }

    public override bool CheckCompletion()
    {
        return curBattles >= battleGoal;
    }

    public override float GetProgressPercentage()
    {
        if (battleGoal <= 0) return 0f;
        return Mathf.Clamp01((float)curBattles / battleGoal) * 100f;
    }

    public override string GetProgressText()
    {
        return $"{curBattles} / {battleGoal} battle ({GetProgressPercentage():F0}%)";
    }
}
