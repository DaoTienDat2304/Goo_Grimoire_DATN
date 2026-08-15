using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>A clickable egg that waits on the world map until the player collects it.</summary>
public class WorldEggPickup : MonoBehaviour, IPointerClickHandler
{
    private const int WorldEggSortingOrder = -99;

    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private Vector2 visualSize = new Vector2(0.9f, 1.15f);

    private string eggId;
    private SpriteRenderer spriteRenderer;
    private Vector3 restingPosition;
    private bool isCollected;
    private static Sprite generatedEggSprite;

    public string EggId => eggId;
    public Sprite Icon => spriteRenderer != null ? spriteRenderer.sprite : null;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetOrCreateEggSprite();
        spriteRenderer.sortingOrder = WorldEggSortingOrder;

        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider == null)
        {
            CapsuleCollider2D capsule = gameObject.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = visualSize;
        }
    }

    public void Initialize(string id, Vector3 worldPosition)
    {
        eggId = id;
        transform.position = worldPosition;
        restingPosition = worldPosition;
        isCollected = false;
    }

    private void Update()
    {
        if (isCollected) return;
        float offset = Mathf.Sin((Time.unscaledTime + GetInstanceID() * 0.01f) * bobSpeed) * bobHeight;
        transform.position = restingPosition + Vector3.up * offset;
    }

    public void OnPointerClick(PointerEventData eventData) => TryCollect();

    // Keeps collection working when the gameplay camera has no Physics2DRaycaster.
    private void OnMouseDown() => TryCollect();

    private void TryCollect()
    {
        if (isCollected || string.IsNullOrEmpty(eggId) || SlimeEggSystem.Instance == null)
            return;

        isCollected = true;
        if (!SlimeEggSystem.Instance.CollectWorldEgg(eggId, transform.position, Icon))
            isCollected = false;
    }

    private static Sprite GetOrCreateEggSprite()
    {
        if (generatedEggSprite != null) return generatedEggSprite;

        const int width = 64;
        const int height = 80;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "GeneratedEggTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color shell = new Color32(202, 165, 229, 255);
        Color shade = new Color32(151, 105, 190, 255);
        Color spot = new Color32(255, 236, 176, 255);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float ny = (y - height * 0.48f) / (height * 0.48f);
            float halfWidth = Mathf.Sqrt(Mathf.Max(0f, 1f - ny * ny)) * (0.32f + (1f - (ny + 1f) * 0.5f) * 0.16f);
            for (int x = 0; x < width; x++)
            {
                float nx = Mathf.Abs((x - width * 0.5f) / width);
                Color color = clear;
                if (nx <= halfWidth)
                {
                    color = Color.Lerp(shade, shell, Mathf.Clamp01((float)x / width + 0.25f));
                    float dx = x - width * 0.37f;
                    float dy = y - height * 0.42f;
                    if (dx * dx + dy * dy < 22f) color = spot;
                    dx = x - width * 0.61f;
                    dy = y - height * 0.65f;
                    if (dx * dx + dy * dy < 14f) color = spot;
                }
                pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        generatedEggSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
        generatedEggSprite.name = "GeneratedEggSprite";
        generatedEggSprite.hideFlags = HideFlags.HideAndDontSave;
        return generatedEggSprite;
    }
}
