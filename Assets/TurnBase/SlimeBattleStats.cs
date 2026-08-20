using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public struct ActiveBuff
{
    public BuffStat stat;
    public int originalValue;
    public int turnsLeft;
    public bool isDebuff;  // true = debuff
}

[System.Serializable]
public struct ActiveDoT
{
    public EffectType type; // Poison or Bleed
    public int damagePerTurn;
    public int turnsLeft;
}

public class SlimeBattleStats : MonoBehaviour
{
    [Header("Base Stats (from SlimeStats)")]
    public SlimeStats baseStats;
    public bool isInitialized = false;

    [Header("Battle Modifiers")]
    public float critChance = 0f;
    public float damageReduction = 0f;
    public int maxHPBonus = 0;
    public int speedBonus = 0;

    // Calculated stats for battle
    [Header("Battle Stats (Live editable)")]
    [SerializeField] private int currentHP;
    [SerializeField] private int maxHP;
    [SerializeField] private int battleAttack;
    [SerializeField] private int battleMagicAttack;
    [SerializeField] private int battleDefense;
    [SerializeField] private int battleSpeed;
    [SerializeField] private float battleCritRate;
    [SerializeField] private float battleCritDMG;

    [SerializeField] private int currentEnergy = 0;
    private const int MAX_ENERGY = 100;

    [SerializeField] private float currentAV;
    [SerializeField] private int currentShield = 0;
    public int CurrentEnergy { get => currentEnergy; }
    public float CurrentAV { get => currentAV; set => currentAV = value; }
    public int CurrentShield { get => currentShield; }

    public int CurrentHP { get { return currentHP; } set { currentHP = value; } }
    public int MaxHP { get { return maxHP; } set { maxHP = value; } }
    public int BattleAttack { get { return battleAttack; } set { battleAttack = value; } }
    public int BattleMagicAttack { get { return battleMagicAttack; } set { battleMagicAttack = value; } }
    public int BattleDefense { get { return battleDefense; } set { battleDefense = value; } }
    public int BattleSpeed { get { return battleSpeed; } set { battleSpeed = value; } }
    public float BattleCritRate { get { return battleCritRate; } set { battleCritRate = value; } }
    public float BattleCritDMG { get { return battleCritDMG; } set { battleCritDMG = value; } }

    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    private List<ActiveDoT> activeDoTs = new List<ActiveDoT>();

    [HideInInspector] public int initialBattleAttack;
    [HideInInspector] public int initialBattleMagicAttack;
    [HideInInspector] public int initialBattleDefense;
    [HideInInspector] public int initialBattleSpeed;

    public void ReinitializeFromBaseStats()
    {
        isInitialized = false;
        InitializeBattleStats();
    }

    [Header("Special Mechanic Flags")]
    public bool isCounterStanceActive = false;
    public bool isCrystalBarrierActive = false;

    public int StunTurns { get; private set; }
    public bool IsStunned => StunTurns > 0;

    public int GetPoisonStackCount()
    {
        return activeDoTs.Count(d => d.type == EffectType.Poison && d.turnsLeft > 0);
    }

    public void ApplyPoison(int duration = 2, int maxStacks = -1)
    {
        if (maxStacks < 0) maxStacks = RemoteBalance.Battle.poisonMaxStacks;
        int current = GetPoisonStackCount();
        if (current < maxStacks)
        {
            int poisonDmg = Mathf.Max(1, Mathf.RoundToInt(MaxHP * RemoteBalance.Battle.poisonPercentHp));
            activeDoTs.Add(new ActiveDoT { type = EffectType.Poison, damagePerTurn = poisonDmg, turnsLeft = duration });
            var turnSys = GetTurnSys();
            if (turnSys != null)
                turnSys.CreateDamagePopup(transform.position + Vector3.up * 2.0f, $"POISON ({current + 1})", Color.green);
        }
    }

    private static TurnSystem _cachedTurnSys;
    private static bool _isTowerModeCached = false;
    private static bool _isTowerModeValue = false;

    private static TurnSystem GetTurnSys()
    {
        if (_cachedTurnSys == null)
            _cachedTurnSys = UnityEngine.Object.FindAnyObjectByType<TurnSystem>();
        return _cachedTurnSys;
    }

    public static void ClearTurnSysCache()
    {
        _cachedTurnSys = null;
        _isTowerModeCached = false;
    }

    private void Awake()
    {
        if (baseStats == null)
            baseStats = GetComponent<SlimeStats>();
        if (_cachedTurnSys == null)
            _cachedTurnSys = UnityEngine.Object.FindAnyObjectByType<TurnSystem>();
    }

    private void Start()
    {
        if (!isInitialized)
        {
            InitializeBattleStats();
        }
    }

    void InitializeBattleStats()
    {
        if (baseStats == null) return;

        if (baseStats.isEnemy && baseStats.useRarityBossScaling)
        {
            var m = BossStatScaling.Get(baseStats.enemyRarity);
            MaxHP = Mathf.RoundToInt((baseStats.MaxHP + maxHPBonus) * m.hp);
            BattleAttack = Mathf.RoundToInt(baseStats.Attack * m.atk);
            BattleMagicAttack = Mathf.RoundToInt(baseStats.MagicAttack * m.magic);
            BattleDefense = Mathf.RoundToInt(baseStats.Defense * m.def);
            BattleSpeed = Mathf.RoundToInt((baseStats.Speed + speedBonus) * m.speed);
        }
        else
        {
            if (!_isTowerModeCached)
            {
                _isTowerModeValue = UnityEngine.Object.FindAnyObjectByType<TowerTurnSystem>() != null;
                _isTowerModeCached = true;
            }
            bool isTowerMode = _isTowerModeValue;
            bool isFarmMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode();
            float multiplier = 1f;
            
            if (baseStats.isEnemy && !isTowerMode && !isFarmMode)
            {
                multiplier = RemoteBalance.Battle.legacyBossMultiplier;
            }

            MaxHP = Mathf.RoundToInt((baseStats.MaxHP + maxHPBonus) * multiplier);
            BattleAttack = Mathf.RoundToInt(baseStats.Attack * multiplier);
            BattleMagicAttack = Mathf.RoundToInt(baseStats.MagicAttack * multiplier);
            BattleDefense = Mathf.RoundToInt(baseStats.Defense * multiplier);
            BattleSpeed = Mathf.RoundToInt((baseStats.Speed + speedBonus) * multiplier);
        }
        CurrentHP = MaxHP;

        BattleCritRate = baseStats.CritRate;
        BattleCritDMG = baseStats.CritDMG;

        baseStats.MaxHP = MaxHP;
        baseStats.HP = CurrentHP;

        if (baseStats.hpbar != null)
        {
            baseStats.hpbar.maxValue = MaxHP;
            baseStats.hpbar.value = CurrentHP;
        }

        initialBattleAttack = BattleAttack;
        initialBattleMagicAttack = BattleMagicAttack;
        initialBattleDefense = BattleDefense;
        initialBattleSpeed = BattleSpeed;

        isInitialized = true;
    }

    public void CalculateInitialAV()
    {
        currentAV = 10000f / Mathf.Max(1, BattleSpeed);
    }

    public void ResetAV()
    {
        currentAV += 10000f / Mathf.Max(1, BattleSpeed);
    }

    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, MAX_ENERGY);
#if UNITY_EDITOR
        Debug.Log($"{name} heal {amount} Energy. Current: {currentEnergy}/100");
#endif
    }

    public void AddShield(int amount)
    {
        currentShield += amount;
        var turnSys = GetTurnSys();
        if (turnSys != null)
        {
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 2.2f, $"+{amount} SHIELD!", Color.cyan);
        }
#if UNITY_EDITOR
        Debug.Log($"{name} taken {amount} Shield. Shield: {currentShield}");
#endif
    }

    public void UseEnergy(int amount)
    {
        currentEnergy = Mathf.Max(0, currentEnergy - amount);
    }

    public void SpendEnergy(int amount) => UseEnergy(amount);

    // Dynamic stats computation taking conversion into account.
    public float GetEffectiveCritRate()
    {
        // critChance is additional buff from skills, represented as percentage (e.g. 10 means +10% or +0.10f)
        return BattleStatFormula.EffectiveCritRate(BattleCritRate, critChance);
    }

    public float GetEffectiveCritDMG()
    {
        return BattleStatFormula.EffectiveCritDMG(BattleCritRate, BattleCritDMG, critChance);
    }

    public int GetEffectiveAttack()
    {
        return BattleStatFormula.EffectiveAttack(BattleAttack, BattleCritRate, BattleCritDMG, critChance);
    }

    public int GetEffectiveMagicAttack()
    {
        return BattleStatFormula.EffectiveMagicAttack(BattleMagicAttack, BattleCritRate, BattleCritDMG, critChance);
    }

    public float GetFinalCritDMG()
    {
        return BattleStatFormula.FinalCritDMG(BattleCritRate, BattleCritDMG, critChance);
    }

    public float GetFinalCritRate()
    {
        return BattleStatFormula.FinalCritRate(BattleCritRate, critChance);
    }

    public void TakeDamage(int rawDamage, GameObject attacker = null, bool isCrit = false, bool isAoE = false)
    {
        float defReduction = BattleStatFormula.DefenseReduction(BattleDefense);
        float finalDamage = rawDamage * (1f - defReduction);

        float totalDR = damageReduction + (isCrystalBarrierActive ? 10f : 0f);
        finalDamage *= (1f - (totalDR / 100f));
        finalDamage = Mathf.Max(1, finalDamage); // Minimum 1 damage

        int finalDmgInt = Mathf.RoundToInt(finalDamage);

        // Counter Stance Reflection (Iron Golem / Elite Iron Golem or counter stance flag)
        bool shouldCounter = isCounterStanceActive || (baseStats != null && baseStats.isEnemy && (gameObject.name.Contains("IronGolem") || gameObject.name.Contains("EliteIronGolem")) && (isCrit || isAoE));
        if (shouldCounter && attacker != null && attacker != gameObject)
        {
            var attackerStats = attacker.GetComponent<SlimeBattleStats>();
            if (attackerStats != null)
            {
                int counterDmg = Mathf.Max(1, Mathf.RoundToInt(finalDmgInt * 0.5f));
                attackerStats.TakeDamage(counterDmg);
                var cTurnSys = GetTurnSys();
                if (cTurnSys != null)
                    cTurnSys.CreateDamagePopup(transform.position + Vector3.up * 2f, "COUNTER 50%!", Color.red);
            }
        }

        AddEnergy(RemoteBalance.Battle.energyPerAction);

        if (currentShield > 0)
        {
            if (currentShield >= finalDmgInt)
            {
                currentShield -= finalDmgInt;
#if UNITY_EDITOR
                Debug.Log($"{name} hit for {finalDmgInt} but shield blocked it!");
#endif
                return;
            }
            else
            {
                finalDmgInt -= currentShield;
                currentShield = 0;
            }
        }

        CurrentHP -= finalDmgInt;
        CurrentHP = Mathf.Max(0, CurrentHP);

        if (baseStats != null)
        {
            baseStats.HP = CurrentHP;
            if (baseStats.hpbar != null)
                baseStats.hpbar.value = CurrentHP;
            if (CurrentHP <= 0) baseStats.SetDeadVisual();
        }

        var turnSys = GetTurnSys();
        if (turnSys != null)
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 1.5f, $"-{finalDmgInt}", Color.red);

#if UNITY_EDITOR
        Debug.Log($"{name} takes {finalDmgInt} damage! HP: {CurrentHP}/{MaxHP}");
#endif
    }

    public void ModifyActionValue(float percentage, bool isAdvance)
    {
        float baseAV = 10000f / Mathf.Max(1, BattleSpeed);
        float changeAmount = baseAV * (percentage / 100f);

        if (isAdvance)
        {
            currentAV = Mathf.Max(0, currentAV - changeAmount);
#if UNITY_EDITOR
            Debug.Log($"{name} pulled {percentage}%, AV now {currentAV}");
#endif
        }
        else
        {
            currentAV += changeAmount;
#if UNITY_EDITOR
            Debug.Log($"{name} pushed {percentage}%, AV tang on {currentAV}");
#endif
        }
    }

    public void Heal(int healAmount)
    {
        CurrentHP += healAmount;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP);

        if (baseStats != null)
        {
            baseStats.HP = CurrentHP;
            if (baseStats.hpbar != null)
                baseStats.hpbar.value = CurrentHP;
        }

        var turnSys = GetTurnSys();
        if (turnSys != null)
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 1.5f, $"+{healAmount} HP", Color.green);

#if UNITY_EDITOR
        Debug.Log($"{name} heals for {healAmount}! HP: {CurrentHP}/{MaxHP}");
#endif
    }

    public void ApplyBuff(BuffStat stat, float multiplier, int duration, bool isDebuff = false)
    {
        int existingIdx = activeBuffs.FindIndex(b => b.stat == stat);
        int trueOriginal;
        if (existingIdx >= 0)
        {
            trueOriginal = activeBuffs[existingIdx].originalValue;
            SetStat(stat, trueOriginal);
            activeBuffs.RemoveAt(existingIdx);
        }
        else
        {
            trueOriginal = GetStat(stat);
        }

        int buffed = Mathf.RoundToInt(trueOriginal * multiplier);
        SetStat(stat, buffed);

        if (duration > 0)
        {
            activeBuffs.Add(new ActiveBuff
            {
                stat = stat,
                originalValue = trueOriginal,
                turnsLeft = duration,
                isDebuff = isDebuff
            });
        }

        var turnSys = GetTurnSys();
        if (turnSys != null)
        {
            string symbol = isDebuff ? "-" : "+";
            Color c = isDebuff ? new Color(1f, 0.5f, 0f) : Color.cyan;
            string statName = stat.ToString().Substring(0, 3).ToUpper();
            int diff = buffed - trueOriginal;
            string valueStr = diff != 0 ? $"{symbol}{Mathf.Abs(diff)} {statName}" : $"{symbol}{statName}";
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 1.8f, valueStr, c);
        }
    }

    public void CleanseDebuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].isDebuff)
            {
                SetStat(activeBuffs[i].stat, activeBuffs[i].originalValue);
                activeBuffs.RemoveAt(i);
            }
        }
    }

    public void DispelBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (!activeBuffs[i].isDebuff)
            {
                SetStat(activeBuffs[i].stat, activeBuffs[i].originalValue);
                activeBuffs.RemoveAt(i);
            }
        }
    }

    public void ApplyStun(int duration)
    {
        if (duration > StunTurns)
            StunTurns = duration;

        var turnSys = GetTurnSys();
        if (turnSys != null)
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 1.8f, "STUNNED!", Color.magenta);

#if UNITY_EDITOR
        Debug.Log($"{name} bi stun {duration} turn!");
#endif
    }

    public void TickStun()
    {
        if (StunTurns <= 0) return;
        StunTurns--;
        if (StunTurns == 0)
            Debug.Log($"{name} thoat khoi stun.");
    }

    public void TickBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            buff.turnsLeft--;
            if (buff.turnsLeft <= 0)
            {
                SetStat(buff.stat, buff.originalValue);
                Debug.Log($"{name}: {buff.stat} buff het han, restore ve {buff.originalValue}");
                activeBuffs.RemoveAt(i);
            }
            else
            {
                activeBuffs[i] = buff;
            }
        }
    }

    private int GetStat(BuffStat stat)
    {
        return stat switch
        {
            BuffStat.Defense => BattleDefense,
            BuffStat.Attack => BattleAttack,
            BuffStat.Speed => BattleSpeed,
            _ => 0
        };
    }

    private void SetStat(BuffStat stat, int value)
    {
        switch (stat)
        {
            case BuffStat.Defense: BattleDefense = value; break;
            case BuffStat.Attack: BattleAttack = value; break;
            case BuffStat.Speed: BattleSpeed = value; break;
        }
    }
    public void ApplyDoT(EffectType type, int damagePerTurn, int duration)
    {
        activeDoTs.Add(new ActiveDoT { type = type, damagePerTurn = damagePerTurn, turnsLeft = duration });
        var turnSys = GetTurnSys();
        if (turnSys != null)
        {
            string effectName = type == EffectType.Poison ? "POISONED!" : "BLEEDING!";
            Color color = type == EffectType.Poison ? Color.green : new Color(0.6f, 0f, 0f);
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 2.0f, effectName, color);
        }
#if UNITY_EDITOR
        Debug.Log($"{name} bi taken {type} deals {damagePerTurn} damage per turn for {duration} turn!");
#endif
    }

    public void TickDoTs()
    {
        for (int i = activeDoTs.Count - 1; i >= 0; i--)
        {
            var dot = activeDoTs[i];
            dot.turnsLeft--;

            TakeDamage(dot.damagePerTurn);

            var dotTurnSys = GetTurnSys();
            if (dotTurnSys != null)
            {
                Color color = dot.type == EffectType.Poison ? Color.green : new Color(0.6f, 0f, 0f);
                string suffix = dot.type == EffectType.Poison ? " Poison" : " Bleed";
                dotTurnSys.CreateDamagePopup(transform.position + Vector3.up * 1.5f, dot.damagePerTurn.ToString() + suffix, color);
            }

            if (dot.turnsLeft <= 0)
            {
                Debug.Log($"{name}: {dot.type} ended.");
                activeDoTs.RemoveAt(i);
            }
            else
            {
                activeDoTs[i] = dot;
            }
        }
    }

    public bool TryCriticalHit()
    {
        float finalCritRate = GetFinalCritRate();
        return Random.Range(0f, 1f) < finalCritRate;
    }
}
