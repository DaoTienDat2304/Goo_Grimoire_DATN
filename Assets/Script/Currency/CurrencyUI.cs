using UnityEngine;
using UnityEngine.UI;

public class CurrencyUI : MonoBehaviour
{
    [Header("Currency Display - Chọn 1 trong 2 loại")]
    [SerializeField] private Text coinsText;           // Unity UI Text thông thường
    [SerializeField] private Text gemsText;            // Unity UI Text thông thường
    
    [Header("Currency Icons (Optional)")]
    [SerializeField] private Image coinsIcon;
    [SerializeField] private Image gemsIcon;

    /// <summary>Sprite icon coin/gem đang dùng trong HUD — cho UI khác (vd breeding) tái sử dụng.</summary>
    public Sprite CoinSprite => coinsIcon != null ? coinsIcon.sprite : null;
    public Sprite GemSprite => gemsIcon != null ? gemsIcon.sprite : null;
    
    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = false;
    
    [Header("Tower Database (Optional)")]
    [SerializeField] private TowerSlimeBosses towerDatabase;  // Kéo TowerSlimeBosses asset vào đây

    private void Start()
    {
        // Đăng ký event listeners
        CurrencyManager.OnCurrencyChanged += OnCurrencyChanged;
        
        // Cập nhật UI ban đầu
        UpdateAllCurrencyDisplay();
        
        // Kiểm tra và claim reward từ tower nếu có
        CheckAndClaimTowerRewards();
    }

    private void OnDestroy()
    {
        // Hủy đăng ký event listeners
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.OnCurrencyChanged -= OnCurrencyChanged;
        }
    }

    private void OnCurrencyChanged(CurrencyType type, int oldAmount, int newAmount)
    {
        UpdateCurrencyDisplay(type, newAmount);
        
        if (useAnimation)
        {
            PlaySimpleAnimation(type);
        }
    }

    private void UpdateAllCurrencyDisplay()
    {
        if (CurrencyManager.Instance != null)
        {
            UpdateCurrencyDisplay(CurrencyType.Coins, CurrencyManager.Instance.GetCurrency(CurrencyType.Coins));
            Debug.Log("Updating Coins Display: " + CurrencyManager.Instance.GetCurrency(CurrencyType.Coins));
            UpdateCurrencyDisplay(CurrencyType.Gems, CurrencyManager.Instance.GetCurrency(CurrencyType.Gems));
            Debug.Log("Updating Gems Display: " + CurrencyManager.Instance.GetCurrency(CurrencyType.Gems));
        }
    }

    private void UpdateCurrencyDisplay(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Coins:
                if (coinsText != null)
                    coinsText.text = FormatCurrencyAmount(amount);
                break;
                
            case CurrencyType.Gems:
                if (gemsText != null)
                    gemsText.text = FormatCurrencyAmount(amount);
                break;
        }
    }

    private string FormatCurrencyAmount(int amount)
    {
        // Format số tiền cho dễ đọc
        if (amount >= 1000000)
            return (amount / 1000000f).ToString("0.0") + "M";
        else if (amount >= 1000)
            return (amount / 1000f).ToString("0.0") + "K";
        else
            return amount.ToString();
    }

    private void PlaySimpleAnimation(CurrencyType type)
    {
        Text targetText = null;
        
        switch (type)
        {
            case CurrencyType.Coins:
                targetText = coinsText;
                break;
            case CurrencyType.Gems:
                targetText = gemsText;
                break;
        }

        if (targetText != null)
        {
            // Hiệu ứng đơn giản không cần LeanTween
            StartCoroutine(SimpleScaleAnimation(targetText.transform));
        }
    }

    private System.Collections.IEnumerator SimpleScaleAnimation(Transform target)
    {
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        // Scale up
        float time = 0;
        while (time < 0.1f)
        {
            target.localScale = Vector3.Lerp(originalScale, targetScale, time / 0.1f);
            time += Time.deltaTime;
            yield return null;
        }
        
        // Scale down
        time = 0;
        while (time < 0.1f)
        {
            target.localScale = Vector3.Lerp(targetScale, originalScale, time / 0.1f);
            time += Time.deltaTime;
            yield return null;
        }
        
        target.localScale = originalScale;
    }

    // Public methods để các script khác có thể gọi
    public void RefreshDisplay()
    {
        UpdateAllCurrencyDisplay();
    }

    // Method để hiển thị popup khi không đủ tiền
    public void ShowInsufficientCurrencyMessage(CurrencyType type, int required, int current)
    {
        string currencyName = type == CurrencyType.Coins ? "Coins" : "Gems";
        string message = $"Không đủ {currencyName}!\nCần: {FormatCurrencyAmount(required)}\nHiện có: {FormatCurrencyAmount(current)}";
        
        Debug.LogWarning(message);
        
        // Có thể tích hợp với hệ thống notification/popup nếu có
        // NotificationManager.ShowMessage(message);
    }
    
    /// <summary>
    /// Kiểm tra và claim reward từ các màn tower đã hoàn thành
    /// </summary>
    private void CheckAndClaimTowerRewards()
    {
        // Tìm tower database nếu chưa được gán
        if (towerDatabase == null)
        {
            // Tìm từ TurnSystem hoặc các nơi khác
            var turnSystem = FindAnyObjectByType<TurnSystem>();
            if (turnSystem != null)
            {
                // Sử dụng reflection hoặc public field nếu có
                var field = typeof(TurnSystem).GetField("towerBosses", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    towerDatabase = field.GetValue(turnSystem) as TowerSlimeBosses;
                }
            }
            
            // Nếu vẫn null, tìm trong Resources
            if (towerDatabase == null)
            {
                var allTowers = Resources.FindObjectsOfTypeAll<TowerSlimeBosses>();
                if (allTowers != null && allTowers.Length > 0)
                {
                    towerDatabase = allTowers[0];
                }
            }
        }
        
        if (towerDatabase == null)
        {
            // Không có tower database, bỏ qua
            return;
        }
        
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("CurrencyManager không tồn tại! Không thể claim tower rewards.");
            return;
        }
        
        int totalCoinsClaimed = 0;
        int totalGemsClaimed = 0;
        int floorsClaimed = 0;
        
        // Duyệt qua tất cả các màn
        foreach (var floor in towerDatabase.floors)
        {
            if (floor == null) continue;
            
            // Nếu màn đã completed nhưng chưa claimed
            if (floor.completed && !floor.claimed)
            {
                int coinsReward = floor.rewardCoins;
                int gemsReward = floor.rewardGems;
                
                // Hỗ trợ tương thích với dữ liệu cũ
                if (coinsReward == 0 && gemsReward == 0 && floor.rewardCurrency > 0)
                {
                    coinsReward = floor.rewardCurrency;
                }
                
                // Thêm reward
                if (coinsReward > 0)
                {
                    CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, coinsReward);
                    totalCoinsClaimed += coinsReward;
                }
                
                if (gemsReward > 0)
                {
                    CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, gemsReward);
                    totalGemsClaimed += gemsReward;
                }
                
                // Đánh dấu đã claimed
                floor.claimed = true;
                floorsClaimed++;
                
                Debug.Log($"Đã claim reward màn {floor.floorNumber}: {coinsReward} Coins, {gemsReward} Gems");
            }
        }
        
        // Lưu vào JSON nếu có reward được claim
        if (floorsClaimed > 0)
        {
            if (SaveAndLoadSystem.Instance != null)
            {
                SaveAndLoadSystem.Instance.Save();
            }
            
            Debug.Log($"Đã claim {floorsClaimed} màn tower: Tổng {totalCoinsClaimed} Coins, {totalGemsClaimed} Gems");
        }
    }
    
    /// <summary>
    /// Public method để các script khác có thể gọi để check và claim rewards
    /// </summary>
    public void RefreshTowerRewards()
    {
        CheckAndClaimTowerRewards();
    }
}
