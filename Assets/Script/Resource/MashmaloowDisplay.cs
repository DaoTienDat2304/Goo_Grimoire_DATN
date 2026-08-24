using TMPro;
using UnityEngine;

public class MashmaloowDisplay : MonoBehaviour
{
    private ResourceManager resourceManager;
    public TMP_Text count;

    private void Awake()
    {
        ResolveText();
        EnsureResourceManager();
    }

    private void OnValidate()
    {
        ResolveText();
    }

    private void OnEnable()
    {
        ResourceManager.OnResourceChanged -= HandleResourceChanged;
        ResourceManager.OnResourceChanged += HandleResourceChanged;
        ResolveText();
        EnsureResourceManager();
        RefreshCount();
    }

    private void OnDisable()
    {
        ResourceManager.OnResourceChanged -= HandleResourceChanged;
    }

    private void HandleResourceChanged(ResourceType type, int oldAmount, int newAmount)
    {
        if (type == ResourceType.Marshmallow && count != null)
            count.text = newAmount.ToString();
    }

    private void RefreshCount()
    {
        if (count != null && resourceManager != null)
            count.text = resourceManager.GetResource(ResourceType.Marshmallow).ToString();
    }

    private void ResolveText()
    {
        if (count == null)
            count = GetComponentInChildren<TMP_Text>(true);
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
