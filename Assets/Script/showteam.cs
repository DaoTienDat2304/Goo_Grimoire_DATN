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
    public SidePanelSlider panelSlider;

    public bool IsOpen => isActive;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResolveReferences();
        RefreshTeamDisplay();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenCloseTeam()
    {
        ResolveReferences();
        isActive = !isActive;
        RefreshTeamDisplay();

        if (panelSlider != null)
            panelSlider.SetOpen(isActive, false);
        else if (animator != null)
            animator.SetBool("open", isActive);
        else
            gameObject.SetActive(isActive);
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

            var bodyRenderer = teamMembers[i].body?.GetComponent<Image>();
            var armorRenderer = teamMembers[i].armor?.GetComponent<Image>();
            var weaponRenderer = teamMembers[i].weapon?.GetComponent<Image>();
            teamMembers[i].id = slime.id;

            if (slime != null)
            {

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
            }
            else
            {
                Debug.LogWarning($"Slime at index {i} is null!");
            }

            i++;
        }

        Debug.Log($"Team display refresh completed. Displayed {i} slimes.");
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

        if (panelSlider == null)
            panelSlider = GetComponent<SidePanelSlider>();

        if (teamMembers == null || teamMembers.Count == 0)
            teamMembers = new List<ShowTeamSlime>(GetComponentsInChildren<ShowTeamSlime>(true));
    }
}

public class SidePanelSlider : MonoBehaviour
{
    public enum SlideSide
    {
        Left,
        Right
    }

    [SerializeField] private SlideSide side = SlideSide.Left;
    [SerializeField] private RectTransform panel;
    [SerializeField] private float duration = 0.22f;
    [SerializeField] private bool startClosed = true;

    private Vector2 openPosition;
    private Vector2 closedPosition;
    private Coroutine slideRoutine;
    private bool initialized;
    private bool open;

    public bool IsOpen => open;

    private void Awake()
    {
        Initialize();
        SetOpen(!startClosed, true);
    }

    public void Configure(SlideSide newSide, bool closeOnStart)
    {
        side = newSide;
        startClosed = closeOnStart;
        initialized = false;
        Initialize();
        if (!Application.isPlaying)
            return;
        SetOpen(!startClosed, true);
    }

    public void SetOpen(bool value, bool instant)
    {
        Initialize();
        open = value;

        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        Vector2 target = open ? openPosition : closedPosition;
        if (instant || duration <= 0f || !gameObject.activeInHierarchy)
        {
            panel.anchoredPosition = target;
            return;
        }

        slideRoutine = StartCoroutine(SlideTo(target));
    }

    private IEnumerator SlideTo(Vector2 target)
    {
        Vector2 start = panel.anchoredPosition;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            panel.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }

        panel.anchoredPosition = target;
        slideRoutine = null;
    }

    private void Initialize()
    {
        if (initialized)
            return;

        panel = panel != null ? panel : transform as RectTransform;
        if (panel == null)
            return;

        openPosition = panel.anchoredPosition;
        float width = panel.rect.width > 1f ? panel.rect.width : panel.sizeDelta.x;
        float offset = width + 80f;
        closedPosition = openPosition + new Vector2(side == SlideSide.Left ? -offset : offset, 0f);
        initialized = true;
    }
}

public static class TameSidebarFlowBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        WireActiveScene();
        SceneManager.sceneLoaded += (_, __) => WireActiveScene();
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

        Button teamButton = FindButton("ButtonTeam", "TeamButton", "ButtonTame", "TameButton");
        Button inventoryButton = FindButton("ButtonTameInventory", "TameInventoryButton", "ButtonInventory", "InventoryButton");

        if (teamButton != null && teamPanel != null)
            AddRuntimeClick(teamButton, teamPanel.GetComponent<showteam>().OpenCloseTeam);

        if (inventoryButton != null && inventoryPanel != null)
            AddRuntimeClick(inventoryButton, inventoryPanel.GetComponent<AdventureBag>().click);
    }

    private static void WireTeamPanel(GameObject teamPanel)
    {
        var slider = teamPanel.GetComponent<SidePanelSlider>();
        if (slider == null)
            slider = teamPanel.AddComponent<SidePanelSlider>();
        slider.Configure(SidePanelSlider.SlideSide.Left, true);

        var team = teamPanel.GetComponent<showteam>();
        if (team == null)
            team = teamPanel.AddComponent<showteam>();

        team.panelSlider = slider;
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
        var slider = inventoryPanel.GetComponent<SidePanelSlider>();
        if (slider == null)
            slider = inventoryPanel.AddComponent<SidePanelSlider>();
        slider.Configure(SidePanelSlider.SlideSide.Right, true);

        var bag = inventoryPanel.GetComponent<AdventureBag>();
        if (bag == null)
            bag = inventoryPanel.AddComponent<AdventureBag>();

        bag.panelSlider = slider;
        bag.slimeCollectionPanel = inventoryPanel;
        bag.RefreshAllUI();
    }

    private static void AddRuntimeClick(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        string methodName = action.Method.Name;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == methodName)
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
