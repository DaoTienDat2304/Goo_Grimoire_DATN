using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý ảnh Nền Trận Đấu cho Tower Mode.
/// Tự động đổi nền theo tầng hiện tại: 6 map cho 30 tầng (mỗi 5 tầng = 1 map).
/// </summary>
public class BattleBackgroundManager : MonoBehaviour
{
    [Header("Background Display")]
    public SpriteRenderer worldSpriteRenderer; // Nền 2D World Space
    public Image canvasBackgroundImage;        // Nền UI Canvas

    [Header("6 Map Nền (Kéo 6 ảnh map vào đây)")]
    public Sprite[] towerMaps = new Sprite[6];
    // [0] = Tầng 1-5, [1] = Tầng 6-10, [2] = Tầng 11-15
    // [3] = Tầng 16-20, [4] = Tầng 21-25, [5] = Tầng 26-30

    private void Start()
    {
        ApplyBattleBackground();
    }

    public void ApplyBattleBackground()
    {
        int floor = GetCurrentFloor();
        int mapIndex = Mathf.Clamp((floor - 1) / 5, 0, towerMaps.Length - 1);

        if (towerMaps.Length == 0 || towerMaps[mapIndex] == null) return;

        Sprite bg = towerMaps[mapIndex];

        if (worldSpriteRenderer != null) worldSpriteRenderer.sprite = bg;
        if (canvasBackgroundImage != null) canvasBackgroundImage.sprite = bg;
    }

    private int GetCurrentFloor()
    {
        var towerDB = Resources.Load<TowerSlimeBosses>("TowerSlimeBosses");
        if (towerDB != null)
        {
            return towerDB.replayFloor > 0 ? towerDB.replayFloor : Mathf.Max(1, towerDB.currentFloor);
        }
        return 1;
    }
}
