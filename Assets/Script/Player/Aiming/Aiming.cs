using UnityEngine;
using UnityEngine.SceneManagement;

public class Aiming : MonoBehaviour
{
    private const float VirtualThrowReleaseThreshold = 12f;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform startPosition;

    private Vector2 aimingLinePosition;
    private bool clickedWithinArea;
    [SerializeField] private float maxLength = 5f;
    [SerializeField] private Transform idlePosition;
    [SerializeField] private aimingArea aimingarea;
    [SerializeField] private ThrowingCatcher catcherPrefab;
    [SerializeField] private float force = 8f;
    [SerializeField] private GameObject tamingUI;
    [SerializeField] private int marshmallowCostPerThrow = 1; // Số marshmallow cần để ném 1 catcher
    private ThrowingCatcher spawnedCatcher;
    private bool spriteFacesRight = true;
    
    /// <summary>
    /// Kiểm tra xem có đang ở travel scene không (không trừ marshmallow trong travel scene)
    /// </summary>
    private bool IsTravelScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == "travelSence" || sceneName.ToLower().Contains("travel");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        lineRenderer.enabled = false;

    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MobileInput.TryGetAimPointer(out var pointerPosition, out bool pointerPressed, out bool pointerHeld, out bool pointerReleased);
        bool isVirtualThrowButton = MobileInput.LastAimPointerFromVirtualButton;

        spriteFacesRight = (lineRenderer.GetPosition(0).x > lineRenderer.GetPosition(1).x);
        if (pointerPressed && (isVirtualThrowButton || aimingarea.isWithinArea(pointerPosition)))
        {
            // Trong travel scene thì không cần check marshmallow
            bool isTravel = IsTravelScene();
            bool canSpawn = isTravel || 
                           (ResourceManager.Instance != null && 
                            ResourceManager.Instance.HasEnoughResource(ResourceType.Marshmallow, marshmallowCostPerThrow));
            
            if (canSpawn)
            {
                clickedWithinArea = true;
                SpawnCatcher();
            }
            else
            {
                Debug.LogWarning("Không đủ Marshmallow để ném catcher!");
            }
        }
        if (pointerHeld && clickedWithinArea)
        {
            if (isVirtualThrowButton)
                DrawVirtualLine(MobileInput.VirtualAimDragVector);
            else
                DrawLine(pointerPosition);

            CatcherRotation();
            spawnedCatcher.transform.SetParent(this.transform);
        }
        if (pointerReleased)
        {
            clickedWithinArea = false;

            if (spawnedCatcher != null && isVirtualThrowButton && MobileInput.VirtualAimDragVector.magnitude < VirtualThrowReleaseThreshold)
            {
                Destroy(spawnedCatcher.gameObject);
                spawnedCatcher = null;
                SetLine(startPosition.position);
                lineRenderer.enabled = false;
                return;
            }

            if (spawnedCatcher != null) // chỉ bắn khi còn object
            {
                // Tiêu hao marshmallow khi ném catcher (không trừ trong travel scene)
                bool isTravel = IsTravelScene();
                if (!isTravel && ResourceManager.Instance != null)
                {
                    bool spent = ResourceManager.Instance.SpendResource(ResourceType.Marshmallow, marshmallowCostPerThrow);
                    if (!spent)
                    {
                        // Nếu không đủ marshmallow, hủy việc ném và destroy catcher
                        Debug.LogWarning("Không đủ Marshmallow! Hủy ném catcher.");
                        Destroy(spawnedCatcher.gameObject);
                        spawnedCatcher = null;
                        SetLine(startPosition.position);
                        lineRenderer.enabled = false;
                        return;
                    }
                }

                spawnedCatcher.transform.SetParent(null);
                Vector3 a = lineRenderer.GetPosition(0);
                Vector3 b = lineRenderer.GetPosition(1);
                Vector2 dir = (b - a);

                spawnedCatcher.throwCatcher(dir, force);
                spawnedCatcher = null; // reset biến để tránh reuse sau khi đã destroy

                // Play catcher throw sound effect
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayCatcherThrowSFX();
                }
            }

            SetLine(startPosition.position);
            lineRenderer.enabled = false; // tắt hẳn line khi thả chuột
        }
    }
    private void DrawLine(Vector2 screenPosition)
    {
        if (Camera.main == null) return;

        Vector3 touchPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        aimingLinePosition = startPosition.position + Vector3.ClampMagnitude(touchPosition - startPosition.position, maxLength);
        SetLine(aimingLinePosition);
    }
    private void DrawVirtualLine(Vector2 dragVector)
    {
        if (Camera.main == null) return;

        Vector3 startScreen = Camera.main.WorldToScreenPoint(startPosition.position);
        Vector3 endScreen = startScreen + new Vector3(dragVector.x, dragVector.y, 0f);
        Vector3 startWorld = Camera.main.ScreenToWorldPoint(startScreen);
        Vector3 endWorld = Camera.main.ScreenToWorldPoint(endScreen);

        aimingLinePosition = startPosition.position + Vector3.ClampMagnitude(endWorld - startWorld, maxLength);
        SetLine(aimingLinePosition);
    }
    private void SetLine(Vector2 position)
    {
        if (!lineRenderer.enabled) lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, position);
        lineRenderer.SetPosition(1, startPosition.position);
    }
    private void SpawnCatcher()
    {
        SetLine(idlePosition.position);
        spawnedCatcher = Instantiate(catcherPrefab, idlePosition.position, Quaternion.identity);
        spawnedCatcher.tamingPanel = tamingUI;
        spawnedCatcher.transform.localScale = Vector3.one*0.15f;
    }
    private void CatcherRotation()
    {
        Vector3 a = lineRenderer.GetPosition(0);
        Vector3 b = lineRenderer.GetPosition(1);
        Vector2 dir = (b - a).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (spriteFacesRight)
            spawnedCatcher.transform.rotation = Quaternion.Euler(0, 0, angle);
        else
            spawnedCatcher.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
}
