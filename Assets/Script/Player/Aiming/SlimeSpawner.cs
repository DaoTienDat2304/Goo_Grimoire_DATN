using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Spine.Unity;

public class SlimeSpawner : MonoBehaviour
{
    public enum SpawnAreaShape
    {
        Circle,
        Rectangle
    }

    [Header("Spawn Settings")]
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private Transform player;
    [SerializeField] private float spawnRadius = 50f;
    [SerializeField] private int minSlimeCount = 5;
    [SerializeField] private int maxSlimeCount = 10;
    [SerializeField] private float movementThreshold = 100f;

    [Header("Zone Mode")]
    [SerializeField] private bool useSpawnerAsZoneCenter = true;
    [SerializeField] private bool respawnWhenPlayerMoves = false;
    [SerializeField] private bool limitMaxDistanceFromPlayer = false;
    [SerializeField] private bool passZoneToSlimeAI = true;
    [SerializeField] private SpawnAreaShape spawnAreaShape = SpawnAreaShape.Circle;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(80f, 45f);

    [Header("Spawn Position Settings")]
    [SerializeField] private float minDistanceFromPlayer = 10f;
    [SerializeField] private float maxDistanceFromPlayer = 50f;
    [SerializeField] private int maxSpawnAttempts = 50;
    [SerializeField, Min(1)] private int spawnsPerFrame = 2;

    [Header("Spawn Area")]
    [SerializeField] private LayerMask obstacleLayerMask = -1;

    [Header("Cleanup Settings")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private bool preserveCameraVisibleSlimes = true;
    [SerializeField] private float cameraViewportPadding = 0.08f;

    [Header("Simulation Culling")]
    [SerializeField] private bool enableSimulationCulling = true;
    [SerializeField] private float simulationViewportPadding = 0.35f;
    [SerializeField] private float simulationCullInterval = 0.05f;
    [SerializeField, Min(1)] private int maxSimulationActivationsPerPass = 1;
    [SerializeField] private bool disableRenderersOutsideSimulation = true;
    [SerializeField, Min(1)] private int maxActiveSlimeAI = 4;
    [SerializeField, Min(1)] private int maxAnimatedSlimes = 6;
    [SerializeField] private float visualViewportPadding = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = false;
    [SerializeField] private Color spawnAreaColor = Color.green;
    [SerializeField] private Color minDistanceColor = Color.yellow;
    [SerializeField] private Color maxDistanceColor = Color.blue;
    [SerializeField] private Color slimePositionColor = Color.red;
    [SerializeField] private int gizmoSegments = 32;

    public List<GameObject> activeSlimes = new List<GameObject>();
    private Vector3 lastSpawnPosition;
    private Vector3 lastPlayerPosition;

    private PlayerMovement playerMovement;
    public int spawnedSlimeCount = 0;
    private float simulationCullTimer = 0f;
    private Coroutine spawnRoutine;
    private readonly Dictionary<int, bool> slimeSimulationStates = new Dictionary<int, bool>();
    private readonly Dictionary<int, bool> slimeVisualStates = new Dictionary<int, bool>();
    private readonly Dictionary<int, bool> slimeAnimationStates = new Dictionary<int, bool>();
    private readonly Collider2D[] spawnOverlapResults = new Collider2D[16];

    public void SetSustainedPerformanceMode(bool enabled)
    {
        if (!Application.isMobilePlatform) return;
        // Chi giam tan suat tac vu culling nen khong lam slime dang hien thi
        // bi dung AI/animation trong mot phien choi dai.
        simulationCullInterval = enabled ? Mathf.Max(simulationCullInterval, 0.25f) : Mathf.Max(simulationCullInterval, 0.15f);
        simulationCullTimer = 0f;
    }

    private void Awake()
    {
        // Scene cũ có thể vẫn lưu các giá trị trước khi tối ưu. Ép cấu hình an
        // toàn ở runtime để mọi map, kể cả map tạo sau này, có cùng hành vi.
        simulationCullInterval = Application.isMobilePlatform
            ? Mathf.Max(simulationCullInterval, 0.15f)
            : Mathf.Min(simulationCullInterval, 0.05f);
        maxSimulationActivationsPerPass = Mathf.Max(1, maxSimulationActivationsPerPass);
        disableRenderersOutsideSimulation = true;
        maxActiveSlimeAI = Mathf.Clamp(maxActiveSlimeAI, 1, 4);
        maxAnimatedSlimes = Mathf.Clamp(maxAnimatedSlimes, maxActiveSlimeAI, 6);
        if (Application.isMobilePlatform)
        {
            maxActiveSlimeAI = Mathf.Min(maxActiveSlimeAI, 3);
            maxAnimatedSlimes = Mathf.Min(maxAnimatedSlimes, 4);
        }
        // 8 slime hiển thị tốt trên màn hình nhỏ nhưng nhẹ hơn đáng kể so với
        // 10-12 bộ mesh Spine, collider và trait object ở các scene cũ.
        maxSlimeCount = Mathf.Min(maxSlimeCount, 8);
        minSlimeCount = Mathf.Min(minSlimeCount, maxSlimeCount);

        if (obstacleLayerMask.value == -1 || obstacleLayerMask.value == 0)
        {
            int obstacleMask = LayerMask.GetMask("obstacle", "Obstacle", "Obstacles");
            obstacleLayerMask = obstacleMask;
        }
    }

    void Start()
    {
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

        if (player != null)
        {
            lastPlayerPosition = player.position;
            lastSpawnPosition = player.position;
        }
        else
        {
            lastSpawnPosition = GetSpawnCenter();
        }

        SpawnSlimes();

        Debug.Log("SlimeSpawner initialized!");
    }

    void Update()
    {
        UpdateSlimeSimulationCulling();

        if (!respawnWhenPlayerMoves || player == null) return;

        float distanceMoved = Vector3.Distance(player.position, lastSpawnPosition);

        if (distanceMoved >= movementThreshold)
        {
            SpawnSlimes();
            lastSpawnPosition = player.position;
        }
    }

    void SpawnSlimes()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnSlimesOverFrames());
    }

    IEnumerator SpawnSlimesOverFrames()
    {
        CleanupSlimesForRespawn();

        int targetSlimeCount = maxSlimeCount;
        int slimeCountToSpawn = Mathf.Max(0, targetSlimeCount - activeSlimes.Count);

        Debug.Log($"Keeping {activeSlimes.Count} slimes, spawning {slimeCountToSpawn} more...");

        int spawnedThisFrame = 0;
        for (int i = 0; i < slimeCountToSpawn; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            if (spawnPosition != Vector3.zero)
            {
                SpawnSingleSlime(spawnPosition);
                spawnedThisFrame++;
            }

            if (spawnedThisFrame >= Mathf.Max(1, spawnsPerFrame))
            {
                spawnedThisFrame = 0;
                yield return null;
            }
        }

        Debug.Log($"Active slimes after spawn: {activeSlimes.Count}");
        spawnRoutine = null;
    }

    public Vector3 GetRandomSpawnPosition()
    {
        int attempts = 0;
        Vector3 spawnCenter = GetSpawnCenter();

        while (attempts < maxSpawnAttempts)
        {
            Vector3 candidatePosition = GetRandomPointInSpawnArea(spawnCenter);

            if (player != null)
            {
                float distanceFromPlayer = Vector3.Distance(candidatePosition, player.position);
                if (distanceFromPlayer < minDistanceFromPlayer || (limitMaxDistanceFromPlayer && distanceFromPlayer > maxDistanceFromPlayer))
                {
                    attempts++;
                    continue;
                }
            }

            if (IsPositionValid(candidatePosition))
            {
                return candidatePosition;
            }

            attempts++;
        }

        Debug.LogWarning("Could not find valid spawn position after " + maxSpawnAttempts + " attempts");
        return GetFallbackSpawnPosition(spawnCenter);
    }

    bool IsPositionValid(Vector3 position)
    {
        if (obstacleLayerMask.value == 0)
            return true;

        int hitCount = Physics2D.OverlapCircleNonAlloc(position, 1f, spawnOverlapResults, obstacleLayerMask);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D obstacle = spawnOverlapResults[i];
            if (obstacle != null && !obstacle.isTrigger)
                return false;
        }

        return true;
    }

    Vector3 GetFallbackSpawnPosition(Vector3 spawnCenter)
    {
        if (player == null)
            return spawnCenter;

        Vector2 direction = ((Vector2)(spawnCenter - player.position)).normalized;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.right;

        float distance = Mathf.Max(minDistanceFromPlayer, 1f);
        return player.position + (Vector3)(direction * distance);
    }

    public void SpawnSingleSlime(Vector3 position)
    {
        if (slimePrefab == null)
        {
            Debug.LogError("Slime prefab is not assigned!");
            return;
        }

        GameObject newSlime = Instantiate(slimePrefab, position, Quaternion.identity);

        spawnedSlimeCount++;
        WildSlimeTraits traits = newSlime.GetComponent<WildSlimeTraits>();
        if (traits != null)
        {
            traits.wildSlimeID = spawnedSlimeCount;
        }

        activeSlimes.Add(newSlime);

        if (!newSlime.CompareTag("Slime"))
        {
            newSlime.tag = "Slime";
        }

        SlimeAI slimeAI = newSlime.GetComponent<SlimeAI>();
        if (slimeAI != null)
        {
            if (passZoneToSlimeAI)
            {
                slimeAI.ConfigureTerritory(
                    GetSpawnCenter(),
                    GetTerritoryRadius(),
                    spawnAreaShape == SpawnAreaShape.Rectangle,
                    GetSafeSpawnAreaSize()
                );
            }

            Debug.Log($"Slime spawned at {position}");
        }
        else
        {
            Debug.LogWarning("Slime prefab does not have SlimeAI component!");
        }

        // Để vòng culling ở Update xử lý sau khi Start của WildSlimeTraits đã
        // tạo xong renderer và Spine animation. Nếu tắt ngay tại đây, các
        // component hình ảnh được tạo sau đó sẽ không nhận trạng thái culling.
    }

    void CleanupSlimesForRespawn()
    {
        activeSlimes.RemoveAll(slime => slime == null);

        if (!preserveCameraVisibleSlimes)
        {
            ClearOldSlimes();
            return;
        }

        Camera cam = GetGameplayCamera();
        if (cam == null)
        {
            Debug.LogWarning("No gameplay camera found. Keeping existing slimes to avoid clearing visible targets.");
            return;
        }

        for (int i = activeSlimes.Count - 1; i >= 0; i--)
        {
            GameObject slime = activeSlimes[i];
            if (slime == null)
            {
                activeSlimes.RemoveAt(i);
                continue;
            }

            if (IsInsideCameraViewport(slime.transform.position, cam, cameraViewportPadding))
                continue;

            ForgetSlimeState(slime);
            Destroy(slime);
            activeSlimes.RemoveAt(i);
        }
    }

    Camera GetGameplayCamera()
    {
        if (gameplayCamera != null)
            return gameplayCamera;

        gameplayCamera = Camera.main;
        return gameplayCamera;
    }

    bool IsVisibleToCamera(Vector3 worldPosition, Camera cam)
    {
        return IsInsideCameraViewport(worldPosition, cam, cameraViewportPadding);
    }

    bool IsInsideCameraViewport(Vector3 worldPosition, Camera cam, float padding)
    {
        Vector3 viewportPoint = cam.WorldToViewportPoint(worldPosition);
        if (viewportPoint.z < 0f)
            return false;

        padding = Mathf.Max(0f, padding);
        return viewportPoint.x >= -padding
            && viewportPoint.x <= 1f + padding
            && viewportPoint.y >= -padding
            && viewportPoint.y <= 1f + padding;
    }

    void ClearOldSlimes()
    {
        foreach (GameObject slime in activeSlimes)
        {
            if (slime != null)
            {
                ForgetSlimeState(slime);
                Destroy(slime);
            }
        }

        activeSlimes.Clear();

        Debug.Log("Cleared old slimes");
    }

    public void ForceSpawnSlimes()
    {
        SpawnSlimes();
    }

    public void ClearAllSlimes()
    {
        ClearOldSlimes();
    }

    public void UpdateSpawnSettings(int minCount, int maxCount, float radius, float threshold)
    {
        minSlimeCount = minCount;
        maxSlimeCount = maxCount;
        spawnRadius = radius;
        movementThreshold = threshold;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Vector3 spawnCenter = GetSpawnCenter();
        Gizmos.color = spawnAreaColor;
        if (spawnAreaShape == SpawnAreaShape.Rectangle)
            DrawWireRectangle(spawnCenter, spawnAreaSize);
        else
            DrawWireCircle(spawnCenter, spawnRadius);

        if (player != null)
        {
            Gizmos.color = minDistanceColor;
            DrawWireCircle(player.position, minDistanceFromPlayer);

            if (limitMaxDistanceFromPlayer)
            {
                Gizmos.color = maxDistanceColor;
                DrawWireCircle(player.position, maxDistanceFromPlayer);
            }
        }

        Gizmos.color = slimePositionColor;
        foreach (GameObject slime in activeSlimes)
        {
            if (slime != null)
            {
                Gizmos.DrawWireSphere(slime.transform.position, 1f);
            }
        }
    }

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

    public int ActiveSlimeCount => activeSlimes.Count;
    public float CurrentSpawnRadius => spawnRadius;
    public float DistanceToNextSpawn => player != null ? Vector3.Distance(player.position, lastSpawnPosition) : 0f;

    void UpdateSlimeSimulationCulling()
    {
        if (!enableSimulationCulling)
            return;

        simulationCullTimer -= Time.deltaTime;
        if (simulationCullTimer > 0f)
            return;

        simulationCullTimer = Mathf.Max(0.02f, simulationCullInterval);
        activeSlimes.RemoveAll(slime => slime == null);

        Camera cam = GetGameplayCamera();
        Vector3 focusPosition = player != null
            ? player.position
            : (cam != null ? cam.transform.position : transform.position);
        int activationsLeft = Mathf.Max(1, maxSimulationActivationsPerPass);
        for (int i = 0; i < activeSlimes.Count; i++)
        {
            GameObject slime = activeSlimes[i];
            bool shouldRender = cam == null
                || IsInsideCameraViewport(slime.transform.position, cam, visualViewportPadding);
            int closerSlimes = CountCloserVisibleSlimes(slime, focusPosition, cam);
            bool shouldSimulate = shouldRender && closerSlimes < maxActiveSlimeAI;
            bool shouldAnimate = shouldRender && closerSlimes < maxAnimatedSlimes;
            int instanceId = slime.GetInstanceID();
            bool isSimulating = slimeSimulationStates.TryGetValue(instanceId, out bool state) && state;

            // Tắt đối tượng ngoài màn hình ngay, nhưng chỉ bật một số ít slime
            // mỗi nhịp để tránh AI, physics và Spine cùng khởi động trong một frame.
            if (shouldSimulate && !isSimulating)
            {
                if (activationsLeft <= 0)
                    continue;
                activationsLeft--;
            }

            ApplySlimeSimulationState(slime, shouldSimulate);
            ApplySlimeVisualState(slime, shouldRender, shouldAnimate);
        }
    }

    void ForgetSlimeState(GameObject slime)
    {
        if (slime == null)
            return;

        int instanceId = slime.GetInstanceID();
        slimeSimulationStates.Remove(instanceId);
        slimeVisualStates.Remove(instanceId);
        slimeAnimationStates.Remove(instanceId);
    }

    int CountCloserVisibleSlimes(GameObject target, Vector3 focusPosition, Camera cam)
    {
        float targetDistance = ((Vector2)(target.transform.position - focusPosition)).sqrMagnitude;
        int closerCount = 0;

        for (int i = 0; i < activeSlimes.Count; i++)
        {
            GameObject other = activeSlimes[i];
            if (other == null || other == target)
                continue;
            if (cam != null && !IsInsideCameraViewport(other.transform.position, cam, visualViewportPadding))
                continue;

            float otherDistance = ((Vector2)(other.transform.position - focusPosition)).sqrMagnitude;
            if (otherDistance < targetDistance)
                closerCount++;
        }

        return closerCount;
    }

    bool ShouldSimulateSlime(GameObject slime)
    {
        if (!enableSimulationCulling || slime == null)
            return true;

        Camera cam = GetGameplayCamera();
        if (cam == null)
            return true;

        return IsInsideCameraViewport(slime.transform.position, cam, simulationViewportPadding);
    }

    void ApplySlimeSimulationState(GameObject slime, bool shouldSimulate)
    {
        if (slime == null)
            return;

        int instanceId = slime.GetInstanceID();
        if (slimeSimulationStates.TryGetValue(instanceId, out bool currentState) && currentState == shouldSimulate)
            return;

        slimeSimulationStates[instanceId] = shouldSimulate;

        SlimeAI slimeAI = slime.GetComponent<SlimeAI>();
        if (slimeAI != null && slimeAI.enabled != shouldSimulate)
            slimeAI.enabled = shouldSimulate;

        Rigidbody2D slimeRb = slime.GetComponent<Rigidbody2D>();
        if (slimeRb != null)
        {
            if (!shouldSimulate)
                slimeRb.linearVelocity = Vector2.zero;
            // Rigidbody kinematic + trigger rất rẻ. Giữ simulated để catcher
            // vẫn bắt được cả slime đang ở chế độ AI nhẹ.
            if (!slimeRb.simulated)
                slimeRb.simulated = true;
        }
    }

    void ApplySlimeVisualState(GameObject slime, bool shouldRender, bool shouldAnimate)
    {
        if (slime == null)
            return;

        int instanceId = slime.GetInstanceID();
        bool visualChanged = !slimeVisualStates.TryGetValue(instanceId, out bool currentVisual)
            || currentVisual != shouldRender;
        if (disableRenderersOutsideSimulation && visualChanged)
        {
            slimeVisualStates[instanceId] = shouldRender;
            Renderer[] renderers = slime.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = shouldRender;
        }

        bool animationChanged = !slimeAnimationStates.TryGetValue(instanceId, out bool currentAnimation)
            || currentAnimation != shouldAnimate;
        if (animationChanged)
        {
            slimeAnimationStates[instanceId] = shouldAnimate;
            SkeletonAnimation[] spineAnimations = slime.GetComponentsInChildren<SkeletonAnimation>(true);
            for (int i = 0; i < spineAnimations.Length; i++)
                spineAnimations[i].enabled = shouldAnimate;
        }

        Rigidbody2D slimeRb = slime.GetComponent<Rigidbody2D>();
        if (slimeRb != null && slimeRb.simulated != shouldRender)
        {
            if (!shouldRender)
                slimeRb.linearVelocity = Vector2.zero;
            slimeRb.simulated = shouldRender;
        }
    }

    Vector3 GetRandomPointInSpawnArea(Vector3 center)
    {
        if (spawnAreaShape == SpawnAreaShape.Rectangle)
        {
            Vector2 halfSize = GetSafeSpawnAreaSize() * 0.5f;
            return center + new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y),
                0f
            );
        }

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return center + new Vector3(randomCircle.x, randomCircle.y, 0f);
    }

    float GetTerritoryRadius()
    {
        if (spawnAreaShape == SpawnAreaShape.Rectangle)
        {
            Vector2 halfSize = GetSafeSpawnAreaSize() * 0.5f;
            return halfSize.magnitude;
        }

        return spawnRadius;
    }

    Vector2 GetSafeSpawnAreaSize()
    {
        return new Vector2(Mathf.Max(0.1f, spawnAreaSize.x), Mathf.Max(0.1f, spawnAreaSize.y));
    }

    Vector3 GetSpawnCenter()
    {
        if (useSpawnerAsZoneCenter || player == null)
            return transform.position;

        return player.position;
    }

    void DrawWireRectangle(Vector3 center, Vector2 size)
    {
        Vector2 safeSize = GetSafeSpawnAreaSize();
        Vector3 halfSize = new Vector3(safeSize.x * 0.5f, safeSize.y * 0.5f, 0f);
        Vector3 topLeft = center + new Vector3(-halfSize.x, halfSize.y, 0f);
        Vector3 topRight = center + new Vector3(halfSize.x, halfSize.y, 0f);
        Vector3 bottomRight = center + new Vector3(halfSize.x, -halfSize.y, 0f);
        Vector3 bottomLeft = center + new Vector3(-halfSize.x, -halfSize.y, 0f);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}
