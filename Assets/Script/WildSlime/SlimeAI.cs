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

    [Header("Panic Escape (Very Close)")]
    public float panicDistance = 1.2f;       // Rất gần → bứt tốc ngay
    public float panicBurstSpeed = 16f;      // Tốc độ bứt tốc
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
    [Header("Stuck Handling")]
    public float stuckSpeedThreshold = 0.5f; // Nếu tốc độ thực < ngưỡng này → coi như kẹt
    public float stuckCheckTime = 0.2f;      // Kiểm tra kẹt sau thời gian này

    [Header("Rarity Intelligence")]
    public bool scaleBehaviorByRarity = true;
    [Range(0f, 1f)] public float rarityIntelligence = 0f;
    public float maxPredictionTime = 0.45f;
    public float rareDetectionBonus = 3f;
    public float rareSpeedBonus = 3f;
    public float rareObstacleRangeBonus = 2f;

    [Header("References")]
    public Transform player;                 // Reference đến player
    public Rigidbody2D rb;
    private PlayerMovement playerMovement;  // Reference đến PlayerMovement script
    private WildSlimeTraits wildSlimeTraits;

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
    private bool rarityProfileApplied = false;

    void Start()
    {
        // Tự động tìm components
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerMovement == null) playerMovement = player?.GetComponent<PlayerMovement>();
        if (wildSlimeTraits == null) wildSlimeTraits = GetComponent<WildSlimeTraits>();

        // Cấu hình Rigidbody2D
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }

        // Khởi tạo vị trí bắt đầu và trạng thái
        startPosition = transform.position;
        currentAngle = Random.Range(0f, 360f);
        if (player != null) lastPlayerPosition = player.position; // tránh vận tốc player bị sai frame đầu

        ApplyRarityProfile();

        // Bắt đầu với di chuyển ngẫu nhiên
        if (enableRandomMovement)
        {
            StartWandering();
        }
        else
        {
            isMoving = true;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (!rarityProfileApplied) ApplyRarityProfile();

        // Lưu vị trí player trước đó để tính toán vận tốc
        Vector3 currentPlayerPos = player.position;

        // Lấy detection range dựa trên trạng thái player
        float currentDetectionRange = GetCurrentDetectionRange();

        // Tính khoảng cách đến player
        float distanceToPlayer = Vector3.Distance(transform.position, currentPlayerPos);

        // Xử lý khi rất gần: bật Panic Escape trước
        if (distanceToPlayer <= panicDistance)
        {
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
            if (!isFleeing && !isEvading && !isChaotic)
            {
                // Kiểm tra xem player có đang di chuyển về phía slime không
                if (IsPlayerApproaching())
                {
                    // Có cơ hội chuyển sang chế độ hỗn loạn
                    if (Random.value < chaosChance)
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
                StopFleeing();
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
        rb.linearVelocity = desiredVelocity;

        // Stuck detection khi đang né/chạy
        bool escaping = isPanicking || isFleeing || isEvading || isChaotic;
        if (escaping)
        {
            float speed = rb.linearVelocity.magnitude;
            if (speed < stuckSpeedThreshold)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer >= stuckCheckTime)
                {
                    // Tìm hướng thoáng nhất ưu tiên tránh player và bứt ra
                    Vector3 best = GetSmartEscapeDirection(0.15f);
                    desiredVelocity = best * Mathf.Max(fleeSpeed, panicBurstSpeed * 0.8f);
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

    void StartFleeing()
    {
        isFleeing = true;
        isMoving = false;

        // Tính hướng chạy trốn (ngược với player)
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 fleeDirection = -directionToPlayer;

        // Đặt vị trí bắt đầu và target chạy trốn
        fleeStartPosition = transform.position;
        fleeTarget = transform.position + fleeDirection * fleeDistance;
    }

    void ContinueFleeing()
    {
        Vector3 desired = GetSmartEscapeDirection(0.1f);

        // Di chuyển với tốc độ cao
        desiredVelocity = desired * fleeSpeed;

        // Dừng khi đã cách player đủ xa (ổn định hơn đo khoảng cách đã đi)
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float minSafe = Mathf.Max(GetCurrentDetectionRange() * 1.2f, safeDistance);
        if (distanceToPlayer >= minSafe)
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
        wanderTarget = startPosition + new Vector3(randomPoint.x, randomPoint.y, 0);
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
        Vector3 direction = (wanderTarget - transform.position).normalized;
        direction = AvoidObstacles(direction);

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
        isEvading = true;
        isFleeing = false;
        isWandering = false;
        isIdle = false;

        evasionTimeLeft = evasionDuration;
        lastPlayerPosition = player.position;

        // Chọn hướng tránh né ngẫu nhiên
        Vector3 directionToPlayer = (GetThreatPosition() - transform.position).normalized;
        Vector3 evasionDirection = Quaternion.Euler(0, 0, Random.Range(-90f, 90f)) * -directionToPlayer;
        fleeTarget = transform.position + evasionDirection * fleeDistance;
    }

    void ContinueEvasion()
    {
        evasionTimeLeft -= Time.deltaTime;

        // Hướng ưu tiên: tránh xa player; vẫn pha trộn nhẹ với mục tiêu né hiện tại để tự nhiên hơn
        Vector3 away = (transform.position - GetThreatPosition());
        Vector3 toTempTarget = (fleeTarget - transform.position);
        Vector3 directionToTarget = ((away.sqrMagnitude > 0.001f ? away.normalized : GetRandomDirection()) * 0.8f
                                    + (toTempTarget.sqrMagnitude > 0.001f ? toTempTarget.normalized : Vector3.zero) * 0.2f).normalized;
        directionToTarget = GetSmartEscapeDirection(0.2f, directionToTarget);

        // Thêm thay đổi hướng ngẫu nhiên để khó bắt
        if (Random.value < directionChangeChance * 2f * Time.deltaTime)
        {
            directionToTarget = Quaternion.Euler(0, 0, Random.Range(-angleVariation * 1.5f, angleVariation * 1.5f)) * directionToTarget;
        }

        // Thêm biến thiên tốc độ ngẫu nhiên
        float currentSpeed = evasionSpeed * (1f + Random.Range(-speedVariation * 0.5f, speedVariation * 0.5f));
        desiredVelocity = directionToTarget * currentSpeed;

        // Dừng nếu đã an toàn hoặc hết thời gian né
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float minSafe = Mathf.Max(GetCurrentDetectionRange() * 1.2f, safeDistance);
        if (distanceToPlayer >= minSafe || evasionTimeLeft <= 0)
        {
            StopFleeing();
        }
    }

    // === CHAOTIC MODE FUNCTIONS ===

    void StartChaoticMode()
    {
        isChaotic = true;
        isFleeing = false;
        isEvading = false;
        isWandering = false;
        isIdle = false;

        // Chọn hướng hoàn toàn ngẫu nhiên
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;
        fleeTarget = transform.position + randomDirection * fleeDistance;
    }

    void ContinueChaoticMode()
    {
        // Di chuyển với tốc độ hỗn loạn nhưng vẫn ưu tiên tránh player
        Vector3 away = (transform.position - GetThreatPosition());
        Vector3 baseDir = (fleeTarget - transform.position).sqrMagnitude > 0.001f
            ? (fleeTarget - transform.position).normalized
            : GetRandomDirection();
        Vector3 directionToTarget = ((away.sqrMagnitude > 0.001f ? away.normalized : GetRandomDirection()) * 0.7f
                                    + baseDir * 0.3f).normalized;
        directionToTarget = GetSmartEscapeDirection(0.35f, directionToTarget);

        // Thay đổi hướng liên tục và ngẫu nhiên
        if (Random.value < directionChangeChance * 3f * Time.deltaTime)
        {
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;
            fleeTarget = transform.position + randomDirection * fleeDistance;
        }

        // Biến thiên tốc độ cực mạnh
        float currentSpeed = chaosSpeed * (1f + Random.Range(-speedVariation * 2f, speedVariation * 2f));
        desiredVelocity = directionToTarget * currentSpeed;

        // Dừng khi đã cách player đủ xa (an toàn hơn)
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float minSafe = Mathf.Max(GetCurrentDetectionRange() * 1.2f, safeDistance);
        if (distanceToPlayer >= minSafe)
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
        Vector3 direction = (circlePosition - transform.position).normalized;

        // Tránh obstacles
        direction = AvoidObstacles(direction);

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
        RaycastHit2D frontHit = Physics2D.Raycast(transform.position, desiredDirection, obstacleDetectionRange, obstacleLayerMask);
        RaycastHit2D leftHit = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 30) * desiredDirection, obstacleDetectionRange * 0.7f, obstacleLayerMask);
        RaycastHit2D rightHit = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -30) * desiredDirection, obstacleDetectionRange * 0.7f, obstacleLayerMask);

        bool frontBlocked = IsObstacleCollider(frontHit.collider);
        bool leftBlocked = IsObstacleCollider(leftHit.collider);
        bool rightBlocked = IsObstacleCollider(rightHit.collider);

        if (frontBlocked || leftBlocked || rightBlocked)
        {
            // Nếu chắn phía trước, thử trượt theo bề mặt để thoát góc kẹt
            if (frontBlocked && frontHit.collider != null)
            {
                Vector2 n = frontHit.normal;
                Vector2 slide = Vector2.Perpendicular(n);
                if (Vector2.Dot(slide, (Vector2)desiredDirection) < 0) slide = -slide;
                return slide.normalized;
            }

            // Có obstacle, tìm hướng tránh tốt nhất
            Vector3 bestDirection = FindBestAvoidanceDirection(desiredDirection);
            return bestDirection;
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
        Vector3 bestDirection = originalDirection;
        float bestScore = -1f;

        // Thử nhiều hướng khác nhau
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f; // Thử mỗi 45 độ
            Vector3 testDirection = Quaternion.Euler(0, 0, angle) * originalDirection;

            // Kiểm tra hướng này có obstacle không
            RaycastHit2D hit = Physics2D.Raycast(transform.position, testDirection, obstacleDetectionRange, obstacleLayerMask);

            if (!IsObstacleCollider(hit.collider))
            {
                // Hướng này không có obstacle, tính điểm
                float score = Vector3.Dot(testDirection, originalDirection);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = testDirection;
                }
            }
        }

        return bestDirection.normalized;
    }

    float GetCurrentDetectionRange()
    {
        if (!usePlayerStateDetection || playerMovement == null)
            return detectionRange;

        return playerMovement.CurrentDetectionRange;
    }

    // === PANIC ESCAPE ===
    void StartPanicEscape()
    {
        isPanicking = true;
        isFleeing = false;
        isEvading = false;
        isChaotic = false;
        isWandering = false;
        isIdle = false;

        panicTimeLeft = panicBurstDuration;

        Vector3 best = GetSmartEscapeDirection(0.05f);
        desiredVelocity = best * panicBurstSpeed;
    }

    void ContinuePanicEscape()
    {
        panicTimeLeft -= Time.deltaTime;

        Vector3 dir = GetSmartEscapeDirection(0.05f);
        desiredVelocity = dir * panicBurstSpeed;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float minSafe = Mathf.Max(GetCurrentDetectionRange() * 1.2f, safeDistance);
        if (distanceToPlayer >= minSafe || panicTimeLeft <= 0f)
        {
            StopFleeing();
        }
    }

    // Tìm hướng thoáng nhất, ưu tiên tránh xa player
    Vector3 GetBestEscapeDirection(Vector3 preferredAway)
    {
        float bestScore = -Mathf.Infinity;
        Vector3 bestDir = preferredAway;
        // Quét 16 hướng quanh, ưu tiên vùng gần hướng tránh xa
        int samples = 16;
        for (int i = 0; i < samples; i++)
        {
            float angle = (360f / samples) * i;
            Vector3 dir = Quaternion.Euler(0, 0, angle) * preferredAway;
            // CircleCast để tính khoảng trống theo bán kính thân
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, bodyRadius, dir, obstacleDetectionRange, obstacleLayerMask);
            float free = hit.collider == null ? obstacleDetectionRange : hit.distance;
            float align = Vector3.Dot(dir, preferredAway) * 0.6f; // ưu tiên cùng hướng tránh
            float openness = (free / obstacleDetectionRange) * 0.4f; // ưu tiên khoảng trống
            float score = align + openness;
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }
        return bestDir.normalized;
    }

    void ApplyRarityProfile()
    {
        if (rarityProfileApplied)
            return;

        if (!scaleBehaviorByRarity)
        {
            rarityProfileApplied = true;
            return;
        }

        if (wildSlimeTraits != null && wildSlimeTraits.tamingDifficulty <= 0f && !HasTraitData())
            return;

        float score = GetRarityScore();
        rarityIntelligence = score;

        float difficultyBonus = 0f;
        if (wildSlimeTraits != null && wildSlimeTraits.tamingDifficulty > 0f)
            difficultyBonus = Mathf.Max(0f, wildSlimeTraits.tamingDifficulty - 3f) * 0.35f;

        detectionRange += rareDetectionBonus * score;
        safeDistance += rareDetectionBonus * 0.8f * score;
        fleeDistance += rareDetectionBonus * score;

        normalSpeed += rareSpeedBonus * 0.25f * score + difficultyBonus;
        wanderSpeed += rareSpeedBonus * 0.2f * score;
        fleeSpeed += rareSpeedBonus * score + difficultyBonus;
        evasionSpeed += rareSpeedBonus * 0.8f * score;
        panicBurstSpeed += rareSpeedBonus * score;
        chaosSpeed += rareSpeedBonus * 0.6f * score;

        directionChangeChance = Mathf.Clamp01(directionChangeChance + score * 0.25f);
        speedVariation = Mathf.Clamp(speedVariation + score * 0.15f, 0f, 0.75f);
        angleVariation += score * 35f;
        obstacleDetectionRange += rareObstacleRangeBonus * score;
        avoidanceForce += score;

        // Rare slime should look alert, not twitch randomly forever.
        chaosChance = Mathf.Lerp(chaosChance, 0.15f, score);
        wanderTimer = Mathf.Lerp(wanderTimer, 0.75f, score);
        idleTimer = Mathf.Lerp(idleTimer, 0.08f, score);

        rarityProfileApplied = true;
    }

    bool HasTraitData()
    {
        if (wildSlimeTraits == null || wildSlimeTraits.newSlime == null || wildSlimeTraits.newSlime.wildSlimeTraits == null)
            return false;

        foreach (var trait in wildSlimeTraits.newSlime.wildSlimeTraits)
        {
            if (trait != null)
                return true;
        }

        return false;
    }

    float GetRarityScore()
    {
        if (wildSlimeTraits == null)
            return Mathf.Clamp01(rarityIntelligence);

        float score = 0f;
        int count = 0;

        if (wildSlimeTraits.newSlime != null && wildSlimeTraits.newSlime.wildSlimeTraits != null)
        {
            foreach (var trait in wildSlimeTraits.newSlime.wildSlimeTraits)
            {
                if (trait == null) continue;
                score += RarityToScore(trait.rarity);
                count++;
            }
        }

        if (count == 0 && wildSlimeTraits.tamingDifficulty > 0f)
            return Mathf.Clamp01((wildSlimeTraits.tamingDifficulty - 3f) / 5f);

        return count > 0 ? Mathf.Clamp01(score / count) : Mathf.Clamp01(rarityIntelligence);
    }

    float RarityToScore(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return 0f;
            case Rarity.Uncommon: return 0.15f;
            case Rarity.Rare: return 0.3f;
            case Rarity.SuperRare: return 0.45f;
            case Rarity.UltraRare: return 0.6f;
            case Rarity.Legendary: return 0.78f;
            case Rarity.Mythic: return 0.9f;
            case Rarity.Secret: return 1f;
            default: return 0f;
        }
    }

    Vector3 GetThreatPosition()
    {
        if (player == null)
            return transform.position;

        Vector3 playerVelocity = Time.deltaTime > 0f
            ? (player.position - lastPlayerPosition) / Time.deltaTime
            : Vector3.zero;

        float predictionTime = Mathf.Lerp(0f, maxPredictionTime, rarityIntelligence);
        return player.position + playerVelocity * predictionTime;
    }

    Vector3 GetSmartEscapeDirection(float randomWeight, Vector3? preferredDirection = null)
    {
        Vector3 threat = GetThreatPosition();
        Vector3 away = transform.position - threat;
        Vector3 baseDirection = away.sqrMagnitude > 0.001f ? away.normalized : GetRandomDirection();

        if (preferredDirection.HasValue && preferredDirection.Value.sqrMagnitude > 0.001f)
            baseDirection = Vector3.Slerp(baseDirection, preferredDirection.Value.normalized, 0.35f);

        Vector3 playerVelocity = Time.deltaTime > 0f
            ? (player.position - lastPlayerPosition) / Time.deltaTime
            : Vector3.zero;

        if (playerVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 lateral = Vector3.Cross(Vector3.forward, playerVelocity.normalized);
            if (Vector3.Dot(lateral, baseDirection) < 0f) lateral = -lateral;
            baseDirection = Vector3.Slerp(baseDirection, lateral, 0.25f * rarityIntelligence).normalized;
        }

        Vector3 homeVector = startPosition - transform.position;
        if (homeVector.magnitude > circleRadius * 0.9f)
            baseDirection = Vector3.Slerp(baseDirection, homeVector.normalized, 0.3f + 0.25f * rarityIntelligence).normalized;

        if (randomWeight > 0f)
        {
            Vector3 random = GetRandomDirection();
            baseDirection = Vector3.Slerp(baseDirection, random, randomWeight * (1f - rarityIntelligence * 0.55f)).normalized;
        }

        return GetBestEscapeDirection(baseDirection);
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
