using UnityEngine;
using UnityEngine.UI;
using System;

#if TMP_PRESENT || UNITY_2018_1_OR_NEWER
using TMPro;
#endif

public class QuestCardItemUI : MonoBehaviour
{
    [Header("Texts")]
    [Tooltip("Tiêu đề nhiệm vụ / thành tựu (hỗ trợ TMP hoặc Text thường)")]
    public GameObject titleObject;
    [Tooltip("Mô tả chi tiết mục tiêu (hỗ trợ TMP hoặc Text thường)")]
    public GameObject descriptionObject;
    [Tooltip("Tiến độ dạng chữ vd: 3 / 10 (30%)")]
    public GameObject progressTextObject;
    [Tooltip("Số lượng thưởng vd: +500 Coins hoặc +15 Gems")]
    public GameObject rewardAmountObject;

    [Header("Progress Visual")]
    public Slider progressBar;
    public Image progressFillImage;
    public Gradient progressGradient;

    [Header("Reward Visual")]
    public Image rewardIcon;
    public Sprite coinRewardSprite; // Icon Vàng mặc định
    public Sprite gemRewardSprite;  // Icon Đá Quý mặc định

    [Header("Action Button")]
    public Button actionButton;
    public GameObject actionButtonTextObject;

    [Header("Action Button Sprites (Tùy chọn - nếu muốn dùng hình nút riêng)")]
    public Sprite inProgressButtonSprite;
    public Sprite readyClaimButtonSprite;
    public Sprite claimedButtonSprite;

    [Header("Action Button Colors (Dùng khi không gán Sprite riêng)")]
    public Color inProgressButtonColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    public Color readyClaimButtonColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color claimedButtonColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);

    [Header("State Overlays (Tùy chọn - không bắt buộc)")]
    public GameObject readyGlowEffect;
    public GameObject completedCheckmark;
    public GameObject lockOverlay;

    private Action _onClaimCallback;

    private void Awake()
    {
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }
    }

    /// <summary>
    /// Thiết lập hiển thị đầy đủ cho một Card
    /// </summary>
    public void Setup(
        string title,
        string description,
        long currentProgress,
        long targetProgress,
        Sprite rewardSprite,
        int rewardAmount,
        string currencySuffix,
        bool isClaimed,
        Action onClaim)
    {
        _onClaimCallback = onClaim;

        // 1. Title & Description
        SetText(titleObject, title);
        SetText(descriptionObject, description);

        // 2. Progress Slider & Text
        float pct = targetProgress > 0 ? Mathf.Clamp01((float)currentProgress / targetProgress) : 1f;
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = pct;
        }

        if (progressFillImage != null && progressGradient != null)
        {
            progressFillImage.color = progressGradient.Evaluate(pct);
        }

        string pctString = $"({Mathf.RoundToInt(pct * 100f)}%)";
        string progressStr = $"{currentProgress:N0} / {targetProgress:N0}  {pctString}";
        SetText(progressTextObject, progressStr);

        // 3. Reward
        if (rewardIcon != null)
        {
            Sprite finalSprite = rewardSprite;
            if (finalSprite == null)
            {
                if (!string.IsNullOrEmpty(currencySuffix) && currencySuffix.ToLower().Contains("gem"))
                {
                    finalSprite = gemRewardSprite;
                }
                else
                {
                    finalSprite = coinRewardSprite;
                }
            }

            rewardIcon.sprite = finalSprite;
            rewardIcon.preserveAspect = true;
            rewardIcon.gameObject.SetActive(finalSprite != null);
        }

        string rewardStr = $"+{rewardAmount:N0} {currencySuffix}".Trim();
        SetText(rewardAmountObject, rewardStr);

        // 4. Action Button State
        bool isCompleted = currentProgress >= targetProgress;
        Image btnImg = actionButton != null ? actionButton.image : null;

        if (isClaimed)
        {
            // Đã nhận thưởng
            SetText(actionButtonTextObject, "CLAIMED");
            if (actionButton != null) actionButton.interactable = false;

            if (btnImg != null)
            {
                if (claimedButtonSprite != null)
                {
                    btnImg.sprite = claimedButtonSprite;
                    btnImg.color = Color.white;
                }
                else
                {
                    btnImg.color = claimedButtonColor;
                }
            }

            if (readyGlowEffect != null) readyGlowEffect.SetActive(false);
            if (completedCheckmark != null) completedCheckmark.SetActive(true);
            if (lockOverlay != null) lockOverlay.SetActive(false);
        }
        else if (isCompleted)
        {
            // Sẵn sàng nhận thưởng (Claim)
            SetText(actionButtonTextObject, "CLAIM");
            if (actionButton != null) actionButton.interactable = true;

            if (btnImg != null)
            {
                if (readyClaimButtonSprite != null)
                {
                    btnImg.sprite = readyClaimButtonSprite;
                    btnImg.color = Color.white;
                }
                else
                {
                    btnImg.color = readyClaimButtonColor;
                }
            }

            if (readyGlowEffect != null) readyGlowEffect.SetActive(true);
            if (completedCheckmark != null) completedCheckmark.SetActive(false);
            if (lockOverlay != null) lockOverlay.SetActive(false);
        }
        else
        {
            // Đang thực hiện (In Progress)
            SetText(actionButtonTextObject, "IN PROGRESS");
            if (actionButton != null) actionButton.interactable = false;

            if (btnImg != null)
            {
                if (inProgressButtonSprite != null)
                {
                    btnImg.sprite = inProgressButtonSprite;
                    btnImg.color = Color.white;
                }
                else
                {
                    btnImg.color = inProgressButtonColor;
                }
            }

            if (readyGlowEffect != null) readyGlowEffect.SetActive(false);
            if (completedCheckmark != null) completedCheckmark.SetActive(false);
            if (lockOverlay != null) lockOverlay.SetActive(false);
        }
    }

    private void OnActionButtonClicked()
    {
        _onClaimCallback?.Invoke();
    }

    /// <summary>
    /// Hàm trợ giúp gán text đa năng (hỗ trợ cả Text thường và TMP_Text)
    /// </summary>
    private void SetText(GameObject obj, string text)
    {
        if (obj == null) return;

#if TMP_PRESENT || UNITY_2018_1_OR_NEWER
        var tmp = obj.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
            return;
        }
#endif
        var legacyText = obj.GetComponent<Text>();
        if (legacyText != null)
        {
            legacyText.text = text;
        }
    }
}
