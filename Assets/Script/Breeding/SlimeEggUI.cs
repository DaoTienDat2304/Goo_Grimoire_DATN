using System.Collections;
using System.Collections.Generic;
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
    [Header("Dynamic egg list")]
    public ScrollRect eggScrollRect;
    public RectTransform eggContent;
    public Button eggSlotPrefab;
    [Header("Legacy slots (automatically converted at runtime)")]
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
    private readonly List<Button> visibleEggButtons = new List<Button>();
    private readonly List<TMP_Text> visibleEggTexts = new List<TMP_Text>();

    private void Start()
    {
        EnsureDynamicEggList();
        eggHudButton.onClick.AddListener(OpenInventory);
        incubateButton.onClick.AddListener(ConfirmIncubation);
        finishWithGemsButton.onClick.AddListener(FinishWithGems);
        eggInventoryPanel.SetActive(false);
        incubationConfirmPanel.SetActive(false);
        hatchResultPanel.SetActive(false);
        FindSystem();
        Refresh();
    }

    private void OnDestroy()
    {
        if (system == null) return;
        system.EggsChanged -= Refresh;
        system.WorldEggCollected -= PlayCollectionAnimation;
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
            system.WorldEggCollected -= PlayCollectionAnimation;
            system.WorldEggCollected += PlayCollectionAnimation;
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
        if (visibleEggButtons.Count != count)
            RebuildEggItems(count);
        for (int i = 0; i < visibleEggButtons.Count; i++)
        {
            visibleEggButtons[i].interactable = true;
            visibleEggTexts[i].text = GetEggLabel(i);
        }
        if (selectedEgg >= count) selectedEgg = -1;
        RefreshIncubationPanel();
    }

    private void EnsureDynamicEggList()
    {
        if (eggScrollRect != null && eggContent != null && eggSlotPrefab != null)
        {
            eggSlotPrefab.gameObject.SetActive(false);
            return;
        }

        Transform existing = eggInventoryPanel.transform.Find("EggScrollView");
        if (existing != null)
        {
            eggScrollRect = existing.GetComponent<ScrollRect>();
            eggContent = eggScrollRect != null ? eggScrollRect.content : null;
        }

        if (eggScrollRect == null || eggContent == null)
        {
            GameObject scrollObject = CreateUIObject("EggScrollView", eggInventoryPanel.transform);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            SetRect(scrollRectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -15f), new Vector2(650f, 240f));
            Image scrollBackground = scrollObject.AddComponent<Image>();
            scrollBackground.color = new Color32(225, 207, 177, 120);
            eggScrollRect = scrollObject.AddComponent<ScrollRect>();
            eggScrollRect.horizontal = true;
            eggScrollRect.vertical = false;
            eggScrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = CreateUIObject("Viewport", scrollObject.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            SetRect(viewportRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateUIObject("Content", viewport.transform);
            eggContent = content.GetComponent<RectTransform>();
            eggContent.anchorMin = new Vector2(0f, 0f);
            eggContent.anchorMax = new Vector2(0f, 1f);
            eggContent.pivot = new Vector2(0f, 0.5f);
            eggContent.anchoredPosition = Vector2.zero;
            eggContent.sizeDelta = Vector2.zero;
            HorizontalLayoutGroup layout = content.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 5, 5);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            eggScrollRect.viewport = viewportRect;
            eggScrollRect.content = eggContent;
        }

        if (eggSlotPrefab == null && eggSlotButtons != null)
        {
            for (int i = 0; i < eggSlotButtons.Length; i++)
            {
                if (eggSlotButtons[i] == null) continue;
                if (eggSlotPrefab == null)
                {
                    eggSlotPrefab = eggSlotButtons[i];
                    eggSlotPrefab.transform.SetParent(eggContent, false);
                    RectTransform rect = eggSlotPrefab.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(190f, 220f);
                    eggSlotPrefab.gameObject.SetActive(false);
                }
                else
                {
                    Destroy(eggSlotButtons[i].gameObject);
                }
            }
        }
    }

    private void RebuildEggItems(int count)
    {
        for (int i = 0; i < visibleEggButtons.Count; i++)
            if (visibleEggButtons[i] != null) Destroy(visibleEggButtons[i].gameObject);
        visibleEggButtons.Clear();
        visibleEggTexts.Clear();

        if (eggSlotPrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            int index = i;
            Button item = Instantiate(eggSlotPrefab, eggContent);
            item.name = $"EggItem_{i + 1}";
            item.gameObject.SetActive(true);
            item.onClick.RemoveAllListeners();
            item.onClick.AddListener(() => SelectEgg(index));
            TMP_Text text = item.transform.Find("Status")?.GetComponent<TMP_Text>();
            if (text == null) text = item.GetComponentInChildren<TMP_Text>(true);
            visibleEggButtons.Add(item);
            visibleEggTexts.Add(text);
        }
        Canvas.ForceUpdateCanvases();
        if (eggScrollRect != null) eggScrollRect.horizontalNormalizedPosition = 0f;
    }

    private void PlayCollectionAnimation(Vector3 worldPosition, Sprite icon)
    {
        StartCoroutine(CollectionAnimation(worldPosition, icon));
    }

    private IEnumerator CollectionAnimation(Vector3 worldPosition, Sprite icon)
    {
        RectTransform root = transform as RectTransform;
        RectTransform target = eggHudButton != null ? eggHudButton.transform as RectTransform : null;
        if (root == null || target == null) yield break;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Camera worldCamera = Camera.main;
        Vector2 startScreen = worldCamera != null ? worldCamera.WorldToScreenPoint(worldPosition) : (Vector2)Screen.safeArea.center;
        Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, startScreen, uiCamera, out Vector2 start);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, endScreen, uiCamera, out Vector2 end);

        GameObject proxy = CreateUIObject("CollectedEggAnimation", transform);
        proxy.transform.SetAsLastSibling();
        RectTransform proxyRect = proxy.GetComponent<RectTransform>();
        proxyRect.anchorMin = proxyRect.anchorMax = Vector2.one * 0.5f;
        proxyRect.sizeDelta = new Vector2(64f, 80f);
        proxyRect.anchoredPosition = start;
        Image image = proxy.AddComponent<Image>();
        image.sprite = icon;
        image.color = icon != null ? Color.white : new Color32(202, 165, 229, 255);
        image.preserveAspect = true;
        image.raycastTarget = false;

        const float duration = 0.65f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            proxyRect.anchoredPosition = Vector2.LerpUnclamped(start, end, eased) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 70f;
            proxyRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.12f, eased);
            yield return null;
        }
        Destroy(proxy);
        StartCoroutine(PulseHudButton());
    }

    private IEnumerator PulseHudButton()
    {
        if (eggHudButton == null) yield break;
        Transform target = eggHudButton.transform;
        Vector3 baseScale = target.localScale;
        float elapsed = 0f;
        while (elapsed < 0.18f)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(Mathf.Clamp01(elapsed / 0.18f) * Mathf.PI) * 0.16f;
            target.localScale = baseScale * pulse;
            yield return null;
        }
        target.localScale = baseScale;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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
