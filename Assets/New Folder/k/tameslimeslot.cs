using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class tameslimeslot : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject slimeBody;
    public GameObject SlimeArmor;
    public GameObject SlimeWeapon;
    public Button teamButton;

    public WildSlimes wildSlimes;
    public TraitSO body;
    public TraitSO armor;
    public TraitSO weapon;
    

    private void Start()
    {
        ResolveReferences();
    }

    public void SetupSlime(int newSlime)
    {
        ResolveReferences();
        if (wildSlimes == null || wildSlimes.tamedSlimes == null)
            return;
        
        foreach (WildSlimes.WildSlimeTraits a in wildSlimes.tamedSlimes)
        {
            if (a.slimeID == newSlime)
            {
                body = a.wildSlimeTraits[0];
                armor = a.wildSlimeTraits[1];
                weapon = a.wildSlimeTraits[2];
                UpdateUI();
                return;
            }
        }
    }

    private void UpdateUI()
    {
        ResolveReferences();
        if (body == null || armor == null || weapon == null)
            return;

        var bodyRenderer = slimeBody?.GetComponent<Image>();
        var armorRenderer = SlimeArmor?.GetComponent<Image>();
        var weaponRenderer = SlimeWeapon?.GetComponent<Image>();
        if (bodyRenderer == null || armorRenderer == null || weaponRenderer == null)
            return;

        bodyRenderer.transform.localScale = Vector3.one * 1.3f;
        armorRenderer.transform.localScale = Vector3.one;
        weaponRenderer.transform.localScale = Vector3.one;
        bodyRenderer.sprite = body.sprite;
        armorRenderer.sprite = armor.sprite;
        weaponRenderer.sprite = weapon.sprite;

    }

    public void OnPointerClick(PointerEventData eventData)
    {

    }

    // Method to refresh the UI when slime data changes
    public void RefreshUI()
    {
        UpdateUI();
    }

    private void ResolveReferences()
    {
        if (wildSlimes == null)
            wildSlimes = FindAnyObjectByType<WildSlimes>();

        if (slimeBody == null)
            slimeBody = FindChildByName(transform, "slimeBody")?.gameObject ?? FindChildByName(transform, "SlimeBody")?.gameObject;
        if (SlimeArmor == null)
            SlimeArmor = FindChildByName(transform, "SlimeArmor")?.gameObject;
        if (SlimeWeapon == null)
            SlimeWeapon = FindChildByName(transform, "SlimeWeapon")?.gameObject;
        if (teamButton == null)
            teamButton = GetComponentInChildren<Button>(true);
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
}
