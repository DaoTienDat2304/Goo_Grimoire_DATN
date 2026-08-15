using UnityEngine;
using UnityEngine.UI;

public class MashmaloowDisplay : MonoBehaviour
{
    private ResourceManager resourceManager;
    public Text count;

    void Start()
    {
        EnsureResourceManager();
    }

    void Update()
    {
        if (count == null) return;
        EnsureResourceManager();
        if (resourceManager == null) return;
        count.text = resourceManager.GetResource(ResourceType.Marshmallow).ToString();
    }

    private void EnsureResourceManager()
    {
        if (resourceManager != null)
            return;

        resourceManager = ResourceManager.Instance;
        if (resourceManager != null)
            return;

        resourceManager = FindAnyObjectByType<ResourceManager>();
        if (resourceManager != null)
            return;

        resourceManager = new GameObject("ResourceManager").AddComponent<ResourceManager>();
    }
}
