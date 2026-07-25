using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Thành tựu — chạy theo AchievementCatalog (định nghĩa bằng code) và chấm điểm
/// thật từ PlayerStatsManager. Mở khóa → thưởng GEM → lưu. Tái dùng prefab UI cũ.
/// </summary>
public class ArchievementManager : MonoBehaviour
{
    // ── Giữ lại field cũ để không vỡ tham chiếu trong scene ──
    public List<ArchievementPre> listArchievement; // (không còn dùng cho logic — giữ cho scene)
    public GameObject ArchievementPrefab;
    public GameObject visualprefab;
    public CanvasGroup CanvasGroup;
    public SlimeWorldManager slimeWorldManager;

    public Sprite unlockSprite;
    public Sprite coin;
    public Sprite gem;

    [Header("Sorting")]
    public int sortingOrder = 9999;

    [Header("Options")]
    [Tooltip("Nếu bật, mỗi lần nhấn Play tất cả thành tựu sẽ được reset (xóa PlayerPrefs).")]
    public bool resetAchievementsOnPlay = false;

    public Button archivementButton;
    public Button closeButton;

    public static ArchievementManager Instance { get; private set; }

    private const string PrefKeyPrefix = "ACH_";

    private class Row
    {
        public AchievementDef def;
        public GameObject go;
        public bool unlocked;
    }

    private readonly List<Row> rows = new List<Row>();
    private bool _built;
    private bool _evaluating;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()  { PlayerStatsManager.OnStatsChanged += HandleStatsChanged; }
    private void OnDisable() { PlayerStatsManager.OnStatsChanged -= HandleStatsChanged; }

    void Start()
    {
        if (archivementButton != null) archivementButton.onClick.AddListener(hideUI);
        if (closeButton != null) closeButton.onClick.AddListener(hideUI);

        if (CanvasGroup != null)
        {
            bool visible = CanvasGroup.alpha > 0.99f;
            CanvasGroup.blocksRaycasts = visible;
            CanvasGroup.interactable = visible;
        }

        EnsureTopSorting();

        if (resetAchievementsOnPlay) ClearAllPrefs();

        BuildRows();
        EvaluateAll();
    }

    // ── Dựng UI từ catalog ──────────────────────────────────────────────
    private void BuildRows()
    {
        if (_built || ArchievementPrefab == null) return;

        var parent = GameObject.Find("general");

        foreach (var def in AchievementCatalog.All)
        {
            GameObject go = Instantiate(ArchievementPrefab);
            go.name = PrefKeyPrefix + def.Id;

            SetChildText(go, 0, def.Title);
            SetChildText(go, 1, def.Description);
            SetChildText(go, 4, $"+{def.GemReward} Gem");
            SetIconSprite(go, gem);

            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
                go.transform.SetAsLastSibling();
            }
            go.transform.localScale = Vector3.one;

            var row = new Row
            {
                def = def,
                go = go,
                unlocked = PlayerPrefs.GetInt(PrefKeyPrefix + def.Id, 0) == 1
            };
            rows.Add(row);
        }
        _built = true;
    }

    // ── Chấm điểm & mở khóa ─────────────────────────────────────────────
    private void HandleStatsChanged() => EvaluateAll();

    public void EvaluateAll()
    {
        if (!_built || _evaluating) return;
        _evaluating = true;
        bool anyNew = false;

        try
        {
            bool changedInPass;
            do
            {
                changedInPass = false;
                foreach (var row in rows)
                {
                    long cur = Current(row.def);
                    if (!row.unlocked && cur >= row.def.Target)
                    {
                        Unlock(row);
                        anyNew = true;
                        changedInPass = true; // gem thưởng có thể mở khóa bậc khác → quét lại
                    }
                    UpdateRowVisual(row, cur);
                }
            } while (changedInPass);
        }
        finally
        {
            _evaluating = false;
        }

        if (anyNew)
        {
            PlayerPrefs.Save();
            SaveAndLoadSystem.Instance?.Save();
        }
    }

    private void Unlock(Row row)
    {
        row.unlocked = true;
        PlayerPrefs.SetInt(PrefKeyPrefix + row.def.Id, 1);

        if (row.def.GemReward > 0 && CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, row.def.GemReward);

        Debug.Log($"[Achievement] Mở khóa '{row.def.Title}' (+{row.def.GemReward} Gem)");
    }

    private long Current(AchievementDef def)
    {
        var st = PlayerStatsManager.Instance;
        if (st == null) return 0;

        switch (def.Metric)
        {
            case AchievementMetric.TotalBred:      return st.TotalSlimesBred;
            case AchievementMetric.DistinctTraits: return st.DistinctTraitsCount;
            case AchievementMetric.CoinsEarned:    return st.TotalCoinsEarned;
            case AchievementMetric.GemsEarned:     return st.TotalGemsEarned;
            case AchievementMetric.FarmWins:       return st.TotalFarmWins;
            case AchievementMetric.Captures:       return st.TotalCaptures;
            case AchievementMetric.RarityObtained: return st.GetRarityObtained(def.RarityTarget);
            case AchievementMetric.TowerFloor:     return st.HighestTowerFloor;
            case AchievementMetric.BattleWins:     return st.TotalBattleWins;
            case AchievementMetric.Mutations:      return st.TotalMutations;
            case AchievementMetric.OwnedSlimes:
                return BreedingManager.Instance != null ? BreedingManager.Instance.GetAllSlimes().Count : 0;
            default: return 0;
        }
    }

    private void UpdateRowVisual(Row row, long cur)
    {
        if (row.go == null) return;

        var bg = row.go.GetComponent<Image>();
        var icon = GetChildImage(row.go, 2);

        if (row.unlocked)
        {
            if (bg != null) bg.color = Color.yellow;
            if (icon != null) icon.color = Color.white;
            SetChildText(row.go, 1, $"{row.def.Description}  (Đã đạt)");
        }
        else
        {
            if (bg != null) bg.color = Color.white;
            if (icon != null) icon.color = new Color(1, 1, 1, 0.4f);
            long shown = cur > row.def.Target ? row.def.Target : cur;
            SetChildText(row.go, 1, $"{row.def.Description}  ({shown}/{row.def.Target})");
        }

        // Đã mở khóa → phủ panel đen mờ che lại.
        QuestUIEffects.SetDimmed(row.go, row.unlocked);
    }

    /// <summary>Nạp lại trạng thái mở khóa từ PlayerPrefs (gọi sau khi load save).</summary>
    public void ReloadUnlockStates()
    {
        if (!_built) return;
        foreach (var row in rows)
        {
            row.unlocked = PlayerPrefs.GetInt(PrefKeyPrefix + row.def.Id, 0) == 1;
        }
        EvaluateAll();
    }

    // ── Shim tương thích code cũ (BreedingManager/Quest/BuildingSlot gọi) ──
    public void GetArchivement(int dex) => EvaluateAll();

    // ── UI helpers ──────────────────────────────────────────────────────
    private static void SetChildText(GameObject go, int childIndex, string text)
    {
        if (go == null || childIndex >= go.transform.childCount) return;
        var t = go.transform.GetChild(childIndex).GetComponentInChildren<Text>();
        if (t != null) t.text = text;
    }

    private static Image GetChildImage(GameObject go, int childIndex)
    {
        if (go == null || childIndex >= go.transform.childCount) return null;
        return go.transform.GetChild(childIndex).GetComponentInChildren<Image>();
    }

    private void SetIconSprite(GameObject go, Sprite sprite)
    {
        if (sprite == null) return;
        var img = GetChildImage(go, 2);
        if (img != null) img.sprite = sprite;
    }

    private void ClearAllPrefs()
    {
        foreach (var def in AchievementCatalog.All)
            PlayerPrefs.DeleteKey(PrefKeyPrefix + def.Id);
        PlayerPrefs.Save();
    }

    [ContextMenu("Reset All Achievements")]
    public void ResetAllAchievements()
    {
        ClearAllPrefs();
        foreach (var row in rows)
        {
            row.unlocked = false;
            UpdateRowVisual(row, Current(row.def));
        }
    }

    private void EnsureTopSorting()
    {
        var target = CanvasGroup != null ? CanvasGroup.gameObject : this.gameObject;
        var canvas = target.GetComponent<Canvas>();
        if (canvas == null) canvas = target.AddComponent<Canvas>();

        if (target.GetComponent<GraphicRaycaster>() == null)
            target.AddComponent<GraphicRaycaster>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        var parentCanvas = target.transform.parent != null
            ? target.transform.parent.GetComponentInParent<Canvas>() : null;
        if (parentCanvas != null) canvas.sortingLayerID = parentCanvas.sortingLayerID;

        target.transform.SetAsLastSibling();

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }
    }

    public void hideUI()
    {
        if (CanvasGroup == null) return;
        if (CanvasGroup.alpha == 0)
        {
            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;
            EvaluateAll();
        }
        else
        {
            CanvasGroup.alpha = 0;
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
        }
    }
}
