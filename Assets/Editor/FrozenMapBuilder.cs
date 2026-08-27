using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class FrozenMapBuilder
{
    const string AdventureScenePath = "Assets/Scenes/adventureSence.unity";
    const string FrozenScenePath = "Assets/Scenes/Frozen_Map.unity";

    static readonly HashSet<string> FrozenRootsToKeep = new HashSet<string> { "Grid" };

    static readonly HashSet<string> AdventureRootsToSkip = new HashSet<string> { "Grid" };

    [MenuItem("Tools/Goo Grimoire/Build Frozen Map")]
    public static void BuildFrozenMap()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Frozen Map",
                "Tool se copy TOAN BO he thong (Player, Canvas, managers, camera Cinemachine, SlimeSpawner, " +
                "WildSlimeManager...) tu adventureSence sang Frozen_Map, GIU NGUYEN grid bang gia ban da ve.\n\n" +
                "• adventureSence se KHONG bi thay doi.\n" +
                "• Chay lai nhieu lan van an toan.\n\n" +
                "Tiep tuc?",
                "Build", "Huy"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene frozenScene = EditorSceneManager.OpenScene(FrozenScenePath, OpenSceneMode.Single);
        if (!frozenScene.IsValid())
        {
            EditorUtility.DisplayDialog("Build Frozen Map", "Khong mo duoc Frozen_Map.unity.", "OK");
            return;
        }

        int removed = 0;
        foreach (GameObject root in frozenScene.GetRootGameObjects())
        {
            if (FrozenRootsToKeep.Contains(root.name.Trim()))
                continue;
            Object.DestroyImmediate(root);
            removed++;
        }

        Scene advScene = EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Additive);
        if (!advScene.IsValid())
        {
            EditorUtility.DisplayDialog("Build Frozen Map", "Khong mo duoc adventureSence.unity.", "OK");
            return;
        }

        int moved = 0;
        var movedNames = new List<string>();
        foreach (GameObject root in advScene.GetRootGameObjects())
        {
            if (AdventureRootsToSkip.Contains(root.name.Trim()))
                continue;

            SceneManager.MoveGameObjectToScene(root, frozenScene);
            moved++;
            movedNames.Add(root.name);
        }

        EditorSceneManager.CloseScene(advScene, true);

        SceneManager.SetActiveScene(frozenScene);
        EditorSceneManager.MarkSceneDirty(frozenScene);
        bool saved = EditorSceneManager.SaveScene(frozenScene);

        string summary =
            $"[FrozenMapBuilder] Xoa {removed} root thua khoi Frozen_Map, " +
            $"dua {moved} nhom he thong tu adventure sang: {string.Join(", ", movedNames)}. " +
            $"Saved={saved}.";

        EditorUtility.DisplayDialog(
            "Build Frozen Map",
            (saved
                ? $"Xong! Da dua {moved} nhom he thong vao Frozen_Map."
                : "Da build nhung LUU THAT BAI — xem Console.") +
            "\n\nNext manual steps:\n" +
            "1. Mo Frozen_Map, kiem tra vi tri spawn cua Player nam tren map bang (keo Player if can).\n" +
            "2. Kiem tra vung SlimeSpawner (Spawn Area) phu dung khu vuc map bang.\n" +
            "3. Nhan Play de xac taken: di chuyen, joystick, anim, slime, dem marshmallow deu chay.\n" +
            "4. Add Frozen_Map button in firstsave (MapSelection.MapIndex = 5).",
            "OK");
    }
    [MenuItem("Tools/Goo Grimoire/Log Frozen Map Roots")]
    public static void LogFrozenMapRoots()
    {
        Scene frozenScene = EditorSceneManager.OpenScene(FrozenScenePath, OpenSceneMode.Single);
        var names = frozenScene.GetRootGameObjects().Select(go => go.name);
    }
}
