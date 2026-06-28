using UnityEngine;
using System.Collections.Generic;

public class SlimeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject slimePrefab; // Prefab của slime
    [SerializeField] private Transform player; // Reference đến player
    [SerializeField] private float spawnRadius = 50f; // Bán kính spawn (50 đơn vị)
    [SerializeField] private int minSlimeCount = 5; // Số slime tối thiểu
    [SerializeField] private int maxSlimeCount = 10; // Số slime tối đa
    [SerializeField] private float movementThreshold = 100f; // Khoảng cách di chuyển để spawn mới (100 đơn vị)

    [Header("Spawn Position Settings")]
    [SerializeField] private float minDistanceFromPlayer = 10f; // Khoảng cách tối thiểu từ player
    [SerializeField] private float maxDistanceFromPlayer = 50f; // Khoảng cách tối đa từ player
    [SerializeField] private int maxSpawnAttempts = 50; // Số lần thử spawn tối đa

    [Header("Spawn Area")]
    [SerializeField] private LayerMask obstacleLayerMask = -1; // Layer mask cho obstacles

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = false; // Tắt debug gizmos
    [SerializeField] private Color spawnAreaColor = Color.green;
    [SerializeField] private Color minDistanceColor = Color.yellow;
    [SerializeField] private Color maxDistanceColor = Color.blue;
    [SerializeField] private Color slimePositionColor = Color.red;
    [SerializeField] private int gizmoSegments = 32; // Số đoạn để vẽ vòng tròn

    public List<GameObject> activeSlimes = new List<GameObject>(); // Danh sách slime đang hoạt động
    private Vector3 lastSpawnPosition; // Vị trí spawn cuối cùng
    private Vector3 lastPlayerPosition; // Vị trí player cuối cùng

    private PlayerMovement playerMovement; // Reference đến PlayerMovement script
    public int spawnedSlimeCount = 0;

    void Start()
    {
        // Tự động tìm player nếu chưa được gán
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerMovement = playerObj.GetComponent<PlayerMovement>();
            }
        }
        else
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }

        // Khởi tạo vị trí ban đầu
        if (player != null)
        {
            lastPlayerPosition = player.position;
            lastSpawnPosition = player.position;
        }

        // Spawn slime ban đầu
        SpawnSlimes();

        Debug.Log("SlimeSpawner initialized!");
    }

    void Update()
    {
        if (player == null) return;

        // Tính khoảng cách di chuyển từ lần spawn cuối
        float distanceMoved = Vector3.Distance(player.position, lastSpawnPosition);

        // Nếu player đã di chuyển đủ xa, spawn slime mới
        if (distanceMoved >= movementThreshold)
        {
            SpawnSlimes();
            lastSpawnPosition = player.position;
        }
    }

    void SpawnSlimes()
    {
        // Xóa slime cũ trước
        ClearOldSlimes();

        // Tạo số lượng slime ngẫu nhiên
        int slimeCount = Random.Range(minSlimeCount, maxSlimeCount + 1);

        Debug.Log($"Spawning {slimeCount} slimes...");

        for (int i = 0; i < slimeCount; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            if (spawnPosition != Vector3.zero) // Nếu tìm được vị trí hợp lệ
            {
                SpawnSingleSlime(spawnPosition);
            }
        }

        Debug.Log($"Successfully spawned {activeSlimes.Count} slimes");
    }

    public Vector3 GetRandomSpawnPosition()
    {
        int maxAttempts = maxSpawnAttempts; // Số lần thử tối đa
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            // Tạo vị trí ngẫu nhiên trong vòng tròn
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 candidatePosition = player.position + new Vector3(randomCircle.x, randomCircle.y, 0);

            // Kiểm tra khoảng cách từ player
            float distanceFromPlayer = Vector3.Distance(candidatePosition, player.position);
            if (distanceFromPlayer < minDistanceFromPlayer || distanceFromPlayer > maxDistanceFromPlayer)
            {
                attempts++;
                continue;
            }

            // Kiểm tra có obstacle không
            if (IsPositionValid(candidatePosition))
            {
                return candidatePosition;
            }

            attempts++;
        }

        Debug.LogWarning("Could not find valid spawn position after " + maxAttempts + " attempts");
        return Vector3.zero; // Không tìm được vị trí hợp lệ
    }

    bool IsPositionValid(Vector3 position)
    {
        // Kiểm tra có obstacle tại vị trí này không
        Collider2D obstacle = Physics2D.OverlapCircle(position, 1f, obstacleLayerMask);
        return obstacle == null;
    }

    public void SpawnSingleSlime(Vector3 position)
    {
        if (slimePrefab == null)
        {
            Debug.LogError("Slime prefab is not assigned!");
            return;
        }

        // Tạo slime mới
        GameObject newSlime = Instantiate(slimePrefab, position, Quaternion.identity);

        // Thêm vào danh sách
        spawnedSlimeCount++;
        newSlime.GetComponent<WildSlimeTraits>().wildSlimeID = spawnedSlimeCount;
        activeSlimes.Add(newSlime);

        // Đặt tag Slime nếu chưa có
        if (!newSlime.CompareTag("Slime"))
        {
            newSlime.tag = "Slime";
        }

        // Đảm bảo SlimeAI được setup đúng
        SlimeAI slimeAI = newSlime.GetComponent<SlimeAI>();
        if (slimeAI != null)
        {
            // SlimeAI sẽ tự động tìm player trong Start()
            Debug.Log($"Slime spawned at {position}");
        }
        else
        {
            Debug.LogWarning("Slime prefab does not have SlimeAI component!");
        }
    }

    void ClearOldSlimes()
    {
        // Xóa tất cả slime cũ
        foreach (GameObject slime in activeSlimes)
        {
            if (slime != null)
            {
                Destroy(slime);
            }
        }

        // Xóa danh sách
        activeSlimes.Clear();

        Debug.Log("Cleared old slimes");
    }

    // Method để spawn thủ công (có thể gọi từ script khác)
    public void ForceSpawnSlimes()
    {
        SpawnSlimes();
    }

    // Method để xóa tất cả slime (có thể gọi từ script khác)
    public void ClearAllSlimes()
    {
        ClearOldSlimes();
    }

    // Method để thay đổi cài đặt spawn
    public void UpdateSpawnSettings(int minCount, int maxCount, float radius, float threshold)
    {
        minSlimeCount = minCount;
        maxSlimeCount = maxCount;
        spawnRadius = radius;
        movementThreshold = threshold;
    }

    // Debug - vẽ spawn area
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || player == null) return;

        // Vẽ vòng tròn spawn area (xanh lá)
        Gizmos.color = spawnAreaColor;
        DrawWireCircle(player.position, spawnRadius);

        // Vẽ vòng tròn khoảng cách tối thiểu (vàng)
        Gizmos.color = minDistanceColor;
        DrawWireCircle(player.position, minDistanceFromPlayer);

        // Vẽ vòng tròn khoảng cách tối đa (xanh dương)
        Gizmos.color = maxDistanceColor;
        DrawWireCircle(player.position, maxDistanceFromPlayer);

        // Vẽ vị trí slime hiện tại (đỏ)
        Gizmos.color = slimePositionColor;
        foreach (GameObject slime in activeSlimes)
        {
            if (slime != null)
            {
                Gizmos.DrawWireSphere(slime.transform.position, 1f);
            }
        }
    }

    // Helper method để vẽ vòng tròn
    void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = gizmoSegments;
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }
    }

    // Public properties để truy cập thông tin
    public int ActiveSlimeCount => activeSlimes.Count;
    public float CurrentSpawnRadius => spawnRadius;
    public float DistanceToNextSpawn => Vector3.Distance(player.position, lastSpawnPosition);
}
