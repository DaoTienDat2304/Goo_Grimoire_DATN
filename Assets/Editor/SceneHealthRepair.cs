using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneHealthRepair
{
    private static readonly string[] AdventureScenePaths =
    {
        "Assets/Scenes/Map1_IceMap.unity",
        "Assets/Scenes/Map2_Fantasymap.unity",
        "Assets/Scenes/Map3_DungeonMap.unity",
    };

    [MenuItem("Tools/Adventure/Repair Scene Health")]
    public static void RepairSceneHealthMenu()
    {
        RepairSceneHealth();
    }

    public static void RepairSceneHealth()
    {
        int removedMissingPrefabs = 0;

        foreach (string scenePath in AdventureScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            removedMissingPrefabs += RemoveMissingPrefabInstances(scene);
            EditorSceneManager.SaveScene(scene);
        }

        EnableSceneInBuildSettings("Assets/Scenes/travelSence.unity");
        Debug.Log($"Scene health repair finished. Removed {removedMissingPrefabs} missing prefab instance(s).");
    }

    private static int RemoveMissingPrefabInstances(Scene scene)
    {
        var missingPrefabRoots = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .Where(gameObject => PrefabUtility.GetPrefabInstanceStatus(gameObject) == PrefabInstanceStatus.MissingAsset)
            .Where(gameObject => PrefabUtility.GetNearestPrefabInstanceRoot(gameObject) == gameObject)
            .ToList();

        foreach (GameObject missingPrefabRoot in missingPrefabRoots)
        {
            Object.DestroyImmediate(missingPrefabRoot);
        }

        return missingPrefabRoots.Count;
    }

    private static void EnableSceneInBuildSettings(string scenePath)
    {
        EditorBuildSettings.scenes = EditorBuildSettings.scenes
            .Select(scene => scene.path == scenePath
                ? new EditorBuildSettingsScene(scene.path, true)
                : scene)
            .ToArray();
    }
}
