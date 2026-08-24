using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Spine.Unity;
using TMPro;
using UnityEngine.UI;

public static class AdventureMapSceneFixer
{
    private const string SourceScenePath = "Assets/Scenes/Map4_T.unity";

    private static readonly string[] TargetScenePaths =
    {
        "Assets/Scenes/Map1_IceMap.unity",
        "Assets/Scenes/Map2_Fantasymap.unity",
        "Assets/Scenes/Map3_DungeonMap.unity",
    };

    private static readonly string[] RequiredRoots =
    {
        "Canvas",
        "Player",
        "EventSystem",
        "SlimeSpawner",
    };

    [MenuItem("Goo Grimoire/Scenes/Fix Adventure Maps From Map4")]
    public static void FixAdventureMapsFromMap4()
    {
        foreach (string targetPath in TargetScenePaths)
            FixTargetScene(targetPath);

        Debug.Log("[AdventureMapSceneFixer] Finished syncing adventure map runtime roots from Map4_T.");
    }

    private static void FixTargetScene(string targetPath)
    {
        Scene targetScene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

        Dictionary<string, GameObject> sourceRoots = GetRootMap(sourceScene);
        Dictionary<string, GameObject> targetRoots = GetRootMap(targetScene);

        foreach (string rootName in RequiredRoots)
        {
            if (targetRoots.ContainsKey(rootName) || !sourceRoots.TryGetValue(rootName, out GameObject sourceRoot))
                continue;

            GameObject clone = Object.Instantiate(sourceRoot);
            clone.name = rootName;
            SceneManager.MoveGameObjectToScene(clone, targetScene);
            targetRoots[rootName] = clone;
        }

        EnsurePlayerAnimations(sourceRoots, targetRoots);
        EnsureTameFlow(targetScene);

        EditorSceneManager.CloseScene(sourceScene, true);
        SceneManager.SetActiveScene(targetScene);
        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);
        Debug.Log("[AdventureMapSceneFixer] Fixed " + targetPath);
    }

    private static void EnsurePlayerAnimations(
        Dictionary<string, GameObject> sourceRoots,
        Dictionary<string, GameObject> targetRoots)
    {
        if (!sourceRoots.TryGetValue("Player", out GameObject sourcePlayer)) return;
        if (!targetRoots.TryGetValue("Player", out GameObject targetPlayer)) return;

        PlayerMovement targetMovement = targetPlayer.GetComponent<PlayerMovement>();
        PlayerMovement sourceMovement = sourcePlayer.GetComponent<PlayerMovement>();
        if (targetMovement == null || sourceMovement == null) return;

        CopyAnimationChildIfMissing(sourceMovement.idle, targetPlayer.transform);
        CopyAnimationChildIfMissing(sourceMovement.running, targetPlayer.transform);
        CopyAnimationChildIfMissing(sourceMovement.backIdle, targetPlayer.transform);

        targetMovement.idle = FindAnimation(targetPlayer, "IdleAnimation");
        targetMovement.running = FindAnimation(targetPlayer, "RunningAnimation");
        targetMovement.backIdle = FindAnimation(targetPlayer, "BackIdleAnimation");

        EditorUtility.SetDirty(targetPlayer);
        EditorUtility.SetDirty(targetMovement);
    }

    private static void EnsureTameFlow(Scene scene)
    {
        WildSlimes wildSlimes = FindInScene<WildSlimes>(scene);
        SlimeSpawner slimeSpawner = FindInScene<SlimeSpawner>(scene);
        PlayerMovement playerMovement = FindInScene<PlayerMovement>(scene);
        tamingManager manager = FindCanonicalTamingManager(scene);
        Spawner spawner = manager != null
            ? manager.GetComponentInChildren<Spawner>(true)
            : FindInScene<Spawner>(scene);
        AdventureBag bag = FindInScene<AdventureBag>(scene);

        EnsureMashmallowCount(scene);
        EnsureAdventureBag(scene, bag, wildSlimes);
        EnsureTamingManager(manager, spawner, playerMovement, wildSlimes, slimeSpawner);
        EnsureAiming(scene, manager);
        EnsureSpawner(spawner, manager);
        EnsureMovingNotes(scene, manager);
    }

    internal static bool RepairCopiedUiReferences(Scene scene)
    {
        bool changed = false;
        tamingManager manager = FindCanonicalTamingManager(scene);
        PlayerMovement playerMovement = FindInScene<PlayerMovement>(scene);
        SlimeSpawner slimeSpawner = FindInScene<SlimeSpawner>(scene);
        Spawner spawner = manager != null
            ? manager.GetComponentInChildren<Spawner>(true)
            : null;

        if (manager != null)
        {
            changed |= SetSerializedReference(manager, "spawner", spawner);
            changed |= SetSerializedReference(manager, "playerMovement", playerMovement);

            if (slimeSpawner != null && manager.slimeSpawner != slimeSpawner)
            {
                manager.slimeSpawner = slimeSpawner;
                EditorUtility.SetDirty(manager);
                changed = true;
            }

            changed |= SetSerializedReference(spawner, "TamingManager", manager);
            foreach (MovingNote note in manager.GetComponentsInChildren<MovingNote>(true))
                changed |= SetSerializedReference(note, "TamingManager", manager);
        }

        Aiming aiming = FindInScene<Aiming>(scene);
        if (aiming != null && manager != null)
            changed |= SetSerializedReference(aiming, "tamingUI", manager.gameObject);

        return changed;
    }

    private static void EnsureMashmallowCount(Scene scene)
    {
        GameObject counter = FindObjectByName(scene, "Mashmallow count");
        if (counter == null) return;

        MashmaloowDisplay display = counter.GetComponent<MashmaloowDisplay>();
        if (display == null) display = counter.AddComponent<MashmaloowDisplay>();
        if (display.count == null)
            display.count = counter.GetComponentInChildren<TMP_Text>(true);

        EditorUtility.SetDirty(display);
    }

    private static void EnsureAdventureBag(Scene scene, AdventureBag bag, WildSlimes wildSlimes)
    {
        if (bag == null) return;

        if (bag.slimeCollectionPanel == null)
            bag.slimeCollectionPanel = bag.gameObject;
        if (bag.animator == null)
            bag.animator = bag.GetComponent<Animator>();
        if (bag.collectionGridParent == null)
        {
            Transform content = FindChildByName(bag.transform, "Content");
            bag.collectionGridParent = content != null ? content : bag.transform;
        }
        if (bag.collectionSlotPrefab == null)
            bag.collectionSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/New Folder/k/tameslime.prefab");
        if (bag.wildSlimes == null)
            bag.wildSlimes = wildSlimes;

        Button button = FindObjectByName(scene, "ButtonTameInventory")?.GetComponent<Button>();
        if (button != null && button.onClick.GetPersistentEventCount() == 0)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, bag.click);
            EditorUtility.SetDirty(button);
        }

        EditorUtility.SetDirty(bag);
    }

    private static void EnsureTamingManager(
        tamingManager manager,
        Spawner spawner,
        PlayerMovement playerMovement,
        WildSlimes wildSlimes,
        SlimeSpawner slimeSpawner)
    {
        if (manager == null) return;

        SerializedObject so = new(manager);
        so.FindProperty("spawner").objectReferenceValue = spawner;
        so.FindProperty("playerMovement").objectReferenceValue = playerMovement;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (manager.wildSlimes == null)
            manager.wildSlimes = wildSlimes;
        if (manager.slimeSpawner == null)
            manager.slimeSpawner = slimeSpawner;
        if (manager.emote == null)
            manager.emote = manager.GetComponentInChildren<Image>(true);

        EditorUtility.SetDirty(manager);
    }

    private static void EnsureAiming(Scene scene, tamingManager manager)
    {
        Aiming aiming = FindInScene<Aiming>(scene);
        if (aiming == null) return;

        SerializedObject so = new(aiming);
        SetObjectIfNull(so, "lineRenderer", aiming.GetComponentInChildren<LineRenderer>(true));
        SetObjectIfNull(so, "startPosition", FindChildByName(aiming.transform, "StartPosition"));
        SetObjectIfNull(so, "idlePosition", FindChildByName(aiming.transform, "IdlePosition"));
        SetObjectIfNull(so, "aimingarea", FindInScene<aimingArea>(scene));
        SetObject(so, "tamingUI", manager != null ? manager.gameObject : null);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(aiming);
    }

    private static void EnsureSpawner(Spawner spawner, tamingManager manager)
    {
        if (spawner == null || manager == null) return;

        SerializedObject so = new(spawner);
        SetObject(so, "TamingManager", manager);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawner);
    }

    private static void EnsureMovingNotes(Scene scene, tamingManager manager)
    {
        if (manager == null) return;
        foreach (MovingNote note in manager.GetComponentsInChildren<MovingNote>(true))
        {
            SerializedObject so = new(note);
            SetObject(so, "TamingManager", manager);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(note);
        }
    }

    private static tamingManager FindCanonicalTamingManager(Scene scene)
    {
        GameObject sidebarRoot = FindObjectByName(scene, "TameSidebarRoot");
        if (sidebarRoot != null)
        {
            foreach (tamingManager manager in sidebarRoot.GetComponentsInChildren<tamingManager>(true))
            {
                if (manager.gameObject.name == "TamingPanel")
                    return manager;
            }
        }

        return FindInScene<tamingManager>(scene);
    }

    private static void SetObject(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static bool SetSerializedReference(Object target, string propertyName, Object value)
    {
        if (target == null || value == null)
            return false;

        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == value)
            return false;

        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        return true;
    }

    private static void SetObjectIfNull(SerializedObject so, string propertyName, Object value)
    {
        if (value == null) return;
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null && property.objectReferenceValue == null)
            property.objectReferenceValue = value;
    }

    private static void CopyAnimationChildIfMissing(SkeletonAnimation sourceAnimation, Transform targetPlayer)
    {
        if (sourceAnimation == null) return;
        string childName = sourceAnimation.gameObject.name;
        if (targetPlayer.Find(childName) != null) return;

        GameObject clone = Object.Instantiate(sourceAnimation.gameObject, targetPlayer);
        clone.name = childName;
        clone.transform.localPosition = sourceAnimation.transform.localPosition;
        clone.transform.localRotation = sourceAnimation.transform.localRotation;
        clone.transform.localScale = sourceAnimation.transform.localScale;
    }

    private static SkeletonAnimation FindAnimation(GameObject player, string childName)
    {
        Transform child = player.transform.Find(childName);
        return child != null ? child.GetComponent<SkeletonAnimation>() : null;
    }

    private static Dictionary<string, GameObject> GetRootMap(Scene scene)
    {
        Dictionary<string, GameObject> roots = new();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!roots.ContainsKey(root.name))
                roots.Add(root.name, root);
        }

        return roots;
    }

    private static T FindInScene<T>(Scene scene) where T : Object
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }

        return null;
    }

    private static IEnumerable<T> FindAllInScene<T>(Scene scene) where T : Object
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T item in root.GetComponentsInChildren<T>(true))
                yield return item;
        }
    }

    private static GameObject FindObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChildByName(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChildByName(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}

[InitializeOnLoad]
public static class AdventureMashmallowTmpConverter
{
    private const string FontPath = "Assets/TextMesh Pro/Fonts/1.asset";
    private const string AutoRunSessionKey = "GooGrimoire.AdventureMashmallowTmpConverter.v3";

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Map1_IceMap.unity",
        "Assets/Scenes/Map2_Fantasymap.unity",
        "Assets/Scenes/Map3_DungeonMap.unity"
    };

    static AdventureMashmallowTmpConverter()
    {
        EditorApplication.delayCall += AutoConvertOnce;
    }

    [MenuItem("Tools/Goo Grimoire/Convert Adventure Mashmallow Count To TMP")]
    public static void ConvertAllFromMenu()
    {
        ConvertAll(true);
    }

    private static void AutoConvertOnce()
    {
        if (SessionState.GetBool(AutoRunSessionKey, false))
            return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += AutoConvertOnce;
            return;
        }

        SessionState.SetBool(AutoRunSessionKey, true);
        ConvertAll(false);
    }

    private static void ConvertAll(bool logResult)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
            ?? TMP_Settings.defaultFontAsset;
        Scene originalActiveScene = SceneManager.GetActiveScene();
        int converted = 0;
        int repaired = 0;

        foreach (string scenePath in ScenePaths)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForConversion = !scene.isLoaded;
            if (openedForConversion)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            bool sceneChanged = ConvertScene(scene, font, out int sceneConverted);
            sceneChanged |= AdventureMapSceneFixer.RepairCopiedUiReferences(scene);
            converted += sceneConverted;
            if (sceneChanged)
            {
                repaired++;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (openedForConversion)
                EditorSceneManager.CloseScene(scene, true);
        }

        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
            SceneManager.SetActiveScene(originalActiveScene);

        AssetDatabase.SaveAssets();
        if (logResult || repaired > 0)
        {
            Debug.Log(
                $"[Mashmallow TMP] Updated {repaired} scene(s), converted {converted} legacy Text component(s), " +
                "and preserved all RectTransforms.");
        }
    }

    private static bool ConvertScene(Scene scene, TMP_FontAsset font, out int converted)
    {
        Transform counter = FindSceneTransform(scene, "Mashmallow count");
        converted = 0;
        if (counter == null)
            return false;

        bool changed = false;
        foreach (Text legacy in counter.GetComponentsInChildren<Text>(true))
        {
            ConvertText(legacy, font);
            converted++;
            changed = true;
        }

        TMP_Text tmp = counter.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null && tmp.font != font)
        {
            tmp.font = font;
            EditorUtility.SetDirty(tmp);
            changed = true;
        }

        MashmaloowDisplay display = counter.GetComponent<MashmaloowDisplay>();
        if (display == null)
        {
            display = counter.gameObject.AddComponent<MashmaloowDisplay>();
            changed = true;
        }

        if (display.count != tmp)
        {
            display.count = tmp;
            EditorUtility.SetDirty(display);
            changed = true;
        }

        return changed;
    }

    private static void ConvertText(Text legacy, TMP_FontAsset font)
    {
        GameObject target = legacy.gameObject;
        string value = legacy.text;
        int fontSize = legacy.fontSize;
        FontStyle fontStyle = legacy.fontStyle;
        TextAnchor alignment = legacy.alignment;
        Color color = legacy.color;
        bool raycastTarget = legacy.raycastTarget;
        bool richText = legacy.supportRichText;
        bool bestFit = legacy.resizeTextForBestFit;
        int minSize = legacy.resizeTextMinSize;
        int maxSize = legacy.resizeTextMaxSize;
        float lineSpacing = legacy.lineSpacing;
        HorizontalWrapMode horizontalOverflow = legacy.horizontalOverflow;
        VerticalWrapMode verticalOverflow = legacy.verticalOverflow;

        Object.DestroyImmediate(legacy, true);
        TextMeshProUGUI tmp = target.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = target.AddComponent<TextMeshProUGUI>();

        tmp.font = font;
        tmp.text = value;
        tmp.fontSize = fontSize;
        tmp.fontStyle = ToTmpFontStyle(fontStyle);
        tmp.alignment = ToTmpAlignment(alignment);
        tmp.color = color;
        tmp.raycastTarget = raycastTarget;
        tmp.richText = richText;
        tmp.enableAutoSizing = bestFit;
        tmp.fontSizeMin = Mathf.Max(1, minSize);
        tmp.fontSizeMax = Mathf.Max(fontSize, maxSize);
        tmp.lineSpacing = lineSpacing;
        tmp.textWrappingMode = horizontalOverflow == HorizontalWrapMode.Wrap
            ? TextWrappingModes.Normal
            : TextWrappingModes.NoWrap;
        tmp.overflowMode = verticalOverflow == VerticalWrapMode.Overflow
            ? TextOverflowModes.Overflow
            : TextOverflowModes.Truncate;
        EditorUtility.SetDirty(target);
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChild(root.transform, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChild(child, objectName);
            if (found != null)
                return found;
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
