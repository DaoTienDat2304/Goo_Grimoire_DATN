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
        if (pickingPanel != null)
        {
            StartCoroutine(SlimeList());
        }
        savedMem = (teamSlime != null && teamSlime.team != null) ? teamSlime.team.Count : 0;
        refreshTeam();
        curMem = savedMem;
    }

    IEnumerator SlimeList()
    {
        if (pickingPanel == null) yield break;

        foreach (Transform child in pickingPanel)
        {
            if (child != null) Destroy(child.gameObject);
        }
        yield return new WaitForSeconds(0.2f);

        if (BreedingManager.Instance == null) yield break;

        // Get all slimes
        var allSlimes = BreedingManager.Instance.GetAllSlimes();
        if (allSlimes == null) yield break;

        // Create new slots
        foreach (var slime in allSlimes)
        {
            if (slime == null || slotPrefab == null || pickingPanel == null) continue;

            GameObject slot = Instantiate(slotPrefab, pickingPanel);
            var slotScript = slot.GetComponent<SlimeSlotUI>();
            if (slotScript != null)
            {
                slotScript.sprite = slotsprite;
                slotScript.SetupSlime(slime);
                if (slotScript.backgroundImage != null) slotScript.backgroundImage.gameObject.SetActive(false);
                if (slotScript.breedingStatusText != null) slotScript.breedingStatusText.gameObject.SetActive(false);
                if (slotScript.teamButton != null) slotScript.teamButton.gameObject.SetActive(true);
            }
            slot.transform.localScale = Vector3.one * 3f;
        }
        refreshTeam();
    }

    public void refreshTeam()
    {
        if (collectionSlots == null || teamSlime == null || teamSlime.team == null) return;

        foreach (var slot in collectionSlots)
        {
            if (slot == null) continue;
            foreach (Transform child in slot.transform)
            {
                if (child != null) Destroy(child.gameObject);
            }
        }

        int i = 0;
        foreach (var mem in teamSlime.team)
        {
            // Bảo vệ tránh lỗi IndexOutOfRangeException khi số slime trong team vượt quá số ô slot
            if (i >= collectionSlots.Count) break;
            if (collectionSlots[i] == null) { i++; continue; }
            if (mem == null || slotPrefab == null) { i++; continue; }

            GameObject pickedMem = Instantiate(slotPrefab, collectionSlots[i].transform);
            var slotScript = pickedMem.GetComponent<SlimeSlotUI>();
            if (slotScript != null)
            {
                slotScript.sprite = slotsprite;
                slotScript.SetupSlime(mem);
                if (slotScript.backgroundImage != null) slotScript.backgroundImage.gameObject.SetActive(false);
                if (slotScript.breedingStatusText != null) slotScript.breedingStatusText.gameObject.SetActive(false);
                if (slotScript.teamButton != null) slotScript.teamButton.gameObject.SetActive(true);
                if (slotScript.slimeBody != null) slotScript.slimeBody.transform.localScale = Vector3.one * 2f;
            }
            pickedMem.transform.localScale = Vector3.one * 2f;
            i++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        savedMem = (teamSlime != null && teamSlime.team != null) ? teamSlime.team.Count : 0;
        if (curMem != savedMem)
        {
            refreshTeam();
            curMem = savedMem;
        }
    }
}
