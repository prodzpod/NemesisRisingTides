using BepInEx.Configuration;
using HarmonyLib;
using MysticsRisky2Utils;
using MysticsRisky2Utils.BaseAssetTypes;
using R2API;
using RisingTides.Equipment;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RisingTides.Buffs.AffixMoney;
using static RoR2.BlastAttack;

namespace NemesisRisingTides.Contents
{
    public class Oppressive
    {
        public class EliteOppressive : BaseElite
        {
            public static EliteOppressive instance;
            public override void OnLoad()
            {
                base.OnLoad();
                instance = this;
                eliteDef.name = "NemesisRisingTides_Oppressive";
                vanillaTier = 1;
                isHonor = true;
                eliteDef.healthBoostCoefficient = Main.Config.Bind(nameof(Oppressive) + "Elites", "Health Boost Coefficient", 4f, "How much health this elite should have? (e.g. 18 means it will have 18x health)").Value;
                eliteDef.damageBoostCoefficient = Main.Config.Bind(nameof(Oppressive) + "Elites", "Damage Boost Coefficient", 2f, "How much damage this elite should have? (e.g. 6 means it will have 6x damage)").Value;
                EliteRamp.AddRamp(eliteDef, Main.AssetBundle.LoadAsset<Texture2D>("Assets/OppressiveRamp.png"));
            }

            public override void AfterContentPackLoaded()
            {
                base.AfterContentPackLoaded();
                eliteDef.eliteEquipmentDef = Main.Config.Bind("Enabled Elites", nameof(Oppressive), true, "").Value ? EquipOppressive.instance.equipmentDef : null;
            }
        }

        public class EquipOppressive : BaseEliteAffix
        {
            public static EquipOppressive instance;
            public static ConfigEntry<bool> DisableOnUse;
            public static ConfigEntry<float> ForceRange;
            public static ConfigEntry<float> ForceActive;
            public static ConfigEntry<float> ActiveDuration;
            public override void OnLoad()
            {
                base.OnLoad();
                DisableOnUse = Main.Config.Bind(nameof(Oppressive) + " Elites", "Disable On-use for enemies", true, "");
                ForceRange = Main.Config.Bind(nameof(Oppressive) + " Elites", "Range", 13f, "");
                ForceActive = Main.Config.Bind(nameof(Oppressive) + " Elites", "Downward Force", 4000f, "");
                ActiveDuration = Main.Config.Bind(nameof(Oppressive) + " Elites", "No Jump Duration", 4f, "");
                instance = this;
                equipmentDef.name = "NemesisRisingTides_AffixOppressive";
                ConfigOptions.ConfigurableValue.CreateFloat(Main.PluginGUID, Main.PluginName, Main.Config, typeof(Oppressive) + "Elites", "On-use Cooldown", 10f, 0f, 1000f, "", null, null, restartRequired: false, delegate (float newValue) { equipmentDef.cooldown = newValue; });
                equipmentDef.pickupIconSprite = Main.AssetBundle.LoadAsset<Sprite>("Assets/OppressiveEquip.png");
                SetUpPickupModel();
                AdjustElitePickupMaterial(Color.white, 1.6f, Main.AssetBundle.LoadAsset<Texture2D>("Assets/OppressiveRamp.png"));
                GameObject wrapper = new GameObject().InstantiateClone("NemesisRisingTidesAffixOppressiveHeadpieceWrapper", registerNetwork: false);
                GameObject crown = Main.AssetBundle.LoadAsset<GameObject>("Assets/AffixOppressiveHeadpiece.prefab").InstantiateClone("NemesisRisingTidesAffixOppressiveHeadpiece", registerNetwork: false);
                crown.transform.localScale = Vector3.one * 200;
                crown.transform.Find("mdlAffixOppressiveCrown").GetComponent<MeshFilter>().sharedMesh = Main.AssetBundle.LoadAsset<Mesh>("Assets/OppressiveCrown.asset");
                Material mat = crown.transform.Find("mdlAffixOppressiveCrown").GetComponent<MeshRenderer>().sharedMaterial;
                mat.color = new Color(1, 1, 1, 0);
                mat.SetTexture("_MainTex", Main.AssetBundle.LoadAsset<Texture2D>("Assets/OppressiveCrownTexture.png"));
                crown.transform.Find("mdlAffixOppressiveCrown").GetComponent<MeshRenderer>().sharedMaterial = mat;
                crown.transform.SetParent(wrapper.transform);
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
                if (equipmentSlot.characterBody == null || (DisableOnUse.Value && equipmentSlot.characterBody.teamComponent.teamIndex != TeamIndex.Player)) return false;
                if (ForceRange.Value > 0)
                {
                    BlastAttack attack = new()
                    {
                        attacker = equipmentSlot.characterBody.gameObject,
                        inflictor = equipmentSlot.characterBody.gameObject,
                        teamIndex = equipmentSlot.characterBody.teamComponent.teamIndex,
                        position = equipmentSlot.characterBody.corePosition,
                        procCoefficient = 1f,
                        radius = ForceRange.Value,
                        baseDamage = 0f,
                        falloffModel = FalloffModel.Linear,
                        damageColorIndex = DamageColorIndex.Item,
                        attackerFiltering = AttackerFiltering.NeverHitSelf
                    };
                    attack.Fire().hitPoints.Do(hitPoint =>
                    {
                        hitPoint.hurtBox?.healthComponent?.TakeDamageForce(Physics.gravity * ForceActive.Value);
                        hitPoint.hurtBox?.healthComponent?.body?.AddTimedBuff(AffixOppressive.NoJump, ActiveDuration.Value);
                    });
                    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ProcStealthkit"), new EffectData
                    {
                        origin = equipmentSlot.characterBody.corePosition,
                        rotation = Quaternion.identity
                    }, transmit: true);
                    return true;
                }
                return false;
            }

            public override void AfterContentPackLoaded()
            {
                base.AfterContentPackLoaded();
                equipmentDef.passiveBuffDef = AffixOppressive.instance.buffDef;
            }

        }

        public class AffixOppressive : BaseBuff
        {
            public static AffixOppressive instance;
            public static ConfigEntry<float> Range;
            public static ConfigEntry<float> ForcePassive;
            public static ConfigEntry<float> DisableDuration;
            public static BuffDef StrongerGravity;
            public static BuffDef NoJump;

            public override void OnPluginAwake()
            {
                base.OnPluginAwake();

                Range = Main.Config.Bind(nameof(Oppressive) + " Elites", "Range", 13f, "");
                ForcePassive = Main.Config.Bind(nameof(Oppressive) + " Elites", "Extra Gravity in Zone", 50f, "");
                DisableDuration = Main.Config.Bind(nameof(Oppressive) + " Elites", "No Jump Duration", 4f, "on hit");

                LanguageAPI.Add("ELITE_MODIFIER_NEMESISRISINGTIDES_OPPRESSIVE", "Oppressive {0}");
                LanguageAPI.Add("EQUIPMENT_NEMESISRISINGTIDES_AFFIXOPPRESSIVE_NAME", "Titanic Pause");
                LanguageAPI.Add("EQUIPMENT_NEMESISRISINGTIDES_AFFIXOPPRESSIVE_PICKUP", "Become an aspect of domination.");
                LanguageAPI.Add("EQUIPMENT_NEMESISRISINGTIDES_AFFIXOPPRESSIVE_DESC", $"Creates a <style=cIsUtility>{Range.Value}m</style> wide area that <style=cIsUtility>increases gravity</style> around it. On hit, <style=cIsUtility>drop</style> airborne enemy and <style=cIsUtility>disables jump</style> for <style=cIsUtility>{DisableDuration.Value}s</style>.");

                StrongerGravity = ScriptableObject.CreateInstance<BuffDef>();
                StrongerGravity.canStack = false;
                StrongerGravity.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffSlow50Icon.tif").WaitForCompletion();
                StrongerGravity.buffColor = new Color(1f, 0.8f, 0.8f);
                ContentAddition.AddBuffDef(StrongerGravity);

                NoJump = ScriptableObject.CreateInstance<BuffDef>();
                NoJump.canStack = false;
                NoJump.iconSprite = Main.AssetBundle.LoadAsset<Sprite>("Assets/OppressiveEffect.png");
                NoJump.buffColor = new Color(0.8f, 0.8f, 1f);
                ContentAddition.AddBuffDef(NoJump);
            }

            public override void OnLoad()
            {
                base.OnLoad();
                instance = this;
                buffDef.name = "NemesisRisingTides_AffixOppressive";
                buffDef.iconSprite = Main.AssetBundle.LoadAsset<Sprite>("Assets/OppressiveBuff.png");

                On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBody_OnBuffFirstStackGained;
                On.RoR2.CharacterBody.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
                GenericGameEvents.OnHitEnemy += GenericGameEvents_OnHitEnemy;
            }

            public override void AfterContentPackLoaded()
            {
                base.AfterContentPackLoaded();
                buffDef.eliteDef = EliteOppressive.instance.eliteDef;
            }

            public class NemesisAffixOppressiveBehaviour : MonoBehaviour
            {
                public RisingTidesAffixMoneyAuraComponent aura;
                public CharacterBody body;
                public float auraRadius = 0f;

                public void Awake()
                {
                    body = GetComponent<CharacterBody>();
                    auraRadius += Range.Value + body.radius;
                }

                public void Start()
                {
                    GameObject gameObject = Main.MakeAura(body, Range.Value + body.radius, new Color(1f, 0.6f, 1f, 0.25f));
                    foreach (string k in new string[] { "SphereOuter", "SphereInner" })
                    {
                        Transform t = gameObject.transform.Find(k);
                        t.GetComponent<MeshFilter>().sharedMesh = Main.AssetBundle.LoadAsset<Mesh>("Assets/Cylinder.asset");
                        t.localPosition = new Vector3(0, 4, 0);
                        t.eulerAngles = new Vector3(90, 0, 0);
                        t.localScale = new Vector3(110, 110, 450);
                    }
                    gameObject.transform.localScale = Vector3.one * auraRadius;
                    aura = gameObject.GetComponent<RisingTidesAffixMoneyAuraComponent>();
                }

                public void FixedUpdate()
                {
                    if (!body.healthComponent || !body.healthComponent.alive || !NetworkServer.active) return;
                    Physics.OverlapCapsule(body.corePosition, body.corePosition + (Vector3.up * 200f), auraRadius).Do(collider =>
                    {
                        CharacterBody victim = collider.GetComponent<HurtBox>()?.healthComponent?.body;
                        if (victim == null || !FriendlyFireManager.ShouldDirectHitProceed(victim.healthComponent, body.teamComponent.teamIndex)) return;
                        victim.AddTimedBuff(StrongerGravity, 4f);
                    });
                }

                public void OnEnable() { if ((bool)aura) aura.gameObject.SetActive(value: true); }
                public void OnDisable() { if ((bool)aura) aura.gameObject.SetActive(value: false); }
            }

            public class StrongerGravityBehaviour : MonoBehaviour
            {
                public CharacterBody body;
                public void Start() { body = GetComponent<CharacterBody>(); }
                public void FixedUpdate()
                {
                    if (!NetworkServer.active) return;
                    body.healthComponent?.TakeDamageForce(Physics.gravity * Time.fixedDeltaTime * ForcePassive.Value, alwaysApply: true);
                }
            }
            public class NoJumpBehaviour : MonoBehaviour
            {
                public CharacterBody body;
                public void Start() { body = GetComponent<CharacterBody>(); }
                public void FixedUpdate()
                {
                    if (!NetworkServer.active || body.characterMotor == null) return;
                    if (body.characterMotor.velocity.y > 0) body.characterMotor.velocity.y = 0;
                }
            }

            private void CharacterBody_OnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
            {
                orig(self, buffDef);
                if (buffDef == base.buffDef)
                {
                    NemesisAffixOppressiveBehaviour component = self.GetComponent<NemesisAffixOppressiveBehaviour>();
                    if (!component) self.gameObject.AddComponent<NemesisAffixOppressiveBehaviour>();
                    else if (!component.enabled) component.enabled = true;
                }
                if (buffDef == StrongerGravity)
                {
                    StrongerGravityBehaviour component = self.GetComponent<StrongerGravityBehaviour>();
                    if (!component) self.gameObject.AddComponent<StrongerGravityBehaviour>();
                    else if (!component.enabled) component.enabled = true;
                }
                if (buffDef == NoJump)
                {
                    NoJumpBehaviour component = self.GetComponent<NoJumpBehaviour>();
                    if (!component) self.gameObject.AddComponent<NoJumpBehaviour>();
                    else if (!component.enabled) component.enabled = true;
                }
            }

            private void CharacterBody_OnBuffFinalStackLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
            {
                orig(self, buffDef);
                if (buffDef == base.buffDef)
                {
                    NemesisAffixOppressiveBehaviour component = self.GetComponent<NemesisAffixOppressiveBehaviour>();
                    if ((bool)component && component.enabled) component.enabled = false;
                }
                if (buffDef == StrongerGravity)
                {
                    StrongerGravityBehaviour component = self.GetComponent<StrongerGravityBehaviour>();
                    if ((bool)component && component.enabled) component.enabled = false;
                }
                if (buffDef == NoJump)
                {
                    NoJumpBehaviour component = self.GetComponent<NoJumpBehaviour>();
                    if ((bool)component && component.enabled) component.enabled = false;
                }
            }

            private void GenericGameEvents_OnHitEnemy(DamageInfo damageInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo attackerInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo victimInfo)
            {
                if (!damageInfo.rejected && damageInfo.procCoefficient > 0f && (bool)attackerInfo.body && attackerInfo.body.HasBuff(buffDef) && (bool)victimInfo.healthComponent && (bool)victimInfo.body && ((!(Object)(object)victimInfo.body.characterMotor && (bool)victimInfo.body.rigidbody) || (bool)(Object)(object)victimInfo.body.characterMotor))
                {
                    EffectData effectData = new()
                    {
                        origin = victimInfo.body.corePosition,
                        scale = victimInfo.body.radius
                    };
                    effectData.SetNetworkedObjectReference(victimInfo.gameObject);
                    EffectManager.SpawnEffect(gravityVFX, effectData, transmit: true);
                    victimInfo.body.AddTimedBuff(NoJump, DisableDuration.Value);
                    victimInfo.healthComponent?.TakeDamageForce(Physics.gravity * ForcePassive.Value, alwaysApply: true);
                }
            }
        }
    }
}
