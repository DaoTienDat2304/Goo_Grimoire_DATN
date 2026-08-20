using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class SlimeDisplayBehavior : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Interaction Settings")]
    public float clickCooldown = 0.5f;
    public float hoverScale = 1.2f;
    public float scaleSpeed = 5f;
    
    [Header("Info Display")]

    public GameObject infoPanel;
    public float infoPanelOffset = 2f;
    
    private Slime slime;
    private Vector3 originalScale;
    private bool isHovered = false;
    private bool canClick = true;
    private Coroutine scaleCoroutine;
    
    private void Start()
    {
        originalScale = transform.localScale;
        
        if (infoPanel == null)
        {
            CreateInfoPanel();
        }
        
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }
    
    public void Initialize(Slime newSlime)
    {
        slime = newSlime;
        UpdateInfoPanel();
    }
    
    private void CreateInfoPanel()
    {
        infoPanel = new GameObject("SlimeInfoPanel");
        infoPanel.transform.SetParent(transform);
        infoPanel.transform.localPosition = Vector3.up * infoPanelOffset;
        
        // Background
        var background = infoPanel.AddComponent<SpriteRenderer>();
        background.sprite = CreatePanelSprite();
        background.color = new Color(0, 0, 0, 0.8f);
        background.sortingOrder = 20;
        
        var nameText = CreateTextMesh("Name", slime?.slimeName ?? "Unknown", 16, Color.white);
        nameText.transform.SetParent(infoPanel.transform);
        nameText.transform.localPosition = Vector3.up * 0.6f;
        
        // Generation
        var genText = CreateTextMesh("Generation", $"Gen {slime?.generation ?? 0}", 12, Color.yellow);
        genText.transform.SetParent(infoPanel.transform);
        genText.transform.localPosition = Vector3.up * 0.3f;
        
        // Stats
        var statsText = CreateTextMesh("Stats", GetStatsText(), 10, Color.cyan);
        statsText.transform.SetParent(infoPanel.transform);
        statsText.transform.localPosition = Vector3.zero;
        
        // Breeding status
        var breedingText = CreateTextMesh("Breeding", GetBreedingStatus(), 10, GetBreedingColor());
        breedingText.transform.SetParent(infoPanel.transform);
        breedingText.transform.localPosition = Vector3.up * -0.3f;
        
        // Experience
        var expText = CreateTextMesh("Experience", $"EXP: {slime?.experience ?? 0}", 10, Color.magenta);
        expText.transform.SetParent(infoPanel.transform);
        expText.transform.localPosition = Vector3.up * -0.6f;
        
        var panelRect = infoPanel.GetComponent<SpriteRenderer>();
        if (panelRect != null)
        {
            panelRect.size = new Vector2(3f, 2.5f);
        }
    }
    
    private TextMesh CreateTextMesh(string name, string text, int fontSize, Color color)
    {
        var textGO = new GameObject(name);
        var textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = color;
        textMesh.characterSize = 0.1f;
        textMesh.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        return textMesh;
    }
    
    private Sprite CreatePanelSprite()
    {
        int width = 64;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < 2 || x > width - 3 || y < 2 || y > height - 3)
                {
                    texture.SetPixel(x, y, new Color(1, 1, 1, 0.3f));
                }
                else
                {
                    texture.SetPixel(x, y, new Color(0, 0, 0, 0.8f));
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
    
    private string GetStatsText()
    {
        if (slime == null) return "HP: 0 ATK: 0 DEF: 0";
        int effAtk = BattleStatFormula.EffectiveAttack(slime.totalAttack, slime.totalCritRate, slime.totalCritDMG);
        float finalCritRate = BattleStatFormula.FinalCritRate(slime.totalCritRate);
        float finalCritDMG = BattleStatFormula.FinalCritDMG(slime.totalCritRate, slime.totalCritDMG);
        return $"HP: {slime.totalHP}  ATK: {effAtk}  Magic: {slime.totalMagicAttack}\n" +
               $"DEF: {slime.totalDefense}  SPD: {slime.totalSpeed}\n" +
               $"Crit Rate: {finalCritRate * 100f:0.#}%  Crit DMG: {finalCritDMG * 100f:0.#}%";
    }
    
    private string GetBreedingStatus()
    {
        if (slime == null) return "Unknown";
        return slime.canBreed ? "Ready to Breed" : $"Cooldown: {Mathf.CeilToInt(slime.breedingCooldown)}s";
    }
    
    private Color GetBreedingColor()
    {
        if (slime == null) return Color.white;
        return slime.canBreed ? Color.green : Color.red;
    }
    
    private void UpdateInfoPanel()
    {
        if (infoPanel == null) return;
        
        var nameText = infoPanel.transform.Find("Name")?.GetComponent<TextMesh>();
        if (nameText != null && slime != null)
        {
            nameText.text = slime.slimeName;
        }
        
        var genText = infoPanel.transform.Find("Generation")?.GetComponent<TextMesh>();
        if (genText != null && slime != null)
        {
            genText.text = $"Gen {slime.generation}";
        }
        
        var statsText = infoPanel.transform.Find("Stats")?.GetComponent<TextMesh>();
        if (statsText != null)
        {
            statsText.text = GetStatsText();
        }
        
        var breedingText = infoPanel.transform.Find("Breeding")?.GetComponent<TextMesh>();
        if (breedingText != null)
        {
            breedingText.text = GetBreedingStatus();
            breedingText.color = GetBreedingColor();
        }
        
        var expText = infoPanel.transform.Find("Experience")?.GetComponent<TextMesh>();
        if (expText != null && slime != null)
        {
            expText.text = $"EXP: {slime.experience}";
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canClick) return;
        
        canClick = false;
        StartCoroutine(ClickCooldown());
        
        if (infoPanel != null)
        {
            bool isActive = infoPanel.activeSelf;
            infoPanel.SetActive(!isActive);
            
            if (!isActive)
            {
                UpdateInfoPanel();
            }
        }
        
        StartCoroutine(ClickEffect());
        
        if (slime != null)
        {

        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale * hoverScale));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale));
    }
    
    private IEnumerator ScaleTo(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    private IEnumerator ClickCooldown()
    {
        yield return new WaitForSeconds(clickCooldown);
        canClick = true;
    }
    
    private IEnumerator ClickEffect()
    {
        Vector3 originalPos = transform.position;
        Vector3 bouncePos = originalPos + Vector3.up * 0.3f;
        
        // Bounce up
        float elapsed = 0f;
        float duration = 0.1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(originalPos, bouncePos, t);
            yield return null;
        }
        
        // Bounce down
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(bouncePos, originalPos, t);
            yield return null;
        }
        
        transform.position = originalPos;
    }
    
    private void OnDestroy()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
    }
}
