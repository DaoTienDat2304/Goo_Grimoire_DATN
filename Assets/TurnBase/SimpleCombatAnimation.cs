using System.Collections;
using UnityEngine;
using Spine.Unity;

public class SimpleCombatAnimation : MonoBehaviour
{
    [Header("Movement Settings")]
    public float attackMoveDistance = 1.5f; // Khoảng cách lao vào
    public float attackMoveSpeed = 8f; // Tốc độ lao vào
    public float returnSpeed = 6f; // Tốc độ quay về vị trí ban đầu
    
    [Header("Visual Effects")]
    public float scaleMultiplier = 1.3f; // Phóng to khi tấn công
    public float scaleBackMultiplier = 0.7f; // Thu nhỏ sau khi tấn công (0.7 * 1.3 = 0.91)
    public Color attackColor = Color.red; // Màu khi tấn công
    public Color hitColor = new Color(1f, 0.8f, 0.8f, 1f); // Màu hồng nhạt khi bị đánh
    
    [Header("Timing")]
    public float prepareTime = 0.15f; // Thời gian chuẩn bị
    public float impactTime = 0.1f; // Thời gian impact
    public float scaleBackTime = 0.2f; // Thời gian thu nhỏ lại
    public float hitEffectTime = 0.3f; // Thời gian hiệu ứng bị đánh
    
    private Vector3 originalPosition;
    private Vector3 formationPosition; // Vị trí formation để quay về
    private Vector3 originalScale;
    private Vector3 lockedOriginalScale; // Backup scale để không bị ghi đè
    private Color originalColor;
    private SkeletonGraphic skeletonGraphic;
    private SlimeAnimationController slimeAnimationController;
    private bool isAnimating = false;
    private Transform formationSlot;
    
    void Start()
    {
        // Delay một chút để đảm bảo transform đã được setup hoàn toàn
        StartCoroutine(DelayedInitialization());
    }
    
    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Thêm delay để đảm bảo scale đã được set đúng
        
        // Lưu vị trí, scale và màu ban đầu
        originalPosition = transform.position;
        formationPosition = GetFormationPosition(); // Lấy vị trí formation
        
        
        // Force set originalScale đúng ngay từ đầu
        var isEnemy = GetComponent<SlimeStats>()?.isEnemy ?? false;
        if (!isEnemy)
        {
            originalScale = Vector3.one * 1.3f; // Team slimes luôn là 1.3f
            lockedOriginalScale = originalScale; // Backup không thể thay đổi
            transform.localScale = originalScale; // Đảm bảo current scale cũng đúng
        }
        else
        {
            originalScale = Vector3.one; // Boss là 1.0f
            lockedOriginalScale = originalScale; // Backup không thể thay đổi
            transform.localScale = originalScale;
        }
        
        // Lấy các component cần thiết
        skeletonGraphic = GetComponentInChildren<SkeletonGraphic>();
        slimeAnimationController = GetComponent<SlimeAnimationController>();
        
        if (skeletonGraphic != null)
        {
            originalColor = skeletonGraphic.color;
        }
        
        // Đảm bảo animation idle đang chạy
        if (slimeAnimationController != null)
        {
            slimeAnimationController.PlayAnimation("animation");
        }
        
        Debug.Log($"{gameObject.name} initialized - Position: {originalPosition}, Scale: {originalScale}, Formation: {formationPosition}, IsEnemy: {isEnemy}");
    }
    
    private Vector3 GetFormationPosition()
    {
        // Tìm DropZone parent (vị trí formation hiện tại sau khi drag & drop)
        DropZone dropZone = GetComponentInParent<DropZone>();
        if (dropZone != null)
        {
            // Quay về vị trí của DropZone (formation slot hiện tại)
            return dropZone.transform.position;
        }
        
        // Nếu không có DropZone, tìm Member component
        Member member = GetComponentInParent<Member>();
        if (member != null)
        {
            // Quay về vị trí của member (formation position ban đầu)
            return member.transform.position;
        }
        
        // Nếu là boss hoặc không tìm thấy gì, quay về original position
        return originalPosition;
    }
    
    public IEnumerator PlayAttackAnimation(Transform target)
    {
        if (isAnimating) yield break;
        
        isAnimating = true;
        
        // 1. Chuẩn bị tấn công - phóng to và đổi màu
        yield return StartCoroutine(PrepareAttack());
        
        // 2. Lao về phía target
        Vector3 targetPosition = target.position;
        Vector3 attackPosition = CalculateAttackPosition(originalPosition, targetPosition);
        
        yield return StartCoroutine(MoveToTarget(attackPosition));
        
        // 3. Hiệu ứng impact
        yield return StartCoroutine(ImpactEffect());
        
        // 4. Thu nhỏ lại về kích thước ban đầu
        yield return StartCoroutine(ScaleBackToOriginal());
        
        // 5. Quay về vị trí formation
        yield return StartCoroutine(ReturnToFormationPosition());
        
        // 6. Khôi phục trạng thái ban đầu (màu sắc)
        ResetToOriginalState();
        
        isAnimating = false;
    }
    
    public IEnumerator PlayHitAnimation()
    {
        if (isAnimating) yield break;
        
        isAnimating = true;
        
        // Hiệu ứng bị đánh - đổi màu và rung lắc
        if (skeletonGraphic != null)
        {
            skeletonGraphic.color = hitColor;
        }
        
        // Rung lắc mạnh
        yield return StartCoroutine(ShakeEffect(hitEffectTime, 0.15f));
        
        // Khôi phục màu ban đầu
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
        
        Debug.Log($"{gameObject.name} PrepareAttack - From: {lockedOriginalScale} To: {targetScale}");
        
        while (elapsedTime < prepareTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / prepareTime;
            
            // Phóng to dần từ lockedOriginalScale
            transform.localScale = Vector3.Lerp(lockedOriginalScale, targetScale, t);
            
            // Đổi màu dần
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
            
            // Sử dụng ease-out curve để tạo chuyển động tự nhiên
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        transform.position = targetPosition;
    }
    
    private IEnumerator ImpactEffect()
    {
        // Rung lắc nhẹ khi đánh trúng
        yield return StartCoroutine(ShakeEffect(impactTime, 0.08f));
    }
    
    private IEnumerator ScaleBackToOriginal()
    {
        Vector3 currentScale = transform.localScale;
        Vector3 targetScale = lockedOriginalScale * scaleBackMultiplier; // Thu nhỏ hơn ban đầu
        float elapsedTime = 0f;
        float duration = scaleBackTime; // Thời gian thu nhỏ lại từ setting
        
        Debug.Log($"{gameObject.name} ScaleBackToOriginal - From: {currentScale} To: {targetScale} (original: {lockedOriginalScale})");
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Thu nhỏ dần về targetScale (nhỏ hơn ban đầu)
            transform.localScale = Vector3.Lerp(currentScale, targetScale, t);
            
            // Đổi màu về ban đầu dần
            if (skeletonGraphic != null)
            {
                skeletonGraphic.color = Color.Lerp(attackColor, originalColor, t);
            }
            
            yield return null;
        }
        
        // Đảm bảo về đúng kích thước nhỏ và màu ban đầu
        transform.localScale = targetScale;
        if (skeletonGraphic != null)
        {
            skeletonGraphic.color = originalColor;
        }
        
        Debug.Log($"{gameObject.name} scaled back to smaller size: {targetScale} (was {lockedOriginalScale})");
    }
    
    private IEnumerator ReturnToFormationPosition()
    {
        Vector3 startPosition = transform.position;
        
        // Cập nhật formation position để đảm bảo có vị trí mới nhất
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
        
        // Cập nhật original position thành formation position
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
            randomOffset.z = 0; // Chỉ rung trong mặt phẳng 2D
            transform.position = originalPos + randomOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }
    
    private void ResetToOriginalState()
    {
        // Chỉ đảm bảo vị trí đúng, scale đã được set trong ScaleBackToOriginal()
        Vector3 targetPos = GetFormationPosition();
        transform.position = targetPos;
        
        // Đảm bảo màu đúng
        if (skeletonGraphic != null && skeletonGraphic.color != originalColor)
        {
            skeletonGraphic.color = originalColor;
            Debug.LogWarning($"{gameObject.name} had to force reset color");
        }
        
        Debug.Log($"{gameObject.name} final reset - Position: {targetPos}, Scale: {transform.localScale}");
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
    
    // Method để force reset scale về ban đầu
    public void ForceResetScale()
    {
        transform.localScale = originalScale;
        Debug.Log($"{gameObject.name} force reset scale to {originalScale}");
    }
    
    // Method để force set originalScale đúng cho team slimes
    public void ForceSetOriginalScale()
    {
        // Team slimes phải có scale 1.3f
        var isEnemy = GetComponent<SlimeStats>()?.isEnemy ?? false;
        if (!isEnemy)
        {
            originalScale = Vector3.one * 1.3f; // Force set 1.3f cho team slimes
        }
        else
        {
            originalScale = Vector3.one; // Boss giữ 1.0f
        }
        
        transform.localScale = originalScale;
        Debug.Log($"{gameObject.name} force set original scale to {originalScale} (isEnemy: {isEnemy})");
    }
    
    // Method để check và fix scale nếu bị sai
    [ContextMenu("Check And Fix Scale")]
    public void CheckAndFixScale()
    {
        var isEnemy = GetComponent<SlimeStats>()?.isEnemy ?? false;
        Vector3 expectedScale = isEnemy ? Vector3.one : Vector3.one * 1.3f;
        
        Debug.Log($"=== {gameObject.name} Scale Check ===");
        Debug.Log($"Current Scale: {transform.localScale}");
        Debug.Log($"Original Scale: {originalScale}");
        Debug.Log($"Locked Original Scale: {lockedOriginalScale}");
        Debug.Log($"Expected Scale: {expectedScale}");
        Debug.Log($"Is Enemy: {isEnemy}");
        
        if (Vector3.Distance(lockedOriginalScale, expectedScale) > 0.01f)
        {
            Debug.LogWarning($"Locked scale is wrong! Fixing from {lockedOriginalScale} to {expectedScale}");
            originalScale = expectedScale;
            lockedOriginalScale = expectedScale;
            transform.localScale = expectedScale;
        }
        else
        {
            Debug.Log("Scale is correct!");
        }
    }
    
    // Method để cập nhật formation position khi cần
    public void UpdateFormationPosition()
    {
        Vector3 newFormationPos = GetFormationPosition();
        formationPosition = newFormationPos;
        
        // Chỉ cập nhật original position nếu đây không phải là boss
        var isEnemy = GetComponent<SlimeStats>()?.isEnemy ?? false;
        if (!isEnemy)
        {
            originalPosition = newFormationPos;
        }
        
        Debug.Log($"{gameObject.name} updated formation position to {newFormationPos} (isEnemy: {isEnemy})");
    }
    
    // Method được gọi khi slime được drag vào formation slot mới
    public void OnDroppedToFormation()
    {
        // Delay một chút để đảm bảo transform đã được cập nhật
        StartCoroutine(DelayedUpdateFormationPosition());
    }
    
    private IEnumerator DelayedUpdateFormationPosition()
    {
        yield return new WaitForEndOfFrame();
        UpdateFormationPosition();
    }
    
    // Debug method để kiểm tra formation position
    [ContextMenu("Debug Formation Position")]
    public void DebugFormationPosition()
    {
        var dropZone = GetComponentInParent<DropZone>();
        var member = GetComponentInParent<Member>();
        var slimeStats = GetComponent<SlimeStats>();
        var currentFormationPos = GetFormationPosition();
        
        Debug.Log($"=== {gameObject.name} Formation Debug ===");
        Debug.Log($"Current Position: {transform.position}");
        Debug.Log($"Current Scale: {transform.localScale}");
        Debug.Log($"Original Position: {originalPosition}");
        Debug.Log($"Original Scale: {originalScale}");
        Debug.Log($"Formation Position: {formationPosition}");
        Debug.Log($"Calculated Formation Position: {currentFormationPos}");
        Debug.Log($"Is Enemy: {slimeStats?.isEnemy ?? false}");
        Debug.Log($"Has DropZone Parent: {dropZone != null}");
        Debug.Log($"Has Member Parent: {member != null}");
        if (dropZone != null) Debug.Log($"DropZone Position: {dropZone.transform.position}");
        if (member != null) Debug.Log($"Member Position: {member.transform.position}");
    }
    
    // Test method để kiểm tra scale animation
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
        Debug.Log($"{gameObject.name} starting scale test - Original: {originalScale}");
        
        // Phóng to
        yield return StartCoroutine(PrepareAttack());
        Debug.Log($"{gameObject.name} after prepare attack - Scale: {transform.localScale}");
        
        yield return new WaitForSeconds(0.5f);
        
        // Thu nhỏ lại
        yield return StartCoroutine(ScaleBackToOriginal());
        Debug.Log($"{gameObject.name} after scale back - Scale: {transform.localScale}");
    }
    
    // Gọi khi slime bị destroy hoặc HP <= 0
    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
