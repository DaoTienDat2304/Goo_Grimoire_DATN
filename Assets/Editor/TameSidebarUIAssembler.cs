#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TameSidebarUIAssembler
{
    private const string ScenePath = "Assets/Scenes/Map1_IceMap.unity";
    private const string SpriteRoot = "Assets/Sprite/NewAsset/UI_sidebar/";
    private const string FontPath = "Assets/TextMesh Pro/Fonts/1.asset";

    [MenuItem("Tools/Goo Grimoire/Wire Existing Tame Sidebar UI")]
    public static void AssembleMap1()
    {
        Scene previous = SceneManager.GetActiveScene();
        string previousPath = previous.path;

        Scene scene = previousPath == ScenePath
            ? previous
            : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogWarning("[TameSidebarUI] Canvas not found in Map1_IceMap.");
            return;
        }

        GameObject tame = FindSceneObject("Tame");
        GameObject tameInventory = FindSceneObject("TameInventory");
        if (tame == null || tameInventory == null)
        {
            Debug.LogWarning("[TameSidebarUI] Missing Tame or TameInventory.");
            return;
        }

        showteam teamUi = tame.GetComponentInChildren<showteam>(true);
        if (teamUi == null) teamUi = tame.AddComponent<showteam>();
        if (teamUi.animator == null)
            teamUi.animator = tame.GetComponent<Animator>();

        CollectExistingTeamSlots(tame.transform, teamUi);
        EnsureAdventureBagRefs(tameInventory);
        EnsureSidebarFlow(tame, tameInventory);
        EnsureToggleButtons(canvas.transform, tame, tameInventory);

        tame.SetActive(true);
        tameInventory.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Goo Grimoire/Rebuild Tame Sidebar UI From UI_sidebar")]
    public static void RebuildMap1()
    {
        Scene previous = SceneManager.GetActiveScene();
        string previousPath = previous.path;

        Scene scene = previousPath == ScenePath
            ? previous
            : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogWarning("[TameSidebarUI] Canvas not found in Map1_IceMap.");
            return;
        }

        GameObject tame = FindSceneObject("Tame");
        GameObject tameInventory = FindSceneObject("TameInventory");
        if (tame == null || tameInventory == null)
        {
            Debug.LogWarning("[TameSidebarUI] Missing Tame or TameInventory.");
            return;
        }

        RectTransform sidebarRoot = EnsureSidebarRoot(canvas.transform);
        PrepareRoot(tame, sidebarRoot, new Vector2(-230f, -10f), new Vector2(170f, 320f));
        PrepareRoot(tameInventory, sidebarRoot, new Vector2(205f, -5f), new Vector2(245f, 320f));

        showteam teamUi = tame.GetComponentInChildren<showteam>(true);
        if (teamUi == null) teamUi = tame.AddComponent<showteam>();
        Animator tameAnimator = tame.GetComponent<Animator>();
        if (tameAnimator == null) tameAnimator = tame.AddComponent<Animator>();
        teamUi.animator = tameAnimator;

        BuildTeamPanel(tame.transform, teamUi);
        BuildInventoryPanel(tameInventory.transform);
        BuildTamingPanel(sidebarRoot);
        EnsureAdventureBagRefs(tameInventory);
        EnsureSidebarFlow(tame, tameInventory);
        EnsureToggleButtons(sidebarRoot, tame, tameInventory);

        tame.SetActive(true);
        tameInventory.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildTeamPanel(Transform parent, showteam teamUi)
    {
        ClearGenerated(parent, "TameSidebarTeamRoot");

        RectTransform root = Rect("TameSidebarTeamRoot", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image rootImage = Image(root.gameObject, Sprite("02_Image_6.png"), true);
        rootImage.color = new Color(1f, 1f, 1f, 0.98f);

        RectTransform title = Rect("TeamTitle", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(130f, 42f));
        Image(title.gameObject, Sprite("15_Image_41.png"), true);
        Text("TEAM", title, new Vector2(0f, 0f), new Vector2(100f, 24f), 16, TextAlignmentOptions.Center, Color.white);

        var members = new List<ShowTeamSlime>();
        for (int i = 0; i < 3; i++)
        {
            RectTransform slot = Rect("TeamSlot_" + (i + 1), root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f - i * 85f), new Vector2(110f, 72f));
            Image(slot.gameObject, Sprite("20_Image_51.png"), true);
            ShowTeamSlime member = slot.gameObject.AddComponent<ShowTeamSlime>();
            member.teamSlimes = teamUi.teamSlimes;

            RectTransform body = Rect("BodyImage", slot, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-18f, -12f));
            RectTransform armor = Rect("ArmorImage", body, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform weapon = Rect("WeaponImage", body, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image(body.gameObject, null, true);
            Image(armor.gameObject, null, true);
            Image(weapon.gameObject, null, true);
            member.body = body.gameObject;
            member.armor = armor.gameObject;
            member.weapon = weapon.gameObject;
            members.Add(member);
        }

        teamUi.teamMembers = members;
        EditorUtility.SetDirty(teamUi);
    }

    private static void BuildInventoryPanel(Transform parent)
    {
        ClearGenerated(parent, "TameSidebarInventoryRoot");

        RectTransform root = Rect("TameSidebarInventoryRoot", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image rootImage = Image(root.gameObject, Sprite("05_Image_16.png"), true);
        rootImage.color = new Color(1f, 1f, 1f, 0.98f);

        RectTransform title = Rect("InventoryTitle", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(150f, 42f));
        Image(title.gameObject, Sprite("15_Image_41.png"), true);
        Text("MY SLIMES", title, Vector2.zero, new Vector2(125f, 24f), 14, TextAlignmentOptions.Center, Color.white);
        Text("12 / 30", root, new Vector2(0f, 110f), new Vector2(100f, 22f), 12, TextAlignmentOptions.Center, new Color32(77, 36, 35, 255));

        RectTransform content = Rect("Content", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        content.SetAsLastSibling();

        Vector2 start = new Vector2(-68f, -78f);
        Vector2 gap = new Vector2(68f, 62f);
        Sprite[] slimeSprites =
        {
            Sprite("17_Image_45.png"), Sprite("18_Image_46.png"), Sprite("36_Image_74.png"),
            Sprite("34_Image_71.png"), Sprite("35_Image_72.png")
        };

        for (int i = 0; i < 12; i++)
        {
            int row = i / 3;
            int col = i % 3;
            RectTransform slot = Rect("TameInventorySlot_" + (i + 1), content, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), start + new Vector2(col * gap.x, -row * gap.y), new Vector2(52f, 52f));
            Image(slot.gameObject, Sprite("21_Image_52.png"), true);

            RectTransform icon = Rect("SlimeIcon", slot, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8f, -8f));
            Image iconImage = Image(icon.gameObject, i < slimeSprites.Length ? slimeSprites[i] : Sprite("23_Image_54.png"), true);
            iconImage.color = i < slimeSprites.Length ? Color.white : Color.black;

            if (i >= 5)
            {
                Text("?", icon, Vector2.zero, new Vector2(40f, 40f), 22, TextAlignmentOptions.Center, Color.black);
                RectTransform lockRt = Rect("Lock", slot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(14f, 14f));
                Image(lockRt.gameObject, Sprite("29_Image_60.png"), true);
            }
        }

        RectTransform arrow = Rect("InventorySideArrow", root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(16f, 0f), new Vector2(28f, 60f));
        Image(arrow.gameObject, Sprite("03_Image_13.png"), true);
    }

    private static void BuildTamingPanel(Transform canvas)
    {
        GameObject panel = FindSceneObject("TamingPanel");
        if (panel == null) return;
        PrepareRoot(panel, canvas, new Vector2(0f, -140f), new Vector2(210f, 44f));
        ClearGenerated(panel.transform, "TameSidebarTamingRoot");

        RectTransform root = Rect("TameSidebarTamingRoot", panel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image(root.gameObject, Sprite("14_Image_40.png"), true);
        Text("3 / 30", root, new Vector2(18f, 0f), new Vector2(88f, 24f), 15, TextAlignmentOptions.Center, new Color32(80, 38, 35, 255));
    }

    private static RectTransform EnsureSidebarRoot(Transform canvas)
    {
        GameObject existing = FindSceneObject("TameSidebarRoot");
        GameObject go = existing != null ? existing : new GameObject("TameSidebarRoot", typeof(RectTransform));
        go.layer = 5;
        if (go.transform.parent != canvas)
            go.transform.SetParent(canvas, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(650f, 360f);
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();

        Transform oldBackground = rt.Find("TameSidebarBackground");
        if (oldBackground != null)
            Object.DestroyImmediate(oldBackground.gameObject);

        RectTransform bg = Rect("TameSidebarBackground", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.SetAsFirstSibling();
        Image(bg.gameObject, Sprite("khung.png"), true);
        return rt;
    }

    private static void EnsureAdventureBagRefs(GameObject tameInventory)
    {
        AdventureBag bag = tameInventory.GetComponent<AdventureBag>();
        if (bag == null) bag = tameInventory.AddComponent<AdventureBag>();
        Transform generatedRoot = tameInventory.transform.Find("TameSidebarInventoryRoot");
        Transform content = generatedRoot != null ? generatedRoot.Find("Content") : null;
        if (content != null)
            bag.collectionGridParent = content;
        bag.slimeCollectionPanel = tameInventory;
        if (bag.animator == null)
            bag.animator = tameInventory.GetComponent<Animator>();
        EditorUtility.SetDirty(bag);
    }

    private static void EnsureSidebarFlow(GameObject tame, GameObject tameInventory)
    {
        showteam teamUi = tame.GetComponent<showteam>();
        if (teamUi == null) teamUi = tame.AddComponent<showteam>();

        AdventureBag bag = tameInventory.GetComponent<AdventureBag>();
        if (bag == null) bag = tameInventory.AddComponent<AdventureBag>();

        EditorUtility.SetDirty(teamUi);
        EditorUtility.SetDirty(bag);
    }

    private static void EnsureToggleButtons(Transform canvas, GameObject tame, GameObject tameInventory)
    {
        Button teamButton = FindExistingButton("TameButton", "ButtonTame", "ButtonTeam", "TeamButton");
        if (teamButton == null)
            teamButton = EnsureButton(canvas, "TameButton", new Vector2(-345f, 0f), new Vector2(30f, 64f), Sprite("03_Image_13.png"));

        Button inventoryButton = EnsureButton(canvas, "ButtonTameInventory", new Vector2(345f, 0f), new Vector2(30f, 64f), Sprite("03_Image_13.png"));
        KeepButtonAlwaysOpenInEditor(teamButton, canvas);
        KeepButtonAlwaysOpenInEditor(inventoryButton, canvas);

        showteam teamUi = tame.GetComponent<showteam>();
        AdventureBag bag = tameInventory.GetComponent<AdventureBag>();

        WireButton(teamButton, teamUi, nameof(showteam.OpenCloseTeam));
        WireButton(inventoryButton, bag, nameof(AdventureBag.click));
    }

    private static Button FindExistingButton(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject obj = FindSceneObject(name);
            if (obj == null)
                continue;

            Button button = obj.GetComponent<Button>();
            if (button != null)
                return button;
        }

        return null;
    }

    private static Button EnsureButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Sprite sprite)
    {
        GameObject existing = FindSceneObject(name);
        bool created = existing == null;
        GameObject go = existing != null ? existing : new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        if (created && go.transform.parent != parent)
            go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        if (created)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        Image image = go.GetComponent<Image>();
        if (image == null)
            image = Image(go, sprite, true);
        else if (created)
            image.sprite = sprite;
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        if (button == null) button = go.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static void KeepButtonAlwaysOpenInEditor(Button button, Transform canvas)
    {
        if (button == null || canvas == null)
            return;

        button.gameObject.SetActive(true);
        button.interactable = true;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group == null)
            group = button.gameObject.AddComponent<CanvasGroup>();

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        group.ignoreParentGroups = true;
        EditorUtility.SetDirty(button.gameObject);
    }

    private static void CollectExistingTeamSlots(Transform tame, showteam teamUi)
    {
        var slots = new List<ShowTeamSlime>(tame.GetComponentsInChildren<ShowTeamSlime>(true));
        if (slots.Count == 0)
            return;

        teamUi.teamMembers = slots;
        EditorUtility.SetDirty(teamUi);
    }

    private static void WireButton(Button button, Object target, string methodName)
    {
        if (button == null || target == null)
            return;

        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (button.onClick.GetPersistentMethodName(i) == methodName)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        UnityAction action = System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName) as UnityAction;
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void PrepareRoot(GameObject go, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        go.layer = 5;
        if (go.transform.parent != parent)
            go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        return rt;
    }

    private static Image Image(GameObject go, Sprite sprite, bool preserveAspect)
    {
        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(string value, Transform parent, Vector2 pos, Vector2 size, int fontSize, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rt = Rect("Text", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        TMP_Text text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ?? TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = 8;
        text.fontSizeMax = fontSize;
        return text;
    }

    private static Sprite Sprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + fileName);
    }

    private static void ClearGenerated(Transform parent, string generatedRootName)
    {
        Transform existing = parent.Find(generatedRootName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }

    private static GameObject FindSceneObject(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root;
            Transform found = FindChild(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
            Transform found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
