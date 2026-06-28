using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class showteam : MonoBehaviour
{
    public Team teamSlimes;
    public List<ShowTeamSlime> teamMembers;
    public List<GameObject> slimeFormation;
    public Transform gridParent;

    bool isActive = false;
    public Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshTeamDisplay();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenCloseTeam()
    {
        isActive = !isActive;
        animator.SetBool("open", isActive);
    }

    public void RefreshTeamDisplay()
    {
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
}
