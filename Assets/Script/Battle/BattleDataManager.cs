using UnityEngine;

public enum BattleMode
{
    Adventure,  // Từ adventure scene (wild slime)
    Tower,      // Từ menu (tower mode)
    Farm        // Chế độ farm coin
}

public class BattleDataManager : MonoBehaviour
{
    public static BattleDataManager Instance { get; private set; }
    
    private Slime bossSlimeData;
    private bool hasBossData = false;
    
    private int wildSlimeID = -1;
    private BattleMode battleMode = BattleMode.Adventure;
    public int SelectedAdventureLevel { get; set; } = 1;
    public string ReturnSceneName { get; set; } = "firstsave";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void SetBossData(Slime slime, BattleMode mode = BattleMode.Adventure)
    {
        bossSlimeData = slime;
        hasBossData = true;
        battleMode = mode;
    }
    
    public void SetBattleMode(BattleMode mode)
    {
        battleMode = mode;
    }
    
    public Slime GetBossData()
    {
        return bossSlimeData;
    }
    
    public bool HasBossData()
    {
        return hasBossData;
    }
    
    public void ClearBossData()
    {
        bossSlimeData = null;
        hasBossData = false;
        wildSlimeID = -1;
        battleMode = BattleMode.Adventure;
    }
    
    public void ClearBossDataExceptWildSlimeID()
    {
        bossSlimeData = null;
        hasBossData = false;
    }
    
    public BattleMode GetBattleMode()
    {
        return battleMode;
    }
    
    public bool IsTowerMode()
    {
        return battleMode == BattleMode.Tower;
    }
    
    public bool IsAdventureMode()
    {
        return battleMode == BattleMode.Adventure;
    }
    
    public bool IsFarmMode()
    {
        return battleMode == BattleMode.Farm;
    }
    
    public void SetWildSlimeID(int id)
    {
        wildSlimeID = id;
    }
    
    public int GetWildSlimeID()
    {
        return wildSlimeID;
    }
}

