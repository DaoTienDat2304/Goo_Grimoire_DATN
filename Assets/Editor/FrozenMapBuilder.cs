using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Một-click tool để biến Frozen_Map thành bản sao đầy đủ tính năng của adventureSence.
///
/// Cách hoạt động (an toàn, không động vào adventureSence):
///  1. Mở Frozen_Map làm scene gốc  -> giữ nguyên grid băng giá bạn đã vẽ + giữ GUID scene.
///  2. Mở adventureSence ở chế độ Additive để "mượn" các hệ thống.
///  3. Xoá mọi root thừa trong Frozen_Map (camera placeholder), chỉ GIỮ lại grid băng giá.
///  4. DI CHUYỂN (không clone) toàn bộ root hệ thống của adventureSence sang Frozen_Map,
///     TRỪ grid của adventure. Vì là move chứ không clone nên MỌI reference nội bộ
///     (Player &lt;-&gt; Cinemachine follow, Canvas, manager, SlimeSpawner...) được giữ nguyên 100%.
///  5. Đóng adventureSence KHÔNG lưu -> file adventureSence.unity tuyệt đối không đổi.
///  6. Lưu Frozen_Map.
///
/// Chạy lại nhiều lần đều an toàn (idempotent): mỗi lần build đều dọn sạch hệ thống cũ
/// trong Frozen_Map (mọi thứ không phải grid băng) trước khi import lại.
/// </summary>
public static class FrozenMapBuilder
{
    const string AdventureScenePath = "Assets/Scenes/adventureSence.unity";
    const string FrozenScenePath = "Assets/Scenes/Frozen_Map.unity";

    // Root duy nhất của Frozen_Map cần GIỮ (grid băng giá đã vẽ + các tilemap layer là con của nó).
    static readonly HashSet<string> FrozenRootsToKeep = new HashSet<string> { "Grid" };

    // Root của adventureSence KHÔNG mang sang (frozen đã có grid riêng).
    static readonly HashSet<string> AdventureRootsToSkip = new HashSet<string> { "Grid" };

    [MenuItem("Tools/Goo Grimoire/Build Frozen Map")]
    public static void BuildFrozenMap()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Frozen Map",
                "Tool sẽ copy TOÀN BỘ hệ thống (Player, Canvas, managers, camera Cinemachine, SlimeSpawner, " +
                "WildSlimeManager...) từ adventureSence sang Frozen_Map, GIỮ NGUYÊN grid băng giá bạn đã vẽ.\n\n" +
                "• adventureSence sẽ KHÔNG bị thay đổi.\n" +
                "• Chạy lại nhiều lần vẫn an toàn.\n\n" +
                "Tiếp tục?",
                "Build", "Huỷ"))
        {
            return;
        }

        // Đảm bảo người dùng đã lưu scene đang mở (nếu có thay đổi chưa lưu).
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[FrozenMapBuilder] Đã huỷ: còn scene chưa lưu.");
            return;
        }

        // 1. Mở Frozen_Map làm scene gốc.
        Scene frozenScene = EditorSceneManager.OpenScene(FrozenScenePath, OpenSceneMode.Single);
        if (!frozenScene.IsValid())
        {
            EditorUtility.DisplayDialog("Build Frozen Map", "Không mở được Frozen_Map.unity.", "OK");
            return;
        }

        // 3. Dọn sạch mọi root trong Frozen_Map trừ grid băng (giúp build lại nhiều lần không bị nhân đôi).
        int removed = 0;
        foreach (GameObject root in frozenScene.GetRootGameObjects())
        {
            if (FrozenRootsToKeep.Contains(root.name.Trim()))
                continue;
            Object.DestroyImmediate(root);
            removed++;
        }

        // 2. Mở adventureSence ở chế độ Additive để mượn hệ thống.
        Scene advScene = EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Additive);
        if (!advScene.IsValid())
        {
            EditorUtility.DisplayDialog("Build Frozen Map", "Không mở được adventureSence.unity.", "OK");
            return;
        }

        // 4. Di chuyển mọi root hệ thống của adventure sang Frozen_Map (trừ grid của adventure).
        //    Move (không clone) -> giữ nguyên toàn bộ reference nội bộ giữa các object.
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

        // 5. Đóng adventureSence KHÔNG lưu -> adventureSence.unity giữ nguyên.
        EditorSceneManager.CloseScene(advScene, true);

        // 6. Đặt Frozen_Map làm active scene rồi lưu.
        SceneManager.SetActiveScene(frozenScene);
        EditorSceneManager.MarkSceneDirty(frozenScene);
        bool saved = EditorSceneManager.SaveScene(frozenScene);

        string summary =
            $"[FrozenMapBuilder] Xoá {removed} root thừa khỏi Frozen_Map, " +
            $"đưa {moved} nhóm hệ thống từ adventure sang: {string.Join(", ", movedNames)}. " +
            $"Saved={saved}.";
        Debug.Log(summary);

        EditorUtility.DisplayDialog(
            "Build Frozen Map",
            (saved
                ? $"Xong! Đã đưa {moved} nhóm hệ thống vào Frozen_Map."
                : "Đã build nhưng LƯU THẤT BẠI — xem Console.") +
            "\n\nViệc cần làm thủ công sau đó:\n" +
            "1. Mở Frozen_Map, kiểm tra vị trí spawn của Player nằm trên map băng (kéo Player nếu cần).\n" +
            "2. Kiểm tra vùng SlimeSpawner (Spawn Area) phủ đúng khu vực map băng.\n" +
            "3. Nhấn Play để xác nhận: di chuyển, joystick, anim, slime, đếm marshmallow đều chạy.\n" +
            "4. Thêm nút chọn Frozen_Map trong firstsave (MapSelection.MapIndex = 5).",
            "OK");
    }

    /// <summary>
    /// Tiện ích kiểm tra nhanh: liệt kê các root hiện có trong Frozen_Map (xem đã import hệ thống chưa).
    /// </summary>
    [MenuItem("Tools/Goo Grimoire/Log Frozen Map Roots")]
    public static void LogFrozenMapRoots()
    {
        Scene frozenScene = EditorSceneManager.OpenScene(FrozenScenePath, OpenSceneMode.Single);
        var names = frozenScene.GetRootGameObjects().Select(go => go.name);
        Debug.Log($"[FrozenMapBuilder] Frozen_Map roots: {string.Join(", ", names)}");
    }
}
