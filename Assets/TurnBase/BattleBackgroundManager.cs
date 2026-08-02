using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý bức ảnh Nền/Bối cảnh Trận đấu (Battle Background) trong scene TurnBaseGame.
/// Tự động đổi Sprite Nền tương ứng theo Môi trường 1 - 6 của Tower Mode hoặc Farm Mode / Adventure Mode.
/// </summary>
public class BattleBackgroundManager : MonoBehaviour
{
    [Header("Background Displays")]
    public SpriteRenderer worldSpriteRenderer; // Nền dạng 2D SpriteRenderer trong World Space (nếu dùng 2D Camera)
    public Image canvasBackgroundImage;        // Nền dạng UI Image trên Canvas (nếu dùng UI Canvas)

    [Header("Environment Sprites theo Môi Trường 1 -> 6")]
    public Sprite environment1Forest;         // Màn 1 - 5: Rừng Cây
    public Sprite environment2Cave;           // Màn 6 - 10: Hang Động
    public Sprite environment3Ice;            // Màn 11 - 15: Tuyết Băng
    public Sprite environment4Volcano;        // Màn 16 - 20: Núi Lửa
    public Sprite environment5Castle;         // Màn 21 - 25: Lâu Đài
    public Sprite environment6Celestial;      // Màn 26 - 30: Thượng Giới

    [Header("Mode Background Sprites")]
    public Sprite farmBackground;             // Nền chế độ Farm
    public Sprite adventureBackground;        // Nền chế độ Thám Hiểm

    private void Start()
    {
        ApplyBattleBackground();
    }

    /// <summary>
    /// Tự động kiểm tra chế độ chơi & số tầng Tower để gán Sprite Bối cảnh phù hợp
    /// </summary>
    public void ApplyBattleBackground()
    {
        Sprite targetBackground = GetTargetBackgroundSprite();
        if (targetBackground == null) return;

        if (worldSpriteRenderer != null)
        {
            worldSpriteRenderer.sprite = targetBackground;
        }

        if (canvasBackgroundImage != null)
        {
            canvasBackgroundImage.sprite = targetBackground;
        }

        Debug.Log($"[BattleBackgroundManager] Đã cập nhật ảnh nền bối cảnh trận đấu: {targetBackground.name}");
    }

    private Sprite GetTargetBackgroundSprite()
    {
        var manager = BattleDataManager.Instance;
        if (manager == null) return environment1Forest;

        if (manager.IsFarmMode())
        {
            return farmBackground != null ? farmBackground : environment1Forest;
        }
        else if (manager.IsTowerMode())
        {
            int floorNumber = 1;
            var towerDB = Resources.Load<TowerSlimeBosses>("TowerSlimeBosses");
            if (towerDB != null)
            {
                floorNumber = towerDB.replayFloor > 0 ? towerDB.replayFloor : Mathf.Max(1, towerDB.currentFloor);
            }

            int envIndex = (floorNumber - 1) / 5; // 0, 1, 2, 3, 4, 5

            return envIndex switch
            {
                0 => environment1Forest != null ? environment1Forest : environment1Forest,
                1 => environment2Cave != null ? environment2Cave : environment1Forest,
                2 => environment3Ice != null ? environment3Ice : environment1Forest,
                3 => environment4Volcano != null ? environment4Volcano : environment1Forest,
                4 => environment5Castle != null ? environment5Castle : environment1Forest,
                5 => environment6Celestial != null ? environment6Celestial : environment1Forest,
                _ => environment1Forest
            };
        }
        else
        {
            return adventureBackground != null ? adventureBackground : environment1Forest;
        }
    }
}
