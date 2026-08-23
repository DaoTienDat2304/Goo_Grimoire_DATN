using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class TowerFloorItem : MonoBehaviour
{
    [Header("Sprites by 5-floor group")]
    public Sprite[] clusterStepSprites = new Sprite[5];

    [Header("Star sprites")]
    public Sprite activeStarSprite;
    public Sprite inactiveStarSprite;

    [Header("3 Star Slots")]
    public Image star1;
    public Image star2;
    public Image star3;

    [Header("So Floor Ben Trong Button (TextMeshPro)")]
    public TMP_Text floorNumberText;

    [Header("Locked/Unlocked Colors")]
    public Color uncompletedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color completedColor   = Color.white;

    private TowerSlimeBosses.TowerFloor floorData;
    private TowerUIManager uiManager;

    public TowerSlimeBosses.TowerFloor FloorData => floorData;

    public void Setup(TowerSlimeBosses.TowerFloor floor, bool isCurrent, TowerUIManager manager)
    {
        floorData = floor;
        uiManager = manager;

        if (floor == null) return;

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

        int stars = 0;
        if (floor.completed)
        {
            stars = floor.stars > 0 ? floor.stars : TowerSlimeBosses.CalculateStars(floor.bestTurnCount);
        }

        EnsureStarUIExists();

        UpdateStarImage(star1, stars >= 1);
        UpdateStarImage(star2, stars >= 2);
        UpdateStarImage(star3, stars >= 3);
    }

    private void EnsureStarUIExists()
    {
        if (star1 != null && star2 != null && star3 != null) return;

        var childImages = GetComponentsInChildren<Image>(true);
        var starList = childImages.Where(img => img.gameObject != gameObject && img.name.ToLower().Contains("star")).ToArray();
        if (starList.Length >= 1 && star1 == null) star1 = starList[0];
        if (starList.Length >= 2 && star2 == null) star2 = starList[1];
        if (starList.Length >= 3 && star3 == null) star3 = starList[2];

        if (star1 != null && star2 != null && star3 != null) return;

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
