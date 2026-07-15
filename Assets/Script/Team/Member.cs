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
        foreach (Slime s in teamSlimes.team)
        {
            if (s.id == id)
            {
                // Tự động sửa/khôi phục SkillInstance từ TraitSO gốc nếu dữ liệu đã lưu bị null
                if (s.body != null && (s.body.skill == null || s.body.skill.baseSkill == null) && s.body.baseTrait != null && s.body.baseTrait.skill != null)
                {
                    s.body.skill = new SkillInstance(s.body.baseTrait.skill);
                    s.body.skill.power = s.body.GetRarityMultiplier(s.body.Rarity) * 1.5f;
                }
                if (s.armor != null && (s.armor.skill == null || s.armor.skill.baseSkill == null) && s.armor.baseTrait != null && s.armor.baseTrait.skill != null)
                {
                    s.armor.skill = new SkillInstance(s.armor.baseTrait.skill);
                    s.armor.skill.power = s.armor.GetRarityMultiplier(s.armor.Rarity) * 1.5f;
                }
                if (s.weapon != null && (s.weapon.skill == null || s.weapon.skill.baseSkill == null) && s.weapon.baseTrait != null && s.weapon.baseTrait.skill != null)
                {
                    s.weapon.skill = new SkillInstance(s.weapon.baseTrait.skill);
                    s.weapon.skill.power = s.weapon.GetRarityMultiplier(s.weapon.Rarity) * 1.5f;
                }

                GameObject slimeGO = Instantiate(slimePrefab, this.transform);
                slimeGO.name = $"TeamSlime_{s.id}";
                slimeGO.transform.position = transform.position;
                slimeGO.transform.localScale = Vector3.one * 1.3f;

                // Thêm SlimeAnimationController để quản lý body
                var skeletonGraphic = slimeGO.GetComponentInChildren<SkeletonGraphic>();

                // Thêm SlimeDragHandler để hỗ trợ drag and drop
                var dragHandler = slimeGO.GetComponent<SlimeDragHandler>();

                if (s?.body?.hasAnimation == true)
                {
                    skeletonGraphic.skeletonDataAsset = s.body.animationAsset;
                    skeletonGraphic.allowMultipleCanvasRenderers = true;
                    skeletonGraphic.enableSeparatorSlots = true;

                    // Khởi tạo lại Skeleton
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

                // Gán sprite cho armor và weapon
                armorRenderer.sprite = (s != null ? s.armor?.sprite : null) ?? formationManager.CreateDefaultSlimeSprite();
                weaponRenderer.sprite = (s != null ? s.weapon?.sprite : null) ?? formationManager.CreateDefaultSlimeSprite();
                var stat = slimeGO.GetComponent<SlimeStats>();
                stat.HP = s.totalHP;
                stat.Attack = s.totalAttack;
                stat.MagicAttack = s.totalMagicAttack;
                stat.Defense = s.totalDefense;
                stat.Speed = s.totalSpeed;
                stat.CritRate = s.totalCritRate;
                stat.CritDMG = s.totalCritDMG;
                stat.isEnemy = false;
                stat.id = s.id;
                stat.bodySkill = s.body?.skill;
                stat.weaponSkill = s.weapon?.skill;
                stat.armorSkill = s.armor?.skill;
                formationManager.slimeFormation.Add(slimeGO);
            }
        }

    }
    private void Start()
    {
        StartCoroutine(TeamSlimeSpawn());
    }
}