using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillUI : MonoBehaviour
{
    public SlimeStats slime;
    public Image bodySkill;
    public Image armorSkill;
    public Image weaponSkill;
    public Image fullSetSkill;
    public Sprite border;

    private SlimeBattleStats _cachedBattleStats;
    private Button _bodyBtn, _armorBtn, _weaponBtn;
    private Text _bodyText, _armorText, _weaponText;

    private int _lastEnergy = -1;
    private int _lastBattlePoints = -1;
    private SkillInstance _lastWeaponSkill;

    private void OnEnable()
    {
        if (BattleSystemManager.Instance != null)
            BattleSystemManager.Instance.OnBattlePointsChanged += HandleBPChanged;
    }

    private void OnDisable()
    {
        if (BattleSystemManager.Instance != null)
            BattleSystemManager.Instance.OnBattlePointsChanged -= HandleBPChanged;

        if (_rainbowCoroutine != null)
        {
            StopCoroutine(_rainbowCoroutine);
            _rainbowCoroutine = null;
        }
        _isUltimateActive = false;
        if (weaponSkill != null) weaponSkill.color = Color.white;
        if (_weaponText != null) _weaponText.color = Color.white;
    }

    private void HandleBPChanged(int bp)
    {
        ForceRefresh();
    }

    private void Start()
    {
        CacheComponents();
        EnsureTooltipTriggers();
        if (fullSetSkill != null) fullSetSkill.gameObject.SetActive(false);
        ForceRefresh();
    }

    private void CacheComponents()
    {
        if (slime != null)
            _cachedBattleStats = slime.GetComponent<SlimeBattleStats>();

        if (bodySkill != null)
        {
            _bodyBtn = bodySkill.GetComponent<Button>() ?? bodySkill.GetComponentInParent<Button>();
            var go = _bodyBtn != null ? _bodyBtn.gameObject : bodySkill.gameObject;
            _bodyText = go.GetComponentInChildren<Text>();
        }
        if (armorSkill != null)
        {
            _armorBtn = armorSkill.GetComponent<Button>() ?? armorSkill.GetComponentInParent<Button>();
            var go = _armorBtn != null ? _armorBtn.gameObject : armorSkill.gameObject;
            _armorText = go.GetComponentInChildren<Text>();
        }
        if (weaponSkill != null)
        {
            _weaponBtn = weaponSkill.GetComponent<Button>() ?? weaponSkill.GetComponentInParent<Button>();
            var go = _weaponBtn != null ? _weaponBtn.gameObject : weaponSkill.gameObject;
            _weaponText = go.GetComponentInChildren<Text>();
        }
    }

    [Header("Ultimate Rainbow Effect")]
    private Coroutine _rainbowCoroutine;
    private bool _isUltimateActive = false;

    private void UpdateUltimateVisualState(bool isReady)
    {
        if (weaponSkill == null) return;

        if (isReady && !_isUltimateActive)
        {
            _isUltimateActive = true;
            if (_rainbowCoroutine != null) StopCoroutine(_rainbowCoroutine);
            _rainbowCoroutine = StartCoroutine(RainbowEffectRoutine());
        }
        else if (!isReady && _isUltimateActive)
        {
            _isUltimateActive = false;
            if (_rainbowCoroutine != null)
            {
                StopCoroutine(_rainbowCoroutine);
                _rainbowCoroutine = null;
            }
            if (weaponSkill != null) weaponSkill.color = Color.white;
            if (_weaponText != null) _weaponText.color = Color.white;
        }
    }

    private System.Collections.IEnumerator RainbowEffectRoutine()
    {
        while (_isUltimateActive)
        {
            float hue = Mathf.Repeat(Time.time * 0.8f, 1.0f);
            Color rainbowColor = Color.HSVToRGB(hue, 0.75f, 1.0f);

            if (weaponSkill != null)
            {
                weaponSkill.color = rainbowColor;
            }
            if (_weaponText != null)
            {
                _weaponText.color = rainbowColor;
            }

            yield return null;
        }

        if (weaponSkill != null) weaponSkill.color = Color.white;
        if (_weaponText != null) _weaponText.color = Color.white;
    }

    public void OnStatsChanged()
    {
        if (slime == null) return;

        int newEnergy = _cachedBattleStats != null ? _cachedBattleStats.CurrentEnergy : 0;
        int newBP = BattleSystemManager.Instance != null ? BattleSystemManager.Instance.TeamBattlePoints : 0;

        bool isUltReady = false;
        SkillInstance weaponSkillToDisplay = slime.weaponSkill;

        if (_cachedBattleStats != null)
        {
            if (slime.weaponUltimateSkill == null && slime.weaponSkill?.baseSkill != null && SlimeGen.Instance != null)
            {
                var ultSO = SlimeGen.Instance.GetMatchingUltimateWeaponSkill(slime.weaponSkill.baseSkill);
                if (ultSO != null)
                {
                    slime.weaponUltimateSkill = new SkillInstance(ultSO);
                }
            }

            if (slime.weaponUltimateSkill != null && slime.weaponUltimateSkill.baseSkill != null)
            {
                int energyCost = slime.weaponUltimateSkill.baseSkill.energyCost > 0 ? slime.weaponUltimateSkill.baseSkill.energyCost : 100;
                if (_cachedBattleStats.CurrentEnergy >= energyCost)
                {
                    weaponSkillToDisplay = slime.weaponUltimateSkill;
                    isUltReady = true;
                }
            }
        }

        UpdateUltimateVisualState(isUltReady);

        bool needsRefresh = newEnergy != _lastEnergy || newBP != _lastBattlePoints || weaponSkillToDisplay != _lastWeaponSkill;
        if (!needsRefresh) return;

        _lastEnergy = newEnergy;
        _lastBattlePoints = newBP;
        _lastWeaponSkill = weaponSkillToDisplay;

        UpdateSkillUI(bodySkill, _bodyBtn, _bodyText, slime.bodySkill, _cachedBattleStats);
        UpdateSkillUI(armorSkill, _armorBtn, _armorText, slime.armorSkill, _cachedBattleStats);
        UpdateSkillUI(weaponSkill, _weaponBtn, _weaponText, weaponSkillToDisplay, _cachedBattleStats);
    }

    public void ForceRefresh()
    {
        _lastEnergy = -1;
        _lastBattlePoints = -1;
        _lastWeaponSkill = null;
        OnStatsChanged();
    }


    private void UpdateSkillUI(Image skillImage, Button btn, Text textComp, SkillInstance skill, SlimeBattleStats battleStats)
    {
        if (skillImage == null) return;

        GameObject targetGO = (btn != null) ? btn.gameObject : skillImage.gameObject;

        skillImage.raycastTarget = true;
        if (btn != null && btn.image != null) btn.image.raycastTarget = true;

        if (skill == null || skill.baseSkill == null)
        {
            skillImage.sprite = border;
            skillImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            if (btn != null) btn.interactable = false;
            if (textComp != null)
            {
                textComp.text = "Empty";
                textComp.color = Color.gray;
            }
            var emptyTrigger = targetGO.GetComponent<SkillTooltipTrigger>();
            if (emptyTrigger != null) emptyTrigger.Setup(null, null);
            return;
        }

        skillImage.sprite = skill.baseSkill.icon != null ? skill.baseSkill.icon : border;

        bool isInteractable = false;
        string skillInfo = skill.baseSkill.skillName;
        Color textColor = Color.white;

        switch (skill.baseSkill.type)
        {
            case SkillType.Passive:
                isInteractable = false;
                skillInfo += "\n(Passive)";
                textColor = new Color(0.2f, 0.8f, 1f);
                skillImage.color = Color.white;
                break;

            case SkillType.BasicAttack:
                isInteractable = true;
                if (skill.baseSkill.battlePointGain > 0)
                    skillInfo += $"\n(+{skill.baseSkill.battlePointGain} BP)";
                textColor = Color.green;
                skillImage.color = Color.white;
                break;

            case SkillType.Active:
                if (BattleSystemManager.Instance != null && battleStats != null)
                    isInteractable = BattleSystemManager.Instance.TeamBattlePoints >= skill.baseSkill.battlePointCost;
                if (skill.baseSkill.battlePointCost > 0)
                    skillInfo += $"\n(-{skill.baseSkill.battlePointCost} BP)";
                textColor = isInteractable ? Color.white : Color.red;
                skillImage.color = isInteractable ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1.0f);
                break;

            case SkillType.Ultimate:
                if (battleStats != null)
                    isInteractable = battleStats.CurrentEnergy >= skill.baseSkill.energyCost;
                if (skill.baseSkill.energyCost > 0)
                    skillInfo += $"\n(-{skill.baseSkill.energyCost} NL)";
                textColor = isInteractable ? Color.yellow : Color.red;
                skillImage.color = isInteractable ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1.0f);
                break;

            default:
                isInteractable = true;
                skillImage.color = Color.white;
                break;
        }

        if (btn != null) btn.interactable = isInteractable;
        if (textComp != null)
        {
            textComp.text = skillInfo;
            textComp.color = textColor;
        }

        var trigger = targetGO.GetComponent<SkillTooltipTrigger>();
        if (trigger != null) trigger.Setup(skill, battleStats);
    }

    private void EnsureTooltipTriggers()
    {
        if (bodySkill != null) AttachTooltipTriggerOnce(bodySkill.gameObject, null, null);
        if (armorSkill != null) AttachTooltipTriggerOnce(armorSkill.gameObject, null, null);
        if (weaponSkill != null) AttachTooltipTriggerOnce(weaponSkill.gameObject, null, null);
    }

    private void AttachTooltipTriggerOnce(GameObject go, SkillInstance skill, SlimeBattleStats battleStats)
    {
        if (go == null) return;

        var btn = go.GetComponent<Button>();
        if (btn != null && btn.image != null) btn.image.raycastTarget = true;
        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        var holdTrigger = go.GetComponent<SkillTooltipTrigger>();
        if (holdTrigger == null) holdTrigger = go.AddComponent<SkillTooltipTrigger>();
        holdTrigger.Setup(skill, battleStats);

        var eventTrigger = go.GetComponent<EventTrigger>();
        if (eventTrigger != null) return;

        eventTrigger = go.AddComponent<EventTrigger>();

        var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener((data) => { holdTrigger.OnPointerDown((PointerEventData)data); });
        eventTrigger.triggers.Add(downEntry);

        var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback.AddListener((data) => { holdTrigger.OnPointerUp((PointerEventData)data); });
        eventTrigger.triggers.Add(upEntry);
    }
}
