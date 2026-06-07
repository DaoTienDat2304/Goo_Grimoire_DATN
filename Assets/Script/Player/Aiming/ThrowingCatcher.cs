using System.Collections;
using UnityEngine;

public class ThrowingCatcher : MonoBehaviour
{
    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    public GameObject tamingPanel;
    [SerializeField] private PlayerMovement playerMovement;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        col = GetComponent<CapsuleCollider2D>();
        col.enabled = false;
        playerMovement = GameObject.FindAnyObjectByType<PlayerMovement>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void throwCatcher(Vector2 direction, float force)
    {
        col.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        col.enabled = true;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        StartCoroutine(DestroyCatcher());
    }

    IEnumerator DestroyCatcher()
    {
        yield return new WaitForSeconds(0.55f);
        Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Slime"))
        {
            // Kiểm tra loại slime (friendly/aggressive)
            WildSlimeTraits wildSlimeTraits = collision.gameObject.GetComponent<WildSlimeTraits>();
            
            if (wildSlimeTraits != null)
            {
                WildSlimeType slimeType = wildSlimeTraits.GetSlimeType();
                
                // Chỉ bật taming panel nếu là friendly slime
                if (slimeType == WildSlimeType.Friendly)
                {
                    if (tamingPanel != null)
                        tamingPanel.SetActive(true); // hiện menu
                    playerMovement.enabled = false;
                }
                // Nếu là aggressive, không làm gì ở đây (WildSlimeTraits sẽ xử lý)
            }
            else
            {
                // Fallback: nếu không tìm thấy WildSlimeTraits, giữ logic cũ (bật panel)
                if (tamingPanel != null)
                    tamingPanel.SetActive(true);
                playerMovement.enabled = false;
            }

            Destroy(gameObject); // huỷ đạn
        }
    }
}