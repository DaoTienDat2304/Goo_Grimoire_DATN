using UnityEngine;
using UnityEngine.UI;

public class BuildingUI : MonoBehaviour
{
    private Building building;
    public Image buildingImage;
    public Text nameText;
    private bool isDimmed = false;
    private const float dimmedAlpha = 0.4f;
    private const float normalAlpha = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 200f);
    }

    public void SetupBuilding(Building newBuilding)
    {
        building = newBuilding;
        if (building != null && buildingImage != null)
            buildingImage.sprite = building.sprite;
        UpdateUI();
    }
    public void UpdateUI()
    {
        if (building == null) return;
        //GetComponentInChildren<Image>().sprite = building.sprite;
        if (nameText != null) nameText.text = building.buildingName + "\nPrice: " + building.currencyCosts;
    }
    public void SetDimmed(bool dimmed)
    {
        isDimmed = dimmed;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        float alpha = isDimmed ? dimmedAlpha : normalAlpha;
        
        if (buildingImage != null)
        {
            Color color = buildingImage.color;
            color.a = alpha;
            buildingImage.color = color;
        }

        Image[] images = GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            if (img != null)
            {
                Color imgColor = img.color;
                imgColor.a = alpha;
                img.color = imgColor;
            }
        }

        if (nameText != null)
        {
            Color textColor = nameText.color;
            textColor.a = alpha;
            nameText.color = textColor;
        }
    }
}
