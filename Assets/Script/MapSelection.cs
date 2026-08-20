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
            Debug.LogWarning("Need 1 slime in team.");
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
            case 1:
                return "Map1_IceMap";
            case 2:
                return "Map2_Fantasymap";
            case 3:
                return "Map3_DungeonMap";
            case 4:
                return "Map4_T";
            default:
                return string.Empty;
        }
    }
}
