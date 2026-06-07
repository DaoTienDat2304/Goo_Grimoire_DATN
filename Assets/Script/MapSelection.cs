using System.Collections;
using UnityEngine;

public class MapSelection : MonoBehaviour
{
    loading loading;
    public int MapIndex;
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

        loading loading = GameObject.Find("loading").GetComponent<loading>();
        await loading.onplay(MapIndex);
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
}
