using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component cho một hàng floor trong danh sách Tower.
/// Gắn vào prefab FloorItem và kéo các Text/Button vào trong Inspector.
/// </summary>
public class TowerFloorItem : MonoBehaviour
{
    [Header("Labels")]
    public Text floorNumberText;   // "Tầng 1"
    public Text floorNameText;     // Tên boss / tên màn
    public Text statusText;        // Locked / Hiện tại / Hoàn thành! / Đã nhận
    public Text rewardText;        // "50 Coins  2 Gems"

    [Header("Buttons")]
    public Button battleButton;    // Hiện khi đây là floor hiện tại (chưa thắng) hoặc đã claim (replay)
    public Text battleButtonLabel; // (Tuỳ chọn) Text bên trong battleButton để đổi "Attack" / "Replay"
    public Button claimButton;     // Hiện khi completed && !claimed

    [Header("Visual State")]
    public Image background;
    public Color colorLocked   = new Color(0.4f, 0.4f, 0.4f);
    public Color colorCurrent  = new Color(0.2f, 0.6f, 1f);
    public Color colorComplete = new Color(0.2f, 0.8f, 0.4f);
    public Color colorClaimed  = new Color(0.6f, 0.6f, 0.6f);

    private TowerSlimeBosses.TowerFloor floorData;
    private TowerUIManager uiManager;

    public void Setup(TowerSlimeBosses.TowerFloor floor, bool isCurrent, TowerUIManager manager)
    {
        floorData = floor;
        uiManager = manager;

        if (floorNumberText != null) floorNumberText.text = $"Floor {floor.floorNumber}";
        if (floorNameText   != null) floorNameText.text   = floor.floorName;

        // Reward text
        string rewardStr = "";
        if (floor.rewardCoins > 0) rewardStr += $"{floor.rewardCoins} Coins  ";
        if (floor.rewardGems  > 0) rewardStr += $"{floor.rewardGems} Gems";
        if (rewardStr == "") rewardStr = "—";
        if (rewardText != null) rewardText.text = rewardStr.Trim();

        // Buttons
        bool canBattle = isCurrent && !floor.completed;
        bool canReplay = floor.claimed;
        bool canClaim  = floor.completed && !floor.claimed;

        if (battleButton != null)
        {
            battleButton.gameObject.SetActive(canBattle || canReplay);
            battleButton.onClick.RemoveAllListeners();
            if (battleButtonLabel != null)
                battleButtonLabel.text = canReplay ? "Replay" : "Attack";
            if (canBattle)
                battleButton.onClick.AddListener(() => uiManager.OnStartBattle());
            else if (canReplay)
                battleButton.onClick.AddListener(() => uiManager.OnReplayFloor(floor.floorNumber));
        }

        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(canClaim);
            claimButton.onClick.RemoveAllListeners();
            if (canClaim)
                claimButton.onClick.AddListener(() => uiManager.OnClaimFloor(floor.floorNumber));
        }

        // Status + background color
        ApplyVisualState(floor, isCurrent);
    }

    private void ApplyVisualState(TowerSlimeBosses.TowerFloor floor, bool isCurrent)
    {
        string status;
        Color bg;

        if (floor.claimed)
        {
            status = "✓ Claimed (Replay available)";
            bg = colorClaimed;
        }
        else if (floor.completed)
        {
            status = "★ Completed!";
            bg = colorComplete;
        }
        else if (isCurrent)
        {
            status = "⚔ Current";
            bg = colorCurrent;
        }
        else
        {
            status = "🔒 Locked";
            bg = colorLocked;
        }

        if (statusText  != null) statusText.text  = status;
        if (background  != null) background.color = bg;
    }
}
