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
        open = !open;
        RefreshAllUI();
        animator.SetBool("open",open);
    }
    public void RefreshAllUI()
    {
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
        var allSlimes = wildSlimes.tamedSlimes;

        // Create new slots
        foreach (var WildSlimeTraits in allSlimes)
        {
            GameObject slot = Instantiate(collectionSlotPrefab, collectionGridParent);
            var slotScript = slot.GetComponent<tameslimeslot>();
            collectionSlots.Add(slot);
            if (slotScript != null)
            {
                slotScript.SetupSlime(WildSlimeTraits.slimeID);
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
