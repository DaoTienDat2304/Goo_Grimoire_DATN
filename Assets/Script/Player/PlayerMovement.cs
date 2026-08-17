using UnityEngine;

public enum PlayerMovementState
{
    Sitting,    // Ngồi - 2 ô phát hiện
    Walking,    // Đi bộ - 4 ô phát hiện  
    Running     // Chạy - 8 ô phát hiện
}

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public Rigidbody2D rb;
    public float walkSpeed = 5f;           // Tốc độ đi bộ
    public float runSpeed = 7f;            // Tốc độ chạy
    public float sitSpeed = 3f;            // Tốc độ khi ngồi (di chuyển chậm)
    
    [Header("State Settings")]
    public KeyCode runKey = KeyCode.LeftShift;     // Phím chạy (Shift)
    public KeyCode sitKey = KeyCode.LeftControl;   // Phím ngồi (Ctrl)
    public float sitTransitionTime = 0.3f;         // Thời gian chuyển sang trạng thái ngồi
    public float mobileJoystickRadius = 120f;
    [Range(0.5f, 1f)] public float mobileRunThreshold = 0.82f;

    [Header("Detection Ranges for Slimes")]
    public float sittingDetectionRange = 2f;   // 2 ô khi ngồi
    public float walkingDetectionRange = 4f;   // 4 ô khi đi bộ
    public float runningDetectionRange = 8f;   // 8 ô khi chạy

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "PlayerAnimation";
    [SerializeField] private string walkStateName = "PlayerAnimationwalk";
    [SerializeField] private Vector3 visualScale = new Vector3(3.266753f, 3.266753f, 3.266753f);
    [SerializeField] private string playerSortingLayerName = "Player";
    [SerializeField] private int playerSortingOrder = 10;

    private Vector2 movement;
    private PlayerMovementState currentState = PlayerMovementState.Walking;
    private float sitTimer = 0f;
    private bool isSitting = false;
    private bool showingWalkAnimation;
    private bool hasPlayedMovementAnimation;

    // Public properties để các script khác có thể truy cập
    public PlayerMovementState CurrentState => currentState;
    public float CurrentDetectionRange => GetDetectionRangeForState(currentState);
    public bool IsMoving => movement.magnitude > 0.1f;
    
    void Start()
    {
        ResolveAnimationReferences();
        ApplyVisualScale();
        ApplyRendererSorting();

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogWarning("PlayerMovement: Rigidbody2D missing on GameObject. Movement will be disabled.");
            }
        }
        
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        
        PlayMovementAnimation(false);
        Debug.Log("PlayerMovement with state system initialized!");
    }

    void Update()
    {
        HandleInput();
        UpdateMovementState();
        UpdateSpriteFlip();
    }
    
    void FixedUpdate()
    {
        ApplyMovementVelocity();
    }

    void HandleInput()
    {
        movement = MobileInput.GetMovementVector(mobileJoystickRadius);

        // Reset sit timer nếu có input di chuyển
        if (movement.magnitude > 0)
        {
            sitTimer = 0f;
            isSitting = false;
        }
        else
        {
            // Tăng sit timer khi không di chuyển
            sitTimer += Time.deltaTime;
        }
    }

    void UpdateMovementState()
    {
        // Kiểm tra trạng thái ngồi (ưu tiên cao nhất)
        if (Input.GetKey(sitKey))
        {
            currentState = PlayerMovementState.Sitting;
            isSitting = true;
            PlayMovementAnimation(false);
            return;
        }

        // Nếu đứng yên lâu, tự động chuyển sang trạng thái ngồi
        if (!IsMoving && sitTimer >= sitTransitionTime)
        {
            currentState = PlayerMovementState.Sitting;
            isSitting = true;
            PlayMovementAnimation(false);
            return;
        }

        // Nếu không di chuyển, về trạng thái đi bộ
        if (!IsMoving)
        {
            currentState = PlayerMovementState.Walking;
            isSitting = false;
            PlayMovementAnimation(false);
            return;
        }

        // Kiểm tra trạng thái chạy
        if ((Input.GetKey(runKey) || MobileInput.IsMobileRunInput(mobileJoystickRadius, mobileRunThreshold)) && IsMoving)
        {
            currentState = PlayerMovementState.Running;
            PlayMovementAnimation(true);
        }
        else if (IsMoving)
        {
            currentState = PlayerMovementState.Walking;
            PlayMovementAnimation(true);
        }

        isSitting = false;
    }

    private void ResolveAnimationReferences()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void PlayMovementAnimation(bool walking)
    {
        if (animator == null || (hasPlayedMovementAnimation && showingWalkAnimation == walking))
            return;

        hasPlayedMovementAnimation = true;
        showingWalkAnimation = walking;
        animator.speed = 1f;
        animator.Play(walking ? walkStateName : idleStateName, 0, 0f);
    }

    void UpdateSpriteFlip()
    {
        // Flip sprite theo hướng di chuyển (không cần animation)
        if (movement.x < 0)
            transform.localScale = new Vector3(-visualScale.x, visualScale.y, visualScale.z);
        else if (movement.x > 0)
            transform.localScale = visualScale;
        else
            ApplyVisualScale();
    }

    private void ApplyVisualScale()
    {
        float facing = transform.localScale.x < 0f ? -1f : 1f;
        transform.localScale = new Vector3(Mathf.Abs(visualScale.x) * facing, visualScale.y, visualScale.z);
    }

    private void ApplyRendererSorting()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.sortingLayerName = playerSortingLayerName;
            renderer.sortingOrder = playerSortingOrder;
        }
    }

    private float GetSpeedForCurrentState()
    {
        switch (currentState)
        {
            case PlayerMovementState.Sitting:
                return sitSpeed;  // 3 - di chuyển chậm khi ngồi
            case PlayerMovementState.Running:
                return runSpeed;  // 7 - chạy nhanh
            case PlayerMovementState.Walking:
            default:
                return walkSpeed; // 5 - đi bộ bình thường
        }
    }

    void ApplyMovementVelocity()
    {
        if (rb == null) return;

        // Di chuyển player
        rb.linearVelocity = movement * GetSpeedForCurrentState();
    }

    float GetDetectionRangeForState(PlayerMovementState state)
    {
        switch (state)
        {
            case PlayerMovementState.Sitting:
                return sittingDetectionRange;
            case PlayerMovementState.Walking:
                return walkingDetectionRange;
            case PlayerMovementState.Running:
                return runningDetectionRange;
            default:
                return walkingDetectionRange;
        }
    }

    // Debug info
    void OnDrawGizmosSelected()
    {
        // Vẽ detection range hiện tại
        Gizmos.color = GetGizmosColorForState(currentState);
        Gizmos.DrawWireSphere(transform.position, CurrentDetectionRange);

        // Vẽ hướng di chuyển
        if (IsMoving)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)movement * 1.5f);
        }
    }

    Color GetGizmosColorForState(PlayerMovementState state)
    {
        switch (state)
        {
            case PlayerMovementState.Sitting:
                return Color.green;
            case PlayerMovementState.Walking:
                return Color.yellow;
            case PlayerMovementState.Running:
                return Color.red;
            default:
                return Color.white;
        }
    }

    // Public method để các script khác có thể lấy thông tin trạng thái
    public string GetStateInfo()
    {
        return $"State: {currentState}, Speed: {rb?.linearVelocity.magnitude:F1}, Detection: {CurrentDetectionRange}";
    }
}
