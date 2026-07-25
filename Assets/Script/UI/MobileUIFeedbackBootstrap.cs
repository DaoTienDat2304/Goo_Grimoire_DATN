using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MobileUIFeedbackBootstrap : MonoBehaviour
{
    private const float ScanInterval = 0.75f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindObjectsByType<MobileUIFeedbackBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            return;

        var runner = new GameObject("MobileUIFeedbackBootstrap");
        DontDestroyOnLoad(runner);
        runner.AddComponent<MobileUIFeedbackBootstrap>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(ScanLoop());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallFeedback();
    }

    private IEnumerator ScanLoop()
    {
        var wait = new WaitForSecondsRealtime(ScanInterval);
        while (enabled)
        {
            InstallFeedback();
            yield return wait;
        }
    }

    private static void InstallFeedback()
    {
        InstallInventorySlotFeedback();

        var selectables = FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var selectable in selectables)
        {
            if (selectable == null)
                continue;

            if (selectable.GetComponentInParent<Canvas>(true) == null)
                continue;

            if (ShouldIgnore(selectable))
            {
                var existingFeedback = selectable.GetComponent<MobileUIFeedback>();
                if (existingFeedback != null)
                    Destroy(existingFeedback);
                continue;
            }

            var feedback = selectable.GetComponent<MobileUIFeedback>();
            if (feedback == null)
                feedback = selectable.gameObject.AddComponent<MobileUIFeedback>();

            bool isTextInput = IsTextInput(selectable);
            feedback.ConfigureForTextInput(isTextInput);
            feedback.SetRippleEnabled(!isTextInput);
        }
    }

    private static void InstallInventorySlotFeedback()
    {
        var slots = FindObjectsByType<InventorySlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var slot in slots)
        {
            if (slot == null || slot.GetComponentInParent<Canvas>(true) == null)
                continue;

            if (ShouldIgnoreTransform(slot.transform))
                continue;

            if (slot.GetComponent<MobileUIFeedback>() == null)
                slot.gameObject.AddComponent<MobileUIFeedback>();
        }
    }

    private static bool ShouldIgnore(Selectable selectable)
    {
        return ShouldIgnoreTransform(selectable.transform);
    }

    private static bool ShouldIgnoreTransform(Transform target)
    {
        return IsMaskObject(target) || IsMaskObject(target != null ? target.parent : null);
    }

    private static bool IsTextInput(Selectable selectable)
    {
        return selectable is InputField;
    }

    private static bool IsMaskObject(Transform target)
    {
        if (target == null)
            return false;

        string objectName = target.name.ToLowerInvariant().Replace(" ", string.Empty);
        bool isNamedMask = objectName == "mask" || objectName == "mask(1)";
        bool isAchievementMask = isNamedMask && target.GetComponentInParent<ArchievementManager>(true) != null;
        return isAchievementMask || target.GetComponent<Mask>() != null || target.GetComponent<RectMask2D>() != null;
    }
}
