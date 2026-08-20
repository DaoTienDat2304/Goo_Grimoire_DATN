#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools/Goo Grimoire/Build Breeding Cost + Gem UI
/// </summary>
public static class BreedingCostGemUIBuilder
{
    private const string ScenePath = "Assets/Scenes/firstsave.unity";
    private const string CoinSpritePath = "Assets/Sprite/coin.png";

    private static readonly Color Ink = new Color32(43, 31, 57, 255);
    private static readonly Color Purple = new Color32(140, 82, 230, 245);
    private static readonly Color GoldPlaceholder = new Color(1f, 0.85f, 0.2f);
    private static readonly Color GemPlaceholder = new Color(0.62f, 0.85f, 1f);

    [MenuItem("Tools/Goo Grimoire/Build Breeding Cost + Gem UI")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var manager = Object.FindFirstObjectByType<BreedingUIManager>(FindObjectsInactive.Include);
        if (manager == null) throw new System.Exception("BreedingUIManager not found in firstsave.");

        var currencyUI = Object.FindFirstObjectByType<CurrencyUI>(FindObjectsInactive.Include);
        Sprite coinSprite = currencyUI != null ? currencyUI.CoinSprite : null;
        Sprite gemSprite = currencyUI != null ? currencyUI.GemSprite : null;
        if (coinSprite == null) coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSpritePath);

        Transform canvas = manager.GetComponentInParent<Canvas>() != null
            ? manager.GetComponentInParent<Canvas>().transform : null;
        Transform costParent = manager.breedingPanel != null ? manager.breedingPanel.transform : canvas;
        Transform gemParent = manager.breedingProgressPanel != null ? manager.breedingProgressPanel.transform : canvas;
        if (costParent == null || gemParent == null)
            throw new System.Exception("Breeding UI roots missing.");

        DestroyChild(costParent, "BreedCostGroup");
        DestroyChild(gemParent, "FinishWithGemsButton");

        GameObject group = Node("BreedCostGroup", costParent, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(240f, 64f));
        group.AddComponent<LayoutElement>().ignoreLayout = true;
        var hl = group.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 8f;
        hl.childControlWidth = false; hl.childControlHeight = false;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        Image coinIcon = Icon("CoinIcon", group.transform, coinSprite, new Vector2(52f, 52f), GoldPlaceholder);
        Text costText = Label("BreedCostText", group.transform, "0", 32, Ink, new Vector2(150f, 56f), TextAnchor.MiddleLeft);

        GameObject btnGO = Node("FinishWithGemsButton", gemParent, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(250f, 66f));
        btnGO.AddComponent<LayoutElement>().ignoreLayout = true;
        Image btnImg = btnGO.AddComponent<Image>(); btnImg.color = Purple;
        Button btn = btnGO.AddComponent<Button>(); btn.targetGraphic = btnImg;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.layer = 5;
        content.transform.SetParent(btnGO.transform, false);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
        var chl = content.AddComponent<HorizontalLayoutGroup>();
        chl.childAlignment = TextAnchor.MiddleCenter; chl.spacing = 6f;
        chl.childControlWidth = false; chl.childControlHeight = false;
        chl.childForceExpandWidth = false; chl.childForceExpandHeight = false;
        Image gemIcon = Icon("GemIcon", content.transform, gemSprite, new Vector2(40f, 40f), GemPlaceholder);
        Text gemText = Label("GemText", content.transform, "0", 26, Color.white, new Vector2(80f, 46f), TextAnchor.MiddleCenter);

        manager.costCoinIcon = coinIcon;
        manager.breedingCostText = costText;
        manager.finishWithGemsButton = btn;
        manager.gemIcon = gemIcon;
        manager.gemCostText = gemText;
        EditorUtility.SetDirty(manager);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BreedingUI] Built Cost + Gem UI in " + ScenePath +
                  ". Select 'BreedCostGroup' (overflowoi breedingPanel) va 'FinishWithGemsButton' (overflowoi breedingProgressPanel) trong Hierarchy to adjust position/size.");
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    private static GameObject Node(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private static Image Icon(string name, Transform parent, Sprite sprite, Vector2 size, Color fallback)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = sprite != null ? Color.white : fallback;
        img.preserveAspect = true;
        img.raycastTarget = false;
        go.GetComponent<RectTransform>().sizeDelta = size;
        return img;
    }

    private static Text Label(string name, Transform parent, string value, int size, Color color, Vector2 box, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = value; t.fontSize = size; t.color = color; t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        go.GetComponent<RectTransform>().sizeDelta = box;
        return t;
    }
}
#endif
