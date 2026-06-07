using UnityEngine;
using System.Collections;
using Spine;
using Spine.Unity;

public class MovePlayer : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public Transform target;
    public bool isMoving = false;

    [Header("Animation")]
    [SerializeField] private SkeletonAnimation idleAnimation;
    [SerializeField] private SkeletonAnimation runAnimation;
    private int moveBoolHash;

    [Header("Events")]
    public System.Action<Transform> OnReachedTarget;
    public System.Action OnStartedMoving;
    public System.Action OnStoppedMoving;

    private Coroutine movementCoroutine;

    public void MoveToTarget(Transform newTarget = null)
    {
        if (newTarget != null)
            target = newTarget;

        if (target == null) return;

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        movementCoroutine = StartCoroutine(MoveToTargetCoroutine());
    }

    private IEnumerator MoveToTargetCoroutine()
    {
        isMoving = true;
        UpdateAnimations();

        OnStartedMoving?.Invoke();

        while (Vector3.Distance(transform.position, target.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = target.position;

        isMoving = false;
        UpdateAnimations();

        OnStoppedMoving?.Invoke();
        OnReachedTarget?.Invoke(target);

        movementCoroutine = null;
    }

    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        isMoving = false;
        UpdateAnimations();

        OnStoppedMoving?.Invoke();
    }

    public bool IsAtTarget()
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= 0.1f;
    }

    void Start()
    {
        UpdateAnimations();
    }

    void Update()
    {
        // bạn có thể thêm logic khác ở đây
    }

    /// <summary>
    /// Bật/tắt animation theo trạng thái di chuyển, 
    /// nếu cả 2 SkeletonAnimation null thì bỏ qua.
    /// </summary>
    private void UpdateAnimations()
    {
        if (idleAnimation == null && runAnimation == null) return;

        if (idleAnimation != null) idleAnimation.gameObject.SetActive(!isMoving);
        if (runAnimation != null) runAnimation.gameObject.SetActive(isMoving);
    }
}
