using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

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
    [Header("Simple Collection Card")]
    public Image slimeImage;
    public TMP_Text slimeNameText;

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

    private void Awake()
    {
        ResolveSimpleCardReferences();
    }

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
        ResolveSimpleCardReferences();
        slime = newSlime;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (slime == null)
        {
            if (nameText != null) nameText.text = "Empty";
            if (slimeNameText != null) slimeNameText.text = string.Empty;
            if (slimeImage != null)
            {
                slimeImage.sprite = null;
                slimeImage.enabled = false;
            }
            if (breedingStatusText != null) breedingStatusText.text = string.Empty;
            SetSprite(slimeBody, null);
            SetSprite(SlimeArmor, null);
            SetSprite(SlimeWeapon, null);
            if (selectionBorder != null) selectionBorder.gameObject.SetActive(false);
            return;
        }

        // Update name
        if (nameText != null)
            nameText.text = slime.slimeName;
        if (slimeNameText != null)
            slimeNameText.text = slime.slimeName;
        if (IsSimpleCollectionCard())
        {
            if (HasCompositeSlime())
            {
                UpdateCompositeSlime();
            }
            else if (slimeImage != null)
            {
                slimeImage.sprite = slime.body?.sprite;
                slimeImage.enabled = slimeImage.sprite != null;
            }
            return;
        }
        

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
            UpdateCompositeSlime();
    }

    private static void SetSprite(GameObject target, Sprite value)
    {
        Image image = target != null ? target.GetComponent<Image>() : null;
        if (image != null) image.sprite = value;
    }

    private bool IsSimpleCollectionCard()
    {
        return slimeImage != null || slimeNameText != null;
    }

    private bool HasCompositeSlime()
    {
        return slimeBody != null || SlimeArmor != null || SlimeWeapon != null;
    }

    private void UpdateCompositeSlime()
    {
        SetLayerSprite(slimeBody, slime?.body?.sprite);
        SetLayerSprite(SlimeArmor, slime?.armor?.sprite);
        SetLayerSprite(SlimeWeapon, slime?.weapon?.sprite);
    }

    private static void SetLayerSprite(GameObject target, Sprite value)
    {
        if (target == null) return;
        Image image = target.GetComponent<Image>();
        if (image == null) return;
        image.sprite = value;
        image.enabled = value != null;
    }

    private void ResolveSimpleCardReferences()
    {
        if (slimeImage == null)
        {
            Transform slimeTransform = transform.Find("Slime");
            if (slimeTransform != null)
                slimeImage = slimeTransform.GetComponent<Image>();
        }

        if (slimeNameText == null)
        {
            Transform nameTransform = transform.Find("Name");
            if (nameTransform != null)
                slimeNameText = nameTransform.GetComponent<TMP_Text>();
        }


        if (slimeBody == null)
            slimeBody = FindChild("slimeBody")?.gameObject;
        if (SlimeArmor == null)
            SlimeArmor = FindChild("SlimeArmor")?.gameObject;
        if (SlimeWeapon == null)
            SlimeWeapon = FindChild("SlimeWeapon")?.gameObject;
    }


    private Transform FindChild(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
            if (child.name == childName) return child;
        return null;
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
