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

    }

    public void SetupSlime(int newSlime)
    {
        
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
        var bodyRenderer = slimeBody?.GetComponent<Image>();
        var armorRenderer = SlimeArmor?.GetComponent<Image>();
        var weaponRenderer = SlimeWeapon?.GetComponent<Image>();
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
}
