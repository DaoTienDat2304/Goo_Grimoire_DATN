using UnityEngine;
using System;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Starting Resources")]
    [SerializeField] private int startingMarshmallow = 10;

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

    // Events để thông báo khi tài nguyên thay đổi
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
        // Khởi tạo tài nguyên ban đầu
        resources[ResourceType.Marshmallow] = startingMarshmallow;
        
        Debug.Log($"Resources initialized: {startingMarshmallow} Marshmallow");
    }

    /// <summary>
    /// Lấy số lượng tài nguyên hiện tại
    /// </summary>
    public int GetResource(ResourceType type)
    {
        return resources.ContainsKey(type) ? resources[type] : 0;
    }

    /// <summary>
    /// Thêm tài nguyên
    /// </summary>
    public void AddResource(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        int oldAmount = GetResource(type);
        resources[type] = oldAmount + amount;
        
        OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
        OnResourceAdded?.Invoke(type, amount);
        
        Debug.Log($"Thêm {amount} {type}. Tổng: {resources[type]}");
    }

    /// <summary>
    /// Trừ tài nguyên (sử dụng)
    /// </summary>
    public bool SpendResource(ResourceType type, int amount)
    {
        if (amount <= 0) return false;
        
        int currentAmount = GetResource(type);
        if (currentAmount < amount)
        {
            Debug.LogWarning($"Không đủ {type}! Cần: {amount}, Có: {currentAmount}");
            return false;
        }

        int oldAmount = currentAmount;
        resources[type] = currentAmount - amount;
        
        OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
        OnResourceSpent?.Invoke(type, amount);
        
        Debug.Log($"Tiêu {amount} {type}. Còn lại: {resources[type]}");
        return true;
    }

    /// <summary>
    /// Kiểm tra có đủ tài nguyên hay không
    /// </summary>
    public bool HasEnoughResource(ResourceType type, int amount)
    {
        return GetResource(type) >= amount;
    }

    /// <summary>
    /// Kiểm tra có đủ nhiều loại tài nguyên hay không
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
    /// Trừ nhiều loại tài nguyên cùng lúc
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
    /// Đặt lại số tài nguyên (chỉ dùng cho debug/testing hoặc load game)
    /// </summary>
    public void SetResource(ResourceType type, int amount)
    {
        int oldAmount = GetResource(type);
        resources[type] = Mathf.Max(0, amount);
        
        OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
    }

    /// <summary>
    /// Reset tất cả tài nguyên về giá trị ban đầu
    /// </summary>
    public void ResetAllResources()
    {
        resources[ResourceType.Marshmallow] = startingMarshmallow;
        
        // Thông báo UI cập nhật
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









