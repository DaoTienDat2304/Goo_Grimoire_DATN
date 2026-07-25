using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("All Buildings (assign in Inspector)")]
    public List<Building> allBuildings = new List<Building>();

    [Header("Tạm thời")]
    [Tooltip("Bỏ MỌI điều kiện xây dựng (đủ tiền + đủ slime) để xây tự do. Tắt (false) để khôi phục điều kiện sau.")]
    public bool freeBuildMode = true;

    /// <summary>Chế độ xây tự do — Building SO đọc qua đây (không có ref scene).</summary>
    public static bool FreeBuildMode => Instance != null ? Instance.freeBuildMode : true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public List<Building> GetAllBuildings()
    {
        return allBuildings;
    }

    public List<Building> GetUnlockedBuildings()
    {
        return allBuildings.Where(b => b != null && b.buildable).ToList();
    }
}
