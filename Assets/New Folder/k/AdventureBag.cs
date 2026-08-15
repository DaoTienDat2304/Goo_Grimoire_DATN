using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdventureBag : MonoBehaviour
{
    public GameObject slimeCollectionPanel;
    public GameObject showslot;
    public Animator animator;

    [Header("Breeding UI")]
    public Sprite slotsprite;
    private bool open = false;

    public bool IsOpen => open;

    [Header("Collection UI")]
    public Transform collectionGridParent;
    public GameObject collectionSlotPrefab;
    public WildSlimes wildSlimes;

    private List<GameObject> slimeSlots = new List<GameObject>();
    private List<GameObject> collectionSlots = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        RefreshAllUI();
    }

    private void OnEnable()
    {
        RefreshAllUI();
    }

    public void click()
    {
        ResolveReferences();
        open = !open;
        RefreshAllUI();
        if (animator != null)
            animator.SetBool("open",open);
        else if (slimeCollectionPanel != null)
            slimeCollectionPanel.SetActive(open);
    }
    public void RefreshAllUI()
    {
        ResolveReferences();
        RefreshSlimeGrid();
        RefreshCollectionGrid();
    }

    private void RefreshSlimeGrid()
    {
        // Clear existing slots
        foreach (var slot in slimeSlots)
        {
            Destroy(slot);
        }
        slimeSlots.Clear();
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
        if (wildSlimes == null || wildSlimes.tamedSlimes == null || collectionGridParent == null || collectionSlotPrefab == null)
            return;

        var allSlimes = wildSlimes.tamedSlimes;

        // Create new slots
        foreach (var WildSlimeTraits in allSlimes)
        {
            GameObject slot = Instantiate(collectionSlotPrefab, collectionGridParent);
            var slotScript = slot.GetComponent<tameslimeslot>();
            collectionSlots.Add(slot);
            if (slotScript != null)
            {
                if (slotScript.wildSlimes == null)
                    slotScript.wildSlimes = wildSlimes;
                slotScript.SetupSlime(WildSlimeTraits.slimeID);
            }
        }
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (slimeCollectionPanel == null)
            slimeCollectionPanel = gameObject;

        if (collectionGridParent == null)
        {
            Transform content = FindChildByName(transform, "Content");
            collectionGridParent = content != null ? content : transform;
        }

        if (collectionSlotPrefab == null)
            collectionSlotPrefab = Resources.Load<GameObject>("tameslime");

        if (wildSlimes == null)
            wildSlimes = FindAnyObjectByType<WildSlimes>();
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChildByName(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
