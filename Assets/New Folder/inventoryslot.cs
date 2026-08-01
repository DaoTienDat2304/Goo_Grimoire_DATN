using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;

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

            var bodyRenderer = slimeBody?.GetComponent<Image>();
            var armorRenderer = SlimeArmor?.GetComponent<Image>();
            var weaponRenderer = SlimeWeapon?.GetComponent<Image>();
            bodyRenderer.transform.localScale = Vector3.one * 1.3f;
            armorRenderer.transform.localScale = Vector3.one;
            weaponRenderer.transform.localScale = Vector3.one;
            bodyRenderer.sprite = (slime != null ? slime.body?.sprite : null) ?? worldManager.CreateDefaultSlimeSprite();
            armorRenderer.sprite = (slime != null ? slime.armor?.sprite : null) ?? worldManager.CreateDefaultSlimeSprite();
            weaponRenderer.sprite = (slime != null ? slime.weapon?.sprite : null) ?? worldManager.CreateDefaultSlimeSprite();
       
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
        onselect = selected;
        if (backgroundImage != null)
            backgroundImage.color = onselect ? Color.black : Color.white;
    }

    public void removedslime()
    {
        if(onselect == true)
        {
            BreedingManager breedingManager = GameObject.Find("BreedingManager").GetComponent<BreedingManager>();
            breedingManager.removeslime(slime);
            // Điểm hi sinh theo độ hiếm slime (đủ 100 → summon 1 secret).
            int points = SlimeInventory.SacrificePoints(SelectiveBreeding.GetSlimeRarity(slime));
            GetComponentInParent<SlimeInventory>().sacrifice += points;
            
            // Remove slime khỏi team nếu đang trong team
            if (slime != null && slime.isPicked)
            {
                // Tìm Team từ SaveAndLoadSystem hoặc tìm trực tiếp
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
                
                // Nếu không tìm thấy, tìm trong Resources
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
                    Debug.Log($"Đã remove slime {slime.slimeName} khỏi team khi hiến tế");
                }
            }
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
