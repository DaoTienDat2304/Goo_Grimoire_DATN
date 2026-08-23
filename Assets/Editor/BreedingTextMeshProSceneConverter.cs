#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BreedingTextMeshProSceneConverter
{
    private const string ScenePath = "Assets/Scenes/firstsave.unity";
    private const string FontPath = "Assets/TextMesh Pro/Fonts/1.asset";

    [MenuItem("Tools/Goo Grimoire/Convert BreedingUI Text To TMP")]
    public static void ConvertActiveBreedingUi()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded || scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform breedingUi = FindByName("BreedingUI");
        if (breedingUi == null)
        {
            Debug.LogWarning("[BreedingUI TMP] BreedingUI not found.");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Text[] legacyTexts = breedingUi.GetComponentsInChildren<Text>(true);
        int converted = 0;
        foreach (Text legacy in legacyTexts)
        {
            if (legacy == null) continue;
            ConvertText(legacy, font);
            converted++;
        }

        RewireBreedingManager(breedingUi);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BreedingUI TMP] Converted {converted} legacy Text components under BreedingUI to TextMeshProUGUI.");
    }

    private static void ConvertText(Text legacy, TMP_FontAsset font)
    {
        GameObject go = legacy.gameObject;
        string text = legacy.text;
        int fontSize = legacy.fontSize;
        FontStyle fontStyle = legacy.fontStyle;
        TextAnchor alignment = legacy.alignment;
        Color color = legacy.color;
        bool raycastTarget = legacy.raycastTarget;
        bool richText = legacy.supportRichText;
        bool bestFit = legacy.resizeTextForBestFit;
        int minSize = legacy.resizeTextMinSize;
        int maxSize = legacy.resizeTextMaxSize;
        HorizontalWrapMode horizontalOverflow = legacy.horizontalOverflow;
        VerticalWrapMode verticalOverflow = legacy.verticalOverflow;

        Object.DestroyImmediate(legacy, true);

        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = ToTmpFontStyle(fontStyle);
        tmp.alignment = ToTmpAlignment(alignment);
        tmp.color = color;
        tmp.raycastTarget = raycastTarget;
        tmp.richText = richText;
        tmp.enableAutoSizing = bestFit;
        tmp.fontSizeMin = Mathf.Max(1, minSize);
        tmp.fontSizeMax = Mathf.Max(fontSize, maxSize);
        tmp.textWrappingMode = horizontalOverflow == HorizontalWrapMode.Wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.overflowMode = verticalOverflow == VerticalWrapMode.Overflow ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
        EditorUtility.SetDirty(go);
    }

    private static void RewireBreedingManager(Transform breedingUi)
    {
        BreedingUIManager manager = Object.FindFirstObjectByType<BreedingUIManager>(FindObjectsInactive.Include);
        if (manager == null) return;

        manager.breedingStatusText = FindTmp(breedingUi, "BreedingStatusText");
        manager.selectedSlimesText = FindTmp(breedingUi, "SelectedSlimesText");
        manager.breedingPreviewText = FindTmp(breedingUi, "BreedingPreviewText");
        manager.breedingCostText = FindTmp(breedingUi, "BreedingCostText") ?? FindTmp(breedingUi, "CostText");
        manager.gemCostText = FindTmp(breedingUi, "GemCostText") ?? FindTmp(breedingUi, "GemText");
        manager.slimeCounterText = FindTmp(breedingUi, "SlimeCounterText") ?? FindTmp(breedingUi, "Soluong");
        EditorUtility.SetDirty(manager);
    }

    private static TMP_Text FindTmp(Transform root, string name)
    {
        Transform t = FindChildRecursive(root, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindByName(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root.transform;
            Transform found = FindChildRecursive(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
            Transform found = FindChildRecursive(child, name);
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
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }
}
#endif
