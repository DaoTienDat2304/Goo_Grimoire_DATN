#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SkillAssetGenerator
{
    const string EFFECT_DIR = "Assets/SkillDB/EffectType";
    const string WEAPON_DIR = "Assets/SkillDB/WeaponSkill";
    const string BODY_DIR = "Assets/SkillDB/BodySkill";
    const string HAT_DIR = "Assets/SkillDB/HatSkill";

    [MenuItem("Tools/Skills/Generate All Skill Assets (Full Spec)")]
    public static void GenerateAll()
    {
        EnsureDir(EFFECT_DIR); EnsureDir(WEAPON_DIR); EnsureDir(BODY_DIR); EnsureDir(HAT_DIR);

        // ── 1. BASE EFFECTS ───────────────────────────────────────────────────
        var d1 = E("Dmg_1", EffectType.Damage, TargetSide.Enemies, AoEShape.Single);
        var dA = E("Dmg_A", EffectType.Damage, TargetSide.Enemies, AoEShape.FullSide);
        var h1 = E("Heal_1", EffectType.Heal, TargetSide.Allies, AoEShape.Single);
        var hA = E("Heal_A", EffectType.Heal, TargetSide.Allies, AoEShape.FullSide, AnchorType.Self);
        var hS = E("Heal_S", EffectType.Heal, TargetSide.Allies, AoEShape.Single, AnchorType.Self);

        var bAtkS = E("B_Atk_S", EffectType.Buff, TargetSide.Allies, AoEShape.Single, AnchorType.Self, BuffStat.Attack);
        var bAtkA = E("B_Atk_A", EffectType.Buff, TargetSide.Allies, AoEShape.FullSide, AnchorType.Self, BuffStat.Attack);
        var bDefS = E("B_Def_S", EffectType.Buff, TargetSide.Allies, AoEShape.Single, AnchorType.Self, BuffStat.Defense);
        var bDefA = E("B_Def_A", EffectType.Buff, TargetSide.Allies, AoEShape.FullSide, AnchorType.Self, BuffStat.Defense);
        var bSpdS = E("B_Spd_S", EffectType.Buff, TargetSide.Allies, AoEShape.Single, AnchorType.Self, BuffStat.Speed);
        var bSpdA = E("B_Spd_A", EffectType.Buff, TargetSide.Allies, AoEShape.FullSide, AnchorType.Self, BuffStat.Speed);

        var dbAtk1 = E("Db_Atk_1", EffectType.Debuff, TargetSide.Enemies, AoEShape.Single, AnchorType.AttackTarget, BuffStat.Attack);
        var dbAtkA = E("Db_Atk_A", EffectType.Debuff, TargetSide.Enemies, AoEShape.FullSide, AnchorType.AttackTarget, BuffStat.Attack);
        var dbDef1 = E("Db_Def_1", EffectType.Debuff, TargetSide.Enemies, AoEShape.Single, AnchorType.AttackTarget, BuffStat.Defense);
        var dbDefA = E("Db_Def_A", EffectType.Debuff, TargetSide.Enemies, AoEShape.FullSide, AnchorType.AttackTarget, BuffStat.Defense);
        var dbSpd1 = E("Db_Spd_1", EffectType.Debuff, TargetSide.Enemies, AoEShape.Single, AnchorType.AttackTarget, BuffStat.Speed);
        var dbSpdA = E("Db_Spd_A", EffectType.Debuff, TargetSide.Enemies, AoEShape.FullSide, AnchorType.AttackTarget, BuffStat.Speed);

        var stun1 = E("Stun_1", EffectType.Stun, TargetSide.Enemies, AoEShape.Single);
        var stunA = E("Stun_A", EffectType.Stun, TargetSide.Enemies, AoEShape.FullSide);
        var psn1 = E("Psn_1", EffectType.Poison, TargetSide.Enemies, AoEShape.Single);
        var bld1 = E("Bld_1", EffectType.Bleed, TargetSide.Enemies, AoEShape.Single);
        var bldA = E("Bld_A", EffectType.Bleed, TargetSide.Enemies, AoEShape.FullSide);

        var shld1 = E("Shld_1", EffectType.Shield, TargetSide.Allies, AoEShape.Single, AnchorType.Self);
        var shldA = E("Shld_A", EffectType.Shield, TargetSide.Allies, AoEShape.FullSide, AnchorType.Self);
        var clns1 = E("Clns_1", EffectType.Cleanse, TargetSide.Allies, AoEShape.Single);
        var clnsA = E("Clns_A", EffectType.Cleanse, TargetSide.Allies, AoEShape.FullSide, AnchorType.Self);
        var dspl1 = E("Dspl_1", EffectType.Dispel, TargetSide.Enemies, AoEShape.Single);
        var dsplA = E("Dspl_A", EffectType.Dispel, TargetSide.Enemies, AoEShape.FullSide);
        var avAd1 = E("AV_Adv_1", EffectType.ActionValue, TargetSide.Allies, AoEShape.Single);
        var avAdA = E("AV_Adv_A", EffectType.ActionValue, TargetSide.Allies, AoEShape.FullSide, AnchorType.Self);
        var avAdS = E("AV_Adv_S", EffectType.ActionValue, TargetSide.Allies, AoEShape.Single, AnchorType.Self);
        var avRt1 = E("AV_Ret_1", EffectType.ActionValue, TargetSide.Enemies, AoEShape.Single);
        var avRtA = E("AV_Ret_A", EffectType.ActionValue, TargetSide.Enemies, AoEShape.FullSide);
        var enGnA = E("En_Gn_A", EffectType.Energy, TargetSide.Allies, AoEShape.FullSide, AnchorType.Self);
        var rvv1 = E("Rev_1", EffectType.Revive, TargetSide.Allies, AoEShape.Single);
        var rvvA = E("Rev_A", EffectType.Revive, TargetSide.Allies, AoEShape.FullSide, AnchorType.Self);

        int Act = (int)SkillType.Active; int Ult = (int)SkillType.Ultimate; int Psv = (int)SkillType.Passive;

        // ── 2. WEAPON SKILLS (Chiến kỹ & Tuyệt kỹ) ────────────────────────────
        // COMMON
        S(WEAPON_DIR, "W_Com_Pebble", Act, "Pebble Sling", "Pebble Toss · 1 ĐCK · 120% ATK + 75, đơn.", 1, 0, 0, 25, (d1, 1.2f, 75, 0, 100));
        S(WEAPON_DIR, "W_Com_Twin", Act, "Twin Stone", "Twin Pebble · 1 ĐCK · 100% ATK + 75, ×2 hit đơn (tổng 200% ATK + 150).", 1, 0, 0, 25, (d1, 2.0f, 150, 0, 100));
        S(WEAPON_DIR, "W_Com_Mud", Act, "Mud Bucket", "Mud Splash · 1 ĐCK · 105% ATK + 85, AoE.", 1, 0, 0, 25, (dA, 1.05f, 85, 0, 100));
        S(WEAPON_DIR, "W_Com_Slap", Act, "Slap Fin", "Slime Slap · 1 ĐCK · 130% ATK + 80, đơn; 10% −5 SPD 1 lượt.", 1, 0, 0, 25, (d1, 1.3f, 80, 0, 100), (dbSpd1, 5f, 0, 1, 10));
        S(WEAPON_DIR, "W_Com_Glue", Act, "Glue Shooter", "Sticky Shot · 1 ĐCK · 115% ATK + 90, đơn; 20% −5% ATK 2 lượt.", 1, 0, 0, 25, (d1, 1.15f, 90, 0, 100), (dbAtk1, 0.05f, 0, 2, 20));
        S(WEAPON_DIR, "W_Com_Bump", Act, "Bump Shell", "Reckless Bump · 1 ĐCK · 150% ATK + 75, đơn; bản thân +5% sát thương nhận lượt sau.", 1, 0, 0, 25, (d1, 1.5f, 75, 0, 100));

        // UNCOMMON
        S(WEAPON_DIR, "W_Unc_TwinFang", Act, "Twin Fang Dagger", "Twin Fang · 1 ĐCK · 155% ATK + 150, đơn.", 1, 0, 0, 25, (d1, 1.55f, 150, 0, 100));
        S(WEAPON_DIR, "W_Unc_Venom", Act, "Venom Sting", "Venom Nip · 1 ĐCK · 140% ATK + 135, đơn; 30% Độc (−3% HP tối đa/lượt, 2 lượt).", 1, 0, 0, 25, (d1, 1.4f, 135, 0, 100), (psn1, 0.03f, 0, 2, 30));
        S(WEAPON_DIR, "W_Unc_Gale", Act, "Gale Edge", "Gale Slash · 1 ĐCK · 125% ATK + 150, AoE.", 1, 0, 0, 25, (dA, 1.25f, 150, 0, 100));
        S(WEAPON_DIR, "W_Unc_Awl", Act, "Awl Pike", "Piercing Jab · 1 ĐCK · 150% ATK + 140, đơn; xuyên 10% giáp.", 1, 0, 0, 25, (d1, 1.5f, 140, 0, 100));
        S(WEAPON_DIR, "W_Unc_Rip", Act, "Rip Claw", "Rending Claw · 1 ĐCK · 145% ATK + 140, đơn; 25% Chảy Máu (−2% HP/lượt, 2 lượt).", 1, 0, 0, 25, (d1, 1.45f, 140, 0, 100), (bld1, 0.02f, 0, 2, 25));
        S(WEAPON_DIR, "W_Unc_Frenzy", Act, "Frenzy Fang", "Frenzy Bite · 1 ĐCK · 130% ATK + 150, ×2 hit (tổng 260% ATK + 300); mỗi hit hồi 3% thành HP.", 1, 0, 0, 25, (d1, 2.6f, 300, 0, 100), (hS, 0.06f, 0, 0, 100));

        // RARE
        S(WEAPON_DIR, "W_Rar_Mudfang_A", Act, "Mudfang Gauntlet", "Mud Punch · 1 ĐCK · 160% ATK + 300, đơn.", 1, 0, 0, 25, (d1, 1.6f, 300, 0, 100));
        S(WEAPON_DIR, "W_Rar_Mudfang_U", Ult, "Mudfang Gauntlet", "[Đơn] Titan Crash · 280% ATK + 700, đơn; xuyên 30% giáp.", 0, 0, 100, 0, (d1, 2.8f, 700, 0, 100));
        S(WEAPON_DIR, "W_Rar_Tide_A", Act, "Tidecaller Staff", "Water Splash · 1 ĐCK · 130% ATK + 300, AoE.", 1, 0, 0, 25, (dA, 1.3f, 300, 0, 100));
        S(WEAPON_DIR, "W_Rar_Tide_U", Ult, "Tidecaller Staff", "[AoE] Torrential Roar · 240% ATK + 650, AoE; −10 SPD toàn địch 2 lượt.", 0, 0, 100, 0, (dA, 2.4f, 650, 0, 100), (dbSpdA, 10f, 0, 2, 100));
        S(WEAPON_DIR, "W_Rar_Storm_A", Act, "Stormedge Blade", "Lightning Blade · 1 ĐCK · 175% ATK + 260, đơn; 20% Choáng 1 lượt.", 1, 0, 0, 25, (d1, 1.75f, 260, 0, 100), (stun1, 1f, 0, 1, 20));
        S(WEAPON_DIR, "W_Rar_Storm_U", Ult, "Stormedge Blade", "[Khống chế] Thunder Cage · 200% ATK + 550, đơn; 100% Choáng 2 lượt.", 0, 0, 100, 0, (d1, 2.0f, 550, 0, 100), (stun1, 1f, 0, 2, 100));
        S(WEAPON_DIR, "W_Rar_Blood_A", Act, "Bloodthirn Claw", "Blood Rend · 1 ĐCK · 165% ATK + 260, đơn; 35% Chảy Máu (−3% HP/lượt, 2 lượt).", 1, 0, 0, 25, (d1, 1.65f, 260, 0, 100), (bld1, 0.03f, 0, 2, 35));
        S(WEAPON_DIR, "W_Rar_Blood_U", Ult, "Bloodthirn Claw", "[Hồi máu] Sanguine Feast · 220% ATK + 600, đơn; hồi 40% sát thương thành HP toàn đội.", 0, 0, 100, 0, (d1, 2.2f, 600, 0, 100), (hA, 0.4f, 0, 0, 100));
        S(WEAPON_DIR, "W_Rar_Aegis_A", Act, "Aegis Core", "Spike Volley · 1 ĐCK · 135% ATK + 260, AoE; xuyên 15% giáp.", 1, 0, 0, 25, (dA, 1.35f, 260, 0, 100));
        S(WEAPON_DIR, "W_Rar_Aegis_U", Ult, "Aegis Core", "[Phòng thủ] Bulwark Surge · 120% ATK + 300, đơn; lá chắn 15% HP tối đa toàn đội 2 lượt.", 0, 0, 100, 0, (shldA, 0.15f, 300, 2, 100));
        S(WEAPON_DIR, "W_Rar_Rally_A", Act, "Rallyhorn Trident", "Piercing Wave · 1 ĐCK · 150% ATK + 280, đơn; xuyên 15% giáp.", 1, 0, 0, 25, (d1, 1.5f, 280, 0, 100));
        S(WEAPON_DIR, "W_Rar_Rally_U", Ult, "Rallyhorn Trident", "[Buff] War Anthem · +15% ATK & +10% Crit Rate toàn đội 3 lượt.", 0, 0, 100, 0, (bAtkA, 0.15f, 0, 3, 100));

        // SUPER RARE
        S(WEAPON_DIR, "W_SR_Dragon_A", Act, "Dragonslayer Greatsword", "Dragon Slayer Blade · 1 ĐCK · 175% ATK + 450, đơn; xuyên 20% giáp.", 1, 0, 0, 25, (d1, 1.75f, 450, 0, 100));
        S(WEAPON_DIR, "W_SR_Dragon_U", Ult, "Dragonslayer Greatsword", "[Đơn] Dragon's Demise · 330% ATK + 1000, đơn; xuyên 40% giáp; +25% nếu mục tiêu <50% HP.", 0, 0, 100, 0, (d1, 3.3f, 1000, 0, 100));
        S(WEAPON_DIR, "W_SR_Seismic_A", Act, "Seismic Maul", "Earthquake · 1 ĐCK · 150% ATK + 450, AoE; 20% −10 SPD 2 lượt.", 1, 0, 0, 25, (dA, 1.5f, 450, 0, 100), (dbSpdA, 10f, 0, 2, 20));
        S(WEAPON_DIR, "W_SR_Seismic_U", Ult, "Seismic Maul", "[AoE] Earthshatter Roar · 300% ATK + 900, AoE; xuyên 25% giáp; −15 SPD 2 lượt.", 0, 0, 100, 0, (dA, 3.0f, 900, 0, 100), (dbSpdA, 15f, 0, 2, 100));
        S(WEAPON_DIR, "W_SR_Glacier_A", Act, "Glacier Scepter", "Eternal Frost · 1 ĐCK · 160% ATK + 440, AoE; 30% Đóng Băng(stun 1turn)g.", 1, 0, 0, 25, (dA, 1.6f, 440, 0, 100), (stunA, 1f, 0, 1, 30));
        S(WEAPON_DIR, "W_SR_Glacier_U", Ult, "Glacier Scepter", "[Khống chế] Absolute Zero · 200% ATK + 800, AoE; 100% Đóng Băng (−20 SPD) 2 lượt; 40% Choáng 1 lượt.", 0, 0, 100, 0, (dA, 2.0f, 800, 0, 100), (stunA, 1f, 0, 2, 100));
        S(WEAPON_DIR, "W_SR_Soul_A", Act, "Soulreaver Scythe", "Soul Drinker · 1 ĐCK · 180% ATK + 440, đơn; hồi 10% thành HP.", 1, 0, 0, 25, (d1, 1.8f, 440, 0, 100), (hS, 0.1f, 0, 0, 100));
        S(WEAPON_DIR, "W_SR_Soul_U", Ult, "Soulreaver Scythe", "[Hồi máu] Harvest of Souls · 300% ATK + 850, AoE; hồi 50% sát thương chia đều toàn đội.", 0, 0, 100, 0, (dA, 3.0f, 850, 0, 100), (hA, 0.5f, 0, 0, 100));
        S(WEAPON_DIR, "W_SR_Bastion_A", Act, "Bastion Hammer", "Guard Breaker · 1 ĐCK · 165% ATK + 440, đơn; xuyên 25% giáp.", 1, 0, 0, 25, (d1, 1.65f, 440, 0, 100));
        S(WEAPON_DIR, "W_SR_Bastion_U", Ult, "Bastion Hammer", "[Phòng thủ] Fortress Wall · 150% ATK + 400, đơn; lá chắn 20% HP toàn đội 3 lượt; −20% sát thương nhận 2 lượt.", 0, 0, 100, 0, (shldA, 0.2f, 400, 3, 100));
        S(WEAPON_DIR, "W_SR_Warlord_A", Act, "Warlord Banner Spear", "Raging Sandstorm · 1 ĐCK · 190% ATK + 470, đơn; 40% Choáng 1 lượt.", 1, 0, 0, 25, (d1, 1.9f, 470, 0, 100), (stun1, 1f, 0, 1, 40));
        S(WEAPON_DIR, "W_SR_Warlord_U", Ult, "Warlord Banner Spear", "[Buff] Warlord's Ascension · +18% ATK, +12% Crit Rate, +15% Crit DMG toàn đội 3 lượt.", 0, 0, 100, 0, (bAtkA, 0.18f, 0, 3, 100));

        // ULTRA RARE
        S(WEAPON_DIR, "W_UR_Thunder_A", Act, "Thunderlord Blade", "Heaven's Thunder Strike · 2 ĐCK · 200% ATK + 620, đơn; xuyên 60% giáp; 50% Choáng 1 lượt.", 2, 0, 0, 25, (d1, 2.0f, 620, 0, 100), (stun1, 1f, 0, 1, 50));
        S(WEAPON_DIR, "W_UR_Thunder_U", Ult, "Thunderlord Blade", "[Đơn] Cataclysm Verdict · 400% ATK + 1350, đơn; xuyên 60% giáp; 80% Choáng 2 lượt.", 0, 0, 100, 0, (d1, 4.0f, 1350, 0, 100), (stun1, 1f, 0, 2, 80));
        S(WEAPON_DIR, "W_UR_Void_A", Act, "Voidmaw Cannon", "Dark Vortex · 2 ĐCK · 175% ATK + 600, AoE; địch −7% HP hiện tại/lượt 2 lượt.", 2, 0, 0, 25, (dA, 1.75f, 600, 0, 100), (psn1, 0.07f, 0, 2, 100));
        S(WEAPON_DIR, "W_UR_Void_U", Ult, "Voidmaw Cannon", "[AoE] Black Hole Collapse · 360% ATK + 1250, AoE; xuyên 60% giáp; địch −10% HP hiện tại/lượt 2 lượt.", 0, 0, 100, 0, (dA, 3.6f, 1250, 0, 100), (psn1, 0.1f, 0, 2, 100));
        S(WEAPON_DIR, "W_UR_Frost_A", Act, "Frostbind Lance", "Frostfire Lance · 1 ĐCK · 200% ATK + 600, đơn; 40% Đóng Băng(stun 1 turn).", 1, 0, 0, 25, (d1, 2.0f, 600, 0, 100), (stun1, 1f, 0, 1, 40));
        S(WEAPON_DIR, "W_UR_Frost_U", Ult, "Frostbind Lance", "[Khống chế] Glacial Prison · 260% ATK + 1100, AoE; 100% Đóng Băng 2 lượt; đẩy lùi 40% AV.", 0, 0, 100, 0, (dA, 2.6f, 1100, 0, 100), (stunA, 1f, 0, 2, 100), (avRtA, 40f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Vamp_A", Act, "Vampire Fang Dagger", "Vampiric Onslaught · 2 ĐCK · 205% ATK + 630, đơn; hồi 15% thành HP.", 2, 0, 0, 25, (d1, 2.05f, 630, 0, 100), (hS, 0.15f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Vamp_U", Ult, "Vampire Fang Dagger", "[Hồi máu] Eternal Banquet · 350% ATK + 1200, đơn; hồi 60% sát thương thành HP toàn đội; gỡ 1 Debuff/đồng minh.", 0, 0, 100, 0, (d1, 3.5f, 1200, 0, 100), (hA, 0.6f, 0, 0, 100), (clnsA, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Aegis_A", Act, "Divine Aegis Shield", "Shield Bash · 2 ĐCK · 185% ATK + 600, đơn; xuyên 30% giáp.", 2, 0, 0, 25, (d1, 1.85f, 600, 0, 100));
        S(WEAPON_DIR, "W_UR_Aegis_U", Ult, "Divine Aegis Shield", "[Phòng thủ] Sanctuary of Light · lá chắn 25% HP toàn đội 3 lượt; hồi 12% HP; miễn Choáng 2 lượt.", 0, 0, 100, 0, (shldA, 0.25f, 0, 3, 100), (hA, 0.12f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Sov_A", Act, "Sovereign War Standard", "Storm of Blades · 1 ĐCK · 165% ATK + 600, AoE; 30% Chảy Máu.", 1, 0, 0, 25, (dA, 1.65f, 600, 0, 100), (bldA, 0.03f, 0, 2, 30));
        S(WEAPON_DIR, "W_UR_Sov_U", Ult, "Sovereign War Standard", "[Buff] Imperial Overdrive · +20% ATK, +15% Crit Rate toàn đội 3 lượt; +25 Năng lượng toàn đội.", 0, 0, 100, 0, (bAtkA, 0.2f, 0, 3, 100), (enGnA, 25f, 0, 0, 100));

        // LEGENDARY
        S(WEAPON_DIR, "W_Leg_Star_A", Act, "Starforged Blade", "Starlight Blade · 2 ĐCK · 225% ATK + 1000, đơn; xuyên 70% giáp; Rạn Vỡ −8% HP tối đa/lượt 2 lượt; 75% Choáng 1 lượt.", 2, 0, 0, 25, (d1, 2.25f, 1000, 0, 100), (stun1, 1f, 0, 1, 75));
        S(WEAPON_DIR, "W_Leg_Star_U", Ult, "Starforged Blade", "[Đơn] Supernova Edge · 450% ATK + 2000, đơn; xuyên 80% giáp; Boss −15% HP hiện tại lượt kế.", 0, 0, 100, 0, (d1, 4.5f, 2000, 0, 100));
        S(WEAPON_DIR, "W_Leg_Deluge_A", Act, "Deluge Trident", "Great Deluge · 2 ĐCK · 190% ATK + 950, AoE; địch −6% HP hiện tại/lượt 2 lượt.", 2, 0, 0, 25, (dA, 1.9f, 950, 0, 100), (psn1, 0.06f, 0, 2, 100));
        S(WEAPON_DIR, "W_Leg_Deluge_U", Ult, "Deluge Trident", "[AoE] Genesis Starfall · 400% ATK + 1900, AoE; xuyên 70% giáp; 80% Choáng 2 lượt.", 0, 0, 100, 0, (dA, 4.0f, 1900, 0, 100), (stunA, 1f, 0, 2, 80));
        S(WEAPON_DIR, "W_Leg_Chrono_A", Act, "Chronofreeze Staff", "Time Frost · 2 ĐCK · 200% ATK + 950, AoE; 40% Đóng Băng.", 2, 0, 0, 25, (dA, 2.0f, 950, 0, 100), (stunA, 1f, 0, 1, 40));
        S(WEAPON_DIR, "W_Leg_Chrono_U", Ult, "Chronofreeze Staff", "[Khống chế] Temporal Lock · 300% ATK + 1500, AoE; 100% Choáng 2 lượt; đóng băng AV địch 1 lượt.", 0, 0, 100, 0, (dA, 3.0f, 1500, 0, 100), (stunA, 1f, 0, 2, 100));
        S(WEAPON_DIR, "W_Leg_Phoen_A", Act, "Phoenix Soul Reaver", "Dragon Feather Soul Reaper · 2 ĐCK · 235% ATK + 1050, đơn; 85% Choáng 2 lượt.", 2, 0, 0, 25, (d1, 2.35f, 1050, 0, 100), (stun1, 1f, 0, 2, 85));
        S(WEAPON_DIR, "W_Leg_Phoen_U", Ult, "Phoenix Soul Reaver", "[Hồi sinh] Phoenix Rebirth · 380% ATK + 1700, AoE; hồi 20% HP toàn đội; hồi sinh 1 đồng minh gục 35% HP.", 0, 0, 100, 0, (dA, 3.8f, 1700, 0, 100), (rvv1, 0.35f, 0, 0, 100), (hA, 0.2f, 0, 0, 100));
        S(WEAPON_DIR, "W_Leg_Titan_A", Act, "Titan Aegis Wall", "Void Executioner · 2 ĐCK · 230% ATK + 1000, đơn; +40% nếu mục tiêu <40% HP.", 2, 0, 0, 25, (d1, 2.3f, 1000, 0, 100));
        S(WEAPON_DIR, "W_Leg_Titan_U", Ult, "Titan Aegis Wall", "[Phòng thủ] Aegis Eternal · lá chắn 30% HP toàn đội 3 lượt; toàn đội +15% DEF; miễn Debuff 2 lượt.", 0, 0, 100, 0, (shldA, 0.3f, 0, 3, 100), (bDefA, 0.15f, 0, 3, 100));
        S(WEAPON_DIR, "W_Leg_Celest_A", Act, "Celestial War Aegis", "Celestial Tempest · 1 ĐCK · 200% ATK + 950, AoE; 50% Choáng.", 1, 0, 0, 25, (dA, 2.0f, 950, 0, 100), (stunA, 1f, 0, 1, 50));
        S(WEAPON_DIR, "W_Leg_Celest_U", Ult, "Celestial War Aegis", "[Buff] Divine Coronation · +22% ATK, +18% Crit Rate, +25% Crit DMG toàn đội 3 lượt; +30 Năng lượng.", 0, 0, 100, 0, (bAtkA, 0.22f, 0, 3, 100), (enGnA, 30f, 0, 0, 100));

        // MYTHIC
        S(WEAPON_DIR, "W_Myt_World_A", Act, "World-Ender Blade", "Apocalyptic Annihilation · 2 ĐCK · 250% ATK + 1500, đơn; xuyên 80% giáp; Rạn Vỡ −10% HP tối đa/lượt 3 lượt.", 2, 0, 0, 25, (d1, 2.5f, 1500, 0, 100));
        S(WEAPON_DIR, "W_Myt_World_U", Ult, "World-Ender Blade", "[Đơn] Extinction Protocol · 550% ATK + 2750, đơn; xuyên 100% giáp; Boss −25% HP hiện tại lượt kế.", 0, 0, 100, 0, (d1, 5.5f, 2750, 0, 100));
        S(WEAPON_DIR, "W_Myt_Apoc_A", Act, "Apocalypse Cannon", "Wrath of Heaven and Earth · 3 ĐCK · 230% ATK + 1450, AoE; địch −9% HP hiện tại/lượt 3 lượt; 80% Choáng 2 lượt.", 3, 0, 0, 25, (dA, 2.3f, 1450, 0, 100), (stunA, 1f, 0, 2, 80));
        S(WEAPON_DIR, "W_Myt_Apoc_U", Ult, "Apocalypse Cannon", "[AoE] Ultimate Psychic Surge · 520% ATK + 2750, AoE; xuyên 100% giáp; 100% Choáng 3 lượt.", 0, 0, 100, 0, (dA, 5.2f, 2750, 0, 100), (stunA, 1f, 0, 3, 100));
        S(WEAPON_DIR, "W_Myt_Obli_A", Act, "Oblivion Halberd", "Blade of the Void · 2 ĐCK · 260% ATK + 1550, đơn; Boss −20% HP hiện tại lượt kế; 85% Choáng 2 lượt.", 2, 0, 0, 25, (d1, 2.6f, 1550, 0, 100), (stun1, 1f, 0, 2, 85));
        S(WEAPON_DIR, "W_Myt_Obli_U", Ult, "Oblivion Halberd", "[Khống chế] Absolute Silence · 400% ATK + 2200, AoE; 100% Choáng 3 lượt; khoá kỹ năng địch 2 lượt.", 0, 0, 100, 0, (dA, 4.0f, 2200, 0, 100), (stunA, 1f, 0, 3, 100));
        S(WEAPON_DIR, "W_Myt_Eter_A", Act, "Eternal Bloodlord Scythe", "Eternal Cataclysm · 2 ĐCK · 205% ATK + 1500, AoE; hồi 10% thành HP.", 2, 0, 0, 25, (dA, 2.05f, 1500, 0, 100), (hS, 0.1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Myt_Eter_U", Ult, "Eternal Bloodlord Scythe", "[Hồi sinh] Rite of Immortality · 450% ATK + 2400, AoE; hồi 30% HP toàn đội; hồi sinh toàn bộ đồng minh gục 30% HP.", 0, 0, 100, 0, (dA, 4.5f, 2400, 0, 100), (rvvA, 0.3f, 0, 0, 100), (hA, 0.3f, 0, 0, 100));
        S(WEAPON_DIR, "W_Myt_Gen_A", Act, "Genesis Bastion", "Ragnarok Descent · 3 ĐCK · 290% ATK + 1600, đơn; xuyên 100% giáp; +50% nếu mục tiêu <30% HP.", 3, 0, 0, 25, (d1, 2.9f, 1600, 0, 100));
        S(WEAPON_DIR, "W_Myt_Gen_U", Ult, "Genesis Bastion", "[Phòng thủ] Impervious Genesis · lá chắn 40% HP toàn đội 3 lượt; miễn mọi Debuff & True damage 2 lượt.", 0, 0, 100, 0, (shldA, 0.4f, 0, 3, 100), (clnsA, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Myt_Emp_A", Act, "Empyrean War Crown", "Judgment Ray · 2 ĐCK · 240% ATK + 1500, AoE; 60% Choáng.", 2, 0, 0, 25, (dA, 2.4f, 1500, 0, 100), (stunA, 1f, 0, 1, 60));
        S(WEAPON_DIR, "W_Myt_Emp_U", Ult, "Empyrean War Crown", "[Buff] Empyrean Ascension · +30% ATK, +25% Crit Rate, +40% Crit DMG toàn đội 3 lượt; hồi đầy Năng lượng toàn đội (1 lần/trận).", 0, 0, 100, 0, (bAtkA, 0.3f, 0, 3, 100), (enGnA, 100f, 0, 0, 100));

        // SECRET
        S(WEAPON_DIR, "W_Sec_Judg_A", Act, "Judgment Edge", "Celestial Judgment · 1 ĐCK · 200% ATK + 620, đơn; xuyên 60% giáp; 100% Choáng 1 lượt; [ĐỘC QUYỀN] diệt địch → +1 ĐCK.", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100), (stun1, 1f, 0, 1, 100));
        S(WEAPON_DIR, "W_Sec_Judg_U", Ult, "Judgment Edge", "[ĐỘC QUYỀN · Khống chế] Fate Reversal · 200% ATK + 620, AoE; xóa toàn bộ Buff địch; 3 lượt kế mọi đòn đồng minh 100% Crit.", 0, 0, 100, 0, (dA, 2.0f, 620, 0, 100), (dsplA, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Obli_A", Act, "Oblivion Codex", "Genesis Oblivion · 1 ĐCK · 200% ATK + 620, đơn; xuyên giáp hoàn toàn; [ĐỘC QUYỀN] vô hiệu nội tại Boss 3 lượt.", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100));
        S(WEAPON_DIR, "W_Sec_Obli_U", Ult, "Oblivion Codex", "[ĐỘC QUYỀN · Đơn] Null Genesis · 260% ATK + 900, đơn; xuyên giáp hoàn toàn; đánh cắp & vô hiệu 1 buff mạnh nhất Boss 3 lượt.", 0, 0, 100, 0, (d1, 2.6f, 900, 0, 100), (dspl1, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Ecli_A", Act, "Eclipse Reaver", "Eclipse of Eternity · 2 ĐCK · 200% ATK + 620, AoE; xuyên giáp hoàn toàn; 100% Độc (−8% HP tối đa/lượt, 3 lượt); [ĐỘC QUYỀN] diệt địch → +1 ĐCK.", 2, 0, 0, 25, (dA, 2.0f, 620, 0, 100), (psn1, 0.08f, 0, 3, 100));
        S(WEAPON_DIR, "W_Sec_Ecli_U", Ult, "Eclipse Reaver", "[ĐỘC QUYỀN · Hồi máu] Eternal Eclipse · 200% ATK + 700, AoE; hồi 40% thành HP toàn đội; toàn đội +1 ĐCK.", 0, 0, 100, 0, (dA, 2.0f, 700, 0, 100), (hA, 0.4f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Chro_A", Act, "Chrono Severance", "Zero-Point Severance · 1 ĐCK · 200% ATK + 620, đơn; bỏ qua lá chắn/buff thủ; mở màn lượt → 100% Crit.", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100));
        S(WEAPON_DIR, "W_Sec_Chro_U", Ult, "Chrono Severance", "[ĐỘC QUYỀN · Hành động] Time Stop · toàn đội hành động thêm 1 lượt (1 lần/trận); địch bị bỏ qua 1 lượt.", 0, 0, 100, 0, (avAdA, 100f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Req_A", Act, "Requiem Engine", "Requiem Protocol · 1 ĐCK · 170% ATK + 600, AoE; đòn thường kế không tốn lượt.", 1, 0, 0, 25, (dA, 1.7f, 600, 0, 100));
        S(WEAPON_DIR, "W_Sec_Req_U", Ult, "Requiem Engine", "[ĐỘC QUYỀN · Phòng thủ] Requiem Aegis · toàn đội bất tử 1 lượt (HP ≥1); hồi đầy Năng lượng (1 lần/trận).", 0, 0, 100, 0, (shldA, 9.9f, 0, 1, 100), (enGnA, 100f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Sov_A", Act, "Sovereign Protocol", "Dominion Strike · 1 ĐCK · 200% ATK + 620, đơn; xuyên 60% giáp.", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100));
        S(WEAPON_DIR, "W_Sec_Sov_U", Ult, "Sovereign Protocol", "[ĐỘC QUYỀN · Khống chế] Absolute Dominion · Boss −40% ATK & −40% SPD 2 lượt (địch thường: 50% chiếm quyền 2 lượt); toàn đội +1 ĐCK.", 0, 0, 100, 0, (dbAtk1, 0.4f, 0, 2, 100), (dbSpd1, 40f, 0, 2, 100));

        // ── 3. HEAD SKILLS (Mũ - Hỗ trợ tốn ĐCK) ──────────────────────────────
        S(HAT_DIR, "H_Com_Focus", Act, "Minor Focus", "Buff · 1 ĐCK · +5% ATK & +4 SPD bản thân 2 lượt.", 1, 0, 0, 25, (bAtkS, 0.05f, 0, 2, 100), (bSpdS, 4f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Guard", Act, "Quick Guard", "Buff · 1 ĐCK · +8% DEF bản thân 2 lượt.", 1, 0, 0, 25, (bDefS, 0.08f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Dress", Act, "Field Dressing", "Hồi máu · 1 ĐCK · Hồi 8% HP tối đa 1 đồng minh.", 1, 0, 0, 25, (h1, 0.08f, 0, 0, 100));
        S(HAT_DIR, "H_Com_Warm", Act, "Warm Up", "Haste · 1 ĐCK · +6 SPD bản thân 2 lượt.", 1, 0, 0, 25, (bSpdS, 6f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Taunt", Act, "Taunt Cry", "Debuff · 1 ĐCK · −5% ATK 1 địch 2 lượt.", 1, 0, 0, 25, (dbAtk1, 0.05f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Hobble", Act, "Hobble Shot", "Debuff · 1 ĐCK · −4 SPD 1 địch 2 lượt.", 1, 0, 0, 25, (dbSpd1, 4f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Steady", Act, "Steady Aim", "Buff · 1 ĐCK · +5% Crit Rate bản thân 2 lượt.", 1, 0, 0, 25, (bAtkS, 0.05f, 0, 2, 100));

        S(HAT_DIR, "H_Unc_Chant", Act, "Battle Chant", "Buff · 1 ĐCK · +7% ATK & +3 SPD toàn đội 2 lượt.", 1, 0, 0, 25, (bAtkA, 0.07f, 0, 2, 100), (bSpdA, 3f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Guard", Act, "Guard Formation", "Buff · 1 ĐCK · +8% DEF toàn đội 2 lượt.", 1, 0, 0, 25, (bDefA, 0.08f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Mend", Act, "Mending Wave", "Hồi máu · 1 ĐCK · Hồi 6% HP tối đa toàn đội.", 1, 0, 0, 25, (hA, 0.06f, 0, 0, 100));
        S(HAT_DIR, "H_Unc_Haste", Act, "Haste Cry", "Haste · 1 ĐCK · +8 SPD toàn đội 2 lượt.", 1, 0, 0, 25, (bSpdA, 8f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Weak", Act, "Weakening Shout", "Debuff · 1 ĐCK · −7% ATK toàn địch 2 lượt.", 1, 0, 0, 25, (dbAtkA, 0.07f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Sap", Act, "Sap Speed", "Debuff · 1 ĐCK · −8 SPD toàn địch 2 lượt; đẩy lùi 15% AV 1 địch.", 1, 0, 0, 25, (dbSpdA, 8f, 0, 2, 100), (avRt1, 15f, 0, 0, 100));
        S(HAT_DIR, "H_Unc_Hunt", Act, "Hunter's Mark", "Debuff · 1 ĐCK · 1 địch +10% sát thương phải chịu 2 lượt.", 1, 0, 0, 25, (dbDef1, 0.1f, 0, 2, 100));

        S(HAT_DIR, "H_Rar_Chant", Act, "War Chant", "Buff · 1 ĐCK · +10% ATK, +8% Crit Rate & +5 SPD toàn đội 2 lượt.", 1, 0, 0, 25, (bAtkA, 0.1f, 0, 2, 100), (bSpdA, 5f, 0, 2, 100));
        S(HAT_DIR, "H_Rar_Wall", Act, "Stone Wall", "Buff · 1 ĐCK · +12% DEF toàn đội 2 lượt; −5% sát thương nhận.", 1, 0, 0, 25, (bDefA, 0.12f, 0, 2, 100));
        S(HAT_DIR, "H_Rar_Light", Act, "Healing Light", "Hồi máu · 1 ĐCK · Hồi 12% HP 1 đồng minh; gỡ 1 Debuff.", 1, 0, 0, 25, (h1, 0.12f, 0, 0, 100), (clns1, 1f, 0, 0, 100));
        S(HAT_DIR, "H_Rar_Quick", Act, "Quickstep Cry", "Haste · 1 ĐCK · +10 SPD 1 đồng minh 2 lượt.", 1, 0, 0, 25, (bSpdS, 10f, 0, 2, 100));
        S(HAT_DIR, "H_Rar_Frost", Act, "Frost Hex", "Debuff · 1 ĐCK · −10 SPD toàn địch 2 lượt; 20% Đóng Băng 1 địch.", 1, 0, 0, 25, (dbSpdA, 10f, 0, 2, 100), (stun1, 1f, 0, 1, 20));
        S(HAT_DIR, "H_Rar_Curse", Act, "Curse of Weakness", "Debuff · 1 ĐCK · −10% ATK & −8% DEF 1 địch 3 lượt.", 1, 0, 0, 25, (dbAtk1, 0.1f, 0, 3, 100), (dbDef1, 0.08f, 0, 3, 100));
        S(HAT_DIR, "H_Rar_Rally", Act, "Rallying Horn", "Hỗ trợ · 1 ĐCK · +8 Năng lượng toàn đội.", 1, 0, 0, 25, (enGnA, 8f, 0, 0, 100));

        S(HAT_DIR, "H_SR_Banner", Act, "Rallying Banner", "Buff · 1 ĐCK · +12% ATK, +6% DEF & +6 SPD toàn đội 3 lượt; hồi 5% HP.", 1, 0, 0, 25, (bAtkA, 0.12f, 0, 3, 100), (bDefA, 0.06f, 0, 3, 100));
        S(HAT_DIR, "H_SR_Sanct", Act, "Sanctuary Ward", "Phòng thủ · 1 ĐCK · Lá chắn 8% HP tối đa toàn đội 2 lượt.", 1, 0, 0, 25, (shldA, 0.08f, 0, 2, 100));
        S(HAT_DIR, "H_SR_Purge", Act, "Purge Light", "Hồi máu · 1 ĐCK · Gỡ toàn bộ Debuff 1 đồng minh; hồi 15% HP.", 1, 0, 0, 25, (clns1, 1f, 0, 0, 100), (h1, 0.15f, 0, 0, 100));
        S(HAT_DIR, "H_SR_Order", Act, "Battle Order", "Kéo lượt · 1 ĐCK · Kéo 1 đồng minh TIẾN 50%; +12% ATK 1 lượt.", 1, 0, 0, 25, (avAd1, 50f, 0, 0, 100), (bAtkS, 0.12f, 0, 1, 100));
        S(HAT_DIR, "H_SR_Doom", Act, "Doom Brand", "Debuff · 1 ĐCK · 1 địch +15% sát thương phải chịu 3 lượt; −10% DEF.", 1, 0, 0, 25, (dbDef1, 0.1f, 0, 3, 100));
        S(HAT_DIR, "H_SR_Blizz", Act, "Blizzard Field", "Debuff · 1 ĐCK · −15 SPD toàn địch 2 lượt; 35% Đóng Băng; đẩy lùi 25% AV.", 1, 0, 0, 25, (dbSpdA, 15f, 0, 2, 100), (avRtA, 25f, 0, 0, 100));
        S(HAT_DIR, "H_SR_Grave", Act, "Grave Seal", "Debuff · 1 ĐCK · Khoá Buff 1 địch; −30% hồi máu nhận 2 lượt.", 1, 0, 0, 25, (dspl1, 1f, 0, 2, 100));
        S(HAT_DIR, "H_SR_Overc", Act, "Overcharge", "Hỗ trợ · 1 ĐCK · +12 Năng lượng toàn đội; +8% Crit DMG 2 lượt.", 1, 0, 0, 25, (enGnA, 12f, 0, 0, 100));

        S(HAT_DIR, "H_UR_Warlord", Act, "Warlord's Command", "Buff · 1 ĐCK · +15% ATK, +12% Crit Rate & +8 SPD toàn đội 3 lượt.", 1, 0, 0, 25, (bAtkA, 0.15f, 0, 3, 100), (bSpdA, 8f, 0, 3, 100));
        S(HAT_DIR, "H_UR_EterP", Act, "Eternal Purification", "Hồi máu · 1 ĐCK · Gỡ mọi Debuff, hồi 8% HP, +8% DEF toàn đội 3 lượt.", 1, 0, 0, 25, (hA, 0.08f, 0, 0, 100), (clnsA, 1f, 0, 0, 100));
        S(HAT_DIR, "H_UR_Nightm", Act, "Eternal Nightmare", "Debuff · 1 ĐCK · −20% ATK/DEF/SPD toàn địch 3 lượt.", 1, 0, 0, 25, (dbAtkA, 0.2f, 0, 3, 100), (dbDefA, 0.2f, 0, 3, 100));
        S(HAT_DIR, "H_UR_Dream", Act, "Dream Overture", "Kéo lượt · 1 ĐCK · Kéo 1 đồng minh TIẾN 75%; +20% Crit DMG 2 lượt.", 1, 0, 0, 25, (avAd1, 75f, 0, 0, 100));
        S(HAT_DIR, "H_UR_TimeW", Act, "Time Warp", "Haste · 1 ĐCK · +12 SPD toàn đội 3 lượt.", 1, 0, 0, 25, (bSpdA, 12f, 0, 3, 100));
        S(HAT_DIR, "H_UR_Aegis", Act, "Aegis Barrier", "Phòng thủ · 1 ĐCK · Lá chắn 12% HP toàn đội 3 lượt; miễn Choáng 2 lượt.", 1, 0, 0, 25, (shldA, 0.12f, 0, 3, 100));
        S(HAT_DIR, "H_UR_DoomS", Act, "Doom Sentence", "Debuff · 1 ĐCK · Toàn địch +18% sát thương phải chịu; −40% hồi máu 3 lượt.", 1, 0, 0, 25, (dbDefA, 0.18f, 0, 3, 100));
        S(HAT_DIR, "H_UR_Hymn", Act, "Energizing Hymn", "Hỗ trợ · 1 ĐCK · +30 Năng lượng toàn đội;", 1, 0, 0, 25, (enGnA, 30f, 0, 0, 100));

        S(HAT_DIR, "H_Leg_Holy", Act, "Absolute Holy Domain", "Hồi máu · 1 ĐCK · Hồi 10% HP, +10% ATK/DEF/SPD toàn đội 3 lượt; miễn Debuff 2 lượt.", 1, 0, 0, 25, (hA, 0.1f, 0, 0, 100), (bAtkA, 0.1f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_Valk", Act, "Valkyrie's Blessing", "Buff · 1 ĐCK · +18% ATK, +15% Crit Rate, +20% Crit DMG & +10 SPD toàn đội 3 lượt.", 1, 0, 0, 25, (bAtkA, 0.18f, 0, 3, 100), (bSpdA, 10f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_Dooms", Act, "Doomsday Decree", "Debuff · 1 ĐCK · −25% ATK/DEF/SPD toàn địch 3 lượt; +20% sát thương phải chịu 2 lượt.", 1, 0, 0, 25, (dbAtkA, 0.25f, 0, 3, 100), (dbSpdA, 25f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_SovM", Act, "Sovereign March", "Kéo lượt · 1 ĐCK · Kéo 1 đồng minh TIẾN 100% (đi ngay); +25% ATK & Crit DMG 2 lượt.", 1, 0, 0, 25, (avAd1, 100f, 0, 0, 100), (bAtkS, 0.25f, 0, 2, 100));
        S(HAT_DIR, "H_Leg_Tempo", Act, "Tempo Overdrive", "Haste · 1 ĐCK · +15 SPD toàn đội 3 lượt; miễn hiệu ứng làm chậm 2 lượt.", 1, 0, 0, 25, (bSpdA, 15f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_Guard", Act, "Guardian's Sanctuary", "Phòng thủ · 1 ĐCK · Lá chắn 18% HP toàn đội 3 lượt; vỡ chắn → hồi 8% HP.", 1, 0, 0, 25, (shldA, 0.18f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_GraveE", Act, "Grave Edict", "Debuff · 1 ĐCK · Toàn địch khoá Buff; −50% hồi máu; đẩy lùi 30% AV 2 lượt.", 1, 0, 0, 25, (dsplA, 1f, 0, 2, 100), (avRtA, 30f, 0, 0, 100));
        S(HAT_DIR, "H_Leg_SecW", Act, "Second Wind", "Hồi sinh · 1 ĐCK · Hồi sinh 1 đồng minh gục 30% HP (1 lần/trận).", 1, 0, 0, 25, (rvv1, 0.3f, 0, 0, 100));

        S(HAT_DIR, "H_Myt_Emp", Act, "Empyrean Coronation", "Buff · 1 ĐCK · +25% ATK, +20% Crit Rate, +30% Crit DMG & +12 SPD toàn đội 3 lượt.", 1, 0, 0, 25, (bAtkA, 0.25f, 0, 3, 100), (bSpdA, 12f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_DivA", Act, "Divine Ascension", "Buff · 1 ĐCK · +12% ATK/DEF/SPD toàn đội 3 lượt; −10% SPD toàn địch.", 1, 0, 0, 25, (bAtkA, 0.12f, 0, 3, 100), (bDefA, 0.12f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_WrldS", Act, "World Sanction", "Debuff · 1 ĐCK · −30% ATK/DEF/SPD toàn địch 3 lượt; +25% sát thương phải chịu 3 lượt; khoá Buff 2 lượt.", 1, 0, 0, 25, (dbAtkA, 0.3f, 0, 3, 100), (dbSpdA, 30f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_CelO", Act, "Celestial Overture", "Kéo lượt · 1 ĐCK · Kéo 1 đồng minh TIẾN 100%; +35% Crit DMG 2 lượt; đội +8 SPD 3 lượt.", 1, 0, 0, 25, (avAd1, 100f, 0, 0, 100), (bSpdA, 8f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_ChrO", Act, "Chrono Overdrive", "Haste · 1 ĐCK · +20 SPD toàn đội 3 lượt.", 1, 0, 0, 25, (bSpdA, 20f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_WrldR", Act, "World Requiem", "Debuff · 1 ĐCK · Toàn địch −40% SPD; đẩy lùi 40% AV; khoá Buff; −60% hồi máu 3 lượt.", 1, 0, 0, 25, (dbSpdA, 40f, 0, 3, 100), (avRtA, 40f, 0, 0, 100));
        S(HAT_DIR, "H_Myt_Bast", Act, "Bastion Eternal", "Phòng thủ · 1 ĐCK · Lá chắn 25% HP toàn đội 3 lượt; miễn mọi Debuff & Choáng 3 lượt.", 1, 0, 0, 25, (shldA, 0.25f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_MassR", Act, "Mass Resurrection", "Hồi sinh · 2 ĐCK · Hồi sinh toàn bộ đồng minh gục 25% HP (1 lần/trận).", 2, 0, 0, 25, (rvvA, 0.25f, 0, 0, 100));

        S(HAT_DIR, "H_Sec_Dict", Act, "Dictator's Decree", "[ĐỘC QUYỀN] · 1 ĐCK · Nâng trần ĐCK lên tối đa 7 điểm chiến ;  sử dụng sẽ tăng giới hạn chiến kĩ đội  thêm +1 ; +1 ĐCK/lượt 2 lượt.", 1, 0, 0, 25, (enGnA, 1f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_TimeD", Act, "Time Dilation", "[ĐỘC QUYỀN] · 1 ĐCK · 1 đồng minh hành động thêm 1 lần ngay (extra turn).", 1, 0, 0, 25, (avAd1, 100f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_GrandO", Act, "Grand Overture", "[ĐỘC QUYỀN] · 1 ĐCK · 1 lần/trận · Kéo TOÀN ĐỘI TIẾN 100% (cả đội đi ngay).", 1, 0, 0, 25, (avAdA, 100f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_ChroS", Act, "Chrono Seizure", "[ĐỘC QUYỀN] · 1 ĐCK · Đẩy lùi toàn địch 50% AV; −20% SPD 2 lượt.", 1, 0, 0, 25, (avRtA, 50f, 0, 0, 100), (dbSpdA, 20f, 0, 2, 100));
        S(HAT_DIR, "H_Sec_SovG", Act, "Sovereign's Grace", "[ĐỘC QUYỀN] · 1 ĐCK · 1 lần/trận · Toàn đội hồi đầy Năng lượng ngay.", 1, 0, 0, 25, (enGnA, 100f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_AbsE", Act, "Absolute Edict", "[ĐỘC QUYỀN] · 1 ĐCK · 3 lượt: mọi đòn đồng minh không bị né/chặn; +100% Crit DMG.", 1, 0, 0, 25, (bAtkA, 1f, 0, 3, 100));
        S(HAT_DIR, "H_Sec_MindH", Act, "Mind Hijack", "[ĐỘC QUYỀN] · 1 ĐCK · 40% chiếm quyền 1 địch thường 1 lượt; Boss → −30% ATK 2 lượt.", 1, 0, 0, 25, (dbAtk1, 0.3f, 0, 2, 100));

        // ── 4. BODY SKILLS (Áo - Nội tại Passive) ─────────────────────────────
        S(BODY_DIR, "B_Com_Stick", Psv, "Sticky Slime", "Nội tại · Giảm 5% sát thương nhận vào.", 0, 0, 0, 0, (bDefS, 0.05f, 0, 0, 100));
        S(BODY_DIR, "B_Com_Thin", Psv, "Thin Hide", "Nội tại · +4% HP tối đa.", 0, 0, 0, 0, (bDefS, 0.04f, 0, 0, 100));
        S(BODY_DIR, "B_Com_SlowM", Psv, "Slow Metabolism", "Nội tại · Cuối mỗi lượt hồi 1% HP tối đa.", 0, 0, 0, 0, (hS, 0.01f, 0, 0, 100));
        S(BODY_DIR, "B_Com_SoftB", Psv, "Soft Body", "Nội tại · Giảm 10% sát thương từ đòn Chí Mạng nhận vào.", 0, 0, 0, 0, (bDefS, 0.1f, 0, 0, 100));
        S(BODY_DIR, "B_Com_Light", Psv, "Light Step", "Nội tại · +3 SPD.", 0, 0, 0, 0, (bSpdS, 3f, 0, 0, 100));
        S(BODY_DIR, "B_Com_Spike", Psv, "Minor Spikes", "Nội tại · Phản 3% sát thương nhận vào.", 0, 0, 0, 0, (bDefS, 0.03f, 0, 0, 100));

        S(BODY_DIR, "B_Unc_IronW", Psv, "Iron Will", "Nội tại · Cuối mỗi lượt hồi 1.5% HP tối đa.", 0, 0, 0, 0, (hS, 0.015f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Stone", Psv, "Stone Skin", "Nội tại · +6% DEF.", 0, 0, 0, 0, (bDefS, 0.06f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Vital", Psv, "Vital Growth", "Nội tại · +6% HP tối đa.", 0, 0, 0, 0, (bDefS, 0.06f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Adren", Psv, "Adrenaline", "Nội tại · HP <50% → +5% ATK.", 0, 0, 0, 0, (bAtkS, 0.05f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Corro", Psv, "Corrosive Slime", "Nội tại · Kẻ tấn công bản thân −3% DEF (dồn tối đa 3).", 0, 0, 0, 0, (dbDef1, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Barbe", Psv, "Barbed Coat", "Nội tại · Phản 5% sát thương nhận vào.", 0, 0, 0, 0, (bDefS, 0.05f, 0, 0, 100));

        S(BODY_DIR, "B_Rar_StoneA", Psv, "Stone Armor", "Nội tại · HP <40% → +3% DEF.", 0, 0, 0, 0, (bDefS, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_ReinT", Psv, "Reinforced Thorn Armor", "Nội tại · +3% ATK; phản 5% sát thương.", 0, 0, 0, 0, (bAtkS, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_Muscl", Psv, "Muscle Enhancement", "Nội tại · Đồng đội HP <50% được +3% xuyên giáp.", 0, 0, 0, 0, (bAtkA, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_LifeI", Psv, "Life Infusion", "Nội tại · Hồi 2% HP tối đa toàn đội cuối lượt.", 0, 0, 0, 0, (hA, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_Berse", Psv, "Berserker Blood", "Nội tại · Mỗi 10% HP đã mất → +2% ATK (max +16%).", 0, 0, 0, 0, (bAtkS, 0.16f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_Guard", Psv, "Guardian Barrier", "Nội tại · Địch gần nhất −3% ATK (aura).", 0, 0, 0, 0, (dbAtk1, 0.03f, 0, 0, 100));

        S(BODY_DIR, "B_SR_Dragon", Psv, "Dragon Scale Armor", "Nội tại · +4% ATK, DEF & SPD.", 0, 0, 0, 0, (bAtkS, 0.04f, 0, 0, 100), (bSpdS, 4f, 0, 0, 100));
        S(BODY_DIR, "B_SR_WarrF", Psv, "Warrior's Fury", "Nội tại · Toàn đội +2% DEF; bản thân +4% ATK.", 0, 0, 0, 0, (bDefA, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_SR_RestA", Psv, "Restoration Aura", "Nội tại · Toàn đội hồi 2% HP tối đa cuối lượt.", 0, 0, 0, 0, (hA, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_SR_Diam", Psv, "Diamond Armor", "Nội tại · HP <50% → +4% xuyên giáp.", 0, 0, 0, 0, (bAtkS, 0.04f, 0, 0, 100));
        S(BODY_DIR, "B_SR_Retri", Psv, "Retribution Plate", "Nội tại · Phản 8% sát thương; bị crit phản thêm 5%.", 0, 0, 0, 0, (bDefS, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_SR_Momen", Psv, "Momentum Core", "Nội tại · Dùng đòn thường → +3% ATK 2 lượt (dồn tối đa 3).", 0, 0, 0, 0, (bAtkS, 0.09f, 0, 0, 100));

        S(BODY_DIR, "B_UR_NightS", Psv, "Nightmare Shackles", "Nội tại · +6% DEF & +3% HP tối đa.", 0, 0, 0, 0, (bDefS, 0.06f, 0, 0, 100));
        S(BODY_DIR, "B_UR_DivGA", Psv, "Divine Guardian Armor", "Nội tại · Sau khi bị đánh, hồi HP đồng đội = 5% sát thương bản thân dính.", 0, 0, 0, 0, (hA, 0.05f, 0, 0, 100));
        S(BODY_DIR, "B_UR_EterS", Psv, "Eternal Suppression", "Nội tại · +8% DEF; −4% sát thương nhận.", 0, 0, 0, 0, (bDefS, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_UR_VampC", Psv, "Vampiric Core", "Nội tại · Mọi đòn hồi 8% sát thương thành HP.", 0, 0, 0, 0, (hS, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_UR_SpdSe", Psv, "Speed Seal", "Nội tại · +5% DEF; đầu lượt nếu đi trước địch → +6 SPD lượt đó.", 0, 0, 0, 0, (bSpdS, 6f, 0, 0, 100));
        S(BODY_DIR, "B_UR_BulwA", Psv, "Bulwark Aura", "Nội tại · Toàn đội −5% sát thương nhận; bản thân −8%.", 0, 0, 0, 0, (bDefA, 0.05f, 0, 0, 100));

        S(BODY_DIR, "B_Leg_ArmI", Psv, "Armor of Immortality", "Nội tại · Lần đầu bị hạ gục → hồi sinh 10% HP tối đa (1 lần/trận).", 0, 0, 0, 0, (rvv1, 0.1f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_HeavA", Psv, "Heaven's Aegis", "Nội tại · +10% DEF; −8% sát thương; đầu trận lá chắn 12% HP tối đa.", 0, 0, 0, 0, (bDefS, 0.1f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_DivR", Psv, "Divine Resurrection", "Nội tại · Toàn địch −8% SPD & −5% ATK (aura).", 0, 0, 0, 0, (dbSpdA, 8f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_EterB", Psv, "Eternal Bulwark", "Nội tại · Mỗi lượt sống sót → +2% DEF (max +20%).", 0, 0, 0, 0, (bDefS, 0.2f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_Blood", Psv, "Bloodlord Dominion", "Nội tại · Hồi 12% sát thương thành HP; HP đầy → dư thành lá chắn (max 15% HP).", 0, 0, 0, 0, (hS, 0.12f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_AegF", Psv, "Aegis of the Fallen", "Nội tại · 1 đồng minh gục → toàn đội +10% ATK & −10% sát thương nhận 3 lượt.", 0, 0, 0, 0, (bAtkA, 0.1f, 0, 0, 100));

        S(BODY_DIR, "B_Myt_HeavS", Psv, "Heaven's Suppression", "Nội tại · +10% DEF; −9% sát thương nhận; hồi 2% HP cuối lượt.", 0, 0, 0, 0, (bDefS, 0.1f, 0, 0, 100), (hS, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_DivI", Psv, "Divine Incarnation", "Nội tại · Toàn đội +8% ATK, +8% DEF, +5% SPD (aura).", 0, 0, 0, 0, (bAtkA, 0.08f, 0, 0, 100), (bDefA, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_ImmS", Psv, "Immortal Sovereign", "Nội tại · Miễn Chí Mạng nhận vào; −12% sát thương; hồi 3% HP cuối lượt.", 0, 0, 0, 0, (hS, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_WrldT", Psv, "World Tree Root", "Nội tại · Toàn đội hồi 3% HP tối đa cuối lượt & +6% HP tối đa.", 0, 0, 0, 0, (hA, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_TitR", Psv, "Titan's Reprisal", "Nội tại · Phản 15% sát thương; bị crit phản 25%; miễn hất tung/đẩy lùi.", 0, 0, 0, 0, (bDefS, 0.15f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_AscC", Psv, "Ascendant Core", "Nội tại · HP >70% → +15% ATK; HP <30% → −30% sát thương nhận.", 0, 0, 0, 0, (bAtkS, 0.15f, 0, 0, 100));

        S(BODY_DIR, "B_Sec_Dict", Psv, "Dictator", "Nội tại [ĐỘC QUYỀN] · +2 ĐCK mỗi lượt cho đội (là toàn bộ trần sinh +2/lượt).", 0, 0, 0, 0, (enGnA, 2f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Prio", Psv, "Priority Protocol", "Nội tại [ĐỘC QUYỀN] · Luôn hành động đầu tiên mỗi lượt (bỏ qua SPD); đầu trận +1 ĐCK.", 0, 0, 0, 0, (avAdS, 100f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Hack", Psv, "Hacker", "Nội tại [ĐỘC QUYỀN] · Mỗi lượt người chơi, tự đánh 1 địch bất kỳ 270% ATK thường.", 0, 0, 0, 0, (d1, 2.7f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Fina", Psv, "Final Vengeance", "Nội tại [ĐỘC QUYỀN] · HP về 0: miễn crit, phản 200% ATK; địch chết → hồi sinh 30% HP.", 0, 0, 0, 0, (d1, 2.0f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Over", Psv, "Overclock Engine", "Nội tại [ĐỘC QUYỀN] · Tiêu ĐCK → 50% hoàn 1 ĐCK (tối đa 1/lượt); Ultimate nạp Năng lượng +50%.", 0, 0, 0, 0, (enGnA, 1f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Sing", Psv, "Singularity Field", "Nội tại [ĐỘC QUYỀN] · Toàn địch −15% mọi chỉ số & không thể tự tăng chỉ số bằng buff/nội tại (aura).", 0, 0, 0, 0, (dbAtkA, 0.15f, 0, 0, 100), (dbSpdA, 15f, 0, 0, 100));

        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("<color=green>[SkillGen] HOÀN TẤT! Toàn bộ Database Skill từ Spec đã được tạo thành công!</color>");
    }

    // ── Hàm hỗ trợ ────────────────────────────────────────────────────────
    static SkillEffectSO E(string n, EffectType t, TargetSide s, AoEShape a, AnchorType an = AnchorType.AttackTarget, BuffStat b = BuffStat.Defense)
    {
        string path = $"{EFFECT_DIR}/{n}.asset";
        var so = AssetDatabase.LoadAssetAtPath<SkillEffectSO>(path);
        if (so == null) { so = ScriptableObject.CreateInstance<SkillEffectSO>(); AssetDatabase.CreateAsset(so, path); }
        so.type = t; so.targetSide = s; so.aoeShape = a; so.anchorType = an; so.buffStat = b;
        EditorUtility.SetDirty(so); return so;
    }

    static void S(string dir, string fn, int t, string n, string d, int bC, int bG, int eC, int eG, params (SkillEffectSO e, float v, int f, int dr, float c)[] ents)
    {
        string path = $"{dir}/{fn}.asset";
        var so = AssetDatabase.LoadAssetAtPath<SkillSO>(path);
        if (so == null) { so = ScriptableObject.CreateInstance<SkillSO>(); AssetDatabase.CreateAsset(so, path); }

        so.skillName = n; so.type = (SkillType)t; so.description = d;
        var sO = new SerializedObject(so);
        SetProp(sO, "battlePointCost", bC); SetProp(sO, "battlePointGain", bG); SetProp(sO, "energyCost", eC); SetProp(sO, "energyGain", eG);
        sO.ApplyModifiedProperties();

        so.effects = new List<EffectEntry>();
        foreach (var (e, v, f, dr, c) in ents)
        {
            var ne = new EffectEntry { effect = e, value = v, duration = dr, applyChance = c };
            var fi = typeof(EffectEntry).GetField("flatBonus");
            if (fi != null) fi.SetValue(ne, f);
            so.effects.Add(ne);
        }
        EditorUtility.SetDirty(so);
    }

    static void SetProp(SerializedObject o, string n, int v) { var p = o.FindProperty(n); if (p != null) p.intValue = v; }
    static void EnsureDir(string p) { if (AssetDatabase.IsValidFolder(p)) return; AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\', '/'), Path.GetFileName(p)); }
}
#endif