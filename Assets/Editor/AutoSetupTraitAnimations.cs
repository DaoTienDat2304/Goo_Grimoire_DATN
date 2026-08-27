using UnityEngine;
using UnityEditor;
using Spine.Unity;

[InitializeOnLoad]
public class AutoSetupTraitAnimations
{
    static AutoSetupTraitAnimations()
    {
        EditorApplication.delayCall += CheckAndSetupAnimations;
    }
    
    private static void CheckAndSetupAnimations()
    {
        if (EditorPrefs.GetBool("TraitAnimationsSetup", false))
            return;
            
        string[] guids = AssetDatabase.FindAssets("Slimeanimation t:Prefab");
        if (guids.Length == 0)
        {
            Debug.LogWarning("AutoSetupTraitAnimations: Could not find Slimeanimation prefab");
            return;
        }
        
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        
        if (prefab == null)
        {
            Debug.LogWarning("AutoSetupTraitAnimations: Could not load Slimeanimation prefab");
            return;
        }
        
        var skeletonAnimation = prefab.GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null || skeletonAnimation.skeletonDataAsset == null)
        {
            Debug.LogWarning("AutoSetupTraitAnimations: Slimeanimation prefab doesn't have valid SkeletonAnimation");
            return;
        }
        
        SetupBodyTraitsWithAnimation(skeletonAnimation.skeletonDataAsset);
        
        EditorPrefs.SetBool("TraitAnimationsSetup", true);
        
    }
    
    private static void SetupBodyTraitsWithAnimation(SkeletonDataAsset animationAsset)
    {
        string[] guids = AssetDatabase.FindAssets("t:TraitSO", new[] { "Assets/TraitDatabase" });
        int updatedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TraitSO trait = AssetDatabase.LoadAssetAtPath<TraitSO>(path);
            
            if (trait != null && trait.type == TraitType.Body && trait.animationAsset == null)
            {
                trait.animationAsset = animationAsset;
                trait.animationName = "animation";
                
                EditorUtility.SetDirty(trait);
                updatedCount++;
            }
        }
        
        if (updatedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
    
    [MenuItem("Tools/Reset Trait Animation Setup Flag")]
    public static void ResetSetupFlag()
    {
        EditorPrefs.DeleteKey("TraitAnimationsSetup");
    }
}


