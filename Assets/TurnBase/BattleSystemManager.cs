using UnityEngine;
using System;

public class BattleSystemManager : MonoBehaviour
{
    private static BattleSystemManager _instance;
    private static bool _isShuttingDown = false;

    public static BattleSystemManager Instance
    {
        get
        {
            if (_isShuttingDown) return null;
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BattleSystemManager>();
                if (_instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("BattleSystemManager");
                    _instance = go.AddComponent<BattleSystemManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Battle Points (BP)")]
    [SerializeField] private int teamBattlePoints = 3;
    [SerializeField] private int maxBattlePoints = 5;
    [SerializeField] private int maxPointsGainedPerTurn = 2;

    private int _pointsGainedThisTurn = 0;

    public int TeamBattlePoints => teamBattlePoints;
    public int MaxBattlePoints => maxBattlePoints;

    public event Action<int> OnBattlePointsChanged;
    public event Action<int, int> OnBattlePointsChangedFull; // (current, max)

    private void Awake()
    {
        _isShuttingDown = false;
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    private void Start()
    {
        ResetBattlePoints();
    }

    /// <summary>
    /// </summary>
    public void ResetBattlePoints()
    {
        teamBattlePoints = 3;
        _pointsGainedThisTurn = 0;
        NotifyPointsChanged();
    }

    /// <summary>
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
            return caster != null && caster.CurrentEnergy >= skill.energyCost;
        }

        return teamBattlePoints >= skill.battlePointCost;
    }

    public void ExecuteSkill(SkillSO skill, SlimeBattleStats caster)
    {
        if (!CanUseSkill(skill, caster))
        {
            Debug.LogWarning("Not enough resources.");
            return;
        }

        if (skill.type == SkillType.Ultimate)
        {
            caster?.UseEnergy(skill.energyCost);
        }
        else if (skill.battlePointCost > 0)
        {
            ConsumeBattlePoints(skill.battlePointCost);
        }

        int pointGain = skill.battlePointGain;
        if (pointGain <= 0 && skill.battlePointCost == 0 && skill.type != SkillType.Ultimate)
        {
            pointGain = 1;
        }

        if (pointGain > 0)
        {
            AddBattlePoints(pointGain);
            var turnSys = FindFirstObjectByType<TurnSystem>();
            if (turnSys != null && caster != null)
            {
                turnSys.CreateDamagePopup(caster.transform.position + Vector3.up * 2f, $"+{pointGain} SP", Color.cyan);
            }
        }

        int energyGain = skill.energyGain > 0 ? skill.energyGain : (skill.type == SkillType.BasicAttack ? 20 : 25);
        caster?.AddEnergy(energyGain);

    }

    /// <summary>
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

    public void SpendBattlePoints(int amount) => ConsumeBattlePoints(amount);

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
