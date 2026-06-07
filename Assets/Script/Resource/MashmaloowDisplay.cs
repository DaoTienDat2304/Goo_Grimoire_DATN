using UnityEngine;
using UnityEngine.UI;

public class MashmaloowDisplay : MonoBehaviour
{
    private ResourceManager resourceManager;
    public Text count;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        resourceManager = ResourceManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        count.text = resourceManager.GetResource(ResourceType.Marshmallow).ToString();
    }
}
