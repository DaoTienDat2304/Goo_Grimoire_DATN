using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Spine.Unity;

public class TowerTurnSystem : TurnSystem
{
    [Header("Tower Chapter 1 Wave Config")]
    public int currentWaveIndex = 0;
    public int totalWaves = 1;
    public List<GameObject> activeEnemies = new List<GameObject>();
    private Vector2 originalBossPos;
    private int slimeKingTurnCount = 0;
    private int activeTowerLevel = 1;

    [Header("Enemy Custom Visuals")]
    [SerializeField] public SkeletonDataAsset tinyBatSkeleton;
    [SerializeField] public SkeletonDataAsset slimeKingSkeleton;

    public enum TowerEnemyType { GreenSlime, TinyBat, SlimeKing }

    private void GetEnemyStats(TowerEnemyType type, int level, out int hp, out int atk, out int matk, out int def, out int spd, out float crit, out float critDMG)
    {
        hp = 100; atk = 10; matk = 10; def = 10; spd = 100; crit = 0.05f; critDMG = 1.50f;
        if (type == TowerEnemyType.GreenSlime)
        {
            switch (level)
            {
                case 1: hp = 900; atk = 90; matk = 70; def = 60; spd = 90; crit = 0.02f; critDMG = 1.50f; break;
                case 2: hp = 1004; atk = 99; matk = 77; def = 65; spd = 91; crit = 0.04f; critDMG = 1.50f; break;
                case 3: hp = 1119; atk = 109; matk = 85; def = 71; spd = 92; crit = 0.06f; critDMG = 1.50f; break;
                case 4: hp = 1248; atk = 120; matk = 94; def = 77; spd = 93; crit = 0.08f; critDMG = 1.50f; break;
                case 5: hp = 1392; atk = 133; matk = 104; def = 84; spd = 94; crit = 0.10f; critDMG = 1.50f; break;
            }
        }
        else if (type == TowerEnemyType.TinyBat)
        {
            switch (level)
            {
                case 3: hp = 780; atk = 105; matk = 60; def = 50; spd = 95; crit = 0.06f; critDMG = 1.50f; break;
                case 4: hp = 870; atk = 116; matk = 66; def = 55; spd = 96; crit = 0.08f; critDMG = 1.50f; break;
                case 5: hp = 970; atk = 128; matk = 73; def = 60; spd = 97; crit = 0.10f; critDMG = 1.50f; break;
            }
        }
        else if (type == TowerEnemyType.SlimeKing)
        {
            hp = 4250; atk = 150; matk = 100; def = 105; spd = 97; crit = 0.15f; critDMG = 1.70f;
        }
    }

    private List<List<TowerEnemyType>> GetLevelWaves(int levelNum, out List<List<int>> waveLevels)
    {
        var waves = new List<List<TowerEnemyType>>();
        waveLevels = new List<List<int>>();

        if (levelNum == 1)
        {
            waves.Add(new List<TowerEnemyType> { TowerEnemyType.GreenSlime, TowerEnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 1, 1 });
        }
        else if (levelNum == 2)
        {
            waves.Add(new List<TowerEnemyType> { TowerEnemyType.GreenSlime, TowerEnemyType.GreenSlime, TowerEnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 2, 2, 2 });
        }
        else if (levelNum == 3)
        {
            waves.Add(new List<TowerEnemyType> { TowerEnemyType.GreenSlime, TowerEnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 3, 3 });

            waves.Add(new List<TowerEnemyType> { TowerEnemyType.TinyBat, TowerEnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 3, 3 });
        }
        else if (levelNum == 4)
        {
            waves.Add(new List<TowerEnemyType> { TowerEnemyType.TinyBat, TowerEnemyType.GreenSlime, TowerEnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 4, 4, 4 });

            waves.Add(new List<TowerEnemyType> { TowerEnemyType.TinyBat, TowerEnemyType.TinyBat, TowerEnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 4, 4, 4 });
        }
        else if (levelNum == 5)
        {
            waves.Add(new List<TowerEnemyType> { TowerEnemyType.GreenSlime, TowerEnemyType.GreenSlime, TowerEnemyType.TinyBat });
            waveLevels.Add(new List<int> { 5, 5, 5 });

            waves.Add(new List<TowerEnemyType> { TowerEnemyType.TinyBat, TowerEnemyType.TinyBat, TowerEnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 5, 5, 5 });

            waves.Add(new List<TowerEnemyType> { TowerEnemyType.SlimeKing, TowerEnemyType.GreenSlime, TowerEnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 5, 5, 5 });
        }
        else // Fallback
        {
            waves.Add(new List<TowerEnemyType> { TowerEnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 1 });
        }

        return waves;
    }

    private void SpawnWave(int waveIndex)
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy != boss)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();

        if (boss != null) boss.SetActive(false);

        int levelNum = activeTowerLevel;

        TowerSlimeBosses.TowerFloor currentFloor = null;
        if (towerBosses != null)
        {
            currentFloor = towerBosses.replayFloor > 0 ? towerBosses.GetFloor(towerBosses.replayFloor) : towerBosses.GetCurrentFloor();
        }

        if (currentFloor != null && currentFloor.waves != null && currentFloor.waves.Count > 0)
        {
            totalWaves = currentFloor.waves.Count;
            if (waveIndex >= totalWaves) return;

            var waveConfig = currentFloor.waves[waveIndex];
            for (int i = 0; i < waveConfig.enemies.Count; i++)
            {
                var enemySetup = waveConfig.enemies[i];
                TowerEnemyType type = (TowerEnemyType)((int)enemySetup.enemyType);
                int level = enemySetup.level;

                SpawnEnemy(type, level, i, waveConfig.enemies.Count);
            }
        }
        else
        {
            List<List<int>> waveLevels;
            var levelWaves = GetLevelWaves(levelNum, out waveLevels);

            if (waveIndex >= levelWaves.Count) return;

            var currentWaveEnemies = levelWaves[waveIndex];
            var currentWaveLevels = waveLevels[waveIndex];
            totalWaves = levelWaves.Count;

            for (int i = 0; i < currentWaveEnemies.Count; i++)
            {
                TowerEnemyType type = currentWaveEnemies[i];
                int level = currentWaveLevels[i];

                SpawnEnemy(type, level, i, currentWaveEnemies.Count);
            }
        }

        boss = activeEnemies[0];

        foreach (var enemy in activeEnemies)
        {
            float enemySpd = GetSpeedOf(enemy);
            remainingAV[enemy] = 10000f / enemySpd;
        }
    }

    private void SpawnEnemy(TowerEnemyType type, int level, int i, int totalCount)
    {
        GameObject enemyGo = Instantiate(boss, boss.transform.parent);
        enemyGo.name = $"{type} Lv{level}";
        enemyGo.SetActive(true);

        RectTransform rect = enemyGo.GetComponent<RectTransform>();
        Vector2 offset = Vector2.zero;
        if (totalCount == 1)
        {
            offset = Vector2.zero;
        }
        else if (totalCount == 2)
        {
            offset = i == 0 ? new Vector2(0, 100) : new Vector2(0, -100);
        }
        else if (totalCount == 3)
        {
            if (i == 0) offset = new Vector2(80, 120);
            else if (i == 1) offset = new Vector2(0, 0);
            else offset = new Vector2(80, -120);
        }

        if (type == TowerEnemyType.TinyBat)
        {
            offset += new Vector2(0, 80);
        }

        rect.anchoredPosition = originalBossPos + offset;

        var spine = enemyGo.GetComponentInChildren<SkeletonGraphic>();
        if (type == TowerEnemyType.TinyBat)
        {
            if (tinyBatSkeleton != null && spine != null)
            {
                spine.skeletonDataAsset = tinyBatSkeleton;
                spine.Initialize(true);
                spine.AnimationState.SetAnimation(0, "animation", true);
            }
            else if (spine != null)
            {
                spine.color = new Color(0.6f, 0.2f, 0.8f);
            }
            enemyGo.transform.localScale = Vector3.one * 0.7f;

            // Ẩn nón và vũ khí khi biến hình thành dơi
            var slimeStats = enemyGo.GetComponent<SlimeStats>();
            if (slimeStats != null)
            {
                if (slimeStats.armor != null) slimeStats.armor.gameObject.SetActive(false);
                if (slimeStats.weapon != null) slimeStats.weapon.gameObject.SetActive(false);
            }
        }
        else if (type == TowerEnemyType.SlimeKing)
        {
            if (slimeKingSkeleton != null && spine != null)
            {
                spine.skeletonDataAsset = slimeKingSkeleton;
                spine.Initialize(true);
                spine.AnimationState.SetAnimation(0, "animation", true);
            }
            enemyGo.transform.localScale = Vector3.one * 1.5f;
        }
        else
        {
            enemyGo.transform.localScale = Vector3.one * 0.7f;
        }

        int hp, atk, matk, def, spd;
        float crit, critDMG;
        GetEnemyStats(type, level, out hp, out atk, out matk, out def, out spd, out crit, out critDMG);

        var stats = enemyGo.GetComponent<SlimeStats>();
        if (stats == null) stats = enemyGo.AddComponent<SlimeStats>();
        stats.HP = hp;
        stats.MaxHP = hp;
        stats.Attack = atk;
        stats.MagicAttack = matk;
        stats.Defense = def;
        stats.Speed = spd;
        stats.CritRate = crit;
        stats.CritDMG = critDMG;
        stats.isEnemy = true;

        var battleStats = enemyGo.GetComponent<SlimeBattleStats>();
        if (battleStats == null) battleStats = enemyGo.AddComponent<SlimeBattleStats>();
        battleStats.MaxHP = hp;
        battleStats.CurrentHP = hp;
        battleStats.BattleAttack = atk;
        battleStats.BattleMagicAttack = matk;
        battleStats.BattleDefense = def;
        battleStats.BattleSpeed = spd;
        battleStats.BattleCritRate = crit;
        battleStats.BattleCritDMG = critDMG;

        if (type == TowerEnemyType.SlimeKing)
        {
            stats.bodySkill = new SkillInstance(null);
        }

        activeEnemies.Add(enemyGo);
    }

    private void CheckWinLoseAfterEnemyDeath()
    {
        var nextAlive = activeEnemies.FirstOrDefault(e => e != null && e.GetComponent<SlimeBattleStats>().CurrentHP > 0);
        if (nextAlive != null)
        {
            boss = nextAlive;
        }
    }

    protected override void Start()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (boss != null)
        {
            originalBossPos = boss.GetComponent<RectTransform>().anchoredPosition;
        }

        turnList = formationManager.slimeFormation;

        bool isTowerMode = false;
        if (BattleDataManager.Instance != null)
        {
            isTowerMode = BattleDataManager.Instance.IsTowerMode();
        }

        if (isTowerMode && towerBosses != null)
        {
            var floor = towerBosses.replayFloor > 0 ? towerBosses.GetFloor(towerBosses.replayFloor) : towerBosses.GetCurrentFloor();
            int floorNum = floor != null ? floor.floorNumber : 1;
            if (floorNum >= 1 && floorNum <= 5)
            {
                activeTowerLevel = floorNum;
                currentWaveIndex = 0;
                SpawnWave(0);
            }
            else
            {
                base.Start();
                if (boss != null)
                {
                    activeEnemies.Add(boss);
                }
            }
        }
        else
        {
            base.Start();
            if (boss != null)
            {
                activeEnemies.Add(boss);
            }
        }
    }

    protected override IEnumerator NextTurn()
    {
        if (currentSlime != null) currentSlime.GetComponent<SlimeStats>().turnHalo.SetActive(false);
        yield return new WaitForSeconds(0.3f);

        // Kiểm tra chuyển wave nếu ở Tower Mode tầng 1-5
        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();
        if (isTowerMode && activeTowerLevel >= 1 && activeTowerLevel <= 5)
        {
            bool allWaveEnemiesDead = activeEnemies.All(e => e == null || e.GetComponent<SlimeBattleStats>().CurrentHP <= 0);
            if (allWaveEnemiesDead)
            {
                if (currentWaveIndex + 1 < totalWaves)
                {
                    currentWaveIndex++;
                    CreateDamagePopup(Vector3.up * 2f, $"WAVE {currentWaveIndex + 1}!", Color.red);
                    SpawnWave(currentWaveIndex);
                    InitializeAVSystem();
                    yield return new WaitForSeconds(1.0f);
                    StartCoroutine(NextTurn());
                    yield break;
                }
            }
        }

        // Lọc danh sách còn sống và active
        var activeParticipants = remainingAV.Keys
            .Where(s => s != null && s.activeInHierarchy && s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
            .ToList();

        if (activeParticipants.Count == 0)
        {
            InitializeAVSystem();
            activeParticipants = remainingAV.Keys
                .Where(s => s != null && s.activeInHierarchy && s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
                .ToList();
        }

        GameObject nextSlime = null;
        float minAV = float.MaxValue;
        foreach (var slime in activeParticipants)
        {
            if (remainingAV[slime] < minAV)
            {
                minAV = remainingAV[slime];
                nextSlime = slime;
            }
        }

        if (nextSlime != null)
        {
            foreach (var slime in activeParticipants)
            {
                remainingAV[slime] -= minAV;
            }

            float speed = GetSpeedOf(nextSlime);
            remainingAV[nextSlime] = 10000f / speed;

            currentSlime = nextSlime;
        }
        else
        {
            currentSlime = boss;
        }

        StartCoroutine(turnDisplay());

        var battleStats = currentSlime.GetComponent<SlimeBattleStats>();

        if (battleStats != null)
        {
            battleStats.TickDoTs();
            if (battleStats.CurrentHP <= 0)
            {
                Debug.Log($"{currentSlime.name} chết vì DoT!");
                CheckWinLoseAfterEnemyDeath();
                if (CheckWinCondition())
                {
                    yield return StartCoroutine(HandleVictory());
                    yield break;
                }
                yield return new WaitForSeconds(0.8f);
                StartCoroutine(NextTurn());
                yield break;
            }
            battleStats.TickBuffs();
            battleStats.TickStun();
        }

        if (battleStats != null && battleStats.IsStunned)
        {
            Debug.Log($"{currentSlime.name} bị stun, mất lượt!");
            yield return new WaitForSeconds(0.8f);
            StartCoroutine(NextTurn());
            yield break;
        }

        if (currentSlime.GetComponent<SlimeStats>().isEnemy)
        {
            yield return StartCoroutine(BossTurn());
        }
        else
        {
            skillPanel.SetActive(true);
            memberPanel.SetActive(true);
        }
    }

    protected override IEnumerator AutoAttack()
    {
        var target = boss.GetComponent<SlimeBattleStats>();
        var attacker = currentSlime.GetComponent<SlimeBattleStats>();

        if (attacker != null)
        {
            int damage = attacker.GetEffectiveAttack();
            bool isCrit = attacker.TryCriticalHit();
            if (isCrit)
            {
                float critMult = attacker.GetFinalCritDMG();
                damage = Mathf.RoundToInt(damage * critMult);
                Debug.Log("Critical Hit!");
            }

            if (target != null)
            {
                target.TakeDamage(damage);
                if (isCrit)
                {
                    CreateDamagePopup(boss.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
                }
            }

            var attackerAnim = currentSlime.GetComponent<SimpleCombatAnimation>();
            if (attackerAnim != null)
            {
                yield return StartCoroutine(attackerAnim.PlayAttackAnimation(boss.transform));
            }

            var targetAnimController = boss.GetComponent<SimpleCombatAnimation>();
            if (targetAnimController != null)
            {
                yield return StartCoroutine(targetAnimController.PlayHitAnimation());
            }

            if (target.CurrentHP <= 0)
            {
                Debug.Log($"{boss.name} died!");
                CheckWinLoseAfterEnemyDeath();
            }

            if (CheckWinCondition())
            {
                yield return StartCoroutine(HandleVictory());
                yield break;
            }
            else
            {
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

    protected override IEnumerator DoSkill(SkillInstance skill, GameObject target)
    {
        var attacker = currentSlime.GetComponent<SlimeBattleStats>();
        var attackerAnim = currentSlime.GetComponent<SimpleCombatAnimation>();

        if (attacker == null || skill == null)
            yield break;

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
                        float baseSkillDmg = 0.8f * attacker.GetEffectiveAttack() + 1.2f * attacker.GetEffectiveMagicAttack();
                        float rawDamage = baseSkillDmg * skill.power * entry.value;

                        int finalDamage = Mathf.RoundToInt(rawDamage);
                        bool isCrit = attacker.TryCriticalHit();
                        if (isCrit)
                        {
                            float critMult = attacker.GetFinalCritDMG();
                            finalDamage = Mathf.RoundToInt(finalDamage * critMult);
                            Debug.Log("Critical Hit!");
                        }
                        targetStats.TakeDamage(finalDamage);
                        if (isCrit)
                        {
                            CreateDamagePopup(targetGo.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
                        }
                        Debug.Log($"{currentSlime.name} dùng {skill.baseSkill.skillName} lên {targetGo.name}: {finalDamage} damage");
                        var hitAnim = targetGo.GetComponent<SimpleCombatAnimation>();
                        if (hitAnim != null)
                            yield return StartCoroutine(hitAnim.PlayHitAnimation());
                        if (targetStats.CurrentHP <= 0)
                        {
                            CheckWinLoseAfterEnemyDeath();
                        }
                        break;

                    case EffectType.Heal:
                        int healAmount = Mathf.RoundToInt(targetStats.MaxHP * skill.power * entry.value);
                        targetStats.Heal(healAmount);
                        break;

                    case EffectType.Buff:
                        targetStats.ApplyBuff(entry.effect.buffStat, skill.power * entry.value, entry.duration, false);
                        break;

                    case EffectType.Debuff:
                        targetStats.ApplyBuff(entry.effect.buffStat, skill.power * entry.value, entry.duration, true);
                        break;

                    case EffectType.Stun:
                        targetStats.ApplyStun(entry.duration);
                        break;

                    case EffectType.Poison:
                    case EffectType.Bleed:
                        int dotDmg = Mathf.RoundToInt(targetStats.MaxHP * skill.power * entry.value);
                        targetStats.ApplyDoT(entry.effect.type, dotDmg, entry.duration);
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
        else
        {
            yield return new WaitForSeconds(1f);
            TickCurrentSlimeEffects();
            StartCoroutine(NextTurn());
        }
    }

    protected override IEnumerator BossTurn()
    {
        turnCount++;
        curSlimeBody.skeletonDataAsset = currentSlime.GetComponentInChildren<SkeletonGraphic>().skeletonDataAsset;
        curSlimeBody.allowMultipleCanvasRenderers = true;
        curSlimeBody.enableSeparatorSlots = true;

        curSlimeBody.Initialize(true);
        curSlimeBody.AnimationState.SetAnimation(0, "animation", true);
        curSlimeBody.timeScale = 2;
        curSlimeHat.sprite = currentSlime.GetComponent<SlimeStats>()?.armor.sprite;
        curSlimeWeapon.sprite = currentSlime.GetComponent<SlimeStats>()?.weapon.sprite;
        curSlimeBorder.color = Color.red;

        var target = formationManager.GetRandomRowLastAlive();
        currentSlime.GetComponent<SlimeStats>().turnHalo.SetActive(true);

        bool isSlimeKing = currentSlime.name.Contains("SlimeKing");
        var bossStats = currentSlime.GetComponent<SlimeBattleStats>();

        if (isSlimeKing && bossStats != null)
        {
            slimeKingTurnCount++;
            int cycleTurn = (slimeKingTurnCount - 1) % 6 + 1;

            if (cycleTurn == 1) // Slime Splash
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Slime Splash!", Color.cyan);
                var allies = formationManager.GetAllAliveAllies();
                foreach (var ally in allies)
                {
                    var allyStats = ally.GetComponent<SlimeBattleStats>();
                    if (allyStats != null)
                    {
                        int rawDmg = Mathf.RoundToInt(bossStats.BattleMagicAttack * 1.3f);
                        allyStats.TakeDamage(rawDmg);
                        float mult = (allyStats.BattleSpeed - 10f) / allyStats.BattleSpeed;
                        allyStats.ApplyBuff(BuffStat.Speed, mult, 2, true);
                    }
                }
                yield return new WaitForSeconds(1f);
            }
            else if (cycleTurn == 3) // Absorb
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Absorb!", Color.green);
                int minionCount = activeEnemies.Count(e => e != null && e != currentSlime && e.GetComponent<SlimeBattleStats>().CurrentHP > 0);
                float healPct = 0.12f + (0.05f * minionCount);
                int healAmount = Mathf.RoundToInt(bossStats.MaxHP * healPct);
                bossStats.CurrentHP = Mathf.Min(bossStats.MaxHP, bossStats.CurrentHP + healAmount);
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.2f, $"+{healAmount} HP", Color.green);
                yield return new WaitForSeconds(1f);
            }
            else if (cycleTurn == 5) // Charge Ultimate
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "CHARGING ULTIMATE...", Color.red);
                yield return new WaitForSeconds(1f);
            }
            else if (cycleTurn == 6) // Acid Rain
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Acid Rain (ULTIMATE)!", Color.red);
                var allies = formationManager.GetAllAliveAllies();
                foreach (var ally in allies)
                {
                    var allyStats = ally.GetComponent<SlimeBattleStats>();
                    if (allyStats != null)
                    {
                        int rawDmg = Mathf.RoundToInt(bossStats.BattleMagicAttack * 1.5f);
                        allyStats.TakeDamage(rawDmg);
                        int poisonDmg = Mathf.RoundToInt(allyStats.MaxHP * 0.10f);
                        allyStats.ApplyDoT(EffectType.Poison, poisonDmg, 2);
                    }
                }
                yield return new WaitForSeconds(1f);
            }
            else // Basic Attack
            {
                if (target != null)
                {
                    var targetStats = target.GetComponent<SlimeBattleStats>();
                    var bossAnimController = currentSlime.GetComponent<SimpleCombatAnimation>();
                    var targetAnimController = target.GetComponent<SimpleCombatAnimation>();

                    if (bossAnimController == null) bossAnimController = currentSlime.AddComponent<SimpleCombatAnimation>();
                    if (bossAnimController != null) yield return StartCoroutine(bossAnimController.PlayAttackAnimation(target.transform));

                    int damage = bossStats.GetEffectiveAttack();
                    bool isCrit = bossStats.TryCriticalHit();
                    if (isCrit)
                    {
                        damage = Mathf.RoundToInt(damage * bossStats.GetFinalCritDMG());
                    }

                    if (targetStats != null)
                    {
                        targetStats.TakeDamage(damage);
                        if (isCrit) CreateDamagePopup(target.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
                    }

                    if (targetAnimController != null) yield return StartCoroutine(targetAnimController.PlayHitAnimation());
                }
            }
        }
        else // Tiny Bat, Green Slime, or normal bosses
        {
            if (target != null)
            {
                var targetStats = target.GetComponent<SlimeBattleStats>();
                var bossAnimController = currentSlime.GetComponent<SimpleCombatAnimation>();
                var targetAnimController = target.GetComponent<SimpleCombatAnimation>();

                if (bossAnimController == null) bossAnimController = currentSlime.AddComponent<SimpleCombatAnimation>();
                if (bossAnimController != null) yield return StartCoroutine(bossAnimController.PlayAttackAnimation(target.transform));

                int damage = bossStats != null ? bossStats.GetEffectiveAttack() : currentSlime.GetComponent<SlimeStats>().Attack;
                bool isCrit = bossStats != null && bossStats.TryCriticalHit();
                if (isCrit)
                {
                    damage = Mathf.RoundToInt(damage * (bossStats != null ? bossStats.GetFinalCritDMG() : 1.5f));
                }

                if (targetStats != null)
                {
                    targetStats.TakeDamage(damage);
                    if (isCrit) CreateDamagePopup(target.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
                }
                else
                {
                    target.GetComponent<SlimeStats>().HP -= damage;
                }

                if (targetAnimController != null) yield return StartCoroutine(targetAnimController.PlayHitAnimation());
            }
        }

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
        else
        {
            yield return new WaitForSeconds(1f);
            TickCurrentSlimeEffects();
            StartCoroutine(NextTurn());
        }
    }

    protected override bool CheckWinCondition()
    {
        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();
        if (isTowerMode && activeTowerLevel >= 1 && activeTowerLevel <= 5)
        {
            bool isLastWave = (currentWaveIndex + 1 >= totalWaves);
            bool allEnemiesDead = activeEnemies.All(e => e == null || e.GetComponent<SlimeBattleStats>().CurrentHP <= 0);
            return isLastWave && allEnemiesDead;
        }
        return base.CheckWinCondition();
    }

    protected override IEnumerator HandleVictory()
    {
        ShowResultPanel(true);

        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();

        if (isTowerMode)
        {
            TowerSlimeBosses.TowerFloor currentFloor = null;
            if (towerBosses != null)
            {
                bool isReplay = towerBosses.replayFloor > 0;

                if (isReplay)
                {
                    towerBosses.replayFloor = 0;
                }
                else
                {
                    currentFloor = towerBosses.GetCurrentFloor();
                    if (currentFloor != null)
                    {
                        currentFloor.completed = true;
                    }

                    if (activeTowerLevel >= 1 && activeTowerLevel <= 5)
                    {
                        int gold = 50;
                        int gem = 1;
                        float marshmallowChance = 0.10f;
                        float commonChance = 0.10f;
                        float uncommonChance = 0.00f;
                        float rareChance = 0.00f;

                        switch (activeTowerLevel)
                        {
                            case 1:
                                gold = 50; gem = 1;
                                marshmallowChance = 0.10f; commonChance = 0.10f;
                                break;
                            case 2:
                                gold = 70; gem = 1;
                                marshmallowChance = 0.15f; commonChance = 0.15f; uncommonChance = 0.03f;
                                break;
                            case 3:
                                gold = 100; gem = 2;
                                marshmallowChance = 0.20f; commonChance = 0.20f; uncommonChance = 0.05f; rareChance = 0.01f;
                                break;
                            case 4:
                                gold = 150; gem = 2;
                                marshmallowChance = 0.25f; commonChance = 0.25f; uncommonChance = 0.10f; rareChance = 0.03f;
                                break;
                            case 5:
                                gold = 300; gem = 5;
                                marshmallowChance = 0.30f; commonChance = 0.30f; uncommonChance = 0.15f; rareChance = 0.08f;
                                break;
                        }

                        if (CurrencyManager.Instance != null)
                        {
                            CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, gold);
                            CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, gem);
                        }

                        if (ResourceManager.Instance != null && Random.Range(0f, 1f) < marshmallowChance)
                        {
                            ResourceManager.Instance.AddResource(ResourceType.Marshmallow, 1);
                            CreateDamagePopup(Vector3.up * 1f, "+1 Marshmallow Ball (S)", Color.green);
                        }

                        if (SlimeGen.Instance != null && BreedingManager.Instance != null)
                        {
                            float roll = Random.Range(0f, 1f);
                            Slime newSlime = null;
                            if (roll < rareChance)
                            {
                                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_Rare", Rarity.Rare);
                                CreateDamagePopup(Vector3.up * 1.5f, "NEW RARE SLIME!", Color.magenta);
                            }
                            else if (roll < rareChance + uncommonChance)
                            {
                                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_Uncommon", Rarity.Uncommon);
                                CreateDamagePopup(Vector3.up * 1.5f, "NEW UNCOMMON SLIME!", Color.cyan);
                            }
                            else if (roll < rareChance + uncommonChance + commonChance)
                            {
                                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_Common", Rarity.Common);
                                CreateDamagePopup(Vector3.up * 1.5f, "NEW COMMON SLIME!", Color.white);
                            }

                            if (newSlime != null)
                            {
                                BreedingManager.Instance.GetAllSlimes().Add(newSlime);
                            }
                        }
                    }

                    towerBosses.AdvanceToNextFloor();
                    towerBosses.cachedCompletedFloorNumber = currentFloor.floorNumber;
                    towerBosses.cachedCurrentFloor = towerBosses.currentFloor;
                    towerBosses.cachedHighestFloor = towerBosses.highestFloorReached;
                    towerBosses.hasPendingResult = true;
                }
            }

            if (SaveAndLoadSystem.Instance != null)
            {
                SaveAndLoadSystem.Instance.Save();
            }

            if (BattleDataManager.Instance != null)
            {
                BattleDataManager.Instance.ClearBossData();
            }

            yield return new WaitForSeconds(2f);
            yield return SceneLoader.LoadSceneWithLoadingCoroutine("firstsave");
            yield break;
        }

        yield return base.HandleVictory();
    }
}