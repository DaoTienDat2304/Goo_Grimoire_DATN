using Spine;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class loading : MonoBehaviour
{
    public static loading Instance;
    public Animator animator;
    public Slider slider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

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
    void Start()
    {
       
    }

    public async Task onplay(int scene)
    {
        transform.GetChild(0).gameObject.SetActive(true);
        slider.value = 0;
        animator.SetBool("nextScene", true);

        await sceneloadingAsync(scene); // Task awaitable
    }

    private async Task sceneloadingAsync(int index)
    {
        AsyncOperation scene = SceneManager.LoadSceneAsync(index);
        scene.allowSceneActivation = false;

        while (slider.value < 1f)
        {
            slider.value += 0.4f * Time.deltaTime;
            await Task.Yield(); // t��ng ���ng yield return null
        }

        scene.allowSceneActivation = true;
    }
    
    /// <summary>
    /// Load scene bằng tên với loading screen
    /// </summary>
    public async Task LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is empty!");
            return;
        }
        
        // Tìm scene index từ tên scene
        int sceneIndex = -1;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
            {
                sceneIndex = i;
                break;
            }
        }
        
        if (sceneIndex < 0)
        {
            Debug.LogError($"Scene '{sceneName}' not found in build settings!");
            return;
        }
        
        await onplay(sceneIndex);
    }
}
