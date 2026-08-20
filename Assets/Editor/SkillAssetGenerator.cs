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

        // COMMON
        S(WEAPON_DIR, "W_Com_Pebble", Act, "Pebble Sling", "Pebble Toss · 1 BP · 120% ATK + 75, single.", 1, 0, 0, 25, (d1, 1.2f, 75, 0, 100));
        S(WEAPON_DIR, "W_Com_Twin", Act, "Twin Stone", "Twin Pebble · 1 BP · 100% ATK + 75, ×2 hit single (total 200% ATK + 150).", 1, 0, 0, 25, (d1, 2.0f, 150, 0, 100));
        S(WEAPON_DIR, "W_Com_Mud", Act, "Mud Bucket", "Mud Splash · 1 BP · 105% ATK + 85, AoE.", 1, 0, 0, 25, (dA, 1.05f, 85, 0, 100));
        S(WEAPON_DIR, "W_Com_Slap", Act, "Slap Fin", "Slime Slap · 1 BP · 130% ATK + 80, single; 10% −5 SPD 1 turn.", 1, 0, 0, 25, (d1, 1.3f, 80, 0, 100), (dbSpd1, 5f, 0, 1, 10));
        S(WEAPON_DIR, "W_Com_Glue", Act, "Glue Shooter", "Sticky Shot · 1 BP · 115% ATK + 90, single; 20% −5% ATK 2 turn.", 1, 0, 0, 25, (d1, 1.15f, 90, 0, 100), (dbAtk1, 0.05f, 0, 2, 20));
        S(WEAPON_DIR, "W_Com_Bump", Act, "Bump Shell", "Reckless Bump · 1 BP · 150% ATK + 75, single; self +5% damage taken turn sau.", 1, 0, 0, 25, (d1, 1.5f, 75, 0, 100));

        // UNCOMMON
        S(WEAPON_DIR, "W_Unc_TwinFang", Act, "Twin Fang Dagger", "Twin Fang · 1 BP · 155% ATK + 150, single.", 1, 0, 0, 25, (d1, 1.55f, 150, 0, 100));
        S(WEAPON_DIR, "W_Unc_Venom", Act, "Venom Sting", "Venom Nip · 1 BP · 140% ATK + 135, single; 30% Poison (−3% HP max/turn, 2 turn).", 1, 0, 0, 25, (d1, 1.4f, 135, 0, 100), (psn1, 0.03f, 0, 2, 30));
        S(WEAPON_DIR, "W_Unc_Gale", Act, "Gale Edge", "Gale Slash · 1 BP · 125% ATK + 150, AoE.", 1, 0, 0, 25, (dA, 1.25f, 150, 0, 100));
        S(WEAPON_DIR, "W_Unc_Awl", Act, "Awl Pike", "Piercing Jab · 1 BP · 150% ATK + 140, single; pierce 10% armor.", 1, 0, 0, 25, (d1, 1.5f, 140, 0, 100));
        S(WEAPON_DIR, "W_Unc_Rip", Act, "Rip Claw", "Rending Claw · 1 BP · 145% ATK + 140, single; 25% Bleed (−2% HP/turn, 2 turn).", 1, 0, 0, 25, (d1, 1.45f, 140, 0, 100), (bld1, 0.02f, 0, 2, 25));
        S(WEAPON_DIR, "W_Unc_Frenzy", Act, "Frenzy Fang", "Frenzy Bite · 1 BP · 130% ATK + 150, ×2 hit (total 260% ATK + 300); each hit heal 3% as HP.", 1, 0, 0, 25, (d1, 2.6f, 300, 0, 100), (hS, 0.06f, 0, 0, 100));

        // RARE
        S(WEAPON_DIR, "W_Rar_Mudfang_A", Act, "Mudfang Gauntlet", "Mud Punch · 1 BP · 160% ATK + 300, single.", 1, 0, 0, 25, (d1, 1.6f, 300, 0, 100));
        S(WEAPON_DIR, "W_Rar_Mudfang_U", Ult, "Mudfang Gauntlet", "[Single] Titan Crash · 280% ATK + 700, single; pierce 30% armor.", 0, 0, 100, 0, (d1, 2.8f, 700, 0, 100));
        S(WEAPON_DIR, "W_Rar_Tide_A", Act, "Tidecaller Staff", "Water Splash · 1 BP · 130% ATK + 300, AoE.", 1, 0, 0, 25, (dA, 1.3f, 300, 0, 100));
        S(WEAPON_DIR, "W_Rar_Tide_U", Ult, "Tidecaller Staff", "[AoE] Torrential Roar · 240% ATK + 650, AoE; −10 SPD all foes 2 turn.", 0, 0, 100, 0, (dA, 2.4f, 650, 0, 100), (dbSpdA, 10f, 0, 2, 100));
        S(WEAPON_DIR, "W_Rar_Storm_A", Act, "Stormedge Blade", "Lightning Blade · 1 BP · 175% ATK + 260, single; 20% Stun 1 turn.", 1, 0, 0, 25, (d1, 1.75f, 260, 0, 100), (stun1, 1f, 0, 1, 20));
        S(WEAPON_DIR, "W_Rar_Storm_U", Ult, "Stormedge Blade", "[Control] Thunder Cage · 200% ATK + 550, single; 100% Stun 2 turn.", 0, 0, 100, 0, (d1, 2.0f, 550, 0, 100), (stun1, 1f, 0, 2, 100));
        S(WEAPON_DIR, "W_Rar_Blood_A", Act, "Bloodthirn Claw", "Blood Rend · 1 BP · 165% ATK + 260, single; 35% Bleed (−3% HP/turn, 2 turn).", 1, 0, 0, 25, (d1, 1.65f, 260, 0, 100), (bld1, 0.03f, 0, 2, 35));
        S(WEAPON_DIR, "W_Rar_Blood_U", Ult, "Bloodthirn Claw", "[Heal] Sanguine Feast · 220% ATK + 600, single; heal 40% damage as HP team.", 0, 0, 100, 0, (d1, 2.2f, 600, 0, 100), (hA, 0.4f, 0, 0, 100));
        S(WEAPON_DIR, "W_Rar_Aegis_A", Act, "Aegis Core", "Spike Volley · 1 BP · 135% ATK + 260, AoE; pierce 15% armor.", 1, 0, 0, 25, (dA, 1.35f, 260, 0, 100));
        S(WEAPON_DIR, "W_Rar_Aegis_U", Ult, "Aegis Core", "[Guard] Bulwark Surge · 120% ATK + 300, single; shield 15% HP max team 2 turn.", 0, 0, 100, 0, (shldA, 0.15f, 300, 2, 100));
        S(WEAPON_DIR, "W_Rar_Rally_A", Act, "Rallyhorn Trident", "Piercing Wave · 1 BP · 150% ATK + 280, single; pierce 15% armor.", 1, 0, 0, 25, (d1, 1.5f, 280, 0, 100));
        S(WEAPON_DIR, "W_Rar_Rally_U", Ult, "Rallyhorn Trident", "[Buff] War Anthem · +15% ATK & +10% Crit Rate team 3 turn.", 0, 0, 100, 0, (bAtkA, 0.15f, 0, 3, 100));

        // SUPER RARE
        S(WEAPON_DIR, "W_SR_Dragon_A", Act, "Dragonslayer Greatsword", "Dragon Slayer Blade · 1 BP · 175% ATK + 450, single; pierce 20% armor.", 1, 0, 0, 25, (d1, 1.75f, 450, 0, 100));
        S(WEAPON_DIR, "W_SR_Dragon_U", Ult, "Dragonslayer Greatsword", "[Single] Dragon's Demise · 330% ATK + 1000, single; pierce 40% armor; +25% if target <50% HP.", 0, 0, 100, 0, (d1, 3.3f, 1000, 0, 100));
        S(WEAPON_DIR, "W_SR_Seismic_A", Act, "Seismic Maul", "Earthquake · 1 BP · 150% ATK + 450, AoE; 20% −10 SPD 2 turn.", 1, 0, 0, 25, (dA, 1.5f, 450, 0, 100), (dbSpdA, 10f, 0, 2, 20));
        S(WEAPON_DIR, "W_SR_Seismic_U", Ult, "Seismic Maul", "[AoE] Earthshatter Roar · 300% ATK + 900, AoE; pierce 25% armor; −15 SPD 2 turn.", 0, 0, 100, 0, (dA, 3.0f, 900, 0, 100), (dbSpdA, 15f, 0, 2, 100));
        S(WEAPON_DIR, "W_SR_Glacier_A", Act, "Glacier Scepter", "Eternal Frost · 1 BP · 160% ATK + 440, AoE; 30% Freeze(stun 1turn)g.", 1, 0, 0, 25, (dA, 1.6f, 440, 0, 100), (stunA, 1f, 0, 1, 30));
        S(WEAPON_DIR, "W_SR_Glacier_U", Ult, "Glacier Scepter", "[Control] Absolute Zero · 200% ATK + 800, AoE; 100% Freeze (−20 SPD) 2 turn; 40% Stun 1 turn.", 0, 0, 100, 0, (dA, 2.0f, 800, 0, 100), (stunA, 1f, 0, 2, 100));
        S(WEAPON_DIR, "W_SR_Soul_A", Act, "Soulreaver Scythe", "Soul Drinker · 1 BP · 180% ATK + 440, single; heal 10% as HP.", 1, 0, 0, 25, (d1, 1.8f, 440, 0, 100), (hS, 0.1f, 0, 0, 100));
        S(WEAPON_DIR, "W_SR_Soul_U", Ult, "Soulreaver Scythe", "[Heal] Harvest of Souls · 300% ATK + 850, AoE; heal 50% damage split across team.", 0, 0, 100, 0, (dA, 3.0f, 850, 0, 100), (hA, 0.5f, 0, 0, 100));
        S(WEAPON_DIR, "W_SR_Bastion_A", Act, "Bastion Hammer", "Guard Breaker · 1 BP · 165% ATK + 440, single; pierce 25% armor.", 1, 0, 0, 25, (d1, 1.65f, 440, 0, 100));
        S(WEAPON_DIR, "W_SR_Bastion_U", Ult, "Bastion Hammer", "[Guard] Fortress Wall · 150% ATK + 400, single; shield 20% HP team 3 turn; −20% damage taken 2 turn.", 0, 0, 100, 0, (shldA, 0.2f, 400, 3, 100));
        S(WEAPON_DIR, "W_SR_Warlord_A", Act, "Warlord Banner Spear", "Raging Sandstorm · 1 BP · 190% ATK + 470, single; 40% Stun 1 turn.", 1, 0, 0, 25, (d1, 1.9f, 470, 0, 100), (stun1, 1f, 0, 1, 40));
        S(WEAPON_DIR, "W_SR_Warlord_U", Ult, "Warlord Banner Spear", "[Buff] Warlord's Ascension · +18% ATK, +12% Crit Rate, +15% Crit DMG team 3 turn.", 0, 0, 100, 0, (bAtkA, 0.18f, 0, 3, 100));

        // ULTRA RARE
        S(WEAPON_DIR, "W_UR_Thunder_A", Act, "Thunderlord Blade", "Heaven's Thunder Strike · 2 BP · 200% ATK + 620, single; pierce 60% armor; 50% Stun 1 turn.", 2, 0, 0, 25, (d1, 2.0f, 620, 0, 100), (stun1, 1f, 0, 1, 50));
        S(WEAPON_DIR, "W_UR_Thunder_U", Ult, "Thunderlord Blade", "[Single] Cataclysm Verdict · 400% ATK + 1350, single; pierce 60% armor; 80% Stun 2 turn.", 0, 0, 100, 0, (d1, 4.0f, 1350, 0, 100), (stun1, 1f, 0, 2, 80));
        S(WEAPON_DIR, "W_UR_Void_A", Act, "Voidmaw Cannon", "Dark Vortex · 2 BP · 175% ATK + 600, AoE; enemy −7% HP current/turn 2 turn.", 2, 0, 0, 25, (dA, 1.75f, 600, 0, 100), (psn1, 0.07f, 0, 2, 100));
        S(WEAPON_DIR, "W_UR_Void_U", Ult, "Voidmaw Cannon", "[AoE] Black Hole Collapse · 360% ATK + 1250, AoE; pierce 60% armor; enemy −10% HP current/turn 2 turn.", 0, 0, 100, 0, (dA, 3.6f, 1250, 0, 100), (psn1, 0.1f, 0, 2, 100));
        S(WEAPON_DIR, "W_UR_Frost_A", Act, "Frostbind Lance", "Frostfire Lance · 1 BP · 200% ATK + 600, single; 40% Freeze(stun 1 turn).", 1, 0, 0, 25, (d1, 2.0f, 600, 0, 100), (stun1, 1f, 0, 1, 40));
        S(WEAPON_DIR, "W_UR_Frost_U", Ult, "Frostbind Lance", "[Control] Glacial Prison · 260% ATK + 1100, AoE; 100% Freeze 2 turn; push back 40% AV.", 0, 0, 100, 0, (dA, 2.6f, 1100, 0, 100), (stunA, 1f, 0, 2, 100), (avRtA, 40f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Vamp_A", Act, "Vampire Fang Dagger", "Vampiric Onslaught · 2 BP · 205% ATK + 630, single; heal 15% as HP.", 2, 0, 0, 25, (d1, 2.05f, 630, 0, 100), (hS, 0.15f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Vamp_U", Ult, "Vampire Fang Dagger", "[Heal] Eternal Banquet · 350% ATK + 1200, single; heal 60% damage as HP team; cleanse 1 Debuff/ally.", 0, 0, 100, 0, (d1, 3.5f, 1200, 0, 100), (hA, 0.6f, 0, 0, 100), (clnsA, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Aegis_A", Act, "Divine Aegis Shield", "Shield Bash · 2 BP · 185% ATK + 600, single; pierce 30% armor.", 2, 0, 0, 25, (d1, 1.85f, 600, 0, 100));
        S(WEAPON_DIR, "W_UR_Aegis_U", Ult, "Divine Aegis Shield", "[Guard] Sanctuary of Light · shield 25% HP team 3 turn; heal 12% HP; immune Stun 2 turn.", 0, 0, 100, 0, (shldA, 0.25f, 0, 3, 100), (hA, 0.12f, 0, 0, 100));
        S(WEAPON_DIR, "W_UR_Sov_A", Act, "Sovereign War Standard", "Storm of Blades · 1 BP · 165% ATK + 600, AoE; 30% Bleed.", 1, 0, 0, 25, (dA, 1.65f, 600, 0, 100), (bldA, 0.03f, 0, 2, 30));
        S(WEAPON_DIR, "W_UR_Sov_U", Ult, "Sovereign War Standard", "[Buff] Imperial Overdrive · +20% ATK, +15% Crit Rate team 3 turn; +25 Energy team.", 0, 0, 100, 0, (bAtkA, 0.2f, 0, 3, 100), (enGnA, 25f, 0, 0, 100));

        // LEGENDARY
        S(WEAPON_DIR, "W_Leg_Star_A", Act, "Starforged Blade", "Starlight Blade · 2 BP · 225% ATK + 1000, single; pierce 70% armor; Fracture −8% HP max/turn 2 turn; 75% Stun 1 turn.", 2, 0, 0, 25, (d1, 2.25f, 1000, 0, 100), (stun1, 1f, 0, 1, 75));
        S(WEAPON_DIR, "W_Leg_Star_U", Ult, "Starforged Blade", "[Single] Supernova Edge · 450% ATK + 2000, single; pierce 80% armor; Boss −15% HP current next turn.", 0, 0, 100, 0, (d1, 4.5f, 2000, 0, 100));
        S(WEAPON_DIR, "W_Leg_Deluge_A", Act, "Deluge Trident", "Great Deluge · 2 BP · 190% ATK + 950, AoE; enemy −6% HP current/turn 2 turn.", 2, 0, 0, 25, (dA, 1.9f, 950, 0, 100), (psn1, 0.06f, 0, 2, 100));
        S(WEAPON_DIR, "W_Leg_Deluge_U", Ult, "Deluge Trident", "[AoE] Genesis Starfall · 400% ATK + 1900, AoE; pierce 70% armor; 80% Stun 2 turn.", 0, 0, 100, 0, (dA, 4.0f, 1900, 0, 100), (stunA, 1f, 0, 2, 80));
        S(WEAPON_DIR, "W_Leg_Chrono_A", Act, "Chronofreeze Staff", "Time Frost · 2 BP · 200% ATK + 950, AoE; 40% Freeze.", 2, 0, 0, 25, (dA, 2.0f, 950, 0, 100), (stunA, 1f, 0, 1, 40));
        S(WEAPON_DIR, "W_Leg_Chrono_U", Ult, "Chronofreeze Staff", "[Control] Temporal Lock · 300% ATK + 1500, AoE; 100% Stun 2 turn; freeze AV enemy 1 turn.", 0, 0, 100, 0, (dA, 3.0f, 1500, 0, 100), (stunA, 1f, 0, 2, 100));
        S(WEAPON_DIR, "W_Leg_Phoen_A", Act, "Phoenix Soul Reaver", "Dragon Feather Soul Reaper · 2 BP · 235% ATK + 1050, single; 85% Stun 2 turn.", 2, 0, 0, 25, (d1, 2.35f, 1050, 0, 100), (stun1, 1f, 0, 2, 85));
        S(WEAPON_DIR, "W_Leg_Phoen_U", Ult, "Phoenix Soul Reaver", "[Revive] Phoenix Rebirth · 380% ATK + 1700, AoE; heal 20% HP team; heal sinh 1 ally downed 35% HP.", 0, 0, 100, 0, (dA, 3.8f, 1700, 0, 100), (rvv1, 0.35f, 0, 0, 100), (hA, 0.2f, 0, 0, 100));
        S(WEAPON_DIR, "W_Leg_Titan_A", Act, "Titan Aegis Wall", "Void Executioner · 2 BP · 230% ATK + 1000, single; +40% if target <40% HP.", 2, 0, 0, 25, (d1, 2.3f, 1000, 0, 100));
        S(WEAPON_DIR, "W_Leg_Titan_U", Ult, "Titan Aegis Wall", "[Guard] Aegis Eternal · shield 30% HP team 3 turn; team +15% DEF; immune Debuff 2 turn.", 0, 0, 100, 0, (shldA, 0.3f, 0, 3, 100), (bDefA, 0.15f, 0, 3, 100));
        S(WEAPON_DIR, "W_Leg_Celest_A", Act, "Celestial War Aegis", "Celestial Tempest · 1 BP · 200% ATK + 950, AoE; 50% Stun.", 1, 0, 0, 25, (dA, 2.0f, 950, 0, 100), (stunA, 1f, 0, 1, 50));
        S(WEAPON_DIR, "W_Leg_Celest_U", Ult, "Celestial War Aegis", "[Buff] Divine Coronation · +22% ATK, +18% Crit Rate, +25% Crit DMG team 3 turn; +30 Energy.", 0, 0, 100, 0, (bAtkA, 0.22f, 0, 3, 100), (enGnA, 30f, 0, 0, 100));

        // MYTHIC
        S(WEAPON_DIR, "W_Myt_World_A", Act, "World-Ender Blade", "Apocalyptic Annihilation · 2 BP · 250% ATK + 1500, single; pierce 80% armor; Fracture −10% HP max/turn 3 turn.", 2, 0, 0, 25, (d1, 2.5f, 1500, 0, 100));
        S(WEAPON_DIR, "W_Myt_World_U", Ult, "World-Ender Blade", "[Single] Extinction Protocol · 550% ATK + 2750, single; pierce 100% armor; Boss −25% HP current next turn.", 0, 0, 100, 0, (d1, 5.5f, 2750, 0, 100));
        S(WEAPON_DIR, "W_Myt_Apoc_A", Act, "Apocalypse Cannon", "Wrath of Heaven and Earth · 3 BP · 230% ATK + 1450, AoE; enemy −9% HP current/turn 3 turn; 80% Stun 2 turn.", 3, 0, 0, 25, (dA, 2.3f, 1450, 0, 100), (stunA, 1f, 0, 2, 80));
        S(WEAPON_DIR, "W_Myt_Apoc_U", Ult, "Apocalypse Cannon", "[AoE] Ultimate Psychic Surge · 520% ATK + 2750, AoE; pierce 100% armor; 100% Stun 3 turn.", 0, 0, 100, 0, (dA, 5.2f, 2750, 0, 100), (stunA, 1f, 0, 3, 100));
        S(WEAPON_DIR, "W_Myt_Obli_A", Act, "Oblivion Halberd", "Blade of the Void · 2 BP · 260% ATK + 1550, single; Boss −20% HP current next turn; 85% Stun 2 turn.", 2, 0, 0, 25, (d1, 2.6f, 1550, 0, 100), (stun1, 1f, 0, 2, 85));
        S(WEAPON_DIR, "W_Myt_Obli_U", Ult, "Oblivion Halberd", "[Control] Absolute Silence · 400% ATK + 2200, AoE; 100% Stun 3 turn; lock skill enemy 2 turn.", 0, 0, 100, 0, (dA, 4.0f, 2200, 0, 100), (stunA, 1f, 0, 3, 100));
        S(WEAPON_DIR, "W_Myt_Eter_A", Act, "Eternal Bloodlord Scythe", "Eternal Cataclysm · 2 BP · 205% ATK + 1500, AoE; heal 10% as HP.", 2, 0, 0, 25, (dA, 2.05f, 1500, 0, 100), (hS, 0.1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Myt_Eter_U", Ult, "Eternal Bloodlord Scythe", "[Revive] Rite of Immortality · 450% ATK + 2400, AoE; heal 30% HP team; heal sinh all ally downed 30% HP.", 0, 0, 100, 0, (dA, 4.5f, 2400, 0, 100), (rvvA, 0.3f, 0, 0, 100), (hA, 0.3f, 0, 0, 100));
        S(WEAPON_DIR, "W_Myt_Gen_A", Act, "Genesis Bastion", "Ragnarok Descent · 3 BP · 290% ATK + 1600, single; pierce 100% armor; +50% if target <30% HP.", 3, 0, 0, 25, (d1, 2.9f, 1600, 0, 100));
        S(WEAPON_DIR, "W_Myt_Gen_U", Ult, "Genesis Bastion", "[Guard] Impervious Genesis · shield 40% HP team 3 turn; immune all Debuff & True damage 2 turn.", 0, 0, 100, 0, (shldA, 0.4f, 0, 3, 100), (clnsA, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Myt_Emp_A", Act, "Empyrean War Crown", "Judgment Ray · 2 BP · 240% ATK + 1500, AoE; 60% Stun.", 2, 0, 0, 25, (dA, 2.4f, 1500, 0, 100), (stunA, 1f, 0, 1, 60));
        S(WEAPON_DIR, "W_Myt_Emp_U", Ult, "Empyrean War Crown", "[Buff] Empyrean Ascension · +30% ATK, +25% Crit Rate, +40% Crit DMG team 3 turn; heal full Energy team (1 once/battle).", 0, 0, 100, 0, (bAtkA, 0.3f, 0, 3, 100), (enGnA, 100f, 0, 0, 100));

        // SECRET
        S(WEAPON_DIR, "W_Sec_Judg_A", Act, "Judgment Edge", "Celestial Judgment · 1 BP · 200% ATK + 620, single; pierce 60% armor; 100% Stun 1 turn; [EXCLUSIVE] kill enemy → +1 BP.", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100), (stun1, 1f, 0, 1, 100));
        S(WEAPON_DIR, "W_Sec_Judg_U", Ult, "Judgment Edge", "[EXCLUSIVE · Control] Fate Reversal · 200% ATK + 620, AoE; remove all Buff enemy; 3 next turn all hit ally 100% Crit.", 0, 0, 100, 0, (dA, 2.0f, 620, 0, 100), (dsplA, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Obli_A", Act, "Oblivion Codex", "Genesis Oblivion · 1 BP · 200% ATK + 620, single; pierce armor fully; [EXCLUSIVE] disable passive Boss 3 turn.", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100));
        S(WEAPON_DIR, "W_Sec_Obli_U", Ult, "Oblivion Codex", "[EXCLUSIVE · Single] Null Genesis · 260% ATK + 900, single; pierce armor fully; steal & disable 1 buff strongest Boss 3 turn.", 0, 0, 100, 0, (d1, 2.6f, 900, 0, 100), (dspl1, 1f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Ecli_A", Act, "Eclipse Reaver", "Eclipse of Eternity · 2 BP · 200% ATK + 620, AoE; pierce armor fully; 100% Poison (−8% HP max/turn, 3 turn); [EXCLUSIVE] kill enemy → +1 BP.", 2, 0, 0, 25, (dA, 2.0f, 620, 0, 100), (psn1, 0.08f, 0, 3, 100));
        S(WEAPON_DIR, "W_Sec_Ecli_U", Ult, "Eclipse Reaver", "[EXCLUSIVE · Heal] Eternal Eclipse · 200% ATK + 700, AoE; heal 40% as HP team; team +1 BP.", 0, 0, 100, 0, (dA, 2.0f, 700, 0, 100), (hA, 0.4f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Chro_A", Act, "Chrono Severance", "Zero-Point Severance · 1 BP · 200% ATK + 620, single; ignore shield/buff defense; opening turn → 100% Crit.", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100));
        S(WEAPON_DIR, "W_Sec_Chro_U", Ult, "Chrono Severance", "[EXCLUSIVE · Act] Time Stop · team act extra 1 turn (1 once/battle); enemy skips 1 turn.", 0, 0, 100, 0, (avAdA, 100f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Req_A", Act, "Requiem Engine", "Requiem Protocol · 1 BP · 170% ATK + 600, AoE; hit basic next costs no turn.", 1, 0, 0, 25, (dA, 1.7f, 600, 0, 100));
        S(WEAPON_DIR, "W_Sec_Req_U", Ult, "Requiem Engine", "[EXCLUSIVE · Guard] Requiem Aegis · team immortal 1 turn (HP ≥1); heal full Energy (1 once/battle).", 0, 0, 100, 0, (shldA, 9.9f, 0, 1, 100), (enGnA, 100f, 0, 0, 100));
        S(WEAPON_DIR, "W_Sec_Sov_A", Act, "Sovereign Protocol", "Dominion Strike · 1 BP · 200% ATK + 620, single; pierce 60% armor.", 1, 0, 0, 25, (d1, 2.0f, 620, 0, 100));
        S(WEAPON_DIR, "W_Sec_Sov_U", Ult, "Sovereign Protocol", "[EXCLUSIVE · Control] Absolute Dominion · Boss −40% ATK & −40% SPD 2 turn (enemy basic: 50% control 2 turn); team +1 BP.", 0, 0, 100, 0, (dbAtk1, 0.4f, 0, 2, 100), (dbSpd1, 40f, 0, 2, 100));

        S(HAT_DIR, "H_Com_Focus", Act, "Minor Focus", "Buff · 1 BP · +5% ATK & +4 SPD self 2 turn.", 1, 0, 0, 25, (bAtkS, 0.05f, 0, 2, 100), (bSpdS, 4f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Guard", Act, "Quick Guard", "Buff · 1 BP · +8% DEF self 2 turn.", 1, 0, 0, 25, (bDefS, 0.08f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Dress", Act, "Field Dressing", "Heal · 1 BP · Heal 8% HP max 1 ally.", 1, 0, 0, 25, (h1, 0.08f, 0, 0, 100));
        S(HAT_DIR, "H_Com_Warm", Act, "Warm Up", "Haste · 1 BP · +6 SPD self 2 turn.", 1, 0, 0, 25, (bSpdS, 6f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Taunt", Act, "Taunt Cry", "Debuff · 1 BP · −5% ATK 1 enemy 2 turn.", 1, 0, 0, 25, (dbAtk1, 0.05f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Hobble", Act, "Hobble Shot", "Debuff · 1 BP · −4 SPD 1 enemy 2 turn.", 1, 0, 0, 25, (dbSpd1, 4f, 0, 2, 100));
        S(HAT_DIR, "H_Com_Steady", Act, "Steady Aim", "Buff · 1 BP · +5% Crit Rate self 2 turn.", 1, 0, 0, 25, (bAtkS, 0.05f, 0, 2, 100));

        S(HAT_DIR, "H_Unc_Chant", Act, "Battle Chant", "Buff · 1 BP · +7% ATK & +3 SPD team 2 turn.", 1, 0, 0, 25, (bAtkA, 0.07f, 0, 2, 100), (bSpdA, 3f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Guard", Act, "Guard Formation", "Buff · 1 BP · +8% DEF team 2 turn.", 1, 0, 0, 25, (bDefA, 0.08f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Mend", Act, "Mending Wave", "Heal · 1 BP · Heal 6% HP max team.", 1, 0, 0, 25, (hA, 0.06f, 0, 0, 100));
        S(HAT_DIR, "H_Unc_Haste", Act, "Haste Cry", "Haste · 1 BP · +8 SPD team 2 turn.", 1, 0, 0, 25, (bSpdA, 8f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Weak", Act, "Weakening Shout", "Debuff · 1 BP · −7% ATK all foes 2 turn.", 1, 0, 0, 25, (dbAtkA, 0.07f, 0, 2, 100));
        S(HAT_DIR, "H_Unc_Sap", Act, "Sap Speed", "Debuff · 1 BP · −8 SPD all foes 2 turn; push back 15% AV 1 enemy.", 1, 0, 0, 25, (dbSpdA, 8f, 0, 2, 100), (avRt1, 15f, 0, 0, 100));
        S(HAT_DIR, "H_Unc_Hunt", Act, "Hunter's Mark", "Debuff · 1 BP · 1 enemy +10% damage taken 2 turn.", 1, 0, 0, 25, (dbDef1, 0.1f, 0, 2, 100));

        S(HAT_DIR, "H_Rar_Chant", Act, "War Chant", "Buff · 1 BP · +10% ATK, +8% Crit Rate & +5 SPD team 2 turn.", 1, 0, 0, 25, (bAtkA, 0.1f, 0, 2, 100), (bSpdA, 5f, 0, 2, 100));
        S(HAT_DIR, "H_Rar_Wall", Act, "Stone Wall", "Buff · 1 BP · +12% DEF team 2 turn; −5% damage taken.", 1, 0, 0, 25, (bDefA, 0.12f, 0, 2, 100));
        S(HAT_DIR, "H_Rar_Light", Act, "Healing Light", "Heal · 1 BP · Heal 12% HP 1 ally; cleanse 1 Debuff.", 1, 0, 0, 25, (h1, 0.12f, 0, 0, 100), (clns1, 1f, 0, 0, 100));
        S(HAT_DIR, "H_Rar_Quick", Act, "Quickstep Cry", "Haste · 1 BP · +10 SPD 1 ally 2 turn.", 1, 0, 0, 25, (bSpdS, 10f, 0, 2, 100));
        S(HAT_DIR, "H_Rar_Frost", Act, "Frost Hex", "Debuff · 1 BP · −10 SPD all foes 2 turn; 20% Freeze 1 enemy.", 1, 0, 0, 25, (dbSpdA, 10f, 0, 2, 100), (stun1, 1f, 0, 1, 20));
        S(HAT_DIR, "H_Rar_Curse", Act, "Curse of Weakness", "Debuff · 1 BP · −10% ATK & −8% DEF 1 enemy 3 turn.", 1, 0, 0, 25, (dbAtk1, 0.1f, 0, 3, 100), (dbDef1, 0.08f, 0, 3, 100));
        S(HAT_DIR, "H_Rar_Rally", Act, "Rallying Horn", "Support · 1 BP · +8 Energy team.", 1, 0, 0, 25, (enGnA, 8f, 0, 0, 100));

        S(HAT_DIR, "H_SR_Banner", Act, "Rallying Banner", "Buff · 1 BP · +12% ATK, +6% DEF & +6 SPD team 3 turn; heal 5% HP.", 1, 0, 0, 25, (bAtkA, 0.12f, 0, 3, 100), (bDefA, 0.06f, 0, 3, 100));
        S(HAT_DIR, "H_SR_Sanct", Act, "Sanctuary Ward", "Guard · 1 BP · Shield 8% HP max team 2 turn.", 1, 0, 0, 25, (shldA, 0.08f, 0, 2, 100));
        S(HAT_DIR, "H_SR_Purge", Act, "Purge Light", "Heal · 1 BP · Go all Debuff 1 ally; heal 15% HP.", 1, 0, 0, 25, (clns1, 1f, 0, 0, 100), (h1, 0.15f, 0, 0, 100));
        S(HAT_DIR, "H_SR_Order", Act, "Battle Order", "Pull turn · 1 BP · Pull 1 ally forward 50%; +12% ATK 1 turn.", 1, 0, 0, 25, (avAd1, 50f, 0, 0, 100), (bAtkS, 0.12f, 0, 1, 100));
        S(HAT_DIR, "H_SR_Doom", Act, "Doom Brand", "Debuff · 1 BP · 1 enemy +15% damage taken 3 turn; −10% DEF.", 1, 0, 0, 25, (dbDef1, 0.1f, 0, 3, 100));
        S(HAT_DIR, "H_SR_Blizz", Act, "Blizzard Field", "Debuff · 1 BP · −15 SPD all foes 2 turn; 35% Freeze; push back 25% AV.", 1, 0, 0, 25, (dbSpdA, 15f, 0, 2, 100), (avRtA, 25f, 0, 0, 100));
        S(HAT_DIR, "H_SR_Grave", Act, "Grave Seal", "Debuff · 1 BP · Lock Buff 1 enemy; −30% heal healing taken 2 turn.", 1, 0, 0, 25, (dspl1, 1f, 0, 2, 100));
        S(HAT_DIR, "H_SR_Overc", Act, "Overcharge", "Support · 1 BP · +12 Energy team; +8% Crit DMG 2 turn.", 1, 0, 0, 25, (enGnA, 12f, 0, 0, 100));

        S(HAT_DIR, "H_UR_Warlord", Act, "Warlord's Command", "Buff · 1 BP · +15% ATK, +12% Crit Rate & +8 SPD team 3 turn.", 1, 0, 0, 25, (bAtkA, 0.15f, 0, 3, 100), (bSpdA, 8f, 0, 3, 100));
        S(HAT_DIR, "H_UR_EterP", Act, "Eternal Purification", "Heal · 1 BP · Go all Debuff, heal 8% HP, +8% DEF team 3 turn.", 1, 0, 0, 25, (hA, 0.08f, 0, 0, 100), (clnsA, 1f, 0, 0, 100));
        S(HAT_DIR, "H_UR_Nightm", Act, "Eternal Nightmare", "Debuff · 1 BP · −20% ATK/DEF/SPD all foes 3 turn.", 1, 0, 0, 25, (dbAtkA, 0.2f, 0, 3, 100), (dbDefA, 0.2f, 0, 3, 100));
        S(HAT_DIR, "H_UR_Dream", Act, "Dream Overture", "Pull turn · 1 BP · Pull 1 ally forward 75%; +20% Crit DMG 2 turn.", 1, 0, 0, 25, (avAd1, 75f, 0, 0, 100));
        S(HAT_DIR, "H_UR_TimeW", Act, "Time Warp", "Haste · 1 BP · +12 SPD team 3 turn.", 1, 0, 0, 25, (bSpdA, 12f, 0, 3, 100));
        S(HAT_DIR, "H_UR_Aegis", Act, "Aegis Barrier", "Guard · 1 BP · Shield 12% HP team 3 turn; immune Stun 2 turn.", 1, 0, 0, 25, (shldA, 0.12f, 0, 3, 100));
        S(HAT_DIR, "H_UR_DoomS", Act, "Doom Sentence", "Debuff · 1 BP · All enemy +18% damage taken; −40% heal healing 3 turn.", 1, 0, 0, 25, (dbDefA, 0.18f, 0, 3, 100));
        S(HAT_DIR, "H_UR_Hymn", Act, "Energizing Hymn", "Support · 1 BP · +30 Energy team;", 1, 0, 0, 25, (enGnA, 30f, 0, 0, 100));

        S(HAT_DIR, "H_Leg_Holy", Act, "Absolute Holy Domain", "Heal · 1 BP · Heal 10% HP, +10% ATK/DEF/SPD team 3 turn; immune Debuff 2 turn.", 1, 0, 0, 25, (hA, 0.1f, 0, 0, 100), (bAtkA, 0.1f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_Valk", Act, "Valkyrie's Blessing", "Buff · 1 BP · +18% ATK, +15% Crit Rate, +20% Crit DMG & +10 SPD team 3 turn.", 1, 0, 0, 25, (bAtkA, 0.18f, 0, 3, 100), (bSpdA, 10f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_Dooms", Act, "Doomsday Decree", "Debuff · 1 BP · −25% ATK/DEF/SPD all foes 3 turn; +20% damage taken 2 turn.", 1, 0, 0, 25, (dbAtkA, 0.25f, 0, 3, 100), (dbSpdA, 25f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_SovM", Act, "Sovereign March", "Pull turn · 1 BP · Pull 1 ally forward 100% (act now); +25% ATK & Crit DMG 2 turn.", 1, 0, 0, 25, (avAd1, 100f, 0, 0, 100), (bAtkS, 0.25f, 0, 2, 100));
        S(HAT_DIR, "H_Leg_Tempo", Act, "Tempo Overdrive", "Haste · 1 BP · +15 SPD team 3 turn; immune slow effects 2 turn.", 1, 0, 0, 25, (bSpdA, 15f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_Guard", Act, "Guardian's Sanctuary", "Guard · 1 BP · Shield 18% HP team 3 turn; shield break → heal 8% HP.", 1, 0, 0, 25, (shldA, 0.18f, 0, 3, 100));
        S(HAT_DIR, "H_Leg_GraveE", Act, "Grave Edict", "Debuff · 1 BP · All enemy lock Buff; −50% heal healing; push back 30% AV 2 turn.", 1, 0, 0, 25, (dsplA, 1f, 0, 2, 100), (avRtA, 30f, 0, 0, 100));
        S(HAT_DIR, "H_Leg_SecW", Act, "Second Wind", "Revive · 1 BP · Revive 1 ally downed 30% HP (1 once/battle).", 1, 0, 0, 25, (rvv1, 0.3f, 0, 0, 100));

        S(HAT_DIR, "H_Myt_Emp", Act, "Empyrean Coronation", "Buff · 1 BP · +25% ATK, +20% Crit Rate, +30% Crit DMG & +12 SPD team 3 turn.", 1, 0, 0, 25, (bAtkA, 0.25f, 0, 3, 100), (bSpdA, 12f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_DivA", Act, "Divine Ascension", "Buff · 1 BP · +12% ATK/DEF/SPD team 3 turn; −10% SPD all foes.", 1, 0, 0, 25, (bAtkA, 0.12f, 0, 3, 100), (bDefA, 0.12f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_WrldS", Act, "World Sanction", "Debuff · 1 BP · −30% ATK/DEF/SPD all foes 3 turn; +25% damage taken 3 turn; lock Buff 2 turn.", 1, 0, 0, 25, (dbAtkA, 0.3f, 0, 3, 100), (dbSpdA, 30f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_CelO", Act, "Celestial Overture", "Pull turn · 1 BP · Pull 1 ally forward 100%; +35% Crit DMG 2 turn; team +8 SPD 3 turn.", 1, 0, 0, 25, (avAd1, 100f, 0, 0, 100), (bSpdA, 8f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_ChrO", Act, "Chrono Overdrive", "Haste · 1 BP · +20 SPD team 3 turn.", 1, 0, 0, 25, (bSpdA, 20f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_WrldR", Act, "World Requiem", "Debuff · 1 BP · All enemy −40% SPD; push back 40% AV; lock Buff; −60% heal healing 3 turn.", 1, 0, 0, 25, (dbSpdA, 40f, 0, 3, 100), (avRtA, 40f, 0, 0, 100));
        S(HAT_DIR, "H_Myt_Bast", Act, "Bastion Eternal", "Guard · 1 BP · Shield 25% HP team 3 turn; immune all Debuff & Stun 3 turn.", 1, 0, 0, 25, (shldA, 0.25f, 0, 3, 100));
        S(HAT_DIR, "H_Myt_MassR", Act, "Mass Resurrection", "Revive · 2 BP · Revive all ally downed 25% HP (1 once/battle).", 2, 0, 0, 25, (rvvA, 0.25f, 0, 0, 100));

        S(HAT_DIR, "H_Sec_Dict", Act, "Dictator's Decree", "[EXCLUSIVE] · 1 BP · Raise cap BP on max 7 battle points ;  use raises limit battle skill team  extra +1 ; +1 BP/turn 2 turn.", 1, 0, 0, 25, (enGnA, 1f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_TimeD", Act, "Time Dilation", "[EXCLUSIVE] · 1 BP · 1 ally act extra 1 time now (extra turn).", 1, 0, 0, 25, (avAd1, 100f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_GrandO", Act, "Grand Overture", "[EXCLUSIVE] · 1 BP · 1 once/battle · Pull TEAM forward 100% (ca team act now).", 1, 0, 0, 25, (avAdA, 100f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_ChroS", Act, "Chrono Seizure", "[EXCLUSIVE] · 1 BP · Push back all foes 50% AV; −20% SPD 2 turn.", 1, 0, 0, 25, (avRtA, 50f, 0, 0, 100), (dbSpdA, 20f, 0, 2, 100));
        S(HAT_DIR, "H_Sec_SovG", Act, "Sovereign's Grace", "[EXCLUSIVE] · 1 BP · 1 once/battle · All team heal full Energy ngay.", 1, 0, 0, 25, (enGnA, 100f, 0, 0, 100));
        S(HAT_DIR, "H_Sec_AbsE", Act, "Absolute Edict", "[EXCLUSIVE] · 1 BP · 3 turn: all hit ally cannot be dodged/blocked; +100% Crit DMG.", 1, 0, 0, 25, (bAtkA, 1f, 0, 3, 100));
        S(HAT_DIR, "H_Sec_MindH", Act, "Mind Hijack", "[EXCLUSIVE] · 1 BP · 40% control 1 enemy basic 1 turn; Boss → −30% ATK 2 turn.", 1, 0, 0, 25, (dbAtk1, 0.3f, 0, 2, 100));

        S(BODY_DIR, "B_Com_Stick", Psv, "Sticky Slime", "Passive · Giam 5% damage taken vao.", 0, 0, 0, 0, (bDefS, 0.05f, 0, 0, 100));
        S(BODY_DIR, "B_Com_Thin", Psv, "Thin Hide", "Passive · +4% HP max.", 0, 0, 0, 0, (bDefS, 0.04f, 0, 0, 100));
        S(BODY_DIR, "B_Com_SlowM", Psv, "Slow Metabolism", "Passive · Cuoi each turn heal 1% HP max.", 0, 0, 0, 0, (hS, 0.01f, 0, 0, 100));
        S(BODY_DIR, "B_Com_SoftB", Psv, "Soft Body", "Passive · Giam 10% damage tu hit Crit taken vao.", 0, 0, 0, 0, (bDefS, 0.1f, 0, 0, 100));
        S(BODY_DIR, "B_Com_Light", Psv, "Light Step", "Passive · +3 SPD.", 0, 0, 0, 0, (bSpdS, 3f, 0, 0, 100));
        S(BODY_DIR, "B_Com_Spike", Psv, "Minor Spikes", "Passive · Reflect 3% damage taken vao.", 0, 0, 0, 0, (bDefS, 0.03f, 0, 0, 100));

        S(BODY_DIR, "B_Unc_IronW", Psv, "Iron Will", "Passive · Cuoi each turn heal 1.5% HP max.", 0, 0, 0, 0, (hS, 0.015f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Stone", Psv, "Stone Skin", "Passive · +6% DEF.", 0, 0, 0, 0, (bDefS, 0.06f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Vital", Psv, "Vital Growth", "Passive · +6% HP max.", 0, 0, 0, 0, (bDefS, 0.06f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Adren", Psv, "Adrenaline", "Passive · HP <50% → +5% ATK.", 0, 0, 0, 0, (bAtkS, 0.05f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Corro", Psv, "Corrosive Slime", "Passive · Attacker self −3% DEF (stack max 3).", 0, 0, 0, 0, (dbDef1, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Unc_Barbe", Psv, "Barbed Coat", "Passive · Reflect 5% damage taken vao.", 0, 0, 0, 0, (bDefS, 0.05f, 0, 0, 100));

        S(BODY_DIR, "B_Rar_StoneA", Psv, "Stone Armor", "Passive · HP <40% → +3% DEF.", 0, 0, 0, 0, (bDefS, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_ReinT", Psv, "Reinforced Thorn Armor", "Passive · +3% ATK; phan 5% damage.", 0, 0, 0, 0, (bAtkS, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_Muscl", Psv, "Muscle Enhancement", "Passive · Ally HP <50% duoc +3% pierce armor.", 0, 0, 0, 0, (bAtkA, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_LifeI", Psv, "Life Infusion", "Passive · Heal 2% HP max team cuoi turn.", 0, 0, 0, 0, (hA, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_Berse", Psv, "Berserker Blood", "Passive · Each 10% HP da mat → +2% ATK (max +16%).", 0, 0, 0, 0, (bAtkS, 0.16f, 0, 0, 100));
        S(BODY_DIR, "B_Rar_Guard", Psv, "Guardian Barrier", "Passive · Nearest enemy −3% ATK (aura).", 0, 0, 0, 0, (dbAtk1, 0.03f, 0, 0, 100));

        S(BODY_DIR, "B_SR_Dragon", Psv, "Dragon Scale Armor", "Passive · +4% ATK, DEF & SPD.", 0, 0, 0, 0, (bAtkS, 0.04f, 0, 0, 100), (bSpdS, 4f, 0, 0, 100));
        S(BODY_DIR, "B_SR_WarrF", Psv, "Warrior's Fury", "Passive · All team +2% DEF; self +4% ATK.", 0, 0, 0, 0, (bDefA, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_SR_RestA", Psv, "Restoration Aura", "Passive · All team heal 2% HP max cuoi turn.", 0, 0, 0, 0, (hA, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_SR_Diam", Psv, "Diamond Armor", "Passive · HP <50% → +4% pierce armor.", 0, 0, 0, 0, (bAtkS, 0.04f, 0, 0, 100));
        S(BODY_DIR, "B_SR_Retri", Psv, "Retribution Plate", "Passive · Reflect 8% damage; bi crit phan extra 5%.", 0, 0, 0, 0, (bDefS, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_SR_Momen", Psv, "Momentum Core", "Passive · Use hit basic → +3% ATK 2 turn (stack max 3).", 0, 0, 0, 0, (bAtkS, 0.09f, 0, 0, 100));

        S(BODY_DIR, "B_UR_NightS", Psv, "Nightmare Shackles", "Passive · +6% DEF & +3% HP max.", 0, 0, 0, 0, (bDefS, 0.06f, 0, 0, 100));
        S(BODY_DIR, "B_UR_DivGA", Psv, "Divine Guardian Armor", "Passive · After hit for, heal HP dong team = 5% damage self taken.", 0, 0, 0, 0, (hA, 0.05f, 0, 0, 100));
        S(BODY_DIR, "B_UR_EterS", Psv, "Eternal Suppression", "Passive · +8% DEF; −4% damage taken.", 0, 0, 0, 0, (bDefS, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_UR_VampC", Psv, "Vampiric Core", "Passive · All hit heal 8% damage as HP.", 0, 0, 0, 0, (hS, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_UR_SpdSe", Psv, "Speed Seal", "Passive · +5% DEF; turn start if di truoc enemy → +6 SPD turn do.", 0, 0, 0, 0, (bSpdS, 6f, 0, 0, 100));
        S(BODY_DIR, "B_UR_BulwA", Psv, "Bulwark Aura", "Passive · All team −5% damage taken; self −8%.", 0, 0, 0, 0, (bDefA, 0.05f, 0, 0, 100));

        S(BODY_DIR, "B_Leg_ArmI", Psv, "Armor of Immortality", "Passive · First time bi ha downed → heal sinh 10% HP max (1 once/battle).", 0, 0, 0, 0, (rvv1, 0.1f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_HeavA", Psv, "Heaven's Aegis", "Passive · +10% DEF; −8% damage; battle start shield 12% HP max.", 0, 0, 0, 0, (bDefS, 0.1f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_DivR", Psv, "Divine Resurrection", "Passive · All enemy −8% SPD & −5% ATK (aura).", 0, 0, 0, 0, (dbSpdA, 8f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_EterB", Psv, "Eternal Bulwark", "Passive · Each turn survived → +2% DEF (max +20%).", 0, 0, 0, 0, (bDefS, 0.2f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_Blood", Psv, "Bloodlord Dominion", "Passive · Heal 12% damage as HP; HP full → overflow thanh shield (max 15% HP).", 0, 0, 0, 0, (hS, 0.12f, 0, 0, 100));
        S(BODY_DIR, "B_Leg_AegF", Psv, "Aegis of the Fallen", "Passive · 1 ally downed → team +10% ATK & −10% damage taken 3 turn.", 0, 0, 0, 0, (bAtkA, 0.1f, 0, 0, 100));

        S(BODY_DIR, "B_Myt_HeavS", Psv, "Heaven's Suppression", "Passive · +10% DEF; −9% damage taken; heal 2% HP cuoi turn.", 0, 0, 0, 0, (bDefS, 0.1f, 0, 0, 100), (hS, 0.02f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_DivI", Psv, "Divine Incarnation", "Passive · All team +8% ATK, +8% DEF, +5% SPD (aura).", 0, 0, 0, 0, (bAtkA, 0.08f, 0, 0, 100), (bDefA, 0.08f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_ImmS", Psv, "Immortal Sovereign", "Passive · Immune Crit taken vao; −12% damage; heal 3% HP cuoi turn.", 0, 0, 0, 0, (hS, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_WrldT", Psv, "World Tree Root", "Passive · All team heal 3% HP max cuoi turn & +6% HP max.", 0, 0, 0, 0, (hA, 0.03f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_TitR", Psv, "Titan's Reprisal", "Passive · Reflect 15% damage; bi crit phan 25%; immune knock-up/push back.", 0, 0, 0, 0, (bDefS, 0.15f, 0, 0, 100));
        S(BODY_DIR, "B_Myt_AscC", Psv, "Ascendant Core", "Passive · HP >70% → +15% ATK; HP <30% → −30% damage taken.", 0, 0, 0, 0, (bAtkS, 0.15f, 0, 0, 100));

        S(BODY_DIR, "B_Sec_Dict", Psv, "Dictator", "Passive [EXCLUSIVE] · +2 BP each turn cho team (la all tran sinh +2/turn).", 0, 0, 0, 0, (enGnA, 2f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Prio", Psv, "Priority Protocol", "Passive [EXCLUSIVE] · Always act dau tien each turn (ignore SPD); battle start +1 BP.", 0, 0, 0, 0, (avAdS, 100f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Hack", Psv, "Hacker", "Passive [EXCLUSIVE] · Each turn player, tu danh 1 enemy random 270% ATK basic.", 0, 0, 0, 0, (d1, 2.7f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Fina", Psv, "Final Vengeance", "Passive [EXCLUSIVE] · HP ve 0: immune crit, phan 200% ATK; enemy dies → heal sinh 30% HP.", 0, 0, 0, 0, (d1, 2.0f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Over", Psv, "Overclock Engine", "Passive [EXCLUSIVE] · Spend BP → 50% refund 1 BP (max 1/turn); Ultimate charge Energy +50%.", 0, 0, 0, 0, (enGnA, 1f, 0, 0, 100));
        S(BODY_DIR, "B_Sec_Sing", Psv, "Singularity Field", "Passive [EXCLUSIVE] · All enemy −15% all stats & no the tu tang stats bang buff/passive (aura).", 0, 0, 0, 0, (dbAtkA, 0.15f, 0, 0, 100), (dbSpdA, 15f, 0, 0, 100));

        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("<color=green>[SkillGen] HOAN TAT! All bo Database Skill tu Spec da duoc tao thanh cong!</color>");
    }

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
