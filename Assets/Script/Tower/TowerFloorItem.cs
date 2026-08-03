using System.Linq;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Button))]
public class TowerFloorItem : MonoBehaviour
{
    [Header("Sprites Theo Nhóm 5 Màn (Step 1 -> 5)")]
    public Sprite[] clusterStepSprites = new Sprite[5];

    [Header("Sprite Sao Sáng / Sao Tối (Gán 1 lần)")]
    public Sprite activeStarSprite;      // Sprite Sao Sáng
    public Sprite inactiveStarSprite;    // Sprite Sao Tối

    [Header("3 Ô Ngôi Sao Phía Trên (Tự động tìm hoặc tự tạo nếu để trống)")]
    public Image star1;
    public Image star2;
    public Image star3;

    [Header("Số Tầng Bên Trong Nút")]
    public Text floorNumberText;

    [Header("Màu Tối Khi Chưa Mở Khóa / Sáng Khi Đã Mở Khóa hoặc Đã Thắng")]
    public Color uncompletedColor = new Color(0.45f, 0.45f, 0.45f, 1f); // Màu tối khi màn bị khóa
    public Color completedColor   = Color.white;                        // Màu sáng khi màn đã mở/đã thắng

    private TowerSlimeBosses.TowerFloor floorData;
    private TowerUIManager uiManager;

    public TowerSlimeBosses.TowerFloor FloorData => floorData;

    public void Setup(TowerSlimeBosses.TowerFloor floor, bool isCurrent, TowerUIManager manager)
    {
        floorData = floor;
        uiManager = manager;

        if (floor == null) return;

        // Cập nhật nhãn số tầng bên trong nút
        if (floorNumberText != null)
        {
            floorNumberText.text = $"{floor.floorNumber}";
            floorNumberText.raycastTarget = false;
        }

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();

            if (isCurrent && !floor.completed)
            {
                btn.onClick.AddListener(() => uiManager.OnStartBattle());
            }
            else if (floor.completed)
            {
                btn.onClick.AddListener(() => uiManager.OnReplayFloor(floor.floorNumber));
            }
            else
            {
                btn.onClick.AddListener(() => uiManager.OnLockedFloorClicked(floor.floorNumber));
            }
        }

        ApplyVisualState(floor, isCurrent);
    }

    private void ApplyVisualState(TowerSlimeBosses.TowerFloor floor, bool isCurrent)
    {
        int stepIndex = (floor.floorNumber - 1) % 5;

        int highest = (uiManager != null && uiManager.towerDatabase != null) ? uiManager.towerDatabase.highestFloorReached : 0;
        int current = (uiManager != null && uiManager.towerDatabase != null) ? uiManager.towerDatabase.currentFloor : 1;

        bool isUnlocked = floor.completed || isCurrent || (floor.floorNumber <= highest) || (floor.floorNumber <= current);

        Image nodeImage = GetComponent<Image>();
        if (nodeImage != null)
        {
            if (clusterStepSprites != null && clusterStepSprites.Length > stepIndex && clusterStepSprites[stepIndex] != null)
            {
                nodeImage.sprite = clusterStepSprites[stepIndex];
            }
            nodeImage.color = isUnlocked ? completedColor : uncompletedColor;
        }

        // Tính toán số Sao (1 -> 3 sao nếu đã thắng màn)
        int stars = 0;
        if (floor.completed)
        {
            stars = floor.stars > 0 ? floor.stars : TowerSlimeBosses.CalculateStars(floor.bestTurnCount);
        }

        // Tự động tìm hoặc sinh 3 ô Image Sao phía trên nếu chưa kéo thủ công
        EnsureStarUIExists();

        // Cập nhật Sprite Sao Sáng / Sao Tối cho 3 ngôi sao
        UpdateStarImage(star1, stars >= 1);
        UpdateStarImage(star2, stars >= 2);
        UpdateStarImage(star3, stars >= 3);
    }

    private void EnsureStarUIExists()
    {
        if (star1 != null && star2 != null && star3 != null) return;

        // Tìm trong các object con
        var childImages = GetComponentsInChildren<Image>(true);
        var starList = childImages.Where(img => img.gameObject != gameObject && img.name.ToLower().Contains("star")).ToArray();
        if (starList.Length >= 1 && star1 == null) star1 = starList[0];
        if (starList.Length >= 2 && star2 == null) star2 = starList[1];
        if (starList.Length >= 3 && star3 == null) star3 = starList[2];

        if (star1 != null && star2 != null && star3 != null) return;

        // Tự động sinh container chứa 3 ô Ngôi sao phía trên nút bấm nếu hoàn toàn chưa có
        Transform containerTransform = transform.Find("StarGroupContainer");
        GameObject containerGO;
        if (containerTransform != null)
        {
            containerGO = containerTransform.gameObject;
        }
        else
        {
            containerGO = new GameObject("StarGroupContainer", typeof(RectTransform));
            containerGO.transform.SetParent(transform, false);

            RectTransform containerRT = containerGO.GetComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 1f);
            containerRT.anchorMax = new Vector2(0.5f, 1f);
            containerRT.pivot     = new Vector2(0.5f, 0f);
            containerRT.anchoredPosition = new Vector2(0f, 10f);
            containerRT.sizeDelta = new Vector2(90f, 30f);
        }

        Image[] existingStars = containerGO.GetComponentsInChildren<Image>(true);
        if (existingStars.Length >= 3)
        {
            if (star1 == null) star1 = existingStars[0];
            if (star2 == null) star2 = existingStars[1];
            if (star3 == null) star3 = existingStars[2];
            return;
        }

        if (star1 == null) star1 = CreateSingleStarImage(containerGO.transform, "Star1", new Vector2(-26f, 0f));
        if (star2 == null) star2 = CreateSingleStarImage(containerGO.transform, "Star2", new Vector2(0f, 6f));
        if (star3 == null) star3 = CreateSingleStarImage(containerGO.transform, "Star3", new Vector2(26f, 0f));
    }

    private Image CreateSingleStarImage(Transform parent, string name, Vector2 pos)
    {
        GameObject starGO = new GameObject(name, typeof(RectTransform), typeof(Image));
        starGO.transform.SetParent(parent, false);

        RectTransform rt = starGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(24f, 24f);

        Image img = starGO.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private void UpdateStarImage(Image starImg, bool active)
    {
        if (starImg == null) return;

        if (active && activeStarSprite != null)
        {
            starImg.sprite = activeStarSprite;
            starImg.color = Color.white;
            starImg.gameObject.SetActive(true);
        }
        else if (inactiveStarSprite != null)
        {
            starImg.sprite = inactiveStarSprite;
            starImg.color = Color.white;
            starImg.gameObject.SetActive(true);
        }
        else
        {
            starImg.gameObject.SetActive(active);
        }
    }
}
