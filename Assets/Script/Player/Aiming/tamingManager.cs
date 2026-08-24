using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class tamingManager : MonoBehaviour
{
    private const string MobileControlsCanvasName = "MobileControlsCanvas";
    private const float InitialTamingPoint = 30f;
    private static tamingManager activeManager;

    public static tamingManager Active
    {
        get
        {
            if (activeManager != null)
                return activeManager;

            return TamingPanelFlow.CanonicalManager;
        }
    }

    public float maxTamingPoint = 100;
    public float curTamingPoint = InitialTamingPoint;
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

    [Header("Taming Note Zones")]
    [SerializeField] private Collider2D checkBar;
    [SerializeField] private Collider2D failBar;

    public Collider2D CheckBar
    {
        get
        {
            ResolveTamingZones();
            return checkBar;
        }
    }

    public Collider2D FailBar
    {
        get
        {
            ResolveTamingZones();
            return failBar;
        }
    }

    private GameObject mobileControlsCanvas;
    private bool shouldRestoreMobileControls;
    private bool encounterFinishing;

    private void Awake()
    {
        PrepareForRuntime();
    }

    private void OnEnable()
    {
        PrepareForRuntime();
        HideMobileControlsCanvas();
    }

    private void OnDisable()
    {
        RestoreMobileControlsCanvas();
        if (playerMovement != null)
            playerMovement.enabled = true;
        if (activeManager == this)
            activeManager = null;
    }

    private void Update()
    {
        if (encounterFinishing)
            return;

        ResolveReferences();
        if (wildSlimes == null || wildSlimes.slimes == null || slimeSpawner == null || spawner == null)
            return;

        WildSlimes.WildSlimeTraits currentSlime = FindCurrentSlime();
        if (!HasCompleteTraits(currentSlime))
            return;

        difficulty = CalculateDifficulty(currentSlime);
        if (curTamingPoint <= 0f)
            FinishEncounter(currentSlime, false);
        else if (curTamingPoint >= maxTamingPoint)
            FinishEncounter(currentSlime, true);
    }

    public void PrepareForRuntime()
    {
        activeManager = this;
        ResolveReferences();
        AutoFindDirectionButtons();
        RegisterDirectionButtons();
    }

    public void BeginTaming(int slimeId, WildSlimes.WildSlimeTraits slimeData)
    {
        bool continuingSameEncounter = gameObject.activeSelf && curID == slimeId;
        PrepareForRuntime();
        curID = slimeId;
        encounterFinishing = false;

        if (!continuingSameEncounter)
            curTamingPoint = InitialTamingPoint;

        ApplyPreview(slimeData);
        gameObject.SetActive(true);

        if (playerMovement != null)
            playerMovement.enabled = false;
    }

    public void hit()
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

    private void FinishEncounter(WildSlimes.WildSlimeTraits slime, bool success)
    {
        encounterFinishing = true;
        Debug.Log(success ? "Success" : "Fail");
        ClearNotes();

        if (success)
        {
            if (wildSlimes.tamedSlimes == null)
                wildSlimes.tamedSlimes = new List<WildSlimes.WildSlimeTraits>();

            if (!ContainsTamedSlime(slime.slimeID))
            {
                wildSlimes.tamedSlimes.Add(slime);
                PlayerStatsManager.Instance?.RecordCapture(slime.wildSlimeTraits);
            }

            RefreshAdventureBags();
            SaveAndLoadSystem.Instance?.Save();
        }

        ReplaceWorldSlime(slime.slimeID);
        wildSlimes.slimes.Remove(slime);
        curTamingPoint = InitialTamingPoint;
        spawner.gameObject.SetActive(false);

        if (emote != null)
        {
            emote.gameObject.SetActive(true);
            emote.sprite = success ? succeedcatch : failcatch;
        }

        StartCoroutine(DeactivateAfterResult());
    }

    private void ReplaceWorldSlime(int slimeId)
    {
        if (slimeSpawner == null || slimeSpawner.activeSlimes == null)
            return;

        for (int i = slimeSpawner.activeSlimes.Count - 1; i >= 0; i--)
        {
            GameObject worldSlime = slimeSpawner.activeSlimes[i];
            WildSlimeTraits traits = worldSlime != null ? worldSlime.GetComponent<WildSlimeTraits>() : null;
            if (traits == null || traits.wildSlimeID != slimeId)
                continue;

            if (worldSlime != null)
                Destroy(worldSlime);
            slimeSpawner.activeSlimes.RemoveAt(i);
            slimeSpawner.SpawnSingleSlime(slimeSpawner.GetRandomSpawnPosition());
            return;
        }
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

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this
                && button.onClick.GetPersistentMethodName(i) == action.Method.Name)
            {
                return;
            }
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void AutoFindDirectionButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
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
        foreach (Button button in buttons)
        {
            if (button != rightButton && button != upButton && button != leftButton && button != downButton)
                unassignedButtons.Add(button);
        }

        if (unassignedButtons.Count < 4)
            return;

        Button leftMost = null;
        Button rightMost = null;
        Button topMost = null;
        Button bottomMost = null;

        foreach (Button button in unassignedButtons)
        {
            RectTransform rect = button.transform as RectTransform;
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

        if (leftButton == null) leftButton = leftMost;
        if (rightButton == null) rightButton = rightMost;
        if (upButton == null) upButton = topMost;
        if (downButton == null) downButton = bottomMost;
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

    private IEnumerator DeactivateAfterResult()
    {
        yield return new WaitForSeconds(3f);
        if (spawner != null)
            spawner.gameObject.SetActive(true);
        if (emote != null)
            emote.gameObject.SetActive(false);

        gameObject.SetActive(false);
        if (playerMovement != null)
            playerMovement.enabled = true;
        encounterFinishing = false;
    }

    private void ResolveReferences()
    {
        if (spawner == null)
            spawner = GetComponentInChildren<Spawner>(true);
        if (spawner == null)
            spawner = FindAnyObjectByType<Spawner>(FindObjectsInactive.Include);
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (wildSlimes == null && SaveAndLoadSystem.Instance != null)
            wildSlimes = SaveAndLoadSystem.Instance.wildSlimes;
        if (wildSlimes == null)
            wildSlimes = FindAnyObjectByType<WildSlimes>(FindObjectsInactive.Include);
        if (slimeSpawner == null)
            slimeSpawner = FindAnyObjectByType<SlimeSpawner>(FindObjectsInactive.Include);
        if (emote == null)
        {
            Transform emoteTransform = FindChild(transform, "Emote");
            if (emoteTransform != null)
                emote = emoteTransform.GetComponent<Image>();
        }

        ResolveTamingZones();
    }

    private void ResolveTamingZones()
    {
        if (checkBar != null && !checkBar.transform.IsChildOf(transform))
            checkBar = null;
        if (failBar != null && !failBar.transform.IsChildOf(transform))
            failBar = null;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        if (checkBar == null)
            checkBar = FindZoneCollider(colliders, "CheckBar", "CheckBar");
        if (failBar == null)
            failBar = FindZoneCollider(colliders, "FailBar", "FailBar");

        PrepareZoneCollider(checkBar);
        PrepareZoneCollider(failBar);
    }

    private static Collider2D FindZoneCollider(
        Collider2D[] colliders,
        string compactName,
        string tagName)
    {
        foreach (Collider2D candidate in colliders)
        {
            if (HasCompactName(candidate, compactName))
                return candidate;
        }

        foreach (Collider2D candidate in colliders)
        {
            if (candidate != null && candidate.CompareTag(tagName))
                return candidate;
        }

        return null;
    }

    private static bool HasCompactName(Collider2D candidate, string compactName)
    {
        if (candidate == null)
            return false;

        string candidateName = candidate.gameObject.name.Replace(" ", "").Replace("_", "");
        return string.Equals(
            candidateName,
            compactName,
            System.StringComparison.OrdinalIgnoreCase);
    }

    private static void PrepareZoneCollider(Collider2D zone)
    {
        if (zone == null)
            return;

        zone.isTrigger = true;
        if (zone is not BoxCollider2D box || zone.transform is not RectTransform rect)
            return;

        bool hasDefaultSize = Mathf.Approximately(box.size.x, 1f)
            && Mathf.Approximately(box.size.y, 1f);
        Vector2 rectSize = rect.rect.size;
        if (hasDefaultSize && rectSize.x > 1f && rectSize.y > 1f)
            box.size = rectSize;
    }

    private WildSlimes.WildSlimeTraits FindCurrentSlime()
    {
        foreach (WildSlimes.WildSlimeTraits slime in wildSlimes.slimes)
        {
            if (slime != null && slime.slimeID == curID)
                return slime;
        }

        return null;
    }

    private void ApplyPreview(WildSlimes.WildSlimeTraits slimeData)
    {
        if (slimeData == null || slimeData.wildSlimeTraits == null)
            return;

        foreach (TraitSO trait in slimeData.wildSlimeTraits)
        {
            if (trait == null)
                continue;

            string targetName = null;
            if (trait.type == TraitType.Body) targetName = "BodySprite";
            else if (trait.type == TraitType.Armor) targetName = "ArmorSprite";
            else if (trait.type == TraitType.Weapon) targetName = "WeaponSprite";
            if (targetName == null)
                continue;

            Transform target = FindChild(transform, targetName);
            Image image = target != null ? target.GetComponent<Image>() : null;
            if (image != null)
                image.sprite = trait.sprite;
        }
    }

    private float CalculateDifficulty(WildSlimes.WildSlimeTraits slime)
    {
        float total = 0f;
        foreach (TraitSO trait in slime.wildSlimeTraits)
        {
            if (trait != null)
                total += trait.GenerateInstance().GetRarityMultiplier(trait.rarity);
        }

        return Mathf.Max(1f, total);
    }

    private bool ContainsTamedSlime(int slimeId)
    {
        if (wildSlimes == null || wildSlimes.tamedSlimes == null)
            return false;

        foreach (WildSlimes.WildSlimeTraits slime in wildSlimes.tamedSlimes)
        {
            if (slime != null && slime.slimeID == slimeId)
                return true;
        }

        return false;
    }

    private void ClearNotes()
    {
        if (spawner == null || spawner.notes == null)
            return;

        foreach (GameObject note in spawner.notes)
        {
            if (note != null)
                Destroy(note);
        }

        spawner.notes.Clear();
    }

    private void RefreshAdventureBags()
    {
        AdventureBag[] bags = FindObjectsByType<AdventureBag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AdventureBag bag in bags)
        {
            if (bag != null)
                bag.RefreshAllUI();
        }
    }

    private static bool HasCompleteTraits(WildSlimes.WildSlimeTraits slime)
    {
        if (slime == null || slime.wildSlimeTraits == null || slime.wildSlimeTraits.Length < 3)
            return false;

        return slime.wildSlimeTraits[0] != null
            && slime.wildSlimeTraits[1] != null
            && slime.wildSlimeTraits[2] != null;
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChild(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
