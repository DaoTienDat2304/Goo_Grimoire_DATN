using UnityEngine;
using UnityEditor;
using System.IO;
using Spine.Unity;

public class TraitAnimationSetup : EditorWindow
{
    [MenuItem("Tools/Setup Trait Animations")]
    public static void ShowWindow()
    {
        GetWindow<TraitAnimationSetup>("Trait Animation Setup");
    }

    private SkeletonDataAsset slimeAnimationAsset;

    private void OnGUI()
    {
        GUILayout.Label("Setup Animation for Body Traits", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        slimeAnimationAsset = (SkeletonDataAsset)EditorGUILayout.ObjectField(
            "Slime Animation Asset", 
            slimeAnimationAsset, 
            typeof(SkeletonDataAsset), 
            false
        );
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Load SlimeAnimation Prefab"))
        {
            LoadSlimeAnimationAsset();
        }
        
        EditorGUILayout.Space();
        
        if (slimeAnimationAsset != null)
        {
            if (GUILayout.Button("Apply Animation to All Body Traits"))
            {
                ApplyAnimationToBodyTraits();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please load the SlimeAnimation asset first", MessageType.Warning);
        }
    }
    
    private void LoadSlimeAnimationAsset()
    {
        string[] guids = AssetDatabase.FindAssets("Slimeanimation t:Prefab");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                var skeletonAnimation = prefab.GetComponent<SkeletonAnimation>();
                if (skeletonAnimation != null)
                {
                    slimeAnimationAsset = skeletonAnimation.skeletonDataAsset;
                    Debug.Log($"Loaded SlimeAnimation asset: {slimeAnimationAsset.name}");
                }
                else
                {
                    Debug.LogError("Slimeanimation prefab doesn't have SkeletonAnimation component");
                }
            }
        }
        else
        {
            Debug.LogError("Could not find Slimeanimation prefab");
        }
    }
    
    private void ApplyAnimationToBodyTraits()
    {
        if (slimeAnimationAsset == null)
        {
            Debug.LogError("No animation asset loaded");
            return;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:TraitSO", new[] { "Assets/TraitDatabase" });
        int updatedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TraitSO trait = AssetDatabase.LoadAssetAtPath<TraitSO>(path);
            
            if (trait != null && trait.type == TraitType.Body)
            {
                trait.animationAsset = slimeAnimationAsset;
                trait.animationName = "animation";
                
                EditorUtility.SetDirty(trait);
                updatedCount++;
                
                Debug.Log($"Updated trait: {trait.name}");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Updated {updatedCount} Body traits with animation");
        EditorUtility.DisplayDialog("Animation Setup Complete", 
            $"Successfully updated {updatedCount} Body traits with animation.", "OK");
    }
}


