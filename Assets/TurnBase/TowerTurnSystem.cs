using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Spine.Unity;
using Random = UnityEngine.Random;

public enum TowerEnemyType
{
    // Chapter 1
    GreenSlime, TinyBat, SlimeKing,
    // Chapter 2
    GoblinWarrior, GoblinArcher, GoblinShaman, GoblinChief,
    // Chapter 3
    PoisonSlime, CorruptedGoblin, DarkGoblinShaman, CorruptedGoblinChief,
    // Chapter 4
    StoneGoblin, CrystalSlime, AncientShaman, AncientGuardian,
    // Chapter 5
    StoneGolem, IronGolem, CrystalGolem, AncientColossus,
    // Chapter 6 (Elites & Boss)
    EliteStoneGolem, EliteIronGolem, EliteCrystalGolem, CorruptedGoblinElite, PoisonSlimeElite, TinyBatElite, CelestialGuardian
}

[Serializable]
public class EnemyVisualSetup
{
    [Header("Enemy ID")]
    public TowerEnemyType enemyType;

    [Header("Visuals")]
    public SkeletonDataAsset spineAsset;
    public Sprite staticSprite;
    public string defaultAnimation = "animation";
    public float scale = 0.7f;
    public Color colorTint = Color.white;
    public Vector2 positionOffset = Vector2.zero;
    public bool hideArmorAndWeapon = false;
}

public class TowerStatData
{
    public int hp, atk, matk, def, spd;
    public float crit, critDMG;
    public TowerStatData(int h, int a, int ma, int d, int s, float c, float cd)
    {
        hp = h; atk = a; matk = ma; def = d; spd = s; crit = c; critDMG = cd;
    }
}

public class TowerTurnSystem : TurnSystem
{
    [Header("Tower Wave System")]
    public int currentWaveIndex = 0;
    public int totalWaves = 1;
    public List<GameObject> activeEnemies = new List<GameObject>();
    private Vector2 originalBossPos;
    private GameObject enemyTemplate;
    private int bossTurnCounter = 0;
    private int activeTowerLevel = 1;
    public Text waveText;

    [Header("Enemy Custom Visuals (Assign Spine here)")]
    public List<EnemyVisualSetup> enemyVisualSetups = new List<EnemyVisualSetup>();
    private Dictionary<TowerEnemyType, EnemyVisualSetup> visualLookup;

    protected override void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (boss != null)
        {
            originalBossPos = boss.GetComponent<RectTransform>().anchoredPosition;
            enemyTemplate = boss;
        }

        turnList = formationManager.slimeFormation;

        visualLookup = new Dictionary<TowerEnemyType, EnemyVisualSetup>();
        foreach (var setup in enemyVisualSetups)
        {
            if (!visualLookup.ContainsKey(setup.enemyType))
                visualLookup.Add(setup.enemyType, setup);
        }

        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();

        if (isTowerMode && towerBosses != null)
        {
            var floor = towerBosses.replayFloor > 0 ? towerBosses.GetFloor(towerBosses.replayFloor) : towerBosses.GetCurrentFloor();
            int floorNum = floor != null ? floor.floorNumber : 1;

            if (floorNum >= 1)
            {
                base.Start();
                activeTowerLevel = floorNum;
                currentWaveIndex = 0;
                SpawnWave(0);
            }
            else
            {
                base.Start();
                if (boss != null) activeEnemies.Add(boss);
            }
        }
        else
        {
            base.Start();
            if (boss != null) activeEnemies.Add(boss);
        }
    }

    private void SpawnWave(int waveIndex)
    {
        TowerSlimeBosses.TowerFloor currentFloor = null;
        if (towerBosses != null)
        {
            currentFloor = towerBosses.replayFloor > 0 ? towerBosses.GetFloor(towerBosses.replayFloor) : towerBosses.GetCurrentFloor();
        }

        bool hasWaves = currentFloor != null && currentFloor.waves != null && currentFloor.waves.Count > 0;

        if (hasWaves)
        {
            if (targetIndicator != null)
            {
                targetIndicator.SetActive(false);
                targetIndicator.transform.SetParent(null, false);
            }
            if (turnList != null)
            {
                turnList.RemoveAll(x => x != null && x.GetComponent<SlimeStats>() != null && x.GetComponent<SlimeStats>().isEnemy);
                turnList.RemoveAll(x => x == null);
            }
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && enemy != enemyTemplate) Destroy(enemy);
            }
            activeEnemies.Clear();
            if (boss != null && boss != enemyTemplate)
            {
                Destroy(boss);
            }
            if (enemyTemplate != null)
            {
                enemyTemplate.SetActive(false);
            }
            boss = null;

            totalWaves = currentFloor.waves.Count;
            if (waveIndex >= totalWaves) return;

            if (waveText != null)
            {
                waveText.text = $"WAVE {waveIndex + 1}/{totalWaves}";
            }

            var waveConfig = currentFloor.waves[waveIndex];
            for (int i = 0; i < waveConfig.enemies.Count; i++)
            {
                var enemySetup = waveConfig.enemies[i];
                TowerEnemyType type = (TowerEnemyType)((int)enemySetup.enemyType);
                SpawnEnemy(type, enemySetup.level, i, waveConfig.enemies.Count);
            }
            if (activeEnemies.Count > 0)
            {
                boss = activeEnemies[0];
                foreach (var enemy in activeEnemies)
                {
                    if (!turnList.Contains(enemy)) turnList.Add(enemy);
                }

                var firstAliveEnemy = activeEnemies.FirstOrDefault(e => e != null && e.GetComponent<SlimeBattleStats>()?.CurrentHP > 0);
                if (firstAliveEnemy != null) SelectTarget(firstAliveEnemy);
            }
        }
        else
        {
            totalWaves = 1;
            
            var allManuallyPlacedEnemies = FindObjectsByType<SlimeStats>(FindObjectsSortMode.None)
                .Where(s => s != null && (s.isEnemy || s.gameObject == boss))
                .Select(s => s.gameObject).ToList();

            foreach (var enemy in allManuallyPlacedEnemies)
            {
                if (enemy == null) continue;
                enemy.SetActive(true);
                if (!activeEnemies.Contains(enemy)) activeEnemies.Add(enemy);
                
                var ss = enemy.GetComponent<SlimeStats>();
                if (ss != null) ss.isEnemy = true;
                MakeEnemyTargetable(enemy);

                // Set stats fallback if not set by TurnSystem yet
                var battleStats = enemy.GetComponent<SlimeBattleStats>();
                if (battleStats == null || battleStats.CurrentHP <= 0)
                {
                    if (ss != null && currentFloor != null)
                    {
                        ss.HP = currentFloor.baseHP;
                        ss.MaxHP = currentFloor.baseHP;
                        ss.Attack = currentFloor.baseAttack;
                        ss.Defense = currentFloor.baseDefense;
                        ss.Speed = currentFloor.baseSpeed;
                        ss.isEnemy = true;
                    }
                    if (battleStats != null && currentFloor != null)
                    {
                        battleStats.MaxHP = currentFloor.baseHP;
                        battleStats.CurrentHP = currentFloor.baseHP;
                        battleStats.BattleAttack = currentFloor.baseAttack;
                        battleStats.BattleDefense = currentFloor.baseDefense;
                        battleStats.BattleSpeed = currentFloor.baseSpeed;
                    }
                }
            }
            if (activeEnemies.Count > 0 && boss == null) boss = activeEnemies[0];
        }

        foreach (var enemy in activeEnemies)
        {
            float enemySpd = GetSpeedOf(enemy);
            remainingAV[enemy] = 10000f / enemySpd;
        }
    }

    private void SpawnEnemy(TowerEnemyType type, int level, int index, int totalCount)
    {
        GameObject templateObj = enemyTemplate != null ? enemyTemplate : boss;
        if (templateObj == null) templateObj = activeEnemies.FirstOrDefault();
        if (templateObj == null)
        {
            Debug.LogError("[TowerTurnSystem] Enemy template missing!");
            return;
        }

        GameObject enemyGo = Instantiate(templateObj, templateObj.transform.parent);
        enemyGo.name = $"{type} Lv{level}";
        enemyGo.SetActive(true);

        var spineGraphic = enemyGo.GetComponentInChildren<SkeletonGraphic>(true);
        if (spineGraphic != null)
        {
            spineGraphic.color = Color.white;
        }

        RectTransform rect = enemyGo.GetComponent<RectTransform>();
        Vector2 offset = Vector2.zero;

        Vector2 baseShift = new Vector2(0, -90f);

        if (totalCount == 1)
        {
            offset = Vector2.zero;
        }
        else if (totalCount == 2)
        {
            offset = index == 0 ? new Vector2(0, 110f) : new Vector2(0, -110f);
        }
        else
        {
            if (index == 0) offset = new Vector2(40f, 170f);
            else if (index == 1) offset = new Vector2(0f, 0f);
            else offset = new Vector2(40f, -170f);
        }

        Vector2 originalPos = templateObj.GetComponent<RectTransform>().anchoredPosition;

        var spine = enemyGo.GetComponentInChildren<SkeletonGraphic>(true);
        if (spine != null) spine.color = Color.white;
        var staticImg = enemyGo.transform.Find("StaticSprite")?.GetComponent<UnityEngine.UI.Image>();
        if (staticImg != null) staticImg.color = Color.white;

        GameObject imgGo = null;
        float actualScale = 0.7f;

        if (visualLookup.TryGetValue(type, out var visualSetup))
        {
            actualScale = visualSetup.scale;
            if (visualSetup.spineAsset != null && spine != null)
            {
                spine.gameObject.SetActive(true);
                spine.skeletonDataAsset = visualSetup.spineAsset;
                spine.Initialize(true);
                spine.AnimationState.SetAnimation(0, visualSetup.defaultAnimation, true);
                spine.transform.localScale = Vector3.one * visualSetup.scale;
            }
            else if (visualSetup.staticSprite != null)
            {
                if (spine != null) spine.gameObject.SetActive(false);
                imgGo = new GameObject("StaticSprite");
                imgGo.transform.SetParent(enemyGo.transform, false);
                var img = imgGo.AddComponent<Image>();
                img.sprite = visualSetup.staticSprite;
                img.color = visualSetup.colorTint;
                img.SetNativeSize();
                imgGo.transform.localScale = Vector3.one * visualSetup.scale;
            }
            else if (spine != null)
            {
                spine.gameObject.SetActive(true);
                spine.color = visualSetup.colorTint;
                spine.transform.localScale = Vector3.one * visualSetup.scale;
            }

            enemyGo.transform.localScale = Vector3.one;
            offset += visualSetup.positionOffset;

            if (visualSetup.hideArmorAndWeapon)
            {
                var sStats = enemyGo.GetComponent<SlimeStats>();
                if (sStats != null)
                {
                    if (sStats.armor != null) sStats.armor.gameObject.SetActive(false);
                    if (sStats.weapon != null) sStats.weapon.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            enemyGo.transform.localScale = Vector3.one;
            if (spine != null) spine.transform.localScale = Vector3.one * 0.7f;
        }

        rect.anchoredPosition = originalBossPos + baseShift + offset;

        TowerStatData eData = GetEnemyStatDatabase(type, level);

        var stats = enemyGo.GetComponent<SlimeStats>();
        if (stats == null) stats = enemyGo.AddComponent<SlimeStats>();

        stats.HP = eData.hp;
        stats.MaxHP = eData.hp;
        stats.Attack = eData.atk;
        stats.MagicAttack = eData.matk;
        stats.Defense = eData.def;
        stats.Speed = eData.spd;
        stats.CritRate = eData.crit;
        stats.CritDMG = eData.critDMG;
        stats.isEnemy = true;
        stats.useRarityBossScaling = false;

        var bStats = enemyGo.GetComponent<SlimeBattleStats>();
        if (bStats == null) bStats = enemyGo.AddComponent<SlimeBattleStats>();
        bStats.ReinitializeFromBaseStats();


        if (stats.hpbar != null)
        {
            stats.hpbar.interactable = false;
            stats.hpbar.transform.localScale = Vector3.one * (actualScale * 0.7f);
            stats.hpbar.maxValue = eData.hp;
            stats.hpbar.value = eData.hp;

            var hpRect = stats.hpbar.GetComponent<RectTransform>();
            if (hpRect != null)
            {
                hpRect.anchoredPosition = new Vector2(hpRect.anchoredPosition.x, -120f * actualScale);
            }
        }

        var battleStats = enemyGo.GetComponent<SlimeBattleStats>();
        if (battleStats == null) battleStats = enemyGo.AddComponent<SlimeBattleStats>();

        battleStats.baseStats = stats;
        battleStats.MaxHP = eData.hp;
        battleStats.CurrentHP = eData.hp;
        battleStats.BattleAttack = eData.atk;
        battleStats.BattleMagicAttack = eData.matk;
        battleStats.BattleDefense = eData.def;
        battleStats.BattleSpeed = eData.spd;
        battleStats.BattleCritRate = eData.crit;
        battleStats.BattleCritDMG = eData.critDMG;

        battleStats.isInitialized = true;

        if (type == TowerEnemyType.SlimeKing || type == TowerEnemyType.GoblinChief)
        {
            stats.bodySkill = new SkillInstance(null);
        }

        activeEnemies.Add(enemyGo);
        MakeEnemyTargetable(enemyGo);
    }

    private TowerStatData GetEnemyStatDatabase(TowerEnemyType type, int level)
    {
        switch (type)
        {
            // ==========================================
            // CHAPTER 1 (Common Tier — F1-5)
            // ==========================================
            case TowerEnemyType.GreenSlime:
                if (level == 1) return new TowerStatData(1600, 200, 180, 220, 85, 0.08f, 1.30f);
                if (level == 2) return new TowerStatData(1750, 225, 200, 240, 87, 0.09f, 1.30f);
                if (level == 3) return new TowerStatData(1900, 250, 220, 260, 89, 0.10f, 1.30f);
                if (level == 4) return new TowerStatData(2050, 275, 240, 280, 91, 0.11f, 1.30f);
                return new TowerStatData(2200, 300, 260, 300, 93, 0.12f, 1.30f);

            case TowerEnemyType.TinyBat:
                if (level == 3) return new TowerStatData(1400, 280, 150, 180, 96, 0.12f, 1.35f);
                if (level == 4) return new TowerStatData(1550, 310, 170, 200, 98, 0.13f, 1.35f);
                return new TowerStatData(1700, 340, 190, 220, 100, 0.14f, 1.35f);

            case TowerEnemyType.SlimeKing:
                return new TowerStatData(10000, 360, 480, 420, 95, 0.18f, 1.40f);

            // ==========================================
            // CHAPTER 2 (Uncommon Tier — F6-10)
            // ==========================================
            case TowerEnemyType.GoblinWarrior:
                if (level == 6) return new TowerStatData(2800, 360, 0, 450, 94, 0.15f, 1.35f);
                if (level == 7) return new TowerStatData(3050, 390, 0, 490, 96, 0.16f, 1.35f);
                if (level == 8) return new TowerStatData(3300, 420, 0, 530, 98, 0.17f, 1.35f);
                if (level == 9) return new TowerStatData(3550, 450, 0, 570, 100, 0.18f, 1.35f);
                return new TowerStatData(3800, 480, 0, 610, 102, 0.19f, 1.35f);

            case TowerEnemyType.GoblinArcher:
                if (level == 7) return new TowerStatData(2400, 420, 0, 350, 106, 0.20f, 1.40f);
                if (level == 8) return new TowerStatData(2600, 455, 0, 380, 108, 0.21f, 1.40f);
                if (level == 9) return new TowerStatData(2800, 490, 0, 410, 110, 0.22f, 1.40f);
                return new TowerStatData(3000, 525, 0, 440, 112, 0.23f, 1.40f);

            case TowerEnemyType.GoblinShaman:
                if (level == 8) return new TowerStatData(2600, 180, 460, 400, 100, 0.18f, 1.35f);
                if (level == 9) return new TowerStatData(2850, 195, 500, 430, 102, 0.19f, 1.35f);
                return new TowerStatData(3100, 210, 540, 460, 104, 0.20f, 1.35f);

            case TowerEnemyType.GoblinChief:
                return new TowerStatData(22000, 550, 720, 750, 104, 0.22f, 1.50f);

            // ==========================================
            // CHAPTER 3 (Rare Tier — F11-15)
            // ==========================================
            case TowerEnemyType.PoisonSlime:
                if (level == 11) return new TowerStatData(5000, 420, 580, 680, 100, 0.22f, 1.45f);
                if (level == 12) return new TowerStatData(5400, 450, 620, 730, 102, 0.23f, 1.45f);
                if (level == 13) return new TowerStatData(5800, 480, 660, 780, 104, 0.24f, 1.45f);
                if (level == 14) return new TowerStatData(6200, 510, 700, 830, 106, 0.25f, 1.50f);
                return new TowerStatData(6600, 540, 740, 880, 108, 0.26f, 1.50f);

            case TowerEnemyType.CorruptedGoblin:
                if (level == 11) return new TowerStatData(5200, 560, 0, 720, 102, 0.22f, 1.45f);
                if (level == 12) return new TowerStatData(5650, 600, 0, 770, 104, 0.23f, 1.45f);
                if (level == 13) return new TowerStatData(6100, 640, 0, 820, 106, 0.24f, 1.50f);
                if (level == 14) return new TowerStatData(6550, 680, 0, 870, 108, 0.25f, 1.50f);
                return new TowerStatData(7000, 720, 0, 920, 110, 0.26f, 1.55f);

            case TowerEnemyType.DarkGoblinShaman:
                if (level == 12) return new TowerStatData(4600, 220, 650, 600, 105, 0.22f, 1.45f);
                if (level == 13) return new TowerStatData(5000, 240, 700, 640, 107, 0.23f, 1.45f);
                if (level == 14) return new TowerStatData(5400, 260, 750, 680, 109, 0.24f, 1.50f);
                return new TowerStatData(5800, 280, 800, 720, 111, 0.25f, 1.50f);

            case TowerEnemyType.CorruptedGoblinChief:
                return new TowerStatData(48000, 850, 1150, 1200, 110, 0.28f, 1.60f);

            // ==========================================
            // CHAPTER 4 (SuperRare Tier — F16-20)
            // ==========================================
            case TowerEnemyType.StoneGoblin:
                if (level == 16) return new TowerStatData(8500, 820, 0, 1100, 108, 0.28f, 1.55f);
                if (level == 17) return new TowerStatData(9300, 875, 0, 1180, 110, 0.29f, 1.55f);
                if (level == 18) return new TowerStatData(10100, 930, 0, 1260, 112, 0.30f, 1.60f);
                if (level == 19) return new TowerStatData(10900, 985, 0, 1340, 114, 0.31f, 1.60f);
                return new TowerStatData(11800, 1050, 0, 1420, 116, 0.32f, 1.65f);

            case TowerEnemyType.CrystalSlime:
                if (level == 16) return new TowerStatData(8000, 580, 860, 1050, 108, 0.28f, 1.55f);
                if (level == 17) return new TowerStatData(8750, 620, 920, 1120, 110, 0.29f, 1.55f);
                if (level == 18) return new TowerStatData(9500, 660, 980, 1190, 112, 0.30f, 1.60f);
                if (level == 19) return new TowerStatData(10250, 700, 1040, 1260, 114, 0.31f, 1.60f);
                return new TowerStatData(11000, 740, 1100, 1330, 116, 0.32f, 1.65f);

            case TowerEnemyType.AncientShaman:
                if (level == 17) return new TowerStatData(7200, 320, 980, 950, 112, 0.28f, 1.55f);
                if (level == 18) return new TowerStatData(7800, 345, 1050, 1010, 114, 0.29f, 1.60f);
                if (level == 19) return new TowerStatData(8400, 370, 1120, 1070, 116, 0.30f, 1.60f);
                return new TowerStatData(9000, 395, 1200, 1130, 118, 0.32f, 1.65f);

            case TowerEnemyType.AncientGuardian:
                return new TowerStatData(95000, 1300, 1750, 2200, 118, 0.35f, 1.70f);

            // ==========================================
            // CHAPTER 5 (UltraRare Tier — F21-25)
            // ==========================================
            case TowerEnemyType.StoneGolem:
                if (level == 21) return new TowerStatData(15000, 1250, 0, 1800, 116, 0.32f, 1.65f);
                if (level == 22) return new TowerStatData(16500, 1330, 0, 1930, 118, 0.33f, 1.65f);
                if (level == 23) return new TowerStatData(18000, 1410, 0, 2060, 120, 0.34f, 1.70f);
                if (level == 24) return new TowerStatData(19500, 1490, 0, 2190, 122, 0.35f, 1.70f);
                return new TowerStatData(21000, 1580, 0, 2320, 124, 0.36f, 1.75f);

            case TowerEnemyType.IronGolem:
                if (level == 21) return new TowerStatData(18500, 1050, 0, 2400, 112, 0.28f, 1.60f);
                if (level == 22) return new TowerStatData(20200, 1120, 0, 2570, 114, 0.29f, 1.60f);
                if (level == 23) return new TowerStatData(21900, 1190, 0, 2740, 116, 0.30f, 1.65f);
                if (level == 24) return new TowerStatData(23600, 1260, 0, 2910, 118, 0.31f, 1.65f);
                return new TowerStatData(25500, 1340, 0, 3100, 120, 0.32f, 1.70f);

            case TowerEnemyType.CrystalGolem:
                if (level == 21) return new TowerStatData(14000, 650, 1450, 1650, 118, 0.32f, 1.70f);
                if (level == 22) return new TowerStatData(15300, 690, 1540, 1770, 120, 0.33f, 1.70f);
                if (level == 23) return new TowerStatData(16600, 730, 1630, 1890, 122, 0.34f, 1.75f);
                if (level == 24) return new TowerStatData(17900, 770, 1720, 2010, 124, 0.35f, 1.75f);
                return new TowerStatData(19500, 820, 1830, 2150, 126, 0.36f, 1.80f);

            case TowerEnemyType.AncientColossus:
                return new TowerStatData(170000, 1850, 2500, 3600, 126, 0.38f, 1.85f);

            // ==========================================
            // CHAPTER 6 (Legendary & Mythic Tier — F26-30)
            // ==========================================
            case TowerEnemyType.EliteStoneGolem:
                if (level == 26) return new TowerStatData(26000, 2050, 0, 3400, 136, 0.38f, 1.85f);
                if (level == 27) return new TowerStatData(29500, 2200, 0, 3700, 139, 0.39f, 1.85f);
                if (level == 28) return new TowerStatData(33000, 2350, 0, 4000, 142, 0.40f, 1.90f);
                if (level == 29) return new TowerStatData(36500, 2500, 0, 4300, 145, 0.41f, 1.90f);
                return new TowerStatData(40000, 2680, 0, 4650, 148, 0.42f, 1.95f);

            case TowerEnemyType.EliteIronGolem:
                if (level == 26) return new TowerStatData(32000, 1750, 0, 4500, 132, 0.35f, 1.80f);
                if (level == 27) return new TowerStatData(36500, 1880, 0, 4900, 135, 0.36f, 1.80f);
                if (level == 28) return new TowerStatData(41000, 2010, 0, 5300, 138, 0.37f, 1.85f);
                if (level == 29) return new TowerStatData(45500, 2140, 0, 5700, 141, 0.38f, 1.85f);
                return new TowerStatData(50000, 2280, 0, 6150, 144, 0.39f, 1.90f);

            case TowerEnemyType.EliteCrystalGolem:
                if (level == 26) return new TowerStatData(25000, 1100, 2300, 3200, 138, 0.38f, 1.90f);
                if (level == 27) return new TowerStatData(28500, 1180, 2480, 3500, 141, 0.39f, 1.90f);
                if (level == 28) return new TowerStatData(32000, 1260, 2660, 3800, 144, 0.40f, 1.95f);
                if (level == 29) return new TowerStatData(35500, 1340, 2840, 4100, 147, 0.41f, 1.95f);
                return new TowerStatData(39000, 1430, 3050, 4450, 150, 0.42f, 2.00f);

            case TowerEnemyType.CorruptedGoblinElite:
                if (level == 26) return new TowerStatData(22000, 2150, 0, 2800, 142, 0.38f, 1.85f);
                if (level == 27) return new TowerStatData(25000, 2300, 0, 3050, 145, 0.39f, 1.85f);
                if (level == 28) return new TowerStatData(28000, 2450, 0, 3300, 148, 0.40f, 1.90f);
                if (level == 29) return new TowerStatData(31000, 2600, 0, 3550, 151, 0.41f, 1.90f);
                return new TowerStatData(34500, 2780, 0, 3850, 154, 0.42f, 1.95f);

            case TowerEnemyType.PoisonSlimeElite:
                if (level == 26) return new TowerStatData(21000, 1300, 1950, 2700, 140, 0.38f, 1.85f);
                if (level == 27) return new TowerStatData(24000, 1400, 2100, 2950, 143, 0.39f, 1.85f);
                if (level == 28) return new TowerStatData(27000, 1500, 2250, 3200, 146, 0.40f, 1.90f);
                if (level == 29) return new TowerStatData(30000, 1600, 2400, 3450, 149, 0.41f, 1.90f);
                return new TowerStatData(33500, 1720, 2580, 3750, 152, 0.42f, 1.95f);

            case TowerEnemyType.TinyBatElite:
                if (level == 26) return new TowerStatData(18000, 1750, 0, 2200, 146, 0.38f, 1.85f);
                if (level == 27) return new TowerStatData(20500, 1880, 0, 2400, 149, 0.39f, 1.85f);
                if (level == 28) return new TowerStatData(23000, 2010, 0, 2600, 152, 0.40f, 1.90f);
                if (level == 29) return new TowerStatData(25500, 2140, 0, 2800, 155, 0.41f, 1.90f);
                return new TowerStatData(28500, 2280, 0, 3050, 158, 0.42f, 1.95f);

            case TowerEnemyType.CelestialGuardian:
                return new TowerStatData(350000, 2900, 4000, 6500, 155, 0.45f, 2.10f);
        }

        return new TowerStatData(1600, 200, 180, 220, 85, 0.08f, 1.30f);
    }

    private void CheckWinLoseAfterEnemyDeath()
    {
        var nextAlive = activeEnemies.FirstOrDefault(e => e != null && e.GetComponent<SlimeBattleStats>()?.CurrentHP > 0);
        if (nextAlive != null)
        {
            SelectTarget(nextAlive);
        }

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            var stats = enemy.GetComponent<SlimeBattleStats>();
            if (stats != null && stats.CurrentHP <= 0)
            {
                Color darkColor = new Color(0.3f, 0.3f, 0.3f, 1f);

                var spine = enemy.GetComponentInChildren<SkeletonGraphic>(true);
                if (spine != null)
                {
                    spine.color = darkColor;
                }

                var img = enemy.transform.Find("StaticSprite")?.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.color = darkColor;
                }

                var slimeStats = enemy.GetComponent<SlimeStats>();
                if (slimeStats != null && slimeStats.turnHalo != null)
                {
                    slimeStats.turnHalo.SetActive(false);
                }
            }
        }
    }


    protected override IEnumerator NextTurn()
    {
        if (currentSlime != null) currentSlime.GetComponent<SlimeStats>().turnHalo.SetActive(false);
        yield return new WaitForSeconds(0.3f);

        if (activeTowerLevel >= 1)
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

        var activeParticipants = remainingAV.Keys.Where(s => s != null && s.activeInHierarchy && s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0).ToList();

        if (activeParticipants.Count == 0)
        {
            InitializeAVSystem();
            activeParticipants = remainingAV.Keys.Where(s => s != null && s.activeInHierarchy && s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0).ToList();
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
            foreach (var slime in activeParticipants) remainingAV[slime] -= minAV;
            float speed = GetSpeedOf(nextSlime);
            remainingAV[nextSlime] = 10000f / speed;
            currentSlime = nextSlime;
        }
        StartCoroutine(turnDisplay());

        if (BattleSystemManager.Instance != null)
        {
            BattleSystemManager.Instance.OnNewTurnStarted();
        }

        var battleStats = currentSlime.GetComponent<SlimeBattleStats>();
        if (battleStats != null)
        {
            battleStats.TickDoTs();
            if (battleStats.CurrentHP <= 0)
            {
                CheckWinLoseAfterEnemyDeath();
                if (CheckWinCondition()) { yield return StartCoroutine(HandleVictory()); yield break; }
                yield return new WaitForSeconds(0.8f);
                StartCoroutine(NextTurn());
                yield break;
            }
            battleStats.TickBuffs();
            battleStats.TickStun();
        }

        if (battleStats != null && battleStats.IsStunned)
        {
            yield return new WaitForSeconds(0.8f);
            StartCoroutine(NextTurn());
            yield break;
        }

        if (currentSlime.GetComponent<SlimeStats>().isEnemy)
            yield return StartCoroutine(BossTurn());
        else
        {
            PlayerTurn();
        }
    }

    protected override IEnumerator AutoAttack()
    {
        if (boss == null)
        {
            Debug.LogWarning("[TowerTurnSystem] AutoAttack: boss is null, skipping.");
            yield break;
        }
        var target = boss.GetComponent<SlimeBattleStats>();
        var attacker = currentSlime.GetComponent<SlimeBattleStats>();

        if (attacker != null)
        {
            attacker.AddEnergy(20);

            // Cộng +1 Điểm Chiến Kỹ (SP) khi đánh thường
            if (BattleSystemManager.Instance != null)
            {
                BattleSystemManager.Instance.AddBattlePoints(1);
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 2f, "+1 SP", Color.cyan);
            }

            int damage = attacker.GetEffectiveAttack();
            bool isCrit = attacker.TryCriticalHit();
            if (isCrit)
            {
                float critMult = attacker.GetFinalCritDMG();
                damage = Mathf.RoundToInt(damage * critMult);
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
            if (attackerAnim != null && attackerAnim.gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(attackerAnim.PlayAttackAnimation(boss.transform));
            }

            var targetAnimController = boss.GetComponent<SimpleCombatAnimation>();
            if (targetAnimController != null && targetAnimController.gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(targetAnimController.PlayHitAnimation());
            }

            if (target.CurrentHP <= 0)
            {
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

        if (skill.baseSkill != null && !string.IsNullOrEmpty(skill.baseSkill.skillName))
        {
            Color popupColor = skill.baseSkill.type == SkillType.Ultimate ? Color.yellow : Color.cyan;
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 2.2f, skill.baseSkill.skillName, popupColor);
        }

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
                        float magicWeight = (skill.baseSkill != null && skill.baseSkill.type == SkillType.Ultimate) ? 0.9f : 0.8f;
                        float baseSkillPower = magicWeight * attacker.GetEffectiveMagicAttack() + (1f - magicWeight) * attacker.GetEffectiveAttack();
                        float rawDamage = baseSkillPower * entry.value + entry.flatBonus;

                        int finalDamage = Mathf.RoundToInt(rawDamage);
                        bool isCrit = attacker.TryCriticalHit();
                        if (isCrit)
                        {
                            float critMult = attacker.GetFinalCritDMG();
                            finalDamage = Mathf.RoundToInt(finalDamage * critMult);
                        }
                        targetStats.TakeDamage(finalDamage);
                        if (isCrit)
                        {
                            CreateDamagePopup(targetGo.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
                        }
                        var hitAnim = targetGo.GetComponent<SimpleCombatAnimation>();
                        if (hitAnim != null && hitAnim.gameObject.activeInHierarchy)
                        {
                            yield return StartCoroutine(hitAnim.PlayHitAnimation());
                        }
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

        var spineGraphic = currentSlime.GetComponentInChildren<SkeletonGraphic>(true);
        if (spineGraphic != null && spineGraphic.skeletonDataAsset != null && curSlimeBody != null)
        {
            curSlimeBody.skeletonDataAsset = spineGraphic.skeletonDataAsset;
            curSlimeBody.allowMultipleCanvasRenderers = true;
            curSlimeBody.enableSeparatorSlots = true;
            curSlimeBody.Initialize(true);
            curSlimeBody.AnimationState.SetAnimation(0, "animation", true);
            curSlimeBody.timeScale = 2;
            curSlimeBody.gameObject.SetActive(true);
        }
        else if (curSlimeBody != null)
        {
            curSlimeBody.gameObject.SetActive(false);
        }

        var enemyStats = currentSlime.GetComponent<SlimeStats>();
        if (enemyStats != null)
        {
            if (curSlimeHat != null) curSlimeHat.sprite = enemyStats.armor?.sprite;
            if (curSlimeWeapon != null) curSlimeWeapon.sprite = enemyStats.weapon?.sprite;
        }
        if (curSlimeBorder != null) curSlimeBorder.color = Color.red;

        if (enemyStats != null && enemyStats.turnHalo != null) enemyStats.turnHalo.SetActive(true);

        var aiState = currentSlime.GetComponent<EnemyAIState>();
        if (aiState == null) aiState = currentSlime.AddComponent<EnemyAIState>();
        aiState.currentTurnCycle++;

        TowerEnemyType currentType = GetEnemyTypeFromName(currentSlime.name);

        GameObject target = GetAIQueryTarget(currentType);

        if (target != null)
        {
            yield return StartCoroutine(ExecuteEnemyAIBehavior(currentType, aiState, target));
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

    private TowerEnemyType GetEnemyTypeFromName(string objName)
    {
        foreach (TowerEnemyType type in Enum.GetValues(typeof(TowerEnemyType)))
        {
            if (objName.Contains(type.ToString())) return type;
        }
        return TowerEnemyType.GreenSlime;
    }

    // ==========================================
    // ==========================================
    private GameObject GetAIQueryTarget(TowerEnemyType type)
    {
        var playerAllies = formationManager.GetAllAliveAllies()
            .Where(a => a != null && a.GetComponent<SlimeBattleStats>()?.CurrentHP > 0).ToList();

        if (playerAllies.Count == 0) return null;

        switch (type)
        {
            case TowerEnemyType.GoblinArcher:
                return playerAllies.OrderBy(a => a.GetComponent<SlimeBattleStats>().CurrentHP).First();

            case TowerEnemyType.CorruptedGoblin:
            case TowerEnemyType.CorruptedGoblinElite:
                return playerAllies.OrderBy(a => a.GetComponent<SlimeBattleStats>().BattleDefense).First();

            case TowerEnemyType.StoneGoblin:
            case TowerEnemyType.StoneGolem:
            case TowerEnemyType.EliteStoneGolem:
                return playerAllies.OrderByDescending(a => a.GetComponent<SlimeBattleStats>().BattleSpeed).First();

            default:
                return formationManager.GetRandomRowLastAlive() ?? playerAllies.First();
        }
    }

    // ==========================================
    // ==========================================
    private void UpdateEnemyAuras()
    {
        if (activeEnemies == null || activeEnemies.Count == 0) return;

        bool hasGoblinShaman = false, hasDarkGoblinShaman = false, hasAncientShaman = false;
        bool hasIronGolem = false, hasStoneGoblin = false, hasCrystalSlime = false;
        bool hasStoneGolem = false, hasCrystalGolem = false;
        int crystalGolemCount = 0;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            var e = activeEnemies[i];
            if (e == null) continue;
            var bs = e.GetComponent<SlimeBattleStats>();
            if (bs == null || bs.CurrentHP <= 0) continue;

            string n = e.name;
            if (!hasGoblinShaman && n.Contains("GoblinShaman"))      hasGoblinShaman = true;
            if (!hasDarkGoblinShaman && n.Contains("DarkGoblinShaman")) hasDarkGoblinShaman = true;
            if (!hasAncientShaman && n.Contains("AncientShaman"))    hasAncientShaman = true;
            if (!hasIronGolem && (n.Contains("IronGolem") || n.Contains("EliteIronGolem"))) hasIronGolem = true;
            if (!hasStoneGoblin && n.Contains("StoneGoblin"))        hasStoneGoblin = true;
            if (!hasCrystalSlime && n.Contains("CrystalSlime"))      hasCrystalSlime = true;
            if (!hasStoneGolem && (n.Contains("StoneGolem") || n.Contains("EliteStoneGolem"))) hasStoneGolem = true;
            if (n.Contains("CrystalGolem") || n.Contains("EliteCrystalGolem"))
            {
                hasCrystalGolem = true;
                crystalGolemCount++;
            }
        }
        bool hasAll3Golems = hasStoneGolem && hasIronGolem && hasCrystalGolem;

        var playerAllies = formationManager.GetAllAliveAllies();
        int poisonedPlayerCount = 0;
        for (int i = 0; i < playerAllies.Count; i++)
        {
            var a = playerAllies[i];
            if (a == null) continue;
            var aStats = a.GetComponent<SlimeBattleStats>();
            if (aStats != null && aStats.GetPoisonStackCount() > 0) poisonedPlayerCount++;
        }

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            var enemy = activeEnemies[i];
            if (enemy == null) continue;
            var stats = enemy.GetComponent<SlimeBattleStats>();
            if (stats == null || stats.baseStats == null) continue;
            var bsHP = stats.CurrentHP;
            if (bsHP <= 0) continue;

            string n = enemy.name;
            float defMult = 1.0f, matkMult = 1.0f, atkMult = 1.0f;

            if (hasGoblinShaman && n.Contains("GoblinWarrior")) atkMult *= 1.15f;
            if (hasDarkGoblinShaman) { matkMult *= 1.20f; stats.critChance = 15f; }
            if (hasAncientShaman) defMult *= 1.20f;
            if (hasIronGolem && n.Contains("Golem")) stats.damageReduction = 15f;
            if (n.Contains("CrystalSlime") && hasStoneGoblin) defMult *= 1.15f;
            if (n.Contains("StoneGoblin") && hasStoneGoblin && hasCrystalSlime) defMult *= 1.10f;
            if (hasAll3Golems && n.Contains("Golem")) defMult *= 1.10f;
            if (crystalGolemCount >= 2) defMult *= 1.25f;
            if (n.Contains("PoisonSlime") && poisonedPlayerCount >= 2) matkMult *= 1.20f;

            stats.BattleDefense = Mathf.RoundToInt(stats.initialBattleDefense * defMult);
            stats.BattleMagicAttack = Mathf.RoundToInt(stats.initialBattleMagicAttack * matkMult);
            stats.BattleAttack = Mathf.RoundToInt(stats.initialBattleAttack * atkMult);
        }
    }


    // ==========================================
    // ==========================================
    private IEnumerator ExecuteEnemyAIBehavior(TowerEnemyType type, EnemyAIState ai, GameObject target)
    {
        var bossStats = currentSlime.GetComponent<SlimeBattleStats>();
        if (bossStats == null) yield break;

        UpdateEnemyAuras();

        float hpPercent = (float)bossStats.CurrentHP / bossStats.MaxHP;

        if (type == TowerEnemyType.CelestialGuardian)
        {
            bossStats.damageReduction = 25f;

            foreach (var pAlly in formationManager.GetAllAliveAllies())
            {
                var pStats = pAlly?.GetComponent<SlimeBattleStats>();
                if (pStats != null && pStats.baseStats != null)
                {
                    pStats.BattleSpeed = Mathf.Max(1, Mathf.RoundToInt(pStats.baseStats.Speed * 0.90f));
                }
            }

            // Phase 2 Transition (70% -> 40% HP)
            if (hpPercent <= 0.70f && ai.currentPhase < 2)
            {
                ai.currentPhase = 2;
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 2.2f, "PHASE 2: CELESTIAL AWAKENING!", Color.cyan);
                bossStats.CleanseDebuffs();
                bossStats.ApplyBuff(BuffStat.Attack, 1.20f, -1); // +20% ATK
                bossStats.ApplyBuff(BuffStat.Speed, 1.20f, -1);  // +20% SPD
                int shieldAmount = Mathf.RoundToInt(bossStats.MaxHP * 0.50f);
                bossStats.AddShield(shieldAmount); // Shield Rebuild: 50% Max HP
            }
            // Phase 3 Transition (40% -> 0% HP)
            else if (hpPercent <= 0.40f && ai.currentPhase < 3)
            {
                ai.currentPhase = 3;
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 2.2f, "PHASE 3: BERSERK MODE!", Color.red);
                bossStats.CleanseDebuffs();
                bossStats.ApplyBuff(BuffStat.Attack, 1.30f, -1); // +30% ATK
                bossStats.critChance += 20f;                      // +20% Crit Rate
            }
        }

        switch (type)
        {
            case TowerEnemyType.TinyBat:
            case TowerEnemyType.TinyBatElite:
                yield return StartCoroutine(AI_TinyBat(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.GoblinArcher:
                yield return StartCoroutine(AI_GoblinArcher(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.GoblinShaman:
                yield return StartCoroutine(AI_GoblinShaman(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.DarkGoblinShaman:
                yield return StartCoroutine(AI_DarkGoblinShaman(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.PoisonSlime:
            case TowerEnemyType.PoisonSlimeElite:
                yield return StartCoroutine(AI_PoisonSlime(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.CorruptedGoblin:
            case TowerEnemyType.CorruptedGoblinElite:
                yield return StartCoroutine(AI_CorruptedGoblin(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.StoneGoblin:
                yield return StartCoroutine(AI_StoneGoblin(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.CrystalSlime:
                yield return StartCoroutine(AI_CrystalSlime(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.AncientShaman:
                yield return StartCoroutine(AI_AncientShaman(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.IronGolem:
            case TowerEnemyType.EliteIronGolem:
                yield return StartCoroutine(AI_IronGolem(type, ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.StoneGolem:
            case TowerEnemyType.EliteStoneGolem:
                yield return StartCoroutine(AI_StoneGolem(type, ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.CrystalGolem:
            case TowerEnemyType.EliteCrystalGolem:
                yield return StartCoroutine(AI_CrystalGolem(type, ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.SlimeKing:
                yield return StartCoroutine(AI_SlimeKing(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.GoblinChief:
            case TowerEnemyType.CorruptedGoblinChief:
                yield return StartCoroutine(AI_GoblinChief(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.CelestialGuardian:
                yield return StartCoroutine(AI_CelestialGuardian(ai.currentTurnCycle, ai.currentPhase, bossStats, target));
                break;

            default:
                yield return StartCoroutine(DefaultEnemyAttack(bossStats, target));
                break;
        }
    }

    // ── AI Tiny Bat ──
    private IEnumerator AI_TinyBat(int turn, SlimeBattleStats stats, GameObject target)
    {
        CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Sonic Bite!", Color.cyan);
        yield return StartCoroutine(DefaultEnemyAttack(stats, target));
    }

    // ── AI Goblin Archer ──
    private IEnumerator AI_GoblinArcher(int turn, SlimeBattleStats stats, GameObject target)
    {
        var targetStats = target.GetComponent<SlimeBattleStats>();
        float targetHpPct = targetStats != null ? (float)targetStats.CurrentHP / targetStats.MaxHP : 1f;

        if (targetHpPct < 0.40f || turn % 2 == 0) // Rapid Shot / Weak Point
        {
            float bonus = targetHpPct < 0.40f ? 1.3f : 1.0f; // Weak Point +30% Dmg
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, targetHpPct < 0.40f ? "WEAK POINT RAPID SHOT!" : "Rapid Shot!", Color.red);
            int hitDmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 0.8f * bonus);

            var animController = currentSlime.GetComponent<SimpleCombatAnimation>();
            if (animController != null) yield return StartCoroutine(animController.PlayAttackAnimation(target.transform));

            if (targetStats != null) targetStats.TakeDamage(hitDmg, currentSlime);
            yield return new WaitForSeconds(0.2f);
            if (targetStats != null) targetStats.TakeDamage(hitDmg, currentSlime);
        }
        else
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Arrow Shot!", Color.white);
            yield return StartCoroutine(DefaultEnemyAttack(stats, target));
        }
    }

    // ── AI Goblin Shaman ──
    private IEnumerator AI_GoblinShaman(int turn, SlimeBattleStats stats, GameObject target)
    {
        if (turn == 1) // War Cry
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "War Cry! (+15% ATK)", Color.yellow);
            foreach (var e in activeEnemies)
            {
                if (e != null && e.name.Contains("Goblin"))
                    e.GetComponent<SlimeBattleStats>()?.ApplyBuff(BuffStat.Attack, 1.15f, 2);
            }
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            var lowestAlly = activeEnemies.Where(e => e != null && e.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
                .OrderBy(e => (float)e.GetComponent<SlimeBattleStats>().CurrentHP / e.GetComponent<SlimeBattleStats>().MaxHP).FirstOrDefault();
            var lowestStats = lowestAlly != null ? lowestAlly.GetComponent<SlimeBattleStats>() : null;

            if (lowestStats != null && (float)lowestStats.CurrentHP / lowestStats.MaxHP < 0.50f)
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Minor Heal!", Color.green);
                int heal = Mathf.RoundToInt(lowestStats.MaxHP * 0.12f);
                lowestStats.Heal(heal);
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Magic Bolt!", Color.magenta);
                int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 1.1f);
                target.GetComponent<SlimeBattleStats>()?.TakeDamage(dmg, currentSlime);
                yield return new WaitForSeconds(0.8f);
            }
        }
    }

    // ── AI Dark Goblin Shaman ──
    private IEnumerator AI_DarkGoblinShaman(int turn, SlimeBattleStats stats, GameObject target)
    {
        if (turn == 1) // Dark Blessing
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Dark Blessing! (+20% MATK)", Color.magenta);
            foreach (var e in activeEnemies)
            {
                e?.GetComponent<SlimeBattleStats>()?.ApplyBuff(BuffStat.Attack, 1.20f, 2);
            }
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            var lowestAlly = activeEnemies.Where(e => e != null && e.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
                .OrderBy(e => (float)e.GetComponent<SlimeBattleStats>().CurrentHP / e.GetComponent<SlimeBattleStats>().MaxHP).FirstOrDefault();
            var lowestStats = lowestAlly != null ? lowestAlly.GetComponent<SlimeBattleStats>() : null;

            if (lowestStats != null && (float)lowestStats.CurrentHP / lowestStats.MaxHP < 0.50f)
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Minor Heal!", Color.green);
                int heal = Mathf.RoundToInt(lowestStats.MaxHP * 0.15f);
                lowestStats.Heal(heal);
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Dark Bolt!", Color.magenta);
                int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 1.2f);
                target.GetComponent<SlimeBattleStats>()?.TakeDamage(dmg, currentSlime);
                yield return new WaitForSeconds(0.8f);
            }
        }
    }

    // ── AI Poison Slime ──
    private IEnumerator AI_PoisonSlime(int turn, SlimeBattleStats stats, GameObject target)
    {
        var playerAllies = formationManager.GetAllAliveAllies();
        if (turn == 1 || playerAllies.Count >= 2) // Toxic Burst / Poison Splash AoE
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Poison Splash (AoE)!", Color.green);
            foreach (var ally in playerAllies)
            {
                var aStats = ally.GetComponent<SlimeBattleStats>();
                if (aStats != null)
                {
                    int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 1.1f);
                    aStats.TakeDamage(dmg, currentSlime, isAoE: true);
                    if (Random.Range(0f, 1f) < 0.40f) aStats.ApplyPoison(2);
                }
            }
            yield return new WaitForSeconds(1f);
        }
        else
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Toxic Bite!", Color.green);
            var tStats = target.GetComponent<SlimeBattleStats>();
            if (tStats != null)
            {
                tStats.TakeDamage(stats.GetEffectiveAttack(), currentSlime);
                tStats.ApplyPoison(2);
            }
            yield return new WaitForSeconds(0.8f);
        }
    }

    // ── AI Corrupted Goblin ──
    private IEnumerator AI_CorruptedGoblin(int turn, SlimeBattleStats stats, GameObject target)
    {
        var targetStats = target.GetComponent<SlimeBattleStats>();
        bool isPoisoned = targetStats != null && targetStats.GetPoisonStackCount() > 0;
        float targetHpPct = targetStats != null ? (float)targetStats.CurrentHP / targetStats.MaxHP : 1f;

        if (isPoisoned || targetHpPct < 0.50f) // Brutal Strike Execute
        {
            float bonus = targetHpPct < 0.50f ? 1.25f : 1.0f;
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "BRUTAL STRIKE!", Color.red);
            int dmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 1.4f * bonus);
            targetStats?.TakeDamage(dmg, currentSlime);
            if (Random.Range(0f, 1f) < 0.20f && targetStats != null) targetStats.ApplyDoT(EffectType.Bleed, Mathf.RoundToInt(targetStats.MaxHP * 0.05f), 2);
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Corrupted Slash!", Color.red);
            yield return StartCoroutine(DefaultEnemyAttack(stats, target));
        }
    }

    // ── AI Stone Goblin ──
    private IEnumerator AI_StoneGoblin(int turn, SlimeBattleStats stats, GameObject target)
    {
        if (turn % 2 == 1) // Shield Bash
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Shield Bash (-15 SPD)!", Color.gray);
            var tStats = target.GetComponent<SlimeBattleStats>();
            if (tStats != null)
            {
                int dmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 1.3f);
                tStats.TakeDamage(dmg, currentSlime);
                tStats.ApplyBuff(BuffStat.Speed, 0.85f, 2, true);
            }
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Stone Slash!", Color.gray);
            yield return StartCoroutine(DefaultEnemyAttack(stats, target));
        }
    }

    // ── AI Crystal Slime ──
    private IEnumerator AI_CrystalSlime(int turn, SlimeBattleStats stats, GameObject target)
    {
        float hpPct = (float)stats.CurrentHP / stats.MaxHP;
        if (turn == 1 || hpPct < 0.70f) // Crystal Barrier
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Crystal Barrier! (+25% DEF)", Color.cyan);
            stats.ApplyBuff(BuffStat.Defense, 1.25f, 2);
            stats.isCrystalBarrierActive = true;
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Crystal Shot!", Color.cyan);
            int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 1.2f);
            target.GetComponent<SlimeBattleStats>()?.TakeDamage(dmg, currentSlime);
            yield return new WaitForSeconds(0.8f);
        }
    }

    // ── AI Ancient Shaman ──
    private IEnumerator AI_AncientShaman(int turn, SlimeBattleStats stats, GameObject target)
    {
        if (turn == 1) // Ancient Blessing
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Ancient Blessing! (+20% DEF)", Color.yellow);
            foreach (var e in activeEnemies)
            {
                e?.GetComponent<SlimeBattleStats>()?.ApplyBuff(BuffStat.Defense, 1.20f, 2);
            }
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            var lowestAlly = activeEnemies.Where(e => e != null && e.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
                .OrderBy(e => (float)e.GetComponent<SlimeBattleStats>().CurrentHP / e.GetComponent<SlimeBattleStats>().MaxHP).FirstOrDefault();
            var lowestStats = lowestAlly != null ? lowestAlly.GetComponent<SlimeBattleStats>() : null;

            if (lowestStats != null && (float)lowestStats.CurrentHP / lowestStats.MaxHP < 0.50f)
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Restore!", Color.green);
                int heal = Mathf.RoundToInt(lowestStats.MaxHP * 0.18f);
                lowestStats.Heal(heal);
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Ancient Bolt!", Color.yellow);
                int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 1.3f);
                target.GetComponent<SlimeBattleStats>()?.TakeDamage(dmg, currentSlime);
                yield return new WaitForSeconds(0.8f);
            }
        }
    }

    // ── AI Iron Golem ──
    private IEnumerator AI_IronGolem(TowerEnemyType type, int turn, SlimeBattleStats stats, GameObject target)
    {
        float hpPct = (float)stats.CurrentHP / stats.MaxHP;
        bool isElite = (type == TowerEnemyType.EliteIronGolem);

        var crystalGolem = activeEnemies.FirstOrDefault(e => e != null && e.name.Contains("CrystalGolem") && e.GetComponent<SlimeBattleStats>()?.CurrentHP > 0);
        bool crystalLow = crystalGolem != null && ((float)crystalGolem.GetComponent<SlimeBattleStats>().CurrentHP / crystalGolem.GetComponent<SlimeBattleStats>().MaxHP) < 0.50f;

        if (turn == 1 && isElite) // Elite turn 1 counter stance
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "COUNTER STANCE (READY)", Color.red);
            stats.isCounterStanceActive = true;
            yield return new WaitForSeconds(0.8f);
        }
        else if (turn == 1) // Iron Guard
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Iron Guard! (+30% DEF)", Color.gray);
            stats.ApplyBuff(BuffStat.Defense, 1.30f, 2);
            yield return new WaitForSeconds(0.8f);
        }
        else if (hpPct < 0.70f || crystalLow) // Counter Stance / Fortress Mode
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "COUNTER STANCE / FORTRESS!", Color.red);
            stats.isCounterStanceActive = true;
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Iron Slam!", Color.gray);
            int dmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 1.2f);
            target.GetComponent<SlimeBattleStats>()?.TakeDamage(dmg, currentSlime);
            yield return new WaitForSeconds(0.8f);
        }
    }

    // ── AI Stone Golem ──
    private IEnumerator AI_StoneGolem(TowerEnemyType type, int turn, SlimeBattleStats stats, GameObject target)
    {
        bool isElite = (type == TowerEnemyType.EliteStoneGolem);
        if (turn == 1 && isElite) // Gravity Smash
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "GRAVITY SMASH!", Color.red);
            foreach (var ally in formationManager.GetAllAliveAllies())
            {
                var aStats = ally.GetComponent<SlimeBattleStats>();
                if (aStats != null)
                {
                    int dmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 1.5f);
                    aStats.TakeDamage(dmg, currentSlime, isAoE: true);
                    aStats.ApplyBuff(BuffStat.Speed, 0.85f, 2, true);
                }
            }
            yield return new WaitForSeconds(1f);
        }
        else if (turn % 2 == 1) // Ground Smash
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Ground Smash (-15 SPD)!", Color.red);
            var tStats = target.GetComponent<SlimeBattleStats>();
            if (tStats != null)
            {
                int dmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 1.5f);
                tStats.TakeDamage(dmg, currentSlime);
                tStats.ApplyBuff(BuffStat.Speed, 0.85f, 2, true);
            }
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Rock Punch!", Color.gray);
            int dmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 1.1f);
            target.GetComponent<SlimeBattleStats>()?.TakeDamage(dmg, currentSlime);
            yield return new WaitForSeconds(0.8f);
        }
    }

    // ── AI Crystal Golem ──
    private IEnumerator AI_CrystalGolem(TowerEnemyType type, int turn, SlimeBattleStats stats, GameObject target)
    {
        float hpPct = (float)stats.CurrentHP / stats.MaxHP;
        var playerAllies = formationManager.GetAllAliveAllies();

        var ironGolem = activeEnemies.FirstOrDefault(e => e != null && e.name.Contains("IronGolem"));
        bool ironCounterActive = ironGolem != null && ironGolem.GetComponent<SlimeBattleStats>()?.isCounterStanceActive == true;

        if (turn == 1 || hpPct < 0.70f) // Crystal Shield
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Crystal Shield! (+25% DEF)", Color.cyan);
            foreach (var e in activeEnemies)
            {
                e?.GetComponent<SlimeBattleStats>()?.ApplyBuff(BuffStat.Defense, 1.25f, 2);
            }
            yield return new WaitForSeconds(0.8f);
        }
        else if (playerAllies.Count >= 2 || ironCounterActive) // Crystal Nova AoE
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Crystal Nova (AoE)!", Color.cyan);
            foreach (var ally in playerAllies)
            {
                var aStats = ally.GetComponent<SlimeBattleStats>();
                if (aStats != null)
                {
                    int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 1.1f);
                    aStats.TakeDamage(dmg, currentSlime, isAoE: true);
                }
            }
            yield return new WaitForSeconds(1f);
        }
        else
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Crystal Beam!", Color.cyan);
            int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 1.35f);
            target.GetComponent<SlimeBattleStats>()?.TakeDamage(dmg, currentSlime);
            yield return new WaitForSeconds(0.8f);
        }
    }

    private IEnumerator AI_CelestialGuardian(int turn, int phase, SlimeBattleStats stats, GameObject target)
    {
        var playerAllies = formationManager.GetAllAliveAllies();

        if (phase == 1) // Phase 1 (100% -> 70% HP)
        {
            int cycle = (turn - 1) % 3 + 1;
            if (cycle == 1) // Crystal Armor (+40% DEF)
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Crystal Armor! (+40% DEF)", Color.cyan);
                stats.ApplyBuff(BuffStat.Defense, 1.40f, 2);
                yield return new WaitForSeconds(0.8f);
            }
            else if (cycle == 2) // Heaven Break AOE (180% MATK + -15% SPD)
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Heaven Break (AoE 180%)!", Color.red);
                foreach (var ally in playerAllies)
                {
                    var aStats = ally.GetComponent<SlimeBattleStats>();
                    if (aStats != null)
                    {
                        int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 1.80f);
                        aStats.TakeDamage(dmg, currentSlime, isAoE: true);
                        aStats.ApplyBuff(BuffStat.Speed, 0.85f, 2, true);
                    }
                }
                yield return new WaitForSeconds(1f);
            }
            else // Celestial Strike (140% ATK Single Target)
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Celestial Strike (140%)!", Color.yellow);
                int dmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 1.40f);
                target.GetComponent<SlimeBattleStats>()?.TakeDamage(dmg, currentSlime);
                yield return new WaitForSeconds(0.8f);
            }
        }
        else if (phase == 2) // Phase 2 (70% -> 40% HP)
        {
            int cycle = (turn - 1) % 3 + 1;
            if (cycle == 1) // Gravity Collapse (-30% SPD team player)
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Gravity Collapse (-30% SPD)!", Color.magenta);
                foreach (var ally in playerAllies)
                {
                    ally.GetComponent<SlimeBattleStats>()?.ApplyBuff(BuffStat.Speed, 0.70f, 2, true);
                }
                yield return new WaitForSeconds(0.8f);
            }
            else if (cycle == 2) // Starfall AOE (200% MATK)
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "STARFALL (AoE 200%)!", Color.red);
                foreach (var ally in playerAllies)
                {
                    var aStats = ally.GetComponent<SlimeBattleStats>();
                    if (aStats != null)
                    {
                        int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 2.00f);
                        aStats.TakeDamage(dmg, currentSlime, isAoE: true);
                    }
                }
                yield return new WaitForSeconds(1f);
            }
            else // Celestial Strike Heavy
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Celestial Strike!", Color.cyan);
                int dmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 1.50f);
                target.GetComponent<SlimeBattleStats>()?.TakeDamage(dmg, currentSlime);
                yield return new WaitForSeconds(0.8f);
            }
        }
        else
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "BERSERK STARFALL!", Color.red);
            foreach (var ally in playerAllies)
            {
                var aStats = ally.GetComponent<SlimeBattleStats>();
                if (aStats != null)
                {
                    int dmg = Mathf.RoundToInt(stats.GetEffectiveMagicAttack() * 2.00f);
                    aStats.TakeDamage(dmg, currentSlime, isAoE: true);
                }
            }
            yield return new WaitForSeconds(0.5f);

            if (turn % 3 == 0)
            {
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 2.2f, "EXTRA ACTION: CELESTIAL STRIKE!", Color.yellow);
                int singleDmg = Mathf.RoundToInt(stats.GetEffectiveAttack() * 2.20f);
                target.GetComponent<SlimeBattleStats>()?.TakeDamage(singleDmg, currentSlime);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    // ── AI Slime King (Chapter 1 Boss) ──
    private IEnumerator AI_SlimeKing(int turn, SlimeBattleStats bossStats, GameObject target)
    {
        int cycleTurn = (turn - 1) % 6 + 1;

        if (cycleTurn == 1) // Slime Splash
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Slime Splash!", Color.cyan);
            foreach (var ally in formationManager.GetAllAliveAllies())
            {
                var allyStats = ally.GetComponent<SlimeBattleStats>();
                if (allyStats != null)
                {
                    allyStats.TakeDamage(Mathf.RoundToInt(bossStats.BattleMagicAttack * 1.3f), currentSlime, isAoE: true);
                    allyStats.ApplyBuff(BuffStat.Speed, 0.8f, 2, true);
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
            bossStats.Heal(healAmount);
            yield return new WaitForSeconds(1f);
        }
        else if (cycleTurn == 5) // Charge
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "CHARGING ULTIMATE...", Color.red);
            yield return new WaitForSeconds(1f);
        }
        else if (cycleTurn == 6) // Acid Rain
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Acid Rain!", Color.magenta);
            foreach (var ally in formationManager.GetAllAliveAllies())
            {
                var allyStats = ally.GetComponent<SlimeBattleStats>();
                if (allyStats != null)
                {
                    allyStats.TakeDamage(Mathf.RoundToInt(bossStats.BattleMagicAttack * 1.5f), currentSlime, isAoE: true);
                    allyStats.ApplyPoison(2);
                }
            }
            yield return new WaitForSeconds(1f);
        }
        else
        {
            yield return StartCoroutine(DefaultEnemyAttack(bossStats, target));
        }
    }

    // ── AI Goblin Chief (Chapter 2 & 3 Boss) ──
    private IEnumerator AI_GoblinChief(int turn, SlimeBattleStats bossStats, GameObject target)
    {
        int cycleTurn = (turn - 1) % 7 + 1;

        if (cycleTurn == 1) // War Cry
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "War Cry! (+20% ATK)", Color.yellow);
            foreach (var e in activeEnemies)
            {
                if (e != null && e.name.Contains("Goblin"))
                    e.GetComponent<SlimeBattleStats>()?.ApplyBuff(BuffStat.Attack, 1.20f, 2);
            }
            yield return new WaitForSeconds(1f);
        }
        else if (cycleTurn == 3) // Cleave (AOE)
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Cleave AOE!", Color.red);
            foreach (var ally in formationManager.GetAllAliveAllies())
            {
                ally.GetComponent<SlimeBattleStats>()?.TakeDamage(Mathf.RoundToInt(bossStats.BattleAttack * 1.4f), currentSlime, isAoE: true);
            }
            yield return new WaitForSeconds(1f);
        }
        else if (cycleTurn == 5) // Charge
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "CHARGING ULTIMATE...", Color.red);
            yield return new WaitForSeconds(1f);
        }
        else if (cycleTurn == 6) // Goblin Frenzy Ultimate
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Goblin Frenzy!", Color.magenta);
            foreach (var ally in formationManager.GetAllAliveAllies())
            {
                ally.GetComponent<SlimeBattleStats>()?.TakeDamage(Mathf.RoundToInt(bossStats.BattleAttack * 1.7f), currentSlime, isAoE: true);
            }
            yield return new WaitForSeconds(1f);
        }
        else if (cycleTurn == 7) // Execution Strike
        {
            int dmg = Mathf.RoundToInt(bossStats.BattleAttack * 2.0f);
            var targetStats = target.GetComponent<SlimeBattleStats>();
            if (targetStats != null && (float)targetStats.CurrentHP / targetStats.MaxHP < 0.5f) dmg = Mathf.RoundToInt(dmg * 1.5f);
            targetStats?.TakeDamage(dmg, currentSlime);
            yield return new WaitForSeconds(1f);
        }
        else
        {
            yield return StartCoroutine(DefaultEnemyAttack(bossStats, target));
        }
    }

    private IEnumerator DefaultEnemyAttack(SlimeBattleStats attackerStats, GameObject targetGo)
    {
        var targetStats = targetGo.GetComponent<SlimeBattleStats>();
        var animController = currentSlime.GetComponent<SimpleCombatAnimation>();
        var targetAnim = targetGo.GetComponent<SimpleCombatAnimation>();

        if (animController != null) yield return StartCoroutine(animController.PlayAttackAnimation(targetGo.transform));

        int damage = attackerStats.GetEffectiveAttack();
        bool isCrit = attackerStats.TryCriticalHit();
        if (isCrit) damage = Mathf.RoundToInt(damage * attackerStats.GetFinalCritDMG());

        if (targetStats != null)
        {
            targetStats.TakeDamage(damage);
            if (isCrit) CreateDamagePopup(targetGo.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
        }
        if (targetAnim != null) yield return StartCoroutine(targetAnim.PlayHitAnimation());
    }

    protected override bool CheckWinCondition()
    {
        if (activeTowerLevel >= 1)
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

                    if (activeTowerLevel >= 1 && activeTowerLevel <= 30)
                    {
                        towerBosses.pendingRewardFloor = activeTowerLevel;
                    }

                    towerBosses.AdvanceToNextFloor();
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
            yield return SceneLoader.LoadSceneWithLoadingCoroutine("firstsave");
            yield break;
        }

        yield return base.HandleVictory();
    }
}
