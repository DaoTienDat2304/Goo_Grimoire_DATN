using UnityEngine;
using System;

public class BattleSystemManager : MonoBehaviour
{
    private static BattleSystemManager _instance;
    public static BattleSystemManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BattleSystemManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("BattleSystemManager");
                    _instance = go.AddComponent<BattleSystemManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Battle Points (ĐCK)")]
    [SerializeField] private int teamBattlePoints = 3;
    [SerializeField] private int maxBattlePoints = 5;
    [SerializeField] private int maxPointsGainedPerTurn = 2; // Trần sinh thêm ≤ +2/lượt (mọi nguồn)

    private int _pointsGainedThisTurn = 0;

    public int TeamBattlePoints => teamBattlePoints;
    public int MaxBattlePoints => maxBattlePoints;

    public event Action<int> OnBattlePointsChanged;
    public event Action<int, int> OnBattlePointsChangedFull; // (current, max)

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        ResetBattlePoints();
    }

    /// <summary>
    /// Reset ĐCK đầu trận (Đầu trận = 3 ĐCK, Max = 5)
    /// </summary>
    public void ResetBattlePoints()
    {
        teamBattlePoints = 3;
        _pointsGainedThisTurn = 0;
        NotifyPointsChanged();
    }

    /// <summary>
    /// Gọi mỗi khi bắt đầu một lượt hành động mới theo AV để reset trần sinh điểm của lượt đó
    /// </summary>
    public void OnNewTurnStarted()
    {
        _pointsGainedThisTurn = 0;
    }

    public bool CanUseSkill(SkillSO skill, SlimeBattleStats caster)
    {
        if (skill == null) return false;

        if (skill.type == SkillType.Ultimate)
        {
            return caster != null && caster.CurrentEnergy >= skill.energyCost; // Tuyệt kỹ cần đủ năng lượng
        }

        // Kỹ năng thường / Chiến kỹ cần đủ ĐCK của đội
        return teamBattlePoints >= skill.battlePointCost;
    }

    public void ExecuteSkill(SkillSO skill, SlimeBattleStats caster)
    {
        if (!CanUseSkill(skill, caster))
        {
            Debug.LogWarning("Không đủ tài nguyên để dùng kỹ năng này!");
            return;
        }

        // Trừ chi phí
        if (skill.type == SkillType.Ultimate)
        {
            caster?.UseEnergy(skill.energyCost);
        }
        else if (skill.battlePointCost > 0)
        {
            ConsumeBattlePoints(skill.battlePointCost);
        }

        // Hồi tài nguyên (ĐCK hoặc Năng lượng)
        int pointGain = skill.battlePointGain;
        if (pointGain <= 0 && skill.battlePointCost == 0 && skill.type != SkillType.Ultimate)
        {
            // Mọi đòn đánh thường hoặc kỹ năng không tốn ĐCK đều mặc định hồi +1 ĐCK
            pointGain = 1;
        }

        if (pointGain > 0)
        {
            AddBattlePoints(pointGain);
            var turnSys = FindFirstObjectByType<TurnSystem>();
            if (turnSys != null && caster != null)
            {
                turnSys.CreateDamagePopup(caster.transform.position + Vector3.up * 2f, $"+{pointGain} ĐCK", Color.cyan);
            }
        }

        int energyGain = skill.energyGain > 0 ? skill.energyGain : (skill.type == SkillType.BasicAttack ? 20 : 25);
        caster?.AddEnergy(energyGain);

        Debug.Log($"[ĐCK] {caster?.name} thi triển {skill.skillName}. ĐCK còn: {teamBattlePoints}/{maxBattlePoints}");
    }

    /// <summary>
    /// Thêm ĐCK cho đội, tuân thủ trần sinh thêm ≤ +2/lượt (mọi nguồn)
    /// </summary>
    public void AddBattlePoints(int amount, bool bypassTurnCap = false)
    {
        if (amount <= 0) return;

        int actualGain = amount;
        if (!bypassTurnCap)
        {
            int remainingQuota = Mathf.Max(0, maxPointsGainedPerTurn - _pointsGainedThisTurn);
            actualGain = Mathf.Min(amount, remainingQuota);
        }

        if (actualGain <= 0) return;

        _pointsGainedThisTurn += actualGain;
        teamBattlePoints = Mathf.Min(teamBattlePoints + actualGain, maxBattlePoints);
        NotifyPointsChanged();
    }

    public void ConsumeBattlePoints(int amount)
    {
        if (amount <= 0) return;
        teamBattlePoints = Mathf.Max(0, teamBattlePoints - amount);
        NotifyPointsChanged();
    }

    // Tăng giới hạn ĐCK (Cho nội tại Độc Quyền của Secret)
    public void IncreaseMaxBattlePoints(int amount)
    {
        maxBattlePoints += amount;
        NotifyPointsChanged();
    }

    private void NotifyPointsChanged()
    {
        OnBattlePointsChanged?.Invoke(teamBattlePoints);
        OnBattlePointsChangedFull?.Invoke(teamBattlePoints, maxBattlePoints);
    }
}