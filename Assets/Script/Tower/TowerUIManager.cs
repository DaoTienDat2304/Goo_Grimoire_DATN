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
        yield return new WaitForSeconds(0.2f);
        Refresh();
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

    public void OnClaimFloor(int floorNumber)
    {
        if (towerDatabase == null) return;

        var floor = towerDatabase.GetFloor(floorNumber);
        if (floor == null || !floor.completed || floor.claimed) return;

        floor.claimed = true;

        if (CurrencyManager.Instance != null)
        {
            if (floor.rewardCoins > 0)
                CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, floor.rewardCoins);
            if (floor.rewardGems > 0)
                CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, floor.rewardGems);
        }

        if (floor.rewardTraits != null)
        {
            foreach (var trait in floor.rewardTraits)
            {
                if (trait != null)
                    trait.unlocked = true;
            }
        }

        if (SaveAndLoadSystem.Instance != null)
            SaveAndLoadSystem.Instance.Save();

        ShowRewardPopup(floor);
        Refresh();
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

    private void ShowRewardPopup(TowerSlimeBosses.TowerFloor floor)
    {
        if (rewardPopup == null) return;

        string msg = $"Floor {floor.floorNumber} — {floor.floorName}\n";
        if (floor.rewardCoins > 0) msg += $"+{floor.rewardCoins} Coins\n";
        if (floor.rewardGems  > 0) msg += $"+{floor.rewardGems} Gems\n";
        if (floor.rewardTraits != null)
        {
            foreach (var t in floor.rewardTraits)
                if (t != null) msg += $"New Trait: {t.name}\n";
        }

        if (rewardPopupText != null) rewardPopupText.text = msg.Trim();
        rewardPopup.SetActive(true);
    }

    private void HideRewardPopup()
    {
        if (rewardPopup != null) rewardPopup.SetActive(false);
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
