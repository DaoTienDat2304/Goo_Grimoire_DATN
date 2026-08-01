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

    [SerializeField] private int currentEnergy = 0;
    private const int MAX_ENERGY = 100;

    [SerializeField] private float currentAV; // Action Value hiện tại
    [SerializeField] private int currentShield = 0; // Lá chắn
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

    public int StunTurns { get; private set; }
    public bool IsStunned => StunTurns > 0;

    private void Awake()
    {
        if (baseStats == null)
            baseStats = GetComponent<SlimeStats>();
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
            // Adventure: hệ số Boss theo ĐỘ HIẾM & TỪNG chỉ số (design "Hệ số chỉ số Boss").
            var m = BossStatScaling.Get(baseStats.enemyRarity);
            MaxHP = Mathf.RoundToInt((baseStats.MaxHP + maxHPBonus) * m.hp);
            BattleAttack = Mathf.RoundToInt(baseStats.Attack * m.atk);
            BattleMagicAttack = Mathf.RoundToInt(baseStats.MagicAttack * m.magic);
            BattleDefense = Mathf.RoundToInt(baseStats.Defense * m.def);
            BattleSpeed = Mathf.RoundToInt((baseStats.Speed + speedBonus) * m.speed);
        }
        else
        {
            // Tower/khác: giữ hệ số phẳng theo Remote Config (mặc định 3x); đồng minh = 1x.
            bool isTowerMode = UnityEngine.Object.FindAnyObjectByType<TowerTurnSystem>() != null;
            float multiplier = 1f;
            
            if (baseStats.isEnemy && !isTowerMode)
            {
                multiplier = (RemoteConfigManager.Instance != null ? RemoteConfigManager.Instance.BossStatMultiplier : 3f);
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

        // Cập nhật baseStats để UI hiển thị đúng
        baseStats.MaxHP = MaxHP;
        baseStats.HP = CurrentHP;

        if (baseStats.hpbar != null)
        {
            baseStats.hpbar.maxValue = MaxHP;
            baseStats.hpbar.value = CurrentHP;
        }

        isInitialized = true;
    }

    // Tính toán AV khởi đầu dựa trên SPD
    public void CalculateInitialAV()
    {
        currentAV = 10000f / Mathf.Max(1, BattleSpeed);
    }

    // Khi hành động xong, reset lại AV
    public void ResetAV()
    {
        currentAV += 10000f / Mathf.Max(1, BattleSpeed);
    }

    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, MAX_ENERGY);
        Debug.Log($"{name} hồi {amount} Năng lượng. Current: {currentEnergy}/100");
    }

    public void UseEnergy(int amount)
    {
        currentEnergy = Mathf.Max(0, currentEnergy - amount);
    }

    // Dynamic stats computation taking conversion into account.
    // Toàn bộ công thức nằm ở BattleStatFormula để đồng bộ với hiển thị ngoài trận.
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

    public void TakeDamage(int rawDamage)
    {
        float defReduction = BattleStatFormula.DefenseReduction(BattleDefense);
        float finalDamage = rawDamage * (1f - defReduction);

        finalDamage *= (1f - (damageReduction / 100f));
        finalDamage = Mathf.Max(1, finalDamage); // Minimum 1 damage

        int finalDmgInt = Mathf.RoundToInt(finalDamage);

        AddEnergy(10);

        // Trừ khiên trước
        if (currentShield > 0)
        {
            if (currentShield >= finalDmgInt)
            {
                currentShield -= finalDmgInt;
                Debug.Log($"{name} bị đánh {finalDmgInt} nhưng khiên đã đỡ hết!");
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

        if (finalDmgInt > 0 && CurrentHP > 0)
        {
            AddEnergy(10); // +10 khi bị đánh
        }

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

    // Kéo lượt (Tiến) hoặc Đẩy lùi
    public void ModifyActionValue(float percentage, bool isAdvance)
    {
        float baseAV = 10000f / Mathf.Max(1, BattleSpeed);
        float changeAmount = baseAV * (percentage / 100f);

        if (isAdvance)
        {
            currentAV = Mathf.Max(0, currentAV - changeAmount); // Tiến
            Debug.Log($"{name} được kéo lượt {percentage}%, AV giảm còn {currentAV}");
        }
        else
        {
            currentAV += changeAmount; // Lùi
            Debug.Log($"{name} bị đẩy lùi {percentage}%, AV tăng lên {currentAV}");
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
        TurnSystem turnSys = FindObjectOfType<TurnSystem>();
        if (turnSys != null)
        {
            string effectName = type == EffectType.Poison ? "POISONED!" : "BLEEDING!";
            Color color = type == EffectType.Poison ? Color.green : new Color(0.6f, 0f, 0f);
            turnSys.CreateDamagePopup(transform.position + Vector3.up * 2.0f, effectName, color);
        }
        Debug.Log($"{name} bị dính {type} gây {damagePerTurn} sát thương mỗi lượt trong {duration} lượt!");
    }

    public void TickDoTs()
    {
        for (int i = activeDoTs.Count - 1; i >= 0; i--)
        {
            var dot = activeDoTs[i];
            dot.turnsLeft--;

            // Gây sát thương
            TakeDamage(dot.damagePerTurn);

            // Hiện popup sát thương DoT
            TurnSystem turnSys = FindObjectOfType<TurnSystem>();
            if (turnSys != null)
            {
                Color color = dot.type == EffectType.Poison ? Color.green : new Color(0.6f, 0f, 0f);
                string suffix = dot.type == EffectType.Poison ? " Poison" : " Bleed";
                turnSys.CreateDamagePopup(transform.position + Vector3.up * 1.5f, dot.damagePerTurn.ToString() + suffix, color);
            }

            if (dot.turnsLeft <= 0)
            {
                Debug.Log($"{name}: {dot.type} hết hạn.");
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