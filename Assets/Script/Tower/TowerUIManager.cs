using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TowerUIManager : MonoBehaviour
{
    [Header("Data")]
    public TowerSlimeBosses towerDatabase;

    [Header("Panel & Horizontal Scroll View")]
    public GameObject towerPanel;               // Root panel (bật/tắt)
    public ScrollRect mapScrollRect;            // ScrollRect cuộn ngang
    public Transform floorListContainer;        // Container chứa nút (tùy chọn)
    public GameObject floorItemPrefab;          // Prefab nút chọn màn (FloorItem)

    [Header("Map Nodes Trực Tiếp Trên Canvas")]
    public List<TowerFloorItem> mapNodes = new List<TowerFloorItem>();

    [Header("Auto-Spawn Settings (Nếu chưa kéo nút sẵn)")]
    public float nodeHorizontalSpacing = 220f;  // Khoảng cách ngang giữa các nút sinh thêm
    public float nodeZigZagAmplitude = 80f;     // Độ nhấp nhô Ziczac Lên/Xuống theo chiều cao Y
    public float startMarginX = 150f;           // Lề xuất phát bên trái

    [Header("Sprites Theo Nhóm 5 Màn (Gán chung 1 lần cho Màn 1->5)")]
    public Sprite[] globalClusterSprites = new Sprite[5]; // 5 Sprite cho Màn 1, 2, 3, 4, 5 (Hoặc 6-10, 11-15)

    [Header("Global Star Sprites (Gán 1 lần cho toàn bộ Nút)")]
    public Sprite globalActiveStarSprite;      // Sprite Sao Sáng
    public Sprite globalInactiveStarSprite;    // Sprite Sao Tối

    [Header("Header Info")]
    public Text headerText;                     // "Tower of Slimes"
    public Text currentFloorText;               // "Current floor: 3 / 15"
    public Text highestFloorText;               // "Highest Reached: Floor 2"

    [Header("Warning")]
    public GameObject warningText;              // Hiện khi team chưa có slime hoặc tầng bị khóa
    public Text warningTextLabel;               // (Tùy chọn) Label thông báo

    private static readonly WaitForSeconds WarningDelay = new(3f);

    [Header("Reward Popup (tuỳ chọn)")]
    public GameObject rewardPopup;              // Panel hiện khi claim
    public Text rewardPopupText;                // Nội dung phần thưởng
    public Button rewardPopupCloseButton;

    private void Awake()
    {
        if (rewardPopupCloseButton != null)
            rewardPopupCloseButton.onClick.AddListener(HideRewardPopup);
        if (rewardPopup != null)
            rewardPopup.SetActive(false);
        if (towerPanel != null)
            towerPanel.SetActive(false);
        if (warningText != null)
            warningText.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(RefreshAfterLoad());
    }

    private IEnumerator RefreshAfterLoad()
    {
        yield return new WaitForSeconds(0.3f);
        Refresh();

        if (towerDatabase != null && towerDatabase.pendingRewardFloor > 0)
        {
            int floorToReward = towerDatabase.pendingRewardFloor;
            towerDatabase.pendingRewardFloor = 0;
            OpenPanel();
            GrantAndShowReward(floorToReward);
        }
    }

    // ── Public interface ──────────────────────────────────────────────

    public void OpenPanel()
    {
        if (towerPanel != null) towerPanel.SetActive(true);
        Refresh();
        StartCoroutine(ScrollToCurrentFloorNextFrame());
    }

    public void ClosePanel()
    {
        if (towerPanel != null) towerPanel.SetActive(false);
    }

    public void Refresh()
    {
        if (towerDatabase == null)
        {
            Debug.LogWarning("TowerUIManager: towerDatabase chưa được gán!");
            return;
        }

        UpdateHeader();
        RebuildFloorList();
    }

    // ── Thao tác Trận đấu & Phần thưởng ───────────────────────────────

    public void OnStartBattle()
    {
        if (towerDatabase == null) return;

        var saveSystem = SaveAndLoadSystem.Instance;
        var team = saveSystem != null ? saveSystem.GetTeam() : null;
        if (team == null || team.team == null || team.team.Count == 0)
        {
            ShowWarning("Cần ít nhất 1 slime trong team để vào Tower!");
            return;
        }

        if (warningText != null) warningText.SetActive(false);

        if (towerDatabase.currentFloor == 0)
            towerDatabase.currentFloor = 1;

        if (BattleDataManager.Instance == null)
        {
            var go = new GameObject("BattleDataManager");
            go.AddComponent<BattleDataManager>();
        }
        BattleDataManager.Instance.SetBattleMode(BattleMode.Tower);

        if (SaveAndLoadSystem.Instance != null)
        {
            SaveAndLoadSystem.Instance.Save();
            Debug.Log("[TowerUIManager] Đã lưu dữ liệu trước khi vào trận đấu chính.");
        }
        StartCoroutine(LoadBattleScene());
    }

    public void OnReplayFloor(int floorNumber)
    {
        if (towerDatabase == null) return;

        var saveSystem = SaveAndLoadSystem.Instance;
        var team = saveSystem != null ? saveSystem.GetTeam() : null;
        if (team == null || team.team == null || team.team.Count == 0)
        {
            ShowWarning("Cần ít nhất 1 slime trong team để vào Tower!");
            return;
        }

        if (warningText != null) warningText.SetActive(false);

        towerDatabase.replayFloor = floorNumber;

        if (BattleDataManager.Instance == null)
        {
            var go = new GameObject("BattleDataManager");
            go.AddComponent<BattleDataManager>();
        }
        BattleDataManager.Instance.SetBattleMode(BattleMode.Tower);

        if (SaveAndLoadSystem.Instance != null)
            SaveAndLoadSystem.Instance.Save();

        StartCoroutine(LoadBattleScene());
    }

    /// <summary>
    /// Phát thưởng ngay khi thắng trận tower + hiện popup.
    /// Được gọi từ TowerTurnSystem sau khi thắng.
    /// </summary>
    public static void GrantAndShowReward(int floorLevel)
    {
        GetFloorReward(floorLevel,
            out int gold, out int gem, out int marshmallowCount, out float marshmallowChance,
            out float commonChance, out float uncommonChance, out float rareChance,
            out float superRareChance, out float ultraRareChance, out float legendaryChance, out float mythicChance);

        string msg = $"FLOOR {floorLevel} — VICTORY!\n";

        // ── Gold & Gem (100% guaranteed) — nhân hệ số remote `reward_mult_tower` ──
        gold = RemoteBalance.ScaleReward(gold, RemoteBalance.Reward.tower);
        gem = RemoteBalance.ScaleReward(gem, RemoteBalance.Reward.tower);
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, gold);
            CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, gem);
        }
        msg += $"+{gold} Gold\n+{gem} Gem\n";

        // ── Roll Marshmallow Ball (S) ──
        if (ResourceManager.Instance != null && Random.Range(0f, 1f) < marshmallowChance)
        {
            ResourceManager.Instance.AddResource(ResourceType.Marshmallow, marshmallowCount);
            msg += $"+{marshmallowCount} Marshmallow Ball (S)\n";
        }

        // ── Roll Slime Reward (từ hiếm nhất → phổ thông nhất) ──
        if (SlimeGen.Instance != null && BreedingManager.Instance != null)
        {
            float roll = Random.Range(0f, 1f);
            Slime newSlime = null;
            string slimeMsg = null;

            if (mythicChance > 0f && roll < mythicChance)
            {
                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_Mythic", Rarity.Mythic);
                slimeMsg = "Mythic Slime";
            }
            else if (legendaryChance > 0f && roll < legendaryChance)
            {
                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_Legendary", Rarity.Legendary);
                slimeMsg = "Legendary Slime";
            }
            else if (ultraRareChance > 0f && roll < ultraRareChance)
            {
                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_UltraRare", Rarity.UltraRare);
                slimeMsg = "Ultra Rare Slime";
            }
            else if (superRareChance > 0f && roll < superRareChance)
            {
                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_SuperRare", Rarity.SuperRare);
                slimeMsg = "Super Rare Slime";
            }
            else if (rareChance > 0f && roll < rareChance)
            {
                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_Rare", Rarity.Rare);
                slimeMsg = "Rare Slime";
            }
            else if (uncommonChance > 0f && roll < uncommonChance)
            {
                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_Uncommon", Rarity.Uncommon);
                slimeMsg = "Uncommon Slime";
            }
            else if (commonChance > 0f && roll < commonChance)
            {
                newSlime = SlimeGen.Instance.GenerateSlimeOfRarity("Slime_Common", Rarity.Common);
                slimeMsg = "Common Slime";
            }

            if (newSlime != null)
            {
                BreedingManager.Instance.GetAllSlimes().Add(newSlime);
                msg += $"+1 {slimeMsg}!\n";
            }
        }

        // ── Hiện popup phần thưởng (tìm instance trong scene) ──
        var ui = FindFirstObjectByType<TowerUIManager>();
        if (ui != null)
        {
            ui.ShowRewardPopup(msg.Trim());
        }
        else
        {
            Debug.Log($"[TowerReward] {msg}");
        }
    }

    public void OnLockedFloorClicked(int floorNumber)
    {
        ShowWarning($"Tầng {floorNumber} chưa mở khóa! Hãy hoàn thành các tầng trước.");
    }

    // ── Private helpers ───────────────────────────────────────────────

    private void UpdateHeader()
    {
        int total   = towerDatabase.floors?.Count ?? 0;
        int current = towerDatabase.currentFloor;
        int highest = towerDatabase.highestFloorReached;

        if (currentFloorText != null)
            currentFloorText.text = $"Current floor: {(current == 0 ? 1 : current)} / {total}";
        if (highestFloorText != null)
            highestFloorText.text = $"Highest Reached: Floor {highest}";
    }

    private void ApplyGlobalClusterSprites(TowerFloorItem item)
    {
        if (item == null) return;

        if (globalClusterSprites != null && HasAnySprite(globalClusterSprites))
        {
            if (!HasAnySprite(item.clusterStepSprites))
                item.clusterStepSprites = globalClusterSprites;
        }

        if (item.activeStarSprite == null && globalActiveStarSprite != null)
            item.activeStarSprite = globalActiveStarSprite;

        if (item.inactiveStarSprite == null && globalInactiveStarSprite != null)
            item.inactiveStarSprite = globalInactiveStarSprite;
    }

    private bool HasAnySprite(Sprite[] arr)
    {
        if (arr == null) return false;
        foreach (var s in arr) if (s != null) return true;
        return false;
    }

    private void RebuildFloorList()
    {
        if (towerDatabase == null) return;

        int requiredFloors = Mathf.Max(30, mapNodes != null ? mapNodes.Count : 30);
        towerDatabase.EnsureFloorCount(requiredFloors);

        if (towerDatabase.floors == null) return;

        int currentFloor = Mathf.Max(1, towerDatabase.currentFloor);
        int totalFloors = towerDatabase.floors.Count;

        // Container chứa Nút
        Transform parentContainer = mapScrollRect != null ? mapScrollRect.content : null;

        if ((mapNodes == null || mapNodes.Count == 0) && parentContainer != null)
        {
            mapNodes = parentContainer.GetComponentsInChildren<TowerFloorItem>(true).ToList();
        }

        int preplacedCount = mapNodes != null ? mapNodes.Count : 0;

        // Nếu làm sẵn ít nút hơn tổng số tầng, tự động sinh thêm các nút còn lại nối tiếp nút cuối cùng
        if (preplacedCount < totalFloors && floorItemPrefab != null && parentContainer != null)
        {
            float lastX = startMarginX;
            if (preplacedCount > 0 && mapNodes[preplacedCount - 1] != null)
            {
                RectTransform lastRT = mapNodes[preplacedCount - 1].GetComponent<RectTransform>();
                if (lastRT != null)
                {
                    lastX = lastRT.anchoredPosition.x;
                }
            }

            for (int i = preplacedCount; i < totalFloors; i++)
            {
                var go = Instantiate(floorItemPrefab, parentContainer);
                go.name = $"FloorNode_{i + 1}";
                go.SetActive(true);

                var item = go.GetComponent<TowerFloorItem>();
                if (item == null) item = go.AddComponent<TowerFloorItem>();

                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot     = new Vector2(0.5f, 0.5f);

                    float posX = lastX + ((i - preplacedCount + 1) * nodeHorizontalSpacing);
                    float posY = (i % 2 == 0) ? nodeZigZagAmplitude : -nodeZigZagAmplitude;

                    rt.anchoredPosition3D = new Vector3(posX, posY, 0f);
                }

                mapNodes.Add(item);
            }
        }

        if (mapNodes == null || mapNodes.Count == 0)
        {
            Debug.LogWarning("TowerUIManager: Chưa có nút TowerFloorItem nào!");
            return;
        }

        float maxX = 0f;

        // Cài đặt thông số & Hiển thị cho từng Nút
        for (int i = 0; i < mapNodes.Count; i++)
        {
            var item = mapNodes[i];
            if (item == null) continue;

            if (i < totalFloors)
            {
                item.gameObject.SetActive(true);

                RectTransform rt = item.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Chỉ sắp xếp vị trí tự động nếu HOÀN TOÀN KHÔNG CÓ NÚT NÀO ĐẶT SẴN
                    if (preplacedCount == 0)
                    {
                        rt.anchorMin = new Vector2(0f, 0.5f);
                        rt.anchorMax = new Vector2(0f, 0.5f);
                        rt.pivot     = new Vector2(0.5f, 0.5f);

                        float posX = startMarginX + (i * nodeHorizontalSpacing);
                        float posY = (i % 2 == 0) ? nodeZigZagAmplitude : -nodeZigZagAmplitude;

                        rt.anchoredPosition3D = new Vector3(posX, posY, 0f);
                    }

                    float rightEdge = rt.anchoredPosition.x + (rt.rect.width * 0.5f);
                    if (rightEdge > maxX) maxX = rightEdge;
                }

                ApplyGlobalClusterSprites(item);
                var floor = towerDatabase.floors[i];
                bool isCurrent = (floor.floorNumber == currentFloor);
                item.Setup(floor, isCurrent, this);
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }

        if (parentContainer != null)
        {
            RectTransform contentRT = parentContainer as RectTransform;
            if (contentRT != null)
            {
                float targetWidth = 0f;
                var bgImage = parentContainer.GetComponentInChildren<Image>();
                if (bgImage != null && bgImage.gameObject != parentContainer.gameObject)
                {
                    RectTransform bgRT = bgImage.rectTransform;
                    if (bgRT != null)
                    {
                        targetWidth = Mathf.Max(bgRT.rect.width, bgRT.sizeDelta.x);
                    }
                }

                if (targetWidth <= 0f || maxX > targetWidth)
                {
                    targetWidth = maxX + 300f;
                }

                if (targetWidth > 0f)
                {
                    contentRT.sizeDelta = new Vector2(targetWidth, contentRT.sizeDelta.y);
                }
            }
        }
    }

    private IEnumerator ScrollToCurrentFloorNextFrame()
    {
        yield return new WaitForEndOfFrame();
        ScrollToCurrentFloor();
    }

    public void ScrollToCurrentFloor()
    {
        if (mapScrollRect == null || towerDatabase == null || mapNodes == null || mapNodes.Count == 0) return;

        int total = towerDatabase.floors != null ? towerDatabase.floors.Count : mapNodes.Count;
        if (total <= 0) return;

        int current = Mathf.Clamp(towerDatabase.currentFloor, 1, total);
        int index = current - 1;

        if (index >= 0 && index < mapNodes.Count && mapNodes[index] != null)
        {
            RectTransform nodeRT = mapNodes[index].GetComponent<RectTransform>();
            RectTransform contentRT = mapScrollRect.content;
            RectTransform viewportRT = mapScrollRect.viewport != null ? mapScrollRect.viewport : (mapScrollRect.transform as RectTransform);

            if (nodeRT != null && contentRT != null && viewportRT != null)
            {
                float nodeX = nodeRT.anchoredPosition.x;
                float viewportWidth = viewportRT.rect.width;
                float contentWidth = contentRT.rect.width;

                float maxScrollX = contentWidth - viewportWidth;
                if (maxScrollX > 0f)
                {
                    float targetContentX = nodeX - (viewportWidth * 0.5f);
                    float normalizedPos = Mathf.Clamp01(targetContentX / maxScrollX);
                    mapScrollRect.horizontalNormalizedPosition = normalizedPos;
                }
                else
                {
                    mapScrollRect.horizontalNormalizedPosition = 0f;
                }
            }
        }
        else
        {
            mapScrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    public void ShowRewardPopup(string message)
    {
        if (rewardPopup == null) return;
        if (rewardPopupText != null) rewardPopupText.text = message;
        rewardPopup.SetActive(true);
    }

    private void HideRewardPopup()
    {
        if (rewardPopup != null) rewardPopup.SetActive(false);
    }

    /// <summary>
    /// Bảng phần thưởng Tower Mode đầy đủ 30 tầng.
    /// </summary>
    public static void GetFloorReward(int level,
        out int gold, out int gem, out int marshmallowCount, out float marshmallowChance,
        out float commonChance, out float uncommonChance, out float rareChance,
        out float superRareChance, out float ultraRareChance, out float legendaryChance, out float mythicChance)
    {
        gold = 50; gem = 1; marshmallowCount = 1; marshmallowChance = 0f;
        commonChance = 0f; uncommonChance = 0f; rareChance = 0f;
        superRareChance = 0f; ultraRareChance = 0f; legendaryChance = 0f; mythicChance = 0f;

        switch (level)
        {
            // ── Khu vực 1: Tầng 1-5 ──
            case 1:  gold=50;   gem=1;  marshmallowCount=1; marshmallowChance=0.10f; commonChance=0.10f; break;
            case 2:  gold=70;   gem=1;  marshmallowCount=1; marshmallowChance=0.12f; commonChance=0.15f; uncommonChance=0.03f; break;
            case 3:  gold=100;  gem=2;  marshmallowCount=1; marshmallowChance=0.15f; commonChance=0.20f; uncommonChance=0.05f; rareChance=0.01f; break;
            case 4:  gold=140;  gem=2;  marshmallowCount=1; marshmallowChance=0.18f; commonChance=0.25f; uncommonChance=0.08f; rareChance=0.03f; break;
            case 5:  gold=300;  gem=5;  marshmallowCount=1; marshmallowChance=0.30f; commonChance=0.30f; uncommonChance=0.15f; rareChance=0.08f; break;

            // ── Khu vực 2: Tầng 6-10 ──
            case 6:  gold=90;   gem=2;  marshmallowCount=1; marshmallowChance=0.20f; commonChance=0.40f; uncommonChance=0.15f; rareChance=0.05f; break;
            case 7:  gold=110;  gem=2;  marshmallowCount=1; marshmallowChance=0.22f; commonChance=0.45f; uncommonChance=0.18f; rareChance=0.07f; break;
            case 8:  gold=140;  gem=3;  marshmallowCount=1; marshmallowChance=0.25f; commonChance=0.50f; uncommonChance=0.20f; rareChance=0.08f; superRareChance=0.01f; break;
            case 9:  gold=180;  gem=3;  marshmallowCount=1; marshmallowChance=0.28f; commonChance=0.55f; uncommonChance=0.25f; rareChance=0.10f; superRareChance=0.02f; break;
            case 10: gold=450;  gem=8;  marshmallowCount=1; marshmallowChance=0.45f; commonChance=0.60f; uncommonChance=0.30f; rareChance=0.15f; superRareChance=0.05f; break;

            // ── Khu vực 3: Tầng 11-15 ──
            case 11: gold=220;  gem=4;  marshmallowCount=1; marshmallowChance=0.30f; uncommonChance=0.40f; rareChance=0.18f; superRareChance=0.08f; ultraRareChance=0.02f; break;
            case 12: gold=260;  gem=4;  marshmallowCount=1; marshmallowChance=0.33f; uncommonChance=0.45f; rareChance=0.22f; superRareChance=0.10f; ultraRareChance=0.03f; break;
            case 13: gold=310;  gem=5;  marshmallowCount=1; marshmallowChance=0.36f; uncommonChance=0.50f; rareChance=0.25f; superRareChance=0.12f; ultraRareChance=0.04f; break;
            case 14: gold=360;  gem=5;  marshmallowCount=1; marshmallowChance=0.40f; uncommonChance=0.55f; rareChance=0.30f; superRareChance=0.15f; ultraRareChance=0.05f; break;
            case 15: gold=700;  gem=12; marshmallowCount=2; marshmallowChance=0.50f; uncommonChance=0.60f; rareChance=0.40f; superRareChance=0.20f; ultraRareChance=0.10f; break;

            // ── Khu vực 4: Tầng 16-20 ──
            case 16: gold=420;  gem=6;  marshmallowCount=2; marshmallowChance=0.35f; rareChance=0.45f; superRareChance=0.22f; ultraRareChance=0.10f; break;
            case 17: gold=470;  gem=6;  marshmallowCount=2; marshmallowChance=0.38f; rareChance=0.50f; superRareChance=0.28f; ultraRareChance=0.12f; legendaryChance=0.01f; break;
            case 18: gold=530;  gem=7;  marshmallowCount=2; marshmallowChance=0.42f; rareChance=0.55f; superRareChance=0.32f; ultraRareChance=0.15f; legendaryChance=0.02f; break;
            case 19: gold=600;  gem=8;  marshmallowCount=2; marshmallowChance=0.45f; rareChance=0.60f; superRareChance=0.35f; ultraRareChance=0.18f; legendaryChance=0.03f; break;
            case 20: gold=1000; gem=20; marshmallowCount=3; marshmallowChance=0.60f; rareChance=0.70f; superRareChance=0.45f; ultraRareChance=0.25f; legendaryChance=0.08f; break;

            // ── Khu vực 5: Tầng 21-25 ──
            case 21: gold=700;  gem=9;  marshmallowCount=2; marshmallowChance=0.45f; superRareChance=0.55f; ultraRareChance=0.25f; legendaryChance=0.05f; break;
            case 22: gold=800;  gem=10; marshmallowCount=2; marshmallowChance=0.50f; superRareChance=0.60f; ultraRareChance=0.30f; legendaryChance=0.08f; break;
            case 23: gold=900;  gem=12; marshmallowCount=2; marshmallowChance=0.55f; superRareChance=0.65f; ultraRareChance=0.35f; legendaryChance=0.10f; break;
            case 24: gold=1000; gem=14; marshmallowCount=3; marshmallowChance=0.60f; superRareChance=0.70f; ultraRareChance=0.40f; legendaryChance=0.12f; break;
            case 25: gold=1500; gem=30; marshmallowCount=4; marshmallowChance=0.70f; superRareChance=0.80f; ultraRareChance=0.50f; legendaryChance=0.15f; break;

            // ── Khu vực 6: Tầng 26-30 ──
            case 26: gold=1200; gem=15; marshmallowCount=3; marshmallowChance=0.65f; superRareChance=0.75f; ultraRareChance=0.45f; legendaryChance=0.15f; break;
            case 27: gold=1350; gem=16; marshmallowCount=3; marshmallowChance=0.70f; superRareChance=0.80f; ultraRareChance=0.50f; legendaryChance=0.18f; break;
            case 28: gold=1500; gem=18; marshmallowCount=4; marshmallowChance=0.75f; superRareChance=0.85f; ultraRareChance=0.55f; legendaryChance=0.20f; break;
            case 29: gold=1700; gem=20; marshmallowCount=4; marshmallowChance=0.80f; superRareChance=0.90f; ultraRareChance=0.60f; legendaryChance=0.25f; break;
            case 30: gold=3000; gem=50; marshmallowCount=5; marshmallowChance=1.00f; superRareChance=1.00f; ultraRareChance=0.70f; legendaryChance=0.30f; mythicChance=0.01f; break;
        }
    }

    private void ShowWarning(string message = null)
    {
        if (warningText == null) return;
        StopCoroutine(nameof(HideWarningAfterDelay));

        if (warningTextLabel != null && !string.IsNullOrEmpty(message))
        {
            warningTextLabel.text = message;
        }

        warningText.SetActive(true);
        StartCoroutine(nameof(HideWarningAfterDelay));
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return WarningDelay;
        if (warningText != null)
            warningText.SetActive(false);
    }

    private IEnumerator LoadBattleScene()
    {
        yield return SceneLoader.LoadSceneWithLoadingCoroutine("TurnBaseGame");
    }
}
