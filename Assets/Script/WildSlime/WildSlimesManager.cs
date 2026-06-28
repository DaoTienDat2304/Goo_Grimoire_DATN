using UnityEngine;

public class WildSlimesManager : MonoBehaviour
{
    public WildSlimes wildSlimes;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void Awake()
    {
        wildSlimes.slimes.Clear();
    }
    // Update is called once per frame
    void Update()
    {

    }
}
