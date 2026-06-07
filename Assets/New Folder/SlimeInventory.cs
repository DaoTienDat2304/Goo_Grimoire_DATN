using Spine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SlimeInventory : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject breedingPanel;
    public GameObject slimeCollectionPanel;
    public GameObject breedingProgressPanel;
    public Button button;
    public GameObject showslot;

    [Header("Breeding UI")]
    public Sprite slotsprite;

    [Header("Collection UI")]
    public Transform collectionGridParent;
    public GameObject collectionSlotPrefab;

    [Header("Slime Counter UI")]
    public Text slimeCounterText;
    public GameObject messagePanel;
    public Text messageText;

    private List<GameObject> slimeSlots = new List<GameObject>();
    private List<GameObject> collectionSlots = new List<GameObject>();
    public float interval = 1f;
    public bool panelBreedingActive;
    int maxsacrifice = 100;
    public int sacrifice;
    public Slider Slider;

    private void Awake()
    {
        EnsureRuntimeFallbacks();
    }

    private void Start()
    {
        RefreshAllUI();
        StartCoroutine(Countdown());
    }

    private void OnEnable()
    {
        RefreshAllUI();
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(1);

        // Kiểm tra xem BreedingManager đã tạo slimes chưa
        if (BreedingManager.Instance != null)
        {
            var allSlimes = BreedingManager.Instance.GetAllSlimes();
            int slimeCount = allSlimes.Count;

            if (slimeCount == 0)
            {
                yield return new WaitForSeconds(2); // Đợi thêm 2 giây nữa
            }
        }

        RefreshAllUI(); // Refresh UI để đọc slimes đã được tạo sẵn
    }

    private void Update()
    {
        /*UpdateSlimeCounter(); // Cập nhật counter liên tục
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f; // reset
            RefreshCollectionGrid();

            // Kiểm tra và refresh nếu có slimes mới được tạo
            CheckAndRefreshIfNeeded();
        }*/
        if(Slider.value >= 100)
        {
            button.gameObject.SetActive(true);
        }
        else
        {
            button.gameObject.SetActive(false);
        }
        
        if(Slider.value < sacrifice)
        {
            Slider.value += 60 * Time.deltaTime;
        }
    }

    public void summonbutton()
    {
        sacrifice -= maxsacrifice;
        Slider.value = sacrifice;
    }


    public void ondeseclect()
    {
        foreach (GameObject inventorySlot in collectionSlots)
        {
            InventorySlot i = inventorySlot.GetComponent<InventorySlot>();
            i.onselect = false;
            Debug.Log("can not");
        }
        RefreshCollectionGrid();
    }
    public void ondelete()
    {
        foreach (GameObject inventorySlot in collectionSlots)
        {
            InventorySlot i = inventorySlot.GetComponent<InventorySlot>();
            i.removedslime();
            Debug.Log("can not");
        }
        RefreshCollectionGrid();
        // Kiểm tra và refresh nếu có slimes mới được tạo
        CheckAndRefreshIfNeeded();
    }

    private int lastKnownSlimeCount = 0;
    private void CheckAndRefreshIfNeeded()
    {
        if (BreedingManager.Instance != null)
        {
            int currentCount = BreedingManager.Instance.GetAllSlimes().Count;
            if (currentCount != lastKnownSlimeCount)
            {
                lastKnownSlimeCount = currentCount;
                RefreshAllUI();
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }


    private void EnsureRuntimeFallbacks()
    {
        // Create a basic Canvas and EventSystem if none present
        if (FindAnyObjectByType<Canvas>() == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        var canvasRoot = FindAnyObjectByType<Canvas>().transform;

        // Ensure a grid parents exist if not assigned
     

        // Panels and basic controls if missing
       
        if (slimeCollectionPanel == null)
        {
            slimeCollectionPanel = CreatePanel(canvasRoot, "CollectionPanel");
            collectionGridParent.SetParent(slimeCollectionPanel.transform, false);
        }




        // Create simple runtime slot prefab if none assigned

        // Hook up button events if created at runtime
     

        // Default visible panels
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.1f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(10f, 10f);
        rt.offsetMax = new Vector2(-10f, -10f);
        return go;
    }
    public void RefreshAllUI()
    {
        RefreshSlimeGrid();
        RefreshCollectionGrid();
        UpdateSlimeCounter();
    }

    private void RefreshSlimeGrid()
    {
        // Clear existing slots
        foreach (var slot in slimeSlots)
        {
            Destroy(slot);
        }
        slimeSlots.Clear();

        // Get breedable slimes
        if (BreedingManager.Instance == null)
        {
            return;
        }
        var breedableSlimes = BreedingManager.Instance.GetBreedableSlimes();
    }

    private void RefreshCollectionGrid()
    {
        // Clear existing slots
        foreach (var slot in collectionSlots)
        {
            Destroy(slot);
        }
        collectionSlots.Clear();

        // Get all slimes
        var allSlimes = BreedingManager.Instance.GetAllSlimes();

        // Create new slots
        foreach (var slime in allSlimes)
        {
            GameObject slot = Instantiate(collectionSlotPrefab, collectionGridParent);
            var slotScript = slot.GetComponent<InventorySlot>();
            slotScript.canselect = true;
            slotScript.sprite = slotsprite;
            collectionSlots.Add(slot);
            if (slotScript != null)
            {
                slotScript.SetupSlime(slime);
            }
        }
    }

    public void ShowCollectionPanel()
    {
        breedingPanel.SetActive(false);
        slimeCollectionPanel.SetActive(true);
        RefreshCollectionGrid();
    }

    private void UpdateSlimeCounter()
    {
        if (slimeCounterText != null && BreedingManager.Instance != null)
        {
            int current = BreedingManager.Instance.GetCurrentSlimeCount();
            int max = BreedingManager.Instance.GetMaxSlimeCount();
            slimeCounterText.text = $"{current}/{max}";
        }
    }


}
