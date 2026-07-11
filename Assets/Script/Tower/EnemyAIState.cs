using UnityEngine;

public class EnemyAIState : MonoBehaviour
{
    public int currentTurnCycle = 0; // Đếm số turn nội bộ của quái này
    public bool isPhase2Triggered = false; // Dùng cho các Boss có cơ chế dưới 50% HP
}