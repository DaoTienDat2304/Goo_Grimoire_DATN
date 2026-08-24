using System.Collections;
using UnityEngine;

public class ThrowingCatcher : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private float minStraightSpeed = 6f;
    [SerializeField] private float maxStraightSpeed = 24f;
    [SerializeField] private float dragDistanceToSpeed = 1.8f;
    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    public GameObject tamingPanel;
    [SerializeField] private PlayerMovement playerMovement;
    private bool hasHitSlime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        col = GetComponent<CapsuleCollider2D>();
        col.enabled = false;
        playerMovement = GameObject.FindAnyObjectByType<PlayerMovement>();
        if (tamingPanel == null)
            tamingPanel = TamingPanelFlow.GetCanonicalPanel();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void throwCatcher(Vector2 direction, float force)
    {
        col.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        col.enabled = true;
        StartCoroutine(StraightFlight(direction, force));
    }

    IEnumerator StraightFlight(Vector2 direction, float force)
    {
        Vector2 moveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        float speed = Mathf.Clamp(direction.magnitude * dragDistanceToSpeed + force, minStraightSpeed, maxStraightSpeed);
        float timeLeft = lifetime;

        while (timeLeft > 0f)
        {
            Vector3 delta = (Vector3)(moveDirection * speed * Time.deltaTime);
            transform.position += delta;
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHitSlime || !collision.gameObject.CompareTag("Slime"))
            return;

        hasHitSlime = true;
        WildSlimeTraits wildSlimeTraits = collision.gameObject.GetComponent<WildSlimeTraits>();

        if (wildSlimeTraits != null && wildSlimeTraits.GetSlimeType() == WildSlimeType.Friendly)
        {
            bool opened = TamingPanelFlow.OpenFor(wildSlimeTraits);
            if (!opened)
                Debug.LogWarning("[TamingPanel] Catcher hit a friendly slime, but no usable TamingPanel was found.", this);
        }
        else if (wildSlimeTraits == null)
        {
            tamingManager manager = tamingPanel != null ? tamingPanel.GetComponent<tamingManager>() : null;
            if (manager != null)
                manager.BeginTaming(0, null);
            else if (tamingPanel != null)
            {
                tamingPanel.SetActive(true);
                if (playerMovement != null)
                    playerMovement.enabled = false;
            }
        }

        Destroy(gameObject);
    }
}
