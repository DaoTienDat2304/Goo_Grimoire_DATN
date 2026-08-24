using UnityEngine;
using System;
using System.Collections.Generic;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Starting Currency (fallback - Remote Config overrides when available)")]
    [SerializeField] private int startingCoins = 5000;
    [SerializeField] private int startingGems = 5000;

    // Remote keys: `starting_coins` / `starting_gems`; fall back to Inspector values.
    private int StartingCoins => Mathf.Max(0, RemoteBalance.IntOr(RemoteConfigKeys.StartingCoins, startingCoins));
    private int StartingGems => Mathf.Max(0, RemoteBalance.IntOr(RemoteConfigKeys.StartingGems, startingGems));

    private Dictionary<CurrencyType, int> currencies = new Dictionary<CurrencyType, int>();

    public static event Action<CurrencyType, int, int> OnCurrencyChanged; // type, oldAmount, newAmount
    public static event Action<CurrencyType, int> OnCurrencyAdded; // type, amount
    public static event Action<CurrencyType, int> OnCurrencySpent; // type, amount
    public bool firstLoadDone = false;

    private void Awake()
    { 
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeCurrencies();
    }

    private void InitializeCurrencies()
    {
        // Use starting values only when no saved value exists.
        currencies[CurrencyType.Coins] = StartingCoins;
        currencies[CurrencyType.Gems] = StartingGems;
        LoadCurrencyData();
        
        // Saved values override the defaults above when they exist.
        
        // Persist defaults for a new installation and preserve loaded values.
        SaveCurrencyData();
        firstLoadDone = true;
        
        Debug.Log($"Currency loaded: {GetCurrency(CurrencyType.Coins)} Coins, {GetCurrency(CurrencyType.Gems)} Gems");
    }
    public int GetCurrency(CurrencyType type)
    {
        return currencies.ContainsKey(type) ? currencies[type] : 0;
    }
    public void AddCurrency(CurrencyType type, int amount)
    {
        if (amount <= 0) return;

        int oldAmount = GetCurrency(type);
        currencies[type] = oldAmount + amount;
        
        OnCurrencyChanged?.Invoke(type, oldAmount, currencies[type]);
        OnCurrencyAdded?.Invoke(type, amount);
        
        SaveCurrencyData();
        
        Debug.Log($"Add {amount} {type}. Total: {currencies[type]}");
    }
    public bool SpendCurrency(CurrencyType type, int amount)
    {
        if (amount <= 0) return false;
        
        int currentAmount = GetCurrency(type);
        if (currentAmount < amount)
        {
            Debug.LogWarning($"Not enough {type}! Need: {amount}, Have: {currentAmount}");
            return false;
        }

        int oldAmount = currentAmount;
        currencies[type] = currentAmount - amount;
        
        OnCurrencyChanged?.Invoke(type, oldAmount, currencies[type]);
        OnCurrencySpent?.Invoke(type, amount);
        
        SaveCurrencyData();
        
        Debug.Log($"Spend {amount} {type}. Left: {currencies[type]}");
        return true;
    }
    public bool HasEnoughCurrency(CurrencyType type, int amount)
    {
        return GetCurrency(type) >= amount;
    }
    public bool HasEnoughCurrency(List<CurrencyData> costs)
    {
        foreach (var cost in costs)
        {
            if (!HasEnoughCurrency(cost.type, cost.amount))
                return false;
        }
        return true;
    }
    public bool SpendCurrency(List<CurrencyData> costs)
    {
        if (!HasEnoughCurrency(costs)) return false;

        foreach (var cost in costs)
        {
            SpendCurrency(cost.type, cost.amount);
        }
        return true;
    }
    public void SetCurrency(CurrencyType type, int amount)
    {
        int oldAmount = GetCurrency(type);
        currencies[type] = Mathf.Max(0, amount);
        
        OnCurrencyChanged?.Invoke(type, oldAmount, currencies[type]);
        SaveCurrencyData();
    }
    public void SaveCurrencyData()
    {
        foreach (var currency in currencies)
        {
            PlayerPrefs.SetInt($"Currency_{currency.Key}", currency.Value);
        }
        PlayerPrefs.Save();
    }
    private void LoadCurrencyData()
    {
        foreach (CurrencyType type in System.Enum.GetValues(typeof(CurrencyType)))
        {
            string key = $"Currency_{type}";
            if (PlayerPrefs.HasKey(key))
            {
                currencies[type] = PlayerPrefs.GetInt(key);
            }
        }
    }
    public void ResetAllCurrency()
    {
        currencies[CurrencyType.Coins] = startingCoins;
        currencies[CurrencyType.Gems] = startingGems;
        
        foreach (CurrencyType type in System.Enum.GetValues(typeof(CurrencyType)))
        {
            PlayerPrefs.DeleteKey($"Currency_{type}");
        }
        
        SaveCurrencyData();
        
        foreach (var currency in currencies)
        {
            OnCurrencyChanged?.Invoke(currency.Key, 0, currency.Value);
        }
    }

    // Debug methods
    [ContextMenu("Add 100 Coins")]
    private void DebugAdd100Coins() => AddCurrency(CurrencyType.Coins, 100);
    
    [ContextMenu("Add 10 Gems")]
    private void DebugAdd10Gems() => AddCurrency(CurrencyType.Gems, 10);
    
    [ContextMenu("Reset Currency")]
    private void DebugResetCurrency() => ResetAllCurrency();
}
