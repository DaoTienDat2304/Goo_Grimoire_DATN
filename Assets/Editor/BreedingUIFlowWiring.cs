#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BreedingUIFlowWiring
{
    private const string ScenePath = "Assets/Scenes/firstsave.unity";
    private const string CollectionPrefabPath = "Assets/Prefab/BreedingCollectionCard.prefab";

    [MenuItem("Tools/Goo Grimoire/Wire Edited Breeding UI")]
    public static void Wire()
    {
        Scene scene = SceneManager.GetActiveScene();
        BreedingUIManager manager = Object.FindFirstObjectByType<BreedingUIManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            manager = Object.FindFirstObjectByType<BreedingUIManager>(FindObjectsInactive.Include);
        }
        if (manager == null) throw new System.Exception("Khong tim thay BreedingUIManager.");

        Transform root = manager.breedingUIRoot != null ? manager.breedingUIRoot.transform : FindSceneObject("BreedingUI");
        if (root == null) throw new System.Exception("Khong tim thay BreedingUI root.");

        Transform grid = Find(root, "CollectionGrid");
        if (grid == null) throw new System.Exception("Khong tim thay CollectionGrid.");

        GameObject collectionPrefab = CreateCollectionPrefab(grid);
        ClearGrid(grid);

        manager.selectedSlime1Image = Component<Image>(root, "Slime1");
        manager.selectedSlime2Image = Component<Image>(root, "Slime2");
        ConfigureSelectedSlime(manager, Find(root, "Slime1"), true);
        ConfigureSelectedSlime(manager, Find(root, "Slime2"), false);
        manager.mutationPercentText = Component<TMP_Text>(root, "SoPhanTram");
        manager.energyCostText = Component<TMP_Text>(root, "SoNangLuong");
        manager.breedButton = Component<Button>(root, "BreedButton");
        manager.cancelButton = Component<Button>(root, "CancelButton");
        manager.breedingProgressPanel = Find(root, "BreedingProgressPanel")?.gameObject;
        manager.collectionGridParent = grid;
        manager.collectionSlotPrefab = collectionPrefab;
        manager.previousPageButton = Component<Button>(root, "PreviousPageButton");
        manager.nextPageButton = Component<Button>(root, "NextPageButton");
        manager.collectionPageSize = 9;

        manager.pageDots = new Image[6];
        for (int i = 0; i < manager.pageDots.Length; i++)
            manager.pageDots[i] = Component<Image>(root, "PageDot_" + (i + 1));
        manager.activePageDotSprite = manager.pageDots[0] != null ? manager.pageDots[0].sprite : null;
        manager.inactivePageDotSprite = manager.pageDots[1] != null ? manager.pageDots[1].sprite : null;

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = manager.gameObject;
    }

    private static GameObject CreateCollectionPrefab(Transform grid)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionPrefabPath);
        if (grid.childCount == 0)
        {
            if (existing == null) throw new System.Exception("CollectionGrid has no template for prefab creation.");
            return UpgradeCollectionPrefab(existing);
        }

        GameObject source = grid.GetChild(0).gameObject;
        GameObject clone = Object.Instantiate(source);
        clone.name = "BreedingCollectionCard";
        clone.transform.SetParent(null, false);
        RectTransform rect = clone.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(130f, 165f);
        rect.localScale = Vector3.one;

        SlimeSlotUI slot = clone.GetComponent<SlimeSlotUI>();
        if (slot == null) slot = clone.AddComponent<SlimeSlotUI>();
        Transform slime = Find(clone.transform, "Slime");
        Transform name = Find(clone.transform, "Name");
        ConfigureCompositeSlime(slot, slime);
        slot.slimeNameText = name != null ? name.GetComponent<TMP_Text>() : null;
        ConfigureNameText(slot.slimeNameText);
        slot.backgroundImage = clone.GetComponent<Image>();
        slot.nameText = null;
        slot.breedingStatusText = null;

        if (slot.slimeBody == null || slot.SlimeArmor == null || slot.SlimeWeapon == null || slot.slimeNameText == null)
        {
            Object.DestroyImmediate(clone);
            throw new System.Exception("Khung mau phai co Slime va TMP text ten Name.");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clone, CollectionPrefabPath);
        Object.DestroyImmediate(clone);
        return prefab;
    }

    private static GameObject UpgradeCollectionPrefab(GameObject existing)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CollectionPrefabPath);
        SlimeSlotUI slot = root.GetComponent<SlimeSlotUI>();
        Transform slime = Find(root.transform, "Slime");
        ConfigureCompositeSlime(slot, slime);
        slot.slimeNameText = Component<TMP_Text>(root.transform, "Name");
        ConfigureNameText(slot.slimeNameText);
        PrefabUtility.SaveAsPrefabAsset(root, CollectionPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(CollectionPrefabPath);
    }

    private static void ConfigureNameText(TMP_Text text)
    {
        if (text == null) return;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Truncate;
    }

    private static void ConfigureCompositeSlime(SlimeSlotUI slot, Transform slime)
    {
        if (slot == null || slime == null) return;
        Image oldImage = slime.GetComponent<Image>();
        if (oldImage != null) oldImage.enabled = false;

        slot.slimeImage = null;
        slot.slimeBody = EnsureLayer(slime, "slimeBody");
        slot.SlimeArmor = EnsureLayer(slime, "SlimeArmor");
        slot.SlimeWeapon = EnsureLayer(slime, "SlimeWeapon");
    }

    private static GameObject EnsureLayer(Transform parent, string layerName)
    {
        Transform existing = parent.Find(layerName);
        if (existing != null)
            return existing.gameObject;

        GameObject layer = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = layer.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = layer.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.sprite = null;
        image.enabled = false;
        return layer;
    }

    private static void ConfigureSelectedSlime(BreedingUIManager manager, Transform container, bool first)
    {
        if (container == null) return;
        Image containerImage = container.GetComponent<Image>();
        if (containerImage != null) containerImage.enabled = false;
        Image body = EnsureLayer(container, "slimeBody").GetComponent<Image>();
        Image armor = EnsureLayer(container, "SlimeArmor").GetComponent<Image>();
        Image weapon = EnsureLayer(container, "SlimeWeapon").GetComponent<Image>();
        if (first)
        {
            manager.selectedSlime1Body = body;
            manager.selectedSlime1Armor = armor;
            manager.selectedSlime1Weapon = weapon;
        }
        else
        {
            manager.selectedSlime2Body = body;
            manager.selectedSlime2Armor = armor;
            manager.selectedSlime2Weapon = weapon;
        }
    }

    private static void ClearGrid(Transform grid)
    {
        for (int i = grid.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(grid.GetChild(i).gameObject);
    }

    private static T Component<T>(Transform root, string name) where T : Component
    {
        Transform found = Find(root, name);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static Transform FindSceneObject(string name)
    {
        RectTransform[] rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (RectTransform rect in rects)
            if (rect.name == name) return rect;
        return null;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
