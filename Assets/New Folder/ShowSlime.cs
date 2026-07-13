using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Unity.VisualScripting;

public class ShowSlime : MonoBehaviour
{

    [Header("UI Elements")]
    public GameObject slimeBody;
    public GameObject SlimeArmor;
    public GameObject SlimeWeapon;
    public Text slimename;
    public Text bodytext;
    public Text weapontext;
    public Text armortext;
    public Text special;
    public Text des;
    public GameObject slimeshow;
    public GameObject specialslime;
    public InputField named;
    public string slime_name;

    public Text HP;
    public Text DEF;
    public Text ATK;
    public Text SPD;
    public Text EVA;


    private Slime slime;
    private TraitSO trait;
    private bool isSelected = false;

    public void SetupSlime(Slime newSlime)
    {
        slime = newSlime;
        UpdateUI();
    }

    Color traitcolor(TraitInstance A)
    {
        switch(A.Rarity)
        {
            case Rarity.Common:
                return Color.gray;
            case Rarity.Uncommon:
                return Color.green;
            case Rarity.Rare:
                return Color.blue;
            case Rarity.UltraRare:
                return Color.yellow;
            case Rarity.Legendary:
                return Color.red;
            case Rarity.Mythic:
                return Color.black;
        }
        return Color.magenta;
    }

    private void UpdateUI()
    {
        if(slime.body.Rarity != Rarity.Secret)
        {
            slimeshow.SetActive(true);
            specialslime.SetActive(false);
            slimename.text = $"Name : {slime.slimeName}";
            bodytext.text = $"{slime.body.Rarity}";
            bodytext.color = traitcolor(slime.body);
            weapontext.text = $"{slime.weapon.Rarity}";
            weapontext.color = traitcolor(slime.weapon);
            armortext.text = $"{slime.armor.Rarity}";
            armortext.color = traitcolor(slime.armor);
        }   
        else
        {
            slimeshow.SetActive(false);
            specialslime.SetActive(true);
            special.text = $"Name : {slime.slimeName}";
        }

        HP.text = $"Health : {slime.totalHP}";
        DEF.text = $"Defense : {slime.totalDefense}";
        ATK.text = $"Attack : {slime.totalAttack}";
        SPD.text = $"Speed : {slime.totalSpeed}";
        EVA.text = $"crit : {slime.totalCritRate:P0} (x{slime.totalCritDMG:0.##})";


        if (slime.body.Rarity != Rarity.Secret)
        {
            var bodyRenderer = slimeBody?.GetComponent<SpriteRenderer>();
            var armorRenderer = SlimeArmor?.GetComponent<SpriteRenderer>();
            var weaponRenderer = SlimeWeapon?.GetComponent<SpriteRenderer>();
            bodyRenderer.transform.localScale = Vector3.one * 30;
            armorRenderer.transform.localScale = Vector3.one;
            weaponRenderer.transform.localScale = Vector3.one;
            bodyRenderer.sprite = (slime != null ? slime.body?.sprite : null);
            bodyRenderer.sortingOrder = 2;
            armorRenderer.sprite = (slime != null ? slime.armor?.sprite : null);
            armorRenderer.sortingOrder = 3;
            weaponRenderer.sprite = (slime != null ? slime.weapon?.sprite : null);
            weaponRenderer.sortingOrder = 4;
        }
        else
        {
            var bodyRenderer = slimeBody?.GetComponent<SpriteRenderer>();
            var armorRenderer = SlimeArmor?.GetComponent<SpriteRenderer>();
            var weaponRenderer = SlimeWeapon?.GetComponent<SpriteRenderer>();
            armorRenderer.sprite = null;
            weaponRenderer.sprite = null;
            bodyRenderer.transform.localScale = Vector3.one * 30;
            bodyRenderer.sprite = (slime != null ? slime.body?.sprite : null);
            bodyRenderer.sortingOrder = 2;
        }
    }
    private void OnEnable()
    {
        if (named != null)
        {
            named.text = null;
        }

        if(SlimeArmor != null)
        {
            var armorRenderer = SlimeArmor?.GetComponent<SpriteRenderer>();
            var weaponRenderer = SlimeWeapon?.GetComponent<SpriteRenderer>();
            armorRenderer.sprite = null;
            weaponRenderer.sprite = null;
        }

    }
    public string Slimename
    {
        get { return slime_name; }
        set { }
    }

    public void submitname()
    {
        if(string.IsNullOrEmpty(named.text) == false)
        {
            slime_name = named.text;
            slime.slimeName = slime_name;
            UpdateUI();
            named.text = null;
        }
        else
        {
     
        }
    }
    public Slime GetSlime()
    {
        return slime;
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    // Method to refresh the UI when slime data changes
    public void RefreshUI()
    {
        UpdateUI();
    }
}

