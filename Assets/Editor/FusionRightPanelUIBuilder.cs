#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class FusionRightPanelUIBuilder
{
    private const string ScenePath = "Assets/Scenes/firstsave.unity";

    [MenuItem("Tools/Goo Grimoire/Rebuild Fusion Right Panel")]
    public static void RebuildFusionRightPanel()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var inventory = Object.FindAnyObjectByType<SlimeInventory>();
        if (inventory == null)
        {
            Debug.LogError("SlimeInventory not found in firstsave scene.");
            return;
        }

        var right = FindChild(inventory.transform, "Right");
        if (right == null)
        {
            Debug.LogError("Object named Right not found under SlimeInventory.");
            return;
        }

        var backdrop = FindChild(inventory.transform, "Fushion");
        if (backdrop != null)
        {
            inventory.fusionBackdrop = backdrop.gameObject;
            backdrop.SetAsFirstSibling();
            foreach (var graphic in backdrop.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        Slider slider = inventory.Slider != null ? inventory.Slider : right.GetComponentInChildren<Slider>(true);
        RectTransform sliderRect = null;
        Vector2 sliderPosition = new Vector2(0f, -23f);
        Vector2 sliderSize = new Vector2(300f, 28f);
        if (slider != null)
        {
            sliderRect = slider.GetComponent<RectTransform>();
            sliderPosition = sliderRect.anchoredPosition;
            sliderSize = sliderRect.sizeDelta;
            sliderRect.SetParent(right, true);
            slider.gameObject.name = "Slider";
        }

        var old = FindChild(right, "FusionRightRuntimeUI");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        var root = Rect("FusionRightRuntimeUI", right, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        var dark = root.gameObject.AddComponent<Image>();
        dark.color = new Color(0.11f, 0.07f, 0.15f, 0.35f);

        var counter = Text("SelectedSacrificeCounterText", root, "SELECTED (0)", new Vector2(0f, 205f), new Vector2(250f, 30f), 16, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.47f, 1f));
        var scroll = CreateSelectedScroll(root, inventory, out RectTransform content, out GameObject template);

        Text("FusionEnergyLabel", root, "FUSION ENERGY", new Vector2(0f, 2f), new Vector2(190f, 24f), 14, TextAlignmentOptions.Center, Color.white);
        if (slider == null)
        {
            slider = CreateSlider(root, sliderPosition, sliderSize);
            sliderRect = slider.GetComponent<RectTransform>();
        }

        if (slider != null)
        {
            sliderRect.SetParent(root, false);
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = sliderPosition;
            sliderRect.sizeDelta = sliderSize;
            sliderRect.localRotation = Quaternion.identity;
            sliderRect.localScale = Vector3.one;
            slider.direction = Slider.Direction.LeftToRight;
            inventory.Slider = slider;
        }

        var coinPanel = Rect("GoldRequirement", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -96f), new Vector2(180f, 90f));
        var coinBg = coinPanel.gameObject.AddComponent<Image>();
        coinBg.color = new Color(0.08f, 0.05f, 0.11f, 0.45f);
        Text("RequirementLabel", coinPanel, "REQUIREMENT", new Vector2(0f, 34f), new Vector2(180f, 20f), 12, TextAlignmentOptions.Center, Color.white);
        Image("CoinIcon", coinPanel, FindCoinSprite(), new Vector2(0f, 4f), new Vector2(36f, 36f), Color.white);
        Text("GoldText", coinPanel, "GOLD\nx0", new Vector2(0f, -31f), new Vector2(120f, 36f), 13, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.25f, 1f));

        Text("WarningText", root, "Selected slimes will be permanently deleted", new Vector2(0f, -158f), new Vector2(330f, 24f), 12, TextAlignmentOptions.Center, new Color(1f, 0.72f, 0.33f, 1f));
        var deselect = Button("DeselectAllButton", root, "DESELECT ALL", new Vector2(-94f, -205f), new Vector2(150f, 42f));
        var dismantle = Button("DismantleButton", root, "DISMANTLE", new Vector2(94f, -205f), new Vector2(150f, 42f));
        UnityEventTools.AddPersistentListener(deselect.onClick, inventory.ondeseclect);
        UnityEventTools.AddPersistentListener(dismantle.onClick, inventory.ondelete);

        inventory.selectedSacrificeGrid = content;
        inventory.selectedSacrificeBodies = new Image[0];
        inventory.selectedSacrificeCounterText = counter;
        inventory.selectedSacrificeContent = content;
        inventory.selectedSacrificeItemTemplate = template;
        EditorUtility.SetDirty(inventory);
        EditorUtility.SetDirty(scroll);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Fusion Right panel rebuilt under Right.");
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static TMP_Text Text(string name, Transform parent, string value, Vector2 pos, Vector2 size, int fontSize, TextAlignmentOptions anchor, Color color)
    {
        var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 8;
        text.fontSizeMax = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Image Image(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Button Button(string name, Transform parent, string label, Vector2 pos, Vector2 size)
    {
        var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = new Color(0.67f, 0.33f, 0.1f, 1f);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text("Label", rt, label, Vector2.zero, size, 15, TextAlignmentOptions.Center, Color.white);
        return button;
    }

    private static ScrollRect CreateSelectedScroll(Transform parent, SlimeInventory inventory, out RectTransform content, out GameObject template)
    {
        var scrollRt = Rect("SelectedSacrificeScroll", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 106f), new Vector2(330f, 170f));
        var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = Rect("Viewport", scrollRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        var viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        content = Rect("SelectedSacrificeContent", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 0f));
        content.pivot = new Vector2(0.5f, 1f);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.offsetMin = new Vector2(0f, 0f);
        content.offsetMax = new Vector2(0f, 0f);

        var layout = content.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(92f, 78f);
        layout.spacing = new Vector2(18f, 12f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.childAlignment = TextAnchor.UpperCenter;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;

        var item = Rect("SelectedSlimeItemTemplate", content, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(92f, 78f));
        var bg = item.gameObject.AddComponent<Image>();
        bg.sprite = inventory.slotsprite;
        bg.color = new Color(1f, 1f, 1f, 0.45f);
        bg.raycastTarget = false;

        var body = Image("PreviewBody", item, null, Vector2.zero, new Vector2(56f, 56f), new Color(1f, 1f, 1f, 0.92f));
        body.rectTransform.localScale = Vector3.one * 1.3f;
        Image("PreviewArmor", item, null, Vector2.zero, new Vector2(56f, 56f), Color.white);
        Image("PreviewWeapon", item, null, Vector2.zero, new Vector2(56f, 56f), Color.white);
        var remove = Rect("RemoveButton", item, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(24f, 24f));
        var removeImage = remove.gameObject.AddComponent<Image>();
        removeImage.color = new Color(0.72f, 0.07f, 0.05f, 0.95f);
        var removeButton = remove.gameObject.AddComponent<Button>();
        removeButton.targetGraphic = removeImage;
        Text("XLabel", remove, "X", Vector2.zero, new Vector2(24f, 24f), 14, TextAlignmentOptions.Center, Color.white);

        template = item.gameObject;
        template.SetActive(false);
        return scroll;
    }

    private static Slider CreateSlider(Transform parent, Vector2 pos, Vector2 size)
    {
        var root = Rect("Slider", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 0f;
        slider.direction = Slider.Direction.LeftToRight;

        var background = Rect("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        var bgImage = background.gameObject.AddComponent<Image>();
        bgImage.color = new Color(0.18f, 0.09f, 0.22f, 0.9f);
        bgImage.raycastTarget = false;

        var fillArea = Rect("Fill Area", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fillArea.offsetMin = new Vector2(4f, 4f);
        fillArea.offsetMax = new Vector2(-4f, -4f);

        var fill = Rect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(1f, 0.72f, 0.16f, 1f);
        fillImage.raycastTarget = false;

        slider.fillRect = fill;
        slider.targetGraphic = bgImage;
        return slider;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
            var found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Sprite FindCoinSprite()
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/coin.png");
    }
}
#endif
