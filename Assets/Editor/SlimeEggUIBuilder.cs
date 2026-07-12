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
    private static readonly Color Ink = new Color32(43, 31, 57, 255);
    private static readonly Color Cream = new Color32(255, 245, 218, 255);
    private static readonly Color Gold = new Color32(242, 181, 62, 255);
    private static readonly Color Purple = new Color32(104, 68, 139, 255);

    [MenuItem("Tools/Goo Grimoire/Build Egg UI In FirstSave")]
    public static void Build()
    {
        RemoveOldMenuUI();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) throw new System.Exception("Canvas not found in menu scene.");
        Transform old = canvas.transform.Find("EggUI");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject oldSystem = GameObject.Find("SlimeEggSystem");
        if (oldSystem != null) Object.DestroyImmediate(oldSystem);
        new GameObject("SlimeEggSystem").AddComponent<SlimeEggSystem>();

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
        ui.eggSlotButtons = new Button[3]; ui.eggSlotTexts = new TMP_Text[3];
        for (int i=0;i<3;i++)
        {
            float x = -230 + i * 230;
            ui.eggSlotButtons[i] = Button($"EggSlot_{i+1}", ui.eggInventoryPanel.transform, "", Vector2.one*.5f, Vector2.one*.5f, new Vector2(x,-15), new Vector2(190,230), new Color32(238,222,190,255));
            Oval("EggVisual", ui.eggSlotButtons[i].transform, new Vector2(82,108), new Color32(188,150,216,255));
            ui.eggSlotTexts[i] = Text("Status", ui.eggSlotButtons[i].transform, "EMPTY", 19, Ink, TextAlignmentOptions.Center, new Vector2(0,-78), new Vector2(175,60));
        }
        Button closeInventory = Button("CloseButton", ui.eggInventoryPanel.transform, "CLOSE", Vector2.one*.5f, Vector2.one*.5f, new Vector2(0,-190), new Vector2(170,52), Purple);
        UnityEventTools.AddPersistentListener(closeInventory.onClick, ui.CloseInventory);

        ui.incubationConfirmPanel = Modal("IncubationConfirmPanel", root.transform, new Vector2(500,330));
        Text("Title", ui.incubationConfirmPanel.transform, "INCUBATOR", 34, Ink, TextAlignmentOptions.Center, new Vector2(0,105), new Vector2(420,48));
        ui.incubationInfoText = Text("Info", ui.incubationConfirmPanel.transform, "", 23, Ink, TextAlignmentOptions.Center, new Vector2(0,32), new Vector2(420,90));
        ui.incubateButton = Button("IncubateButton", ui.incubationConfirmPanel.transform, "START INCUBATING", Vector2.one*.5f, Vector2.one*.5f, new Vector2(0,-65), new Vector2(280,58), Gold);
        ui.finishWithGemsButton = Button("FinishWithGemsButton", ui.incubationConfirmPanel.transform, "", Vector2.one*.5f, Vector2.one*.5f, new Vector2(0,-65), new Vector2(310,58), Gold);
        ui.gemCostText = ui.finishWithGemsButton.GetComponentInChildren<TMP_Text>();
        Button closeIncubation = Button("CloseButton", ui.incubationConfirmPanel.transform, "X", new Vector2(1,1), new Vector2(1,1), new Vector2(-28,-28), new Vector2(42,42), Purple);
        UnityEventTools.AddPersistentListener(closeIncubation.onClick, ui.CloseIncubation);

        ui.hatchResultPanel = Modal("HatchResultPanel", root.transform, new Vector2(780,570));
        ui.hatchAnimationRoot = UI("HatchAnimationRoot", ui.hatchResultPanel.transform, Vector2.one*.5f, Vector2.one*.5f, new Vector2(-220,30), new Vector2(280,350));
        Oval("SlimePreviewPlaceholder", ui.hatchAnimationRoot.transform, new Vector2(210,165), new Color32(172,113,205,255));
        Text("AnimationNote", ui.hatchAnimationRoot.transform, "HATCH ANIMATION\nPLACEHOLDER", 18, Cream, TextAlignmentOptions.Center, Vector2.zero, new Vector2(220,80));
        ui.hatchTitleText = Text("SlimeTitle", ui.hatchResultPanel.transform, "NEW SLIME", 30, Ink, TextAlignmentOptions.Center, new Vector2(165,190), new Vector2(370,90));
        ui.hatchStatsText = Text("Stats", ui.hatchResultPanel.transform, "", 21, Ink, TextAlignmentOptions.Left, new Vector2(170,-20), new Vector2(380,330));
        Button closeHatch = Button("CollectButton", ui.hatchResultPanel.transform, "COLLECT", Vector2.one*.5f, Vector2.one*.5f, new Vector2(170,-235), new Vector2(220,58), Gold);
        UnityEventTools.AddPersistentListener(closeHatch.onClick, ui.CloseHatchResult);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[EggUI] Built and saved UI hierarchy in " + ScenePath);
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
        Debug.Log("[EggUI] Added missing ExitButton objects without rebuilding existing UI.");
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
