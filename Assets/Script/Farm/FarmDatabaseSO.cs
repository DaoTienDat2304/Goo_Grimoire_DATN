using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FarmDatabase", menuName = "Farm/FarmDatabase")]
public class FarmDatabaseSO : ScriptableObject
{
    [Header("Farm Difficulties")]
    public List<FarmDifficulty> difficulties = new List<FarmDifficulty>();

    [Header("Runtime Cache (Non-Serialized)")]
    [System.NonSerialized] public bool hasPendingResult = false;
    [System.NonSerialized] public int cachedCompletedIndex = -1;
    [System.NonSerialized] public int cachedRewardCoins = 0;
    [System.NonSerialized] public int cachedRewardGems = 0;
    [System.NonSerialized] public int activeSelectedDifficultyIndex = -1;

    public bool IsDifficultyUnlocked(int index)
    {
        if (difficulties == null || index < 0 || index >= difficulties.Count) return false;
        if (FarmModeManager.Instance != null)
        {
            return FarmModeManager.Instance.IsDifficultyUnlocked(index);
        }
        return index == 0 || difficulties[index].unlocked;
    }

    public bool IsDifficultyCompleted(int index)
    {
        if (difficulties == null || index < 0 || index >= difficulties.Count) return false;
        return difficulties[index].completed;
    }

    public FarmDifficulty GetDifficulty(int index)
    {
        if (difficulties == null || index < 0 || index >= difficulties.Count) return null;
        return difficulties[index];
    }

    public void RecordVictory(int index, int coins, int gems)
    {
        hasPendingResult = true;
        cachedCompletedIndex = index;
        cachedRewardCoins = coins;
        cachedRewardGems = gems;
    }

    public void ClearPendingResult()
    {
        hasPendingResult = false;
        cachedCompletedIndex = -1;
        cachedRewardCoins = 0;
        cachedRewardGems = 0;
        activeSelectedDifficultyIndex = -1;
    }
}
