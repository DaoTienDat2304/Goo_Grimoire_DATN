using UnityEngine;
using UnityEngine.EventSystems;

public class SlimeClickHandler : MonoBehaviour, IPointerClickHandler
{
    private Slime slime;
    private GameObject infoPanel;
    private bool isInfoVisible = false;
    
    public void Initialize(Slime newSlime)
    {
        slime = newSlime;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleInfoPanel();
        
        // Log thông tin slime
        if (slime != null)
        {

        }
    }
    
    private void ToggleInfoPanel()
    {
        if (infoPanel == null)
        {
            CreateInfoPanel();
        }
        
        isInfoVisible = !isInfoVisible;
        infoPanel.SetActive(isInfoVisible);
        
        if (isInfoVisible)
        {
            UpdateInfoPanel();
        }
    }
    
    private void CreateInfoPanel()
    {
        infoPanel = new GameObject("WorldSlimeInfoPanel");
        infoPanel.transform.SetParent(transform);
        infoPanel.transform.localPosition = Vector3.up * 2f;
        
        // Background
        var background = infoPanel.AddComponent<SpriteRenderer>();
        background.sprite = CreatePanelSprite();
        background.color = new Color(0, 0, 0, 0.9f);
        background.sortingOrder = 30;
        
        // Tên slime
        var nameText = CreateTextMesh("Name", slime?.slimeName ?? "Unknown", 16, Color.white);
        nameText.transform.SetParent(infoPanel.transform);
        nameText.transform.localPosition = Vector3.up * 0.8f;
        
        // Generation
        var genText = CreateTextMesh("Generation", $"Gen {slime?.generation ?? 0}", 12, Color.yellow);
        genText.transform.SetParent(infoPanel.transform);
        genText.transform.localPosition = Vector3.up * 0.4f;
        
        // Stats
        var statsText = CreateTextMesh("Stats", GetStatsText(), 10, Color.cyan);
        statsText.transform.SetParent(infoPanel.transform);
        statsText.transform.localPosition = Vector3.zero;
        
        // Breeding status
        var breedingText = CreateTextMesh("Breeding", GetBreedingStatus(), 10, GetBreedingColor());
        breedingText.transform.SetParent(infoPanel.transform);
        breedingText.transform.localPosition = Vector3.up * -0.4f;
        
        // Experience
        var expText = CreateTextMesh("Experience", $"EXP: {slime?.experience ?? 0}", 10, Color.magenta);
        expText.transform.SetParent(infoPanel.transform);
        expText.transform.localPosition = Vector3.up * -0.8f;
        
        // Thiết lập kích thước panel
        var panelRect = infoPanel.GetComponent<SpriteRenderer>();
        if (panelRect != null)
        {
            panelRect.size = new Vector2(4f, 3f);
        }
        
        // Ẩn panel ban đầu
        infoPanel.SetActive(false);
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
                    texture.SetPixel(x, y, new Color(1, 1, 1, 0.5f));
                }
                else
                {
                    texture.SetPixel(x, y, new Color(0, 0, 0, 0.9f));
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
    
    private string GetStatsText()
    {
        if (slime == null) return "HP: 0 ATK: 0 DEF: 0";
        return $"HP: {slime.totalHP} ATK: {slime.totalAttack} DEF: {slime.totalDefense}";
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
        if (infoPanel == null || slime == null) return;
        
        // Cập nhật các text
        var nameText = infoPanel.transform.Find("Name")?.GetComponent<TextMesh>();
        if (nameText != null)
        {
            nameText.text = slime.slimeName;
        }
        
        var genText = infoPanel.transform.Find("Generation")?.GetComponent<TextMesh>();
        if (genText != null)
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
        if (expText != null)
        {
            expText.text = $"EXP: {slime.experience}";
        }
    }
    
    private void OnDestroy()
    {
        if (infoPanel != null)
        {
            Destroy(infoPanel);
        }
    }
}
