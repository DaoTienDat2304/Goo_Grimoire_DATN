using Spine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TraitCollectionManager : MonoBehaviour
{
    public BreedingManager breedingManager;
    public Transform bodyCollection;
    public Transform armorCollection;
    public Transform weaponCollection;
    public GameObject traitCollectionSlot;
    private int curSlime;
    public SlimeWorldManager slimeWorldManager;

    public Transform slimeInventory;
    public Transform secretInventory;
    public GameObject collectionSlotPrefab;
    private List<GameObject> slimeSlots = new List<GameObject>();

    public GameObject showslot;
    public GameObject showtraslot;
    public Transform[] gidgud;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (TraitSO trait in SlimeGen.Instance.allTraits)
        {
            trait.unlocked = false;
        }
        Refresh();
        HideAllCollections(3);
        this.gameObject.SetActive(false);
    }

    public void turnonandoff()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    // Update is called once per frame
    void Update()
    {
        if (curSlime != BreedingManager.Instance.GetAllSlimes().Count)
        {
            curSlime = BreedingManager.Instance.GetAllSlimes().Count;
            Refresh();
        }
    }

    private void Refresh()
    {
        foreach (var trait in SlimeGen.Instance.allTraits)
        {
            trait.unlocked = false;
        }
        if (bodyCollection != null)
        {
            foreach (Transform child in bodyCollection)
            {
                Destroy(child.gameObject);
            }
        }
        if (armorCollection != null)
        {
            foreach (Transform child in armorCollection)
            {
                Destroy(child.gameObject);
            }
        }
        if (weaponCollection != null)
        {
            foreach (Transform child in weaponCollection)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var slot in slimeSlots)
        {
            Destroy(slot);
        }
        slimeSlots.Clear();
        collectionAdd();
    }

    private void collectionAdd()
    {
        var allSlimes = BreedingManager.Instance.GetAllSlimes();
        foreach (var slime in allSlimes)
        {
            {
                    GameObject slot = Instantiate(collectionSlotPrefab, slimeInventory);
                    slot.transform.localScale = new Vector3(1.4f, 1.4f);
                    var slotScript = slot.GetComponent<InventorySlot>();
                    slimeSlots.Add(slot);
                    if (slotScript != null)
                    {
                        slotScript.SetupSlime(slime);
                        slotScript.canselect = false;
                    }
            }
            

            foreach (var trait in SlimeGen.Instance.allTraits)
            {
                if (trait == slime.body.baseTrait && trait.unlocked == false && slime.body.Rarity != Rarity.Secret)
                {
                    Sprite bodysprite = slime.body.sprite;
                    GameObject bodySlot = Instantiate(traitCollectionSlot, bodyCollection);
                    bodySlot.GetComponent<traitCollectionslot>().Trait = trait;
                    bodySlot.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    bodySlot.transform.GetChild(1).GetComponent<Transform>().localScale = new Vector3(2.4f, 2.4f, 2.4f);
                    bodySlot.transform.GetChild(1).GetComponentInChildren<Image>().sprite = bodysprite;
                    bodySlot.GetComponentInChildren<Text>().text = slime.body.baseTrait.traitName;
                    trait.unlocked = true;
                }

                if (trait == slime.body.baseTrait&&trait.unlocked == false && slime.body.Rarity == Rarity.Secret)
                {
                    Sprite bodysprite = slime.body.sprite;
                    GameObject bodySlot = Instantiate(traitCollectionSlot, secretInventory);
                    bodySlot.GetComponent<traitCollectionslot>().Trait = trait;
                    bodySlot.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    bodySlot.transform.GetChild(1).GetComponent<Transform>().localScale = new Vector3(2.4f, 2.4f, 2.4f);
                    bodySlot.transform.GetChild(1).GetComponentInChildren<Image>().sprite = bodysprite;
                    bodySlot.GetComponentInChildren<Text>().text = slime.body.baseTrait.traitName;
                    trait.unlocked = true;
                }

                if (trait == slime.armor.baseTrait && trait.unlocked == false)
                {
                    Sprite armorsprite = slime.armor.sprite;
                    GameObject armorSlot = Instantiate(traitCollectionSlot, armorCollection);
                    armorSlot.GetComponent<traitCollectionslot>().Trait = trait;
                    armorSlot.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    armorSlot.transform.GetChild(1).GetComponent<Transform>().localScale = new Vector3(2.4f, 2.4f, 2.4f);
                    armorSlot.transform.GetChild(1).GetComponentInChildren<Image>().sprite = armorsprite;
                    armorSlot.GetComponentInChildren<Text>().text = slime.armor.baseTrait.traitName;
                    trait.unlocked = true;
                }
                if (trait == slime.weapon.baseTrait && trait.unlocked == false)
                {
                    Sprite weaponsprite = slime.weapon.sprite;
                    GameObject weaponSlot = Instantiate(traitCollectionSlot, weaponCollection);
                    weaponSlot.GetComponent<traitCollectionslot>().Trait = trait;
                    weaponSlot.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    weaponSlot.transform.GetChild(1).GetComponent<Transform>().localScale = new Vector3(2.4f, 2.4f, 2.4f);
                    weaponSlot.transform.GetChild(1).GetComponentInChildren<Image>().sprite = weaponsprite;
                    weaponSlot.GetComponentInChildren<Text>().text = slime.weapon.baseTrait.traitName;
                    trait.unlocked = true;
                }
            }
        }
    }
    public void HideAllCollections(int A)
    {
        for (int i = 0;i < gidgud.Length;i++)
        {
            gidgud[i].gameObject.SetActive(false);
        }

        switch(A)
        {
            case 0:
                ShowBodyCollection();
                break;
            case 1:
                ShowArmorCollection();
                break;
            case 2:
                ShowWeaponCollection();
                break;
            case 3:
                ShowSlimeCollection();
                break;
            case 4:
                ShowSecretCollection();
                break;
        }
    }
    public void ShowBodyCollection()
    {
        showslot.SetActive(false);
        gidgud[0].gameObject.SetActive(true);
    }

    public void ShowArmorCollection()
    {
        showslot.SetActive(false);
        gidgud[1].gameObject.SetActive(true);
    }
    public void ShowWeaponCollection()
    {
        showslot.SetActive(false);
        gidgud[2].gameObject.SetActive(true);
    }

    public void ShowSlimeCollection()
    {
        showslot.SetActive(false);
        gidgud[3].gameObject.SetActive(true);
    }

    public void ShowSecretCollection()
    {
        showslot.SetActive(false);
        gidgud[4].gameObject.SetActive(true);
    }

    public void slimeinfo(Slime A,Transform B)
    {
        if (showslot.activeSelf)
        {
            var slotScript = showslot.GetComponentInChildren<ShowSlime>();
            //showslot.transform.position = B.transform.position + new Vector3(0.5f, 0,0);
            if (slotScript != null)
            {
                slotScript.SetupSlime(A);
            }
        }
        else
        {
            showslot.SetActive(true);
            var slotScript = showslot.GetComponentInChildren<ShowSlime>();
            //showslot.transform.position = B.transform.position + new Vector3(0.5f, 0, 0); ;
            if (slotScript != null)
            {
                slotScript.SetupSlime(A);
            }
        }
    }

    public void traitinfo(TraitSO A, Transform B)
    {
        if (showtraslot.activeSelf)
        {
            var slotScript = showtraslot.GetComponentInChildren<showtrait>();
            //showslot.transform.position = B.transform.position + new Vector3(0.5f, 0,0);
            if (slotScript != null)
            {
                slotScript.SetupSlime(A);
            }
        }
        else
        {
            showtraslot.SetActive(true);
            var slotScript = showtraslot.GetComponentInChildren<showtrait>();
            //showslot.transform.position = B.transform.position + new Vector3(0.5f, 0, 0); ;
            if (slotScript != null)
            {
                slotScript.SetupSlime(A);
            }
        }
    }
}
