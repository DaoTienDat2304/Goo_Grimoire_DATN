#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Công cụ editor để tự động tạo dữ liệu 15 floor đầu tiên cho TowerSlimeBosses.
/// Dùng: chuột phải vào TowerSlimeBosses asset → "Tạo 15 Floors Mặc Định".
/// Traits phải gán thủ công sau khi chạy lệnh này.
/// </summary>
public static class TowerFloorSetup
{
    [MenuItem("Tools/Tower/Tạo 15 Floors Mặc Định")]
    public static void CreateDefaultFloors()
    {
        // Tìm TowerSlimeBosses asset
        var guids = AssetDatabase.FindAssets("t:TowerSlimeBosses");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy TowerSlimeBosses asset nào.\nHãy tạo asset trước.", "OK");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var db = AssetDatabase.LoadAssetAtPath<TowerSlimeBosses>(path);
        if (db == null)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không load được asset tại: {path}", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Xác nhận",
            $"Sẽ THAY THẾ toàn bộ floors hiện có trong '{db.name}' bằng 15 floors mặc định.\nTraits sẽ cần gán thủ công sau.\nTiếp tục?",
            "Tạo", "Hủy");

        if (!confirm) return;

        Undo.RecordObject(db, "Tạo 15 Tower Floors Mặc Định");

        db.floors = BuildFloors();
        db.currentFloor = 0;
        db.highestFloorReached = 0;

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Hoàn thành",
            $"Đã tạo {db.floors.Count} floors cho '{db.name}'.\nNhớ gán Body/Armor/Weapon TraitSO cho từng floor trong Inspector.",
            "OK");

        Debug.Log($"[TowerFloorSetup] Đã tạo {db.floors.Count} floors tại {path}");
    }

    // ── Floor data ─────────────────────────────────────────────────────
    // Stat tăng dần: mỗi 5 floor tăng ~50% HP, ~30% ATK
    // Reward: coins tăng theo floor; gems chỉ cho floor 5/10/15 (milestone)
    private static List<TowerSlimeBosses.TowerFloor> BuildFloors()
    {
        var floors = new List<TowerSlimeBosses.TowerFloor>();

        // (floor, name,        HP,  ATK, DEF, SPD, EVD, coins, gems)
        var data = new (int f, string n, int hp, int atk, int def, int spd, int evd, int coins, int gems)[]
        {
            (1,  "Gooey Meadow",       80,   20,  10,  12,   5,   50,  0),
            (2,  "Misty Forest",       110,   28,  14,  14,   6,   65,  0),
            (3,  "Toxic Mushroom Cave",145,   36,  18,  16,   7,   80,  0),
            (4,  "Bubble Swamp",       185,   45,  22,  18,   8,  100,  0),
            (5,  "Smoldering Crater",  230,   55,  28,  20,  10,  130,  2),  // milestone
            (6,  "Frozen Spire",       280,   66,  35,  22,  11,  160,  0),
            (7,  "Ancient Canopy",     335,   78,  42,  24,  13,  190,  0),
            (8,  "Cloud Sea",          395,   91,  50,  26,  15,  225,  0),
            (9,  "Crystal Labyrinth",  460,  105,  58,  28,  17,  260,  0),
            (10, "Golden Citadel",     535,  120,  68,  31,  19,  310,  3),  // milestone
            (11, "Eternal Abyss",      615,  137,  78,  34,  21,  360,  0),
            (12, "Thunder Peak",       705,  155,  90,  37,  24,  415,  0),
            (13, "Galactic Shore",     805,  175, 103,  40,  27,  475,  0),
            (14, "Void Gate",          915,  197, 117,  43,  30,  540,  0),
            (15, "Grimoire Throne",   1040,  220, 133,  47,  33,  620,  5),  // milestone
        };

        foreach (var d in data)
        {
            floors.Add(new TowerSlimeBosses.TowerFloor
            {
                floorNumber  = d.f,
                floorName    = d.n,
                baseHP       = d.hp,
                baseAttack   = d.atk,
                baseDefense  = d.def,
                baseSpeed    = d.spd,
                baseEvade    = d.evd,
                rewardCoins  = d.coins,
                rewardGems   = d.gems,
                completed    = false,
                claimed      = false,
            });
        }

        return floors;
    }
}
#endif
