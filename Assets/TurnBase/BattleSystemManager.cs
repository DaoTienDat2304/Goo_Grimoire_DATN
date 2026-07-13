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
    [SerializeField] private int teamBattlePoints;
    [SerializeField] private int maxBattlePoints = 5;

    public int TeamBattlePoints => teamBattlePoints;

    public event Action<int> OnBattlePointsChanged;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        // Khởi đầu trận có 3 ĐCK
        teamBattlePoints = 3;
        OnBattlePointsChanged?.Invoke(teamBattlePoints);
    }

    public bool CanUseSkill(SkillSO skill, SlimeBattleStats caster)
    {
        if (skill.type == SkillType.Ultimate)
        {
            return caster.CurrentEnergy >= skill.energyCost; // Tuyệt kỹ cần đủ năng lượng
        }

        return teamBattlePoints >= skill.battlePointCost; // Kỹ năng thường cần đủ ĐCK
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
            caster.UseEnergy(skill.energyCost);
        }
        else
        {
            ConsumeBattlePoints(skill.battlePointCost);
        }

        // Hồi tài nguyên (ĐCK hoặc Năng lượng)
        AddBattlePoints(skill.battlePointGain);
        caster.AddEnergy(skill.energyGain);

        Debug.Log($"{caster.name} thi triển {skill.skillName}. ĐCK còn: {teamBattlePoints}");

        // Logic xử lý Damage/Heal/Buff dựa trên effects
    }

    public void AddBattlePoints(int amount)
    {
        if (amount <= 0) return;
        teamBattlePoints = Mathf.Min(teamBattlePoints + amount, maxBattlePoints);
        OnBattlePointsChanged?.Invoke(teamBattlePoints);
    }

    public void ConsumeBattlePoints(int amount)
    {
        if (amount <= 0) return;
        teamBattlePoints = Mathf.Max(0, teamBattlePoints - amount);
        OnBattlePointsChanged?.Invoke(teamBattlePoints);
    }

    // Tăng giới hạn ĐCK (Cho nội tại Độc Quyền của Secret)
    public void IncreaseMaxBattlePoints(int amount)
    {
        maxBattlePoints += amount;
    }
}