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
        if (count == null) return;
        if (resourceManager == null)
        {
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null) return; // chưa có ResourceManager trong scene này
        }
        count.text = resourceManager.GetResource(ResourceType.Marshmallow).ToString();
    }
}
