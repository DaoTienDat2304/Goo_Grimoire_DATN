using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("All Buildings (assign in Inspector)")]
    public List<Building> allBuildings = new List<Building>();

    [Header("Temporary")]
    [Tooltip("Free build mode.")]
    public bool freeBuildMode = true;

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
