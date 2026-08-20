using Spine.Unity;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Member : MonoBehaviour
{
    public GameObject body;
    public GameObject armor;
    public GameObject weapon;
    public int id;
    public Team teamSlimes;
    public FormationManager formationManager;
    public Transform formation;

    [Header("Prefab Settings")]
    public GameObject slimePrefab;

    IEnumerator TeamSlimeSpawn()
    {
        yield return new WaitForSeconds(0.5f);

        if (teamSlimes == null || teamSlimes.team == null || formationManager == null) yield break;

        int myIndex = formationManager.teamMembers.IndexOf(this);
        if (myIndex < 0 || myIndex >= teamSlimes.team.Count) yield break;

        Slime s = teamSlimes.team[myIndex];
        if (s == null) yield break;

        id = s.id;

        if (s.body != null && (s.body.skill == null || s.body.skill.baseSkill == null) && s.body.baseTrait != null && s.body.baseTrait.skill != null)
        {
            s.body.skill = new SkillInstance(s.body.baseTrait.skill);
            s.body.skill.power = s.body.GetSkillPower();
        }
        if (s.armor != null && (s.armor.skill == null || s.armor.skill.baseSkill == null) && s.armor.baseTrait != null && s.armor.baseTrait.skill != null)
        {
            s.armor.skill = new SkillInstance(s.armor.baseTrait.skill);
            s.armor.skill.power = s.armor.GetSkillPower();
        }
        if (s.weapon != null && (s.weapon.skill == null || s.weapon.skill.baseSkill == null) && s.weapon.baseTrait != null && s.weapon.baseTrait.skill != null)
        {
            s.weapon.skill = new SkillInstance(s.weapon.baseTrait.skill);
            s.weapon.skill.power = s.weapon.GetSkillPower();
        }

        GameObject slimeGO = Instantiate(slimePrefab, this.transform);
        slimeGO.name = $"TeamSlime_{s.id}";
        slimeGO.transform.position = transform.position;
        slimeGO.transform.localScale = Vector3.one * 1.3f;

        var skeletonGraphic = slimeGO.GetComponentInChildren<SkeletonGraphic>();

        var dragHandler = slimeGO.GetComponent<SlimeDragHandler>();

        if (s?.body?.hasAnimation == true)
        {
            skeletonGraphic.skeletonDataAsset = s.body.animationAsset;
            skeletonGraphic.allowMultipleCanvasRenderers = true;
            skeletonGraphic.enableSeparatorSlots = true;

            skeletonGraphic.Initialize(true);

            // Set animation
            if (!string.IsNullOrEmpty(s.body.animationName))
            {
                skeletonGraphic.AnimationState.SetAnimation(0, s.body.animationName, true);
                skeletonGraphic.timeScale = 2;
            }
        }
        var armorGO = slimeGO.GetComponent<SlimeDragHandler>().armor;
        var armorRenderer = armorGO.GetComponent<Image>();
        var weaponGO = slimeGO.GetComponent<SlimeDragHandler>().weapon;
        var weaponRenderer = weaponGO.GetComponent<Image>();

        armorRenderer.sprite = (s != null ? s.armor?.sprite : null) ?? formationManager.CreateDefaultSlimeSprite();
        weaponRenderer.sprite = (s != null ? s.weapon?.sprite : null) ?? formationManager.CreateDefaultSlimeSprite();
        var stat = slimeGO.GetComponent<SlimeStats>();
        stat.slimeName = !string.IsNullOrEmpty(s.slimeName) ? s.slimeName : $"Slime_{s.id}";
        stat.HP = s.totalHP;
        stat.Attack = s.totalAttack;
        stat.MagicAttack = s.totalMagicAttack;
        stat.Defense = s.totalDefense;
        stat.Speed = s.totalSpeed;
        stat.CritRate = s.totalCritRate;
        stat.CritDMG = s.totalCritDMG;
        stat.isEnemy = false;
        stat.enemyRarity = s.GetHighestRarity();
        stat.id = s.id;
        stat.bodySkill = s.body?.skill;
        stat.weaponSkill = s.weapon?.skill;
        stat.weaponUltimateSkill = s.weapon?.ultimateSkill;
        if (stat.weaponUltimateSkill == null && s.weapon != null && s.weapon.Rarity != Rarity.Common && s.weapon.Rarity != Rarity.Uncommon)
        {
            if (SlimeGen.Instance != null && s.weapon.skill?.baseSkill != null)
            {
                var ultSO = SlimeGen.Instance.GetMatchingUltimateWeaponSkill(s.weapon.skill.baseSkill);
                if (ultSO != null)
                {
                    s.weapon.ultimateSkill = new SkillInstance(ultSO);
                    stat.weaponUltimateSkill = s.weapon.ultimateSkill;
                }
            }
        }
        stat.armorSkill = s.armor?.skill;
        formationManager.slimeFormation.Add(slimeGO);
    }

    private void Start()
    {
        StartCoroutine(TeamSlimeSpawn());
    }
}
