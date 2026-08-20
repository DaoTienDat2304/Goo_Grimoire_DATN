using UnityEngine;
using Spine.Unity;

public enum PlayerMovementState
{
    Sitting,
    Walking,
    Running
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public Rigidbody2D rb;
    public float walkSpeed = 5f;
    public float runSpeed = 7f;
    public float sitSpeed = 3f;

    [Header("State Settings")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode sitKey = KeyCode.LeftControl;
    public float sitTransitionTime = 0.3f;
    public float mobileJoystickRadius = 120f;
    [Range(0.5f, 1f)] public float mobileRunThreshold = 0.82f;

    [Header("Detection Ranges for Slimes")]
    public float sittingDetectionRange = 2f;
    public float walkingDetectionRange = 4f;
    public float runningDetectionRange = 8f;

    [Header("Animation")]
    [HideInInspector] public SkeletonAnimation idle;
    [HideInInspector] public SkeletonAnimation running;
    [HideInInspector] public SkeletonAnimation backIdle;
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "PlayerIdle";
    [SerializeField] private string walkStateName = "PlayerWalk";
    [SerializeField] private string attackStateName = "PlayerAttack";
    [SerializeField] private float attackFrameOneNormalizedTime = 0f;
    [SerializeField] private float attackFrameTwoNormalizedTime = 0.34f;
    [SerializeField] private float attackReleaseNormalizedTime = 0.67f;
    [SerializeField] private float attackReleaseSpeed = 1f;

    private Vector2 movement;
    private PlayerMovementState currentState = PlayerMovementState.Walking;
    private float sitTimer;
    private bool isAimingAttack;
    private string currentAnimationState;

    public PlayerMovementState CurrentState => currentState;
    public float CurrentDetectionRange => GetDetectionRangeForState(currentState);
    public bool IsMoving => movement.magnitude > 0.1f;

    private void Awake()
    {
        ResolveReferences();
        ConfigurePhysics();
    }

    private void Start()
    {
        PlayLocomotionAnimation(false);
    }

    private void Update()
    {
        HandleInput();
        UpdateMovementState();
        UpdateSpriteFlip();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        rb.linearVelocity = isAimingAttack ? Vector2.zero : movement * GetSpeedForCurrentState();
    }

    private void HandleInput()
    {
        movement = MobileInput.GetMovementVector(mobileJoystickRadius);

        if (movement.magnitude > 0f)
            sitTimer = 0f;
        else
            sitTimer += Time.deltaTime;
    }

    private void UpdateMovementState()
    {
        if (isAimingAttack)
            return;

        if (Input.GetKey(sitKey) || (!IsMoving && sitTimer >= sitTransitionTime))
        {
            currentState = PlayerMovementState.Sitting;
            PlayLocomotionAnimation(false);
            return;
        }

        if (!IsMoving)
        {
            currentState = PlayerMovementState.Walking;
            PlayLocomotionAnimation(false);
            return;
        }

        bool wantsRun = Input.GetKey(runKey) || MobileInput.IsMobileRunInput(mobileJoystickRadius, mobileRunThreshold);
        currentState = wantsRun ? PlayerMovementState.Running : PlayerMovementState.Walking;
        PlayLocomotionAnimation(true);
    }

    private void ResolveReferences()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void ConfigurePhysics()
    {
        if (rb == null)
            return;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
        if (playerCollider != null)
            playerCollider.isTrigger = false;
    }

    private void PlayLocomotionAnimation(bool moving)
    {
        if (animator == null)
            return;

        string nextState = moving ? walkStateName : idleStateName;
        if (currentAnimationState == nextState && animator.speed > 0f)
            return;

        animator.speed = 1f;
        animator.Play(nextState, 0, 0f);
        currentAnimationState = nextState;
    }

    public void HoldAttackFrame()
    {
        PlayAttackAt(attackFrameOneNormalizedTime);
    }

    public void DragAttackFrame()
    {
        PlayAttackAt(attackFrameTwoNormalizedTime);
    }

    public void ReleaseAttack()
    {
        if (animator == null)
            return;

        isAimingAttack = false;
        animator.speed = attackReleaseSpeed;
        animator.Play(attackStateName, 0, attackReleaseNormalizedTime);
        currentAnimationState = attackStateName;
    }

    public void CancelAttack()
    {
        isAimingAttack = false;
        PlayLocomotionAnimation(IsMoving);
    }

    private void PlayAttackAt(float normalizedTime)
    {
        if (animator == null)
            return;

        isAimingAttack = true;
        animator.Play(attackStateName, 0, normalizedTime);
        animator.Update(0f);
        animator.speed = 0f;
        currentAnimationState = attackStateName;
    }

    private void UpdateSpriteFlip()
    {
        if (isAimingAttack)
            return;

        if (movement.x < -0.01f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (movement.x > 0.01f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private float GetSpeedForCurrentState()
    {
        switch (currentState)
        {
            case PlayerMovementState.Sitting:
                return sitSpeed;
            case PlayerMovementState.Running:
                return runSpeed;
            case PlayerMovementState.Walking:
            default:
                return walkSpeed;
        }
    }

    private float GetDetectionRangeForState(PlayerMovementState state)
    {
        switch (state)
        {
            case PlayerMovementState.Sitting:
                return sittingDetectionRange;
            case PlayerMovementState.Running:
                return runningDetectionRange;
            case PlayerMovementState.Walking:
            default:
                return walkingDetectionRange;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = GetGizmosColorForState(currentState);
        Gizmos.DrawWireSphere(transform.position, CurrentDetectionRange);

        if (IsMoving)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)movement * 1.5f);
        }
    }

    private Color GetGizmosColorForState(PlayerMovementState state)
    {
        switch (state)
        {
            case PlayerMovementState.Sitting:
                return Color.green;
            case PlayerMovementState.Running:
                return Color.red;
            case PlayerMovementState.Walking:
            default:
                return Color.yellow;
        }
    }

    public string GetStateInfo()
    {
        return $"State: {currentState}, Speed: {rb?.linearVelocity.magnitude:F1}, Detection: {CurrentDetectionRange}";
    }
}
