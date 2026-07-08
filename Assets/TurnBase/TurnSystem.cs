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
    private Dictionary<GameObject, float> remainingAV = new Dictionary<GameObject, float>();
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

    [Header("Chapter 1 Levels Config (Wave System)")]
    public int currentWaveIndex = 0;
    public int totalWaves = 1;
    public List<GameObject> activeEnemies = new List<GameObject>();
    private Vector2 originalBossPos;
    private int slimeKingTurnCount = 0;
    private int activeTowerLevel = 1;

    public enum EnemyType { GreenSlime, TinyBat, SlimeKing }

    private void GetEnemyStats(EnemyType type, int level, out int hp, out int atk, out int matk, out int def, out int spd, out float crit, out float critDMG)
    {
        hp = 100; atk = 10; matk = 10; def = 10; spd = 100; crit = 0.05f; critDMG = 1.50f;
        if (type == EnemyType.GreenSlime)
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
        else if (type == EnemyType.TinyBat)
        {
            switch (level)
            {
                case 3: hp = 780; atk = 105; matk = 60; def = 50; spd = 95; crit = 0.06f; critDMG = 1.50f; break;
                case 4: hp = 870; atk = 116; matk = 66; def = 55; spd = 96; crit = 0.08f; critDMG = 1.50f; break;
                case 5: hp = 970; atk = 128; matk = 73; def = 60; spd = 97; crit = 0.10f; critDMG = 1.50f; break;
            }
        }
        else if (type == EnemyType.SlimeKing)
        {
            hp = 4250; atk = 150; matk = 100; def = 105; spd = 97; crit = 0.15f; critDMG = 1.70f;
        }
    }

    private List<List<EnemyType>> GetLevelWaves(int levelNum, out List<List<int>> waveLevels)
    {
        var waves = new List<List<EnemyType>>();
        waveLevels = new List<List<int>>();

        if (levelNum == 1)
        {
            waves.Add(new List<EnemyType> { EnemyType.GreenSlime, EnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 1, 1 });
        }
        else if (levelNum == 2)
        {
            waves.Add(new List<EnemyType> { EnemyType.GreenSlime, EnemyType.GreenSlime, EnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 2, 2, 2 });
        }
        else if (levelNum == 3)
        {
            waves.Add(new List<EnemyType> { EnemyType.GreenSlime, EnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 3, 3 });

            waves.Add(new List<EnemyType> { EnemyType.TinyBat, EnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 3, 3 });
        }
        else if (levelNum == 4)
        {
            waves.Add(new List<EnemyType> { EnemyType.TinyBat, EnemyType.GreenSlime, EnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 4, 4, 4 });

            waves.Add(new List<EnemyType> { EnemyType.TinyBat, EnemyType.TinyBat, EnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 4, 4, 4 });
        }
        else if (levelNum == 5)
        {
            waves.Add(new List<EnemyType> { EnemyType.GreenSlime, EnemyType.GreenSlime, EnemyType.TinyBat });
            waveLevels.Add(new List<int> { 5, 5, 5 });

            waves.Add(new List<EnemyType> { EnemyType.TinyBat, EnemyType.TinyBat, EnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 5, 5, 5 });

            waves.Add(new List<EnemyType> { EnemyType.SlimeKing, EnemyType.GreenSlime, EnemyType.GreenSlime });
            waveLevels.Add(new List<int> { 5, 5, 5 });
        }
        else // Fallback
        {
            waves.Add(new List<EnemyType> { EnemyType.GreenSlime });
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

        List<List<int>> waveLevels;
        var levelWaves = GetLevelWaves(levelNum, out waveLevels);

        if (waveIndex >= levelWaves.Count) return;

        var currentWaveEnemies = levelWaves[waveIndex];
        var currentWaveLevels = waveLevels[waveIndex];
        totalWaves = levelWaves.Count;

        for (int i = 0; i < currentWaveEnemies.Count; i++)
        {
            EnemyType type = currentWaveEnemies[i];
            int level = currentWaveLevels[i];

            GameObject enemyGo = Instantiate(boss, boss.transform.parent);
            enemyGo.name = $"{type} Lv{level}";
            enemyGo.SetActive(true);

            RectTransform rect = enemyGo.GetComponent<RectTransform>();
            Vector2 offset = Vector2.zero;
            if (currentWaveEnemies.Count == 1)
            {
                offset = Vector2.zero;
            }
            else if (currentWaveEnemies.Count == 2)
            {
                offset = i == 0 ? new Vector2(0, 100) : new Vector2(0, -100);
            }
            else if (currentWaveEnemies.Count == 3)
            {
                if (i == 0) offset = new Vector2(80, 120);
                else if (i == 1) offset = new Vector2(0, 0);
                else offset = new Vector2(80, -120);
            }

            if (type == EnemyType.TinyBat)
            {
                offset += new Vector2(0, 80);
            }

            rect.anchoredPosition = originalBossPos + offset;

            var spine = enemyGo.GetComponentInChildren<SkeletonGraphic>();
            if (type == EnemyType.TinyBat)
            {
                if (spine != null) spine.color = new Color(0.6f, 0.2f, 0.8f);
                enemyGo.transform.localScale = Vector3.one * 0.7f;
            }
            else if (type == EnemyType.SlimeKing)
            {
                enemyGo.transform.localScale = Vector3.one * 1.5f;
            }
            else
            {
                enemyGo.transform.localScale = Vector3.one * 1.0f;
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

            if (type == EnemyType.SlimeKing)
            {
                stats.bodySkill = new SkillInstance(null);
            }

            activeEnemies.Add(enemyGo);
        }

        boss = activeEnemies[0];

        foreach (var enemy in activeEnemies)
        {
            float enemySpd = GetSpeedOf(enemy);
            remainingAV[enemy] = 10000f / enemySpd;
        }
    }

    private void CheckWinLoseAfterEnemyDeath()
    {
        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();
        if (isTowerMode && activeTowerLevel >= 1 && activeTowerLevel <= 5)
        {
            var nextAlive = activeEnemies.FirstOrDefault(e => e != null && e.GetComponent<SlimeBattleStats>().CurrentHP > 0);
            if (nextAlive != null)
            {
                boss = nextAlive;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Ẩn result panel khi bắt đầu
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
        bool isFarmMode = false;
        bool isAdventureMode = true;
        if (BattleDataManager.Instance != null)
        {
            isTowerMode = BattleDataManager.Instance.IsTowerMode();
            isFarmMode = BattleDataManager.Instance.IsFarmMode();
            isAdventureMode = BattleDataManager.Instance.IsAdventureMode();
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
                InitializeBossFromTower();
                if (boss != null)
                {
                    activeEnemies.Add(boss);
                    if (boss.GetComponent<SimpleCombatAnimation>() == null)
                    {
                        boss.AddComponent<SimpleCombatAnimation>();
                    }
                }
            }
        }
        else
        {
            if (isFarmMode && BattleDataManager.Instance != null && BattleDataManager.Instance.HasBossData())
            {
                InitializeBossFromData(BattleDataManager.Instance.GetBossData());
                BattleDataManager.Instance.ClearBossDataExceptWildSlimeID();
            }
            else if (BattleDataManager.Instance != null && BattleDataManager.Instance.HasBossData())
            {
                InitializeBossFromData(BattleDataManager.Instance.GetBossData());
                BattleDataManager.Instance.ClearBossDataExceptWildSlimeID();
            }

            if (boss != null)
            {
                activeEnemies.Add(boss);
                if (boss.GetComponent<SimpleCombatAnimation>() == null)
                {
                    boss.AddComponent<SimpleCombatAnimation>();
                }
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

        // Tự động khôi phục SkillInstance từ TraitSO gốc cho Boss nếu bị null do lưu trữ
        if (slimeData.body != null && (slimeData.body.skill == null || slimeData.body.skill.baseSkill == null) && slimeData.body.baseTrait != null && slimeData.body.baseTrait.skill != null)
        {
            slimeData.body.skill = new SkillInstance(slimeData.body.baseTrait.skill);
            slimeData.body.skill.power = slimeData.body.GetRarityMultiplier(slimeData.body.Rarity) * 1.5f;
        }
        if (slimeData.armor != null && (slimeData.armor.skill == null || slimeData.armor.skill.baseSkill == null) && slimeData.armor.baseTrait != null && slimeData.armor.baseTrait.skill != null)
        {
            slimeData.armor.skill = new SkillInstance(slimeData.armor.baseTrait.skill);
            slimeData.armor.skill.power = slimeData.armor.GetRarityMultiplier(slimeData.armor.Rarity) * 1.5f;
        }
        if (slimeData.weapon != null && (slimeData.weapon.skill == null || slimeData.weapon.skill.baseSkill == null) && slimeData.weapon.baseTrait != null && slimeData.weapon.baseTrait.skill != null)
        {
            slimeData.weapon.skill = new SkillInstance(slimeData.weapon.baseTrait.skill);
            slimeData.weapon.skill.power = slimeData.weapon.GetRarityMultiplier(slimeData.weapon.Rarity) * 1.5f;
        }

        SlimeStats bossStats = boss.GetComponent<SlimeStats>();
        if (bossStats == null)
        {
            bossStats = boss.AddComponent<SlimeStats>();
        }

        bossStats.HP = slimeData.totalHP;
        bossStats.MaxHP = slimeData.totalHP;
        bossStats.Attack = slimeData.totalAttack;
        bossStats.MagicAttack = slimeData.totalMagicAttack;
        bossStats.Defense = slimeData.totalDefense;
        bossStats.Speed = slimeData.totalSpeed;
        bossStats.CritRate = slimeData.totalCritRate;
        bossStats.CritDMG = slimeData.totalCritDMG;
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
        List<GameObject> upcoming = GetUpcomingTurns(5);
        foreach (GameObject go in upcoming)
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
            .Where(s => s != null && s.activeInHierarchy && s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
            .OrderByDescending(s => {
                var battleStats = s.GetComponent<SlimeBattleStats>();
                return battleStats != null ? battleStats.BattleSpeed : s.GetComponent<SlimeStats>().Speed;
            })
            .ToList();
        turnQueue = new Queue<GameObject>(sorted);
    }

    public void StartGame()
    {
        // Vô hiệu hóa tính năng kéo thả khi trận đấu bắt đầu
        var dragHandlers = FindObjectsOfType<SlimeDragHandler>();
        foreach (var handler in dragHandlers)
        {
            handler.enabled = false;
        }

        // Log analytics khi trận đấu bắt đầu
        string battleMode = BattleDataManager.Instance != null
            ? BattleDataManager.Instance.GetBattleMode().ToString().ToLower()
            : "adventure";
        string difficulty = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode()
            ? (FarmModeManager.Instance?.SelectedDifficultyName ?? "unknown")
            : "";
        int teamSize = formationManager?.slimeFormation?.Count ?? 0;
        FirebaseAnalyticsManager.LogBattleStart(battleMode, difficulty, teamSize);

        // Chỉ đưa vào danh sách tham chiến (turnList) những Slime đã được kéo thả ra sân (isUsed) và Boss
        turnList = formationManager.slimeFormation
            .Where(s => s != null && (s == boss || s.GetComponent<SlimeDragHandler>() == null || s.GetComponent<SlimeDragHandler>().isUsed))
            .Where(s => s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
            .ToList();

        foreach (var s in turnList)
        {
            var stats = s.GetComponent<SlimeStats>();
            if (stats != null)
            {
                if (stats.bodySkill != null) stats.bodySkill.currentCooldown = 0;
                if (stats.armorSkill != null) stats.armorSkill.currentCooldown = 0;
                if (stats.weaponSkill != null) stats.weaponSkill.currentCooldown = 0;
            }
        }

        InitializeAVSystem();
        avatar.SetActive(true);
        StartCoroutine(NextTurn());
    }

    private void InitializeAVSystem()
    {
        remainingAV.Clear();
        foreach (var slime in turnList)
        {
            if (slime == null) continue;
            float speed = GetSpeedOf(slime);
            remainingAV[slime] = 10000f / speed;
        }
    }

    private float GetSpeedOf(GameObject slime)
    {
        var stats = slime.GetComponent<SlimeBattleStats>();
        if (stats != null) return Mathf.Max(1f, stats.BattleSpeed);

        var slimeStats = slime.GetComponent<SlimeStats>();
        if (slimeStats != null) return Mathf.Max(1f, slimeStats.Speed);

        return 100f; // fallback
    }

    public void OnSpeedChanged(GameObject slime, float oldSpeed, float newSpeed)
    {
        if (remainingAV.ContainsKey(slime))
        {
            remainingAV[slime] = remainingAV[slime] * (oldSpeed / newSpeed);
            Debug.Log($"{slime.name}: Speed changed from {oldSpeed} to {newSpeed}. Re-calculated AV to {remainingAV[slime]}");
        }
    }

    public List<GameObject> GetUpcomingTurns(int count)
    {
        List<GameObject> upcoming = new List<GameObject>();
        Dictionary<GameObject, float> tempAV = new Dictionary<GameObject, float>();

        foreach (var kvp in remainingAV)
        {
            if (kvp.Key != null && kvp.Key.activeInHierarchy && kvp.Key.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
            {
                tempAV[kvp.Key] = kvp.Value;
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (tempAV.Count == 0) break;

            GameObject next = null;
            float minVal = float.MaxValue;
            foreach (var kvp in tempAV)
            {
                if (kvp.Value < minVal)
                {
                    minVal = kvp.Value;
                    next = kvp.Key;
                }
            }

            if (next == null) break;

            upcoming.Add(next);
            float speed = GetSpeedOf(next);
            tempAV[next] = minVal + (10000f / speed);
        }

        return upcoming;
    }

    IEnumerator NextTurn()
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
            // Reset nếu trống
            InitializeAVSystem();
            activeParticipants = remainingAV.Keys
                .Where(s => s != null && s.activeInHierarchy && s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
                .ToList();
        }

        // Chọn nhân vật có Action Value (AV) nhỏ nhất để đi tiếp
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
            // Trừ AV đã đi qua cho các nhân vật khác (để tiến hành hành động)
            foreach (var slime in activeParticipants)
            {
                remainingAV[slime] -= minAV;
            }

            // Đặt lại AV mới cho nhân vật vừa hành động xong
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

        // Tích giảm thời gian Buff và Stun vào ĐẦU lượt của Slime đó (Chuẩn RPG)
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
        if (stats.bodySkill != null && stats.bodySkill.currentCooldown > 0) stats.bodySkill.currentCooldown--;
        if (stats.armorSkill != null && stats.armorSkill.currentCooldown > 0) stats.armorSkill.currentCooldown--;
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
        StartCoroutine(AutoAttack());
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

            // Tính dame: Dame thường = ATK * (1 - thủ của enemy * 0.008)
            // ATK hiệu dụng bao gồm mọi phần thưởng từ lượng Crit DMG vượt mức
            int damage = attacker.GetEffectiveAttack();
            bool isCrit = attacker.TryCriticalHit();
            if (isCrit)
            {
                float critMult = attacker.GetFinalCritDMG();
                damage = Mathf.RoundToInt(damage * critMult);
                Debug.Log("Critical Hit!");
            }

            // Áp dụng sát thương (TakeDamage của mục tiêu sẽ xử lý phòng thủ và hiển thị popup)
            target.TakeDamage(damage);

            // Hiển thị thông báo CRIT nếu là hit chí mạng
            if (isCrit)
            {
                CreateDamagePopup(target.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
            }

            Debug.Log($"{currentSlime.name} attacks {boss.name} for {damage} damage!");

            // Chơi animation bị đánh cho target
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

        bool isSlimeKing = currentSlime.name.Contains("SlimeKing");
        var bossStats = currentSlime.GetComponent<SlimeBattleStats>();

        if (isSlimeKing && bossStats != null)
        {
            slimeKingTurnCount++;
            int cycleTurn = (slimeKingTurnCount - 1) % 6 + 1;
            Debug.Log($"Slime King action turn: {cycleTurn}");

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
                        // Giảm chính xác 10 Speed
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
            else if (cycleTurn == 6) // Acid Rain (Ultimate)
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
                        // Gây độc Poison, rút 10% MaxHP mỗi lượt
                        int poisonDmg = Mathf.RoundToInt(allyStats.MaxHP * 0.10f);
                        allyStats.ApplyDoT(EffectType.Poison, poisonDmg, 2);
                    }
                }
                yield return new WaitForSeconds(1f);
            }
            else // Basic Attack (Turn 2, 4)
            {
                if (target != null)
                {
                    var targetStats = target.GetComponent<SlimeBattleStats>();
                    var bossAnimController = currentSlime.GetComponent<SimpleCombatAnimation>();
                    var targetAnimController = target.GetComponent<SimpleCombatAnimation>();

                    if (bossAnimController != null)
                    {
                        yield return StartCoroutine(bossAnimController.PlayAttackAnimation(target.transform));
                    }

                    int damage = bossStats.GetEffectiveAttack();
                    bool isCrit = bossStats.TryCriticalHit();
                    if (isCrit)
                    {
                        float critMult = bossStats.GetFinalCritDMG();
                        damage = Mathf.RoundToInt(damage * critMult);
                        Debug.Log("Boss Critical Hit!");
                    }

                    if (targetStats != null)
                    {
                        targetStats.TakeDamage(damage);
                        if (isCrit)
                        {
                            CreateDamagePopup(target.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
                        }
                    }

                    if (targetAnimController != null)
                    {
                        yield return StartCoroutine(targetAnimController.PlayHitAnimation());
                    }
                }
            }
        }
        else // Normal enemies (Tiny Bat, Green Slime, or default bosses)
        {
            if (target != null)
            {
                var targetStats = target.GetComponent<SlimeBattleStats>();
                var bossAnimController = currentSlime.GetComponent<SimpleCombatAnimation>();
                var targetAnimController = target.GetComponent<SimpleCombatAnimation>();

                if (bossAnimController == null)
                {
                    bossAnimController = currentSlime.AddComponent<SimpleCombatAnimation>();
                }

                if (bossAnimController != null)
                {
                    yield return StartCoroutine(bossAnimController.PlayAttackAnimation(target.transform));
                }

                int damage = bossStats != null ? bossStats.GetEffectiveAttack() : currentSlime.GetComponent<SlimeStats>().Attack;
                bool isCrit = bossStats != null && bossStats.TryCriticalHit();
                if (isCrit)
                {
                    float critMult = bossStats != null ? bossStats.GetFinalCritDMG() : 1.5f;
                    damage = Mathf.RoundToInt(damage * critMult);
                    Debug.Log("Boss Critical Hit!");
                }

                if (targetStats != null)
                {
                    targetStats.TakeDamage(damage);
                    if (isCrit)
                    {
                        CreateDamagePopup(target.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
                    }
                }
                else
                {
                    target.GetComponent<SlimeStats>().HP -= damage;
                }

                Debug.Log($"{currentSlime.name} attacks {target.name} for {damage} damage!");

                if (targetAnimController != null)
                {
                    yield return StartCoroutine(targetAnimController.PlayHitAnimation());
                }
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

    private bool CheckWinCondition()
    {
        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();
        if (isTowerMode && activeTowerLevel >= 1 && activeTowerLevel <= 5)
        {
            bool isLastWave = (currentWaveIndex + 1 >= totalWaves);
            bool allEnemiesDead = activeEnemies.All(e => e == null || e.GetComponent<SlimeBattleStats>().CurrentHP <= 0);
            return isLastWave && allEnemiesDead;
        }

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

        bool hasAnyActiveSlime = false;
        foreach (var slime in formationManager.slimeFormation)
        {
            if (slime == null) continue;
            if (slime == boss) continue; // Bỏ qua boss kẻ địch

            var slimeStats = slime.GetComponent<SlimeStats>();
            if (slimeStats != null && slimeStats.isEnemy) continue; // Bỏ qua kẻ địch

            // Bỏ qua các slime nằm trên hàng chờ (chưa kéo ra sân)
            var dragHandler = slime.GetComponent<SlimeDragHandler>();
            if (dragHandler != null && !dragHandler.isUsed) continue;

            hasAnyActiveSlime = true;

            var battleStats = slime.GetComponent<SlimeBattleStats>();
            if (battleStats != null && battleStats.CurrentHP > 0)
            {
                return false; // Còn ít nhất 1 slime chiến đấu sống
            }
            else
            {
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
            string diff = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode()
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

                    // Nhận thưởng theo GDD cho các tầng 1-5
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

                        // Cộng tiền
                        if (CurrencyManager.Instance != null)
                        {
                            CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, gold);
                            CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, gem);
                        }

                        // Rớt Marshmallow
                        if (ResourceManager.Instance != null && Random.Range(0f, 1f) < marshmallowChance)
                        {
                            ResourceManager.Instance.AddResource(ResourceType.Marshmallow, 1);
                            CreateDamagePopup(Vector3.up * 1f, "+1 Marshmallow Ball (S)", Color.green);
                        }

                        // Rớt Slime
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

    // ── Popup damage & stats indicator ──────────────────────────────────
    public void CreateDamagePopup(Vector3 worldPosition, string text, Color color)
    {
        Canvas parentCanvas = FindObjectOfType<Canvas>();
        if (parentCanvas == null) return;

        GameObject popupGO = new GameObject("BattlePopupText");
        popupGO.transform.SetParent(parentCanvas.transform, false);

        Vector2 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPosition) : Vector3.zero;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPos,
            parentCanvas.worldCamera,
            out localPos
        );
        
        RectTransform rectTransform = popupGO.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = localPos + new Vector2(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(-10f, 10f)); // Random offset
        rectTransform.sizeDelta = new Vector2(300, 80);

        Text textComponent = popupGO.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 28;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = color;
        
        Outline outline = popupGO.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        StartCoroutine(AnimatePopupText(popupGO, textComponent));
    }

    private IEnumerator AnimatePopupText(GameObject go, Text textComponent)
    {
        float duration = 1.2f;
        float elapsed = 0f;
        Vector2 startPos = go.GetComponent<RectTransform>().anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, 80); // Float up
        Color startColor = textComponent.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (go == null) yield break;

            go.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            textComponent.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        if (go != null)
        {
            Destroy(go);
        }
    }
}
