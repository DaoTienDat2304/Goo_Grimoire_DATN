using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SharedPlayerMapSetup
{
    private const string SourceScenePath = "Assets/Scenes/Map2_Fantasymap.unity";
    private const string PlayerControllerPath = "Assets/Sprite/Idle/Player.controller";
    private const float PlayerScale = 3.266753f;
    private const string PlayerSortingLayer = "Player";
    private const int PlayerSortingOrder = 10;

    private static readonly string[] TargetScenePaths =
    {
        "Assets/Scenes/Map1_IceMap.unity",
        "Assets/Scenes/Map3_DungeonMap.unity",
        "Assets/Scenes/Map4_T.unity",
    };

    [MenuItem("Tools/Map/Copy Shared Player From Map2")]
    public static void CopySharedPlayerFromMap2()
    {
        Scene baseScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        GameObject basePlayer = FindRootPlayer(baseScene);
        if (basePlayer == null)
        {
            Debug.LogError("SharedPlayerMapSetup: Player not found in Map2_Fantasymap.");
            return;
        }

        EnsurePlayerSetup(basePlayer);
        EditorSceneManager.MarkSceneDirty(baseScene);
        EditorSceneManager.SaveScene(baseScene);

        foreach (string scenePath in TargetScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            GameObject sourcePlayer = FindRootPlayer(sourceScene);
            if (sourcePlayer == null)
            {
                Debug.LogError("SharedPlayerMapSetup: Player not found in Map2_Fantasymap.");
                EditorSceneManager.CloseScene(sourceScene, true);
                return;
            }

            GameObject oldPlayer = FindRootPlayer(scene);
            Vector3 position = oldPlayer != null ? oldPlayer.transform.position : sourcePlayer.transform.position;
            Quaternion rotation = oldPlayer != null ? oldPlayer.transform.rotation : sourcePlayer.transform.rotation;
            Vector3 scale = oldPlayer != null ? oldPlayer.transform.localScale : sourcePlayer.transform.localScale;

            GameObject newPlayer = Object.Instantiate(sourcePlayer);
            newPlayer.name = "Player";
            SceneManager.MoveGameObjectToScene(newPlayer, scene);
            newPlayer.transform.SetPositionAndRotation(position, rotation);
            newPlayer.transform.localScale = scale;
            EnsurePlayerSetup(newPlayer);

            if (oldPlayer != null)
            {
                RewireSceneReferences(scene, oldPlayer, newPlayer);
                Object.DestroyImmediate(oldPlayer);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(sourceScene, true);
            Debug.Log($"SharedPlayerMapSetup: Replaced Player in {scenePath}");
        }

        AssetDatabase.SaveAssets();
    }

    private static GameObject FindRootPlayer(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t.name == "Player")
                    return t.gameObject;
            }
        }

        return null;
    }

    private static void EnsurePlayerSetup(GameObject player)
    {
        player.tag = "Player";
        float facing = player.transform.localScale.x < 0f ? -1f : 1f;
        player.transform.localScale = new Vector3(PlayerScale * facing, PlayerScale, PlayerScale);
        foreach (SpriteRenderer renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingLayerName = PlayerSortingLayer;
            renderer.sortingOrder = PlayerSortingOrder;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        Collider2D collider = player.GetComponent<Collider2D>();
        if (collider == null)
        {
            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.8f, 1.2f);
            capsule.offset = new Vector2(0f, 0.1f);
        }

        if (player.GetComponent<PlayerMovement>() == null)
            player.AddComponent<PlayerMovement>();
        if (player.GetComponent<PlayerAttackAnimator>() == null)
            player.AddComponent<PlayerAttackAnimator>();

        Animator animator = player.GetComponentInChildren<Animator>(true);
        if (animator == null)
            animator = player.AddComponent<Animator>();

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerControllerPath);
        if (controller != null)
            animator.runtimeAnimatorController = controller;
    }

    private static void RewireSceneReferences(Scene scene, GameObject oldPlayer, GameObject newPlayer)
    {
        Component[] oldComponents = oldPlayer.GetComponentsInChildren<Component>(true);
        Component[] newComponents = newPlayer.GetComponentsInChildren<Component>(true);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            RewireObject(root, oldPlayer, newPlayer, oldComponents, newComponents);
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
                RewireObject(component, oldPlayer, newPlayer, oldComponents, newComponents);
        }
    }

    private static void RewireObject(Object obj, GameObject oldPlayer, GameObject newPlayer, Component[] oldComponents, Component[] newComponents)
    {
        if (obj == null || obj == oldPlayer || obj == newPlayer)
            return;

        SerializedObject serializedObject;
        try
        {
            serializedObject = new SerializedObject(obj);
        }
        catch
        {
            return;
        }

        bool changed = false;
        SerializedProperty property = serializedObject.GetIterator();
        while (property.NextVisible(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            Object replacement = GetReplacement(property.objectReferenceValue, oldPlayer, newPlayer, oldComponents, newComponents);
            if (replacement == null)
                continue;

            property.objectReferenceValue = replacement;
            changed = true;
        }

        if (changed)
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object GetReplacement(Object value, GameObject oldPlayer, GameObject newPlayer, Component[] oldComponents, Component[] newComponents)
    {
        if (value == oldPlayer)
            return newPlayer;
        if (value == oldPlayer.transform)
            return newPlayer.transform;

        for (int i = 0; i < oldComponents.Length && i < newComponents.Length; i++)
        {
            if (value == oldComponents[i])
                return newComponents[i];
        }

        return null;
    }
}
