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

// 2. STRUCT CHO PHÉP CUSTOM SPINE TRÊN INSPECTOR
[Serializable]
public class EnemyVisualSetup
{
    [Header("Định danh kẻ địch")]
    public TowerEnemyType enemyType;

    [Header("Cấu hình hình ảnh")]
    public SkeletonDataAsset spineAsset;
    public Sprite staticSprite; // Thêm trường cho ảnh tĩnh (2D Sprite)
    public string defaultAnimation = "animation";
    public float scale = 0.7f;
    public Color colorTint = Color.white;
    public Vector2 positionOffset = Vector2.zero; // Dùng để chỉnh lệch vị trí cho các con quái đặc biệt (như dơi bay cao)
    public bool hideArmorAndWeapon = false;       // Tắt vũ khí mặc định của clone
}

// Lớp phụ trợ để lấy chỉ số
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
    private int bossTurnCounter = 0;
    private int activeTowerLevel = 1;

    [Header("Enemy Custom Visuals (Kéo Spine vào đây)")]
    // CHỈNH SỬA SPINE CHO TỪNG LOẠI QUÁI Ở INSPECTOR
    public List<EnemyVisualSetup> enemyVisualSetups = new List<EnemyVisualSetup>();
    private Dictionary<TowerEnemyType, EnemyVisualSetup> visualLookup;

    protected override void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (boss != null) originalBossPos = boss.GetComponent<RectTransform>().anchoredPosition;

        turnList = formationManager.slimeFormation;

        // Khởi tạo Dictionary cho Visual lookup tốc độ cao
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

            // Xóa giới hạn (floorNum <= 5), mở khóa đi vô hạn tầng
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

        // Kiểm tra xem tầng này có cấu hình wave hay không
        bool hasWaves = currentFloor != null && currentFloor.waves != null && currentFloor.waves.Count > 0;

        // Nếu CÓ cấu hình wave, thì mới ẩn/xoá boss mặc định và sinh quái theo wave
        if (hasWaves)
        {
            // Clear quái hiện tại
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && enemy != boss) Destroy(enemy);
            }
            activeEnemies.Clear();
            if (boss != null) boss.SetActive(false);

            totalWaves = currentFloor.waves.Count;
            if (waveIndex >= totalWaves) return;

            var waveConfig = currentFloor.waves[waveIndex];
            for (int i = 0; i < waveConfig.enemies.Count; i++)
            {
                var enemySetup = waveConfig.enemies[i];
                TowerEnemyType type = (TowerEnemyType)((int)enemySetup.enemyType);
                SpawnEnemy(type, enemySetup.level, i, waveConfig.enemies.Count);
            }
        }
        else
        {
            // Nếu KHÔNG có cấu hình wave, thì dùng boss mặc định (Single Boss)
            totalWaves = 1;
            if (boss != null)
            {
                boss.SetActive(true);
                if (!activeEnemies.Contains(boss)) activeEnemies.Add(boss);
                MakeEnemyTargetable(boss);
                
                // Set stats fallback if not set by TurnSystem yet
                var battleStats = boss.GetComponent<SlimeBattleStats>();
                if (battleStats == null || battleStats.CurrentHP <= 0)
                {
                    var baseStats = boss.GetComponent<SlimeStats>();
                    if (baseStats != null && currentFloor != null)
                    {
                        baseStats.HP = currentFloor.baseHP;
                        baseStats.MaxHP = currentFloor.baseHP;
                        baseStats.Attack = currentFloor.baseAttack;
                        baseStats.Defense = currentFloor.baseDefense;
                        baseStats.Speed = currentFloor.baseSpeed;
                        baseStats.isEnemy = true;
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
        }

        // Cập nhật lại list hành động
        if (activeEnemies.Count > 0) 
        {
            SelectTarget(activeEnemies[0]);
        }

        foreach (var enemy in activeEnemies)
        {
            float enemySpd = GetSpeedOf(enemy);
            remainingAV[enemy] = 10000f / enemySpd;
        }
    }

    private void SpawnEnemy(TowerEnemyType type, int level, int index, int totalCount)
    {
        GameObject enemyGo = Instantiate(boss, boss.transform.parent);
        enemyGo.name = $"{type} Lv{level}";
        enemyGo.SetActive(true);

        RectTransform rect = enemyGo.GetComponent<RectTransform>();
        Vector2 offset = Vector2.zero;

        // 3. HỖ TRỢ SPAWN LÊN ĐẾN 5 QUÁI CHO CÁC WAVE CỦA CHAPTER LỚN HƠN
        if (totalCount == 2) offset = index == 0 ? new Vector2(0, 100) : new Vector2(0, -100);
        else if (totalCount == 3)
        {
            if (index == 0) offset = new Vector2(80, 120);
            else if (index == 1) offset = new Vector2(0, 0);
            else offset = new Vector2(80, -120);
        }
        else if (totalCount == 4)
        {
            if (index == 0) offset = new Vector2(80, 150);
            else if (index == 1) offset = new Vector2(0, 50);
            else if (index == 2) offset = new Vector2(80, -50);
            else offset = new Vector2(0, -150);
        }
        else if (totalCount == 5)
        {
            if (index == 0) offset = new Vector2(100, 160);
            else if (index == 1) offset = new Vector2(50, 80);
            else if (index == 2) offset = new Vector2(0, 0);
            else if (index == 3) offset = new Vector2(50, -80);
            else offset = new Vector2(100, -160);
        }

        var spine = enemyGo.GetComponentInChildren<SkeletonGraphic>();

        // CẬP NHẬT VISUAL TỪ INSPECTOR SETUP ĐỂ BẠN TÙY CHỈNH THEO Ý MUỐN
        if (visualLookup.TryGetValue(type, out var visualSetup))
        {
            if (visualSetup.spineAsset != null && spine != null)
            {
                spine.gameObject.SetActive(true);
                spine.skeletonDataAsset = visualSetup.spineAsset;
                spine.Initialize(true);
                spine.AnimationState.SetAnimation(0, visualSetup.defaultAnimation, true);
            }
            else if (visualSetup.staticSprite != null)
            {
                if (spine != null) spine.gameObject.SetActive(false);
                var imgGo = new GameObject("StaticSprite");
                imgGo.transform.SetParent(enemyGo.transform, false);
                var img = imgGo.AddComponent<Image>();
                img.sprite = visualSetup.staticSprite;
                img.color = visualSetup.colorTint;
                img.SetNativeSize();
            }
            else if (spine != null)
            {
                spine.gameObject.SetActive(true);
                spine.color = visualSetup.colorTint;
            }

            enemyGo.transform.localScale = Vector3.one * visualSetup.scale;
            offset += visualSetup.positionOffset;

            // Xóa vũ khí mặc định nếu yêu cầu
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
            enemyGo.transform.localScale = Vector3.one * 0.7f; // Fallback
        }

        rect.anchoredPosition = originalBossPos + offset;

        // Cập nhật hệ thống Stats Database thay vì switch hardcode
        TowerStatData eData = GetEnemyStatDatabase(type, level);

        var stats = enemyGo.GetComponent<SlimeStats>();
        if (stats == null) stats = enemyGo.AddComponent<SlimeStats>();
        stats.HP = eData.hp; stats.MaxHP = eData.hp;
        stats.Attack = eData.atk; stats.MagicAttack = eData.matk;
        stats.Defense = eData.def; stats.Speed = eData.spd;
        stats.CritRate = eData.crit; stats.CritDMG = eData.critDMG;
        stats.isEnemy = true;

        var battleStats = enemyGo.GetComponent<SlimeBattleStats>();
        if (battleStats == null) battleStats = enemyGo.AddComponent<SlimeBattleStats>();
        battleStats.MaxHP = eData.hp; battleStats.CurrentHP = eData.hp;
        battleStats.BattleAttack = eData.atk; battleStats.BattleMagicAttack = eData.matk;
        battleStats.BattleDefense = eData.def; battleStats.BattleSpeed = eData.spd;
        battleStats.BattleCritRate = eData.crit; battleStats.BattleCritDMG = eData.critDMG;

        if (type == TowerEnemyType.SlimeKing || type == TowerEnemyType.GoblinChief) // Init Body Skill cho Boss
        {
            stats.bodySkill = new SkillInstance(null);
        }

        activeEnemies.Add(enemyGo);
        MakeEnemyTargetable(enemyGo);
    }

    // 4. DATABASE STATS GỌN GÀNG HƠN
    private TowerStatData GetEnemyStatDatabase(TowerEnemyType type, int level)
    {
        switch (type)
        {
            // ==========================================
            // CHAPTER 1
            // ==========================================
            case TowerEnemyType.GreenSlime:
                if (level == 1) return new TowerStatData(900, 90, 70, 60, 90, 0.02f, 1.50f);
                if (level == 2) return new TowerStatData(1004, 99, 77, 65, 91, 0.04f, 1.50f);
                if (level == 3) return new TowerStatData(1119, 109, 85, 71, 92, 0.06f, 1.50f);
                if (level == 4) return new TowerStatData(1248, 120, 94, 77, 93, 0.08f, 1.50f);
                return new TowerStatData(1392, 133, 104, 84, 94, 0.10f, 1.50f);

            case TowerEnemyType.TinyBat:
                if (level == 3) return new TowerStatData(780, 105, 60, 50, 95, 0.06f, 1.50f);
                if (level == 4) return new TowerStatData(870, 116, 66, 55, 96, 0.08f, 1.50f);
                return new TowerStatData(970, 128, 73, 60, 97, 0.10f, 1.50f);

            case TowerEnemyType.SlimeKing:
                return new TowerStatData(4250, 150, 100, 105, 97, 0.15f, 1.70f);

            // ==========================================
            // CHAPTER 2
            // ==========================================
            case TowerEnemyType.GoblinWarrior:
                if (level == 6) return new TowerStatData(1550, 155, 0, 95, 96, 0.12f, 1.50f);
                if (level == 7) return new TowerStatData(1705, 168, 0, 103, 97, 0.13f, 1.50f);
                if (level == 8) return new TowerStatData(1875, 182, 0, 112, 98, 0.14f, 1.50f);
                if (level == 9) return new TowerStatData(2062, 197, 0, 121, 99, 0.15f, 1.50f);
                return new TowerStatData(2268, 214, 0, 131, 100, 0.16f, 1.50f);

            case TowerEnemyType.GoblinArcher:
                if (level == 7) return new TowerStatData(1320, 178, 0, 72, 108, 0.14f, 1.55f);
                if (level == 8) return new TowerStatData(1452, 193, 0, 78, 109, 0.15f, 1.55f);
                if (level == 9) return new TowerStatData(1597, 208, 0, 84, 110, 0.16f, 1.55f);
                return new TowerStatData(1757, 225, 0, 90, 111, 0.17f, 1.55f);

            case TowerEnemyType.GoblinShaman:
                if (level == 8) return new TowerStatData(1480, 70, 165, 88, 102, 0.12f, 1.50f);
                if (level == 9) return new TowerStatData(1628, 75, 180, 95, 103, 0.13f, 1.50f);
                return new TowerStatData(1790, 80, 196, 102, 104, 0.14f, 1.50f);

            case TowerEnemyType.GoblinChief:
                return new TowerStatData(8200, 245, 160, 165, 105, 0.18f, 1.80f); 

            // ==========================================
            // CHAPTER 3
            // ==========================================
            case TowerEnemyType.PoisonSlime:
                if (level == 11) return new TowerStatData(2450, 180, 150, 145, 100, 0.16f, 1.55f);
                if (level == 12) return new TowerStatData(2695, 195, 165, 158, 101, 0.17f, 1.55f);
                if (level == 13) return new TowerStatData(2965, 212, 182, 172, 102, 0.18f, 1.60f);
                if (level == 14) return new TowerStatData(3262, 231, 200, 188, 103, 0.19f, 1.60f);
                return new TowerStatData(3588, 252, 220, 205, 104, 0.20f, 1.65f);

            case TowerEnemyType.CorruptedGoblin:
                if (level == 11) return new TowerStatData(2600, 235, 0, 160, 102, 0.16f, 1.60f);
                if (level == 12) return new TowerStatData(2860, 255, 0, 175, 103, 0.17f, 1.60f);
                if (level == 13) return new TowerStatData(3146, 278, 0, 191, 104, 0.18f, 1.65f);
                if (level == 14) return new TowerStatData(3460, 303, 0, 208, 105, 0.19f, 1.65f);
                return new TowerStatData(3806, 330, 0, 227, 106, 0.20f, 1.70f);

            case TowerEnemyType.DarkGoblinShaman:
                if (level == 12) return new TowerStatData(2180, 85, 250, 135, 106, 0.16f, 1.55f);
                if (level == 13) return new TowerStatData(2398, 92, 275, 148, 107, 0.17f, 1.60f);
                if (level == 14) return new TowerStatData(2638, 100, 302, 162, 108, 0.18f, 1.60f);
                return new TowerStatData(2902, 108, 332, 177, 109, 0.19f, 1.65f);

            case TowerEnemyType.CorruptedGoblinChief:
                return new TowerStatData(15500, 360, 220, 260, 110, 0.22f, 1.85f);

            // ==========================================
            // CHAPTER 4
            // ==========================================
            case TowerEnemyType.StoneGoblin:
                if (level == 16) return new TowerStatData(4200, 360, 0, 260, 108, 0.20f, 1.70f);
                if (level == 17) return new TowerStatData(4620, 385, 0, 285, 109, 0.21f, 1.70f);
                if (level == 18) return new TowerStatData(5082, 412, 0, 312, 110, 0.22f, 1.75f);
                if (level == 19) return new TowerStatData(5590, 441, 0, 342, 111, 0.23f, 1.75f);
                return new TowerStatData(6150, 472, 0, 375, 112, 0.24f, 1.80f);

            case TowerEnemyType.CrystalSlime:
                if (level == 16) return new TowerStatData(3900, 250, 295, 245, 108, 0.20f, 1.70f);
                if (level == 17) return new TowerStatData(4290, 270, 322, 268, 109, 0.21f, 1.70f);
                if (level == 18) return new TowerStatData(4720, 292, 351, 294, 110, 0.22f, 1.75f);
                if (level == 19) return new TowerStatData(5192, 316, 382, 322, 111, 0.23f, 1.75f);
                return new TowerStatData(5711, 342, 416, 353, 112, 0.24f, 1.80f);

            case TowerEnemyType.AncientShaman:
                if (level == 17) return new TowerStatData(3300, 120, 365, 225, 113, 0.20f, 1.70f);
                if (level == 18) return new TowerStatData(3630, 130, 398, 247, 114, 0.21f, 1.75f);
                if (level == 19) return new TowerStatData(3993, 141, 434, 271, 115, 0.22f, 1.75f);
                return new TowerStatData(4392, 153, 473, 298, 116, 0.23f, 1.80f);

            case TowerEnemyType.AncientGuardian:
                return new TowerStatData(25000, 520, 380, 420, 115, 0.25f, 1.90f);

            // ==========================================
            // CHAPTER 5
            // ==========================================
            case TowerEnemyType.StoneGolem:
                if (level == 21) return new TowerStatData(8400, 620, 0, 520, 114, 0.25f, 1.85f);
                if (level == 22) return new TowerStatData(9240, 655, 0, 550, 116, 0.26f, 1.85f);
                if (level == 23) return new TowerStatData(10164, 692, 0, 583, 118, 0.27f, 1.90f);
                if (level == 24) return new TowerStatData(11180, 731, 0, 618, 120, 0.28f, 1.90f);
                return new TowerStatData(12300, 772, 0, 655, 122, 0.30f, 1.95f);

            case TowerEnemyType.IronGolem:
                if (level == 21) return new TowerStatData(10800, 480, 0, 760, 112, 0.22f, 1.80f);
                if (level == 22) return new TowerStatData(11880, 508, 0, 805, 114, 0.23f, 1.80f);
                if (level == 23) return new TowerStatData(13068, 538, 0, 853, 116, 0.24f, 1.85f);
                if (level == 24) return new TowerStatData(14375, 570, 0, 904, 118, 0.25f, 1.85f);
                return new TowerStatData(15812, 604, 0, 958, 120, 0.26f, 1.90f);

            case TowerEnemyType.CrystalGolem:
                if (level == 21) return new TowerStatData(7600, 300, 610, 430, 116, 0.25f, 1.90f);
                if (level == 22) return new TowerStatData(8360, 320, 645, 456, 118, 0.26f, 1.90f);
                if (level == 23) return new TowerStatData(9196, 341, 682, 483, 120, 0.27f, 1.95f);
                if (level == 24) return new TowerStatData(10115, 363, 721, 512, 122, 0.28f, 1.95f);
                return new TowerStatData(11127, 387, 763, 543, 124, 0.30f, 2.00f);

            case TowerEnemyType.AncientColossus:
                return new TowerStatData(42000, 820, 580, 820, 126, 0.32f, 2.10f);

            // ==========================================
            // CHAPTER 6
            // ==========================================
            case TowerEnemyType.EliteStoneGolem:
                if (level == 26) return new TowerStatData(15800, 1120, 0, 1120, 138, 0.32f, 2.10f);
                if (level == 27) return new TowerStatData(18600, 1220, 0, 1220, 141, 0.33f, 2.10f);
                if (level == 28) return new TowerStatData(21900, 1320, 0, 1330, 144, 0.34f, 2.15f);
                if (level == 29) return new TowerStatData(25800, 1420, 0, 1450, 147, 0.35f, 2.15f);
                return new TowerStatData(30500, 1550, 0, 1600, 150, 0.36f, 2.20f);

            case TowerEnemyType.EliteIronGolem:
                if (level == 26) return new TowerStatData(21000, 980, 0, 1500, 136, 0.30f, 2.05f);
                if (level == 27) return new TowerStatData(24700, 1080, 0, 1630, 139, 0.31f, 2.05f);
                if (level == 28) return new TowerStatData(29000, 1180, 0, 1770, 142, 0.32f, 2.10f);
                if (level == 29) return new TowerStatData(34200, 1280, 0, 1920, 145, 0.33f, 2.10f);
                return new TowerStatData(40500, 1420, 0, 2100, 148, 0.34f, 2.15f);

            case TowerEnemyType.EliteCrystalGolem:
                if (level == 26) return new TowerStatData(14500, 600, 1150, 980, 140, 0.32f, 2.15f);
                if (level == 27) return new TowerStatData(17100, 700, 1250, 1080, 143, 0.33f, 2.15f);
                if (level == 28) return new TowerStatData(20100, 800, 1360, 1190, 146, 0.34f, 2.20f);
                if (level == 29) return new TowerStatData(23600, 900, 1480, 1310, 149, 0.35f, 2.20f);
                return new TowerStatData(28000, 1050, 1620, 1450, 152, 0.36f, 2.25f);

            case TowerEnemyType.CorruptedGoblinElite:
                if (level == 26) return new TowerStatData(9800, 1220, 0, 850, 141, 0.30f, 2.00f);
                if (level == 27) return new TowerStatData(11500, 1320, 0, 930, 144, 0.31f, 2.00f);
                if (level == 28) return new TowerStatData(13500, 1420, 0, 1010, 147, 0.32f, 2.05f);
                if (level == 29) return new TowerStatData(16000, 1520, 0, 1100, 150, 0.33f, 2.05f);
                return new TowerStatData(19000, 1650, 0, 1220, 153, 0.34f, 2.10f);

            case TowerEnemyType.PoisonSlimeElite:
                if (level == 26) return new TowerStatData(8700, 700, 920, 800, 140, 0.32f, 2.05f);
                if (level == 27) return new TowerStatData(10200, 780, 1020, 880, 143, 0.33f, 2.05f);
                if (level == 28) return new TowerStatData(12000, 860, 1120, 960, 146, 0.34f, 2.10f);
                if (level == 29) return new TowerStatData(14200, 940, 1220, 1040, 149, 0.35f, 2.10f);
                return new TowerStatData(16800, 1040, 1350, 1160, 152, 0.36f, 2.15f);

            case TowerEnemyType.TinyBatElite:
                if (level == 26) return new TowerStatData(7200, 900, 0, 650, 145, 0.31f, 2.05f);
                if (level == 27) return new TowerStatData(8400, 1000, 0, 720, 148, 0.32f, 2.05f);
                if (level == 28) return new TowerStatData(9800, 1100, 0, 800, 151, 0.33f, 2.10f);
                if (level == 29) return new TowerStatData(11500, 1200, 0, 880, 154, 0.34f, 2.10f);
                return new TowerStatData(13500, 1350, 0, 980, 157, 0.35f, 2.15f);

            case TowerEnemyType.CelestialGuardian:
                return new TowerStatData(95000, 1650, 1450, 1700, 153, 0.38f, 2.20f);
        }

        return new TowerStatData(1000, 100, 100, 100, 100, 0.05f, 1.5f);
    }

    private void CheckWinLoseAfterEnemyDeath()
    {
        var nextAlive = activeEnemies.FirstOrDefault(e => e != null && e.GetComponent<SlimeBattleStats>().CurrentHP > 0);
        if (nextAlive != null) SelectTarget(nextAlive);

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                var stats = enemy.GetComponent<SlimeBattleStats>();
                if (stats != null && stats.CurrentHP <= 0)
                {
                    var spine = enemy.GetComponentInChildren<SkeletonGraphic>();
                    if (spine != null) spine.color = new Color(0.3f, 0.3f, 0.3f, 1f);

                    var img = enemy.transform.Find("StaticSprite")?.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }
        }
    }


    protected override IEnumerator NextTurn()
    {
        if (currentSlime != null) currentSlime.GetComponent<SlimeStats>().turnHalo.SetActive(false);
        yield return new WaitForSeconds(0.3f);

        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();

        // Mở rộng giới hạn tầng cho logic Wave
        if (isTowerMode && activeTowerLevel >= 1)
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
        else currentSlime = boss;

        StartCoroutine(turnDisplay());

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

        // SETUP UI TURN CHO KẺ ĐỊCH HIỆN TẠI
        var spineGraphic = currentSlime.GetComponentInChildren<SkeletonGraphic>();
        if (spineGraphic != null && curSlimeBody != null)
        {
            curSlimeBody.skeletonDataAsset = spineGraphic.skeletonDataAsset;
            curSlimeBody.allowMultipleCanvasRenderers = true;
            curSlimeBody.enableSeparatorSlots = true;
            curSlimeBody.Initialize(true);
            curSlimeBody.AnimationState.SetAnimation(0, "animation", true);
            curSlimeBody.timeScale = 2;
        }

        var enemyStats = currentSlime.GetComponent<SlimeStats>();
        if (enemyStats != null)
        {
            if (curSlimeHat != null) curSlimeHat.sprite = enemyStats.armor?.sprite;
            if (curSlimeWeapon != null) curSlimeWeapon.sprite = enemyStats.weapon?.sprite;
        }
        if (curSlimeBorder != null) curSlimeBorder.color = Color.red;

        // Bật Halo báo hiệu lượt của quái này
        if (enemyStats != null && enemyStats.turnHalo != null) enemyStats.turnHalo.SetActive(true);

        // LẤY HOẶC TỰ TẠO TRẠNG THÁI AI CHO QUÁI NÀY
        var aiState = currentSlime.GetComponent<EnemyAIState>();
        if (aiState == null) aiState = currentSlime.AddComponent<EnemyAIState>();
        aiState.currentTurnCycle++; // Tăng lượt nội bộ của con quái này lên

        // Lấy định danh Type của quái hiện tại từ Tên object hoặc một Component nhận diện
        TowerEnemyType currentType = GetEnemyTypeFromName(currentSlime.name);

        // TÌM MỤC TIÊU THÔNG MINH DỰA TRÊN
        GameObject target = GetAIQueryTarget(currentType);

        // THỰC THI AI THEO TỪNG LOẠI QUÁI
        if (target != null)
        {
            yield return StartCoroutine(ExecuteEnemyAIBehavior(currentType, aiState, target));
        }

        // KIỂM TRA ĐIỀU KIỆN KẾT THÚC TURN
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

    // Hàm phụ dùng để nhận diện Loại quái dựa trên tên Object khi khởi tạo đặt tên
    private TowerEnemyType GetEnemyTypeFromName(string objName)
    {
        foreach (TowerEnemyType type in Enum.GetValues(typeof(TowerEnemyType)))
        {
            if (objName.Contains(type.ToString())) return type;
        }
        return TowerEnemyType.GreenSlime; // Mặc định
    }

    // ==========================================
    // 3. LOGIC TÌM MỤC TIÊU
    // ==========================================
    private GameObject GetAIQueryTarget(TowerEnemyType type)
    {
        var playerAllies = formationManager.GetAllAliveAllies()
            .Where(a => a != null && a.GetComponent<SlimeBattleStats>()?.CurrentHP > 0).ToList();

        if (playerAllies.Count == 0) return null;

        switch (type)
        {
            // Chapter 2: Goblin Archer ưu tiên nhắm nhân vật HP thấp nhất
            case TowerEnemyType.GoblinArcher:
                return playerAllies.OrderBy(a => a.GetComponent<SlimeBattleStats>().CurrentHP).First();

            // Chapter 3: Corrupted Goblin ưu tiên nhắm nhân vật DEF thấp nhất
            case TowerEnemyType.CorruptedGoblin:
            case TowerEnemyType.CorruptedGoblinElite:
                return playerAllies.OrderBy(a => a.GetComponent<SlimeBattleStats>().BattleDefense).First();

            // Chapter 4 & 5: Stone Goblin/Golem ưu tiên nhắm đứa có Tốc độ (SPD) cao nhất
            case TowerEnemyType.StoneGoblin:
            case TowerEnemyType.StoneGolem:
                return playerAllies.OrderByDescending(a => a.GetComponent<SlimeBattleStats>().BattleSpeed).First();

            default:
                // Mặc định phản xạ ngẫu nhiên hàng sau/hàng trước
                return formationManager.GetRandomRowLastAlive();
        }
    }

    // ==========================================
    // 4.THỰC THI HÀNH VI AI
    // ==========================================
    private IEnumerator ExecuteEnemyAIBehavior(TowerEnemyType type, EnemyAIState ai, GameObject target)
    {
        var bossStats = currentSlime.GetComponent<SlimeBattleStats>();
        if (bossStats == null) yield break;

        // KIỂM TRA ĐIỀU KIỆN HP ĐỂ KÍCH HOẠT PHASE 2
        float hpPercent = (float)bossStats.CurrentHP / bossStats.MaxHP;
        if (hpPercent < 0.5f && !ai.isPhase2Triggered)
        {
            ai.isPhase2Triggered = true;
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 2f, "PHASE 2: BERSERK!", Color.red);
            // Xóa toàn bộ hiệu ứng bất lợi
            bossStats.GetType().GetMethod("CleanseDebuffs")?.Invoke(bossStats, null);
        }

        // SỬ DỤNG SWITCH-CASE ĐỂ CHIA LOGIC CHO TỪNG LOẠI BOSS
        switch (type)
        {
            case TowerEnemyType.SlimeKing:
                yield return StartCoroutine(AI_SlimeKing(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.GoblinChief:
                yield return StartCoroutine(AI_GoblinChief(ai.currentTurnCycle, bossStats, target));
                break;

            case TowerEnemyType.CelestialGuardian:
                yield return StartCoroutine(AI_CelestialGuardian(ai.currentTurnCycle, ai.isPhase2Triggered, bossStats, target));
                break;

            // QUÁI THƯỜNG / ELITE: ĐÁNH THƯỜNG HOẶC CAST SKILL THEO TỶ LỆ / TURN ĐẦU
            default:
                yield return StartCoroutine(AI_NormalEnemy(type, ai.currentTurnCycle, bossStats, target));
                break;
        }
    }

    // ==========================================
    // CÁC HÀM AI CHI TIẾT CHO TỪNG CON BOSS
    // ==========================================

    // AI Slime King (Chapter 1)
    private IEnumerator AI_SlimeKing(int turn, SlimeBattleStats bossStats, GameObject target)
    {
        int cycleTurn = (turn - 1) % 6 + 1; // Vòng lặp tuần hoàn 6 lượt

        if (cycleTurn == 1) // Slime Splash
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Slime Splash!", Color.cyan);
            foreach (var ally in formationManager.GetAllAliveAllies())
            {
                var allyStats = ally.GetComponent<SlimeBattleStats>();
                if (allyStats != null)
                {
                    allyStats.TakeDamage(Mathf.RoundToInt(bossStats.BattleMagicAttack * 1.3f));
                    allyStats.ApplyBuff(BuffStat.Speed, 0.8f, 2, true); // Giảm tốc
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
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.2f, $"+{healAmount} HP", Color.green);
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
                    allyStats.TakeDamage(Mathf.RoundToInt(bossStats.BattleMagicAttack * 1.5f));
                    allyStats.ApplyDoT(EffectType.Poison, Mathf.RoundToInt(allyStats.MaxHP * 0.1f), 2);
                }
            }
            yield return new WaitForSeconds(1f);
        }
        else // Lượt 2, 4 đánh thường
        {
            yield return StartCoroutine(DefaultEnemyAttack(bossStats, target));
        }
    }

    // AI Goblin Chief (Chapter 2)
    private IEnumerator AI_GoblinChief(int turn, SlimeBattleStats bossStats, GameObject target)
    {
        int cycleTurn = (turn - 1) % 7 + 1; // Chu kỳ 7 lượt

        if (cycleTurn == 1) // War Cry
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "War Cry!", Color.yellow);
            // Tăng 20% ATK và 15 SPD cho toàn bộ Goblins phe địch
            yield return new WaitForSeconds(1f);
        }
        else if (cycleTurn == 3) // Cleave (AOE)
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.8f, "Cleave AOE!", Color.red);
            foreach (var ally in formationManager.GetAllAliveAllies())
            {
                ally.GetComponent<SlimeBattleStats>()?.TakeDamage(Mathf.RoundToInt(bossStats.BattleAttack * 1.4f));
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
            // Gây sát thương AoE 170% ATK
            yield return new WaitForSeconds(1f);
        }
        else if (cycleTurn == 7) // Execution Strike đơn mục tiêu công mạnh
        {
            int dmg = Mathf.RoundToInt(bossStats.BattleAttack * 2.0f);
            var targetStats = target.GetComponent<SlimeBattleStats>();
            if (targetStats != null && (float)targetStats.CurrentHP / targetStats.MaxHP < 0.5f) dmg = Mathf.RoundToInt(dmg * 1.5f); // Tăng 50% dmg dưới 50% HP
            targetStats?.TakeDamage(dmg);
            yield return new WaitForSeconds(1f);
        }
        else
        {
            yield return StartCoroutine(DefaultEnemyAttack(bossStats, target));
        }
    }

    // AI Boss Cuối Celestial Guardian (Chapter 6 Tầng 30)
    private IEnumerator AI_CelestialGuardian(int turn, bool isPhase2, SlimeBattleStats bossStats, GameObject target)
    {
        int cycleTurn = (turn - 1) % 8 + 1; // Loop hành động của Boss Cuối
        // Turn 1: Crystal Armor, Turn 3: Heaven Break, Turn 5: Starfall, v.v...
        yield return StartCoroutine(DefaultEnemyAttack(bossStats, target)); // Thay thế bằng logic chi tiết
    }

    // AI Quái thường hoặc Quái Elite
    private IEnumerator AI_NormalEnemy(TowerEnemyType type, int turn, SlimeBattleStats bossStats, GameObject target)
    {
        // Goblin Shaman luôn dùng War Cry ở lượt đầu tiên (turn == 1)
        if ((type == TowerEnemyType.GoblinShaman || type == TowerEnemyType.DarkGoblinShaman) && turn == 1)
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "War Cry Buff!", Color.yellow);
            // Thực hiện Buff tăng ATK toàn đội địch...
            yield return new WaitForSeconds(1f);
        }
        // Crystal Slime luôn sử dụng Crystal Barrier ở lượt đầu tiên
        else if (type == TowerEnemyType.CrystalSlime && turn == 1)
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 1.5f, "Crystal Barrier!", Color.blue);
            bossStats.ApplyBuff(BuffStat.Defense, 1.25f, 2, false);
            yield return new WaitForSeconds(1f);
        }
        else
        {
            // Mặc định quái thường đánh đơn mục tiêu thông thường
            yield return StartCoroutine(DefaultEnemyAttack(bossStats, target));
        }
    }

    // Hàm tái sử dụng logic Đánh thường của phe địch bao gồm Chơi Animation và Tính Crit
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
        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();
        // Cập nhật lại điều kiện WinCondition không bị kẹt ở tầng 5
        if (isTowerMode && activeTowerLevel >= 1)
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