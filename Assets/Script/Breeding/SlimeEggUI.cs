using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlimeEggUI : MonoBehaviour
{
    [Header("HUD")]
    public Button eggHudButton;
    public TMP_Text eggCountText;
    [Header("Panels")]
    public GameObject eggInventoryPanel;
    public GameObject incubationConfirmPanel;
    public GameObject hatchResultPanel;
    public GameObject hatchAnimationRoot;
    [Header("Egg slots")]
    public Button[] eggSlotButtons = new Button[3];
    public TMP_Text[] eggSlotTexts = new TMP_Text[3];
    [Header("Incubation")]
    public TMP_Text incubationInfoText;
    public Button incubateButton;
    public Button finishWithGemsButton;
    public TMP_Text gemCostText;
    [Header("Hatch result")]
    public TMP_Text hatchTitleText;
    public TMP_Text hatchStatsText;

    private SlimeEggSystem system;
    private int selectedEgg = -1;

    private void Start()
    {
        eggHudButton.onClick.AddListener(OpenInventory);
        incubateButton.onClick.AddListener(ConfirmIncubation);
        finishWithGemsButton.onClick.AddListener(FinishWithGems);
        for (int i = 0; i < eggSlotButtons.Length; i++)
        {
            int index = i;
            eggSlotButtons[i].onClick.AddListener(() => SelectEgg(index));
        }
        eggInventoryPanel.SetActive(false);
        incubationConfirmPanel.SetActive(false);
        hatchResultPanel.SetActive(false);
        FindSystem();
        Refresh();
    }

    private void OnDestroy()
    {
        if (system != null) system.EggsChanged -= Refresh;
    }

    private void Update()
    {
        if (system == null) FindSystem();
        if (system != null) RefreshVisibleTimers();
    }

    private void FindSystem()
    {
        system = SlimeEggSystem.Instance != null ? SlimeEggSystem.Instance : FindAnyObjectByType<SlimeEggSystem>();
        if (system != null)
        {
            system.EggsChanged -= Refresh;
            system.EggsChanged += Refresh;
        }
    }

    public void OpenInventory()
    {
        hatchResultPanel.SetActive(false);
        incubationConfirmPanel.SetActive(false);
        eggInventoryPanel.SetActive(true);
        Refresh();
    }

    public void CloseInventory() => eggInventoryPanel.SetActive(false);
    public void CloseIncubation() => incubationConfirmPanel.SetActive(false);
    public void CloseHatchResult() => hatchResultPanel.SetActive(false);

    private void SelectEgg(int index)
    {
        if (system == null || index < 0 || index >= system.EggCount) return;
        selectedEgg = index;
        var egg = system.Eggs[index];
        if (egg.isIncubating && system.GetRemainingSeconds(index) <= 0f)
        {
            Slime slime = system.Hatch(index);
            if (slime != null) ShowHatchResult(slime);
            return;
        }
        incubationConfirmPanel.SetActive(true);
        RefreshIncubationPanel();
    }

    private void ConfirmIncubation()
    {
        if (system == null || !system.StartIncubation(selectedEgg)) return;
        incubationConfirmPanel.SetActive(false);
        Refresh();
    }

    private void FinishWithGems()
    {
        if (system == null || !system.FinishWithGems(selectedEgg)) return;
        incubationConfirmPanel.SetActive(false);
        Refresh();
    }

    private void ShowHatchResult(Slime slime)
    {
        eggInventoryPanel.SetActive(false);
        incubationConfirmPanel.SetActive(false);
        hatchResultPanel.SetActive(true);
        hatchAnimationRoot.SetActive(true); // Attach Animator/Timeline here later.
        hatchTitleText.text = $"{slime.slimeName}\n{slime.body.Rarity} • {slime.eggStatQuality}";
        hatchStatsText.text =
            $"HP             {slime.totalHP:N0}\n" +
            $"ATK            {slime.totalAttack:N0}\n" +
            $"MAGIC ATK      {slime.totalMagicAttack:N0}\n" +
            $"DEF            {slime.totalDefense:N0}\n" +
            $"SPEED          {slime.totalSpeed:N0}\n" +
            $"CRIT RATE      {slime.critRate:0.#}%\n" +
            $"CRIT DAMAGE    {slime.critDamage:0.#}%\n" +
            $"STAT ROLL      {slime.eggStatRollPercent:0.##}%";
    }

    private void Refresh()
    {
        int count = system != null ? system.EggCount : 0;
        eggCountText.text = count.ToString();
        for (int i = 0; i < eggSlotButtons.Length; i++)
        {
            bool exists = system != null && i < count;
            eggSlotButtons[i].interactable = exists;
            eggSlotTexts[i].text = exists ? GetEggLabel(i) : "EMPTY";
        }
        RefreshIncubationPanel();
    }

    private void RefreshVisibleTimers()
    {
        if (eggInventoryPanel.activeSelf) Refresh();
        else if (incubationConfirmPanel.activeSelf) RefreshIncubationPanel();
        else eggCountText.text = system.EggCount.ToString();
    }

    private string GetEggLabel(int index)
    {
        var egg = system.Eggs[index];
        if (!egg.isIncubating) return $"EGG {index + 1}\nReady to incubate";
        float left = system.GetRemainingSeconds(index);
        return left <= 0f ? $"EGG {index + 1}\nTAP TO HATCH!" : $"EGG {index + 1}\n{FormatTime(left)}";
    }

    private void RefreshIncubationPanel()
    {
        bool valid = system != null && selectedEgg >= 0 && selectedEgg < system.EggCount;
        if (!valid) return;
        var egg = system.Eggs[selectedEgg];
        float left = system.GetRemainingSeconds(selectedEgg);
        incubationInfoText.text = egg.isIncubating
            ? (left <= 0f ? "Incubation complete!\nReturn and tap the egg to hatch." : $"Incubating\n{FormatTime(left)} remaining")
            : $"Incubate Egg {selectedEgg + 1}?\nTime: {FormatTime(system.incubationDurationSeconds)}";
        incubateButton.gameObject.SetActive(!egg.isIncubating);
        finishWithGemsButton.gameObject.SetActive(egg.isIncubating && left > 0f);
        gemCostText.text = egg.isIncubating && left > 0f ? $"FINISH NOW  •  {system.GetFinishGemCost(selectedEgg)} GEM" : string.Empty;
    }

    private static string FormatTime(float seconds)
    {
        int value = Mathf.CeilToInt(seconds);
        return $"{value / 60:00}:{value % 60:00}";
    }
}
