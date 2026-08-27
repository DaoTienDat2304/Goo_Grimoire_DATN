using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class showteam : MonoBehaviour
{
    public Team teamSlimes;
    public List<ShowTeamSlime> teamMembers;
    public List<GameObject> slimeFormation;
    public Transform gridParent;

    bool isActive = false;
    public Animator animator;

    [Header("Sidebar motion")]
    [SerializeField] private float closedX = -320f;
    [SerializeField] private float openX = 0f;
    [SerializeField] private float slideDuration = 0.22f;

    private RectTransform sidebarRect;
    private Coroutine sidebarRoutine;
    private bool sidebarInitialized;

    public bool IsOpen => isActive;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResolveReferences();
        InitializeSidebar();
        RefreshTeamDisplay();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenCloseTeam()
    {
        ResolveReferences();
        InitializeSidebar();
        isActive = !isActive;
        SlideSidebar(isActive);
        RefreshTeamDisplay();
    }

    public void InitializeSidebar()
    {
        if (sidebarInitialized)
            return;

        sidebarRect = transform as RectTransform;
        if (sidebarRect == null)
        {
            Debug.LogWarning("[TameSidebar] Tame must use a RectTransform.", this);
            return;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        // The old clips contain coordinates from the previous UI and would
        // overwrite the position driven by this flow every frame.
        if (animator != null)
            animator.enabled = false;

        gameObject.SetActive(true);
        CanvasGroup panelGroup = GetComponent<CanvasGroup>();
        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }

        Vector2 position = sidebarRect.anchoredPosition;
        position.x = closedX;
        sidebarRect.anchoredPosition = position;
        isActive = false;
        sidebarInitialized = true;
    }

    private void SlideSidebar(bool shouldOpen)
    {
        if (sidebarRect == null)
            return;

        if (sidebarRoutine != null)
            StopCoroutine(sidebarRoutine);

        float targetX = shouldOpen ? openX : closedX;
        if (!gameObject.activeInHierarchy || slideDuration <= 0f)
        {
            Vector2 position = sidebarRect.anchoredPosition;
            position.x = targetX;
            sidebarRect.anchoredPosition = position;
            sidebarRoutine = null;
            return;
        }

        sidebarRoutine = StartCoroutine(SlideSidebarTo(targetX));
    }

    private IEnumerator SlideSidebarTo(float targetX)
    {
        Vector2 start = sidebarRect.anchoredPosition;
        Vector2 target = new Vector2(targetX, start.y);
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            sidebarRect.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }

        sidebarRect.anchoredPosition = target;
        sidebarRoutine = null;
    }

    public void RefreshTeamDisplay()
    {
        ResolveReferences();
        if (teamSlimes == null || teamSlimes.team == null)
        {
            return;
        }

        if (teamMembers == null || teamMembers.Count == 0)
        {
            return;
        }


        // Clear all slots first
        for (int j = 0; j < teamMembers.Count; j++)
        {

            if (teamMembers[j] != null)
            {
                var bodyRenderer = teamMembers[j].body?.GetComponent<Image>();
                var armorRenderer = teamMembers[j].armor?.GetComponent<Image>();
                var weaponRenderer = teamMembers[j].weapon?.GetComponent<Image>();

                if (bodyRenderer != null) bodyRenderer.sprite = null;
                if (armorRenderer != null) armorRenderer.sprite = null;
                if (weaponRenderer != null) weaponRenderer.sprite = null;
            }
        }

        // Display team slimes
        int i = 0;
        foreach (Slime slime in teamSlimes.team)
        {
            if (i >= teamMembers.Count)
            {
                break;
            }

            if (teamMembers[i] == null)
            {
                i++;
                continue;
            }

            if (slime == null)
            {
                Debug.LogWarning($"Slime at index {i} is null!", this);
                i++;
                continue;
            }

            var bodyRenderer = teamMembers[i].body?.GetComponent<Image>();
            var armorRenderer = teamMembers[i].armor?.GetComponent<Image>();
            var weaponRenderer = teamMembers[i].weapon?.GetComponent<Image>();
            teamMembers[i].id = slime.id;

            if (bodyRenderer != null)
            {
                bodyRenderer.sprite = slime.body?.sprite;
            }

            if (armorRenderer != null)
            {
                armorRenderer.sprite = slime.armor?.sprite;
            }

            if (weaponRenderer != null)
            {
                weaponRenderer.sprite = slime.weapon?.sprite;
            }

            i++;
        }

    }

    private void ResolveReferences()
    {
        if (teamSlimes == null)
        {
            var save = SaveAndLoadSystem.Instance != null ? SaveAndLoadSystem.Instance : FindAnyObjectByType<SaveAndLoadSystem>(FindObjectsInactive.Include);
            if (save != null)
                teamSlimes = save.GetTeam();
        }

        if (teamSlimes == null)
        {
            var teams = Resources.FindObjectsOfTypeAll<Team>();
            if (teams != null && teams.Length > 0)
                teamSlimes = teams[0];
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        if (teamMembers == null || teamMembers.Count == 0)
            teamMembers = new List<ShowTeamSlime>(GetComponentsInChildren<ShowTeamSlime>(true));
    }
}

// Kept so existing scene components deserialize without becoming Missing Script.
// Sidebar movement is now owned by showteam and AdventureBag.
[AddComponentMenu("")]
public sealed class SidePanelSlider : MonoBehaviour
{
}

public static class TameSidebarFlowBootstrap
{
    private const int SidebarSortingOrder = 5100;
    private const int SidebarButtonSortingOrder = 5101;
    private static bool sceneHookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        sceneHookInstalled = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        WireActiveScene();

        if (!sceneHookInstalled)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHookInstalled = true;
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        WireActiveScene();
    }

    private static void WireActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Map1_IceMap" && sceneName != "Map2_Fantasymap" && sceneName != "Map3_DungeonMap")
            return;

        GameObject teamPanel = FindObject("Tame");
        GameObject inventoryPanel = FindObject("TameInventory");

        if (teamPanel != null)
            WireTeamPanel(teamPanel);

        if (inventoryPanel != null)
            WireInventoryPanel(inventoryPanel);

        KeepPanelAboveMobileControls(teamPanel);
        KeepPanelAboveMobileControls(inventoryPanel);

        Button teamButton = FindButton("ButtonTeam", "TeamButton", "ButtonTame", "TameButton");
        Button inventoryButton = FindButton("ButtonTameInventory", "TameInventoryButton", "ButtonInventory", "InventoryButton");
        KeepButtonAlwaysOpen(teamButton);
        KeepButtonAlwaysOpen(inventoryButton);
        KeepDetachedButtonAboveMobileControls(teamButton, teamPanel);
        KeepDetachedButtonAboveMobileControls(inventoryButton, inventoryPanel);

        if (teamButton != null && teamPanel != null)
            AddRuntimeClick(teamButton, teamPanel.GetComponent<showteam>().OpenCloseTeam);

        if (inventoryButton != null && inventoryPanel != null)
            AddRuntimeClick(inventoryButton, inventoryPanel.GetComponent<AdventureBag>().click);
    }

    private static void KeepButtonAlwaysOpen(Button button)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(true);
        button.interactable = true;

        var image = button.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        var group = button.GetComponent<CanvasGroup>();
        if (group == null)
            group = button.gameObject.AddComponent<CanvasGroup>();

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        group.ignoreParentGroups = true;
    }

    private static void KeepPanelAboveMobileControls(GameObject panel)
    {
        if (panel == null)
            return;

        EnsureSortingCanvas(panel, SidebarSortingOrder);
    }

    private static void KeepDetachedButtonAboveMobileControls(Button button, GameObject panel)
    {
        if (button == null)
            return;

        if (panel != null && button.transform.IsChildOf(panel.transform))
            return;

        EnsureSortingCanvas(button.gameObject, SidebarButtonSortingOrder);
    }

    private static void EnsureSortingCanvas(GameObject target, int sortingOrder)
    {
        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas == null)
            canvas = target.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        if (target.GetComponent<GraphicRaycaster>() == null)
            target.AddComponent<GraphicRaycaster>();
    }

    private static void WireTeamPanel(GameObject teamPanel)
    {
        var team = teamPanel.GetComponent<showteam>();
        if (team == null)
            team = teamPanel.AddComponent<showteam>();

        teamPanel.SetActive(true);
        team.InitializeSidebar();
        if (team.teamSlimes == null)
        {
            SaveAndLoadSystem save = SaveAndLoadSystem.Instance != null ? SaveAndLoadSystem.Instance : Object.FindAnyObjectByType<SaveAndLoadSystem>(FindObjectsInactive.Include);
            if (save != null)
                team.teamSlimes = save.GetTeam();
        }

        team.RefreshTeamDisplay();
    }

    private static void WireInventoryPanel(GameObject inventoryPanel)
    {
        var bag = inventoryPanel.GetComponent<AdventureBag>();
        if (bag == null)
            bag = inventoryPanel.AddComponent<AdventureBag>();

        inventoryPanel.SetActive(true);
        bag.slimeCollectionPanel = inventoryPanel;
        bag.InitializeSidebar();
        bag.RefreshAllUI();
    }

    private static void AddRuntimeClick(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        string methodName = action.Method.Name;
        Object actionTarget = action.Target as Object;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == methodName
                && button.onClick.GetPersistentTarget(i) == actionTarget)
                return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static Button FindButton(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject obj = FindObject(name);
            if (obj == null)
                continue;

            Button button = obj.GetComponent<Button>();
            if (button != null)
                return button;
        }

        return null;
    }

    private static GameObject FindObject(string name)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            Transform found = FindChild(root.transform, name);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root.name == name)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChild(child, name);
            if (found != null)
                return found;
        }

        return null;
    }
}
