using UnityEngine;

public class PlayerAttackAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string attackStateName = "PlayerAanimationAttack";
    [SerializeField] private string idleStateName = "PlayerAnimation";
    [SerializeField] private float frame1NormalizedTime = 0f;
    [SerializeField] private float frame2NormalizedTime = 0.34f;
    [SerializeField] private float releaseStartNormalizedTime = 0.66f;

    private bool releasing;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void Update()
    {
        if (releasing && animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(attackStateName) && state.normalizedTime >= 0.98f)
            {
                releasing = false;
                animator.speed = 1f;
                animator.Play(idleStateName, 0, 0f);
            }
        }
    }

    public void HoldStart()
    {
        PauseAttackAt(frame1NormalizedTime);
    }

    public void Drag()
    {
        PauseAttackAt(frame2NormalizedTime);
    }

    public void Release()
    {
        if (animator == null)
            return;

        releasing = true;
        animator.speed = 1f;
        animator.Play(attackStateName, 0, releaseStartNormalizedTime);
    }

    public void Cancel()
    {
        releasing = false;
        if (animator == null)
            return;

        animator.speed = 1f;
        animator.Play(idleStateName, 0, 0f);
    }

    private void PauseAttackAt(float normalizedTime)
    {
        if (animator == null)
            return;

        releasing = false;
        animator.Play(attackStateName, 0, normalizedTime);
        animator.Update(0f);
        animator.speed = 0f;
    }
}
