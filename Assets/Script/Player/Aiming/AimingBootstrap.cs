using UnityEngine;
using UnityEngine.SceneManagement;

public class AimingBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAimingAfterSceneLoad()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
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
