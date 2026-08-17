using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneOpenProbe
{
    private const string ScenePathEnvironmentVariable = "UNITY_PROBE_SCENE_PATH";

    [MenuItem("Tools/Diagnostics/Open Firstsave")]
    public static void OpenFirstsave()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/firstsave.unity", OpenSceneMode.Single);
        Debug.Log("[SceneOpenProbe] firstsave opened.");
    }

    public static void OpenSceneFromEnvironment()
    {
        string scenePath = System.Environment.GetEnvironmentVariable(ScenePathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.LogError($"[SceneOpenProbe] Missing {ScenePathEnvironmentVariable}.");
            EditorApplication.Exit(2);
            return;
        }

        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log($"[SceneOpenProbe] {scenePath} opened.");
    }
}
