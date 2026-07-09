using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LandscapeUIEditorFixer
{
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    [MenuItem("Goo Grimoire/UI/Bake Landscape Canvas Settings In Open Scenes")]
    private static void BakeOpenScenes()
    {
        int changed = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
                changed += ConfigureCanvasesIn(root);

            if (changed > 0)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log($"Landscape UI bake finished for open scenes. Updated {changed} CanvasScaler component(s).");
    }

    [MenuItem("Goo Grimoire/UI/Bake Landscape Canvas Settings In Project Scenes And Prefabs")]
    private static void BakeProjectScenesAndPrefabs()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        int changed = 0;
        string activeScenePath = SceneManager.GetActiveScene().path;

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int sceneChanges = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
                sceneChanges += ConfigureCanvasesIn(root);

            if (sceneChanges > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                changed += sceneChanges;
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UI", "Assets/Prefab", "Assets/Archievement", "Assets/New Folder" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int prefabChanges = ConfigureCanvasesIn(root);

            if (prefabChanges > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed += prefabChanges;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(activeScenePath))
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

        Debug.Log($"Landscape UI bake finished for project scenes and prefabs. Updated {changed} CanvasScaler component(s).");
    }

    private static int ConfigureCanvasesIn(GameObject root)
    {
        int changed = 0;
        var canvases = root.GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;

            if (ConfigureCanvas(canvas))
                changed++;
        }

        return changed;
    }

    private static bool ConfigureCanvas(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        else
            Undo.RecordObject(scaler, "Bake Landscape Canvas Settings");

        bool changed =
            scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
            scaler.referenceResolution != ReferenceResolution ||
            scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight ||
            !Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f) ||
            !Mathf.Approximately(scaler.referencePixelsPerUnit, 100f);

        if (!changed)
            return false;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        EditorUtility.SetDirty(scaler);
        return true;
    }
}
