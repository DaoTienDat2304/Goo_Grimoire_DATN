using System;
using UnityEngine;
using UnityEngine.UI;

public class showtrait : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject slimeBody;
    public Text slimename;
    public Text typetext;
    public Text rarity;
    public GameObject slimeshow;
    public GameObject specialslime;

    private Slime slime;
    private TraitSO trait;

    public void SetupSlime(TraitSO newSlime)
    {
        trait = newSlime;
        UpdateUI();
    }

    Color traitcolor(TraitSO A)
    {
        switch (A.rarity)
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
            case Rarity.Secret:
                return Color.HSVToRGB(21, 22, 44);
        }
        return Color.magenta;
    }

    private void UpdateUI()
    {
        if (trait.rarity != Rarity.Secret)
        {
            slimename.text = $"Name : {trait.traitName}";
            rarity.text = $"{trait.rarity}";
            rarity.color = traitcolor(trait);
            typetext.text = $"{trait.type}";
        }
        else
        {
            slimename.text = $"Name : {trait.traitName}";
            rarity.text = $"{trait.rarity}";
            rarity.color = traitcolor(trait);
            typetext.text = $"special :";
        }



        if (trait.rarity != Rarity.Secret)
        {
            var bodyRenderer = slimeBody?.GetComponent<SpriteRenderer>();
            bodyRenderer.transform.localScale = Vector3.one * 30;
            bodyRenderer.sprite = trait.sprite;
            bodyRenderer.sortingOrder = 2;
        }
        else
        {
            var bodyRenderer = slimeBody?.GetComponent<SpriteRenderer>();
            bodyRenderer.transform.localScale = Vector3.one * 30;
            bodyRenderer.sprite = trait.sprite;
            bodyRenderer.sortingOrder = 2;
        }
    }
    private void OnEnable()
    {

    }
    //public string Slimename
    //{
        //get { return slime_name; }
        //set { }
    //}

   /* public void submitname()
    {
        if (string.IsNullOrEmpty(name.text) == false)
        {
            slime_name = name.text;
            slime.slimeName = slime_name;
            UpdateUI();
            name.text = null;
        }
        else
        {

        }
    }*/
    public Slime GetSlime()
    {
        return slime;
    }

    // Method to refresh the UI when slime data changes
    public void RefreshUI()
    {
        UpdateUI();
    }
}
