using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Unity.VisualScripting;

public class traitCollectionslot : MonoBehaviour, IPointerClickHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("UI Elements")]
    public GameObject slimeBody;
    public TraitSO Trait;
    private void Start()
    {

    }

    public void SetupSlime(TraitSO trait)
    {
        Trait = trait;
        UpdateUI();
    }

    private void UpdateUI()
    {
        var bodyRenderer = slimeBody?.GetComponent<Image>();
        bodyRenderer.sprite = Trait.sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GetComponentInParent<TraitCollectionManager>().traitinfo(Trait, this.gameObject.transform);
    }

    

    // Method to refresh the UI when slime data changes
    public void RefreshUI()
    {
        UpdateUI();
    }
}
