using BepInEx.Configuration;
using HarmonyLib;
using MysticsRisky2Utils;
using MysticsRisky2Utils.ContentManagement;
using R2API;
using RisingTides;
using RisingTides.Buffs;
using RisingTides.Equipment;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using static RoR2.BlastAttack;
using static RoR2.OverlapAttack;

namespace NemesisRisingTides.Changes
{
    public class Bismuth
    {
        public static bool enabled;
        public static string Description;
        public static ConfigEntry<string> DebuffBlacklist;
        public static ConfigEntry<bool> UseWhitelist;
        public static ConfigEntry<bool> RemoveBarrierShinanigan;
        public static ConfigEntry<bool> DisableOnUse;
        public static ConfigEntry<float> OnUseCooldown;
        public static ConfigEntry<float> BlastDamage;
        public static ConfigEntry<float> BlastRange;
        public static ConfigEntry<int> RandomBuffAmount;

        public static List<Action<float, float, CharacterBody, CharacterBody>> RandomDebuffs = new();
        public static void Init()
        {
            Description = $"<style=cIsHealth>Barrier does not decay</style>. Immune to knockback while <style=cIsHealing>barrier</style> is active. Attacks apply a <style=cIsUtility>random debuff</style> on hit. On Use, Gain <style=cIsHealth>{AffixBarrierEquipment.barrierRecharge.Value}%</style> of <style=cIsHealth>maximum health</style> as <style=cIsHealth>temporary barrier</style>.";
            enabled = Main.Config.Bind(nameof(Bismuth) + " Elites", "Enable Changes", true, "").Value;
            if (!enabled) return;
            Main.Log.LogInfo("Applying change to " + nameof(Bismuth) + " Elite");

            Main.AfterEquipContentPackLoaded += () => { RisingTidesContent.Equipment.RisingTides_AffixBarrier.cooldown = OnUseCooldown.Value; };

            DebuffBlacklist = Main.Config.Bind(nameof(Bismuth) + " Elites", "Debuff Blacklist", "", "see log for list of debuff names, separated by comma");
            UseWhitelist = Main.Config.Bind(nameof(Bismuth) + " Elites", "Use blacklist as whitelist instead", false, "");
            RemoveBarrierShinanigan = Main.Config.Bind(nameof(Bismuth) + " Elites", "Remove Barrier Features", true, "enable if Buffered Elites are enabled.");
            DisableOnUse = Main.Config.Bind(nameof(Bismuth) + " Elites", "Disable On-use for enemies", true, "");
            OnUseCooldown = Main.Config.Bind(nameof(Bismuth) + " Elites", "On-use Cooldown", 30f, "in seconds");
            BlastDamage = Main.Config.Bind(nameof(Bismuth) + " Elites", "On-use Damage Coefficient", 1.8f, "1 = 100%");
            BlastRange = Main.Config.Bind(nameof(Bismuth) + " Elites", "On-use Damage Radius", 13f, "in meters");
            RandomBuffAmount = Main.Config.Bind(nameof(Bismuth) + " Elites", "On-use Blast Random Debuff Amount", 3, "");

            RoR2Application.onLoad += () =>
            {
                List<string> names = new();
                List<string> blacklist = DebuffBlacklist.Value.Split(',').Select(x => x.Trim()).ToList();
                foreach (DotController.DotDef dot in DotAPI.DotDefs)
                {
                    string name = dot?.associatedBuff?.name;
                    if (name == null) continue;
                    names.Add(name);
                    if (blacklist.Contains(name) == UseWhitelist.Value) RandomDebuffs.Add((duration, damage, attacker, victim) =>
                    {
                        InflictDotInfo inflictDotInfo = default;
                        inflictDotInfo.victimObject = victim.gameObject;
                        inflictDotInfo.attackerObject = attacker.gameObject;
                        inflictDotInfo.dotIndex = (DotController.DotIndex)DotAPI.DotDefs.ToList().IndexOf(dot);
                        inflictDotInfo.damageMultiplier = 1f;
                        inflictDotInfo.totalDamage = damage * 0.5f;
                        inflictDotInfo.duration = duration;
                        DotController.InflictDot(ref inflictDotInfo);
                    });
                }
                List<BuffDef> dotBuffs = DotController.dotDefs.Select(x => x.associatedBuff).ToList();
                foreach (BuffDef buff in BuffCatalog.buffDefs)
                {
                    if (dotBuffs.Contains(buff) || !buff.isDebuff || buff.isElite) continue;
                    string name = buff.name;
                    names.Add(name);
                    if (blacklist.Contains(name) == UseWhitelist.Value) RandomDebuffs.Add((duration, damage, attacker, victim) =>
                    {
                        victim.AddTimedBuff(buff, duration);
                    });
                }
                Main.Log.LogInfo("List of debuff names to use for Bismuth elite config: " + names.Join());
            };
            if (RemoveBarrierShinanigan.Value)
            {
                Description = $"Attacks apply a <style=cIsUtility>random debuff</style> on hit. On Use, cause a blast in a <style=cIsDamage>{BlastRange.Value}m</style> radius for <style=cIsDamage>{BlastDamage.Value * 100}%</style> base damage that inflicts <style=cIsDamage>{RandomBuffAmount.Value} random debuffs</style> to all nearby enemies.";
                LanguageAPI.AddOverlay("EQUIPMENT_RISINGTIDES_AFFIXBARRIER_NAME", "Reversed Decision");
                Main.SuperOverrides.Add("AFFIX_BARRIER_NAME", "Reversed Decision");
                LanguageAPI.AddOverlay("EQUIPMENT_RISINGTIDES_AFFIXBARRIER_PICKUP", "Become an aspect of chaos. On use, cause a blast that inflicts 3 random debuffs to all nearby enemies.");
                Main.SuperOverrides.Add("AFFIX_BARRIER_PICKUP", "Become an aspect of chaos.");
                Main.SuperOverrides.Add("ASPECT_OF_UNITY", "<style=cDeath>Aspect of Chaos</style> :");
                Main.SuperOverrides.Add("PASSIVE_BARRIER_STOP", "");
                Main.SuperOverrides.Add("FORCE_IMMUNE_BARRIER", "");
                Main.SuperOverrides.Add("AFFIX_BARRIER_ACTIVE", $"Cause a blast that inflicts <style=cIsDamage>{RandomBuffAmount.Value} random debuffs</style>.");
                Main.AfterBuffContentPackLoaded += () => 
                {
                    AffixBarrier instance = (AffixBarrier)BaseLoadableAsset.staticAssetDictionary[typeof(AffixBarrier)];
                    On.RoR2.HealthComponent.TakeDamageForce_Vector3_bool_bool -= instance.HealthComponent_TakeDamageForce_Vector3_bool_bool;
                    On.RoR2.HealthComponent.TakeDamageForce_DamageInfo_bool_bool -= instance.HealthComponent_TakeDamageForce_DamageInfo_bool_bool;
                    GenericGameEvents.OnApplyDamageReductionModifiers -= instance.GenericGameEvents_OnApplyDamageReductionModifiers;
                    On.RoR2.CharacterBody.RecalculateStats -= instance.CharacterBody_RecalculateStats;
                    IL.RoR2.CharacterBody.RecalculateStats -= instance.CharacterBody_RecalculateStats1;
                    On.RoR2.UI.HealthBar.UpdateBarInfos -= instance.HealthBar_UpdateBarInfos;
                    RisingTidesContent.Buffs.RisingTides_AffixBarrier.iconSprite = Main.AssetBundle.LoadAsset<Sprite>("Assets/BismuthBuff.png");
                };
            }
            Main.Harmony.PatchAll(typeof(PatchBarrierOnHit));
            Main.Harmony.PatchAll(typeof(PatchBarrierEquip));
        }

        [HarmonyPatch(typeof(AffixBarrier), nameof(AffixBarrier.GenericGameEvents_OnHitEnemy))]
        public class PatchBarrierOnHit
        {
            public static bool Prefix(DamageInfo damageInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo attackerInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo victimInfo)
            {
                if (damageInfo.rejected || !(damageInfo.procCoefficient > 0f) || !attackerInfo.body || !attackerInfo.body.HasBuff(RisingTidesContent.Buffs.RisingTides_AffixBarrier) || !victimInfo.body) return false;
                var action = Run.instance.runRNG.NextElementUniform(RandomDebuffs);
                action(AffixBarrier.debuffDuration.Value, damageInfo.damage, attackerInfo.body, victimInfo.body);
                return false;
            }
        }

        [HarmonyPatch(typeof(AffixBarrierEquipment), nameof(AffixBarrierEquipment.OnUse))]
        public class PatchBarrierEquip
        {
            public static bool Prefix(ref bool __result, EquipmentSlot equipmentSlot)
            {
                __result = false;
                if (equipmentSlot.characterBody == null || (DisableOnUse.Value && equipmentSlot.characterBody.teamComponent.teamIndex != TeamIndex.Player)) return false;
                if (BlastRange.Value > 0 && RandomDebuffs.Count > 0)
                {
                    BlastAttack attack = new()
                    {
                        attacker = equipmentSlot.characterBody.gameObject,
                        inflictor = equipmentSlot.characterBody.gameObject,
                        teamIndex = equipmentSlot.characterBody.teamComponent.teamIndex,
                        position = equipmentSlot.characterBody.corePosition,
                        procCoefficient = 1f,
                        radius = BlastRange.Value,
                        baseDamage = BlastDamage.Value,
                        falloffModel = FalloffModel.Linear,
                        damageColorIndex = DamageColorIndex.Item,
                        attackerFiltering = AttackerFiltering.NeverHitSelf
                    };
                    attack.Fire().hitPoints.Do(hitPoint =>
                    {
                        CharacterBody body = hitPoint.hurtBox?.healthComponent?.body;
                        if (body != null)
                        {
                            for (int i = 0; i < RandomBuffAmount.Value; i++)
                            {
                                var action = Run.instance.runRNG.NextElementUniform(RandomDebuffs);
                                action(AffixBarrier.debuffDuration.Value * 5f, BlastDamage.Value * equipmentSlot.characterBody.damage, equipmentSlot.characterBody, body);
                            }
                        }
                    });
                    __result = true;
                }
                return !RemoveBarrierShinanigan.Value;
            }
        }
    }   
}
