using BepInEx.Configuration;
using HarmonyLib;
using MysticsRisky2Utils;
using MysticsRisky2Utils.ContentManagement;
using R2API;
using RisingTides;
using RisingTides.Buffs;
using RisingTides.Equipment;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RisingTides.Buffs.AffixMoney;
using static UnityEngine.UI.Image;

namespace NemesisRisingTides.Changes
{
    public class Magnetic
    {
        public static bool enabled;
        public static string Description;
        public static ConfigEntry<bool> SapMoneyEnable;
        public static ConfigEntry<float> SapMoneyAmount;
        public static ConfigEntry<float> GainMoneyAmount;
        public static ConfigEntry<float> KillMoneyAmount;
        public static ConfigEntry<bool> OnUseEnable;
        public static ConfigEntry<float> OnUseCooldown;
        public static ConfigEntry<float> OnUseDamage;
        public static ConfigEntry<float> OnUseRange;
        public static ConfigEntry<float> OnUseAmount;

        public static BuffDef SapMoneyBuff;
        public static void Init()
        {
            Description = $"<style=cIsUtility>Steal the money</style> of nearby enemies. Attacks <style=cIsUtility>pull down</style> airborne enemies on hit. On Use, <style=cIsUtility>Stop all incoming projectiles</style> for <style=cIsUtility>{AffixMoneyEquipment.duration.Value}s</style>.";
            enabled = Main.Config.Bind(nameof(Magnetic) + " Elites", "Enable Changes", true, "").Value;
            if (!enabled) return;
            Main.Log.LogInfo("Applying change to " + nameof(Magnetic) + " Elite");
            Main.AfterEquipContentPackLoaded += () => { RisingTidesContent.Equipment.RisingTides_AffixMoney.cooldown = OnUseCooldown.Value; };

            SapMoneyEnable = Main.Config.Bind(nameof(Magnetic) + " Elites", "Money Sap Buff Enable", true, "");
            SapMoneyAmount = Main.Config.Bind(nameof(Magnetic) + " Elites", "Money Sap Amount", 0.5f, "Scaled with time, amount of money sapped on hit");
            GainMoneyAmount = Main.Config.Bind(nameof(Magnetic) + " Elites", "Money Sap Amount Gained", 0.05f, "Scaled with time, amount of money gained when hit");
            KillMoneyAmount = Main.Config.Bind(nameof(Magnetic) + " Elites", "Extra Money Gain Per Enemy", 0.25f, "Scaled with time");
            OnUseEnable = Main.Config.Bind(nameof(Magnetic) + " Elites", "Change On-use", true, "");
            OnUseCooldown = Main.Config.Bind(nameof(Magnetic) + " Elites", "On-use Cooldown", 30f, "in seconds, set to 0 to remove the on-use effect altogether.");
            OnUseDamage = Main.Config.Bind(nameof(Magnetic) + " Elites", "On-use Damage Coefficient", 1.8f, "1 = 100%");
            OnUseRange = Main.Config.Bind(nameof(Magnetic) + " Elites", "On-use Damage Radius", 13f, "in meters");
            OnUseAmount = Main.Config.Bind(nameof(Magnetic) + " Elites", "On-use Damage Money Steal Amount", 1f, "Scaled with time");

            if (SapMoneyEnable.Value)
            {
                Description = $"On hit, Gain <style=cIsUtility>{GainMoneyAmount.Value * SapMoneyAmount.Value * 25}$</style> that increases over time. On Use, cause a blast in a <style=cIsDamage>{OnUseRange.Value}m</style> radius for <style=cIsDamage>{OnUseDamage.Value * 100}%</style> base damage that <style=cIsUtility>Steals money</style> from all nearby enemies.";
                Main.SuperOverrides.Add("AFFIX_MONEY_ACTIVE", "Cause a blast that <style=cIsUtility>Steals money</style> from all nearby enemies.");
                Main.SuperOverrides.Add("PASSIVE_DRAIN_MONEY", $"On hit, Gain <style=cIsUtility>{GainMoneyAmount.Value * SapMoneyAmount.Value * 25}$</style> that increases over time.");
                Main.SuperOverrides.Add("PULLDOWN_ON_HIT", "");
                SapMoneyBuff = ScriptableObject.CreateInstance<BuffDef>();
                SapMoneyBuff.canStack = false;
                SapMoneyBuff.iconSprite = Main.AssetBundle.LoadAsset<Sprite>("Assets/MagneticEffect.png");
                SapMoneyBuff.buffColor = Color.yellow;
                ContentAddition.AddBuffDef(SapMoneyBuff);

                Main.AfterBuffContentPackLoaded += () => { GenericGameEvents.OnHitEnemy -= ((AffixMoney)BaseLoadableAsset.staticAssetDictionary[typeof(AffixMoney)]).GenericGameEvents_OnHitEnemy; };
                Main.Harmony.PatchAll(typeof(PatchMoneyGiveBuff));
                On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, _victim) =>
                {
                    if (damageInfo == null
                        || damageInfo.rejected
                        || damageInfo.procCoefficient <= 0
                        || !damageInfo.attacker
                        || !damageInfo.attacker.GetComponent<CharacterBody>()
                        || !_victim
                        || !_victim.GetComponent<CharacterBody>()) { orig(self, damageInfo, _victim); return; }
                    CharacterBody attacker = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victim = _victim.GetComponent<CharacterBody>();
                    if (!(attacker.HasBuff(SapMoneyBuff) || attacker.HasBuff(RisingTidesContent.Buffs.RisingTides_AffixMoney) || damageInfo.HasModdedDamageType(magneticDamageType))
                        || !victim.master
                        || victim.master.money <= 0) { orig(self, damageInfo, _victim); return; }
                    uint stealCount = Math.Min(victim.master.money, (uint)Run.instance.GetDifficultyScaledCost((int)(SapMoneyAmount.Value * 25f)));
                    victim.master.money -= stealCount;
                    if (attacker.master) attacker.master.money += (uint)(stealCount * GainMoneyAmount.Value);
                    orig(self, damageInfo, _victim);
                };
            }
            if (KillMoneyAmount.Value != 0)
            {
                On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, damageReport) =>
                {
                    if (damageReport.attackerMaster && damageReport.attackerMaster.money != 0 && damageReport.victimBody && damageReport.victimBody.HasBuff(RisingTidesContent.Buffs.RisingTides_AffixMoney))
                    {
                        SphereSearch sphereSearch = new()
                        {
                            radius = auraRadius + damageReport.victimBody.radius,
                            queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                            mask = LayerIndex.entityPrecise.mask,
                            origin = damageReport.victimBody.corePosition
                        };
                        sphereSearch.RefreshCandidates();
                        sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                        TeamMask mask = default; mask.AddTeam(damageReport.victimBody.teamComponent.teamIndex);
                        sphereSearch.FilterCandidatesByHurtBoxTeam(mask);

                        float money = 0;
                        sphereSearch.GetHurtBoxes().Do(hurtBox => {
                            if ((bool)hurtBox
                                && (bool)hurtBox.healthComponent
                                && (bool)hurtBox.healthComponent.body
                                && (bool)hurtBox.healthComponent.body.master
                                && hurtBox.healthComponent.body != damageReport.victimBody)
                                money += hurtBox.healthComponent.body.master.money;
                        });
                        damageReport.attackerMaster.money += (uint)(money * KillMoneyAmount.Value);
                    }
                    orig(self, damageReport);
                };
            }
            if (OnUseEnable.Value) Main.Harmony.PatchAll(typeof(PatchMoneyEquip));
        }

        [HarmonyPatch(typeof(RisingTidesAffixMoneyBehaviour), nameof(RisingTidesAffixMoneyBehaviour.FixedUpdate))]
        public class PatchMoneyGiveBuff
        {
            public static bool Prefix(RisingTidesAffixMoneyBehaviour __instance)
            {
                if (!__instance.body.healthComponent || !__instance.body.healthComponent.alive || !NetworkServer.active || auraRadius <= 0) return false;
                __instance.sphereSearch.origin = __instance.body.corePosition;
                __instance.sphereSearch.RefreshCandidates();
                __instance.sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                TeamMask mask = default; mask.AddTeam(__instance.body.teamComponent.teamIndex);
                __instance.sphereSearch.FilterCandidatesByHurtBoxTeam(mask);
                __instance.sphereSearch.GetHurtBoxes().Do(hurtBox =>
                {
                    if (hurtBox
                        && hurtBox.healthComponent
                        && hurtBox.healthComponent.body
                        && hurtBox.healthComponent.body.master 
                        && hurtBox.healthComponent.body != __instance.body) 
                        hurtBox.healthComponent.body.AddTimedBuff(SapMoneyBuff, 4f);
                });
                return false;
            }
        }

        [HarmonyPatch(typeof(AffixMoneyEquipment), nameof(AffixMoneyEquipment.OnUse))]
        public class PatchMoneyEquip
        {
            public static bool Prefix(ref bool __result, EquipmentSlot equipmentSlot)
            {
                __result = false;
                if (NetworkServer.active 
                    && equipmentSlot.characterBody
                    && equipmentSlot.characterBody.master
                    && equipmentSlot.characterBody.teamComponent.teamIndex == TeamIndex.Player 
                    && OnUseCooldown.Value > 0) // nuke if cooldown is 0
                {
                    BlastAttack attack = new()
                    {
                        attacker = equipmentSlot.characterBody.gameObject,
                        inflictor = equipmentSlot.characterBody.gameObject,
                        teamIndex = equipmentSlot.characterBody.teamComponent.teamIndex,
                        position = equipmentSlot.characterBody.corePosition,
                        procCoefficient = 1f,
                        radius = OnUseRange.Value,
                        baseDamage = OnUseDamage.Value * equipmentSlot.characterBody.damage,
                        falloffModel = BlastAttack.FalloffModel.Linear,
                        damageColorIndex = DamageColorIndex.Item,
                        attackerFiltering = AttackerFiltering.NeverHitSelf
                    };
                    int count = attack.Fire().hitCount;
                    equipmentSlot.characterBody.master.money += (uint)Run.instance.GetDifficultyScaledCost((int)(count * OnUseAmount.Value * 25f));
                    __result = true;
                    EffectData effectData = new()
                    {
                        scale = equipmentSlot.characterBody.radius,
                        origin = equipmentSlot.characterBody.corePosition,
                        genericFloat = 0.6f
                    };
                    effectData.SetHurtBoxReference(equipmentSlot.characterBody.gameObject);
                    for (int i = 0; i < count; i++) EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/GoldOrbEffect"), effectData, transmit: true);
                    return false;
                }
                return false;
            }
        }
    }
}
