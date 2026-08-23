using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// </summary>
public class CollectionBookUI : MonoBehaviour
{
    [Header("Tab Buttons (Top)")]
    public Button tabSlimesBtn;
    public Button tabPartsBtn;
    public Button tabSkillsBtn;

    [Header("Right Page Containers")]
    public GameObject slimesGridContainer;
    public GameObject partsGridContainer;
    public GameObject skillsGridContainer;

    [Header("Left Detail Panel")]
    public CollectionDetailPanel detailPanel;

    // ── Prefabs ──
    [Header("Grid Item Prefab")]
    public GameObject gridItemPrefab;

    // ── Grid content transforms ──
    [Header("Grid Content Parents")]
    public Transform slimesGridContent;
    public Transform partsGridContent;
    public Transform skillsGridContent;

    // ── Close button ──
    [Header("Close")]
    public Button closeButton;

    // ── Tab colors ──
    [Header("Tab Visuals")]
    public Color activeTabColor = new Color(0.9f, 0.8f, 0.5f);
    public Color inactiveTabColor = Color.white;

    private enum BookTab { Slimes, Parts, Skills }
    private BookTab _currentTab = BookTab.Slimes;

    private List<CollectionGridItem> _slimeItems = new List<CollectionGridItem>();
    private List<CollectionGridItem> _partItems  = new List<CollectionGridItem>();
    private List<CollectionGridItem> _skillItems = new List<CollectionGridItem>();

    void Awake()
    {
        tabSlimesBtn?.onClick.AddListener(() => SwitchTab(BookTab.Slimes));
        tabPartsBtn?.onClick.AddListener(() => SwitchTab(BookTab.Parts));
        tabSkillsBtn?.onClick.AddListener(() => SwitchTab(BookTab.Skills));
        closeButton?.onClick.AddListener(CloseBook);

        LocalizeButton(tabSlimesBtn, "Slimes");
        LocalizeButton(tabPartsBtn, "Parts");
        LocalizeButton(tabSkillsBtn, "Skills");
    }

    private void LocalizeButton(Button btn, string text)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TMPro.TMP_Text>();
        if (tmp != null)
        {
            tmp.enableAutoSizing = true;
            tmp.text = text;
            return;
        }
        var txt = btn.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.resizeTextForBestFit = true;
            txt.text = text;
        }
    }

    void OnEnable()
    {
        if (CollectionBookManager.Instance != null)
            CollectionBookManager.Instance.RefreshFromSave();

        PopulateAllGrids();
        SwitchTab(BookTab.Slimes);
    }

    // ─────────────────────────────────────────
    // Tab switching
    // ─────────────────────────────────────────
    private void SwitchTab(BookTab tab)
    {
        _currentTab = tab;
        slimesGridContainer?.SetActive(tab == BookTab.Slimes);
        partsGridContainer?.SetActive(tab == BookTab.Parts);
        skillsGridContainer?.SetActive(tab == BookTab.Skills);

        SetTabColor(tabSlimesBtn, tab == BookTab.Slimes);
        SetTabColor(tabPartsBtn, tab == BookTab.Parts);
        SetTabColor(tabSkillsBtn, tab == BookTab.Skills);

        detailPanel?.ClearDetail();
    }

    private void SetTabColor(Button btn, bool isActive)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = isActive ? activeTabColor : inactiveTabColor;
    }

    // ─────────────────────────────────────────
    // Populate grids
    // ─────────────────────────────────────────
    private void PopulateAllGrids()
    {
        var mgr = CollectionBookManager.Instance;
        if (mgr == null) return;

        PopulateSlimeGrid(mgr);
        PopulatePartsGrid(mgr);
        PopulateSkillsGrid(mgr);
    }

    private void PopulateSlimeGrid(CollectionBookManager mgr)
    {
        if (slimesGridContent == null || gridItemPrefab == null) return;
        ClearGrid(slimesGridContent, _slimeItems);

        var allBodyTraits = mgr.GetAllBodyTraits();
        foreach (var trait in allBodyTraits)
        {
            if (trait == null) continue;
            var go = Instantiate(gridItemPrefab, slimesGridContent);
            var item = go.GetComponent<CollectionGridItem>();
            if (item == null) continue;

            bool unlocked = mgr.IsTraitUnlocked(trait);
            Slime bestSlime = unlocked ? mgr.GetBestSlimeForBodyTrait(trait) : null;

            item.SetupAsSlime(trait, bestSlime, unlocked, () =>
            {
                if (unlocked && bestSlime != null)
                    detailPanel?.ShowSlimeDetail(bestSlime, trait);
                else if (unlocked)
                    detailPanel?.ShowTraitDetail(trait);
            });

            _slimeItems.Add(item);
        }
    }

    private void PopulatePartsGrid(CollectionBookManager mgr)
    {
        if (partsGridContent == null || gridItemPrefab == null) return;
        ClearGrid(partsGridContent, _partItems);

        var armorTraits  = mgr.GetAllArmorTraits();
        var weaponTraits = mgr.GetAllWeaponTraits();
        var allParts = new List<TraitSO>();
        allParts.AddRange(armorTraits);
        allParts.AddRange(weaponTraits);

        foreach (var trait in allParts)
        {
            if (trait == null) continue;
            var go = Instantiate(gridItemPrefab, partsGridContent);
            var item = go.GetComponent<CollectionGridItem>();
            if (item == null) continue;

            bool unlocked = mgr.IsTraitUnlocked(trait);
            item.SetupAsTrait(trait, unlocked, () =>
            {
                if (unlocked) detailPanel?.ShowTraitDetail(trait);
            });

            _partItems.Add(item);
        }
    }

    private void PopulateSkillsGrid(CollectionBookManager mgr)
    {
        if (skillsGridContent == null || gridItemPrefab == null) return;
        ClearGrid(skillsGridContent, _skillItems);

        var allSkills = mgr.GetAllSkills();
        foreach (var skill in allSkills)
        {
            if (skill == null) continue;
            var go = Instantiate(gridItemPrefab, skillsGridContent);
            var item = go.GetComponent<CollectionGridItem>();
            if (item == null) continue;

            bool unlocked = mgr.IsSkillUnlocked(skill);
            item.SetupAsSkill(skill, unlocked, () =>
            {
                if (unlocked) detailPanel?.ShowSkillDetail(skill);
            });

            _skillItems.Add(item);
        }
    }

    private void ClearGrid<T>(Transform parent, List<T> cache)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
        cache.Clear();
    }

    public void OpenBook()
    {
        gameObject.SetActive(true);
    }

    private void CloseBook()
    {
        gameObject.SetActive(false);

        var worldManager = Object.FindFirstObjectByType<SlimeWorldManager>();
        if (worldManager != null)
        {
            worldManager.StartWorldView();
        }
    }
}
