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
    [SerializeField] protected Queue<GameObject> turnQueue = new Queue<GameObject>();
    protected Dictionary<GameObject, float> remainingAV = new Dictionary<GameObject, float>();
    public GameObject boss;
    protected GameObject currentSlime;
    [SerializeField] protected FormationManager formationManager;
    [Header("Wild Slimes Database")]
    [SerializeField] public WildSlimes wildSlimes;

    [Header("Tower Database")]
    [SerializeField] public TowerSlimeBosses towerBosses;

    [Header("Farm Database")]
    [SerializeField] public FarmDatabaseSO farmDatabase;

    protected List<GameObject> turnList;
    public int turnCount = 0;
    public bool isBattleStarted = false;
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
    [SerializeField] protected GameObject resultPanel;
    [SerializeField] protected Text resultText;

    // Các biến cho hệ thống Chọn Mục tiêu
    protected GameObject targetIndicator;

    // ── Performance Cache ──
    // Cache Canvas để tránh FindObjectOfType mỗi lần tạo popup
    private Canvas _cachedCanvas;
    // Object Pool cho damage popup — tái sử dụng thay vì Instantiate/Destroy
    private readonly Queue<GameObject> _popupPool = new Queue<GameObject>();
    private const int POPUP_POOL_SIZE = 10;
    // List tái sử dụng tránh tạo allocation mỗi NextTurn
    private readonly List<GameObject> _activeParticipantsCache = new List<GameObject>();

    protected virtual void Start()
    {
        // Warm-up Canvas cache ngay khi start
        _cachedCanvas = GetComponentInParent<Canvas>();
        if (_cachedCanvas == null) _cachedCanvas = FindObjectOfType<Canvas>();

        // Pre-warm damage popup pool
        if (_cachedCanvas != null)
        {
            for (int i = 0; i < POPUP_POOL_SIZE; i++)
            {
                var go = CreatePopupObject();
                go.SetActive(false);
                go.transform.SetParent(_cachedCanvas.transform, false);
                _popupPool.Enqueue(go);
            }
        }
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // Tạo Target Indicator
        CreateTargetIndicator();

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

    protected void CreateTargetIndicator()
    {
        if (targetIndicator != null) return;

        targetIndicator = new GameObject("TargetIndicator");
        var spriteRenderer = targetIndicator.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Resources.Load<Sprite>("Arrow");
        spriteRenderer.sortingOrder = 1;
        
        targetIndicator.transform.localScale = new Vector3(4f, 4f, 4f);
        targetIndicator.SetActive(false);
    }

    public void SelectTarget(GameObject newTarget)
    {
        if (newTarget == null) return;

        var stats = newTarget.GetComponent<SlimeBattleStats>();
        if (stats == null || stats.CurrentHP <= 0) return; // Không chọn quái đã chết

        boss = newTarget; // Gán lại biến boss thành mục tiêu mới

        if (targetIndicator == null)
        {
            CreateTargetIndicator();
        }

        // Di chuyển Indicator lên đầu quái vật
        targetIndicator.transform.SetParent(newTarget.transform, false);
        targetIndicator.transform.localPosition = new Vector3(0, 100.0f, 0);
        targetIndicator.transform.localScale = new Vector3(4f, 4f, 4f);

        if (isBattleStarted)
        {
            targetIndicator.SetActive(true);
        }
    }

    public void MakeEnemyTargetable(GameObject enemyGo)
    {
        if (enemyGo == null) return;

        var handler = enemyGo.GetComponent<SlimeBattleClickHandler>();
        if (handler == null) handler = enemyGo.AddComponent<SlimeBattleClickHandler>();
        handler.Init(this, enemyGo.GetComponent<SlimeStats>());

        // 1. Tạo hitbox vô hình để dễ click trên Mobile
        var hitbox = new GameObject("ClickHitbox");
        hitbox.transform.SetParent(enemyGo.transform);
        hitbox.transform.localPosition = Vector3.zero;
        hitbox.transform.localScale = Vector3.one;

        var hitboxImg = hitbox.AddComponent<Image>();
        hitboxImg.color = new Color(1, 1, 1, 0); // Trong suốt hoàn toàn
        var hitboxRt = hitbox.GetComponent<RectTransform>();
        hitboxRt.sizeDelta = new Vector2(150, 200); // Kích thước hitbox bao trọn quái

        // 2. Gắn EventTrigger
        var pointerClick = hitbox.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var clickEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick };
        clickEntry.callback.AddListener((data) => {
            SelectTarget(enemyGo);
            if (SlimeStatsInspectorUI.Instance != null)
            {
                var stats = enemyGo.GetComponent<SlimeStats>();
                if (stats != null) SlimeStatsInspectorUI.Instance.InspectSlime(stats);
            }
        });
        pointerClick.triggers.Add(clickEntry);
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
            slimeData.body.skill.power = slimeData.body.GetSkillPower();
        }
        if (slimeData.armor != null && (slimeData.armor.skill == null || slimeData.armor.skill.baseSkill == null) && slimeData.armor.baseTrait != null && slimeData.armor.baseTrait.skill != null)
        {
            slimeData.armor.skill = new SkillInstance(slimeData.armor.baseTrait.skill);
            slimeData.armor.skill.power = slimeData.armor.GetSkillPower();
        }
        if (slimeData.weapon != null && (slimeData.weapon.skill == null || slimeData.weapon.skill.baseSkill == null) && slimeData.weapon.baseTrait != null && slimeData.weapon.baseTrait.skill != null)
        {
            slimeData.weapon.skill = new SkillInstance(slimeData.weapon.baseTrait.skill);
            slimeData.weapon.skill.power = slimeData.weapon.GetSkillPower();
        }

        SlimeStats bossStats = boss.GetComponent<SlimeStats>();
        if (bossStats == null)
        {
            bossStats = boss.AddComponent<SlimeStats>();
        }

        bossStats.slimeName = !string.IsNullOrEmpty(slimeData.slimeName) ? slimeData.slimeName : "Boss";
        bossStats.HP = slimeData.totalHP;
        bossStats.MaxHP = slimeData.totalHP;
        bossStats.Attack = slimeData.totalAttack;
        bossStats.MagicAttack = slimeData.totalMagicAttack;
        bossStats.Defense = slimeData.totalDefense;
        bossStats.Speed = slimeData.totalSpeed;
        bossStats.CritRate = slimeData.totalCritRate;
        bossStats.CritDMG = slimeData.totalCritDMG;
        bossStats.isEnemy = true;
        
        bool isFarm = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode();
        if (isFarm)
        {
            bossStats.useRarityBossScaling = false; // Farm Mode: Dùng đúng 100% chỉ số từ Farm Database
        }
        else
        {
            bossStats.enemyRarity = slimeData.GetHighestRarity();
            bossStats.useRarityBossScaling = true;
        }

        var bStats = boss.GetComponent<SlimeBattleStats>();
        if (bStats == null)
        {
            bStats = boss.AddComponent<SlimeBattleStats>();
        }
        bStats.ReinitializeFromBaseStats();

        if (slimeData.body?.skill != null)
            bossStats.bodySkill = slimeData.body.skill;
        if (slimeData.armor?.skill != null)
            bossStats.armorSkill = slimeData.armor.skill;
        if (slimeData.weapon?.skill != null)
            bossStats.weaponSkill = slimeData.weapon.skill;
        if (slimeData.weapon?.ultimateSkill != null)
            bossStats.weaponUltimateSkill = slimeData.weapon.ultimateSkill;

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

    protected Sprite GetStaticSpriteForTurnDisplay(GameObject go)
    {
        if (go == null) return null;

        var stats = go.GetComponent<SlimeStats>();

        // 1. Slime của PHE TA (Player) luôn dùng Spine + nón + vũ khí -> KHÔNG dùng Static Avatar!
        if (stats != null && !stats.isEnemy)
        {
            return null;
        }

        // 2. Với PHE ĐỊCH (Enemy): Ưu tiên 1 - Child "StaticSprite" được sinh ra từ SpawnEnemy
        var staticChild = go.transform.Find("StaticSprite");
        if (staticChild != null)
        {
            var img = staticChild.GetComponent<Image>();
            if (img != null && img.sprite != null) return img.sprite;

            var sr = staticChild.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) return sr.sprite;
        }

        // 3. Ưu tiên 2: Tra cứu từ enemyVisualSetups trong TowerTurnSystem theo tên quái
        var towerSystem = this as TowerTurnSystem;
        if (towerSystem != null && towerSystem.enemyVisualSetups != null)
        {
            foreach (var setup in towerSystem.enemyVisualSetups)
            {
                if (setup != null && setup.staticSprite != null)
                {
                    if (go.name.StartsWith(setup.enemyType.ToString(), System.StringComparison.OrdinalIgnoreCase))
                    {
                        return setup.staticSprite;
                    }
                }
            }
        }

        // 4. Ưu tiên 3: Nếu quái KHÔNG CÓ Spine animation (spine == null hoặc skeletonDataAsset == null)
        var spine = go.GetComponentInChildren<SkeletonGraphic>(true);
        if (spine == null || spine.skeletonDataAsset == null)
        {
            if (stats != null && stats.bodySkill?.baseSkill?.icon != null)
            {
                return stats.bodySkill.baseSkill.icon;
            }
        }

        return null;
    }

    protected IEnumerator turnDisplay()
    {
        foreach (Transform child in turnPanel.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        yield return new WaitForSeconds(0.3f);
        List<GameObject> upcoming = GetUpcomingTurns(5);
        foreach (GameObject go in upcoming)
        {
            if (go == null) continue;
            GameObject turn = Instantiate(slimeTurn, turnPanel.transform);
            var display = turn.GetComponent<TurnDisplay>();
            if (display == null) continue;

            Sprite staticSprite = GetStaticSpriteForTurnDisplay(go);

            if (staticSprite != null)
            {
                // Tắt các thành phần Spine & vũ khí mặc định của Slime 3 thành phần
                if (display.body != null) display.body.gameObject.SetActive(false);
                if (display.hat != null) display.hat.gameObject.SetActive(false);
                if (display.weapon != null) display.weapon.gameObject.SetActive(false);

                // Hiển thị Avatar Tĩnh (2D Image)
                Image avatarImg = display.staticAvatar;
                if (avatarImg == null)
                {
                    Transform staticTrans = display.transform.Find("StaticAvatar");
                    if (staticTrans != null)
                    {
                        avatarImg = staticTrans.GetComponent<Image>();
                    }
                    else
                    {
                        GameObject avatarGO = new GameObject("StaticAvatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        avatarGO.transform.SetParent(display.transform, false);
                        avatarGO.transform.SetAsFirstSibling();
                        avatarImg = avatarGO.GetComponent<Image>();
                        RectTransform rt = avatarGO.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = Vector2.zero;
                        rt.sizeDelta = new Vector2(160f, 160f);
                    }
                }

                if (avatarImg != null)
                {
                    avatarImg.gameObject.SetActive(true);
                    avatarImg.sprite = staticSprite;
                    avatarImg.preserveAspect = true;
                }
            }
            else
            {
                // Nếu có staticAvatar UI cũ thì ẩn đi
                if (display.staticAvatar != null) display.staticAvatar.gameObject.SetActive(false);
                Transform staticTrans = display.transform.Find("StaticAvatar");
                if (staticTrans != null) staticTrans.gameObject.SetActive(false);

                // Hiển thị Spine Avatar cho Slime phe ta
                var spine = go.GetComponentInChildren<SkeletonGraphic>(true);
                if (spine != null && spine.skeletonDataAsset != null)
                {
                    if (display.body != null)
                    {
                        display.body.gameObject.SetActive(true);
                        display.body.skeletonDataAsset = spine.skeletonDataAsset;
                        display.body.allowMultipleCanvasRenderers = true;
                        display.body.enableSeparatorSlots = true;
                        display.body.Initialize(true);
                        display.body.AnimationState.SetAnimation(0, "animation", true);
                        display.body.timeScale = 2;
                    }
                }
                else
                {
                    if (display.body != null) display.body.gameObject.SetActive(false);
                }

                var stats = go.GetComponent<SlimeStats>();
                if (stats != null)
                {
                    if (display.hat != null)
                    {
                        bool hasArmor = stats.armor != null && stats.armor.sprite != null;
                        display.hat.gameObject.SetActive(hasArmor);
                        if (hasArmor) display.hat.sprite = stats.armor.sprite;
                    }
                    if (display.weapon != null)
                    {
                        bool hasWeapon = stats.weapon != null && stats.weapon.sprite != null;
                        display.weapon.gameObject.SetActive(hasWeapon);
                        if (hasWeapon) display.weapon.sprite = stats.weapon.sprite;
                    }
                }
            }
        }
    }


    protected virtual IEnumerator DelayedSetupCombatAnimations()
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
        foreach (var slime in formationManager.slimeFormation)
        {
            if (slime != null && slime.GetComponent<SimpleCombatAnimation>() == null)
                slime.AddComponent<SimpleCombatAnimation>();
        }

        var allEnemies = FindObjectsByType<SlimeStats>(FindObjectsSortMode.None).Where(s => s != null && s.isEnemy && s.gameObject.activeInHierarchy);
        foreach (var enemy in allEnemies)
        {
            if (enemy.GetComponent<SimpleCombatAnimation>() == null)
                enemy.gameObject.AddComponent<SimpleCombatAnimation>();
        }
    }

    private void SetupBattleStats()
    {
        foreach (var slime in formationManager.slimeFormation)
        {
            if (slime != null && slime.GetComponent<SlimeBattleStats>() == null)
                slime.AddComponent<SlimeBattleStats>();
        }

        var allEnemies = FindObjectsByType<SlimeStats>(FindObjectsSortMode.None).Where(s => s != null && s.isEnemy && s.gameObject.activeInHierarchy);
        foreach (var enemy in allEnemies)
        {
            if (enemy.GetComponent<SlimeBattleStats>() == null)
                enemy.gameObject.AddComponent<SlimeBattleStats>();
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

    protected virtual void TurnSorting()
    {
        // Tạo list tạm thời — chỉ một GetComponent mỗi slime
        var scored = new List<(GameObject go, int score)>(turnList.Count);
        for (int i = 0; i < turnList.Count; i++)
        {
            var s = turnList[i];
            if (s == null || !s.activeInHierarchy) continue;
            var bStats = s.GetComponent<SlimeBattleStats>();
            if (bStats == null || bStats.CurrentHP <= 0) continue;
            int spd = bStats.BattleSpeed;
            var sStats = s.GetComponent<SlimeStats>();
            if (sStats != null && sStats.isEnemy)
            {
                string n = s.name;
                if (n.Contains("TinyBat")) spd += 100000;
                else if (n.Contains("GoblinArcher")) spd += 50000;
            }
            scored.Add((s, spd));
        }
        scored.Sort((a, b) => b.score.CompareTo(a.score));
        turnQueue = new Queue<GameObject>(scored.Count);
        for (int i = 0; i < scored.Count; i++)
            turnQueue.Enqueue(scored[i].go);
    }

    public void StartGame()
    {
        isBattleStarted = true;

        if (boss == null)
        {
            var firstEnemy = FindObjectsByType<SlimeStats>(FindObjectsSortMode.None)
                .FirstOrDefault(s => s.isEnemy && s.gameObject.activeInHierarchy);
            if (firstEnemy != null) boss = firstEnemy.gameObject;
        }

        if (boss != null)
        {
            SelectTarget(boss);
        }

        if (targetIndicator != null && boss != null)
        {
            targetIndicator.SetActive(true);
        }

        var dragHandlers = FindObjectsByType<SlimeDragHandler>(FindObjectsSortMode.None);
        foreach (var handler in dragHandlers) handler.enabled = false;

        // Lấy danh sách phe ta
        turnList = formationManager.slimeFormation
            .Where(s => s != null && (s.GetComponent<SlimeDragHandler>() == null || s.GetComponent<SlimeDragHandler>().isUsed))
            .Where(s => s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
            .ToList();

        // Lấy TẤT CẢ kẻ địch đang có trên sân để đưa vào Turn
        var allEnemies = FindObjectsByType<SlimeStats>(FindObjectsSortMode.None)
            .Where(s => s != null && s.isEnemy && s.gameObject.activeInHierarchy && s.GetComponent<SlimeBattleStats>()?.CurrentHP > 0)
            .Select(s => s.gameObject).ToList();

        foreach (var enemy in allEnemies)
        {
            if (!turnList.Contains(enemy)) turnList.Add(enemy);
        }

        foreach (var s in turnList)
        {
            if (s != null)
            {
                var handler = s.GetComponent<SlimeBattleClickHandler>();
                if (handler == null) handler = s.AddComponent<SlimeBattleClickHandler>();
                handler.Init(this, s.GetComponent<SlimeStats>());
            }

            var stats = s != null ? s.GetComponent<SlimeStats>() : null;
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


    protected void InitializeAVSystem()
    {
        remainingAV.Clear();
        foreach (var slime in turnList)
        {
            if (slime == null) continue;
            float speed = GetSpeedOf(slime);
            remainingAV[slime] = 10000f / speed;
        }
    }

    protected float GetSpeedOf(GameObject slime)
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

    protected virtual IEnumerator NextTurn()
    {
        if (currentSlime != null) currentSlime.GetComponent<SlimeStats>().turnHalo.SetActive(false);
        yield return new WaitForSeconds(0.3f);

        // Lọc danh sách còn sống và active — tái sử dụng list, không tạo mới
        _activeParticipantsCache.Clear();
        foreach (var kvp in remainingAV)
        {
            var s = kvp.Key;
            if (s != null && s.activeInHierarchy)
            {
                var bs = s.GetComponent<SlimeBattleStats>();
                if (bs != null && bs.CurrentHP > 0)
                    _activeParticipantsCache.Add(s);
            }
        }
        var activeParticipants = _activeParticipantsCache;

        if (activeParticipants.Count == 0)
        {
            // Reset nếu trống
            InitializeAVSystem();
        _activeParticipantsCache.Clear();
            foreach (var kvp in remainingAV)
            {
                var s = kvp.Key;
                if (s != null && s.activeInHierarchy)
                {
                    var bs = s.GetComponent<SlimeBattleStats>();
                    if (bs != null && bs.CurrentHP > 0)
                        _activeParticipantsCache.Add(s);
                }
            }
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
            // Trừ AV đã đi qua cho các nhân vật khác (để tiến hành động)
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

        // Bắt đầu lượt mới theo AV: reset trần sinh ĐCK (≤ +2/lượt)
        if (BattleSystemManager.Instance != null)
        {
            BattleSystemManager.Instance.OnNewTurnStarted();
        }

        var battleStats = currentSlime.GetComponent<SlimeBattleStats>();

        // Tích giảm thời gian Buff và Stun vào ĐẦU lượt của Slime đó (Chuẩn RPG)
        if (battleStats != null)
        {
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

        // Cache GetComponent — tránh gọi 3 lần trên cùng một object
        var slimeStats = currentSlime.GetComponent<SlimeStats>();
        if (slimeStats != null)
        {
            if (slimeStats.turnHalo != null) slimeStats.turnHalo.SetActive(true);
            var armorSprite = slimeStats.armor?.sprite;
            if (curSlimeHat != null)
            {
                curSlimeHat.gameObject.SetActive(armorSprite != null);
                if (armorSprite != null) curSlimeHat.sprite = armorSprite;
            }
            var weaponSprite = slimeStats.weapon?.sprite;
            if (curSlimeWeapon != null)
            {
                curSlimeWeapon.gameObject.SetActive(weaponSprite != null);
                if (weaponSprite != null) curSlimeWeapon.sprite = weaponSprite;
            }
        }

        var spine = currentSlime.GetComponentInChildren<SkeletonGraphic>();
        if (spine != null)
        {
            curSlimeBody.skeletonDataAsset = spine.skeletonDataAsset;
            curSlimeBody.allowMultipleCanvasRenderers = true;
            curSlimeBody.enableSeparatorSlots = true;
            curSlimeBody.Initialize(true);
            curSlimeBody.AnimationState.SetAnimation(0, "animation", true);
            curSlimeBody.timeScale = 2;
        }

        curSlimeBorder.color = Color.white;

        // Cập nhật SkillUI — event-driven thay vì Update() mỗi frame
        var skillUI = skillPanel.GetComponent<SkillUI>();
        if (skillUI != null)
        {
            skillUI.slime = slimeStats;
            skillUI.ForceRefresh();
        }
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
    protected void TickCurrentSlimeEffects()
    {
        var battleStats = currentSlime?.GetComponent<SlimeBattleStats>();
        battleStats?.TickBuffs();
        battleStats?.TickStun();
    }
    public void DoAutoAttack()
    {
        if (!skillPanel.activeSelf) return; // Tránh spam click nhiều lần
        skillPanel.SetActive(false); // Ẩn ngay lập tức để không click thêm được nữa
        StartCoroutine(AutoAttack());
    }

    protected virtual IEnumerator AutoAttack()
    {
        var target = boss.GetComponent<SlimeBattleStats>();
        var attacker = currentSlime.GetComponent<SlimeBattleStats>();

        if (target != null && attacker != null && attacker.CurrentHP > 0)
        {
            attacker.AddEnergy(20); // +20 Energy khi đánh đòn thường

            // Cộng +1 Điểm Chiến Kỹ (SP) khi đánh thường
            if (BattleSystemManager.Instance != null)
            {
                BattleSystemManager.Instance.AddBattlePoints(1);
                CreateDamagePopup(currentSlime.transform.position + Vector3.up * 2f, "+1 SP", Color.cyan);
            }

            // Lấy SimpleCombatAnimation của slime tấn công
            var attackerAnimController = currentSlime.GetComponent<SimpleCombatAnimation>();
            var targetAnimController = boss.GetComponent<SimpleCombatAnimation>();

            // Chơi animation tấn công
            if (attackerAnimController != null && attackerAnimController.gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(attackerAnimController.PlayAttackAnimation(boss.transform));
            }

            // Calculate damage using GDD: Normal Damage = ATK * (1 - DEF_enemy * 0.008)
            // Effective ATK includes any Crit DMG overflow bonus
            int damage = attacker.GetEffectiveAttack();
            bool isCrit = attacker.TryCriticalHit();
            if (isCrit)
            {
                float critMult = attacker.GetFinalCritDMG();
                damage = Mathf.RoundToInt(damage * critMult);
                Debug.Log("Critical Hit!");
            }

            // Apply damage (TakeDamage in target applies defense and popup)
            target.TakeDamage(damage);

            // Show CRIT notification if critical
            if (isCrit)
            {
                CreateDamagePopup(target.transform.position + Vector3.up * 2.2f, "CRIT!", Color.yellow);
            }

#if UNITY_EDITOR
            Debug.Log($"{currentSlime.name} attacks {boss.name} for {damage} damage!");
#endif

            // Chơi animation bị đánh cho target
            if (targetAnimController != null && targetAnimController.gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(targetAnimController.PlayHitAnimation());
            }

            if (target.CurrentHP <= 0)
            {
#if UNITY_EDITOR
                Debug.Log($"{boss.name} died!");
#endif
                var nextAlive = formationManager.GetAllAliveEnemies(boss).FirstOrDefault();
                if (nextAlive != null) SelectTarget(nextAlive);
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
    public void UseBodySkill()
    {
        var stats = currentSlime != null ? currentSlime.GetComponent<SlimeStats>() : null;
        if (stats == null) return;

        if (stats.bodySkill != null && stats.bodySkill.baseSkill != null && stats.bodySkill.baseSkill.type == SkillType.Passive)
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 2.2f, "Can't use Passive!", Color.yellow);
            return;
        }

        ExecuteSkillLogic(stats.bodySkill);
    }

    public void UseHatSkill()
    {
        ExecuteSkillLogic(currentSlime.GetComponent<SlimeStats>().armorSkill);
    }

    public void UseWeaponSkill()
    {
        var stats = currentSlime.GetComponent<SlimeStats>();
        if (stats == null) return;

        var battleStats = currentSlime.GetComponent<SlimeBattleStats>();
        SkillInstance skillToUse = stats.weaponSkill;

        // Auto fallback ultimate skill nếu chưa có
        if (stats.weaponUltimateSkill == null && stats.weaponSkill?.baseSkill != null && SlimeGen.Instance != null)
        {
            var ultSO = SlimeGen.Instance.GetMatchingUltimateWeaponSkill(stats.weaponSkill.baseSkill);
            if (ultSO != null) stats.weaponUltimateSkill = new SkillInstance(ultSO);
        }

        if (battleStats != null && stats.weaponUltimateSkill != null && stats.weaponUltimateSkill.baseSkill != null)
        {
            int energyCost = stats.weaponUltimateSkill.baseSkill.energyCost > 0 ? stats.weaponUltimateSkill.baseSkill.energyCost : 100;
            if (battleStats.CurrentEnergy >= energyCost)
            {
                skillToUse = stats.weaponUltimateSkill;
            }
        }

        ExecuteSkillLogic(skillToUse);
    }

    private void ExecuteSkillLogic(SkillInstance skillInstance)
    {
        if (skillInstance == null || skillInstance.baseSkill == null)
        {
            Debug.Log("Không có kỹ năng.");
            return;
        }

        if (skillInstance.baseSkill.type == SkillType.Passive)
        {
            CreateDamagePopup(currentSlime.transform.position + Vector3.up * 2.2f, "Can't use Passive!", Color.yellow);
            return;
        }

        var caster = currentSlime.GetComponent<SlimeBattleStats>();

        // Kiểm tra tài nguyên ĐCK / NL thông qua BattleSystemManager
        if (BattleSystemManager.Instance != null)
        {
            if (!BattleSystemManager.Instance.CanUseSkill(skillInstance.baseSkill, caster))
            {
                Debug.Log($"Không đủ điểm để dùng {skillInstance.baseSkill.skillName}");
                return;
            }

            // Trừ điểm ngay lập tức nếu đủ điều kiện
            BattleSystemManager.Instance.ExecuteSkill(skillInstance.baseSkill, caster);
        }

        // Tắt bảng điều khiển kỹ năng
        skillPanel.SetActive(false);

        // Kích hoạt kỹ năng
        StartCoroutine(DoSkill(skillInstance, boss));
    }

    protected virtual IEnumerator DoSkill(SkillInstance skill, GameObject target)
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
                        float magicWeight = (skill.baseSkill != null && skill.baseSkill.type == SkillType.Ultimate) ? 0.9f : 0.8f;
                        float baseSkillPower = magicWeight * attacker.GetEffectiveMagicAttack() + (1f - magicWeight) * attacker.GetEffectiveAttack();
                        float rawDamage = baseSkillPower * entry.value + entry.flatBonus;
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

    protected virtual IEnumerator BossTurn()
    {
        turnCount++;
        
        var spine = currentSlime.GetComponentInChildren<SkeletonGraphic>(true);
        if (spine != null && spine.skeletonDataAsset != null)
        {
            curSlimeBody.skeletonDataAsset = spine.skeletonDataAsset;
            curSlimeBody.allowMultipleCanvasRenderers = true;
            curSlimeBody.enableSeparatorSlots = true;
            curSlimeBody.Initialize(true);
            curSlimeBody.AnimationState.SetAnimation(0, "animation", true);
            curSlimeBody.timeScale = 2;
            curSlimeBody.gameObject.SetActive(true);
        }
        else
        {
            curSlimeBody.gameObject.SetActive(false);
        }
        
        var stats = currentSlime.GetComponent<SlimeStats>();
        if (stats != null)
        {
            if (stats.armor != null) curSlimeHat.sprite = stats.armor.sprite;
            if (stats.weapon != null) curSlimeWeapon.sprite = stats.weapon.sprite;
        }
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
            if (bossAnimController != null && bossAnimController.gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(bossAnimController.PlayAttackAnimation(target.transform));
            }

            // Calculate damage using GDD
            int damage = bossStats != null ? bossStats.GetEffectiveAttack() : boss.GetComponent<SlimeStats>().Attack;
            bool isCrit = bossStats != null && bossStats.TryCriticalHit();
            if (isCrit)
            {
                float critMult = bossStats != null ? bossStats.GetFinalCritDMG() : 1.5f;
                damage = Mathf.RoundToInt(damage * critMult);
                Debug.Log("Boss Critical Hit!");
            }

            // Apply damage
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
                    combatAnim.ForceSetOriginalScale();
            }
        }

        if (boss != null)
        {
            var bossAnim = boss.GetComponent<SimpleCombatAnimation>();
            if (bossAnim != null)
                bossAnim.ForceSetOriginalScale();
        }
    }

    protected virtual bool CheckWinCondition()
    {
        // Dùng formationManager để kiểm tra enemy còn sống — tránh FindObjectsByType
        var aliveEnemies = formationManager.GetAllAliveEnemies(boss);
        return aliveEnemies == null || aliveEnemies.Count == 0;
    }


    // Kiểm tra điều kiện thua (tất cả team slimes HP = 0)
    protected bool CheckLoseCondition()
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

    protected virtual IEnumerator HandleVictory()
    {
        {
            string bMode = BattleDataManager.Instance?.GetBattleMode().ToString().ToLower() ?? "adventure";
            string diff = "";
            int coinsEarned = 0;
            
            if (BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode())
            {
                diff = PlayerPrefs.GetString("ActiveFarm_Name", "unknown");
                coinsEarned = PlayerPrefs.GetInt("ActiveFarm_Coins", 0);
            }
            FirebaseAnalyticsManager.LogBattleWin(bMode, diff, turnCount, coinsEarned);
        }

        // Đếm lifetime: 1 trận thắng (mọi chế độ) cho Thành tựu "Chiến binh".
        PlayerStatsManager.Instance?.AddBattleWin();

        // Thông báo quest system về trận thắng
        if (QuestManager.Instance != null && BattleDataManager.Instance != null)
            QuestManager.Instance.RegisterBattleWin(BattleDataManager.Instance.GetBattleMode());

        // Hiển thị panel kết quả thắng
        ShowResultPanel(true);

        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();
        bool isFarmMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode();

        if (isFarmMode)
        {
            int completedIndex = PlayerPrefs.GetInt("ActiveFarm_Index", -1);
            int pCoins = PlayerPrefs.GetInt("ActiveFarm_Coins", 0);
            int pGems = PlayerPrefs.GetInt("ActiveFarm_Gems", 0);

            if (farmDatabase != null)
            {
                if (completedIndex < 0) completedIndex = farmDatabase.activeSelectedDifficultyIndex;
                farmDatabase.RecordVictory(completedIndex, pCoins, pGems);
            }

            PlayerPrefs.SetInt("PendingFarm_Index", completedIndex);
            PlayerPrefs.SetInt("PendingFarm_Coins", pCoins);
            PlayerPrefs.SetInt("PendingFarm_Gems", pGems);
            PlayerPrefs.Save();

            PlayerStatsManager.Instance?.AddFarmWin();

            if (BattleDataManager.Instance != null)
            {
                BattleDataManager.Instance.ClearBossData();
            }

            yield return new WaitForSeconds(2f);
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
                        int newStars = TowerSlimeBosses.CalculateStars(turnCount);
                        if (newStars > currentFloor.stars) currentFloor.stars = newStars;
                        if (currentFloor.bestTurnCount == 0 || turnCount < currentFloor.bestTurnCount) currentFloor.bestTurnCount = turnCount;

                        Debug.Log($"Đã hoàn thành màn {currentFloor.floorNumber}: {currentFloor.floorName} trong {turnCount} lượt ({newStars} sao)");

                        towerBosses.cachedCompletedFloorNumber = currentFloor.floorNumber;
                        towerBosses.cachedCurrentFloor = towerBosses.currentFloor;
                        towerBosses.cachedHighestFloor = towerBosses.highestFloorReached;
                        towerBosses.cachedCompletedStars = currentFloor.stars;
                        towerBosses.cachedCompletedTurnCount = currentFloor.bestTurnCount;
                        towerBosses.hasPendingResult = true;
                    }

                    towerBosses.AdvanceToNextFloor();
                    PlayerStatsManager.Instance?.RecordTowerFloor(towerBosses.highestFloorReached);
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
                    PlayerStatsManager.Instance?.RecordCapture(tamedSlime.wildSlimeTraits);

                    if (SaveAndLoadSystem.Instance != null)
                    {
                        SaveAndLoadSystem.Instance.Save();
                    }
                }
            }

            BattleDataManager.Instance.ClearBossData();
        }

        yield return new WaitForSeconds(2f);
        string returnScene = BattleDataManager.Instance != null && !string.IsNullOrEmpty(BattleDataManager.Instance.ReturnSceneName)
            ? BattleDataManager.Instance.ReturnSceneName
            : "firstsave";
        yield return SceneLoader.LoadSceneWithLoadingCoroutine(returnScene);
    }

    public void TriggerDefeat()
    {
        StartCoroutine(HandleDefeat());
    }

    protected IEnumerator HandleDefeat()
    {
        // Log analytics khi thua
        {
            string bMode = BattleDataManager.Instance?.GetBattleMode().ToString().ToLower() ?? "adventure";
            string diff = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode()
                ? (FarmModeManager.Instance?.SelectedDifficultyName ?? "unknown") : "";
            FirebaseAnalyticsManager.LogBattleLose(bMode, diff, turnCount);
        }

        // Hiển thị panel kết quả thua
        ShowResultPanel(false);

        bool isTowerMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsTowerMode();
        bool isFarmMode = BattleDataManager.Instance != null && BattleDataManager.Instance.IsFarmMode();

        string targetReturnScene = "firstsave";
        if (BattleDataManager.Instance != null && !string.IsNullOrEmpty(BattleDataManager.Instance.ReturnSceneName))
        {
            targetReturnScene = BattleDataManager.Instance.ReturnSceneName;
        }

        if (BattleDataManager.Instance != null)
        {
            BattleDataManager.Instance.ClearBossData();
        }

        yield return new WaitForSeconds(2f);

        yield return SceneLoader.LoadSceneWithLoadingCoroutine(targetReturnScene);
    }

    protected void ShowResultPanel(bool isVictory)
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

    private Font GetDefaultFont()
    {
        var existingText = FindFirstObjectByType<Text>();
        if (existingText != null && existingText.font != null) return existingText.font;

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") 
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
            ?? Resources.Load<Font>("Knewave-Regular")
            ?? Font.CreateDynamicFontFromOSFont("Arial", 28);
    }

    private GameObject CreatePopupObject()
    {
        var go = new GameObject("BattlePopupText");
        var rt = go.AddComponent<RectTransform>();

        float screenScale = Screen.height / 1080f;
        rt.sizeDelta = new Vector2(180 * screenScale, 45 * screenScale);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        var txt = go.AddComponent<Text>();
        txt.font = GetDefaultFont();
        txt.fontSize = Mathf.Max(16, (int)(28 * screenScale));
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;

        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        return go;
    }

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
        float duration = 1.0f;
        float elapsed = 0f;
        var rt = go.GetComponent<RectTransform>();
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, 80f);
        Color startColor = textComponent != null ? textComponent.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (go == null) yield break;

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            if (textComponent != null)
            {
                textComponent.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            }
            yield return null;
        }

        if (go != null)
        {
            go.SetActive(false);
            _popupPool.Enqueue(go);
        }
    }
}