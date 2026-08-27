#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SkillTagUpdater : EditorWindow
{
    [MenuItem("Tools/Skills/2. Auto-Tag Rarity & Type")]
    public static void AutoTagSkills()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkillSO", new[] { "Assets/SkillDB" });
        int count = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillSO so = AssetDatabase.LoadAssetAtPath<SkillSO>(path);

            if (path.Contains("WeaponSkill") || so.name.StartsWith("W_")) so.targetTrait = TraitType.Weapon;
            else if (path.Contains("HatSkill") || so.name.StartsWith("H_")) so.targetTrait = TraitType.Armor;
            else if (path.Contains("BodySkill") || so.name.StartsWith("B_")) so.targetTrait = TraitType.Body;

            if (so.name.Contains("_Com_")) so.rarity = Rarity.Common;
            else if (so.name.Contains("_Unc_")) so.rarity = Rarity.Uncommon;
            else if (so.name.Contains("_Rar_")) so.rarity = Rarity.Rare;
            else if (so.name.Contains("_SR_")) so.rarity = Rarity.SuperRare;
            else if (so.name.Contains("_UR_")) so.rarity = Rarity.UltraRare;
            else if (so.name.Contains("_Leg_")) so.rarity = Rarity.Legendary;
            else if (so.name.Contains("_Myt_")) so.rarity = Rarity.Mythic;
            else if (so.name.Contains("_Sec_")) so.rarity = Rarity.Secret;

            EditorUtility.SetDirty(so);
            count++;
        }

        AssetDatabase.SaveAssets();
    }
}
#endif
