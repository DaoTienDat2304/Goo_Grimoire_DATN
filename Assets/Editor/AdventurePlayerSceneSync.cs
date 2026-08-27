using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdventurePlayerSceneSync
{
    private const string SourceScenePath = "Assets/Scenes/Map1_IceMap.unity";
    private const string TempPrefabPath = "Assets/Editor/__PlayerSyncTemplate.prefab";
    private static readonly string[] TargetScenePaths =
    {
        "Assets/Scenes/Map2_Fantasymap.unity",
        "Assets/Scenes/Map3_DungeonMap.unity"
    };

    [MenuItem("Tools/Adventure/Sync Player From Ice Map")]
    public static void SyncPlayerFromIceMap()
    {
        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        GameObject sourcePlayer = FindRootPlayer();
        if (sourcePlayer == null)
        {
            Debug.LogError($"AdventurePlayerSceneSync: Player not found in {SourceScenePath}");
            return;
        }

        EnsurePlayerComponents(sourcePlayer);
        EditorSceneManager.MarkSceneDirty(sourceScene);
        EditorSceneManager.SaveScene(sourceScene);

        GameObject sourceTemplate = PrefabUtility.SaveAsPrefabAsset(sourcePlayer, TempPrefabPath);
        if (sourceTemplate == null)
        {
            Debug.LogError($"AdventurePlayerSceneSync: Could not create temp prefab at {TempPrefabPath}");
            return;
        }

        foreach (string scenePath in TargetScenePaths)
        {
            Scene targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject oldPlayer = FindRootPlayer();
            Vector3 targetPosition = oldPlayer != null ? oldPlayer.transform.position : sourceTemplate.transform.position;
            Quaternion targetRotation = oldPlayer != null ? oldPlayer.transform.rotation : sourceTemplate.transform.rotation;
            Vector3 targetScale = sourceTemplate.transform.localScale;

            if (oldPlayer != null)
                Object.DestroyImmediate(oldPlayer);

            GameObject newPlayer = Object.Instantiate(sourceTemplate);
            newPlayer.name = "Player";
            newPlayer.hideFlags = HideFlags.None;
            newPlayer.tag = "Player";
            newPlayer.transform.position = targetPosition;
            newPlayer.transform.rotation = targetRotation;
            newPlayer.transform.localScale = targetScale;
            SceneManager.MoveGameObjectToScene(newPlayer, targetScene);

            EnsurePlayerComponents(newPlayer);
            WireSceneReferences(newPlayer);

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
        }

        AssetDatabase.DeleteAsset(TempPrefabPath);
        EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
    }

    private static GameObject FindRootPlayer()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "Player")
                return root;
        }

        return GameObject.Find("Player");
    }

    private static void EnsurePlayerComponents(GameObject player)
    {
        if (!player.TryGetComponent(out Rigidbody2D rb))
            rb = player.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (!player.TryGetComponent(out BoxCollider2D boxCollider))
            boxCollider = player.AddComponent<BoxCollider2D>();

        boxCollider.isTrigger = false;

        if (!player.TryGetComponent<PlayerMovement>(out PlayerMovement movement))
            movement = player.AddComponent<PlayerMovement>();

        SerializedObject movementObject = new SerializedObject(movement);
        movementObject.FindProperty("rb").objectReferenceValue = rb;
        movementObject.ApplyModifiedPropertiesWithoutUndo();

        if (player.GetComponentInChildren<Animator>(true) == null)
            Debug.LogWarning($"AdventurePlayerSceneSync: {player.name} has no Animator.");
    }

    private static void WireSceneReferences(GameObject player)
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour == null)
                continue;

            SerializedObject serializedBehaviour = new SerializedObject(behaviour);
            SerializedProperty movementProperty = serializedBehaviour.FindProperty("playerMovement");
            if (movementProperty != null)
                movementProperty.objectReferenceValue = movement;
            serializedBehaviour.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (Component component in Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (component == null || !component.GetType().FullName.Contains("Cinemachine"))
                continue;

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty trackingTarget = serializedObject.FindProperty("Target.TrackingTarget");
            if (trackingTarget != null)
            {
                trackingTarget.objectReferenceValue = player.transform;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
