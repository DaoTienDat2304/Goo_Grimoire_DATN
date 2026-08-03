using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        //  Tự tạo BoxCollider2D lớn bao trọn Slime
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.5f, 2.5f);
        }

        // Bật RaycastTarget hoặc Hitbox
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = true;
        }

        //  Bật raycastTarget cho Image
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

        // Chỉ chọn mục tiêu nếu là Enemy
        if (slimeStats != null && slimeStats.isEnemy && turnSystem != null)
        {
            turnSystem.SelectTarget(gameObject);
        }

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
