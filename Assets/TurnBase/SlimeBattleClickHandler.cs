using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Component tự động gắn lên tất cả Slime trên sàn đấu (Player Slimes & Boss/Enemy Slimes).
/// Tự động sinh Collider2D và RaycastTarget để nhận diện click/tap của người chơi.
/// Khi người chơi chạm vào Slime:
/// 1. Chọn Slime đó làm Mục tiêu (Target).
/// 2. Hiển thị thông tin Slime lên ô chữ dùng chung (BattleInfoDisplayUI) và tự động tắt thông tin Skill.
/// </summary>
public class SlimeBattleClickHandler : MonoBehaviour, IPointerClickHandler
{
    private SlimeStats slimeStats;
    private TurnSystem turnSystem;

    private void Awake()
    {
        EnsureClickable();
    }

    private void Start()
    {
        EnsureClickable();
    }

    public void Init(TurnSystem sys, SlimeStats stats)
    {
        turnSystem = sys;
        slimeStats = stats;
        EnsureClickable();
    }

    public void EnsureClickable()
    {
        if (slimeStats == null) slimeStats = GetComponent<SlimeStats>();
        if (slimeStats == null) slimeStats = GetComponentInParent<SlimeStats>();
        if (turnSystem == null) turnSystem = Object.FindFirstObjectByType<TurnSystem>();

        // 1. Nếu Slime ở World Space: Tự tạo BoxCollider2D lớn bao trọn Slime
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.5f, 2.5f);
        }

        // 2. Nếu Slime có SpriteRenderer: Bật RaycastTarget hoặc Hitbox
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = true;
        }

        // 3. Nếu Slime ở Canvas UI: Bật raycastTarget cho Image
        var img = GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
        }
    }

    private void OnMouseDown()
    {
        HandleClick();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    private void HandleClick()
    {
        EnsureClickable();

        Debug.Log($"[SlimeBattleClickHandler] Chạm vào Slime: {gameObject.name}");

        // 1. Chọn mục tiêu
        if (turnSystem != null)
        {
            turnSystem.SelectTarget(gameObject);
        }

        // 2. Hiển thị thông tin Slime lên ô chữ dùng chung
        if (slimeStats != null)
        {
            if (BattleInfoDisplayUI.Instance != null)
            {
                BattleInfoDisplayUI.Instance.ShowSlimeInfo(slimeStats);
            }
            else if (SlimeStatsInspectorUI.Instance != null)
            {
                SlimeStatsInspectorUI.Instance.InspectSlime(slimeStats);
            }
        }
    }
}
