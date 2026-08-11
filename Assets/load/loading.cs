using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class loading : MonoBehaviour
{
    private const float MinimumLoadingScreenTime = 0.15f;
    private const float ProgressSpeed = 4f;

    public static loading Instance;
    public Animator animator;
    public Slider slider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task onplay(int scene)
    {
        if (!SceneLoader.TryBeginSceneLoad())
            return;

        await PlayLoading(scene);
    }

    public async Task LoadSceneByName(string sceneName)
    {
        if (!SceneLoader.TryBeginSceneLoad())
            return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is empty.");
            SceneLoader.EndSceneLoadRequest();
            return;
        }

        int sceneIndex = GetSceneIndexByName(sceneName);
        if (sceneIndex < 0)
        {
            Debug.LogError($"Scene '{sceneName}' not found in build settings.");
            SceneLoader.EndSceneLoadRequest();
            return;
        }

        await PlayLoading(sceneIndex);
    }

    private async Task PlayLoading(int sceneIndex)
    {
        if (transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(true);

        if (slider != null)
            slider.value = 0f;

        if (animator != null)
            animator.SetBool("nextScene", true);

        await LoadSceneAsync(sceneIndex);
    }

    private async Task LoadSceneAsync(int index)
    {
        AsyncOperation scene = SceneManager.LoadSceneAsync(index);
        if (scene == null)
        {
            SceneLoader.EndSceneLoadRequest();
            return;
        }

        scene.allowSceneActivation = false;
        float startedAt = Time.realtimeSinceStartup;

        // Unity reports an async scene as ready for activation at 0.9.
        // Drive the UI from real loading progress instead of imposing a fixed delay.
        while (scene.progress < 0.9f ||
               Time.realtimeSinceStartup - startedAt < MinimumLoadingScreenTime)
        {
            if (slider != null)
            {
                float progress = Mathf.Clamp01(scene.progress / 0.9f);
                slider.value = Mathf.MoveTowards(
                    slider.value,
                    progress,
                    ProgressSpeed * Time.unscaledDeltaTime);
            }

            await Task.Yield();
        }

        if (slider != null)
            slider.value = 1f;

        scene.allowSceneActivation = true;

        while (!scene.isDone)
            await Task.Yield();
    }

    private int GetSceneIndexByName(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
                return i;
        }

        return -1;
    }
}
