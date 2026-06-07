using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public struct ActiveBuff
{
    public BuffStat stat;
    public int originalValue;
    public int turnsLeft; // -1 = vĩnh viễn
    public bool isDebuff;  // true = debuff (dùng để cleanse riêng sau này)
}

public class SlimeBattleStats : MonoBehaviour
{
    [Header("Base Stats (from SlimeStats)")]
    public SlimeStats baseStats;
    
    [Header("Battle Modifiers")]
    public float critChance = 0f;
    public float evadeChance = 0f;
    public float damageReduction = 0f;
    public int maxHPBonus = 0;
    public int speedBonus = 0;
    
    // Calculated stats for battle
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public int BattleAttack { get; private set; }
    public int BattleDefense { get; private set; }
    public int BattleSpeed { get; private set; }
    public int BattleEvade { get; private set; }

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
        BattleDefense = Mathf.RoundToInt(baseStats.Defense * multiplier);
        BattleSpeed = Mathf.RoundToInt((baseStats.Speed + speedBonus) * multiplier);
        BattleEvade = baseStats.Evade; // Evade không cần buff
        
        // Cập nhật baseStats để UI (thanh máu) hiển thị đúng
        baseStats.MaxHP = MaxHP;
        baseStats.HP = CurrentHP;
        
        // Cập nhật hpbar nếu có
        if (baseStats.hpbar != null)
        {
            baseStats.hpbar.maxValue = MaxHP;
            baseStats.hpbar.value = CurrentHP;
        }
    }
    
    public void TakeDamage(int damage)
    {
        // Tính toán damage với defense và damage reduction
        float finalDamage = damage*damage/(BattleDefense+damage);
        Debug.Log($"{BattleDefense} {damage}");
        finalDamage *= (1f - (damageReduction / 100f));
        finalDamage = Mathf.Max(1, finalDamage); // Minimum 1 damage
        
        CurrentHP -= Mathf.RoundToInt(finalDamage);
        CurrentHP = Mathf.Max(0, CurrentHP);
        
        // Cập nhật baseStats.HP để UI (thanh máu) hiển thị đúng
        if (baseStats != null)
        {
            baseStats.HP = CurrentHP;
            // Cập nhật hpbar ngay lập tức
            if (baseStats.hpbar != null)
            {
                baseStats.hpbar.value = CurrentHP;
            }
        }
        
        Debug.Log($"{name} takes {finalDamage} damage! HP: {CurrentHP}/{MaxHP}");
    }
    
    public void Heal(int healAmount)
    {
        CurrentHP += healAmount;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP);
        
        // Cập nhật baseStats.HP để UI (thanh máu) hiển thị đúng
        if (baseStats != null)
        {
            baseStats.HP = CurrentHP;
            // Cập nhật hpbar ngay lập tức
            if (baseStats.hpbar != null)
            {
                baseStats.hpbar.value = CurrentHP;
            }
        }
        
        Debug.Log($"{name} heals for {healAmount}! HP: {CurrentHP}/{MaxHP}");
    }
    // Áp dụng buff/debuff lên stat. duration=0 nghĩa là vĩnh viễn.
    public void ApplyBuff(BuffStat stat, float multiplier, int duration, bool isDebuff = false)
    {
        // Lấy original thật: nếu đang có buff trên stat này thì restore về gốc trước
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
    }

    // Xóa tất cả debuff (dùng cho skill "Cleanse" sau này)
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

    // Xóa tất cả buff (dùng cho skill dispel của enemy sau này)
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
        Debug.Log($"{name} bị stun {duration} lượt!");
    }

    // Gọi cuối mỗi lượt của slime này để giảm stun duration
    public void TickStun()
    {
        if (StunTurns <= 0) return;
        StunTurns--;
        if (StunTurns == 0)
            Debug.Log($"{name} thoát khỏi stun.");
    }

    // Gọi cuối mỗi lượt của slime này để giảm duration và restore stat hết hạn
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
    public bool TryEvade()
    {
        return Random.Range(0f, 100f) < evadeChance;
    }
    
    public bool TryCriticalHit()
    {
        return Random.Range(0f, 100f) < critChance;
    }
}
