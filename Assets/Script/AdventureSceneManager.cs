using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdventureSceneManager : MonoBehaviour
{
    private bool isReturningHome;

    public async void movescene()
    {
        if (isReturningHome)
            return;

        isReturningHome = true;
        SaveAndLoadSystem.Instance?.Save();
        await SceneLoader.LoadSceneWithLoading("firstsave");
    }
}

public static class AdventureReturnFlowBootstrap
{
    private static bool sceneHookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
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

        WireBrokenReturnButtons(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        WireBrokenReturnButtons(scene);
    }

    private static void WireBrokenReturnButtons(Scene scene)
    {
        if (!IsAdventureScene(scene.name))
            return;

        AdventureSceneManager manager = null;
        Button[] buttons = Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (!HasBrokenReturnListener(button))
                continue;

            if (manager == null)
            {
                manager = Object.FindAnyObjectByType<AdventureSceneManager>(FindObjectsInactive.Include);
                if (manager == null)
                    manager = new GameObject("AdventureSceneManager").AddComponent<AdventureSceneManager>();
            }

            button.onClick.RemoveListener(manager.movescene);
            button.onClick.AddListener(manager.movescene);
        }
    }

    private static bool HasBrokenReturnListener(Button button)
    {
        int listenerCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < listenerCount; i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == nameof(AdventureSceneManager.movescene)
                && button.onClick.GetPersistentTarget(i) == null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAdventureScene(string sceneName)
    {
        return sceneName == "Map1_IceMap"
            || sceneName == "Map2_Fantasymap"
            || sceneName == "Map3_DungeonMap";
    }
}
