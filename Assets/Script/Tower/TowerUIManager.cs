using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý panel Tower trong scene firstsave.
/// Kéo TowerSlimeBosses asset + các UI element vào Inspector.
/// </summary>
public class TowerUIManager : MonoBehaviour
{
    [Header("Data")]
    public TowerSlimeBosses towerDatabase;

    [Header("Panel")]
    public GameObject towerPanel;           // Root panel (bật/tắt)
    public Transform floorListContainer;    // ScrollView / Content object
    public GameObject floorItemPrefab;      // Prefab có TowerFloorItem

    [Header("Header Info")]
    public Text headerText;                 // "Tower of Slimes"
    public Text currentFloorText;           // "Tầng hiện tại: 3 / 15"
    public Text highestFloorText;           // "Cao nhất: 2"

    [Header("Warning")]
    public GameObject warningText;          // Hiện khi team chưa có slime

    private static readonly WaitForSeconds WarningDelay = new(3f);

    [Header("Reward Popup (tuỳ chọn)")]
    public GameObject rewardPopup;          // Panel hiện khi claim
    public Text rewardPopupText;            // Nội dung phần thưởng
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
        // Refresh UI sau khi save system load xong (ưu tiên sau frame đầu)
        StartCoroutine(RefreshAfterLoad());
    }

    private IEnumerator RefreshAfterLoad()
    {
        // Đợi SaveAndLoadSystem hoàn tất load
        yield return new WaitForSeconds(0.2f);
        Refresh();

    }

    // ── Public interface ──────────────────────────────────────────────

    public void OpenPanel()
    {
        if (towerPanel != null) towerPanel.SetActive(true);
        Refresh();
    }

    public void ClosePanel()
    {
        if (towerPanel != null) towerPanel.SetActive(false);
    }

    /// <summary>Vẽ lại toàn bộ danh sách floor.</summary>
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

    // ── Gọi từ TowerFloorItem ─────────────────────────────────────────

    /// <summary>Bắt đầu battle với floor hiện tại.</summary>
    public void OnStartBattle()
    {
        if (towerDatabase == null) return;

        // Kiểm tra team phải có ít nhất 1 slime
        var saveSystem = SaveAndLoadSystem.Instance;
        var team = saveSystem != null ? saveSystem.GetTeam() : null;
        if (team == null || team.team == null || team.team.Count == 0)
        {
            Debug.LogWarning("Cần ít nhất 1 slime trong team để vào Tower!");
            ShowWarning();
            return;
        }

        if (warningText != null) warningText.SetActive(false);

        // Đảm bảo currentFloor được đặt đúng khi lần đầu tiên
        if (towerDatabase.currentFloor == 0)
            towerDatabase.currentFloor = 1;

        // TurnSystem tự lấy boss từ towerBosses khi không có boss data
        if (BattleDataManager.Instance == null)
        {
            var go = new GameObject("BattleDataManager");
            go.AddComponent<BattleDataManager>();
        }
        BattleDataManager.Instance.SetBattleMode(BattleMode.Tower);
        SaveAndLoadSystem.Instance?.Save();
        StartCoroutine(LoadBattleScene());
    }

    /// <summary>Chơi lại tầng đã hoàn thành (không nhận thưởng lại).</summary>
    public void OnReplayFloor(int floorNumber)
    {
        if (towerDatabase == null) return;

        var saveSystem = SaveAndLoadSystem.Instance;
        var team = saveSystem != null ? saveSystem.GetTeam() : null;
        if (team == null || team.team == null || team.team.Count == 0)
        {
            ShowWarning();
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
        StartCoroutine(LoadBattleScene());
    }

    /// <summary>Claim reward cho floor đã hoàn thành.</summary>
    public void OnClaimFloor(int floorNumber)
    {
        if (towerDatabase == null) return;

        var floor = towerDatabase.GetFloor(floorNumber);
        if (floor == null || !floor.completed || floor.claimed) return;

        floor.claimed = true;

        // Trao tiền tệ
        if (CurrencyManager.Instance != null)
        {
            if (floor.rewardCoins > 0)
                CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, floor.rewardCoins);
            if (floor.rewardGems > 0)
                CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, floor.rewardGems);
        }

        // Mở khóa trait nếu có
        if (floor.rewardTraits != null)
        {
            foreach (var trait in floor.rewardTraits)
            {
                if (trait != null)
                    trait.unlocked = true;
            }
        }

        // Lưu lại
        if (SaveAndLoadSystem.Instance != null)
            SaveAndLoadSystem.Instance.Save();

        ShowRewardPopup(floor);
        Refresh();

        Debug.Log($"Claimed reward cho Tầng {floorNumber}: {floor.rewardCoins} Coins, {floor.rewardGems} Gems");
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

    private void RebuildFloorList()
    {
        if (floorListContainer == null || floorItemPrefab == null) return;

        // Xóa items cũ
        foreach (Transform child in floorListContainer)
            Destroy(child.gameObject);

        if (towerDatabase.floors == null) return;

        int currentFloor = Mathf.Max(1, towerDatabase.currentFloor);

        foreach (var floor in towerDatabase.floors)
        {
            if (floor == null) continue;

            var go   = Instantiate(floorItemPrefab, floorListContainer);
            var item = go.GetComponent<TowerFloorItem>();
            if (item == null) continue;

            bool isCurrent = (floor.floorNumber == currentFloor);
            item.Setup(floor, isCurrent, this);
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

    private void ShowWarning()
    {
        if (warningText == null) return;
        StopCoroutine(nameof(HideWarningAfterDelay));
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
