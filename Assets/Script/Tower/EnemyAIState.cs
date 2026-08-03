using UnityEngine;

public class EnemyAIState : MonoBehaviour
{
    public int currentTurnCycle = 0; // Đếm số turn nội bộ của quái này
    public bool isPhase2Triggered = false;
    public int currentPhase = 1; // 1 = Phase 1, 2 = Phase 2, 3 = Phase 3
}