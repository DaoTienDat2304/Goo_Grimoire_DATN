using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Helper class để load scene với loading screen
/// </summary>
public static class SceneLoader
{
    /// <summary>
    /// Load scene với loading screen bằng tên scene
    /// </summary>
    public static async Task LoadSceneWithLoading(string sceneName)
    {
        if (loading.Instance != null)
        {
            loading.Instance.gameObject.SetActive(true);
            await loading.Instance.LoadSceneByName(sceneName);
        }
        else
        {
            // Fallback nếu không có loading instance
            Debug.LogWarning("Loading instance not found! Loading scene without loading screen.");
            SceneManager.LoadScene(sceneName);
        }
    }
    
    /// <summary>
    /// Load scene với loading screen bằng index
    /// </summary>
    public static async Task LoadSceneWithLoading(int sceneIndex)
    {
        if (loading.Instance != null)
        {
            loading.Instance.gameObject.SetActive(true);
            await loading.Instance.onplay(sceneIndex);
        }
        else
        {
            // Fallback nếu không có loading instance
            Debug.LogWarning("Loading instance not found! Loading scene without loading screen.");
            SceneManager.LoadScene(sceneIndex);
        }
    }
    
    /// <summary>
    /// Coroutine version để load scene với loading (dùng cho IEnumerator)
    /// </summary>
    public static IEnumerator LoadSceneWithLoadingCoroutine(string sceneName)
    {
        if (loading.Instance != null)
        {
            loading.Instance.gameObject.SetActive(true);
            
            // Chuyển async Task thành coroutine
            var task = loading.Instance.LoadSceneByName(sceneName);
            while (!task.IsCompleted)
            {
                yield return null;
            }
        }
        else
        {
            // Fallback nếu không có loading instance
            Debug.LogWarning("Loading instance not found! Loading scene without loading screen.");
            SceneManager.LoadScene(sceneName);
        }
    }
    
    /// <summary>
    /// Coroutine version để load scene với loading bằng index (dùng cho IEnumerator)
    /// </summary>
    public static IEnumerator LoadSceneWithLoadingCoroutine(int sceneIndex)
    {
        if (loading.Instance != null)
        {
            loading.Instance.gameObject.SetActive(true);
            
            // Chuyển async Task thành coroutine
            var task = loading.Instance.onplay(sceneIndex);
            while (!task.IsCompleted)
            {
                yield return null;
            }
        }
        else
        {
            // Fallback nếu không có loading instance
            Debug.LogWarning("Loading instance not found! Loading scene without loading screen.");
            SceneManager.LoadScene(sceneIndex);
        }
    }
}

