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
        S(WEAPON_DIR, "W_Com_Pebble", Act, "Pebble Toss", "120% ATK + 75", 1, 0, 0, 25, (d1, 1.2f, 75, 0, 100));
        S(WEAPON_DIR, "W_Com_Twin", Act, "Twin Pebble", "200% ATK + 150 (2 hit)", 1, 0, 0, 25, (d1, 2.0f, 150, 0, 100));
        S(WEAPON_DIR, "W_Com_Mud", Act, "Mud Splash", "105% ATK + 85 AoE", 1, 0, 0, 25, (dA, 1.05f, 85, 0, 100));
        S(WEAPON_DIR, "W_Com_Slap", Act, "Slime Slap", "130% ATK + 80, 10% -5 SPD", 1, 0, 0, 25, (d1, 1.3f, 80, 0, 100), (dbSpd1, 5f, 0, 1, 10));
        S(WEAPON_DIR, "W_Com_Glue", Act, "Sticky Shot", "115% ATK + 90, 20% -5% ATK", 1, 0, 0, 25, (d1, 1.15f, 90, 0, 100), (dbAtk1, 0.05f, 0, 2, 20));
        S(WEAPON_DIR, "W_Com_Bump", Act, "Reckless Bump", "150% ATK + 75", 1, 0, 0, 25, (d1, 1.5f, 75, 0, 100));

        // UNCOMMON
        S(WEAPON_DIR, "W_Unc_TwinFang", Act, "Twin Fang", "155% ATK + 150", 1, 0, 0, 25, (d1, 1.55f, 150, 0, 100));
        S(WEAPON_DIR, "W_Unc_Venom", Act, "Venom Nip", "140% ATK + 135, 30% Độc", 1, 0, 0, 25, (d1, 1.4f, 135, 0, 100), (psn1, 0.03f, 0, 2, 30));
        S(WEAPON_DIR, "W_Unc_Gale", Act, "Gale Slash", "125% ATK + 150 AoE", 1, 0, 0, 25, (dA, 1.25f, 150, 0, 100));
        S(WEAPON_DIR, "W_Unc_Awl", Act, "Piercing Jab", "150% ATK + 140", 1, 0, 0, 25, (d1, 1.5f, 140, 0, 100));
        S(WEAPON_DIR, "W_Unc_Rip", Act, "Rending Claw", "145% ATK + 140, 25% Bleed", 1, 0, 0, 25, (d1, 1.45f, 140, 0, 100), (bld1, 0.02f, 0, 2, 25));
        S(WEAPON_DIR, "W_Unc_Frenzy", Act, "Frenzy Bite", "260% ATK + 300, Hồi 3%x2", 1, 0, 0, 25, (d1, 2.6f, 300, 0, 100), (hS, 0.06f, 0, 0, 100));

        // RARE
        S(WEAPON_DIR, "W_Rar_Mudfang_A", Act, "Mud Punch", "160% ATK + 300", 1, 0, 0, 25, (d1, 1.6f, 300, 0, 100));
        S(WEAPON_DIR, "W_Rar_Mudfang_U", Ult, "Titan Crash", "280% ATK + 700", 0, 0, 100, 0, (d1, 2.8f, 700, 0, 100));
        S(WEAPON_DIR, "W_Rar_Tide_A", Act, "Water Splash", "130% ATK + 300 AoE", 1, 0, 0, 25, (dA, 1.3f, 300, 0, 100));
        S(WEAPON_DIR, "W_Rar_Tide_U", Ult, "Torrential Roar", "240% ATK + 650 AoE, -10 SPD", 0, 0, 100, 0, (dA, 2.4f, 650, 0, 100), (dbSpdA, 10f, 0, 2, 100));
        S(WEAPON_DIR, "W_Rar_Storm_A", Act, "Lightning Blade", "175% ATK + 260, 20% Choáng", 1, 0, 0, 25, (d1, 1.75f, 260, 0, 100), (stun1, 1f, 0, 1, 20));
        S(WEAPON_DIR, "W_Rar_Storm_U", Ult, "Thunder Cage", "200% ATK + 550, 100% Choáng", 0, 0, 100, 0, (d1, 2.0f, 550, 0, 100), (stun1, 1f, 0, 2, 100));
        S(WEAPON_DIR, "W_Rar_Blood_A", Act, "Blood Rend", "165% ATK + 260, 35% Bleed", 1, 0, 0, 25, (d1, 1.65f, 260, 0, 100), (bld1, 0.03f, 0, 2, 35));
        S(WEAPON_DIR, "W_Rar_Blood_U", Ult, "Sanguine Feast", "220% ATK + 600, Hồi team", 0, 0, 100, 0, (d1, 2.2f, 600, 0, 100), (hA, 0.4f, 0, 0, 100));
        S(WEAPON_DIR, "W_Rar_Aegis_A", Act, "Spike Volley", "135% ATK + 260 AoE", 1, 0, 0, 25, (dA, 1.35f, 260, 0, 100));
        S(WEAPON_DIR, "W_Rar_Aegis_U", Ult, "Bulwark Surge", "Lá chắn 15% HP toàn đội", 0, 0, 100, 0, (shldA, 0.15f, 300, 2, 100));
        S(WEAPON_DIR, "W_Rar_Rally_A", Act, "Piercing Wave", "150% ATK + 280", 1, 0, 0, 25, (d1, 1.5f, 280, 0, 100));
        S(WEAPON_DIR, "W_Rar_Rally_U", Ult, "War Anthem", "+15% ATK team", 0, 0, 100, 0, (bAtkA, 0.15f, 0, 3, 100));

        // SUPER RARE
        S(WEAPON_DIR, "W_SR_Dragon_A", Act, "Dragon Slayer Blade", "175% ATK + 450", 1, 0, 0, 25, (d1, 1.75f, 450, 0, 100));
        S(WEAPON_DIR, "W_SR_Dragon_U", Ult, "Dragon's Demise", "330% ATK + 1000", 0, 0, 100, 0, (d1, 3.3f, 1000, 0, 100));
        S(WEAPON_DIR, "W_SR_Seismic_A", Act, "Earthquake", "150% ATK + 450 AoE", 1, 0, 0, 25, (dA, 1.5f, 450, 0, 100), (dbSpdA, 10f, 0, 2, 20));
        S(WEAPON_DIR, "W_SR_Seismic_U", Ult, "Earthshatter Roar", "300% ATK + 900 AoE", 0, 0, 100, 0, (dA, 3.0f, 900, 0, 100), (dbSpdA, 15f, 0, 2, 100));
        S(WEAPON_DIR, "W_SR_Glacier_A", Act, "Eternal Frost", "160% ATK + 440 AoE, 30% Freeze", 1, 0, 0, 25, (dA, 1.6f, 440, 0, 100), (stunA, 1f, 0, 1, 30));
        S(WEAPON_DIR, "W_SR_Glacier_U", Ult, "Absolute Zero", "200% ATK + 800 AoE, Freeze", 0, 0, 100, 0, (dA, 2.0f, 800, 0, 100), (stunA, 1f, 0, 2, 100));
        S(WEAPON_DIR, "W_SR_Soul_A", Act, "Soul Drinker", "180% ATK + 440", 1, 0, 0, 25, (d1, 1.8f, 440, 0, 100), (hS, 0.1f, 0, 0, 100));
        S(WEAPON_DIR, "W_SR_Soul_U", Ult, "Harvest of Souls", "300% ATK + 850 AoE", 0, 0, 100, 0, (dA, 3.0f, 850, 0, 100), (hA, 0.5f, 0, 0, 100));
        S(WEAPON_DIR, "W_SR_Bastion_A", Act, "Guard Breaker", "165% ATK + 440", 1, 0, 0, 25, (d1, 1.65f, 440, 0, 100));
        S(WEAPON_DIR, "W_SR_Bastion_U", Ult, "Fortress Wall", "Lá chắn 20% HP", 0, 0, 100, 0, (shldA, 0.2f, 400, 3, 100));
        S(WEAPON_DIR, "W_SR_Warlord_A", Act, "Raging Sandstorm", "190% ATK + 470, 40% Choáng", 1, 0, 0, 25, (d1, 1.9f, 470, 0, 100), (stun1, 1f, 0, 1, 40));
        S(WEAPON_DIR, "W_SR_Warlord_U", Ult, "Warlord's Ascension", "+18% ATK team", 0, 0, 100, 0, (bAtkA, 0.18f, 0, 3, 100));

        // ULTRA RARE
        S(WEAPON_DIR, "W_UR_Thunder_A", Act, "Heaven's Thunder Strike", "200% ATK + 620", 2, 0, 0, 25, (d1, 2.0f, 620, 0, 100), (stun1, 1f, 0, 1, 50));
        S(WEAPON_DIR, "W_UR_Thunder_U", Ult, "Cataclysm Verdict", "400% ATK + 1350", 0, 0, 100, 0, (d1, 4.0f, 1350, 0, 100), (stun1, 1f, 0, 2, 80));
        S(WEAPON_DIR, "W_UR_Void_A", Act, "Dark Vortex", "175% ATK + 600 AoE", 2, 0, 0, 25, (dA, 1.75f, 600, 0, 100), (psn1, 0.07f, 0, 2, 100));
        S(WEAPON_DIR, "W_UR_Void_U", Ult, "Black Hole Collapse", "360% ATK + 1250 AoE", 0, 0, 100, 0, (dA, 3.6f, 1250, 0, 100), (psn1, 0.1f, 0, 2, 100));
        S(WEAPON_DIR, "W_UR_Frost_A", Act, "Frostfire Lance", "200% ATK + 600", 1, 0, 0, 25, (d1, 2.0f, 600, 0, 100), (stun1, 1f, 0, 1, 40));
        S(WEAPON_DIR, "W_UR_Frost_U", Ult, "Glacial Prison", "260% ATK + 1100 AoE", 0, 0, 100, 0, (dA, 2.6f, 1100, 0, 100), (stunA, 1f, 0, 2, 100), (avRtA, 40f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Vamp_A", Act, "Vampiric Onslaught", "205% ATK + 630", 2, 0, 0, 25, (d1, 2.05f, 630, 0, 100), (hS, 0.15f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Vamp_U", Ult, "Eternal Banquet", "350% ATK + 1200", 0, 0, 100, 0, (d1, 3.5f, 1200, 0, 100), (hA, 0.6f, 0, 0, 100), (clnsA, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Aegis_A", Act, "Shield Bash", "185% ATK + 600", 2, 0, 0, 25, (d1, 1.85f, 600, 0, 100));
        S(WEAPON_DIR, "W_UR_Aegis_U", Ult, "Sanctuary of Light", "Lá chắn 25%", 0, 0, 100, 0, (shldA, 0.25f, 0, 3, 100), (hA, 0.12f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Sov_A", Act, "Storm of Blades", "165% ATK + 600 AoE", 1, 0, 0, 25, (dA, 1.65f, 600, 0, 100), (bldA, 0.03f, 0, 2, 30));
        S(WEAPON_DIR, "W_UR_Sov_U", Ult, "Imperial Overdrive", "+20% ATK, Năng lượng", 0, 0, 100, 0, (bAtkA, 0.2f, 0, 3, 100), (enGnA, 25f, 0, 0, 100));

        // LEGENDARY
        S(WEAPON_DIR, "W_Leg_Star_A", Act, "Starlight Blade", "225% ATK + 1000", 2, 0, 0, 25, (d1, 2.25f, 1000, 0, 100), (stun1, 1f, 0, 1, 75));
        S(WEAPON_DIR, "W_Leg_Star_U", Ult, "Supernova Edge", "450% ATK + 2000", 0, 0, 100, 0, (d1, 4.5f, 2000, 0, 100));
        S(WEAPON_DIR, "W_Leg_Deluge_A", Act, "Great Deluge", "190% ATK + 950 AoE", 2, 0, 0, 25, (dA, 1.9f, 950, 0, 100), (psn1, 0.06f, 0, 2, 100));
        S(WEAPON_DIR, "W_Leg_Deluge_U", Ult, "Genesis Starfall", "400% ATK + 1900 AoE", 0, 0, 100, 0, (dA, 4.0f, 1900, 0, 100), (stunA, 1f, 0, 2, 80));
        S(WEAPON_DIR, "W_Leg_Chrono_A", Act, "Time Frost", "200% ATK + 950 AoE", 2, 0, 0, 25, (dA, 2.0f, 950, 0, 100), (stunA, 1f, 0, 1, 40));
        S(WEAPON_DIR, "W_Leg_Chrono_U", Ult, "Temporal Lock", "300% ATK + 1500 AoE", 0, 0, 100, 0, (dA, 3.0f, 1500, 0, 100), (stunA, 1f, 0, 2, 100));
        S(WEAPON_DIR, "W_Leg_Phoen_A", Act, "Dragon Feather Soul Reaper", "235% ATK + 1050", 2, 0, 0, 25, (d1, 2.35f, 1050, 0, 100), (stun1, 1f, 0, 2, 85));
        S(WEAPON_DIR, "W_Leg_Phoen_U", Ult, "Phoenix Rebirth", "380% ATK AoE, Revive", 0, 0, 100, 0, (dA, 3.8f, 1700, 0, 100), (rvv1, 0.35f, 0, 0, 100), (hA, 0.2f, 0, 0, 100));
        S(WEAPON_DIR, "W_Leg_Titan_A", Act, "Void Executioner", "230% ATK + 1000", 2, 0, 0, 25, (d1, 2.3f, 1000, 0, 100));
        S(WEAPON_DIR, "W_Leg_Titan_U", Ult, "Aegis Eternal", "Lá chắn 30%", 0, 0, 100, 0, (shldA, 0.3f, 0, 3, 100), (bDefA, 0.15f, 0, 3, 100));
        S(WEAPON_DIR, "W_Leg_Celest_A", Act, "Celestial Tempest", "200% ATK + 950 AoE", 1, 0, 0, 25, (dA, 2.0f, 950, 0, 100), (stunA, 1f, 0, 1, 50));
        S(WEAPON_DIR, "W_Leg_Celest_U", Ult, "Divine Coronation", "+22% ATK team", 0, 0, 100, 0, (bAtkA, 0.22f, 0, 3, 100), (enGnA, 30f, 0, 0, 100));

        // MYTHIC
        S(WEAPON_DIR, "W_Myt_World_A", Act, "Apocalyptic Annihilation", "250% ATK + 1500", 2, 0, 0, 25, (d1, 2.5f, 1500, 0, 100));
        S(WEAPON_DIR, "W_Myt_World_U", Ult, "Extinction Protocol", "550% ATK + 2750", 0, 0, 100, 0, (d1, 5.5f, 2750, 0, 100));
        S(WEAPON_DIR, "W_Myt_Apoc_A", Act, "Wrath of Heaven and Earth", "230% ATK + 1450 AoE", 3, 0, 0, 25, (dA, 2.3f, 1450, 0, 100), (stunA, 1f, 0, 2, 80));
        S(WEAPON_DIR, "W_Myt_Apoc_U", Ult, "Ultimate Psychic Surge", "520% ATK + 2750 AoE", 0, 0, 100, 0, (dA, 5.2f, 2750, 0, 100), (stunA, 1f, 0, 3, 100));
        S(WEAPON_DIR, "W_Myt_Obli_A", Act, "Blade of the Void", "260% ATK + 1550", 2, 0, 0, 25, (d1, 2.6f, 1550, 0, 100), (stun1, 1f, 0, 2, 85));
        S(WEAPON_DIR, "W_Myt_Obli_U", Ult, "Absolute Silence", "400% ATK + 2200 AoE", 0, 0, 100, 0, (dA, 4.0f, 2200, 0, 100), (stunA, 1f, 0, 3, 100));
        S(WEAPON_DIR, "W_Myt_Eter_A", Act, "Eternal Cataclysm", "205% ATK + 1500 AoE", 2, 0, 0, 25, (dA, 2.05f, 1500, 0, 100), (hS, 0.1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Myt_Eter_U", Ult, "Rite of Immortality", "450% ATK AoE, Revive All", 0, 0, 100, 0, (dA, 4.5f, 2400, 0, 100), (rvvA, 0.3f, 0, 0, 100), (hA, 0.3f, 0, 0, 100));
        S(WEAPON_DIR, "W_Myt_Gen_A", Act, "Ragnarok Descent", "290% ATK + 1600", 3, 0, 0, 25, (d1, 2.9f, 1600, 0, 100));
        S(WEAPON_DIR, "W_Myt_Gen_U", Ult, "Impervious Genesis", "Lá chắn 40%", 0, 0, 100, 0, (shldA, 0.4f, 0, 3, 100), (clnsA, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Myt_Emp_A", Act, "Judgment Ray", "240% ATK + 1500 AoE", 2, 0, 0, 25, (dA, 2.4f, 1500, 0, 100), (stunA, 1f, 0, 1, 60));
        S(WEAPON_DIR, "W_Myt_Emp_U", Ult, "Empyrean Ascension", "+30% ATK, Full Năng lượng", 0, 0, 100, 0, (bAtkA, 0.3f, 0, 3, 100), (enGnA, 100f, 0, 0, 100));

        // SECRET
        S(WEAPON_DIR, "W_Sec_Judg_A", Act, "Celestial Judgment", "200% ATK + 620", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100), (stun1, 1f, 0, 1, 100));
        S(WEAPON_DIR, "W_Sec_Judg_U", Ult, "Fate Reversal", "200% ATK + 620 AoE", 0, 0, 100, 0, (dA, 2.0f, 620, 0, 100), (dsplA, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Obli_A", Act, "Genesis Oblivion", "200% ATK + 620", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100));
        S(WEAPON_DIR, "W_Sec_Obli_U", Ult, "Null Genesis", "260% ATK + 900", 0, 0, 100, 0, (d1, 2.6f, 900, 0, 100), (dspl1, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Ecli_A", Act, "Eclipse of Eternity", "200% ATK + 620 AoE", 2, 0, 0, 25, (dA, 2.0f, 620, 0, 100), (psn1, 0.08f, 0, 3, 100));
        S(WEAPON_DIR, "W_Sec_Ecli_U", Ult, "Eternal Eclipse", "200% ATK + 700 AoE", 0, 0, 100, 0, (dA, 2.0f, 700, 0, 100), (hA, 0.4f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Chro_A", Act, "Zero-Point Severance", "200% ATK + 620", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100));
        S(WEAPON_DIR, "W_Sec_Chro_U", Ult, "Time Stop", "Team đi thêm 1 lượt", 0, 0, 100, 0, (avAdA, 100f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Req_A", Act, "Requiem Protocol", "170% ATK + 600 AoE", 1, 0, 0, 25, (dA, 1.7f, 600, 0, 100));
        S(WEAPON_DIR, "W_Sec_Req_U", Ult, "Requiem Aegis", "Bất tử 1 lượt", 0, 0, 100, 0, (shldA, 9.9f, 0, 1, 100), (enGnA, 100f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Sov_A", Act, "Dominion Strike", "200% ATK + 620", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100));
        S(WEAPON_DIR, "W_Sec_Sov_U", Ult, "Absolute Dominion", "-40% ATK/SPD Boss", 0, 0, 100, 0, (dbAtk1, 0.4f, 0, 2, 100), (dbSpd1, 40f, 0, 2, 100));

        // ── 3. HEAD SKILLS (Mũ - Hỗ trợ tốn ĐCK) ──────────────────────────────
        S(HAT_DIR, "H_Com_Focus", Act, "Minor Focus", "+5% ATK, +4 SPD", 1, 0, 0, 25, (bAtkS, 0.05f, 0, 2, 100), (bSpdS, 4f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Guard", Act, "Quick Guard", "+8% DEF", 1, 0, 0, 25, (bDefS, 0.08f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Dress", Act, "Field Dressing", "Hồi 8% HP", 1, 0, 0, 25, (h1, 0.08f, 0, 0, 100));
        S(HAT_DIR, "H_Com_Warm", Act, "Warm Up", "+6 SPD", 1, 0, 0, 25, (bSpdS, 6f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Taunt", Act, "Taunt Cry", "-5% ATK địch", 1, 0, 0, 25, (dbAtk1, 0.05f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Hobble", Act, "Hobble Shot", "-4 SPD địch", 1, 0, 0, 25, (dbSpd1, 4f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Steady", Act, "Steady Aim", "+5% Crit Rate", 1, 0, 0, 25, (bAtkS, 0.05f, 0, 2, 100)); // Map logic

        S(HAT_DIR, "H_Unc_Chant", Act, "Battle Chant", "+7% ATK, +3 SPD team", 1, 0, 0, 25, (bAtkA, 0.07f, 0, 2, 100), (bSpdA, 3f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Guard", Act, "Guard Formation", "+8% DEF team", 1, 0, 0, 25, (bDefA, 0.08f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Mend", Act, "Mending Wave", "Hồi 6% HP team", 1, 0, 0, 25, (hA, 0.06f, 0, 0, 100));
        S(HAT_DIR, "H_Unc_Haste", Act, "Haste Cry", "+8 SPD team", 1, 0, 0, 25, (bSpdA, 8f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Weak", Act, "Weakening Shout", "-7% ATK toàn địch", 1, 0, 0, 25, (dbAtkA, 0.07f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Sap", Act, "Sap Speed", "-8 SPD toàn địch", 1, 0, 0, 25, (dbSpdA, 8f, 0, 2, 100), (avRt1, 15f, 0, 0, 100));
        S(HAT_DIR, "H_Unc_Hunt", Act, "Hunter's Mark", "Địch nhận thêm sát thương", 1, 0, 0, 25, (dbDef1, 0.1f, 0, 2, 100));

        S(HAT_DIR, "H_Rar_Chant", Act, "War Chant", "+10% ATK, +5 SPD team", 1, 0, 0, 25, (bAtkA, 0.1f, 0, 2, 100), (bSpdA, 5f, 0, 2, 100));
        S(HAT_DIR, "H_Rar_Wall", Act, "Stone Wall", "+12% DEF team", 1, 0, 0, 25, (bDefA, 0.12f, 0, 2, 100));
        S(HAT_DIR, "H_Rar_Light", Act, "Healing Light", "Hồi 12% HP, Gỡ debuff", 1, 0, 0, 25, (h1, 0.12f, 0, 0, 100), (clns1, 1f, 0, 0, 100));
        S(HAT_DIR, "H_Rar_Quick", Act, "Quickstep Cry", "+10 SPD đồng minh", 1, 0, 0, 25, (bSpdS, 10f, 0, 2, 100)); // Target ally
        S(HAT_DIR, "H_Rar_Frost", Act, "Frost Hex", "-10 SPD địch, 20% Freeze", 1, 0, 0, 25, (dbSpdA, 10f, 0, 2, 100), (stun1, 1f, 0, 1, 20));
        S(HAT_DIR, "H_Rar_Curse", Act, "Curse of Weakness", "-10% ATK/DEF", 1, 0, 0, 25, (dbAtk1, 0.1f, 0, 3, 100), (dbDef1, 0.08f, 0, 3, 100));
        S(HAT_DIR, "H_Rar_Rally", Act, "Rallying Horn", "+8 Năng lượng team", 1, 0, 0, 25, (enGnA, 8f, 0, 0, 100));

        S(HAT_DIR, "H_SR_Banner", Act, "Rallying Banner", "+12% ATK, +6% DEF/SPD", 1, 0, 0, 25, (bAtkA, 0.12f, 0, 3, 100), (bDefA, 0.06f, 0, 3, 100));
        S(HAT_DIR, "H_SR_Sanct", Act, "Sanctuary Ward", "Lá chắn 8% HP team", 1, 0, 0, 25, (shldA, 0.08f, 0, 2, 100));
        S(HAT_DIR, "H_SR_Purge", Act, "Purge Light", "Gỡ Debuff, Hồi 15%", 1, 0, 0, 25, (clns1, 1f, 0, 0, 100), (h1, 0.15f, 0, 0, 100));
        S(HAT_DIR, "H_SR_Order", Act, "Battle Order", "Kéo lượt 50%, +12% ATK", 1, 0, 0, 25, (avAd1, 50f, 0, 0, 100), (bAtkS, 0.12f, 0, 1, 100));
        S(HAT_DIR, "H_SR_Doom", Act, "Doom Brand", "-10% DEF", 1, 0, 0, 25, (dbDef1, 0.1f, 0, 3, 100));
        S(HAT_DIR, "H_SR_Blizz", Act, "Blizzard Field", "-15 SPD toàn địch", 1, 0, 0, 25, (dbSpdA, 15f, 0, 2, 100), (avRtA, 25f, 0, 0, 100));
        S(HAT_DIR, "H_SR_Grave", Act, "Grave Seal", "Khoá Buff", 1, 0, 0, 25, (dspl1, 1f, 0, 2, 100));
        S(HAT_DIR, "H_SR_Overc", Act, "Overcharge", "+12 Năng lượng team", 1, 0, 0, 25, (enGnA, 12f, 0, 0, 100));

        S(HAT_DIR, "H_UR_Warlord", Act, "Warlord's Command", "+15% ATK, +8 SPD", 1, 0, 0, 25, (bAtkA, 0.15f, 0, 3, 100), (bSpdA, 8f, 0, 3, 100));
        S(HAT_DIR, "H_UR_EterP", Act, "Eternal Purification", "Hồi 8% HP, Gỡ debuff", 1, 0, 0, 25, (hA, 0.08f, 0, 0, 100), (clnsA, 1f, 0, 0, 100));
        S(HAT_DIR, "H_UR_Nightm", Act, "Eternal Nightmare", "-20% ATK/DEF/SPD", 1, 0, 0, 25, (dbAtkA, 0.2f, 0, 3, 100), (dbDefA, 0.2f, 0, 3, 100));
        S(HAT_DIR, "H_UR_Dream", Act, "Dream Overture", "Kéo lượt 75%", 1, 0, 0, 25, (avAd1, 75f, 0, 0, 100));
        S(HAT_DIR, "H_UR_TimeW", Act, "Time Warp", "+12 SPD team", 1, 0, 0, 25, (bSpdA, 12f, 0, 3, 100));
        S(HAT_DIR, "H_UR_Aegis", Act, "Aegis Barrier", "Lá chắn 12% team", 1, 0, 0, 25, (shldA, 0.12f, 0, 3, 100));
        S(HAT_DIR, "H_UR_DoomS", Act, "Doom Sentence", "Địch nhận thêm ST", 1, 0, 0, 25, (dbDefA, 0.18f, 0, 3, 100));
        S(HAT_DIR, "H_UR_Hymn", Act, "Energizing Hymn", "+30 Năng lượng", 1, 0, 0, 25, (enGnA, 30f, 0, 0, 100));

        S(HAT_DIR, "H_Leg_Holy", Act, "Absolute Holy Domain", "Hồi 10%, +10% Stats", 1, 0, 0, 25, (hA, 0.1f, 0, 0, 100), (bAtkA, 0.1f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_Valk", Act, "Valkyrie's Blessing", "+18% ATK, +10 SPD", 1, 0, 0, 25, (bAtkA, 0.18f, 0, 3, 100), (bSpdA, 10f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_Dooms", Act, "Doomsday Decree", "-25% ATK/DEF/SPD", 1, 0, 0, 25, (dbAtkA, 0.25f, 0, 3, 100), (dbSpdA, 25f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_SovM", Act, "Sovereign March", "Kéo 100%, +25% ATK", 1, 0, 0, 25, (avAd1, 100f, 0, 0, 100), (bAtkS, 0.25f, 0, 2, 100));
        S(HAT_DIR, "H_Leg_Tempo", Act, "Tempo Overdrive", "+15 SPD team", 1, 0, 0, 25, (bSpdA, 15f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_Guard", Act, "Guardian's Sanctuary", "Lá chắn 18%", 1, 0, 0, 25, (shldA, 0.18f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_GraveE", Act, "Grave Edict", "Khoá Buff, đẩy lùi 30%", 1, 0, 0, 25, (dsplA, 1f, 0, 2, 100), (avRtA, 30f, 0, 0, 100));
        S(HAT_DIR, "H_Leg_SecW", Act, "Second Wind", "Hồi sinh 30%", 1, 0, 0, 25, (rvv1, 0.3f, 0, 0, 100));

        S(HAT_DIR, "H_Myt_Emp", Act, "Empyrean Coronation", "+25% ATK, +12 SPD", 1, 0, 0, 25, (bAtkA, 0.25f, 0, 3, 100), (bSpdA, 12f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_DivA", Act, "Divine Ascension", "+12% Stats team", 1, 0, 0, 25, (bAtkA, 0.12f, 0, 3, 100), (bDefA, 0.12f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_WrldS", Act, "World Sanction", "-30% Stats địch", 1, 0, 0, 25, (dbAtkA, 0.3f, 0, 3, 100), (dbSpdA, 30f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_CelO", Act, "Celestial Overture", "Kéo 100%, +8 SPD", 1, 0, 0, 25, (avAd1, 100f, 0, 0, 100), (bSpdA, 8f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_ChrO", Act, "Chrono Overdrive", "+20 SPD team", 1, 0, 0, 25, (bSpdA, 20f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_WrldR", Act, "World Requiem", "-40% SPD, Lùi 40%", 1, 0, 0, 25, (dbSpdA, 40f, 0, 3, 100), (avRtA, 40f, 0, 0, 100));
        S(HAT_DIR, "H_Myt_Bast", Act, "Bastion Eternal", "Lá chắn 25%", 1, 0, 0, 25, (shldA, 0.25f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_MassR", Act, "Mass Resurrection", "Hồi sinh team 25%", 2, 0, 0, 25, (rvvA, 0.25f, 0, 0, 100));

        S(HAT_DIR, "H_Sec_Dict", Act, "Dictator's Decree", "Nâng trần ĐCK", 1, 0, 0, 25, (enGnA, 1f, 0, 0, 100)); // Map qua Script
        S(HAT_DIR, "H_Sec_TimeD", Act, "Time Dilation", "Extra turn", 1, 0, 0, 25, (avAd1, 100f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_GrandO", Act, "Grand Overture", "Kéo team 100%", 1, 0, 0, 25, (avAdA, 100f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_ChroS", Act, "Chrono Seizure", "Đẩy lùi 50%, -20% SPD", 1, 0, 0, 25, (avRtA, 50f, 0, 0, 100), (dbSpdA, 20f, 0, 2, 100));
        S(HAT_DIR, "H_Sec_SovG", Act, "Sovereign's Grace", "Hồi đầy Năng lượng", 1, 0, 0, 25, (enGnA, 100f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_AbsE", Act, "Absolute Edict", "Tất trúng, 100% CDMG", 1, 0, 0, 25, (bAtkA, 1f, 0, 3, 100));
        S(HAT_DIR, "H_Sec_MindH", Act, "Mind Hijack", "Chiếm quyền / -30% ATK", 1, 0, 0, 25, (dbAtk1, 0.3f, 0, 2, 100));

        // ── 4. BODY SKILLS (Áo - Nội tại Passive) ─────────────────────────────
        S(BODY_DIR, "B_Com_Stick", Psv, "Sticky Slime", "Giảm 5% sát thương", 0, 0, 0, 0, (bDefS, 0.05f, 0, 0, 100));
        S(BODY_DIR, "B_Com_Thin", Psv, "Thin Hide", "+4% HP", 0, 0, 0, 0, (bDefS, 0.04f, 0, 0, 100));
        S(BODY_DIR, "B_Com_SlowM", Psv, "Slow Metabolism", "Hồi 1% HP", 0, 0, 0, 0, (hS, 0.01f, 0, 0, 100));
        S(BODY_DIR, "B_Com_SoftB", Psv, "Soft Body", "Giảm ST chí mạng", 0, 0, 0, 0, (bDefS, 0.1f, 0, 0, 100));
        S(BODY_DIR, "B_Com_Light", Psv, "Light Step", "+3 SPD", 0, 0, 0, 0, (bSpdS, 3f, 0, 0, 100));
        S(BODY_DIR, "B_Com_Spike", Psv, "Minor Spikes", "Phản 3% ST", 0, 0, 0, 0, (bDefS, 0.03f, 0, 0, 100));

        S(BODY_DIR, "B_Unc_IronW", Psv, "Iron Will", "Hồi 1.5% HP", 0, 0, 0, 0, (hS, 0.015f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Stone", Psv, "Stone Skin", "+6% DEF", 0, 0, 0, 0, (bDefS, 0.06f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Vital", Psv, "Vital Growth", "+6% HP", 0, 0, 0, 0, (bDefS, 0.06f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Adren", Psv, "Adrenaline", "HP <50% -> +5% ATK", 0, 0, 0, 0, (bAtkS, 0.05f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Corro", Psv, "Corrosive Slime", "Kẻ địch -3% DEF", 0, 0, 0, 0, (dbDef1, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Barbe", Psv, "Barbed Coat", "Phản 5% ST", 0, 0, 0, 0, (bDefS, 0.05f, 0, 0, 100));

        S(BODY_DIR, "B_Rar_StoneA", Psv, "Stone Armor", "+3% DEF khi yếu", 0, 0, 0, 0, (bDefS, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_ReinT", Psv, "Reinforced Thorn", "+3% ATK, Phản ST", 0, 0, 0, 0, (bAtkS, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_Muscl", Psv, "Muscle Enhance", "+3% Xuyên giáp team", 0, 0, 0, 0, (bAtkA, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_LifeI", Psv, "Life Infusion", "Hồi 2% HP team", 0, 0, 0, 0, (hA, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_Berse", Psv, "Berserker Blood", "Mất máu tăng công", 0, 0, 0, 0, (bAtkS, 0.16f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_Guard", Psv, "Guardian Barrier", "Địch gần -3% ATK", 0, 0, 0, 0, (dbAtk1, 0.03f, 0, 0, 100));

        S(BODY_DIR, "B_SR_Dragon", Psv, "Dragon Scale", "+4% ATK/DEF/SPD", 0, 0, 0, 0, (bAtkS, 0.04f, 0, 0, 100), (bSpdS, 4f, 0, 0, 100));
        S(BODY_DIR, "B_SR_WarrF", Psv, "Warrior's Fury", "+2% DEF team", 0, 0, 0, 0, (bDefA, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_SR_RestA", Psv, "Restoration Aura", "Hồi 2% HP team", 0, 0, 0, 0, (hA, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_SR_Diam", Psv, "Diamond Armor", "+4% Xuyên giáp", 0, 0, 0, 0, (bAtkS, 0.04f, 0, 0, 100));
        S(BODY_DIR, "B_SR_Retri", Psv, "Retribution Plate", "Phản 8% ST", 0, 0, 0, 0, (bDefS, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_SR_Momen", Psv, "Momentum Core", "Đánh thường +ATK", 0, 0, 0, 0, (bAtkS, 0.09f, 0, 0, 100));

        S(BODY_DIR, "B_UR_NightS", Psv, "Nightmare Shackles", "+6% DEF, +3% HP", 0, 0, 0, 0, (bDefS, 0.06f, 0, 0, 100));
        S(BODY_DIR, "B_UR_DivGA", Psv, "Divine Guardian", "Hồi HP team khi bị đánh", 0, 0, 0, 0, (hA, 0.05f, 0, 0, 100));
        S(BODY_DIR, "B_UR_EterS", Psv, "Eternal Suppress", "+8% DEF, -4% ST", 0, 0, 0, 0, (bDefS, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_UR_VampC", Psv, "Vampiric Core", "Hút máu 8%", 0, 0, 0, 0, (hS, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_UR_SpdSe", Psv, "Speed Seal", "+5% DEF, +6 SPD", 0, 0, 0, 0, (bSpdS, 6f, 0, 0, 100));
        S(BODY_DIR, "B_UR_BulwA", Psv, "Bulwark Aura", "Team -5% ST", 0, 0, 0, 0, (bDefA, 0.05f, 0, 0, 100));

        S(BODY_DIR, "B_Leg_ArmI", Psv, "Armor Immortality", "Lần đầu gục hồi 10%", 0, 0, 0, 0, (rvv1, 0.1f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_HeavA", Psv, "Heaven's Aegis", "+10% DEF", 0, 0, 0, 0, (bDefS, 0.1f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_DivR", Psv, "Divine Resurrect", "Địch -8% SPD", 0, 0, 0, 0, (dbSpdA, 8f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_EterB", Psv, "Eternal Bulwark", "Tích luỹ DEF", 0, 0, 0, 0, (bDefS, 0.2f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_Blood", Psv, "Bloodlord Domin.", "Hút máu 12%", 0, 0, 0, 0, (hS, 0.12f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_AegF", Psv, "Aegis of Fallen", "Đồng minh gục +ATK", 0, 0, 0, 0, (bAtkA, 0.1f, 0, 0, 100));

        S(BODY_DIR, "B_Myt_HeavS", Psv, "Heaven Suppression", "+10% DEF, Hồi 2%", 0, 0, 0, 0, (bDefS, 0.1f, 0, 0, 100), (hS, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_DivI", Psv, "Divine Incarnate", "+8% ATK/DEF team", 0, 0, 0, 0, (bAtkA, 0.08f, 0, 0, 100), (bDefA, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_ImmS", Psv, "Immortal Sovereign", "Miễn Crit, Hồi 3%", 0, 0, 0, 0, (hS, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_WrldT", Psv, "World Tree Root", "Hồi 3% HP team", 0, 0, 0, 0, (hA, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_TitR", Psv, "Titan's Reprisal", "Phản 15% ST", 0, 0, 0, 0, (bDefS, 0.15f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_AscC", Psv, "Ascendant Core", "HP cao +ATK", 0, 0, 0, 0, (bAtkS, 0.15f, 0, 0, 100));

        S(BODY_DIR, "B_Sec_Dict", Psv, "Dictator", "+2 ĐCK", 0, 0, 0, 0, (enGnA, 2f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Prio", Psv, "Priority Protocol", "Đi đầu, +1 ĐCK", 0, 0, 0, 0, (avAdS, 100f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Hack", Psv, "Hacker", "Tự đánh 270%", 0, 0, 0, 0, (d1, 2.7f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Fina", Psv, "Final Vengeance", "Chết phản 200%", 0, 0, 0, 0, (d1, 2.0f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Over", Psv, "Overclock Engine", "Tiêu ĐCK hoàn ĐCK", 0, 0, 0, 0, (enGnA, 1f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Sing", Psv, "Singularity Field", "-15% Mọi chỉ số địch", 0, 0, 0, 0, (dbAtkA, 0.15f, 0, 0, 100), (dbSpdA, 15f, 0, 0, 100));

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