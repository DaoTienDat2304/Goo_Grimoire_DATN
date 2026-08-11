using System.Collections;
using UnityEngine;

public class MapSelection : MonoBehaviour
{
    public int MapIndex;
    public string sceneName;
    public GameObject warningText;

    private static readonly WaitForSeconds WarningDelay = new(3f);

    void Start()
    {
        if (warningText != null)
            warningText.SetActive(false);
    }

    void Update() { }

    public async void onlick()
    {
        var saveSystem = SaveAndLoadSystem.Instance;
        var team = saveSystem != null ? saveSystem.GetTeam() : null;
        if (team == null || team.team == null || team.team.Count == 0)
        {
            Debug.LogWarning("Cần ít nhất 1 slime trong team để chiến đấu!");
            ShowWarning();
            return;
        }
        saveSystem.Save();

        if (warningText != null)
            warningText.SetActive(false);

        string targetScene = GetTargetSceneName();
        if (!string.IsNullOrEmpty(targetScene))
        {
            await SceneLoader.LoadSceneWithLoading(targetScene);
            return;
        }

        await SceneLoader.LoadSceneWithLoading(MapIndex);
    }

    private void ShowWarning()
    {
        if (warningText == null) return;
        StopCoroutine(nameof(HideWarningAfterDelay));
        warningText.SetActive(true);
        StartCoroutine(nameof(HideWarningAfterDelay));
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return WarningDelay;
        if (warningText != null)
            warningText.SetActive(false);
    }

    private string GetTargetSceneName()
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
            return sceneName.Trim();

        switch (MapIndex)
        {
            case 3:
                return "adventureSence";
            case 4:
                return "Map2";
            case 5:
                return "Frozen_Map";
            case 6:
                return "NonameMap";
            default:
                return string.Empty;
        }
    }
}
