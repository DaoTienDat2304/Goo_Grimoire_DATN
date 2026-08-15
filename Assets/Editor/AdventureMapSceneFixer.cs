using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Spine.Unity;
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
        Spawner spawner = FindInScene<Spawner>(scene);
        tamingManager manager = FindInScene<tamingManager>(scene);
        AdventureBag bag = FindInScene<AdventureBag>(scene);

        EnsureMashmallowCount(scene);
        EnsureAdventureBag(scene, bag, wildSlimes);
        EnsureTamingManager(manager, spawner, playerMovement, wildSlimes, slimeSpawner);
        EnsureAiming(scene);
        EnsureSpawner(scene, manager);
        EnsureMovingNotes(scene, manager);
    }

    private static void EnsureMashmallowCount(Scene scene)
    {
        GameObject counter = FindObjectByName(scene, "Mashmallow count");
        if (counter == null) return;

        MashmaloowDisplay display = counter.GetComponent<MashmaloowDisplay>();
        if (display == null) display = counter.AddComponent<MashmaloowDisplay>();
        if (display.count == null)
            display.count = counter.GetComponentInChildren<Text>(true);

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

    private static void EnsureAiming(Scene scene)
    {
        Aiming aiming = FindInScene<Aiming>(scene);
        if (aiming == null) return;

        SerializedObject so = new(aiming);
        SetObjectIfNull(so, "lineRenderer", aiming.GetComponentInChildren<LineRenderer>(true));
        SetObjectIfNull(so, "startPosition", FindChildByName(aiming.transform, "StartPosition"));
        SetObjectIfNull(so, "idlePosition", FindChildByName(aiming.transform, "IdlePosition"));
        SetObjectIfNull(so, "aimingarea", FindInScene<aimingArea>(scene));
        SetObjectIfNull(so, "tamingUI", FindObjectByName(scene, "TamingPanel"));
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(aiming);
    }

    private static void EnsureSpawner(Scene scene, tamingManager manager)
    {
        Spawner spawner = FindInScene<Spawner>(scene);
        if (spawner == null || manager == null) return;

        SerializedObject so = new(spawner);
        SetObjectIfNull(so, "TamingManager", manager);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawner);
    }

    private static void EnsureMovingNotes(Scene scene, tamingManager manager)
    {
        if (manager == null) return;
        foreach (MovingNote note in FindAllInScene<MovingNote>(scene))
        {
            SerializedObject so = new(note);
            SetObjectIfNull(so, "TamingManager", manager);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(note);
        }
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
