using UnityEngine;
using UnityEngine.UI;

public class CurrencyUI : MonoBehaviour
{
    [Header("Currency Display")]
    [SerializeField] private Text coinsText;
    [SerializeField] private Text gemsText;
    
    [Header("Currency Icons (Optional)")]
    [SerializeField] private Image coinsIcon;
    [SerializeField] private Image gemsIcon;

    public Sprite CoinSprite => coinsIcon != null ? coinsIcon.sprite : null;
    public Sprite GemSprite => gemsIcon != null ? gemsIcon.sprite : null;
    
    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = false;
    
    [Header("Tower Database (Optional)")]
    [SerializeField] private TowerSlimeBosses towerDatabase;

    private void Start()
    {
        CurrencyManager.OnCurrencyChanged += OnCurrencyChanged;
        
        UpdateAllCurrencyDisplay();
        
        CheckAndClaimTowerRewards();
    }

    private void OnDestroy()
    {
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
            UpdateCurrencyDisplay(CurrencyType.Gems, CurrencyManager.Instance.GetCurrency(CurrencyType.Gems));
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
        return CurrencyAmountFormatter.Format(amount);
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

    public void RefreshDisplay()
    {
        UpdateAllCurrencyDisplay();
    }

    public void ShowInsufficientCurrencyMessage(CurrencyType type, int required, int current)
    {
        string currencyName = type == CurrencyType.Coins ? "Coins" : "Gems";
        string message = $"Not enough {currencyName}!\nNeed: {FormatCurrencyAmount(required)}\nHave: {FormatCurrencyAmount(current)}";
        
        Debug.LogWarning(message);
        
        // NotificationManager.ShowMessage(message);
    }
    private void CheckAndClaimTowerRewards()
    {
        if (towerDatabase == null)
        {
            var turnSystem = FindAnyObjectByType<TurnSystem>();
            if (turnSystem != null)
            {
                var field = typeof(TurnSystem).GetField("towerBosses", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    towerDatabase = field.GetValue(turnSystem) as TowerSlimeBosses;
                }
            }
            
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
            return;
        }
        
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("CurrencyManager missing! Khong the claim tower rewards.");
            return;
        }
        
        int totalCoinsClaimed = 0;
        int totalGemsClaimed = 0;
        int floorsClaimed = 0;
        
        foreach (var floor in towerDatabase.floors)
        {
            if (floor == null) continue;
            
            if (floor.completed && !floor.claimed)
            {
                int coinsReward = floor.rewardCoins;
                int gemsReward = floor.rewardGems;
                
                if (coinsReward == 0 && gemsReward == 0 && floor.rewardCurrency > 0)
                {
                    coinsReward = floor.rewardCurrency;
                }

                coinsReward = RemoteBalance.ScaleReward(coinsReward, RemoteBalance.Reward.tower);
                gemsReward = RemoteBalance.ScaleReward(gemsReward, RemoteBalance.Reward.tower);

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
                
                floor.claimed = true;
                floorsClaimed++;
                
            }
        }
        
        if (floorsClaimed > 0)
        {
            if (SaveAndLoadSystem.Instance != null)
            {
                SaveAndLoadSystem.Instance.Save();
            }

        }
    }
    public void RefreshTowerRewards()
    {
        CheckAndClaimTowerRewards();
    }
}
