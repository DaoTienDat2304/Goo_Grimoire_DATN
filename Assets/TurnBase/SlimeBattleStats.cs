using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public struct ActiveBuff
{
    public BuffStat stat;
    public int originalValue;
    public int turnsLeft; // -1 = vĩnh viễn
    public bool isDebuff;  // true = debuff
}

public class SlimeBattleStats : MonoBehaviour
{
    [Header("Base Stats (from SlimeStats)")]
    public SlimeStats baseStats;
    
    [Header("Battle Modifiers")]
    public float critChance = 0f; // buff thêm vào Crit Rate (%)
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

    public int CurrentHP { get { return currentHP; } set { currentHP = value; } }
    public int MaxHP { get { return maxHP; } set { maxHP = value; } }
    public int BattleAttack { get { return battleAttack; } set { battleAttack = value; } }
    public int BattleMagicAttack { get { return battleMagicAttack; } set { battleMagicAttack = value; } }
    public int BattleDefense { get { return battleDefense; } set { battleDefense = value; } }
    public int BattleSpeed { get { return battleSpeed; } set { battleSpeed = value; } }
    public float BattleCritRate { get { return battleCritRate; } set { battleCritRate = value; } }
    public float BattleCritDMG { get { return battleCritDMG; } set { battleCritDMG = value; } }

    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    public int StunTurns { get; private set; }
    public bool IsStunned => StunTurns > 0;

    private void Awake()
    {
        if (baseStats == null)
            baseStats = GetComponent<SlimeStats>();
    }
    
    private void Start()
    {
        InitializeBattleStats();
    }
    
    void InitializeBattleStats()
    {
        if (baseStats == null) return;
        
        // Nếu là boss (enemy), buff stats theo Remote Config (mặc định 3x)
        float bossMultiplier = RemoteConfigManager.Instance != null
            ? RemoteConfigManager.Instance.BossStatMultiplier : 3f;
        float multiplier = baseStats.isEnemy ? bossMultiplier : 1f;
        
        MaxHP = Mathf.RoundToInt((baseStats.MaxHP + maxHPBonus) * multiplier);
        CurrentHP = MaxHP;
        BattleAttack = Mathf.RoundToInt(baseStats.Attack * multiplier);
        BattleMagicAttack = Mathf.RoundToInt(baseStats.MagicAttack * multiplier);
        BattleDefense = Mathf.RoundToInt(baseStats.Defense * multiplier);
        BattleSpeed = Mathf.RoundToInt((baseStats.Speed + speedBonus) * multiplier);
        
        BattleCritRate = baseStats.CritRate;
        BattleCritDMG = baseStats.CritDMG;
        
        // Cập nhật baseStats để UI hiển thị đúng
        baseStats.MaxHP = MaxHP;
        baseStats.HP = CurrentHP;
        
        if (baseStats.hpbar != null)
        {
            baseStats.hpbar.maxValue = MaxHP;
            baseStats.hpbar.value = CurrentHP;
        }
    }

    // Dynamic stats computation taking conversion into account
    public float GetEffectiveCritRate()
    {
        float rate = BattleCritRate;
        // critChance is additional buff from skills, represented as percentage (e.g. 10 means +10% or +0.10f)
        rate += critChance / 100f;
        return rate;
    }

    public float GetEffectiveCritDMG()
    {
        float rate = GetEffectiveCritRate();
        float excessCritRate = Mathf.Max(0f, rate - 0.75f);
        float dmg = BattleCritDMG + excessCritRate; // 1:1 conversion
        return dmg;
    }

    public int GetEffectiveAttack()
    {
        float critDmg = GetEffectiveCritDMG();
        float excessCritDmg = Mathf.Max(0f, critDmg - 2.50f);
        int atkBonus = Mathf.RoundToInt(excessCritDmg * 100f * 5f); // 1% excess = 5 ATK
        return BattleAttack + atkBonus;
    }

    public int GetEffectiveMagicAttack()
    {
        float critDmg = GetEffectiveCritDMG();
        float excessCritDmg = Mathf.Max(0f, critDmg - 2.50f);
        int matkBonus = Mathf.RoundToInt(excessCritDmg * 100f * 5f); // 1% excess = 5 Magic ATK
        return BattleMagicAttack + matkBonus;
    }

    public float GetFinalCritDMG()
    {
        float critDmg = GetEffectiveCritDMG();
        return Mathf.Min(2.50f, critDmg); // capped at 250% (2.50)
    }

    public float GetFinalCritRate()
    {
        float rate = GetEffectiveCritRate();
        return Mathf.Min(0.75f, rate); // capped at 75% (0.75)
    }
    
    public void TakeDamage(int rawDamage)
    {
        // GDD: Damage after defense = rawDamage * (1 - DEF_enemy * 0.008)
        // Hard Cap DEF reduction at 80%
        float defReduction = Mathf.Min(0.80f, BattleDefense * 0.008f);
        float finalDamage = rawDamage * (1f - defReduction);
        
        finalDamage *= (1f - (damageReduction / 100f));
        finalDamage = Mathf.Max(1, finalDamage); // Minimum 1 damage
        
        int finalDmgInt = Mathf.RoundToInt(finalDamage);
        CurrentHP -= finalDmgInt;
        CurrentHP = Mathf.Max(0, CurrentHP);
        
        if (baseStats != null)
        {
            baseStats.HP = CurrentHP;
            if (baseStats.hpbar != null)
            {
                baseStats.hpbar.value = CurrentHP;
            }
        }
        
        TurnSystem turnSys = FindObjectOfType<TurnSystem>();
        if (turnSys != null)
        {
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 1.5f, $"-{finalDmgInt}", Color.red);
        }

        Debug.Log($"{name} takes {finalDmgInt} damage! HP: {CurrentHP}/{MaxHP}");
    }
    
    public void Heal(int healAmount)
    {
        CurrentHP += healAmount;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP);
        
        if (baseStats != null)
        {
            baseStats.HP = CurrentHP;
            if (baseStats.hpbar != null)
            {
                baseStats.hpbar.value = CurrentHP;
            }
        }

        TurnSystem turnSys = FindObjectOfType<TurnSystem>();
        if (turnSys != null)
        {
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 1.5f, $"+{healAmount} HP", Color.green);
        }
        
        Debug.Log($"{name} heals for {healAmount}! HP: {CurrentHP}/{MaxHP}");
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

        TurnSystem turnSys = FindObjectOfType<TurnSystem>();
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

        TurnSystem turnSys = FindObjectOfType<TurnSystem>();
        if (turnSys != null)
        {
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 1.8f, "STUNNED!", Color.magenta);
        }

        Debug.Log($"{name} bị stun {duration} lượt!");
    }

    public void TickStun()
    {
        if (StunTurns <= 0) return;
        StunTurns--;
        if (StunTurns == 0)
            Debug.Log($"{name} thoát khỏi stun.");
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
                Debug.Log($"{name}: {buff.stat} buff hết hạn, restore về {buff.originalValue}");
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
            BuffStat.Attack  => BattleAttack,
            BuffStat.Speed   => BattleSpeed,
            _ => 0
        };
    }

    private void SetStat(BuffStat stat, int value)
    {
        switch (stat)
        {
            case BuffStat.Defense: BattleDefense = value; break;
            case BuffStat.Attack:  BattleAttack  = value; break;
            case BuffStat.Speed:   BattleSpeed   = value; break;
        }
    }
    
    public bool TryCriticalHit()
    {
        float finalCritRate = GetFinalCritRate();
        return Random.Range(0f, 1f) < finalCritRate;
    }
}
