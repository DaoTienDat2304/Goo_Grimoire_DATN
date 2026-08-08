using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class loading : MonoBehaviour
{
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

        while (slider != null && slider.value < 1f)
        {
            slider.value += 0.4f * Time.deltaTime;
            await Task.Yield();
        }

        scene.allowSceneActivation = true;
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
