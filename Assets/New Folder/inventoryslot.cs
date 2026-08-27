using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;
using System;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public GameObject slimeBody;
    public GameObject SlimeArmor;
    public GameObject SlimeWeapon;
    public Sprite sprite;
    public Text nameText;
    public Text breedingStatusText;
    public Image backgroundImage;
    public Image selectionBorder;


    private Slime slime;
    private bool isSelected = false;
    public TraitSO trait;
    public SlimeWorldManager worldManager;
    public bool canselect;
    public bool onselect = false;
    public event Action<Slime, bool> OnSacrificeSelectionChanged;

    private void Start()
    {
        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(false);
    }

    public void SetupSlime(Slime newSlime)
    {
        slime = newSlime;
        UpdateUI();
    }

    private void UpdateUI()
    {
        onselect = false;
        if (slime == null) return;
        // Update name
        if (nameText != null)
            nameText.text = slime.slimeName;

        if (worldManager == null)
            worldManager = SlimeWorldManager.Instance != null ? SlimeWorldManager.Instance : FindAnyObjectByType<SlimeWorldManager>();

        var bodyRenderer = slimeBody?.GetComponent<Image>();
        var armorRenderer = SlimeArmor?.GetComponent<Image>();
        var weaponRenderer = SlimeWeapon?.GetComponent<Image>();
        if (bodyRenderer != null)
        {
            bodyRenderer.transform.localScale = Vector3.one;
            bodyRenderer.sprite = (slime != null ? slime.body?.sprite : null) ?? (worldManager != null ? worldManager.CreateDefaultSlimeSprite() : null);
        }
        if (armorRenderer != null)
        {
            armorRenderer.transform.localScale = Vector3.one;
            armorRenderer.sprite = (slime != null ? slime.armor?.sprite : null) ?? (worldManager != null ? worldManager.CreateDefaultSlimeSprite() : null);
        }
        if (weaponRenderer != null)
        {
            weaponRenderer.transform.localScale = Vector3.one;
            weaponRenderer.sprite = (slime != null ? slime.weapon?.sprite : null) ?? (worldManager != null ? worldManager.CreateDefaultSlimeSprite() : null);
        }
       
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (slime != null)
        {
            if(canselect == false)
            {
                GetComponentInParent<TraitCollectionManager>().slimeinfo(slime,this.gameObject.transform);
            }
            else
            {
                SetBreedingSelected(!onselect);
            }
        }
    }

    public void SetBreedingSelected(bool selected)
    {
        bool changed = onselect != selected;
        onselect = selected;
        if (backgroundImage != null)
            backgroundImage.color = onselect ? Color.black : Color.white;
        if (changed)
            OnSacrificeSelectionChanged?.Invoke(slime, onselect);
    }

    public void removedslime()
    {
        if(onselect == true)
        {
            BreedingManager breedingManager = BreedingManager.Instance != null ? BreedingManager.Instance : FindAnyObjectByType<BreedingManager>();
            if (breedingManager == null || slime == null) return;

            int points = SlimeInventory.SacrificePoints(SelectiveBreeding.GetSlimeRarity(slime));
            var inventory = GetComponentInParent<SlimeInventory>();
            if (inventory != null) inventory.sacrifice += points;
            
            if (slime != null && slime.isPicked)
            {
                Team team = null;
                if (SaveAndLoadSystem.Instance != null)
                {
                    var field = typeof(SaveAndLoadSystem).GetField("teamSlime", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        team = field.GetValue(SaveAndLoadSystem.Instance) as Team;
                    }
                }
                
                if (team == null)
                {
                    var allTeams = Resources.FindObjectsOfTypeAll<Team>();
                    if (allTeams != null && allTeams.Length > 0)
                    {
                        team = allTeams[0];
                    }
                }
                
                if (team != null && team.team != null)
                {
                    team.team.Remove(slime);
                    slime.isPicked = false;
                }
            }

            breedingManager.removeslime(slime);
            SaveAndLoadSystem.Instance?.MarkSlimeCollectionChanged();
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(selected);
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
