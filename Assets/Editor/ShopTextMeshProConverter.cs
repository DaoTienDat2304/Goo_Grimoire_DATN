#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ShopTextMeshProConverter
{
    private static readonly string[] PrefabPaths =
    {
        "Assets/Prefab/ShopUI.prefab",
        "Assets/Prefab/Shop Item.prefab"
    };

    [MenuItem("Tools/Goo Grimoire/Convert Shop Text To TMP")]
    public static void ConvertShopTextToTmp()
    {
        foreach (string path in PrefabPaths)
            ConvertPrefab(path);

        foreach (ShopItemsSpawner shop in Object.FindObjectsByType<ShopItemsSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ConvertRoot(shop.gameObject);

        foreach (ShopItemUI item in Object.FindObjectsByType<ShopItemUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            WireShopItem(item);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    private static void ConvertPrefab(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) return;

        ConvertRoot(root);
        foreach (ShopItemUI item in root.GetComponentsInChildren<ShopItemUI>(true))
            WireShopItem(item);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ConvertRoot(GameObject root)
    {
        if (root == null) return;

        Text[] legacyTexts = root.GetComponentsInChildren<Text>(true);
        foreach (Text legacy in legacyTexts)
            ConvertText(legacy);

        foreach (ShopItemUI item in root.GetComponentsInChildren<ShopItemUI>(true))
            WireShopItem(item);
    }

    private static TMP_Text ConvertText(Text legacy)
    {
        if (legacy == null) return null;
        GameObject go = legacy.gameObject;
        TMP_Text existing = go.GetComponent<TMP_Text>();
        if (existing != null)
        {
            Object.DestroyImmediate(legacy, true);
            return existing;
        }

        string value = legacy.text;
        int fontSize = legacy.fontSize;
        Color color = legacy.color;
        TextAnchor alignment = legacy.alignment;
        FontStyle style = legacy.fontStyle;
        bool raycastTarget = legacy.raycastTarget;
        bool autoSize = legacy.resizeTextForBestFit;
        int minSize = Mathf.Max(1, legacy.resizeTextMinSize);
        int maxSize = Mathf.Max(fontSize, legacy.resizeTextMaxSize);

        Object.DestroyImmediate(legacy, true);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = ToTmpAlignment(alignment);
        tmp.fontStyle = ToTmpFontStyle(style);
        tmp.raycastTarget = raycastTarget;
        tmp.enableAutoSizing = autoSize;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static void WireShopItem(ShopItemUI item)
    {
        if (item == null) return;

        item.nameTmpText = FindTmp(item.transform, "NameText") ?? FindTmp(item.transform, "Name");
        item.priceTmpText = FindTmp(item.transform, "Price");
        item.descriptionTmpText = FindTmp(item.transform, "Description");
        item.amountTmpText = FindTmp(item.transform, "Amount");
        if (item.iconImage == null)
            item.iconImage = Find(item.transform, "Icon")?.GetComponent<Image>();
        if (item.buyButton == null)
            item.buyButton = Find(item.transform, "BuyButton")?.GetComponent<Button>();

        EditorUtility.SetDirty(item);
    }

    private static TMP_Text FindTmp(Transform root, string name)
    {
        return Find(root, name)?.GetComponent<TMP_Text>();
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

    private static FontStyles ToTmpFontStyle(FontStyle style)
    {
        switch (style)
        {
            case FontStyle.Bold: return FontStyles.Bold;
            case FontStyle.Italic: return FontStyles.Italic;
            case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
            default: return FontStyles.Normal;
        }
    }

    private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
    {
        switch (alignment)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }
}
#endif
