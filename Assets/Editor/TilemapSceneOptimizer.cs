using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TilemapSceneOptimizer
{
    private const string Map4Path = "Assets/Scenes/Map4_T.unity";

    [MenuItem("Tools/Map/Optimize Map4 Tilemaps")]
    public static void OptimizeMap4Tilemaps()
    {
        Scene scene = EditorSceneManager.OpenScene(Map4Path, OpenSceneMode.Single);
        OptimizeOpenSceneTilemaps(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[TilemapSceneOptimizer] Optimized Map4 tilemaps.");
    }

    [MenuItem("Tools/Map/Optimize Open Scene Tilemaps")]
    public static void OptimizeOpenSceneTilemapsMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        OptimizeOpenSceneTilemaps(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[TilemapSceneOptimizer] Optimized open scene tilemaps.");
    }

    private static void OptimizeOpenSceneTilemaps(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                tilemap.ClearAllEditorPreviewTiles();
                tilemap.CompressBounds();
                EditorUtility.SetDirty(tilemap);
            }

            foreach (TilemapRenderer renderer in root.GetComponentsInChildren<TilemapRenderer>(true))
                EditorUtility.SetDirty(renderer);
        }
    }
}
