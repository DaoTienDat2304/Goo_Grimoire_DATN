using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildingSlot : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public int slotID;
    public bool isOccupied = false;
    public GameObject buildingMenu;
    public BuildingMenuManager buildingMenuManager;
    public Image placedBuildingIcon;
    public SlimeWorldManager slimeWorldManager;
    public int slotIndex;
    public SaveAndLoadSystem saveAndLoadSystem;
    public GameObject TowerPanel;
    public GameObject shop;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        buildingMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // Play building click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBuildingClickSFX();
        }

        if (isOccupied == false) buildingMenuManager.ToggleMenu();
        if (isOccupied == true && slotID == 1)
        {
            slimeWorldManager.StartBreedingView();
        }
        if (isOccupied == true && slotID == 2)
        {
            saveAndLoadSystem.Save();
            slimeWorldManager.ClearWorldSlimes();
            GameObject map = GameObject.Find("MapSelection");
            map.transform.GetChild(0).gameObject.SetActive(true);
        }
        if (isOccupied == true && slotID == 3)
        {
            slimeWorldManager.StartinventoryView();
            slimeWorldManager.ClearWorldSlimes();
        }
        if (isOccupied == true && slotID == 4)
        {
            slimeWorldManager.ClearWorldSlimes();
            TowerPanel.SetActive(true);
        }
        if(isOccupied == true && slotID == 5)
        {
            slimeWorldManager.ClearWorldSlimes();
            shop.SetActive(true);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isOccupied == true) return;

        var dragged = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<BuildingDraggable>() : null;
        if (dragged == null || dragged.building == null) return;
        if (ArchievementManager.Instance != null)
        {
            ArchievementManager.Instance.GetArchivement(2); // 0 = Breed achievement
        }
        PlaceBuilding(dragged.building);
        if (buildingMenu != null) buildingMenu.SetActive(false);
    }

    private void PlaceBuilding(Building building)
    {
        if (isOccupied == false)
        {
            // Kiểm tra và trừ tiền trước khi xây dựng
            if (building.CanAfford())
            {
                // Trừ tiền
                bool purchaseSuccess = building.Purchase();
                if (purchaseSuccess)
                {
                    // Xây dựng thành công
                    if (placedBuildingIcon != null)
                    {
                        placedBuildingIcon.sprite = building.sprite;
                        placedBuildingIcon.enabled = true;
                        isOccupied = true;
                        slotID = building.buildingID;

                        Debug.Log($"Đã xây dựng {building.buildingName} với chi phí: {building.GetCostDescription()}");

                        // Refresh menu để cập nhật trạng thái building (làm nhạt và disable drag)
                        if (buildingMenuManager != null)
                        {
                            buildingMenuManager.RefreshMenu();
                        }

                        SaveAndLoadSystem.Instance?.Save();
                    }
                }
                else
                {
                    Debug.Log($"Không thể xây dựng {building.buildingName}: Không đủ tiền!");
                }
            }
            else
            {
                Debug.Log($"Không thể xây dựng {building.buildingName}: Không đủ tiền! Cần: {building.GetCostDescription()}");
            }
        }
        else return;
        
    }
}
