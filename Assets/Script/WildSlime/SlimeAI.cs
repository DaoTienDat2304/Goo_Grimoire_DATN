using UnityEngine;

public class SlimeAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRange = 2f;        // Khoảng cách phát hiện player (cực ngắn để phản ứng tức thì)
    public bool usePlayerStateDetection = true; // Sử dụng detection range dựa trên trạng thái player

    [Header("Evasive Behavior")]
    public float evasionSpeed = 12f;         // Tốc độ tránh né siêu nhanh
    public float evasionDuration = 0.8f;     // Thời gian tránh né ngắn hơn
    public float directionChangeChance = 0.6f; // Xác suất đổi hướng ngẫu nhiên cao

    [Header("Movement")]
    public float normalSpeed = 4f;           // Tốc độ di chuyển bình thường (siêu nhanh)
    public float fleeSpeed = 10f;            // Tốc độ chạy trốn (cực nhanh)
    public float circleRadius = 6f;          // Bán kính vùng di chuyển ngẫu nhiên (rộng hơn)
    public float turnSpeed = 5f;             // Tốc độ quay (siêu nhanh)
    public float fleeDistance = 12f;         // Khoảng cách chạy trốn (xa hơn)
    public float safeDistance = 6f;          // Khoảng cách an toàn để dừng chạy (luôn > detection)
    public float targetDistanceFromPlayer = 3.5f; // Khoảng cách slime cố giữ khi chạy khỏi player
    public float playerRunSpeed = 8f;        // Tốc độ chạy mượt khi phát hiện player

    [Header("Panic Escape (Very Close)")]
    public float panicDistance = 1.2f;       // Rất gần → bứt tốc ngay
    public float panicBurstSpeed = 10f;      // Tốc độ bứt tốc
    public float panicBurstDuration = 0.25f; // Thời gian bứt tốc ngắn

    [Header("Random Movement")]
    public float wanderSpeed = 3.5f;         // Tốc độ di chuyển ngẫu nhiên (nhanh)
    public float wanderTimer = 1.2f;         // Thời gian di chuyển một hướng (ngắn hơn)
    public float idleTimer = 0.2f;           // Thời gian nghỉ cực ngắn
    public bool enableRandomMovement = true; // Bật di chuyển ngẫu nhiên

    [Header("Chaotic Behavior")]
    public float chaosSpeed = 8f;            // Tốc độ hỗn loạn khi bị đuổi
    public float chaosChance = 0.4f;        // Xác suất chuyển sang chế độ hỗn loạn
    public float speedVariation = 0.3f;     // Biến thiên tốc độ ngẫu nhiên
    public float angleVariation = 45f;      // Biến thiên góc ngẫu nhiên

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayerMask = 64; // Layer mask cho obstacles
    public float obstacleDetectionRange = 2f; // Khoảng cách phát hiện obstacles
    public float avoidanceForce = 1.5f;      // Lực tránh obstacles
    public float bodyRadius = 0.3f;          // Bán kính thân để circle cast
    [Header("Crowd Avoidance")]
    [SerializeField, Min(0.1f)] private float separationRadius = 0.9f;
    [SerializeField, Range(0f, 1f)] private float separationStrength = 0.65f;
    [SerializeField, Min(0.05f)] private float separationUpdateInterval = 0.12f;
    [Header("Stuck Handling")]
    public float stuckSpeedThreshold = 0.5f; // Nếu tốc độ thực < ngưỡng này → coi như kẹt
    public float stuckCheckTime = 0.2f;      // Kiểm tra kẹt sau thời gian này
    public float stuckEscapeSpeedMultiplier = 1.15f;
    public float eightDirectionProbeDistance = 2.4f;
    [Header("Smart Movement")]
    public float maxDetectionRange = 4.5f;
    public float playerPredictionTime = 0.22f;
    public float homeReturnStrength = 0.35f;
    public float escapeSideStepStrength = 0.35f;
    public int directionSamples = 16;
    public float velocityAcceleration = 35f;
    public float velocityDeceleration = 45f;
    public float fearMemoryDuration = 1.15f;
    public float minEscapeRunTime = 0.55f;
    public float escapeTargetRefreshTime = 0.45f;
    public float escapeTargetReachDistance = 0.45f;
    public float escapeTargetSideStep = 0.65f;

    [Header("Performance")]
    [Tooltip("Khoảng thời gian giữa các lần AI quét vật cản khi đi lang thang.")]
    [SerializeField, Min(0.05f)] private float wanderDecisionInterval = 0.2f;
    [Tooltip("Khoảng thời gian giữa các lần AI quét vật cản khi chạy trốn.")]
    [SerializeField, Min(0.03f)] private float escapeDecisionInterval = 0.1f;
    [Tooltip("Khoảng thời gian giữa các lần kiểm tra bị kẹt bằng physics cast.")]
    [SerializeField, Min(0.05f)] private float stuckProbeInterval = 0.1f;
    [Header("Fear Learning")]
    [Range(0f, 1f)] public float fearLevel = 0f;
    public float fearGainOnDetect = 0.18f;
    public float fearGainOnPanic = 0.35f;
    public float fearDecayPerSecond = 0.035f;
    public float fearDetectionBonus = 1.25f;
    public float fearTargetDistanceBonus = 1.5f;
    public float fearRunSpeedBonus = 2f;
    public float fearMemoryBonus = 1.2f;
    public float fearChaosBonus = 0.25f;
    [Header("Home Leash")]
    public float maxDistanceFromHome = 14f;
    public float hardReturnDistanceFromHome = 18f;
    public float homeLeashStrength = 0.75f;
    public bool useSoftHomeLeashWhileScared = false;
    public float calmHomePenaltyWeight = 0.25f;
    public float scaredHomePenaltyWeight = 0.05f;

    [Header("Spawn Zone")]
    public bool useSpawnZoneTerritory = true;

    [Header("References")]
    public Transform player;                 // Reference đến player
    public Rigidbody2D rb;
    private PlayerMovement playerMovement;  // Reference đến PlayerMovement script

    // State variables
    private bool isFleeing = false;
    private bool isMoving = false;
    private bool isEvading = false;
    private bool isWandering = false;
    private bool isIdle = false;
    private bool isChaotic = false;
    private bool isPanicking = false;

    // Random movement
    private Vector3 startPosition;
    private Vector3 wanderTarget;
    private float wanderTimeLeft;
    private float idleTimeLeft;
    private float currentAngle = 0f;
    private float moveDirection = 1f;

    // Fleeing & Evasion
    private Vector3 fleeStartPosition;
    private Vector3 fleeTarget;
    private float evasionTimeLeft;
    private Vector3 lastPlayerPosition;
    private float panicTimeLeft;
    private Vector2 desiredVelocity; // Vận tốc sẽ áp dụng ở FixedUpdate (ổn định physics)
    private float stuckTimer = 0f;
    private float fearTimeLeft = 0f;
    private float escapeRunTime = 0f;
    private float escapeTargetRefreshLeft = 0f;
    private bool hasSpawnZoneTerritory = false;
    private Vector3 spawnZoneCenter;
    private float spawnZoneRadius;
    private bool spawnZoneIsRectangle = false;
    private Vector2 spawnZoneSize;
    private Vector3 cachedMovementDirection;
    private Vector3 cachedPreferredDirection;
    private bool cachedDirectionIsEscaping;
    private float nextMovementDecisionTime;
    private float nextStuckProbeTime;
    private float nextSeparationUpdateTime;
    private Vector2 cachedSeparation;
    private readonly Collider2D[] separationHits = new Collider2D[12];

    void Start()
    {
        // Tự động tìm components
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerMovement == null) playerMovement = player?.GetComponent<PlayerMovement>();

        // Cấu hình Rigidbody2D
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            // Slime tự điều hướng bằng AI, không cần solver giải va chạm với
            // tường/player/slime khác. Kinematic + trigger loại bỏ contact storm
            // khi nhiều slime dồn vào một góc nhưng vẫn nhận được Catcher trigger.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            rb.interpolation = RigidbodyInterpolation2D.None;
        }

        Collider2D slimeCollider = GetComponent<Collider2D>();
        if (slimeCollider != null)
            slimeCollider.isTrigger = true;

        // Prefab cũ lưu mask = 0 nên toàn bộ CircleCast trước đây không thấy tường.
        if (obstacleLayerMask.value == 0)
            obstacleLayerMask = LayerMask.GetMask("obstacle");

        // Khởi tạo vị trí bắt đầu và trạng thái
        startPosition = transform.position;
        currentAngle = Random.Range(0f, 360f);
        // Chia đều tải AI giữa các frame thay vì để tất cả slime quét physics cùng lúc.
        nextMovementDecisionTime = Time.time + Random.Range(0f, Mathf.Max(0.05f, wanderDecisionInterval));
        nextStuckProbeTime = Time.time + Random.Range(0f, Mathf.Max(0.05f, stuckProbeInterval));
        if (player != null) lastPlayerPosition = player.position; // tránh vận tốc player bị sai frame đầu

        // Bắt đầu với di chuyển ngẫu nhiên
        if (enableRandomMovement)
        {
            StartWandering();
        }
        else
        {
            isMoving = true;
        }
        ClampDetectionSettings();
    }

    public void ConfigureTerritory(Vector3 center, float radius)
    {
        ConfigureTerritory(center, radius, false, Vector2.one * radius * 2f);
    }

    public void ConfigureTerritory(Vector3 center, float radius, bool isRectangle, Vector2 size)
    {
        spawnZoneCenter = center;
        spawnZoneRadius = Mathf.Max(radius, circleRadius);
        spawnZoneIsRectangle = isRectangle;
        spawnZoneSize = new Vector2(Mathf.Max(size.x, 0.1f), Mathf.Max(size.y, 0.1f));
        hasSpawnZoneTerritory = true;
    }

    void Update()
    {
        if (player == null) return;

        // Lưu vị trí player trước đó để tính toán vận tốc
        Vector3 currentPlayerPos = player.position;

        // Lấy detection range dựa trên trạng thái player
        float currentDetectionRange = GetCurrentDetectionRange();

        // Tính khoảng cách đến player
        float distanceToPlayer = Vector3.Distance(transform.position, currentPlayerPos);
        fearTimeLeft = Mathf.Max(0f, fearTimeLeft - Time.deltaTime);
        DecayFear(distanceToPlayer);

        // Xử lý khi rất gần: bật Panic Escape trước
        if (distanceToPlayer <= panicDistance)
        {
            RefreshFear(fearGainOnPanic);
            if (!isPanicking)
            {
                StartPanicEscape();
            }
            else
            {
                ContinuePanicEscape();
            }
        }
        // Kiểm tra xem có nên chạy trốn không (ngoài panic)
        else if (distanceToPlayer <= currentDetectionRange)
        {
            RefreshFear(fearGainOnDetect);
            if (!isFleeing && !isEvading && !isChaotic)
            {
                // Kiểm tra xem player có đang di chuyển về phía slime không
                if (IsPlayerApproaching())
                {
                    // Có cơ hội chuyển sang chế độ hỗn loạn
                    if (Random.value < GetCurrentChaosChance())
                    {
                        StartChaoticMode();
                    }
                    else
                    {
                        StartEvasion();
                    }
                }
                else
                {
                    StartFleeing();
                }
            }
            else if (isFleeing)
            {
                ContinueFleeing();
            }
            else if (isEvading)
            {
                ContinueEvasion();
            }
            else if (isChaotic)
            {
                ContinueChaoticMode();
            }
        }
        else
        {
            if (isFleeing || isEvading || isChaotic || isPanicking)
            {
                ContinueScaredMovement();
            }
            else if (enableRandomMovement)
            {
                HandleRandomMovement();
            }
            else
            {
                MoveInCircle();
            }
        }

        // Cập nhật vị trí player cuối frame
        lastPlayerPosition = currentPlayerPos;
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        Vector2 steeringVelocity = ApplyImmediateObstacleSlide(desiredVelocity);
        steeringVelocity = ApplySlimeSeparation(steeringVelocity);
        float acceleration = steeringVelocity.sqrMagnitude > rb.linearVelocity.sqrMagnitude
            ? velocityAcceleration
            : velocityDeceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, steeringVelocity, acceleration * Time.fixedDeltaTime);

        // Stuck detection khi đang né/chạy
        bool escaping = isPanicking || isFleeing || isEvading || isChaotic;
        if (escaping)
        {
            float speed = rb.linearVelocity.magnitude;
            bool wantsToMove = desiredVelocity.magnitude > stuckSpeedThreshold;
            bool blockedAhead = false;
            if (wantsToMove && Time.time >= nextStuckProbeTime)
            {
                nextStuckProbeTime = Time.time + Mathf.Max(0.05f, stuckProbeInterval);
                blockedAhead = IsDirectionBlocked(desiredVelocity.normalized, obstacleDetectionRange * 0.75f);
            }
            if ((wantsToMove && speed < stuckSpeedThreshold) || blockedAhead)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer >= stuckCheckTime)
                {
                    // Bị kẹt thì quét đủ 8 hướng quanh slime và chọn hướng thoáng nhất để thoát thân.
                    Vector3 preferred = desiredVelocity.sqrMagnitude > 0.001f
                        ? ((Vector3)desiredVelocity).normalized
                        : GetSmartEscapeDirection(0.15f);
                    Vector3 best = FindEightDirectionEscape(preferred, true);
                    fleeTarget = ClampPointToHomeRadius(transform.position + best * fleeDistance, hardReturnDistanceFromHome);
                    escapeTargetRefreshLeft = escapeTargetRefreshTime;
                    RefreshFear(fearGainOnDetect * 0.5f);
                    desiredVelocity = best * Mathf.Max(GetPlayerRunSpeed() * stuckEscapeSpeedMultiplier, panicBurstSpeed * 0.8f);
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    Vector2 ApplyImmediateObstacleSlide(Vector2 velocity)
    {
        float speed = velocity.magnitude;
        if (speed <= 0.01f) return velocity;

        float probeDistance = Mathf.Max(bodyRadius * 1.5f, speed * Time.fixedDeltaTime * 1.5f);
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, bodyRadius, velocity / speed, probeDistance, obstacleLayerMask);
        if (!IsObstacleCollider(hit.collider)) return velocity;

        Vector2 tangent = Vector2.Perpendicular(hit.normal).normalized;
        if (Vector2.Dot(tangent, velocity) < 0f) tangent = -tangent;
        cachedMovementDirection = Vector3.zero;
        nextMovementDecisionTime = 0f;
        return tangent * speed * 0.8f;
    }

    Vector2 ApplySlimeSeparation(Vector2 velocity)
    {
        if (separationStrength <= 0f || separationRadius <= 0f) return velocity;
        if (Time.time >= nextSeparationUpdateTime)
        {
            nextSeparationUpdateTime = Time.time + separationUpdateInterval + Random.Range(0f, 0.03f);
            cachedSeparation = Vector2.zero;
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, separationRadius, separationHits);
            for (int i = 0; i < count; i++)
            {
                Collider2D other = separationHits[i];
                if (other == null || other.transform == transform || other.GetComponent<SlimeAI>() == null) continue;
                Vector2 away = (Vector2)transform.position - (Vector2)other.transform.position;
                float distance = away.magnitude;
                if (distance <= 0.001f)
                    away = Random.insideUnitCircle.normalized;
                else
                    away /= distance;
                cachedSeparation += away * (1f - Mathf.Clamp01(distance / separationRadius));
            }
            if (cachedSeparation.sqrMagnitude > 1f) cachedSeparation.Normalize();
        }

        float speed = velocity.magnitude;
        if (speed <= 0.01f || cachedSeparation.sqrMagnitude <= 0.001f) return velocity;
        Vector2 blended = Vector2.Lerp(velocity.normalized, cachedSeparation.normalized, separationStrength * cachedSeparation.magnitude);
        return blended.sqrMagnitude > 0.001f ? blended.normalized * speed : velocity;
    }

    void StartFleeing()
    {
        RefreshFear(fearGainOnDetect);
        isFleeing = true;
        isMoving = false;
        isWandering = false;
        isIdle = false;
        escapeRunTime = 0f;

        // Đặt vị trí bắt đầu và target chạy trốn
        fleeStartPosition = transform.position;
        PickNewEscapeTarget(0.35f);
    }

    void ContinueFleeing()
    {
        escapeRunTime += Time.deltaTime;
        escapeTargetRefreshLeft -= Time.deltaTime;

        if (escapeTargetRefreshLeft <= 0f || Vector3.Distance(transform.position, fleeTarget) <= escapeTargetReachDistance)
            PickNewEscapeTarget(0.25f);

        Vector3 desired = GetSmartTargetDirection(fleeTarget, 0.08f);

        // Di chuyển với tốc độ cao
        desiredVelocity = desired * GetPlayerRunSpeed();

        // Dừng khi đã cách player đủ xa và hết hoảng, tránh cảm giác bị nam châm đẩy bật.
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (CanCalmDown(distanceToPlayer))
        {
            StopFleeing();
        }
    }

    void StopFleeing()
    {
        isFleeing = false;
        isEvading = false;
        isChaotic = false;
        isPanicking = false;
        isMoving = true;

        // Quay lại di chuyển ngẫu nhiên
        if (enableRandomMovement)
        {
            StartWandering();
        }

        desiredVelocity = Vector2.zero;
    }

    // === RANDOM MOVEMENT FUNCTIONS ===

    void StartWandering()
    {
        isWandering = true;
        isIdle = false;
        isMoving = false;

        // Chọn điểm ngẫu nhiên trong vùng tròn
        Vector2 randomPoint = Random.insideUnitCircle * circleRadius;
        wanderTarget = ClampPointToHomeRadius(startPosition + new Vector3(randomPoint.x, randomPoint.y, 0), hardReturnDistanceFromHome);
        wanderTimeLeft = wanderTimer;
    }

    void HandleRandomMovement()
    {
        if (isWandering)
        {
            WanderToTarget();
        }
        else if (isIdle)
        {
            IdleBehavior();
        }
        else
        {
            // Chuyển sang wandering
            StartWandering();
        }
    }

    void WanderToTarget()
    {
        wanderTimeLeft -= Time.deltaTime;

        // Kiểm tra xem có player trên đường đi không
        if (IsPlayerInPath(wanderTarget))
        {
            // Có player trên đường → chọn điểm mới
            StartWandering();
            return;
        }

        // Di chuyển đến target
        Vector3 direction = GetSmartWanderDirection(wanderTarget);

        // Thêm thay đổi hướng ngẫu nhiên để khó đoán
        if (Random.value < directionChangeChance * Time.deltaTime)
        {
            direction = Quaternion.Euler(0, 0, Random.Range(-angleVariation, angleVariation)) * direction;
        }

        // Thêm biến thiên tốc độ ngẫu nhiên
        float currentSpeed = wanderSpeed * (1f + Random.Range(-speedVariation, speedVariation));
        desiredVelocity = direction * currentSpeed;

        // Kiểm tra xem đã đến gần target chưa hoặc hết thời gian
        float distanceToTarget = Vector3.Distance(transform.position, wanderTarget);
        if (distanceToTarget < 0.5f || wanderTimeLeft <= 0)
        {
            StartIdle();
        }
    }

    void StartIdle()
    {
        isWandering = false;
        isIdle = true;
        idleTimeLeft = idleTimer;
        desiredVelocity = Vector2.zero;
    }

    void IdleBehavior()
    {
        idleTimeLeft -= Time.deltaTime;

        if (idleTimeLeft <= 0)
        {
            StartWandering();
        }
    }

    // === EVASION FUNCTIONS ===

    bool IsPlayerApproaching()
    {
        if (player == null) return false;

        Vector3 currentPlayerPos = player.position;
        Vector3 directionToSlime = (transform.position - currentPlayerPos).normalized;
        Vector3 playerVelocity = (currentPlayerPos - lastPlayerPosition) / Time.deltaTime;

        // Kiểm tra xem player có đang di chuyển về phía slime không
        float dotProduct = Vector3.Dot(playerVelocity.normalized, directionToSlime);
        return dotProduct > 0.3f && playerVelocity.magnitude > 0.1f;
    }

    void StartEvasion()
    {
        RefreshFear(fearGainOnDetect);
        isEvading = true;
        isFleeing = false;
        isWandering = false;
        isIdle = false;
        escapeRunTime = 0f;

        evasionTimeLeft = evasionDuration;
        lastPlayerPosition = player.position;
        PickNewEscapeTarget(0.75f);
    }

    void ContinueEvasion()
    {
        escapeRunTime += Time.deltaTime;
        evasionTimeLeft -= Time.deltaTime;
        escapeTargetRefreshLeft -= Time.deltaTime;

        if (escapeTargetRefreshLeft <= 0f || Vector3.Distance(transform.position, fleeTarget) <= escapeTargetReachDistance)
            PickNewEscapeTarget(0.8f);

        Vector3 directionToTarget = GetSmartTargetDirection(fleeTarget, 0.18f);

        // Thêm thay đổi hướng ngẫu nhiên để khó bắt
        if (Random.value < directionChangeChance * 2f * Time.deltaTime)
        {
            directionToTarget = Quaternion.Euler(0, 0, Random.Range(-angleVariation * 1.5f, angleVariation * 1.5f)) * directionToTarget;
        }

        // Thêm biến thiên tốc độ ngẫu nhiên
        float currentSpeed = GetPlayerRunSpeed() * (1f + Random.Range(-speedVariation * 0.5f, speedVariation * 0.5f));
        desiredVelocity = directionToTarget * currentSpeed;

        // Dừng nếu đã an toàn hoặc hết thời gian né
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (CanCalmDown(distanceToPlayer) && evasionTimeLeft <= 0)
        {
            StopFleeing();
        }
    }

    // === CHAOTIC MODE FUNCTIONS ===

    void StartChaoticMode()
    {
        RefreshFear(fearGainOnDetect);
        isChaotic = true;
        isFleeing = false;
        isEvading = false;
        isWandering = false;
        isIdle = false;
        escapeRunTime = 0f;

        PickNewEscapeTarget(1f);
    }

    void ContinueChaoticMode()
    {
        escapeRunTime += Time.deltaTime;
        escapeTargetRefreshLeft -= Time.deltaTime;

        // Di chuyển hỗn loạn nhưng vẫn bám theo một điểm thoát, không đẩy thẳng khỏi player mỗi frame.
        Vector3 directionToTarget = GetSmartTargetDirection(fleeTarget, 0.32f);

        // Thay đổi hướng liên tục và ngẫu nhiên
        if (escapeTargetRefreshLeft <= 0f || Random.value < directionChangeChance * 1.5f * Time.deltaTime)
        {
            PickNewEscapeTarget(1f);
        }

        // Biến thiên tốc độ cực mạnh
        float currentSpeed = GetPlayerRunSpeed() * (1f + Random.Range(-speedVariation * 0.8f, speedVariation * 0.8f));
        desiredVelocity = directionToTarget * currentSpeed;

        // Dừng khi đã cách player đủ xa (an toàn hơn)
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (CanCalmDown(distanceToPlayer))
        {
            StopFleeing();
        }
    }

    void MoveInCircle()
    {
        // Tính vị trí trên vòng tròn
        Vector3 circlePosition = startPosition + new Vector3(
            Mathf.Cos(currentAngle * Mathf.Deg2Rad) * circleRadius,
            Mathf.Sin(currentAngle * Mathf.Deg2Rad) * circleRadius,
            0
        );

        // Kiểm tra xem có player trên đường đi không
        if (IsPlayerInPath(circlePosition))
        {
            // Có player trên đường → đổi hướng
            moveDirection *= -1;
        }

        // Cập nhật góc
        currentAngle += moveDirection * turnSpeed * Time.deltaTime;

        // Tính hướng di chuyển
        Vector3 direction = GetSmartWanderDirection(circlePosition);

        // Di chuyển
        desiredVelocity = direction * normalSpeed;
    }

    bool IsPlayerInPath(Vector3 targetPosition)
    {
        if (player == null) return false;

        // Kiểm tra xem player có nằm giữa slime và target không
        Vector3 slimeToPlayer = player.position - transform.position;
        Vector3 slimeToTarget = targetPosition - transform.position;

        // Nếu player nằm trong khoảng cách từ slime đến target
        float dotProduct = Vector3.Dot(slimeToPlayer.normalized, slimeToTarget.normalized);
        float distanceToPlayer = slimeToPlayer.magnitude;
        float distanceToTarget = slimeToTarget.magnitude;

        return dotProduct > 0.7f && distanceToPlayer < distanceToTarget && distanceToPlayer < 3f;
    }

    // Trả về hướng ngẫu nhiên trên mặt phẳng X-Y
    Vector3 GetRandomDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
    }

    Vector3 AvoidObstacles(Vector3 desiredDirection)
    {
        // Raycast để phát hiện obstacles
        RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, bodyRadius, desiredDirection, obstacleDetectionRange, obstacleLayerMask);
        RaycastHit2D leftHit = Physics2D.CircleCast(transform.position, bodyRadius, Quaternion.Euler(0, 0, 30) * desiredDirection, obstacleDetectionRange * 0.7f, obstacleLayerMask);
        RaycastHit2D rightHit = Physics2D.CircleCast(transform.position, bodyRadius, Quaternion.Euler(0, 0, -30) * desiredDirection, obstacleDetectionRange * 0.7f, obstacleLayerMask);

        bool frontBlocked = IsObstacleCollider(frontHit.collider);
        bool leftBlocked = IsObstacleCollider(leftHit.collider);
        bool rightBlocked = IsObstacleCollider(rightHit.collider);

        if (frontBlocked || leftBlocked || rightBlocked)
        {
            return FindEightDirectionEscape(desiredDirection, isFleeing || isEvading || isChaotic || isPanicking);
        }

        return desiredDirection; // Không có obstacle, giữ nguyên hướng
    }

    bool IsObstacleCollider(Collider2D collider)
    {
        if (collider == null) return false;

        // Bỏ qua player và slime khác
        if (collider.CompareTag("Player") || collider.CompareTag("Slime"))
            return false;

        // Kiểm tra layer mask
        return (obstacleLayerMask.value & (1 << collider.gameObject.layer)) != 0;
    }

    Vector3 FindBestAvoidanceDirection(Vector3 originalDirection)
    {
        return FindEightDirectionEscape(originalDirection, isFleeing || isEvading || isChaotic || isPanicking);
    }

    float GetCurrentDetectionRange()
    {
        float range = (!usePlayerStateDetection || playerMovement == null) ? detectionRange : playerMovement.CurrentDetectionRange;
        return Mathf.Min(range + fearLevel * fearDetectionBonus, maxDetectionRange);
    }

    // === PANIC ESCAPE ===
    void StartPanicEscape()
    {
        RefreshFear(fearGainOnPanic);
        isPanicking = true;
        isFleeing = false;
        isEvading = false;
        isChaotic = false;
        isWandering = false;
        isIdle = false;
        escapeRunTime = 0f;

        panicTimeLeft = panicBurstDuration;
        PickNewEscapeTarget(0.45f);

        Vector3 best = GetSmartTargetDirection(fleeTarget, 0.03f);
        desiredVelocity = best * Mathf.Max(GetPlayerRunSpeed(), panicBurstSpeed);
    }

    void ContinuePanicEscape()
    {
        escapeRunTime += Time.deltaTime;
        panicTimeLeft -= Time.deltaTime;
        escapeTargetRefreshLeft -= Time.deltaTime;

        if (escapeTargetRefreshLeft <= 0f || Vector3.Distance(transform.position, fleeTarget) <= escapeTargetReachDistance)
            PickNewEscapeTarget(0.45f);

        Vector3 dir = GetSmartTargetDirection(fleeTarget, 0.03f);
        desiredVelocity = dir * Mathf.Max(GetPlayerRunSpeed(), panicBurstSpeed);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (panicTimeLeft <= 0f)
        {
            isPanicking = false;
            isFleeing = true;
            ContinueFleeing();
        }
        else if (CanCalmDown(distanceToPlayer))
        {
            StopFleeing();
        }
    }

    void ClampDetectionSettings()
    {
        detectionRange = Mathf.Min(detectionRange, maxDetectionRange);
        safeDistance = Mathf.Min(safeDistance, maxDetectionRange * 1.35f);
        targetDistanceFromPlayer = Mathf.Clamp(targetDistanceFromPlayer, detectionRange, maxDetectionRange * 1.5f);
        playerRunSpeed = Mathf.Max(playerRunSpeed, normalSpeed);
        velocityAcceleration = Mathf.Max(velocityAcceleration, 1f);
        velocityDeceleration = Mathf.Max(velocityDeceleration, 1f);
        fearMemoryDuration = Mathf.Max(fearMemoryDuration, 0f);
        fearLevel = Mathf.Clamp01(fearLevel);
        fearGainOnDetect = Mathf.Max(fearGainOnDetect, 0f);
        fearGainOnPanic = Mathf.Max(fearGainOnPanic, 0f);
        fearDecayPerSecond = Mathf.Max(fearDecayPerSecond, 0f);
        fearDetectionBonus = Mathf.Max(fearDetectionBonus, 0f);
        fearTargetDistanceBonus = Mathf.Max(fearTargetDistanceBonus, 0f);
        fearRunSpeedBonus = Mathf.Max(fearRunSpeedBonus, 0f);
        fearMemoryBonus = Mathf.Max(fearMemoryBonus, 0f);
        fearChaosBonus = Mathf.Max(fearChaosBonus, 0f);
        maxDistanceFromHome = Mathf.Max(maxDistanceFromHome, circleRadius);
        hardReturnDistanceFromHome = Mathf.Max(hardReturnDistanceFromHome, maxDistanceFromHome + 0.1f);
        homeLeashStrength = Mathf.Clamp01(homeLeashStrength);
        calmHomePenaltyWeight = Mathf.Max(calmHomePenaltyWeight, 0f);
        scaredHomePenaltyWeight = Mathf.Max(scaredHomePenaltyWeight, 0f);
        minEscapeRunTime = Mathf.Max(minEscapeRunTime, 0f);
        escapeTargetRefreshTime = Mathf.Max(escapeTargetRefreshTime, 0.1f);
        escapeTargetReachDistance = Mathf.Max(escapeTargetReachDistance, 0.1f);
        escapeTargetSideStep = Mathf.Clamp01(escapeTargetSideStep);
        panicDistance = Mathf.Min(panicDistance, maxDetectionRange * 0.5f);
        directionSamples = Mathf.Max(8, directionSamples);
        wanderDecisionInterval = Mathf.Max(0.05f, wanderDecisionInterval);
        escapeDecisionInterval = Mathf.Max(0.03f, escapeDecisionInterval);
        stuckProbeInterval = Mathf.Max(0.05f, stuckProbeInterval);
    }

    void RefreshFear(float gain)
    {
        fearLevel = Mathf.Clamp01(fearLevel + gain);
        fearTimeLeft = GetCurrentFearMemoryDuration();
    }

    void DecayFear(float distanceToPlayer)
    {
        bool activelyScared = isFleeing || isEvading || isChaotic || isPanicking || fearTimeLeft > 0f;
        if (activelyScared || distanceToPlayer <= GetCurrentDetectionRange())
            return;

        fearLevel = Mathf.Max(0f, fearLevel - fearDecayPerSecond * Time.deltaTime);
    }

    void ContinueScaredMovement()
    {
        if (fearTimeLeft <= 0f)
        {
            StopFleeing();
            return;
        }

        if (isPanicking) ContinuePanicEscape();
        else if (isEvading) ContinueEvasion();
        else if (isChaotic) ContinueChaoticMode();
        else ContinueFleeing();
    }

    bool CanCalmDown(float distanceToPlayer)
    {
        return distanceToPlayer >= GetTargetDistanceFromPlayer()
            && fearTimeLeft <= 0f
            && escapeRunTime >= GetCurrentMinEscapeRunTime();
    }

    void PickNewEscapeTarget(float sideWeight)
    {
        Vector3 away = transform.position - GetPredictedPlayerPosition();
        Vector3 awayDirection = away.sqrMagnitude > 0.001f ? away.normalized : GetRandomDirection();
        Vector3 sideDirection = Vector3.Cross(Vector3.forward, awayDirection).normalized;
        if (Random.value < 0.5f) sideDirection = -sideDirection;

        Vector3 escapeDirection = Vector3.Slerp(awayDirection, sideDirection, Mathf.Clamp01(sideWeight * escapeTargetSideStep)).normalized;
        escapeDirection = Quaternion.Euler(0, 0, Random.Range(-angleVariation * 0.35f, angleVariation * 0.35f)) * escapeDirection;

        fleeTarget = ClampPointToHomeRadius(transform.position + escapeDirection.normalized * fleeDistance, hardReturnDistanceFromHome);
        escapeTargetRefreshLeft = escapeTargetRefreshTime;
    }

    float GetTargetDistanceFromPlayer()
    {
        float scaredTargetDistance = targetDistanceFromPlayer + fearLevel * fearTargetDistanceBonus;
        return Mathf.Max(scaredTargetDistance, GetCurrentDetectionRange() * 1.05f);
    }

    float GetPlayerRunSpeed()
    {
        return Mathf.Max(playerRunSpeed + fearLevel * fearRunSpeedBonus, normalSpeed);
    }

    float GetCurrentFearMemoryDuration()
    {
        return fearMemoryDuration + fearLevel * fearMemoryBonus;
    }

    float GetCurrentMinEscapeRunTime()
    {
        return minEscapeRunTime + fearLevel * fearMemoryBonus * 0.35f;
    }

    float GetCurrentChaosChance()
    {
        return Mathf.Clamp01(chaosChance + fearLevel * fearChaosBonus);
    }

    Vector3 GetPredictedPlayerPosition()
    {
        if (player == null) return transform.position;
        Vector3 playerVelocity = Time.deltaTime > 0f ? (player.position - lastPlayerPosition) / Time.deltaTime : Vector3.zero;
        return player.position + playerVelocity * playerPredictionTime;
    }

    Vector3 GetSmartWanderDirection(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - transform.position;
        Vector3 preferred = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : GetRandomDirection();
        return FindBestMovementDirection(preferred, false);
    }

    Vector3 GetSmartEscapeDirection(float randomWeight, Vector3? preferredDirection = null)
    {
        Vector3 threat = GetPredictedPlayerPosition();
        Vector3 away = transform.position - threat;
        Vector3 preferred = away.sqrMagnitude > 0.001f ? away.normalized : GetRandomDirection();

        if (preferredDirection.HasValue && preferredDirection.Value.sqrMagnitude > 0.001f)
            preferred = Vector3.Slerp(preferred, preferredDirection.Value.normalized, 0.35f).normalized;

        Vector3 playerVelocity = (player != null && Time.deltaTime > 0f) ? (player.position - lastPlayerPosition) / Time.deltaTime : Vector3.zero;
        if (playerVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 sideStep = Vector3.Cross(Vector3.forward, playerVelocity.normalized);
            if (Vector3.Dot(sideStep, preferred) < 0f) sideStep = -sideStep;
            preferred = Vector3.Slerp(preferred, sideStep, escapeSideStepStrength).normalized;
        }

        if (randomWeight > 0f)
            preferred = Vector3.Slerp(preferred, GetRandomDirection(), randomWeight).normalized;

        preferred = ApplyHomeLeash(preferred, true);
        return FindBestMovementDirection(preferred, true);
    }

    Vector3 GetSmartTargetDirection(Vector3 targetPosition, float randomWeight)
    {
        Vector3 toTarget = targetPosition - transform.position;
        Vector3 preferred = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : GetSmartEscapeDirection(randomWeight);
        preferred = ApplyHomeLeash(preferred, true);

        if (randomWeight > 0f)
            preferred = Vector3.Slerp(preferred, GetRandomDirection(), randomWeight).normalized;

        return FindBestMovementDirection(preferred, true);
    }

    Vector3 FindBestMovementDirection(Vector3 preferredDirection, bool escaping)
    {
        if (preferredDirection.sqrMagnitude <= 0.001f)
            preferredDirection = GetRandomDirection();

        preferredDirection.Normalize();
        float decisionInterval = escaping ? escapeDecisionInterval : wanderDecisionInterval;
        bool cacheIsUsable = cachedMovementDirection.sqrMagnitude > 0.001f
            && cachedDirectionIsEscaping == escaping
            && Time.time < nextMovementDecisionTime
            && Vector3.Dot(cachedPreferredDirection, preferredDirection) > 0.65f;
        if (cacheIsUsable)
            return cachedMovementDirection;

        Vector3 playerPos = player != null ? GetPredictedPlayerPosition() : transform.position;
        float bestScore = -Mathf.Infinity;
        Vector3 bestDirection = preferredDirection;
        // 8 hướng đủ cho đi lang thang; lúc chạy trốn cho tối đa 12 hướng.
        // Giá trị serialized cũ có thể là 16+, nhưng tăng thêm gần như không
        // cải thiện đường đi trong tilemap trong khi chi phí physics tăng tuyến tính.
        int samples = escaping
            ? Mathf.Clamp(directionSamples, 8, 12)
            : Mathf.Clamp(directionSamples, 8, 8);

        for (int i = 0; i < samples; i++)
        {
            float angle = (360f / samples) * i;
            Vector3 dir = (Quaternion.Euler(0, 0, angle) * preferredDirection).normalized;
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, bodyRadius, dir, obstacleDetectionRange, obstacleLayerMask);
            bool blockedByObstacle = IsObstacleCollider(hit.collider);
            float free = blockedByObstacle ? hit.distance : obstacleDetectionRange;
            float openness = obstacleDetectionRange <= 0f ? 1f : free / obstacleDetectionRange;
            float align = Vector3.Dot(dir, preferredDirection.normalized);

            Vector3 futurePosition = transform.position + dir * Mathf.Max(0.75f, bodyRadius * 2f);
            float playerDistanceScore = player == null ? 0f : Mathf.Clamp01(Vector3.Distance(futurePosition, playerPos) / Mathf.Max(maxDetectionRange, 0.1f));
            float homePenalty = GetTerritoryPenalty(futurePosition);

            float homeWeight = escaping ? scaredHomePenaltyWeight : calmHomePenaltyWeight;
            float score = align * 0.45f + openness * 0.35f - homePenalty * homeWeight;
            if (escaping) score += playerDistanceScore * 0.35f;
            if (blockedByObstacle) score -= 0.75f;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }

        // Vòng quét ở trên đã bao phủ toàn bộ 360 độ. Quét thêm 8 hướng tại đây
        // chỉ lặp lại physics cast và là nguyên nhân lớn gây spike khi nhiều slime.
        cachedPreferredDirection = preferredDirection;
        cachedMovementDirection = bestDirection.normalized;
        cachedDirectionIsEscaping = escaping;
        nextMovementDecisionTime = Time.time + Mathf.Max(0.03f, decisionInterval);
        return cachedMovementDirection;
    }

    bool IsDirectionBlocked(Vector3 direction, float distance)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return false;

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, bodyRadius, direction.normalized, distance, obstacleLayerMask);
        return IsObstacleCollider(hit.collider);
    }

    Vector3 FindEightDirectionEscape(Vector3 preferredDirection, bool escaping)
    {
        if (preferredDirection.sqrMagnitude <= 0.001f)
            preferredDirection = GetRandomDirection();

        Vector3 preferred = preferredDirection.normalized;
        Vector3 playerPos = player != null ? GetPredictedPlayerPosition() : transform.position;
        float probeDistance = Mathf.Max(eightDirectionProbeDistance, obstacleDetectionRange, bodyRadius * 2f);
        float leashRadius = GetSoftTerritoryRadius();
        float bestScore = -Mathf.Infinity;
        Vector3 bestDirection = preferred;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f);
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, bodyRadius, dir, probeDistance, obstacleLayerMask);
            bool blocked = IsObstacleCollider(hit.collider);
            float freeDistance = blocked ? hit.distance : probeDistance;
            float openness = probeDistance <= 0f ? 1f : Mathf.Clamp01(freeDistance / probeDistance);
            float alignment = Vector3.Dot(dir, preferred);

            Vector3 futurePosition = transform.position + dir * Mathf.Max(freeDistance, bodyRadius * 2f);
            float playerDistanceScore = player == null ? 0f : Mathf.Clamp01(Vector3.Distance(futurePosition, playerPos) / Mathf.Max(maxDetectionRange, 0.1f));
            float homePenalty = GetTerritoryPenalty(futurePosition);

            float homeWeight = escaping ? scaredHomePenaltyWeight : calmHomePenaltyWeight;
            float score = openness * 1.2f + alignment * 0.35f - homePenalty * homeWeight;
            if (escaping)
                score += playerDistanceScore * 0.65f;
            if (blocked && freeDistance <= bodyRadius * 1.25f)
                score -= 1.5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }

        return bestDirection.normalized;
    }

    Vector3 ApplyHomeLeash(Vector3 preferredDirection, bool escaping)
    {
        Vector3 homeVector = GetTerritoryCorrectionVector(transform.position, false);
        float distanceFromHome = homeVector.magnitude;
        if (homeVector.sqrMagnitude <= 0.001f)
            return preferredDirection.normalized;

        Vector3 homeDirection = homeVector.normalized;
        float softRadius = GetSoftTerritoryRadius();
        float hardRadius = GetHardTerritoryRadius();

        if (escaping && !useSoftHomeLeashWhileScared && IsInsideHardTerritory(transform.position))
            return preferredDirection.normalized;

        if (IsInsideSoftTerritory(transform.position))
            return preferredDirection.normalized;

        float t = Mathf.InverseLerp(softRadius, hardRadius, distanceFromHome);
        float strength = !IsInsideHardTerritory(transform.position)
            ? 1f
            : Mathf.Lerp(homeLeashStrength, 1f, t);
        return Vector3.Slerp(preferredDirection.normalized, homeDirection, strength).normalized;
    }

    Vector3 ClampPointToHomeRadius(Vector3 point, float radius)
    {
        if (UseRectangleTerritory())
        {
            Vector2 halfSize = spawnZoneSize * 0.5f;
            Vector3 offset = point - spawnZoneCenter;
            return spawnZoneCenter + new Vector3(
                Mathf.Clamp(offset.x, -halfSize.x, halfSize.x),
                Mathf.Clamp(offset.y, -halfSize.y, halfSize.y),
                offset.z
            );
        }

        Vector3 territoryCenter = GetTerritoryCenter();
        Vector3 fromHome = point - territoryCenter;
        float leashRadius = GetHardTerritoryRadius();
        if (fromHome.magnitude <= leashRadius)
            return point;

        return territoryCenter + fromHome.normalized * leashRadius;
    }

    Vector3 GetTerritoryCenter()
    {
        return useSpawnZoneTerritory && hasSpawnZoneTerritory ? spawnZoneCenter : startPosition;
    }

    bool UseRectangleTerritory()
    {
        return useSpawnZoneTerritory && hasSpawnZoneTerritory && spawnZoneIsRectangle;
    }

    float GetSoftTerritoryRadius()
    {
        return useSpawnZoneTerritory && hasSpawnZoneTerritory
            ? Mathf.Max(spawnZoneRadius * 0.92f, circleRadius)
            : Mathf.Max(maxDistanceFromHome, circleRadius);
    }

    float GetHardTerritoryRadius()
    {
        return useSpawnZoneTerritory && hasSpawnZoneTerritory
            ? Mathf.Max(spawnZoneRadius, circleRadius)
            : Mathf.Max(hardReturnDistanceFromHome, circleRadius);
    }

    bool IsInsideSoftTerritory(Vector3 position)
    {
        if (!UseRectangleTerritory())
            return Vector3.Distance(position, GetTerritoryCenter()) <= GetSoftTerritoryRadius();

        Vector2 halfSize = spawnZoneSize * 0.46f;
        Vector3 offset = position - spawnZoneCenter;
        return Mathf.Abs(offset.x) <= halfSize.x && Mathf.Abs(offset.y) <= halfSize.y;
    }

    bool IsInsideHardTerritory(Vector3 position)
    {
        if (!UseRectangleTerritory())
            return Vector3.Distance(position, GetTerritoryCenter()) <= GetHardTerritoryRadius();

        Vector2 halfSize = spawnZoneSize * 0.5f;
        Vector3 offset = position - spawnZoneCenter;
        return Mathf.Abs(offset.x) <= halfSize.x && Mathf.Abs(offset.y) <= halfSize.y;
    }

    Vector3 GetTerritoryCorrectionVector(Vector3 position, bool useSoftBounds)
    {
        if (!UseRectangleTerritory())
            return GetTerritoryCenter() - position;

        Vector2 halfSize = spawnZoneSize * (useSoftBounds ? 0.46f : 0.5f);
        Vector3 offset = position - spawnZoneCenter;
        Vector3 closest = spawnZoneCenter + new Vector3(
            Mathf.Clamp(offset.x, -halfSize.x, halfSize.x),
            Mathf.Clamp(offset.y, -halfSize.y, halfSize.y),
            offset.z
        );

        return closest - position;
    }

    float GetTerritoryPenalty(Vector3 position)
    {
        if (!UseRectangleTerritory())
        {
            float leashRadius = GetSoftTerritoryRadius();
            return Mathf.Clamp01((Vector3.Distance(position, GetTerritoryCenter()) - leashRadius) / Mathf.Max(leashRadius, 0.1f));
        }

        Vector3 correction = GetTerritoryCorrectionVector(position, true);
        float maxSide = Mathf.Max(spawnZoneSize.x, spawnZoneSize.y, 0.1f);
        return Mathf.Clamp01(correction.magnitude / maxSide);
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // Vẽ detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, GetCurrentDetectionRange());

            // Vẽ hướng di chuyển
            if (isFleeing && rb != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb.linearVelocity.normalized * 3f);

                // Vẽ flee target
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(fleeTarget, 0.5f);
                Gizmos.DrawLine(transform.position, fleeTarget);
            }
            else if (isEvading && rb != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb.linearVelocity.normalized * 4f);

                // Vẽ evasion target
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(fleeTarget, 0.3f);
                Gizmos.DrawLine(transform.position, fleeTarget);
            }
            else if (isWandering && rb != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb.linearVelocity.normalized * 2f);

                // Vẽ wander target
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(wanderTarget, 0.4f);
                Gizmos.DrawLine(transform.position, wanderTarget);
            }
            else if (isMoving && rb != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb.linearVelocity.normalized * 2f);
            }

            // Vẽ vùng di chuyển ngẫu nhiên
            Gizmos.color = Color.white;
            DrawWireCircle(startPosition, circleRadius);

            // Vẽ vị trí bắt đầu
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(startPosition, 0.2f);
        }
    }

    void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
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
}
