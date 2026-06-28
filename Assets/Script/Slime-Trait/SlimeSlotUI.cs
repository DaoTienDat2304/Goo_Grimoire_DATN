using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class SlimeSlotUI : MonoBehaviour, IPointerClickHandler
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
    public Button teamButton;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color breedingColor = Color.red;
    public Color readyColor = Color.white;

    private Slime slime;
    private bool isSelected = false;

    public event Action<Slime> OnSlimeSelected;
    public SlimeWorldManager worldManager;
    public Team teamSlime;
    public void PickTeam()
    {
        if (!slime.isPicked)
        {
            if (teamSlime.team.Count < 5)
            {
                teamSlime.team.Add(slime);
                slime.isPicked = true;
            }
            else
            {
                Debug.Log("Max member");
            }
            foreach (var item in teamSlime.team)
            {
                Debug.Log($"{item.slimeName} + {teamSlime.team.Count}");
            }
        }
        else
        {
            slime.isPicked = false;
            teamSlime.team.Remove(slime);
        }

    }

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
        if (slime == null) return;

        // Update name
        if (nameText != null)
            nameText.text = slime.slimeName;
        

        // Update breeding status
        if (breedingStatusText != null)
        {

            if (slime.canBreed)
            {
                breedingStatusText.text = "Ready";
                breedingStatusText.color = readyColor;
            }
            else
            {
                int cooldown = Mathf.CeilToInt(slime.breedingCooldown);
                breedingStatusText.text = $"{cooldown}s";
                breedingStatusText.color = breedingColor;
            }
        }

        // Update background color based on breeding status
        if (backgroundImage != null)
        {
            backgroundImage.sprite = sprite;
            if (slime.canBreed)
                {}
            else
                backgroundImage.color = breedingColor;
        }
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
        if (slime != null && eventData.button != PointerEventData.InputButton.Right)
        {
            OnSlimeSelected?.Invoke(slime);
        }
        else
        {
            
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
