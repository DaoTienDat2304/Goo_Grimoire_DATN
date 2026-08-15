using UnityEngine;

public class SimpleSlimeWorldSetup : MonoBehaviour
{
    [Header("Setup")]
    public bool setupOnStart = true;
    
    private void Start()
    {
        if (setupOnStart)
        {
            SetupSlimeWorld();
        }
    }
    
    [ContextMenu("Setup Slime World")]
    public void SetupSlimeWorld()
    {
        // Kiểm tra xem đã có SlimeWorldManager trong Scene chưa
        var existingManager = FindAnyObjectByType<SlimeWorldManager>();
        if (existingManager != null)
        {
            return;
        }
        
        // Tạo SlimeWorldManager nếu chưa có bất kỳ manager nào trong scene
        var worldManager = gameObject.AddComponent<SlimeWorldManager>();
        worldManager.showSlimesInWorld = true;
        worldManager.worldRadius = 12f;
        worldManager.maxWorldSlimes = 20;
        worldManager.slimeMoveSpeed = 1.5f;
        worldManager.slimeRotationSpeed = 25f;
        worldManager.slimeBounceHeight = 0.4f;
        worldManager.slimeBounceSpeed = 1.5f;
    }
    

}
