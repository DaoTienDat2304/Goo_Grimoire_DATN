using UnityEngine;
using UnityEngine.UI;

public class BuildingUI : MonoBehaviour
{
    private Building building;
    public Image buildingImage;
    public Text nameText;
    private bool isDimmed = false;
    private const float dimmedAlpha = 0.4f; // Độ trong suốt khi đã đặt (nhạt hơn)
    private const float normalAlpha = 1f; // Độ trong suốt bình thường

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 200f);
    }

    // Update is called once per frame
    void Update()
    {
        if (building != null && buildingImage != null)
        {
            transform.GetChild(1).GetComponent<Image>().sprite = building.sprite;
        }
    }
    public void SetupBuilding(Building newBuilding)
    {
        building = newBuilding;
        UpdateUI();
    }
    public void UpdateUI()
    {
        if (building == null) return;
        //GetComponentInChildren<Image>().sprite = building.sprite;
        if (nameText != null) nameText.text = building.buildingName + "\nPrice: " + building.currencyCosts;
    }

    /// <summary>
    /// Đặt trạng thái làm nhạt màu cho building (khi đã được đặt)
    /// </summary>
    public void SetDimmed(bool dimmed)
    {
        isDimmed = dimmed;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        float alpha = isDimmed ? dimmedAlpha : normalAlpha;
        
        // Cập nhật màu cho building image
        if (buildingImage != null)
        {
            Color color = buildingImage.color;
            color.a = alpha;
            buildingImage.color = color;
        }

        // Cập nhật màu cho tất cả các Image con (bao gồm icon)
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

        // Cập nhật màu cho text nếu có
        if (nameText != null)
        {
            Color textColor = nameText.color;
            textColor.a = alpha;
            nameText.color = textColor;
        }
    }
}
