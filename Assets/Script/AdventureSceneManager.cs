using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AdventureSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public async void movescene()
    {
        await SceneLoader.LoadSceneWithLoading("firstsave");
    }
}
