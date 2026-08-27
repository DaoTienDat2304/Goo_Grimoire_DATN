#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SlimeEggUIBuilder
{
    private const string ScenePath = "Assets/Scenes/firstsave.unity";
    private const string OldScenePath = "Assets/Scenes/menu.unity";
    private const string EggPrefabPath = "Assets/Resources/SlimeEgg.prefab";
    private static readonly Color Ink = new Color32(43, 31, 57, 255);
    private static readonly Color Cream = new Color32(255, 245, 218, 255);
    private static readonly Color Gold = new Color32(242, 181, 62, 255);
    private static readonly Color Purple = new Color32(104, 68, 139, 255);

    [MenuItem("Tools/Goo Grimoire/Build Egg UI In FirstSave")]
    public static void Build()
    {
        BuildWorldEggPrefab();
        RemoveOldMenuUI();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = FindMainCanvas();
        if (canvas == null) throw new System.Exception("Canvas not found in menu scene.");
        ConfigureWorldBackgroundSorting(canvas);
        SlimeEggUI[] oldEggUIs = Object.FindObjectsByType<SlimeEggUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SlimeEggUI oldEggUI in oldEggUIs)
            if (oldEggUI != null) Object.DestroyImmediate(oldEggUI.gameObject);

        SlimeEggSystem[] oldSystems = Object.FindObjectsByType<SlimeEggSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SlimeEggSystem oldSystem in oldSystems)
            if (oldSystem != null) Object.DestroyImmediate(oldSystem.gameObject);
        SlimeEggSystem eggSystem = new GameObject("SlimeEggSystem").AddComponent<SlimeEggSystem>();
        eggSystem.worldEggPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EggPrefabPath);
        if (eggSystem.worldEggPrefab == null) throw new System.Exception("SlimeEgg prefab could not be loaded.");

        GameObject root = UI("EggUI", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        SlimeEggUI ui = root.AddComponent<SlimeEggUI>();

        ui.eggHudButton = Button("EggHUDButton", root.transform, "", new Vector2(1,1), new Vector2(1,1), new Vector2(-85,-85), new Vector2(118,118), Gold);
        Oval("EggIcon", ui.eggHudButton.transform, new Vector2(42,56), Cream);
        GameObject badge = UI("EggCountBadge", ui.eggHudButton.transform, new Vector2(1,1), new Vector2(1,1), new Vector2(2,-2), new Vector2(42,42));
        badge.AddComponent<Image>().color = new Color32(210,68,74,255);
        ui.eggCountText = Text("CountText", badge.transform, "0", 24, Color.white, TextAlignmentOptions.Center);
        Rect(ui.eggCountText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        ui.eggInventoryPanel = Modal("EggInventoryPanel", root.transform, new Vector2(760,460));
        Text("Title", ui.eggInventoryPanel.transform, "EGG NEST", 38, Ink, TextAlignmentOptions.Center, new Vector2(0,175), new Vector2(600,55));
        Text("Subtitle", ui.eggInventoryPanel.transform, "Maximum 3 unhatched eggs", 20, Purple, TextAlignmentOptions.Center, new Vector2(0,132), new Vector2(600,35));
        GameObject scroll = UI("EggScrollView", ui.eggInventoryPanel.transform, Vector2.one*.5f, Vector2.one*.5f, new Vector2(0,-15), new Vector2(650,240));
        scroll.AddComponent<Image>().color = new Color32(225,207,177,120);
        ui.eggScrollRect = scroll.AddComponent<ScrollRect>();
        ui.eggScrollRect.horizontal = true; ui.eggScrollRect.vertical = false; ui.eggScrollRect.movementType = ScrollRect.MovementType.Clamped;
        GameObject viewport = UI("Viewport", scroll.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.AddComponent<RectMask2D>();
        GameObject content = UI("Content", viewport.transform, new Vector2(0,.5f), new Vector2(0,.5f), Vector2.zero, Vector2.zero);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0,0); contentRect.anchorMax = new Vector2(0,1); contentRect.pivot = new Vector2(0,.5f);
        HorizontalLayoutGroup layout = content.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18,18,5,5); layout.spacing = 18; layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false; layout.childControlHeight = false; layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>(); fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ui.eggScrollRect.viewport = viewport.GetComponent<RectTransform>(); ui.eggScrollRect.content = contentRect; ui.eggContent = contentRect;
        ui.eggSlotPrefab = Button("EggItemTemplate", content.transform, "", new Vector2(0,.5f), new Vector2(0,.5f), Vector2.zero, new Vector2(190,220), new Color32(238,222,190,255));
        Oval("EggVisual", ui.eggSlotPrefab.transform, new Vector2(82,108), new Color32(188,150,216,255));
        Text("Status", ui.eggSlotPrefab.transform, "", 19, Ink, TextAlignmentOptions.Center, new Vector2(0,-78), new Vector2(175,60));
        ui.eggSlotPrefab.gameObject.SetActive(false);
        ui.eggSlotButtons = new Button[0]; ui.eggSlotTexts = new TMP_Text[0];
        Button closeInventory = Button("CloseButton", ui.eggInventoryPanel.transform, "CLOSE", Vector2.one*.5f, Vector2.one*.5f, new Vector2(0,-190), new Vector2(170,52), Purple);
        UnityEventTools.AddPersistentListener(closeInventory.onClick, ui.CloseInventory);

        ui.incubationConfirmPanel = Modal("IncubationConfirmPanel", root.transform, new Vector2(500,330));
        Text("Title", ui.incubationConfirmPanel.transform, "INCUBATOR", 34, Ink, TextAlignmentOptions.Center, new Vector2(0,105), new Vector2(420,48));
        ui.incubationInfoText = Text("Info", ui.incubationConfirmPanel.transform, "", 23, Ink, TextAlignmentOptions.Center, new Vector2(0,32), new Vector2(420,90));
        ui.incubateButton = Button("IncubateButton", ui.incubationConfirmPanel.transform, "START INCUBATING", Vector2.one*.5f, Vector2.one*.5f, new Vector2(0,-65), new Vector2(280,58), Gold);
        ui.finishWithGemsButton = Button("FinishWithGemsButton", ui.incubationConfirmPanel.transform, "", Vector2.one*.5f, Vector2.one*.5f, new Vector2(0,-65), new Vector2(310,58), Gold);
        ui.gemCostText = ui.finishWithGemsButton.GetComponentInChildren<TMP_Text>();

        ui.hatchResultPanel = Modal("HatchResultPanel", root.transform, new Vector2(780,570));
        ui.hatchAnimationRoot = UI("HatchAnimationRoot", ui.hatchResultPanel.transform, Vector2.one*.5f, Vector2.one*.5f, new Vector2(-220,30), new Vector2(280,350));
        Oval("SlimePreviewPlaceholder", ui.hatchAnimationRoot.transform, new Vector2(210,165), new Color32(172,113,205,255));
        Text("AnimationNote", ui.hatchAnimationRoot.transform, "HATCH ANIMATION\nPLACEHOLDER", 18, Cream, TextAlignmentOptions.Center, Vector2.zero, new Vector2(220,80));
        ui.hatchTitleText = Text("SlimeTitle", ui.hatchResultPanel.transform, "NEW SLIME", 30, Ink, TextAlignmentOptions.Center, new Vector2(165,190), new Vector2(370,90));
        ui.hatchStatsText = Text("Stats", ui.hatchResultPanel.transform, "", 21, Ink, TextAlignmentOptions.Left, new Vector2(170,-20), new Vector2(380,330));
        Button closeHatch = Button("CollectButton", ui.hatchResultPanel.transform, "COLLECT", Vector2.one*.5f, Vector2.one*.5f, new Vector2(170,-235), new Vector2(220,58), Gold);
        UnityEventTools.AddPersistentListener(closeHatch.onClick, ui.CloseHatchResult);

        EnsureExitButton(ui.eggInventoryPanel, ui.CloseInventory);
        EnsureExitButton(ui.incubationConfirmPanel, ui.CloseIncubation);
        EnsureExitButton(ui.hatchResultPanel, ui.CloseHatchResult);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Canvas FindMainCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
            if (canvas.isRootCanvas && (canvas.transform.Find("BackGround") != null || canvas.transform.Find("BuildingSlotArea") != null))
                return canvas;
        foreach (Canvas canvas in canvases)
            if (canvas.isRootCanvas) return canvas;
        return null;
    }

    private static void BuildWorldEggPrefab()
    {
        GameObject egg = new GameObject("SlimeEgg");
        SpriteRenderer renderer = egg.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 5;
        CapsuleCollider2D collider = egg.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Vertical;
        collider.size = new Vector2(.9f, 1.15f);
        egg.AddComponent<WorldEggPickup>();
        PrefabUtility.SaveAsPrefabAsset(egg, EggPrefabPath);
        Object.DestroyImmediate(egg);
    }

    private static void ConfigureWorldBackgroundSorting(Canvas rootCanvas)
    {
        ConfigureNestedCanvas(rootCanvas.transform.Find("BackGround"), rootCanvas, -100, false);
        ConfigureNestedCanvas(rootCanvas.transform.Find("BuildingSlotArea"), rootCanvas, -90, true);
    }

    private static void ConfigureNestedCanvas(Transform target, Canvas rootCanvas, int sortingOrder, bool needsRaycaster)
    {
        if (target == null) return;
        Canvas nested = target.GetComponent<Canvas>();
        if (nested == null) nested = target.gameObject.AddComponent<Canvas>();
        nested.overrideSorting = true;
        nested.sortingLayerID = rootCanvas.sortingLayerID;
        nested.sortingOrder = sortingOrder;
        Graphic rootGraphic = target.GetComponent<Graphic>();
        if (rootGraphic != null) rootGraphic.raycastTarget = false;
        if (needsRaycaster && target.GetComponent<GraphicRaycaster>() == null)
            target.gameObject.AddComponent<GraphicRaycaster>();
        EditorUtility.SetDirty(nested);
    }

    [MenuItem("Tools/Goo Grimoire/Egg UI/Add Missing Exit Buttons")]
    public static void AddExitButtons()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SlimeEggUI ui = Object.FindFirstObjectByType<SlimeEggUI>(FindObjectsInactive.Include);
        if (ui == null) throw new System.Exception("SlimeEggUI not found in firstsave scene.");

        EnsureExitButton(ui.eggInventoryPanel, ui.CloseInventory);
        EnsureExitButton(ui.incubationConfirmPanel, ui.CloseIncubation);
        EnsureExitButton(ui.hatchResultPanel, ui.CloseHatchResult);

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureExitButton(GameObject panel, UnityEngine.Events.UnityAction closeAction)
    {
        if (panel == null) return;
        Transform existing = panel.transform.Find("ExitButton");
        if (existing != null) return;

        Button exit = Button(
            "ExitButton",
            panel.transform,
            "X",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-32f, -32f),
            new Vector2(52f, 52f),
            new Color32(190, 67, 78, 255));
        exit.navigation = new Navigation { mode = Navigation.Mode.None };
        UnityEventTools.AddPersistentListener(exit.onClick, closeAction);
        EditorUtility.SetDirty(exit);
    }

    private static void RemoveOldMenuUI()
    {
        Scene oldScene = EditorSceneManager.OpenScene(OldScenePath, OpenSceneMode.Single);
        bool changed = false;
        foreach (GameObject root in oldScene.GetRootGameObjects())
        {
            if (root.name == "SlimeEggSystem")
            {
                Object.DestroyImmediate(root);
                changed = true;
                continue;
            }

            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            Transform eggUI = canvas != null ? canvas.transform.Find("EggUI") : null;
            if (eggUI != null)
            {
                Object.DestroyImmediate(eggUI.gameObject);
                changed = true;
            }
        }
        if (changed) EditorSceneManager.SaveScene(oldScene);
    }

    private static GameObject Modal(string name, Transform parent, Vector2 size)
    {
        GameObject panel = UI(name, parent, Vector2.one*.5f, Vector2.one*.5f, Vector2.zero, size);
        Image image = panel.AddComponent<Image>(); image.color = Cream;
        Outline outline = panel.AddComponent<Outline>(); outline.effectColor = Ink; outline.effectDistance = new Vector2(5,-5);
        panel.SetActive(false); return panel;
    }
    private static GameObject UI(string name, Transform parent, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
    { GameObject go=new GameObject(name,typeof(RectTransform)); go.layer=5; go.transform.SetParent(parent,false); Rect(go.GetComponent<RectTransform>(),amin,amax,pos,size); return go; }
    private static void Rect(RectTransform r, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
    { r.anchorMin=amin;r.anchorMax=amax;r.pivot=(amin==amax?new Vector2(.5f,.5f):new Vector2(.5f,.5f));r.anchoredPosition=pos;r.sizeDelta=size; }
    private static TMP_Text Text(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions align, Vector2 pos=default, Vector2 box=default)
    { GameObject go=UI(name,parent,Vector2.one*.5f,Vector2.one*.5f,pos,box==default?new Vector2(200,50):box); var t=go.AddComponent<TextMeshProUGUI>();t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.enableWordWrapping=true;return t; }
    private static Button Button(string name, Transform parent, string label, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size, Color color)
    { GameObject go=UI(name,parent,amin,amax,pos,size);var image=go.AddComponent<Image>();image.color=color;var b=go.AddComponent<Button>();b.targetGraphic=image;var t=Text("Label",go.transform,label,21,Ink,TextAlignmentOptions.Center);Rect(t.rectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);return b; }
    private static void Oval(string name, Transform parent, Vector2 size, Color color)
    { GameObject go=UI(name,parent,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,size);var image=go.AddComponent<Image>();image.color=color;image.raycastTarget=false;go.transform.localScale=new Vector3(.78f,1,1); }
}
#endif
