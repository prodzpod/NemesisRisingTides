using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using MysticsRisky2Utils;
using MysticsRisky2Utils.BaseAssetTypes;
using MysticsRisky2Utils.ContentManagement;
using NemesisRisingTides.Changes;
using R2API;
using RisingTides;
using RisingTides.Buffs;
using RisingTides.Equipment;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NemesisRisingTides.Contents
{
    public class Buffered
    {
        public class EliteBuffered : BaseElite
        {
            public static EliteBuffered instance;
            public override void OnLoad()
            {
                base.OnLoad();
                instance = this;
                eliteDef.name = "NemesisRisingTides_Buffered";
                vanillaTier = 1;
                isHonor = true;
                eliteDef.healthBoostCoefficient = Main.Config.Bind(nameof(Buffered) + "Elites", "Health Boost Coefficient", 4f, "How much health this elite should have? (e.g. 18 means it will have 18x health)").Value;
                eliteDef.damageBoostCoefficient = Main.Config.Bind(nameof(Buffered) + "Elites", "Damage Boost Coefficient", 2f, "How much damage this elite should have? (e.g. 6 means it will have 6x damage)").Value;
                EliteRamp.AddRamp(eliteDef, Main.AssetBundle.LoadAsset<Texture2D>("Assets/BufferedRamp.png"));
            }

            public override void AfterContentPackLoaded()
            {
                base.AfterContentPackLoaded();
                eliteDef.eliteEquipmentDef = Main.Config.Bind("Enabled Elites", nameof(Buffered), true, "").Value ? EquipBuffered.instance.equipmentDef : null;
            }
        }

        public class EquipBuffered : BaseEliteAffix
        {
            public static EquipBuffered instance;
            public static ConfigEntry<bool> DisableOnUse;

            public override void OnPluginAwake()
            {
                base.OnPluginAwake();
                DisableOnUse = Main.Config.Bind(nameof(Buffered) + " Elites", "Disable On-use for enemies", true, "");
            }

            public override void OnLoad()
            {
                base.OnLoad();
                instance = this;
                equipmentDef.name = "NemesisRisingTides_AffixBuffered";
                ConfigOptions.ConfigurableValue.CreateFloat(Main.PluginGUID, Main.PluginName, Main.Config, typeof(Buffered) + "Elites", "On-use Cooldown", 30f, 0f, 1000f, "", null, null, restartRequired: false, delegate (float newValue) { equipmentDef.cooldown = newValue; });
                equipmentDef.pickupIconSprite = Main.AssetBundle.LoadAsset<Sprite>("Assets/BufferedEquip.png");
                SetUpPickupModel();
                AdjustElitePickupMaterial(Color.white, 1.6f, Main.AssetBundle.LoadAsset<Texture2D>("Assets/BufferedRamp.png"));
                GameObject wrapper = new GameObject().InstantiateClone("NemesisRisingTidesAffixBufferedHeadpieceWrapper", registerNetwork: false); // sghetti here...
                GameObject crown = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/BarrierEffect.prefab").WaitForCompletion().transform.Find("MeshHolder").Find("ShieldMesh").gameObject.InstantiateClone("NemesisRisingTidesAffixBufferedHeadpiece", registerNetwork: false);
                crown.transform.localScale = Vector3.one * 7.5f;
                crown.transform.SetParent(wrapper.transform);
                crown.GetComponent<MeshFilter>().sharedMesh = Object.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/SlowOnHit/SlowDownTime.prefab").WaitForCompletion().transform.Find("Visual").Find("Mesh").GetComponent<MeshFilter>().sharedMesh);
                crown.transform.eulerAngles = new Vector3(30f, 0f, 0f);
                GameObject crown2 = crown.InstantiateClone("Crown 2");
                crown2.transform.eulerAngles = Quaternion.AngleAxis(180f, Vector3.up) * crown.transform.eulerAngles;
                crown2.transform.SetParent(wrapper.transform);
                itemDisplayPrefab = PrepareItemDisplayModel(wrapper);
                onSetupIDRS += delegate
                {
                    foreach (CharacterBody allBodyPrefabBodyBodyComponent in BodyCatalog.allBodyPrefabBodyBodyComponents)
                    {
                        CharacterModel componentInChildren = allBodyPrefabBodyBodyComponent.GetComponentInChildren<CharacterModel>();
                        if ((bool)componentInChildren && componentInChildren.itemDisplayRuleSet != null)
                        {
                            DisplayRuleGroup equipmentDisplayRuleGroup = componentInChildren.itemDisplayRuleSet.GetEquipmentDisplayRuleGroup(RoR2Content.Equipment.AffixWhite.equipmentIndex);
                            if (!equipmentDisplayRuleGroup.Equals(DisplayRuleGroup.empty))
                            {
                                string bodyName = BodyCatalog.GetBodyName(allBodyPrefabBodyBodyComponent.bodyIndex);
                                ItemDisplayRule[] rules = equipmentDisplayRuleGroup.rules;
                                for (int i = 0; i < rules.Length; i++)
                                {
                                    ItemDisplayRule itemDisplayRule = rules[i];
                                    AddDisplayRule(bodyName, itemDisplayRule.childName, itemDisplayRule.localPos, itemDisplayRule.localAngles, itemDisplayRule.localScale);
                                }
                            }
                        }
                    }
                };
            }

            public override bool OnUse(EquipmentSlot equipmentSlot)
            {
                if (DisableOnUse.Value && equipmentSlot?.characterBody?.teamComponent?.teamIndex != TeamIndex.Player) return false;
                if ((bool)equipmentSlot.characterBody)
                {
                    EffectData effectData = new()
                    {
                        origin = equipmentSlot.characterBody.corePosition,
                        scale = equipmentSlot.characterBody.radius
                    };
                    effectData.SetHurtBoxReference(equipmentSlot.characterBody.gameObject);
                    EffectManager.SpawnEffect(AffixBarrierEquipment.selfBuffUseEffect, effectData, transmit: true);
                    if ((bool)equipmentSlot.characterBody.healthComponent)
                    {
                        equipmentSlot.characterBody.healthComponent.AddBarrier(equipmentSlot.characterBody.maxBarrier * (float)AffixBarrierEquipment.barrierRecharge / 100f);
                    }
                    return true;
                }
                return false;
            }

            public override void AfterContentPackLoaded()
            {
                base.AfterContentPackLoaded();
                equipmentDef.passiveBuffDef = AffixBuffered.instance.buffDef;
            }

        }

        public class AffixBuffered : BaseBuff
        {
            public static AffixBuffered instance;
            public static ConfigEntry<float> DeathRange;
            public static ConfigEntry<float> DeathHealth;

            public override void OnPluginAwake()
            {
                base.OnPluginAwake();
                DeathRange = Main.Config.Bind(nameof(Buffered) + " Elites", "On Death Nova Range", 13f, "in meters");
                DeathHealth = Main.Config.Bind(nameof(Buffered) + " Elites", "On Death Nova Percentage", 0.25f, "1 = 100% of each enemies max hp");
            }

            public override void OnLoad()
            {
                base.OnLoad();
                instance = this;
                buffDef.name = "NemesisRisingTides_AffixBuffered";
                buffDef.iconSprite = Main.AssetBundle.LoadAsset<Sprite>("Assets/BufferedBuff.png");

                LanguageAPI.Add("ELITE_MODIFIER_NEMESISRISINGTIDES_BUFFERED", "Buffered {0}");
                LanguageAPI.Add("EQUIPMENT_NEMESISRISINGTIDES_AFFIXBUFFERED_NAME", "Combined Efforts");
                LanguageAPI.Add("EQUIPMENT_NEMESISRISINGTIDES_AFFIXBUFFERED_PICKUP", "Become an aspect of unity.");
                LanguageAPI.Add("EQUIPMENT_NEMESISRISINGTIDES_AFFIXBUFFERED_DESC", $"<style=cIsHealth>Barrier does not decay</style>. Immune to knockback while <style=cIsHealing>barrier</style> is active. Attacks apply a <style=cIsUtility>random debuff</style> on hit. On Use, Gain <style=cIsHealth>{AffixBarrierEquipment.barrierRecharge.Value}%</style> of <style=cIsHealth>maximum health</style> as <style=cIsHealth>temporary barrier</style>.");

                // TODO: hooks
                AffixBarrier _instance = (AffixBarrier)staticAssetDictionary[typeof(AffixBarrier)];
                On.RoR2.HealthComponent.TakeDamageForce_Vector3_bool_bool += _instance.HealthComponent_TakeDamageForce_Vector3_bool_bool;
                On.RoR2.HealthComponent.TakeDamageForce_DamageInfo_bool_bool += _instance.HealthComponent_TakeDamageForce_DamageInfo_bool_bool;
                GenericGameEvents.OnApplyDamageReductionModifiers += _instance.GenericGameEvents_OnApplyDamageReductionModifiers;
                On.RoR2.CharacterBody.RecalculateStats += _instance.CharacterBody_RecalculateStats;
                IL.RoR2.CharacterBody.RecalculateStats += _instance.CharacterBody_RecalculateStats1;
                On.RoR2.CharacterBody.OnBuffFinalStackLost += _instance.CharacterBody_OnBuffFinalStackLost;
                On.RoR2.UI.HealthBar.UpdateBarInfos += _instance.HealthBar_UpdateBarInfos;
                IL.RoR2.HealthComponent.TakeDamage += _instance.HealthComponent_TakeDamage;
                On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, damageReport) =>
                {
                    orig(self, damageReport);
                    if (damageReport.victimBody?.HasBuff(buffDef) ?? false)
                    {
                        SphereSearch sphereSearch = new()
                        {
                            radius = DeathRange.Value + damageReport.victimBody.radius,
                            queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                            mask = LayerIndex.entityPrecise.mask,
                            origin = damageReport.victimBody.corePosition
                        };
                        sphereSearch.RefreshCandidates();
                        sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                        TeamMask mask = default; mask.AddTeam(damageReport.victimBody.teamComponent.teamIndex);
                        sphereSearch.FilterCandidatesByHurtBoxTeam(mask);
                        sphereSearch.GetHurtBoxes().Do(hurtBox => {
                            if (hurtBox?.healthComponent?.body != null && hurtBox.healthComponent.body != damageReport.victimBody)
                                hurtBox.healthComponent.AddBarrier(hurtBox.healthComponent.body.maxHealth * DeathHealth.Value);
                        });
                    }
                };
                Main.Harmony.PatchAll(typeof(PatchBuffered));
            }

            [HarmonyPatch]
            public class PatchBuffered
            {
                public static void ILManipulator(ILContext il) 
                {
                    ILCursor c = new(il);
                    while (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<BaseBuff>(nameof(buffDef))))
                    {
                        c.Emit(OpCodes.Pop);
                        c.EmitDelegate(() => instance.buffDef);
                    }
                }

                public static IEnumerable<MethodBase> TargetMethods()
                {
                    string[] list = { 
                        nameof(AffixBarrier.HealthComponent_TakeDamageForce_Vector3_bool_bool),
                        nameof(AffixBarrier.HealthComponent_TakeDamageForce_DamageInfo_bool_bool),
                        nameof(AffixBarrier.GenericGameEvents_OnApplyDamageReductionModifiers),
                        nameof(AffixBarrier.CharacterBody_RecalculateStats),
                        nameof(AffixBarrier.CharacterBody_RecalculateStats1),
                        nameof(AffixBarrier.CharacterBody_OnBuffFinalStackLost),
                        nameof(AffixBarrier.HealthBar_UpdateBarInfos),
                        nameof(AffixBarrier.HealthComponent_TakeDamage),
                        "<CharacterBody_RecalculateStats1>b__12_0",
                        "<HealthComponent_TakeDamage>b__15_0",
                    };
                    return AccessTools.GetDeclaredMethods(typeof(AffixBarrier)).Where(x => list.Contains(x.Name));
                        // .Concat(hidden.Select(x => AccessTools.DeclaredMethod(typeof(AffixBarrier).GetNestedType("<>c", AccessTools.all), x)));
                }
            }

            public override void AfterContentPackLoaded()
            {
                base.AfterContentPackLoaded();
                buffDef.eliteDef = EliteBuffered.instance.eliteDef;
            }
        }
    }
}
