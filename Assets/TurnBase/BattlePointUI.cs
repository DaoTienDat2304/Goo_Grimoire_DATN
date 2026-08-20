using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị 5 Viên Ngọc Điểm Chiến Kỹ (Battle Points / SP) trong trận đấu.
/// Tự động đồng bộ với BattleSystemManager và hỗ trợ đổi Sprite / Màu sắc kèm hiệu ứng hoạt họa.
/// </summary>
public class BattlePointUI : MonoBehaviour
{
    [Header("Dãy 5 Viên Ngọc SP (Kéo 5 Image vào đây)")]
    [Tooltip("Danh sách 5 Image đại diện cho 5 điểm chiến kỹ")]
    public Image[] spOrbs = new Image[5];

    [Header("Tuỳ Chọn Sprite (Nếu có 2 ảnh Sáng & Tắt)")]
    [Tooltip("Sprite viên ngọc khi CÓ ĐIỂM (để trống nếu chỉ dùng đổi màu Color)")]
    public Sprite activeOrbSprite;
    [Tooltip("Sprite viên ngọc khi HẾT ĐIỂM (để trống nếu chỉ dùng đổi màu Color)")]
    public Sprite inactiveOrbSprite;

    [Header("Tuỳ Chọn Màu Sắc (Nếu dùng 1 ảnh và đổi màu)")]
    [Tooltip("Màu của viên ngọc khi CÓ ĐIỂM")]
    public Color activeColor = Color.white;
    [Tooltip("Màu của viên ngọc khi HẾT ĐIỂM / TRỐNG")]
    public Color inactiveColor = new Color(0.25f, 0.25f, 0.25f, 0.4f);

    [Header("Hiệu Ứng Hoạt Họa (Animation)")]
    [Tooltip("Bật hiệu ứng phóng to nhẹ khi vừa nhận thêm SP")]
    public bool enablePulseAnimation = true;
    public float pulseScaleMultiplier = 1.3f;
    public float pulseDuration = 0.25f;

    private int _lastPoints = -1;
    private Coroutine[] _pulseCoroutines;

    private void Awake()
    {
        _pulseCoroutines = new Coroutine[spOrbs.Length];
    }

    private void OnEnable()
    {
        if (BattleSystemManager.Instance != null)
        {
            BattleSystemManager.Instance.OnBattlePointsChanged += UpdateOrbs;
        }

        RefreshCurrentPoints();
    }

    private void OnDisable()
    {
        if (BattleSystemManager.Instance != null)
        {
            BattleSystemManager.Instance.OnBattlePointsChanged -= UpdateOrbs;
        }
    }

    private void Start()
    {
        RefreshCurrentPoints();
    }

    /// <summary>
    /// Làm mới toàn bộ 5 viên ngọc dựa theo điểm SP hiện tại từ BattleSystemManager.
    /// </summary>
    public void RefreshCurrentPoints()
    {
        int currentPoints = BattleSystemManager.Instance != null 
            ? BattleSystemManager.Instance.TeamBattlePoints 
            : 3;

        UpdateOrbs(currentPoints);
    }

    /// <summary>
    /// Cập nhật trạng thái sáng/tối cho từng viên ngọc.
    /// </summary>
    /// <param name="currentPoints">Số điểm SP hiện tại (0 - 5)</param>
    public void UpdateOrbs(int currentPoints)
    {
        for (int i = 0; i < spOrbs.Length; i++)
        {
            if (spOrbs[i] == null) continue;

            bool isActive = (i < currentPoints);

            // 1. Cập nhật Sprite (nếu có gán)
            if (activeOrbSprite != null && inactiveOrbSprite != null)
            {
                spOrbs[i].sprite = isActive ? activeOrbSprite : inactiveOrbSprite;
            }

            // 2. Cập nhật Màu sắc
            spOrbs[i].color = isActive ? activeColor : inactiveColor;

            // 3. Hiệu ứng phóng to nhẹ (Pulse) nếu vừa mới được kích hoạt sáng
            if (enablePulseAnimation && isActive && i >= _lastPoints && _lastPoints != -1)
            {
                if (_pulseCoroutines[i] != null) StopCoroutine(_pulseCoroutines[i]);
                _pulseCoroutines[i] = StartCoroutine(PulseOrb(spOrbs[i].transform));
            }
        }

        _lastPoints = currentPoints;
    }

    private IEnumerator PulseOrb(Transform orbTransform)
    {
        if (orbTransform == null) yield break;

        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * pulseScaleMultiplier;

        float halfDuration = pulseDuration * 0.5f;
        float elapsed = 0f;

        // Phóng to lên
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            orbTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;
        // Thu nhỏ lại ban đầu
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            orbTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        orbTransform.localScale = originalScale;
    }

    // ── Context Menu để Test Nhanh Trong Unity Editor ────────────────────────
    [ContextMenu("Test: Add 1 SP")]
    public void TestAddSP()
    {
        if (BattleSystemManager.Instance != null)
            BattleSystemManager.Instance.AddBattlePoints(1);
        else
            UpdateOrbs(Mathf.Clamp(_lastPoints + 1, 0, 5));
    }

    [ContextMenu("Test: Spend 1 SP")]
    public void TestSpendSP()
    {
        if (BattleSystemManager.Instance != null)
            BattleSystemManager.Instance.ConsumeBattlePoints(1);
        else
            UpdateOrbs(Mathf.Clamp(_lastPoints - 1, 0, 5));
    }

    [ContextMenu("Test: Reset to 3 SP")]
    public void TestResetSP()
    {
        if (BattleSystemManager.Instance != null)
            BattleSystemManager.Instance.ResetBattlePoints();
        else
            UpdateOrbs(3);
    }
}
