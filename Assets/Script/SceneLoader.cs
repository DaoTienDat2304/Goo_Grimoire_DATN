using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const float SceneLoadClickCooldown = 0.2f;
    private static bool isLoadingScene;
    private static float lastSceneLoadRequestTime = -SceneLoadClickCooldown;

    static SceneLoader()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static bool TryBeginSceneLoad()
    {
        if (isLoadingScene)
            return false;

        if (Time.unscaledTime - lastSceneLoadRequestTime < SceneLoadClickCooldown)
            return false;

        isLoadingScene = true;
        lastSceneLoadRequestTime = Time.unscaledTime;
        return true;
    }

    public static void EndSceneLoadRequest()
    {
        isLoadingScene = false;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EndSceneLoadRequest();
    }

    public static async Task LoadSceneWithLoading(string sceneName)
    {
        if (loading.Instance != null)
        {
            await loading.Instance.LoadSceneByName(sceneName);
            return;
        }

        if (!TryBeginSceneLoad())
            return;

        Debug.LogWarning("Loading instance not found. Loading scene without loading screen.");
        SceneManager.LoadScene(sceneName);
    }

    public static async Task LoadSceneWithLoading(int sceneIndex)
    {
        if (loading.Instance != null)
        {
            await loading.Instance.onplay(sceneIndex);
            return;
        }

        if (!TryBeginSceneLoad())
            return;

        Debug.LogWarning("Loading instance not found. Loading scene without loading screen.");
        SceneManager.LoadScene(sceneIndex);
    }

    public static IEnumerator LoadSceneWithLoadingCoroutine(string sceneName)
    {
        Task task = LoadSceneWithLoading(sceneName);
        while (!task.IsCompleted)
            yield return null;
    }

    public static IEnumerator LoadSceneWithLoadingCoroutine(int sceneIndex)
    {
        Task task = LoadSceneWithLoading(sceneIndex);
        while (!task.IsCompleted)
            yield return null;
    }
}
