using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class tamingManager : MonoBehaviour
{
    private const string MobileControlsCanvasName = "MobileControlsCanvas";

    public float maxTamingPoint = 100;
    public float curTamingPoint = 30;
    [SerializeField] private Spawner spawner;
    [SerializeField] private PlayerMovement playerMovement;
    public int curID;
    public WildSlimes wildSlimes;
    public SlimeSpawner slimeSpawner;
    public float difficulty;

    public Image emote;
    public Sprite succeedcatch;
    public Sprite failcatch;

    [Header("Mobile Taming Buttons")]
    [SerializeField] private Button rightButton;
    [SerializeField] private Button upButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button downButton;

    private GameObject mobileControlsCanvas;
    private bool shouldRestoreMobileControls;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        ResolveReferences();
        AutoFindDirectionButtons();
        RegisterDirectionButtons();
    }

    void OnEnable()
    {
        HideMobileControlsCanvas();
    }

    void OnDisable()
    {
        RestoreMobileControlsCanvas();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ResolveReferences();
        if (wildSlimes == null || wildSlimes.slimes == null || slimeSpawner == null || spawner == null)
            return;

        var curSlime = new WildSlimes.WildSlimeTraits();
        foreach (var s in wildSlimes.slimes)
        {
            if (s.slimeID == curID)
            {
                curSlime = s;
                difficulty = curSlime.wildSlimeTraits[0].GenerateInstance().GetRarityMultiplier(curSlime.wildSlimeTraits[0].rarity)
                    + curSlime.wildSlimeTraits[1].GenerateInstance().GetRarityMultiplier(curSlime.wildSlimeTraits[1].rarity)
                    + curSlime.wildSlimeTraits[2].GenerateInstance().GetRarityMultiplier(curSlime.wildSlimeTraits[2].rarity);
                break;
            }
        }
        if (curTamingPoint <= 0)
        {
            Debug.Log("Fail");
            foreach (var note in spawner.notes)
            {
                Destroy(note.gameObject);
            }
            foreach(var t in slimeSpawner.activeSlimes)
            {
                if (t.GetComponent<WildSlimeTraits>().wildSlimeID == curID)
                {
                    Destroy(t);
                    Vector3 spawnPosition = slimeSpawner.GetRandomSpawnPosition();
                    slimeSpawner.SpawnSingleSlime(spawnPosition);
                    break;
                }
            }
            wildSlimes.slimes.Remove(curSlime);
            curTamingPoint = 30;
            spawner.gameObject.SetActive(false);
            if (emote != null)
            {
                emote.gameObject.SetActive(true);
                emote.sprite = failcatch;
            }
            StartCoroutine(deactive());
        }
        if (curTamingPoint >= maxTamingPoint)
        {
            Debug.Log("Success");
            foreach (var note in spawner.notes)
            {
                Destroy(note.gameObject);
            }
            if (wildSlimes.tamedSlimes == null)
                wildSlimes.tamedSlimes = new List<WildSlimes.WildSlimeTraits>();
            wildSlimes.tamedSlimes.Add(curSlime);
            RefreshAdventureBags();
            SaveAndLoadSystem.Instance?.Save();
            PlayerStatsManager.Instance?.RecordCapture(curSlime.wildSlimeTraits);
            for (int i = slimeSpawner.activeSlimes.Count - 1; i >= 0; i--)
            {
                var t = slimeSpawner.activeSlimes[i];
                if (t.GetComponent<WildSlimeTraits>().wildSlimeID == curID)
                {
                    if (t != null)Destroy(t);
                    slimeSpawner.activeSlimes.RemoveAt(i);

                    Vector3 spawnPosition = slimeSpawner.GetRandomSpawnPosition();
                    slimeSpawner.SpawnSingleSlime(spawnPosition);
                    break;
                }
            }
            wildSlimes.slimes.Remove(curSlime);
            curTamingPoint = 30;
            spawner.gameObject.SetActive(false);
            if (emote != null)
            {
                emote.gameObject.SetActive(true);
                emote.sprite = succeedcatch;
            }
            StartCoroutine(deactive());
            
        }
    }

    public void hit ()
    {
        if (emote != null)
            emote.gameObject.SetActive(true);
    }

    public void PressRight()
    {
        PressDirection(MobileDirection.Right);
    }

    public void PressUp()
    {
        PressDirection(MobileDirection.Up);
    }

    public void PressLeft()
    {
        PressDirection(MobileDirection.Left);
    }

    public void PressDown()
    {
        PressDirection(MobileDirection.Down);
    }

    public void PressDirection(MobileDirection direction)
    {
        MobileInput.QueueDirection(direction);
    }

    private void RegisterDirectionButtons()
    {
        RegisterDirectionButton(rightButton, PressRight);
        RegisterDirectionButton(upButton, PressUp);
        RegisterDirectionButton(leftButton, PressLeft);
        RegisterDirectionButton(downButton, PressDown);
    }

    private void RegisterDirectionButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        if (button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void AutoFindDirectionButtons()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            string buttonName = button.name.ToLowerInvariant();
            if (rightButton == null && (buttonName.Contains("right") || buttonName.Contains("phai")))
                rightButton = button;
            else if (upButton == null && (buttonName.Contains("up") || buttonName.Contains("len")))
                upButton = button;
            else if (leftButton == null && (buttonName.Contains("left") || buttonName.Contains("trai")))
                leftButton = button;
            else if (downButton == null && (buttonName.Contains("down") || buttonName.Contains("xuong")))
                downButton = button;
        }

        if (rightButton != null && upButton != null && leftButton != null && downButton != null)
            return;

        var unassignedButtons = new List<Button>();
        foreach (var button in buttons)
        {
            if (button == rightButton || button == upButton || button == leftButton || button == downButton)
                continue;
            if (button.onClick.GetPersistentEventCount() > 0)
                continue;

            unassignedButtons.Add(button);
        }

        if (unassignedButtons.Count < 4)
            return;

        Button leftMost = null;
        Button rightMost = null;
        Button topMost = null;
        Button bottomMost = null;

        foreach (var button in unassignedButtons)
        {
            var rect = button.transform as RectTransform;
            if (rect == null)
                continue;

            if (leftMost == null || rect.anchoredPosition.x < ((RectTransform)leftMost.transform).anchoredPosition.x)
                leftMost = button;
            if (rightMost == null || rect.anchoredPosition.x > ((RectTransform)rightMost.transform).anchoredPosition.x)
                rightMost = button;
            if (topMost == null || rect.anchoredPosition.y > ((RectTransform)topMost.transform).anchoredPosition.y)
                topMost = button;
            if (bottomMost == null || rect.anchoredPosition.y < ((RectTransform)bottomMost.transform).anchoredPosition.y)
                bottomMost = button;
        }

        if (leftButton == null)
            leftButton = leftMost;
        if (rightButton == null)
            rightButton = rightMost;
        if (upButton == null)
            upButton = topMost;
        if (downButton == null)
            downButton = bottomMost;
    }

    private void HideMobileControlsCanvas()
    {
        mobileControlsCanvas = GameObject.Find(MobileControlsCanvasName);
        if (mobileControlsCanvas == null || !mobileControlsCanvas.activeSelf)
            return;

        shouldRestoreMobileControls = true;
        mobileControlsCanvas.SetActive(false);
        MobileInput.ResetVirtualControls();
    }

    private void RestoreMobileControlsCanvas()
    {
        if (!shouldRestoreMobileControls || mobileControlsCanvas == null)
            return;

        mobileControlsCanvas.SetActive(true);
        shouldRestoreMobileControls = false;
    }

    IEnumerator hitdeactive()
    {
        yield return new WaitForSeconds(0.5f);
        emote.gameObject.SetActive(false);
    }



    IEnumerator deactive()
    {
        yield return new WaitForSeconds(3);
        if (spawner != null)
            spawner.gameObject.SetActive(true);
        if (emote != null)
            emote.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    private void ResolveReferences()
    {
        if (spawner == null)
            spawner = FindAnyObjectByType<Spawner>(FindObjectsInactive.Include);
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (wildSlimes == null)
            wildSlimes = FindAnyObjectByType<WildSlimes>(FindObjectsInactive.Include);
        if (slimeSpawner == null)
            slimeSpawner = FindAnyObjectByType<SlimeSpawner>(FindObjectsInactive.Include);
        if (emote == null)
        {
            Transform emoteTransform = transform.Find("Emote");
            if (emoteTransform != null)
                emote = emoteTransform.GetComponent<Image>();
        }
    }

    private void RefreshAdventureBags()
    {
        var bags = FindObjectsByType<AdventureBag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bag in bags)
        {
            if (bag != null)
                bag.RefreshAllUI();
        }
    }
}
