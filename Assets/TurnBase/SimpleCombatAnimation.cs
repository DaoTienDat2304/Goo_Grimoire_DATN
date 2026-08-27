using System.Collections;
using UnityEngine;
using Spine.Unity;

public class SimpleCombatAnimation : MonoBehaviour
{
    [Header("Movement Settings")]
    public float attackMoveDistance = 1.5f;
    public float attackMoveSpeed = 8f;
    public float returnSpeed = 6f;
    
    [Header("Visual Effects")]
    public float scaleMultiplier = 1.3f;
    public float scaleBackMultiplier = 0.7f;
    public Color attackColor = Color.red;
    public Color hitColor = new Color(1f, 0.8f, 0.8f, 1f);
    
    [Header("Timing")]
    public float prepareTime = 0.15f;
    public float impactTime = 0.1f;
    public float scaleBackTime = 0.2f;
    public float hitEffectTime = 0.3f;
    
    private Vector3 originalPosition;
    private Vector3 formationPosition;
    private Vector3 originalScale;
    private Vector3 lockedOriginalScale;
    private Color originalColor;
    private SkeletonGraphic skeletonGraphic;
    private SlimeAnimationController slimeAnimationController;
    private bool isAnimating = false;
    private Transform formationSlot;
    
    void Start()
    {
        StartCoroutine(DelayedInitialization());
    }
    
    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        originalPosition = transform.position;
        formationPosition = GetFormationPosition();
        
        
        originalScale = Vector3.one;
        lockedOriginalScale = originalScale;
        transform.localScale = originalScale;
        
        skeletonGraphic = GetComponentInChildren<SkeletonGraphic>();
        slimeAnimationController = GetComponent<SlimeAnimationController>();
        
        if (skeletonGraphic != null)
        {
            originalColor = skeletonGraphic.color;
        }
        
        if (slimeAnimationController != null)
        {
            slimeAnimationController.PlayAnimation("animation");
        }
        
    }
    
    private Vector3 GetFormationPosition()
    {
        DropZone dropZone = GetComponentInParent<DropZone>();
        if (dropZone != null)
        {
            return dropZone.transform.position;
        }
        
        Member member = GetComponentInParent<Member>();
        if (member != null)
        {
            return member.transform.position;
        }
        
        return originalPosition;
    }
    
    public IEnumerator PlayAttackAnimation(Transform target)
    {
        if (isAnimating) yield break;
        
        isAnimating = true;
        
        yield return StartCoroutine(PrepareAttack());
        
        Vector3 targetPosition = target.position;
        Vector3 attackPosition = CalculateAttackPosition(originalPosition, targetPosition);
        
        yield return StartCoroutine(MoveToTarget(attackPosition));
        
        yield return StartCoroutine(ImpactEffect());
        
        yield return StartCoroutine(ScaleBackToOriginal());
        
        yield return StartCoroutine(ReturnToFormationPosition());
        
        ResetToOriginalState();
        
        isAnimating = false;
    }
    
    public IEnumerator PlayHitAnimation()
    {
        if (isAnimating) yield break;
        
        isAnimating = true;
        
        if (skeletonGraphic != null)
        {
            skeletonGraphic.color = hitColor;
        }
        
        yield return StartCoroutine(ShakeEffect(hitEffectTime, 0.15f));
        
        if (skeletonGraphic != null)
        {
            skeletonGraphic.color = originalColor;
        }
        
        isAnimating = false;
    }
    
    private IEnumerator PrepareAttack()
    {
        formationSlot = transform.parent;
        float elapsedTime = 0f;
        Vector3 targetScale = lockedOriginalScale * scaleMultiplier;
        
        
        while (elapsedTime < prepareTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / prepareTime;
            
            transform.localScale = Vector3.Lerp(lockedOriginalScale, targetScale, t);
            
            if (skeletonGraphic != null)
            {
                skeletonGraphic.color = Color.Lerp(originalColor, attackColor, t);
            }
            
            yield return null;
        }
        
        transform.localScale = targetScale;
        if (skeletonGraphic != null)
        {
            skeletonGraphic.color = attackColor;
        }
    }
    
    private Vector3 CalculateAttackPosition(Vector3 start, Vector3 target)
    {
        Vector3 direction = (target - start).normalized;
        return start + direction * attackMoveDistance;
    }
    
    private IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        transform.SetParent(transform.parent.parent.parent);
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / attackMoveSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        transform.position = targetPosition;
    }
    
    private IEnumerator ImpactEffect()
    {
        yield return StartCoroutine(ShakeEffect(impactTime, 0.08f));
    }
    
    private IEnumerator ScaleBackToOriginal()
    {
        Vector3 currentScale = transform.localScale;
        Vector3 targetScale = lockedOriginalScale * scaleBackMultiplier;
        float elapsedTime = 0f;
        float duration = scaleBackTime;
        
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            transform.localScale = Vector3.Lerp(currentScale, targetScale, t);
            
            if (skeletonGraphic != null)
            {
                skeletonGraphic.color = Color.Lerp(attackColor, originalColor, t);
            }
            
            yield return null;
        }
        
        transform.localScale = targetScale;
        if (skeletonGraphic != null)
        {
            skeletonGraphic.color = originalColor;
        }
        
    }
    
    private IEnumerator ReturnToFormationPosition()
    {
        Vector3 startPosition = transform.position;
        
        Vector3 targetFormationPos = GetFormationPosition();
        
        float distance = Vector3.Distance(startPosition, targetFormationPos);
        float duration = distance / returnSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Smooth return
            t = Mathf.SmoothStep(0f, 1f, t);
            
            transform.position = Vector3.Lerp(startPosition, targetFormationPos, t);
            yield return null;
        }
        
        transform.position = targetFormationPos;
        
        originalPosition = targetFormationPos;
        transform.SetParent(formationSlot);
    }
    
    private IEnumerator ShakeEffect(float duration, float intensity)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            Vector3 randomOffset = Random.insideUnitSphere * intensity;
            randomOffset.z = 0;
            transform.position = originalPos + randomOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }
    
    private void ResetToOriginalState()
    {
        Vector3 targetPos = GetFormationPosition();
        transform.position = targetPos;
        
        if (skeletonGraphic != null && skeletonGraphic.color != originalColor)
        {
            skeletonGraphic.color = originalColor;
            Debug.LogWarning($"{gameObject.name} had to force reset color");
        }
        
    }
    
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    public void ForceReset()
    {
        isAnimating = false;
        ResetToOriginalState();
    }
    
    public void ForceResetScale()
    {
        transform.localScale = originalScale;
    }
    
    public void ForceSetOriginalScale()
    {
        var isEnemy = GetComponent<SlimeStats>()?.isEnemy ?? false;
        if (!isEnemy)
        {
            originalScale = Vector3.one * 1.3f; // Force set 1.3f cho team slimes
        }
        else
        {
            originalScale = Vector3.one;
        }
        
        transform.localScale = originalScale;
    }
    
    [ContextMenu("Check And Fix Scale")]
    public void CheckAndFixScale()
    {
        Vector3 expectedScale = Vector3.one;
        
        originalScale = expectedScale;
        lockedOriginalScale = expectedScale;
        transform.localScale = expectedScale;
    }
    
    public void UpdateFormationPosition()
    {
        Vector3 newFormationPos = GetFormationPosition();
        formationPosition = newFormationPos;
        
        var isEnemy = GetComponent<SlimeStats>()?.isEnemy ?? false;
        if (!isEnemy)
        {
            originalPosition = newFormationPos;
        }
        
    }
    
    public void OnDroppedToFormation()
    {
        StartCoroutine(DelayedUpdateFormationPosition());
    }
    
    private IEnumerator DelayedUpdateFormationPosition()
    {
        yield return new WaitForEndOfFrame();
        UpdateFormationPosition();
    }
    
    [ContextMenu("Debug Formation Position")]
    public void DebugFormationPosition()
    {
        var dropZone = GetComponentInParent<DropZone>();
        var member = GetComponentInParent<Member>();
        var slimeStats = GetComponent<SlimeStats>();
        var currentFormationPos = GetFormationPosition();
        
    }
    
    [ContextMenu("Test Scale Animation")]
    public void TestScaleAnimation()
    {
        if (!isAnimating)
        {
            StartCoroutine(TestScaleAnimationCoroutine());
        }
    }
    
    private IEnumerator TestScaleAnimationCoroutine()
    {
        
        yield return StartCoroutine(PrepareAttack());
        
        yield return new WaitForSeconds(0.5f);
        
        yield return StartCoroutine(ScaleBackToOriginal());
    }
    
    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
