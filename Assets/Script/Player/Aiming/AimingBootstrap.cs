using UnityEngine;
using UnityEngine.SceneManagement;

public class AimingBootstrap : MonoBehaviour
{
    private static bool sceneHookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        sceneHookInstalled = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAimingAfterSceneLoad()
    {
        if (!sceneHookInstalled)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneHookInstalled = true;
        }

        EnsureAimingExists();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAimingExists();
    }

    private static void EnsureAimingExists()
    {
        if (!IsAimingScene(SceneManager.GetActiveScene().name))
            return;

        PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (playerMovement == null)
            return;

        Aiming aiming = playerMovement.GetComponent<Aiming>();
        if (aiming == null)
            aiming = playerMovement.gameObject.AddComponent<Aiming>();

        Transform startPosition = FindOrCreateChild(playerMovement.transform, "StartPosition", new Vector3(0f, -0.2f, 0f));
        Transform idlePosition = FindOrCreateChild(playerMovement.transform, "IdlePosition", new Vector3(0f, -0.2f, 0f));
        LineRenderer lineRenderer = FindAnyObjectByType<LineRenderer>(FindObjectsInactive.Include);
        if (lineRenderer == null)
            lineRenderer = CreateLineRenderer(playerMovement.transform);

        aiming.ConfigureRuntimeReferences(
            lineRenderer,
            startPosition,
            idlePosition,
            null,
            playerMovement,
            Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>());
        aiming.SetTamingPanel(TamingPanelFlow.GetCanonicalPanel());

        Debug.Log($"AimingBootstrap ready in {SceneManager.GetActiveScene().name}.", aiming);
    }

    private static bool IsAimingScene(string sceneName)
    {
        return sceneName == "travelSence"
            || sceneName == "Map1_IceMap"
            || sceneName == "Map2_Fantasymap"
            || sceneName == "Map3_DungeonMap"
            || sceneName.ToLower().Contains("travel");
    }

    private static Transform FindOrCreateChild(Transform parent, string childName, Vector3 localPosition)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        var childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        childObject.transform.localPosition = localPosition;
        return childObject.transform;
    }

    private static LineRenderer CreateLineRenderer(Transform parent)
    {
        var lineObject = new GameObject("AimingLineRenderer");
        lineObject.transform.SetParent(parent, false);
        var line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.05f;
        line.endWidth = 0.02f;
        line.useWorldSpace = true;
        line.enabled = false;
        return line;
    }
}

public static class TamingPanelFlow
{
    private static tamingManager canonicalManager;
    private static bool sceneHookInstalled;

    public static tamingManager CanonicalManager
    {
        get
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (canonicalManager == null || canonicalManager.gameObject.scene != activeScene)
                PrepareScene(activeScene);

            return canonicalManager;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        canonicalManager = null;
        sceneHookInstalled = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!sceneHookInstalled)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHookInstalled = true;
        }

        PrepareScene(SceneManager.GetActiveScene());
    }

    public static GameObject GetCanonicalPanel()
    {
        tamingManager manager = CanonicalManager;
        return manager != null ? manager.gameObject : null;
    }

    public static bool OpenFor(WildSlimeTraits wildSlime)
    {
        if (wildSlime == null)
            return false;

        tamingManager manager = CanonicalManager;
        if (manager == null)
            return false;

        manager.BeginTaming(wildSlime.wildSlimeID, wildSlime.newSlime);
        return true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PrepareScene(scene);
    }

    private static void PrepareScene(Scene scene)
    {
        canonicalManager = null;
        if (!IsAdventureScene(scene.name))
            return;

        tamingManager[] managers = Object.FindObjectsByType<tamingManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Transform sidebarRoot = FindSceneTransform(scene, "TameSidebarRoot");
        if (sidebarRoot != null)
        {
            foreach (tamingManager manager in sidebarRoot.GetComponentsInChildren<tamingManager>(true))
            {
                if (manager != null && manager.gameObject.name == "TamingPanel")
                {
                    canonicalManager = manager;
                    break;
                }
            }
        }

        if (canonicalManager == null)
        {
            foreach (tamingManager manager in managers)
            {
                if (manager != null
                    && manager.gameObject.scene == scene
                    && manager.gameObject.name == "TamingPanel")
                {
                    canonicalManager = manager;
                    break;
                }
            }
        }

        if (canonicalManager == null)
        {
            Debug.LogWarning($"[TamingPanel] No tamingManager found in {scene.name}.");
            return;
        }

        foreach (tamingManager manager in managers)
        {
            if (manager == null || manager.gameObject.scene != scene || manager == canonicalManager)
                continue;

            manager.enabled = false;
            manager.gameObject.SetActive(false);
        }

        canonicalManager.enabled = true;
        canonicalManager.PrepareForRuntime();
        canonicalManager.gameObject.SetActive(false);

        Debug.Log($"[TamingPanel] Using {GetHierarchyPath(canonicalManager.transform)} in {scene.name}.", canonicalManager);
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChild(root.transform, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChild(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }

    private static bool IsAdventureScene(string sceneName)
    {
        return sceneName == "Map1_IceMap"
            || sceneName == "Map2_Fantasymap"
            || sceneName == "Map3_DungeonMap";
    }
}
