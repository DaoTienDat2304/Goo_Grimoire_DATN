#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
public static class TowerFloorSetup
{
    [MenuItem("Tools/Tower/Create 15 Default Floors")]
    public static void CreateDefaultFloors()
    {
        var guids = AssetDatabase.FindAssets("t:TowerSlimeBosses");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "TowerSlimeBosses asset not found.\nCreate one first.", "OK");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var db = AssetDatabase.LoadAssetAtPath<TowerSlimeBosses>(path);
        if (db == null)
        {
            EditorUtility.DisplayDialog("Error", $"Cannot load asset at: {path}", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Confirm",
            $"This will replace all floors in '{db.name}' with 15 default floors.\nAssign traits manually.\nContinue?",
            "Create", "Cancel");

        if (!confirm) return;

        Undo.RecordObject(db, "Create 15 Tower Floors Default");

        db.floors = BuildFloors();
        db.currentFloor = 0;
        db.highestFloorReached = 0;

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done",
            $"Created {db.floors.Count} floors for '{db.name}'.\nAssign Body/Armor/Weapon TraitSO per floor.",
            "OK");

    }

    // ── Floor data ─────────────────────────────────────────────────────
    private static List<TowerSlimeBosses.TowerFloor> BuildFloors()
    {
        var floors = new List<TowerSlimeBosses.TowerFloor>();

        // (floor, name,        HP,  ATK, MATK, DEF, SPD, CR, CD, coins, gems)
        var data = new (int f, string n, int hp, int atk, int matk, int def, int spd, float cr, float cd, int coins, int gems)[]
        {
            (1,  "Gooey Meadow",       80,   20,   40,  10,  12,  0.05f, 1.30f,  50,  0),
            (2,  "Misty Forest",       110,   28,   56,  14,  14,  0.06f, 1.35f,  65,  0),
            (3,  "Toxic Mushroom Cave",145,   36,   72,  18,  16,  0.08f, 1.45f,  80,  0),
            (4,  "Bubble Swamp",       185,   45,   90,  22,  18,  0.10f, 1.55f, 100,  0),
            (5,  "Smoldering Crater",  230,   55,  110,  28,  20,  0.13f, 1.70f, 130,  2),  // milestone
            (6,  "Frozen Spire",       280,   66,  132,  35,  22,  0.16f, 1.90f, 160,  0),
            (7,  "Ancient Canopy",     335,   78,  156,  42,  24,  0.20f, 2.20f, 190,  0),
            (8,  "Cloud Sea",          395,   91,  182,  50,  26,  0.22f, 2.30f, 225,  0),
            (9,  "Crystal Labyrinth",  460,  105,  210,  58,  28,  0.25f, 2.40f, 260,  0),
            (10, "Golden Citadel",     535,  120,  240,  68,  31,  0.28f, 2.50f, 310,  3),  // milestone
            (11, "Eternal Abyss",      615,  137,  274,  78,  34,  0.30f, 2.50f, 360,  0),
            (12, "Thunder Peak",       705,  155,  310,  90,  37,  0.32f, 2.50f, 415,  0),
            (13, "Galactic Shore",     805,  175,  350, 103,  40,  0.35f, 2.50f, 475,  0),
            (14, "Void Gate",          915,  197,  394, 117,  43,  0.38f, 2.50f, 540,  0),
            (15, "Grimoire Throne",   1040,  220,  440, 133,  47,  0.40f, 2.50f, 620,  5),  // milestone
        };

        foreach (var d in data)
        {
            floors.Add(new TowerSlimeBosses.TowerFloor
            {
                floorNumber  = d.f,
                floorName    = d.n,
                baseHP       = d.hp,
                baseAttack   = d.atk,
                baseMagicAttack = d.matk,
                baseDefense  = d.def,
                baseSpeed    = d.spd,
                baseCritRate = d.cr,
                baseCritDMG  = d.cd,
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
