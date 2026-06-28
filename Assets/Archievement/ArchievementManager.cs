using System.Collections.Generic;
using System.Net.Sockets;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArchievementManager : MonoBehaviour
{
    public List<ArchievementPre> listArchievement;
    public GameObject ArchievementPrefab;
    public GameObject visualprefab;
    public Dictionary<string,Archievement> disarchievement = new Dictionary<string,Archievement>();
    public CanvasGroup CanvasGroup;
    public SlimeWorldManager slimeWorldManager;

    public Sprite unlockSprite;
    public Sprite coin;
    public Sprite gem;
    [Header("Sorting")]
    public int sortingOrder = 9999; // bảo đảm Achievement UI trên cùng
    [Header("Options")]
    [Tooltip("Nếu bật, mỗi lần nhấn Play tất cả thành tựu sẽ được reset (xóa PlayerPrefs).")]
    public bool resetAchievementsOnPlay = true;
    private static ArchievementManager instance;
    public Button archivementButton;
    public Button closeButton;
    public static ArchievementManager Instance { get; private set; }

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        archivementButton.onClick.AddListener(hideUI);
        closeButton.onClick.AddListener(hideUI);

        // Đồng bộ trạng thái raycast theo alpha hiện tại
        if (CanvasGroup != null)
        {
            bool visible = CanvasGroup.alpha > 0.99f;
            CanvasGroup.blocksRaycasts = visible;
            CanvasGroup.interactable = visible;
        }

        // Bảo đảm Canvas của Achievement nằm trên cùng và có raycaster
        EnsureTopSorting();

        foreach (ArchievementPre pre in listArchievement)
        {
            createprefab(pre);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    [ContextMenu("Reset All Achievements")]
    public void ResetAllAchievements()
    {
        if (listArchievement == null) return;
        foreach (ArchievementPre pre in listArchievement)
        {
            if (pre != null && !string.IsNullOrEmpty(pre.name))
            {
                PlayerPrefs.DeleteKey(pre.name);
            }
        }
        PlayerPrefs.Save();

        // Cập nhật lại UI nếu đã khởi tạo
        foreach (var kv in disarchievement)
        {
            var a = kv.Value;
            if (a != null && a.ArchievementRef != null)
            {
                var img = a.ArchievementRef.GetComponent<Image>();
                if (img != null) img.color = Color.white;
                var icon = a.ArchievementRef.transform.GetChild(2).GetComponentInChildren<Image>();
                if (icon != null) icon.color = new Color(1,1,1,0.3f);
            }
        }
    }

    void createprefab(ArchievementPre pre,int progresstion = 0)
    {
        string progess = progresstion > 0 ? "" : string.Empty ;

        GameObject archieve = Instantiate(ArchievementPrefab);
        archieve.transform.GetChild(0).GetComponentInChildren<Text>().text = pre.title;
        archieve.transform.GetChild(1).GetComponentInChildren<Text>().text = pre.description;
        archieve.transform.GetChild(2).GetComponentInChildren<Image>().sprite = pre.sprite;
        //archieve.transform.GetChild(3).GetComponentInChildren<Image>().sprite = pre.currencyReward.GetRewardtype() == "Coins" ? coin:gem;
        archieve.transform.GetChild(4).GetComponentInChildren<Text>().text = pre.currencyReward.GetRewardDescription();
        archieve.name = pre.name;
        Archievement newarchievement = new Archievement(pre.name, pre.description, archieve,pre.targetValue);
        disarchievement.Add(pre.title, newarchievement);
        var general = GameObject.Find("general");
        if (general != null)
        {
            archieve.transform.SetParent(general.transform, false);
            archieve.transform.SetAsLastSibling();
        }
        archieve.transform.localScale = new Vector3(1, 1, 1);
        
        // Lưu reference đến currency reward để sử dụng sau
        if (pre.currencyReward != null)
        {
            // Tạo một component tạm để lưu currency reward
            var rewardHolder = archieve.AddComponent<AchievementRewardHolder>();
            rewardHolder.currencyReward = pre.currencyReward;
        }
    }

    private void EnsureTopSorting()
    {
        // Chỉ áp dụng sorting cho panel của Achievement (CanvasGroup holder)
        var target = CanvasGroup != null ? CanvasGroup.gameObject : this.gameObject;
        var canvas = target.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = target.AddComponent<Canvas>();
        }

        // Đảm bảo có GraphicRaycaster để panel chặn click xuống dưới
        if (target.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            target.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Không thay đổi render mode của Canvas cha; chỉ overrideSorting tại panel này
        // Giữ nguyên renderMode hiện tại của canvas con
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        // Đồng bộ sorting layer với Canvas cha (nếu có) để giữ nguyên layer
        var parentCanvas = target.transform.parent != null ? target.transform.parent.GetComponentInParent<Canvas>() : null;
        if (parentCanvas != null)
        {
            canvas.sortingLayerID = parentCanvas.sortingLayerID;
        }

        // Đưa panel lên cuối cùng trong hierarchy để đảm bảo ở trên trong cùng layer
        target.transform.SetAsLastSibling();

        // Đảm bảo có EventSystem trong scene để nhận input
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }
    }

    public void GetArchivement(int dex)
    {
        switch(dex)
        {
            case 0:
                EarnArchievement("Breed");
                EarnArchievement("Breed 2");
                EarnArchievement("Breed 3");
                break;
            case 1:
                EarnArchievement("secret");
                EarnArchievement("secret 2");
                EarnArchievement("secret 3");
                break;
            case 2:
                EarnArchievement("build 2");
                EarnArchievement("build");
                break;
            case 3:
                EarnArchievement("quest 2");
                EarnArchievement("quest");
                Debug.Log("test quest");
                break;
        }
    }    

    void EarnArchievement(string Title)
    {
        if (disarchievement[Title].EarnArchievement())
        {
            Debug.Log("get Something");

            // Thưởng currency nếu có
            var achievement = disarchievement[Title];
            var rewardHolder = achievement.ArchievementRef.GetComponent<AchievementRewardHolder>();
            if (rewardHolder != null && rewardHolder.currencyReward != null)
            {
                achievement.GiveCurrencyReward(rewardHolder.currencyReward);
                Debug.Log($"Achievement '{Title}' đã thưởng: {rewardHolder.currencyReward.GetRewardDescription()}");
            }

            SaveAndLoadSystem.Instance?.Save();
        }
    }

    public void hideUI()
    {
        if (CanvasGroup != null && CanvasGroup.alpha == 0)
        {
            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;
        }
        else
        {
            CanvasGroup.alpha = 0;
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
        }
    }


    
}
