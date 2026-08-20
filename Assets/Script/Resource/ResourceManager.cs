using UnityEngine;
using System;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Starting Resources")]
    [SerializeField] private int startingMarshmallow = 10;

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

    public static event Action<ResourceType, int, int> OnResourceChanged; // type, oldAmount, newAmount
    public static event Action<ResourceType, int> OnResourceAdded; // type, amount
    public static event Action<ResourceType, int> OnResourceSpent; // type, amount

    private void Awake()
    { 
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeResources();
    }

    private void InitializeResources()
    {
        resources[ResourceType.Marshmallow] = startingMarshmallow;
        
        Debug.Log($"Resources initialized: {startingMarshmallow} Marshmallow");
    }

    /// <summary>
    /// </summary>
    public int GetResource(ResourceType type)
    {
        return resources.ContainsKey(type) ? resources[type] : 0;
    }

    /// <summary>
    /// </summary>
    public void AddResource(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        int oldAmount = GetResource(type);
        resources[type] = oldAmount + amount;
        
        OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
        OnResourceAdded?.Invoke(type, amount);
        
        Debug.Log($"Add {amount} {type}. Total: {resources[type]}");
    }

    /// <summary>
    /// </summary>
    public bool SpendResource(ResourceType type, int amount)
    {
        if (amount <= 0) return false;
        
        int currentAmount = GetResource(type);
        if (currentAmount < amount)
        {
            Debug.LogWarning($"Not enough {type}! Need: {amount}, Have: {currentAmount}");
            return false;
        }

        int oldAmount = currentAmount;
        resources[type] = currentAmount - amount;
        
        OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
        OnResourceSpent?.Invoke(type, amount);
        
        Debug.Log($"Spend {amount} {type}. Left: {resources[type]}");
        return true;
    }

    /// <summary>
    /// </summary>
    public bool HasEnoughResource(ResourceType type, int amount)
    {
        return GetResource(type) >= amount;
    }

    /// <summary>
    /// </summary>
    public bool HasEnoughResource(List<ResourceData> costs)
    {
        foreach (var cost in costs)
        {
            if (!HasEnoughResource(cost.type, cost.amount))
                return false;
        }
        return true;
    }

    /// <summary>
    /// </summary>
    public bool SpendResource(List<ResourceData> costs)
    {
        if (!HasEnoughResource(costs)) return false;

        foreach (var cost in costs)
        {
            SpendResource(cost.type, cost.amount);
        }
        return true;
    }

    /// <summary>
    /// </summary>
    public void SetResource(ResourceType type, int amount)
    {
        int oldAmount = GetResource(type);
        resources[type] = Mathf.Max(0, amount);
        
        OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
    }

    /// <summary>
    /// </summary>
    public void ResetAllResources()
    {
        resources[ResourceType.Marshmallow] = startingMarshmallow;
        
        foreach (var resource in resources)
        {
            OnResourceChanged?.Invoke(resource.Key, 0, resource.Value);
        }
    }

    // Debug methods
    [ContextMenu("Add 10 Marshmallow")]
    private void DebugAdd10Marshmallow() => AddResource(ResourceType.Marshmallow, 10);
    
    [ContextMenu("Reset Resources")]
    private void DebugResetResources() => ResetAllResources();
}









