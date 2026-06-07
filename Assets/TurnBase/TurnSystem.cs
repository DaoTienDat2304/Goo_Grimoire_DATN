using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Spine.Unity;
using UnityEngine.SceneManagement;

public class TurnSystem : MonoBehaviour
{
    [SerializeField] private Queue<GameObject> turnQueue = new Queue<GameObject>();
    public GameObject boss;
    private GameObject currentSlime;
    [SerializeField] private FormationManager formationManager;
    [Header("Wild Slimes Database")]
    [SerializeField] public WildSlimes wildSlimes;
    
    [Header("Tower Database")]
    [SerializeField] public TowerSlimeBosses towerBosses;
    
    private List<GameObject> turnList;
    public int turnCount = 0;
    public GameObject skillPanel;
    public GameObject memberPanel;
    public SkeletonGraphic curSlimeBody;
    public Image curSlimeHat;
    public Image curSlimeWeapon;
    public Image curSlimeBorder;
    public GameObject avatar;
    public GameObject turnPanel;
    public GameObject slimeTurn;
    
    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultText;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Ẩn result panel khi bắt đầu
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        
        turnList = formationManager.slimeFormation;
        
        bool isTowerMode = false;
        bool isFarmMode = false;
        if (BattleDataManager.Instance != null)
        {
            isTowerMode = BattleDataManager.Instance.IsTowerMode();
            isFarmMode = BattleDataManager.Instance.IsFarmMode();
        }
        
        // Farm mode luôn có boss data từ FarmModeManager
        if (isFarmMode && BattleDataManager.Instance != null && BattleDataManager.Instance.HasBossData())
        {
            InitializeBossFromData(BattleDataManager.Instance.GetBossData());
            BattleDataManager.Instance.ClearBossDataExceptWildSlimeID();
        }
        else if (isTowerMode && (BattleDataManager.Instance == null || !BattleDataManager.Instance.HasBossData()))
        {
            InitializeBossFromTower();
        }
        else if (BattleDataManager.Instance != null && BattleDataManager.Instance.HasBossData())
        {
            InitializeBossFromData(BattleDataManager.Instance.GetBossData());
            BattleDataManager.Instance.ClearBossDataExceptWildSlimeID();
        }
        
        if (boss != null)
        {
            turnList.Add(boss);
            if (boss.GetComponent<SimpleCombatAnimation>() == null)
            {
                boss.AddComponent<SimpleCombatAnimation>();
            }
        }
        
        StartCoroutine(DelayedSetupCombatAnimations());
    }
    
    private void InitializeBossFromTower()
    {
        if (towerBosses == null)
        {
            Debug.LogError("TowerSlimeBosses is not assigned in TurnSystem!");
            return;
        }
        
        var currentFloor = towerBosses.replayFloor > 0
            ? towerBosses.GetFloor(towerBosses.replayFloor)
            : towerBosses.GetCurrentFloor();
        if (currentFloor == null)
        {
            Debug.LogError($"No current floor found! Current floor index: {towerBosses.currentFloor}");
            return;
        }
        
        Slime bossSlime = towerBosses.CreateBossSlimeFromFloor(currentFloor);
        if (bossSlime == null)
        {
            Debug.LogError("Failed to create boss slime from tower floor!");
            return;
        }
        
        if (BattleDataManager.Instance == null)
        {
            GameObject battleDataManagerGO = new GameObject("BattleDataManager");
            battleDataManagerGO.AddComponent<BattleDataManager>();
        }
        BattleDataManager.Instance.SetBattleMode(BattleMode.Tower);
        
        InitializeBossFromData(bossSlime);
    }
    
    private void InitializeBossFromData(Slime slimeData)
    {
        if (boss == null || slimeData == null) return;
        
        SlimeStats bossStats = boss.GetComponent<SlimeStats>();
        if (bossStats == null)
        {
            bossStats = boss.AddComponent<SlimeStats>();
        }
        
        bossStats.HP = slimeData.totalHP;
        bossStats.MaxHP = slimeData.totalHP;
        bossStats.Attack = slimeData.totalAttack;
        bossStats.Defense = slimeData.totalDefense;
        bossStats.Speed = slimeData.totalSpeed;
        bossStats.Evade = slimeData.totalEvade;
        bossStats.isEnemy = true;
        
        if (slimeData.body?.skill != null)
            bossStats.bodySkill = slimeData.body.skill;
        if (slimeData.armor?.skill != null)
            bossStats.armorSkill = slimeData.armor.skill;
        if (slimeData.weapon?.skill != null)
            bossStats.weaponSkill = slimeData.weapon.skill;
        
        SetupBossVisuals(slimeData);
    }
    
    private void SetupBossVisuals(Slime slimeData)
    {
        if (boss == null || slimeData == null) return;
        
        var bossStats = boss.GetComponent<SlimeStats>();
        if (bossStats == null) return;
        
        if (bossStats.skeletonGraphic != null && slimeData.body != null)
        {
            if (slimeData.body.hasAnimation && slimeData.body.animationAsset != null)
            {
                bossStats.skeletonGraphic.skeletonDataAsset = slimeData.body.animationAsset;
                bossStats.skeletonGraphic.Initialize(true);
                bossStats.skeletonGraphic.AnimationState.SetAnimation(0, slimeData.body.animationName ?? "animation", true);
            }
        }
        
        if (bossStats.armor != null && slimeData.armor != null)
        {
            bossStats.armor.sprite = slimeData.armor.sprite;
        }
        
        if (bossStats.weapon != null && slimeData.weapon != null)
        {
            bossStats.weapon.sprite = slimeData.weapon.sprite;
        }
    }

    IEnumerator turnDisplay()
    {
        foreach (Transform child in turnPanel.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        yield return new WaitForSeconds(0.3f);
        foreach (GameObject go in turnQueue)
        {
            GameObject turn = Instantiate(slimeTurn, turnPanel.transform);
            var display = turn.GetComponent<TurnDisplay>();
            display.body.skeletonDataAsset = go.GetComponentInChildren<SkeletonGraphic>().skeletonDataAsset;
            display.body.allowMultipleCanvasRenderers = true;
            display.body.enableSeparatorSlots = true;
            display.body.Initialize(true);
            display.body.AnimationState.SetAnimation(0, "animation", true);
            display.body.timeScale = 2;
            display.hat.sprite = go.GetComponent<SlimeStats>().armor.sprite;
            display.weapon.sprite = go.GetComponent<SlimeStats>().weapon.sprite;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator DelayedSetupCombatAnimations()
    {
        // Đợi 1 giây để đảm bảo team slimes đã được tạo
        yield return new WaitForSeconds(1f);
        
        // Setup animation cho tất cả slime trong formation
        SetupCombatAnimations();
        
        // Setup battle stats và skills cho tất cả slimes
        SetupBattleStats();
        
    }

    private void SetupCombatAnimations()
    {
        // Setup cho tất cả slime trong formation
        foreach (var slime in formationManager.slimeFormation)
        {
            if (slime != null && slime.GetComponent<SimpleCombatAnimation>() == null)
            {
                slime.AddComponent<SimpleCombatAnimation>();
            }
        }
        
        // Setup cho boss (nếu chưa có)
        if (boss != null && boss.GetComponent<SimpleCombatAnimation>() == null)
        {
            boss.AddComponent<SimpleCombatAnimation>();
        }
    }
    
    private void SetupBattleStats()
    {
        // Setup SlimeBattleStats cho tất cả slime trong formation
        foreach (var slime in formationManager.slimeFormation)
        {
            if (slime != null && slime.GetComponent<SlimeBattleStats>() == null)
            {
                slime.AddComponent<SlimeBattleStats>();
            }
        }
        
        // Setup cho boss (nếu chưa có)
        if (boss != null && boss.GetComponent<SlimeBattleStats>() == null)
        {
            boss.AddComponent<SlimeBattleStats>();
        }
    }
    
    private void EnsureAllSlimesHaveAnimation()
    {
        // Kiểm tra và thêm animation cho tất cả slimes trong turnList
        foreach (var slime in turnList)
        {
            if (slime != null && slime.GetComponent<SimpleCombatAnimation>() == null)
            {
                slime.AddComponent<SimpleCombatAnimation>();
            }
        }
    }

    private void TurnSorting()
    {
        var sorted = turnList
            .Where(s => s != null)
            .OrderByDescending(s => {
                var battleStats = s.GetComponent<SlimeBattleStats>();
                return battleStats != null ? battleStats.BattleSpeed : s.GetComponent<SlimeStats>().Speed;
            })
            .ToList();
        turnQueue = new Queue<GameObject>(sorted);
    }

    public void StartGame()
    {
        // Log analytics khi trận đấu bắt đầu
        string battleMode = BattleDataManager.Instance != null
            ? BattleDataManager.Instance.GetBattleMode().ToString().ToLower()
            : "adventure";
        string difficulty = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode()
            ? (FarmModeManager.Instance?.SelectedDifficultyName ?? "unknown")
            : "";
        int teamSize = formationManager?.slimeFormation?.Count ?? 0;
        FirebaseAnalyticsManager.LogBattleStart(battleMode, difficulty, teamSize);

        foreach (var s in formationManager.slimeFormation)
        {
            if (s.GetComponent<SlimeStats>().bodySkill != null) s.GetComponent<SlimeStats>().bodySkill.currentCooldown = 0;
            if (s.GetComponent<SlimeStats>().armorSkill != null) s.GetComponent<SlimeStats>().armorSkill.currentCooldown = 0;
            if (s.GetComponent<SlimeStats>().weaponSkill != null) s.GetComponent<SlimeStats>().weaponSkill.currentCooldown = 0;
        }
        turnList = formationManager.slimeFormation
            .Where(s => s != null && s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
            .ToList();
        TurnSorting();
        avatar.SetActive(true);
        StartCoroutine(NextTurn());
    }
    IEnumerator NextTurn()
    {
        if (currentSlime != null) currentSlime.GetComponent<SlimeStats>().turnHalo.SetActive(false);
        yield return new WaitForSeconds(0.3f);
        if (turnQueue.Count == 0)
        {
            TurnSorting();
            EnsureAllSlimesHaveAnimation();
        }

        currentSlime = turnQueue.Dequeue();
        StartCoroutine(turnDisplay());

        var battleStats = currentSlime.GetComponent<SlimeBattleStats>();

        if (battleStats != null && battleStats.IsStunned)
        {
            Debug.Log($"{currentSlime.name} bị stun, mất lượt!");
            yield return new WaitForSeconds(0.8f);
            TickCurrentSlimeEffects();
            StartCoroutine(NextTurn());
            yield break;
        }

        TickCooldowns(currentSlime);

        if (currentSlime.GetComponent<SlimeStats>().isEnemy)
        {
            StartCoroutine(BossTurn());
        }
        else
        {
            if (battleStats == null || battleStats.CurrentHP <= 0 || !currentSlime.activeInHierarchy)
            {
                StartCoroutine(NextTurn());
            }
            else
            {
                PlayerTurn();
            }
        }
    }

    public void PlayerTurn()
    {
        turnCount++;
        skillPanel.SetActive(true);
        currentSlime.GetComponent<SlimeStats>().turnHalo.SetActive(true);
        curSlimeBody.skeletonDataAsset = currentSlime.GetComponentInChildren<SkeletonGraphic>().skeletonDataAsset;
        curSlimeBody.allowMultipleCanvasRenderers = true;
        curSlimeBody.enableSeparatorSlots = true;

        // Khởi tạo lại Skeleton
        curSlimeBody.Initialize(true);

        curSlimeBody.AnimationState.SetAnimation(0, "animation", true);
        curSlimeBody.timeScale = 2;
        curSlimeHat.sprite = currentSlime.GetComponent<SlimeStats>()?.armor.sprite;
        curSlimeWeapon.sprite = currentSlime.GetComponent<SlimeStats>()?.weapon.sprite;
        curSlimeBorder.color = Color.white;
        skillPanel.GetComponent<SkillUI>().slime = currentSlime.gameObject.GetComponent<SlimeStats>();
    }

    private void TickCooldowns(GameObject slime)
    {
        var stats = slime.GetComponent<SlimeStats>();
        if (stats == null) return;
        if (stats.bodySkill   != null && stats.bodySkill.currentCooldown   > 0) stats.bodySkill.currentCooldown--;
        if (stats.armorSkill  != null && stats.armorSkill.currentCooldown  > 0) stats.armorSkill.currentCooldown--;
        if (stats.weaponSkill != null && stats.weaponSkill.currentCooldown > 0) stats.weaponSkill.currentCooldown--;
    }

    // Gọi cuối lượt của chính slime đó (sau khi hành động hoặc bỏ lượt vì stun)
    private void TickCurrentSlimeEffects()
    {
        var battleStats = currentSlime?.GetComponent<SlimeBattleStats>();
        battleStats?.TickBuffs();
        battleStats?.TickStun();
    }
    public void DoAutoAttack()
    {
        StartCoroutine (AutoAttack());
    }

    IEnumerator AutoAttack()
    {
        var target = boss.GetComponent<SlimeBattleStats>();
        var attacker = currentSlime.GetComponent<SlimeBattleStats>();

        if (target != null && attacker != null && attacker.CurrentHP > 0)
        {
            // Lấy SimpleCombatAnimation của slime tấn công
            var attackerAnimController = currentSlime.GetComponent<SimpleCombatAnimation>();
            var targetAnimController = boss.GetComponent<SimpleCombatAnimation>();

            // Chơi animation tấn công
            if (attackerAnimController != null)
            {
                yield return StartCoroutine(attackerAnimController.PlayAttackAnimation(boss.transform));
            }

            // Check if target evades
            if (target.TryEvade())
            {
                Debug.Log($"{boss.name} evades the attack!");
                yield break;
            }

            // Calculate damage with critical hit chance
            int damage = attacker.BattleAttack;
            if (attacker.TryCriticalHit())
            {
                float critMult = RemoteConfigManager.Instance != null
                    ? RemoteConfigManager.Instance.CritDamageMultiplier : 1.5f;
                damage = Mathf.RoundToInt(damage * critMult);
                Debug.Log("Critical Hit!");
            }

            // Apply damage
            target.TakeDamage(damage);
            Debug.Log($"{currentSlime.name} attacks {boss.name} for {damage} damage!");

            // Chơi animation bị đánh cho target
            if (targetAnimController != null)
            {
                yield return StartCoroutine(targetAnimController.PlayHitAnimation());
            }

            if (target.CurrentHP <= 0)
            {
                Debug.Log($"{boss.name} died!");
                // Thắng - thuần hóa slime và quay về adventure scene
                yield return StartCoroutine(HandleVictory());
                yield break;
            }
            else
            {
                // Kiểm tra xem team còn sống không
                if (CheckLoseCondition())
                {
                    yield return StartCoroutine(HandleDefeat());
                    yield break;
                }
                yield return new WaitForSeconds(1f);
                TickCurrentSlimeEffects();
                StartCoroutine(NextTurn());
            }
        }
    }
    public void UseBodySkill()
    {
        if (currentSlime.GetComponent<SlimeStats>().bodySkill.baseSkill != null)
        {
            if (currentSlime.GetComponent<SlimeStats>().bodySkill.currentCooldown == 0)
            {
                skillPanel.SetActive(false);
                StartCoroutine(DoSkill(currentSlime.GetComponent<SlimeStats>().bodySkill, boss));
            }
            else Debug.Log($"Skill is on Cooldown {currentSlime.GetComponent<SlimeStats>().bodySkill.currentCooldown}");
        }
        else Debug.Log("this slime have no Body skill");
    }

    public void UseHatSkill()
    {
        if (currentSlime.GetComponent<SlimeStats>().armorSkill.baseSkill != null)
        {
            if (currentSlime.GetComponent<SlimeStats>().armorSkill.currentCooldown <= 0)
            {
                skillPanel.SetActive(false);
                StartCoroutine(DoSkill(currentSlime.GetComponent<SlimeStats>().armorSkill, boss));
            }
            else Debug.Log($"Skill is on Cooldown {currentSlime.GetComponent<SlimeStats>().armorSkill.currentCooldown}");
        }
        else Debug.Log("this slime have no Armor skill");
    }

    public void UseWeaponSkill()
    {
        if (currentSlime.GetComponent<SlimeStats>().weaponSkill.baseSkill != null)
        {
            if (currentSlime.GetComponent<SlimeStats>().weaponSkill.currentCooldown <= 0)
            {
                skillPanel.SetActive(false);
                StartCoroutine(DoSkill(currentSlime.GetComponent<SlimeStats>().weaponSkill, boss));
            }
            else Debug.Log($"Skill is on Cooldown {currentSlime.GetComponent<SlimeStats>().weaponSkill.currentCooldown}");
        }
        else Debug.Log("this slime have no Weapon skill");
    }

    private IEnumerator DoSkill(SkillInstance skill, GameObject target)
    {
        var attacker = currentSlime.GetComponent<SlimeBattleStats>();
        var attackerAnim = currentSlime.GetComponent<SimpleCombatAnimation>();

        if (attacker == null || skill == null)
            yield break;

        // Animation tấn công của caster — play 1 lần trước toàn bộ effects
        if (attackerAnim != null)
            yield return StartCoroutine(attackerAnim.PlayAttackAnimation(target.transform));

        foreach (var entry in skill.baseSkill.effects)
        {
            if (entry.effect == null) continue;
            List<GameObject> targets = formationManager.ResolveTargets(entry, currentSlime, target, boss);

            foreach (var targetGo in targets)
            {
                var targetStats = targetGo.GetComponent<SlimeBattleStats>();
                if (targetStats == null) continue;

                switch (entry.effect.type)
                {
                    case EffectType.Damage:
                        float rawDamage = attacker.BattleAttack * skill.power * entry.value;
                        int finalDamage = Mathf.RoundToInt(rawDamage);
                        if (attacker.TryCriticalHit())
                        {
                            float critMult = RemoteConfigManager.Instance != null
                                ? RemoteConfigManager.Instance.CritDamageMultiplier : 1.5f;
                            finalDamage = Mathf.RoundToInt(finalDamage * critMult);
                            Debug.Log("Critical Hit!");
                        }
                        targetStats.TakeDamage(finalDamage);
                        Debug.Log($"{currentSlime.name} dùng {skill.baseSkill.skillName} lên {targetGo.name}: {finalDamage} damage");
                        var hitAnim = targetGo.GetComponent<SimpleCombatAnimation>();
                        if (hitAnim != null)
                            yield return StartCoroutine(hitAnim.PlayHitAnimation());
                        if (targetGo == boss && CheckWinCondition())
                        {
                            yield return StartCoroutine(HandleVictory());
                            yield break;
                        }
                        break;

                    case EffectType.Heal:
                        int healAmount = Mathf.RoundToInt(targetStats.MaxHP * skill.power * entry.value);
                        targetStats.Heal(healAmount);
                        Debug.Log($"{currentSlime.name} heal {targetGo.name} {healAmount} HP");
                        break;

                    case EffectType.Buff:
                        targetStats.ApplyBuff(entry.effect.buffStat, skill.power * entry.value, entry.duration, false);
                        Debug.Log($"{currentSlime.name} buff {entry.effect.buffStat} lên {targetGo.name} x{skill.power * entry.value:F2} ({entry.duration} lượt)");
                        break;

                    case EffectType.Debuff:
                        targetStats.ApplyBuff(entry.effect.buffStat, skill.power * entry.value, entry.duration, true);
                        Debug.Log($"{currentSlime.name} debuff {entry.effect.buffStat} lên {targetGo.name} x{skill.power * entry.value:F2} ({entry.duration} lượt)");
                        break;

                    case EffectType.Stun:
                        targetStats.ApplyStun(entry.duration);
                        Debug.Log($"{currentSlime.name} stun {targetGo.name} {entry.duration} lượt");
                        break;
                }

                yield return new WaitForSeconds(0.15f);
            }
        }

        skill.currentCooldown = skill.baseSkill.cooldown;

        if (CheckWinCondition())
        {
            yield return StartCoroutine(HandleVictory());
            yield break;
        }
        else if (CheckLoseCondition())
        {
            yield return StartCoroutine(HandleDefeat());
            yield break;
        }

        yield return new WaitForSeconds(1f);
        TickCurrentSlimeEffects();
        StartCoroutine(NextTurn());
    }

    IEnumerator BossTurn()
    {
        turnCount++;
        curSlimeBody.skeletonDataAsset = currentSlime.GetComponentInChildren<SkeletonGraphic>().skeletonDataAsset;
        curSlimeBody.allowMultipleCanvasRenderers = true;
        curSlimeBody.enableSeparatorSlots = true;

        // Khởi tạo lại Skeleton
        curSlimeBody.Initialize(true);

        curSlimeBody.AnimationState.SetAnimation(0, "animation", true);
        curSlimeBody.timeScale = 2;
        curSlimeHat.sprite = currentSlime.GetComponent<SlimeStats>()?.armor.sprite;
        curSlimeWeapon.sprite = currentSlime.GetComponent<SlimeStats>()?.weapon.sprite;
        curSlimeBorder.color = Color.red;

        var target = formationManager.GetRandomRowLastAlive();
        currentSlime.GetComponent<SlimeStats>().turnHalo.SetActive(true);
        if (target != null)
        {
            var bossStats = boss.GetComponent<SlimeBattleStats>();
            var targetStats = target.GetComponent<SlimeBattleStats>();
            
            // Lấy SimpleCombatAnimation của boss và target
            var bossAnimController = boss.GetComponent<SimpleCombatAnimation>();
            var targetAnimController = target.GetComponent<SimpleCombatAnimation>();
            
            // Chơi animation tấn công cho boss
            if (bossAnimController != null)
            {
                yield return StartCoroutine(bossAnimController.PlayAttackAnimation(target.transform));
            }
            
            // Check if target evades
            if (targetStats != null && targetStats.TryEvade())
            {
                Debug.Log($"{target.name} evades the boss attack!");
            }
            else
            {
                // Calculate damage
                int damage = bossStats != null ? bossStats.BattleAttack : boss.GetComponent<SlimeStats>().Attack;
                
                // Apply damage
                if (targetStats != null)
                {
                    targetStats.TakeDamage(damage);
                }
                else
                {
                    target.GetComponent<SlimeStats>().HP -= damage;
                }
                
                Debug.Log($"{boss.name} attacks {target.name} for {damage} damage!");
                
                // Chơi animation bị đánh cho target
                if (targetAnimController != null)
                {
                    yield return StartCoroutine(targetAnimController.PlayHitAnimation());
                }
                
                int currentHP = targetStats != null ? targetStats.CurrentHP : target.GetComponent<SlimeStats>().HP;
                if (currentHP <= 0)
                {
                    Debug.Log($"{target.name} died!");
                }
            }
            
            // Kiểm tra điều kiện thắng/thua
            if (CheckWinCondition())
            {
                yield return StartCoroutine(HandleVictory());
                yield break;
            }
            else if (CheckLoseCondition())
            {
                yield return StartCoroutine(HandleDefeat());
                yield break;
            }

            yield return new WaitForSeconds(1f);
            TickCurrentSlimeEffects();
            StartCoroutine(NextTurn());
        }
        else
        {
            Debug.Log("You Lost!");
            yield return StartCoroutine(HandleDefeat());
            yield break;
        }
    }
    
    [ContextMenu("Debug Animation Components")]
    public void DebugAnimationComponents()
    {
    }
    
    [ContextMenu("Update All Formation Positions")]
    public void UpdateAllFormationPositions()
    {
        foreach (var slime in formationManager.slimeFormation)
        {
            if (slime != null)
            {
                var combatAnim = slime.GetComponent<SimpleCombatAnimation>();
                if (combatAnim != null)
                {
                    combatAnim.UpdateFormationPosition();
                }
            }
        }
        
        if (boss != null)
        {
            var bossAnim = boss.GetComponent<SimpleCombatAnimation>();
            if (bossAnim != null)
            {
                bossAnim.UpdateFormationPosition();
            }
        }
    }
    
    [ContextMenu("Force Reset All Scales")]
    public void ForceResetAllScales()
    {
        foreach (var slime in formationManager.slimeFormation)
        {
            if (slime != null)
            {
                var combatAnim = slime.GetComponent<SimpleCombatAnimation>();
                if (combatAnim != null)
                {
                    combatAnim.ForceResetScale();
                }
            }
        }
        
        if (boss != null)
        {
            var bossAnim = boss.GetComponent<SimpleCombatAnimation>();
            if (bossAnim != null)
            {
                bossAnim.ForceResetScale();
            }
        }
    }
    
    [ContextMenu("Force Set All Original Scales")]
    public void ForceSetAllOriginalScales()
    {
        foreach (var slime in formationManager.slimeFormation)
        {
            if (slime != null)
            {
                var combatAnim = slime.GetComponent<SimpleCombatAnimation>();
                if (combatAnim != null)
                {
                    combatAnim.ForceSetOriginalScale();
                }
            }
        }
        
        if (boss != null)
        {
            var bossAnim = boss.GetComponent<SimpleCombatAnimation>();
            if (bossAnim != null)
            {
                bossAnim.ForceSetOriginalScale();
            }
        }
    }
    
    // Kiểm tra điều kiện thắng (boss HP = 0)
    private bool CheckWinCondition()
    {
        if (boss == null) return false;
        
        var bossStats = boss.GetComponent<SlimeBattleStats>();
        if (bossStats != null)
        {
            return bossStats.CurrentHP <= 0;
        }
        else
        {
            var bossSlimeStats = boss.GetComponent<SlimeStats>();
            return bossSlimeStats != null && bossSlimeStats.HP <= 0;
        }
    }
    
    // Kiểm tra điều kiện thua (tất cả team slimes HP = 0)
    private bool CheckLoseCondition()
    {
        if (formationManager == null || formationManager.slimeFormation == null)
            return false;
        
        foreach (var slime in formationManager.slimeFormation)
        {
            if (slime == null) continue;
            
            var battleStats = slime.GetComponent<SlimeBattleStats>();
            if (battleStats != null && battleStats.CurrentHP > 0)
            {
                return false; // Còn ít nhất 1 slime sống
            }
            else
            {
                var slimeStats = slime.GetComponent<SlimeStats>();
                if (slimeStats != null && slimeStats.HP > 0)
                {
                    return false; // Còn ít nhất 1 slime sống
                }
            }
        }
        
        return true; // Tất cả đều chết
    }
    
    private IEnumerator HandleVictory()
    {
        // Log analytics trước khi xử lý reward
        {
            string bMode = BattleDataManager.Instance?.GetBattleMode().ToString().ToLower() ?? "adventure";
            string diff  = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode()
                ? (FarmModeManager.Instance?.SelectedDifficultyName ?? "unknown") : "";
            int coinsEarned = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode()
                ? (FarmModeManager.Instance?.GetRewardCoins() ?? 0) : 0;
            FirebaseAnalyticsManager.LogBattleWin(bMode, diff, turnCount, coinsEarned);
        }

        // Thông báo quest system về trận thắng
        if (QuestManager.Instance != null && BattleDataManager.Instance != null)
            QuestManager.Instance.RegisterBattleWin(BattleDataManager.Instance.GetBattleMode());

        // Hiển thị panel kết quả thắng
        ShowResultPanel(true);
        
        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();
        bool isFarmMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode();
        
        if (isFarmMode)
        {
            // Xử lý farm mode victory
            if (FarmModeManager.Instance != null)
            {
                FarmModeManager.Instance.OnFarmVictory();
            }
            else
            {
                Debug.LogWarning("FarmModeManager.Instance is null! Không thể thêm coins.");
            }
            
            if (BattleDataManager.Instance != null)
            {
                BattleDataManager.Instance.ClearBossData();
            }
            
            yield return new WaitForSeconds(2f);
            
            // Quay về firstsave scene
            Debug.Log("Thắng farm mode, về firstsave");
            yield return SceneLoader.LoadSceneWithLoadingCoroutine("firstsave");
            
            yield break;
        }
        
        if (isTowerMode)
        {
            TowerSlimeBosses.TowerFloor currentFloor = null;
            if (towerBosses != null)
            {
                bool isReplay = towerBosses.replayFloor > 0;

                if (isReplay)
                {
                    // Chơi lại tầng đã qua — không thay đổi tiến trình, không nhận thưởng
                    Debug.Log($"Replay tầng {towerBosses.replayFloor} hoàn thành, không cộng thưởng.");
                    towerBosses.replayFloor = 0;
                }
                else
                {
                    currentFloor = towerBosses.GetCurrentFloor();

                    if (currentFloor != null)
                    {
                        currentFloor.completed = true;
                        Debug.Log($"Đã hoàn thành màn {currentFloor.floorNumber}: {currentFloor.floorName}");
                    }

                    towerBosses.AdvanceToNextFloor();

                    // Cache kết quả để SaveAndLoadSystem apply sau khi load cloud xong ở firstsave
                    towerBosses.cachedCompletedFloorNumber = currentFloor.floorNumber;
                    towerBosses.cachedCurrentFloor = towerBosses.currentFloor;
                    towerBosses.cachedHighestFloor = towerBosses.highestFloorReached;
                    towerBosses.hasPendingResult = true;
                }
            }
            
            if (BattleDataManager.Instance != null)
            {
                BattleDataManager.Instance.ClearBossData();
            }
            
            yield return new WaitForSeconds(2f);
            
            // Luôn về firstsave sau khi thắng tower
            Debug.Log("Thắng tower, về firstsave");
            yield return SceneLoader.LoadSceneWithLoadingCoroutine("firstsave");
            
            yield break;
        }
        
        if (BattleDataManager.Instance != null && wildSlimes != null)
        {
            var wildSlimeID = BattleDataManager.Instance.GetWildSlimeID();
            
            if (wildSlimeID >= 0)
            {
                WildSlimes.WildSlimeTraits wildSlimeTraits = null;
                for (int i = 0; i < wildSlimes.slimes.Count; i++)
                {
                    if (wildSlimes.slimes[i] != null && wildSlimes.slimes[i].slimeID == wildSlimeID)
                    {
                        wildSlimeTraits = wildSlimes.slimes[i];
                        break;
                    }
                }
                
                if (wildSlimeTraits != null)
                {
                    WildSlimes.WildSlimeTraits tamedSlime = new WildSlimes.WildSlimeTraits();
                    tamedSlime.slimeID = wildSlimeTraits.slimeID;
                    tamedSlime.slimeType = wildSlimeTraits.slimeType;
                    tamedSlime.wildSlimeTraits = new TraitSO[3];
                    for (int i = 0; i < 3 && i < wildSlimeTraits.wildSlimeTraits.Length; i++)
                    {
                        tamedSlime.wildSlimeTraits[i] = wildSlimeTraits.wildSlimeTraits[i];
                    }
                    
                    if (wildSlimes.tamedSlimes == null)
                    {
                        wildSlimes.tamedSlimes = new System.Collections.Generic.List<WildSlimes.WildSlimeTraits>();
                    }
                    if (wildSlimes.slimes == null)
                    {
                        wildSlimes.slimes = new System.Collections.Generic.List<WildSlimes.WildSlimeTraits>();
                    }
                    
                    wildSlimes.tamedSlimes.Add(tamedSlime);
                    wildSlimes.slimes.Remove(wildSlimeTraits);
                    
                    if (SaveAndLoadSystem.Instance != null)
                    {
                        SaveAndLoadSystem.Instance.Save();
                    }
                }
            }
            
            BattleDataManager.Instance.ClearBossData();
        }
        
        yield return new WaitForSeconds(2f);
        yield return SceneLoader.LoadSceneWithLoadingCoroutine("adventureSence");
    }
    
    private IEnumerator HandleDefeat()
    {
        // Log analytics khi thua
        {
            string bMode = BattleDataManager.Instance?.GetBattleMode().ToString().ToLower() ?? "adventure";
            string diff  = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode()
                ? (FarmModeManager.Instance?.SelectedDifficultyName ?? "unknown") : "";
            FirebaseAnalyticsManager.LogBattleLose(bMode, diff, turnCount);
        }

        // Hiển thị panel kết quả thua
        ShowResultPanel(false);
        
        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();
        bool isFarmMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode();
        
        if (BattleDataManager.Instance != null)
        {
            BattleDataManager.Instance.ClearBossData();
        }
        
        yield return new WaitForSeconds(2f);
        
        if (isFarmMode)
        {
            // Quay về firstsave scene khi thua farm mode
            yield return SceneLoader.LoadSceneWithLoadingCoroutine("firstsave");
        }
        else if (isTowerMode)
        {
            yield return SceneLoader.LoadSceneWithLoadingCoroutine("menu");
        }
        else
        {
            yield return SceneLoader.LoadSceneWithLoadingCoroutine("adventureSence");
        }
    }
    
    /// <summary>
    /// Hiển thị panel kết quả với text tương ứng
    /// </summary>
    private void ShowResultPanel(bool isVictory)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            
            if (resultText != null)
            {
                if (isVictory)
                {
                    resultText.text = "YOU WIN!";
                }
                else
                {
                    resultText.text = "YOU LOSE!";
                }
            }
        }
        else
        {
            Debug.LogWarning("Result Panel is not assigned in TurnSystem!");
        }
    }
}
