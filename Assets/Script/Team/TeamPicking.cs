using Spine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamPicking : MonoBehaviour
{
    [SerializeField] private BreedingManager breedingManager;
    [SerializeField] private Team teamSlime;
    public GameObject slotPrefab;
    public Transform pickingPanel;
    public List<GameObject> collectionSlots = new List<GameObject>();
    public Sprite slotsprite;
    private int curMem;
    private int savedMem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        StartCoroutine(SlimeList());
        savedMem = teamSlime.team.Count;
        refreshTeam();
        curMem = savedMem;
    }

    IEnumerator SlimeList()
    {
        foreach (Transform child in pickingPanel)
        {
            Destroy(child.gameObject);
        }
        yield return new WaitForSeconds(0.2f);
        // Get all slimes
        var allSlimes = BreedingManager.Instance.GetAllSlimes();

        // Create new slots
        foreach (var slime in allSlimes)
        {
            GameObject slot = Instantiate(slotPrefab, pickingPanel);
            var slotScript = slot.GetComponent<SlimeSlotUI>();
            slotScript.sprite = slotsprite;
            if (slotScript != null)
            {
                slotScript.SetupSlime(slime);
                slotScript.backgroundImage.gameObject.SetActive(false);
                slotScript.breedingStatusText.gameObject.SetActive(false);
                slotScript.teamButton.gameObject.SetActive(true);
            }
            slot.transform.localScale = Vector3.one *3f;
        }
        refreshTeam();
    }
    public void refreshTeam()
    {
        foreach (var slot in collectionSlots)
        {
            foreach (Transform child in slot.transform)
            {
                Destroy(child.gameObject);
            }
        }
        int i = 0;
        foreach (var mem in teamSlime.team)
        {
            GameObject pickedMem = Instantiate(slotPrefab, collectionSlots[i].transform);
            var slotScript = pickedMem.GetComponent<SlimeSlotUI>();
            slotScript.sprite = slotsprite;
            if (slotScript != null)
            {
                slotScript.SetupSlime(mem);
                slotScript.backgroundImage.gameObject.SetActive(false);
                slotScript.breedingStatusText.gameObject.SetActive(false);
                slotScript.teamButton.gameObject.SetActive(true);
            }
            pickedMem.transform.localScale = Vector3.one * 2f;
            slotScript.slimeBody.transform.localScale = Vector3.one * 2f;
            i++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        savedMem = teamSlime.team.Count;
        if (curMem != savedMem)
        {
            foreach (var slot in collectionSlots)
            {
                foreach (Transform child in slot.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            refreshTeam();
            curMem = savedMem;
        }
    }
}
