using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// </summary>
public class BattleBackgroundManager : MonoBehaviour
{
    [Header("Background Display")]
    public SpriteRenderer worldSpriteRenderer;
    public Image canvasBackgroundImage;

    [Header("6 Backgrounds")]
    public Sprite[] towerMaps = new Sprite[6];

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
