using UnityEngine;
using Spine;
using Spine.Unity;
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
    public SkeletonAnimation idle;
    public SkeletonAnimation running;
    public SkeletonAnimation backIdle;

    private Vector2 movement;
    private PlayerMovementState currentState = PlayerMovementState.Walking;
    private float sitTimer = 0f;
    private bool isSitting = false;

    // Public properties để các script khác có thể truy cập
    public PlayerMovementState CurrentState => currentState;
    public float CurrentDetectionRange => GetDetectionRangeForState(currentState);
    public bool IsMoving => movement.magnitude > 0.1f;
    
    void Start()
    {
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
        }
        
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
        HandleMovement();
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
            GetIdle().gameObject.SetActive(true);
            backIdle.gameObject.SetActive(false);
            running.gameObject.SetActive(false);
            return;
        }

        // Nếu đứng yên lâu, tự động chuyển sang trạng thái ngồi
        if (!IsMoving && sitTimer >= sitTransitionTime)
        {
            currentState = PlayerMovementState.Sitting;
            isSitting = true;
            idle.gameObject.SetActive(true);
            backIdle.gameObject.SetActive(false);
            running.gameObject.SetActive(false);
            return;
        }

        // Nếu không di chuyển, về trạng thái đi bộ
        if (!IsMoving)
        {
            currentState = PlayerMovementState.Walking;
            isSitting = false;
            idle.gameObject.SetActive(true);
            backIdle.gameObject.SetActive(false);
            running.gameObject.SetActive(false);
            return;
        }

        // Kiểm tra trạng thái chạy
        if ((Input.GetKey(runKey) || MobileInput.IsMobileRunInput(mobileJoystickRadius, mobileRunThreshold)) && IsMoving)
        {
            currentState = PlayerMovementState.Running;
            if (movement.y > 0 && movement.x == 0)
            {
                idle.gameObject.SetActive(false);
                backIdle.gameObject.SetActive(true);
                running.gameObject.SetActive(false);
            }
            else if (movement.y < 0 && movement.x == 0)
            {
                idle.gameObject.SetActive(true);
                backIdle.gameObject.SetActive(false);
                running.gameObject.SetActive(false);
            }
            else
            {
                idle.gameObject.SetActive(false);
                backIdle.gameObject.SetActive(false);
                running.gameObject.SetActive(true);
            }
        }
        else if (IsMoving)
        {
            currentState = PlayerMovementState.Walking;
            if (movement.y > 0 && movement.x == 0)
            {
                idle.gameObject.SetActive(false);
                backIdle.gameObject.SetActive(true);
                running.gameObject.SetActive(false);
            } else if (movement.y < 0 && movement.x == 0)
            {
                idle.gameObject.SetActive(true);
                backIdle.gameObject.SetActive(false);
                running.gameObject.SetActive(false);
            } else
            {
                idle.gameObject.SetActive(false);
                backIdle.gameObject.SetActive(false);
                running.gameObject.SetActive(true);
            }
        }

        isSitting = false;
    }

    // Replace the static method with an instance method to access the 'idle' field correctly.
    private SkeletonAnimation GetIdle()
    {
        return idle;     }

    void UpdateSpriteFlip()
    {
        // Flip sprite theo hướng di chuyển (không cần animation)
        if (movement.x < 0)
            transform.localScale = new Vector3(-1, 1, 1); // Quay trái
        else if (movement.x > 0)
            transform.localScale = new Vector3(1, 1, 1);  // Quay phải
    }

    void HandleMovement()
    {
        if (rb == null) return;

        // Tính toán tốc độ dựa trên trạng thái
        float currentSpeed;
        switch (currentState)
        {
            case PlayerMovementState.Sitting:
                currentSpeed = sitSpeed;  // 3 - di chuyển chậm khi ngồi
                break;
            case PlayerMovementState.Running:
                currentSpeed = runSpeed;  // 7 - chạy nhanh
                break;
            case PlayerMovementState.Walking:
            default:
                currentSpeed = walkSpeed; // 5 - đi bộ bình thường
                break;
        }
        
        // Di chuyển player
        Vector2 targetVelocity = movement * currentSpeed;
        rb.linearVelocity = targetVelocity;
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
