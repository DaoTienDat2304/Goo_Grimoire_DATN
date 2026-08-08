#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BreedingBookUIBuilder
{
    private const string ScenePath = "Assets/Scenes/firstsave.unity";
    private const string SpriteRoot = "Assets/Sprite/NewAsset/UI_breeding/";
    private const string ParentSlotPrefabPath = "Assets/Prefab/BreedingParentSlot.prefab";
    private const string CollectionSlotPrefabPath = "Assets/Prefab/BreedingCollectionSlot.prefab";

    private static readonly Color Ink = new Color32(69, 38, 23, 255);
    private static readonly Color MutedInk = new Color32(118, 77, 48, 255);
    private static readonly Color Purple = new Color32(113, 38, 143, 255);
    private static readonly Color Gold = new Color32(218, 146, 48, 255);
    private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

    [MenuItem("Tools/Goo Grimoire/Rebuild Clean Breeding UI")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        BreedingUIManager manager = UnityEngine.Object.FindFirstObjectByType<BreedingUIManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            manager = UnityEngine.Object.FindFirstObjectByType<BreedingUIManager>(FindObjectsInactive.Include);
        }
        if (manager == null) throw new Exception("Khong tim thay BreedingUIManager.");

        Canvas canvas = FindRootCanvas();
        if (canvas == null) throw new Exception("Khong tim thay Canvas.");

        SlimeWorldManager worldManager = UnityEngine.Object.FindFirstObjectByType<SlimeWorldManager>(FindObjectsInactive.Include);
        Transform host = worldManager != null && worldManager.breedUI != null
            ? worldManager.breedUI.transform
            : Find(canvas.transform, "BreedingUI");
        if (host == null)
        {
            host = StretchNode("BreedingUI", canvas.transform).transform;
        }
        if (host.parent != canvas.transform)
            host.SetParent(canvas.transform, false);
        DestroyDuplicateBreedingUI(host);
        DestroyAllBreedingRoots();
        PrepareHost(host);
        ClearChildren(host);

        GameObject parentSlotPrefab = BuildSlotPrefab(true);
        GameObject collectionSlotPrefab = BuildSlotPrefab(false);
        AssetDatabase.SaveAssets();
        if (parentSlotPrefab == null)
            parentSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ParentSlotPrefabPath);
        if (collectionSlotPrefab == null)
            collectionSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionSlotPrefabPath);
        if (parentSlotPrefab == null || collectionSlotPrefab == null)
            throw new Exception("Khong load duoc prefab slot sau khi tao.");
        GameObject root = StretchNode("BreedingBookRoot", host).gameObject;
        root.AddComponent<Image>().color = new Color32(25, 21, 20, 210);

        GameObject book = Node("Book", root.transform, Vector2.zero, new Vector2(1720f, 980f));
        Image bookImage = book.AddComponent<Image>();
        bookImage.sprite = Sprite("Book.png");
        bookImage.preserveAspect = false;
        bookImage.raycastTarget = true;

        BuildHeader(book.transform, manager, out Button closeButton);

        GameObject content = Node("Content", book.transform, new Vector2(0f, -25f), new Vector2(1180f, 690f));
        content.transform.localScale = Vector3.one * 1.23f;
        GameObject left = Node("LeftPage_ParentSelection", content.transform, new Vector2(-294f, 0f), new Vector2(540f, 650f));
        GameObject right = Node("RightPage_SlimeCollection", content.transform, new Vector2(294f, 0f), new Vector2(540f, 650f));

        BuildLeftPage(left.transform, out GameObject breedingPanel, out Transform parentGrid,
            out Button breedButton, out Button cancelButton, out Image coinIcon, out Text costText,
            out Text previewText, out GameObject progressPanel, out Slider progressBar,
            out Text statusText, out Button gemButton, out Image gemIcon, out Text gemText);

        BuildRightPage(right.transform, out GameObject collectionPanel, out Transform collectionGrid,
            out Text counterText);

        manager.breedingUIRoot = host.gameObject;
        manager.closeButton = closeButton;
        manager.breedingPanel = breedingPanel;
        manager.slimeCollectionPanel = collectionPanel;
        manager.breedingProgressPanel = progressPanel;
        manager.slimeGridParent = parentGrid;
        manager.slimeSlotPrefab = parentSlotPrefab;
        manager.collectionGridParent = collectionGrid;
        manager.collectionSlotPrefab = collectionSlotPrefab;
        manager.breedButton = breedButton;
        manager.cancelButton = cancelButton;
        manager.breedingProgressBar = progressBar;
        manager.breedingStatusText = statusText;
        manager.selectedSlimesText = null;
        manager.breedingPreviewText = previewText;
        manager.costCoinIcon = coinIcon;
        manager.breedingCostText = costText;
        manager.finishWithGemsButton = gemButton;
        manager.gemIcon = gemIcon;
        manager.gemCostText = gemText;
        manager.slimeCounterText = counterText;
        manager.createMissingUIAtRuntime = false;
        EditorUtility.SetDirty(manager);

        breedingPanel.SetActive(true);
        collectionPanel.SetActive(true);
        progressPanel.SetActive(false);
        host.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = root;
        Debug.Log("[BreedingUI] Clean two-page UI rebuilt. Root: BreedingUI/BreedingBookRoot");
    }

    private static void BuildHeader(Transform parent, BreedingUIManager manager, out Button close)
    {
        GameObject header = Node("Header", parent, new Vector2(0f, 415f), new Vector2(1180f, 90f));
        header.transform.localScale = Vector3.one * 1.2f;
        Image titleArt = ImageNode("Title_Breeding", header.transform, Sprite("Breeding.png"), new Vector2(-295f, 0f), new Vector2(430f, 110f));
        titleArt.preserveAspect = true;
        Label("PageTitle", header.transform, "Breeding", 26, FontStyle.Normal, new Vector2(-295f, 3f), new Vector2(300f, 42f), TextAnchor.MiddleCenter, Color.white);
        Label("CollectionTitle", header.transform, "Breeding hien co", 21, FontStyle.Normal, new Vector2(275f, 3f), new Vector2(330f, 40f), TextAnchor.MiddleCenter, Ink);
        close = TextButton("CloseButton", header.transform, "X", new Vector2(575f, 8f), new Vector2(52f, 52f), new Color32(190, 31, 18, 255));
    }

    private static void BuildLeftPage(Transform parent, out GameObject panel, out Transform parentGrid,
        out Button breed, out Button cancel, out Image coin, out Text cost, out Text preview,
        out GameObject progressPanel, out Slider progress, out Text status, out Button gemButton,
        out Image gemIcon, out Text gemText)
    {
        panel = StretchNode("BreedingPanel", parent).gameObject;
        panel.AddComponent<Image>().color = Clear;
        Label("ParentALabel", panel.transform, "Parent A", 15, FontStyle.Normal, new Vector2(-138f, 232f), new Vector2(160f, 28f), TextAnchor.MiddleCenter, Ink);
        Label("ParentBLabel", panel.transform, "Parent B", 15, FontStyle.Normal, new Vector2(138f, 232f), new Vector2(160f, 28f), TextAnchor.MiddleCenter, Ink);
        ImageNode("Plus", panel.transform, Sprite("Dấu cộng.png"), new Vector2(0f, 143f), new Vector2(58f, 58f)).preserveAspect = true;

        parentGrid = Node("ParentSlots", panel.transform, new Vector2(0f, 130f), new Vector2(430f, 170f)).transform;
        GridLayoutGroup parents = parentGrid.gameObject.AddComponent<GridLayoutGroup>();
        parents.cellSize = new Vector2(174f, 174f);
        parents.spacing = new Vector2(90f, 0f);
        parents.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        parents.constraintCount = 2;
        parents.childAlignment = TextAnchor.MiddleCenter;

        Image arrow = ImageNode("ResultArrow", panel.transform, Sprite("Mũi tên result.png"), new Vector2(0f, 22f), new Vector2(170f, 120f));
        arrow.preserveAspect = true;
        Label("ResultLabel", panel.transform, "Result", 15, FontStyle.Normal, new Vector2(0f, -34f), new Vector2(120f, 24f), TextAnchor.MiddleCenter, Ink);
        Image resultFrame = ImageNode("ResultFrame", panel.transform, Sprite("KhungSlime.png"), new Vector2(0f, -110f), new Vector2(145f, 145f));
        resultFrame.preserveAspect = true;
        Image resultSlime = ImageNode("ResultPreview", resultFrame.transform, Sprite("Slime.png"), new Vector2(0f, 7f), new Vector2(95f, 95f));
        resultSlime.preserveAspect = true;
        Image resultName = ImageNode("ResultNameFrame", panel.transform, Sprite("Khung name slime.png"), new Vector2(0f, -186f), new Vector2(190f, 48f));
        resultName.preserveAspect = true;
        Label("ResultName", resultName.transform, "Slime moi", 15, FontStyle.Normal, Vector2.zero, new Vector2(145f, 25f), TextAnchor.MiddleCenter, Color.white);

        GameObject summary = Node("BreedingSummary", panel.transform, new Vector2(0f, -244f), new Vector2(440f, 75f));
        Image costCard = ImageNode("BreedingCostCard", summary.transform, Sprite("BreedingCost.png"), new Vector2(-150f, 0f), new Vector2(140f, 72f));
        costCard.preserveAspect = true;
        GameObject valueCover = Panel("DynamicCost", costCard.transform, new Vector2(34f, -12f), new Vector2(58f, 27f), new Color32(248, 218, 171, 255));
        coin = ImageNode("CoinIcon", valueCover.transform, null, new Vector2(-20f, 0f), new Vector2(1f, 1f));
        cost = Label("BreedingCostText", valueCover.transform, string.Empty, 15, FontStyle.Bold, new Vector2(5f, 0f), new Vector2(54f, 24f), TextAnchor.MiddleCenter, new Color32(77, 36, 111, 255));
        preview = Label("BreedingPreviewText", summary.transform, "Chon 2 slime", 12, FontStyle.Normal, new Vector2(0f, -47f), new Vector2(250f, 22f), TextAnchor.MiddleCenter, Ink);
        coin.gameObject.SetActive(false);
        breed = SpriteTextButton("BreedButton", summary.transform, Sprite("BreedButton.png"), "Breed!", Vector2.zero, new Vector2(160f, 66f));
        cancel = TextButton("CancelButton", panel.transform, "Bo chon", new Vector2(0f, -296f), new Vector2(105f, 32f), new Color32(132, 82, 57, 255));
        Image rate = ImageNode("SuccessRate", summary.transform, Sprite("Succese rate.png"), new Vector2(150f, 0f), new Vector2(140f, 72f));
        rate.preserveAspect = true;
        Image tip = ImageNode("Tip", panel.transform, Sprite("Tip.png"), new Vector2(0f, -328f), new Vector2(440f, 76f));
        tip.preserveAspect = true;

        progressPanel = Panel("BreedingProgressPanel", panel.transform, new Vector2(0f, 5f), new Vector2(470f, 380f), new Color32(250, 219, 170, 250));
        progressPanel.transform.SetAsLastSibling();
        Label("ProgressTitle", progressPanel.transform, "DANG LAI TAO", 24, FontStyle.Bold, new Vector2(0f, 120f), new Vector2(360f, 40f), TextAnchor.MiddleCenter, Ink);
        status = Label("BreedingStatusText", progressPanel.transform, "Dang chuan bi...", 19, FontStyle.Normal, new Vector2(0f, 50f), new Vector2(400f, 90f), TextAnchor.MiddleCenter, Ink);
        progress = SliderNode("BreedingProgressBar", progressPanel.transform, new Vector2(0f, -30f), new Vector2(390f, 28f));
        gemButton = TextButton("FinishWithGemsButton", progressPanel.transform, "HOAN THANH NGAY", new Vector2(0f, -105f), new Vector2(270f, 56f), Purple);
        gemIcon = ImageNode("GemIcon", gemButton.transform, Sprite("14_Image_16.png"), new Vector2(-92f, 0f), new Vector2(25f, 25f));
        gemText = Label("GemCostText", gemButton.transform, "0", 18, FontStyle.Bold, new Vector2(92f, 0f), new Vector2(45f, 28f), TextAnchor.MiddleCenter, Color.white);
    }

    private static void BuildRightPage(Transform parent, out GameObject panel, out Transform grid, out Text counter)
    {
        panel = StretchNode("CollectionPanel", parent).gameObject;
        panel.AddComponent<Image>().color = Clear;
        GameObject viewport = Panel("CollectionViewport", panel.transform, new Vector2(0f, 15f), new Vector2(490f, 500f), Clear);
        RectMask2D mask = viewport.AddComponent<RectMask2D>();
        mask.padding = new Vector4(5f, 5f, 5f, 5f);
        grid = Node("CollectionGrid", viewport.transform, Vector2.zero, new Vector2(456f, 480f)).transform;
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(104f, 142f);
        layout.spacing = new Vector2(12f, 15f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.childAlignment = TextAnchor.UpperCenter;

        GameObject footer = Node("CollectionFooter", panel.transform, new Vector2(0f, -280f), new Vector2(490f, 52f));
        ImageButton("PreviousPageButton", footer.transform, Sprite("Button chuyển trang trái.png"), new Vector2(-135f, 0f), new Vector2(48f, 40f));
        for (int i = 0; i < 6; i++)
        {
            Image dot = ImageNode("PageDot_" + (i + 1), footer.transform,
                Sprite(i == 0 ? "Dấu chấm trang đang ở.png" : "Dấu chấm trang trống.png"),
                new Vector2(-65f + i * 27f, 0f), new Vector2(15f, 15f));
            dot.preserveAspect = true;
        }
        ImageButton("NextPageButton", footer.transform, Sprite("Button chuyển trang phải.png"), new Vector2(135f, 0f), new Vector2(48f, 40f));
        counter = Label("SlimeCounterText", panel.transform, "0/0", 14, FontStyle.Bold, new Vector2(205f, -280f), new Vector2(70f, 28f), TextAnchor.MiddleCenter, Ink);
    }

    private static GameObject BuildSlotPrefab(bool parentSlot)
    {
        string objectName = parentSlot ? "BreedingParentSlot" : "BreedingCollectionSlot";
        string prefabPath = parentSlot ? ParentSlotPrefabPath : CollectionSlotPrefabPath;
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null) return existingPrefab;

        Vector2 slotSize = parentSlot ? new Vector2(174f, 174f) : new Vector2(104f, 142f);
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(SlimeSlotUI));
        root.layer = 5;
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = slotSize;
        rt.localScale = Vector3.one;
        Image background = root.GetComponent<Image>();
        background.sprite = Sprite(parentSlot ? "KhungSlime.png" : "Khung slime.png");
        background.preserveAspect = true;
        background.color = Color.white;

        Vector2 slimeSize = parentSlot ? new Vector2(112f, 112f) : new Vector2(76f, 76f);
        float bodyY = parentSlot ? 8f : 17f;
        GameObject body = SpriteLayer("Body", root.transform, new Vector2(0f, bodyY), slimeSize);
        GameObject armor = SpriteLayer("Armor", body.transform, Vector2.zero, slimeSize);
        GameObject weapon = SpriteLayer("Weapon", body.transform, Vector2.zero, slimeSize);
        Text name = Label("NameText", root.transform, "Chua chon", parentSlot ? 14 : 11, FontStyle.Normal,
            new Vector2(0f, parentSlot ? -63f : -38f), new Vector2(parentSlot ? 130f : 88f, 22f), TextAnchor.MiddleCenter, Ink);
        Text state = Label("StatusText", root.transform, string.Empty, 10, FontStyle.Normal,
            new Vector2(0f, parentSlot ? 66f : 57f), new Vector2(88f, 17f), TextAnchor.MiddleCenter, MutedInk);
        Image border = ImageNode("SelectionBorder", root.transform, Sprite(parentSlot ? "KhungSlime.png" : "Khung slime.png"), Vector2.zero, slotSize + new Vector2(8f, 8f));
        border.color = Gold;
        border.raycastTarget = false;
        border.gameObject.SetActive(false);

        SlimeSlotUI slot = root.GetComponent<SlimeSlotUI>();
        slot.slimeBody = body;
        slot.SlimeArmor = armor;
        slot.SlimeWeapon = weapon;
        slot.nameText = name;
        slot.breedingStatusText = state;
        slot.backgroundImage = background;
        slot.selectionBorder = border;
        slot.normalColor = Color.white;
        slot.selectedColor = Gold;
        slot.readyColor = new Color32(67, 125, 60, 255);
        slot.breedingColor = new Color32(155, 64, 50, 255);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void PrepareHost(Transform host)
    {
        host.gameObject.layer = 5;
        RectTransform rt = host as RectTransform;
        if (rt == null) rt = host.gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        Image image = host.GetComponent<Image>();
        if (image != null) image.color = Clear;
    }

    private static GameObject SpriteLayer(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = Node(name, parent, pos, size);
        Image image = go.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return go;
    }

    private static GameObject Panel(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = Node(name, parent, pos, size);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return go;
    }

    private static Button TextButton(string name, Transform parent, string value, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = Node(name, parent, pos, size);
        Image image = go.AddComponent<Image>();
        image.sprite = Sprite("09_Image_11.png");
        image.type = Image.Type.Sliced;
        image.color = color;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.85f, 1f);
        button.colors = colors;
        Label("Label", go.transform, value, 17, FontStyle.Bold, Vector2.zero, size, TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    private static Button SpriteTextButton(string name, Transform parent, Sprite sprite, string value, Vector2 pos, Vector2 size)
    {
        GameObject go = Node(name, parent, pos, size);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        Label("Label", go.transform, value, 17, FontStyle.Normal, Vector2.zero, new Vector2(110f, 30f), TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    private static Button ImageButton(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        Image image = ImageNode(name, parent, sprite, pos, size);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Slider SliderNode(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject root = Node(name, parent, pos, size);
        Slider slider = root.AddComponent<Slider>();
        Image bg = ImageNode("Background", root.transform, null, Vector2.zero, size);
        bg.color = new Color32(121, 73, 45, 255);
        RectTransform fillArea = Node("FillArea", root.transform, Vector2.zero, new Vector2(size.x - 8f, size.y - 8f)).GetComponent<RectTransform>();
        Image fill = ImageNode("Fill", fillArea, null, Vector2.zero, fillArea.sizeDelta);
        fill.color = Gold;
        slider.targetGraphic = bg;
        slider.fillRect = fill.rectTransform;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    private static Text Label(string name, Transform parent, string value, int fontSize, FontStyle style,
        Vector2 pos, Vector2 size, TextAnchor alignment, Color color)
    {
        GameObject go = Node(name, parent, pos, size);
        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static Image ImageNode(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject go = Node(name, parent, pos, size);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite == null ? Color.white : Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static GameObject Node(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        return go;
    }

    private static RectTransform StretchNode(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        return rt;
    }

    private static Sprite Sprite(string file)
    {
        string path = SpriteRoot + file;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        foreach (UnityEngine.Object item in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            if (item is Sprite found) return found;
        return null;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = Find(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private static Canvas FindRootCanvas()
    {
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name == "Canvas" && canvases[i].transform.parent == null)
                return canvases[i];
        }
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].isRootCanvas) return canvases[i];
        }
        return null;
    }

    private static void DestroyAllNamedDescendants(Transform parent, string name)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            else
                DestroyAllNamedDescendants(child, name);
        }
    }

    private static void DestroyAllBreedingRoots()
    {
        RectTransform[] rects = UnityEngine.Object.FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = rects.Length - 1; i >= 0; i--)
        {
            if (rects[i] != null && rects[i].name == "BreedingBookRoot")
                UnityEngine.Object.DestroyImmediate(rects[i].gameObject);
        }
    }

    private static void DestroyDuplicateBreedingUI(Transform keep)
    {
        RectTransform[] rects = UnityEngine.Object.FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = rects.Length - 1; i >= 0; i--)
        {
            if (rects[i] != null && rects[i] != keep && rects[i].name == "BreedingUI")
                UnityEngine.Object.DestroyImmediate(rects[i].gameObject);
        }
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }
}
#endif
