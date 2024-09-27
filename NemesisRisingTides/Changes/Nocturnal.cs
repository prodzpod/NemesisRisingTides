using BepInEx.Configuration;
using R2API;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine;
using RoR2;
using HarmonyLib;
using RisingTides.Buffs;
using static RisingTides.Buffs.AffixMoney;
using MysticsRisky2Utils.ContentManagement;
using RisingTides.Equipment;
using UnityEngine.Networking;
using RisingTides.Elites;
using RisingTides;
using MysticsRisky2Utils;

namespace NemesisRisingTides.Changes
{
    public class Nocturnal
    {
        public static bool enabled;
        public static string Description;
        public static ConfigEntry<bool> UpgradeToT2;
        public static ConfigEntry<float> BoostRange;
        public static ConfigEntry<bool> DisableOnUse;
        public static ConfigEntry<float> OnUseCooldown;
        public static ConfigEntry<bool> BetterBlindness;

        public static PostProcessVolume AngelsitePPV;
        public static void Init()
        {
            Description = $"Attacks apply <style=cIsUtility>darkened vision</style> on hit <style=cStack>(Vision range is reduced to {NightReducedVision.visionDistance.Value}m)</style>. Gain {NightSpeedBoost.movementSpeed.Value}% increased <style=cIsUtility>movement speed</style> and {NightSpeedBoost.attackSpeed.Value}% increased <style=cIsDamage>attack speed</style> while out of danger. On use, Gain <style=cIsUtility>invisibility</style> for <style=cIsUtility>{AffixNightEquipment.duration.Value}s</style>.";
            enabled = Main.Config.Bind(nameof(Nocturnal) + " Elites", "Enable Changes", true, "").Value;
            if (!enabled) return;
            Main.Log.LogInfo("Applying change to " + nameof(Nocturnal) + " Elite");
            LanguageAPI.AddOverlay("EQUIPMENT_RISINGTIDES_AFFIXNIGHT_NAME", "Phantom Lullaby"); // funny spikestrip reference here
            Main.SuperOverrides.Add("AFFIX_NIGHT_NAME", "Phantom Lullaby");

            UpgradeToT2 = Main.Config.Bind(nameof(Nocturnal) + " Elites", "Upgrade to T2", true, "");
            BoostRange = Main.Config.Bind(nameof(Nocturnal) + " Elites", "Boost Range", 35f, "in meters. set to 0 to disable.");
            DisableOnUse = Main.Config.Bind(nameof(Nocturnal) + " Elites", "Disable On-use for enemies", true, "");
            OnUseCooldown = Main.Config.Bind(nameof(Nocturnal) + " Elites", "On-use Cooldown", 40f, "in seconds");
            BetterBlindness = Main.Config.Bind(nameof(Nocturnal) + " Elites", "Replace Blind effect with Artifact of Blindness / WRB one", true, "hifu so goated");
            Main.AfterEquipContentPackLoaded += () => { RisingTidesContent.Equipment.RisingTides_AffixNight.cooldown = OnUseCooldown.Value; };
            if (UpgradeToT2.Value)
            {
                LanguageAPI.AddOverlay("ELITE_MODIFIER_RISINGTIDES_NIGHT", "Anglesite {0}");
                Main.AfterEliteContentPackLoaded += () =>
                {
                    ((Night)BaseLoadableAsset.staticAssetDictionary[typeof(Night)]).vanillaTier = 2;
                    ((Night)BaseLoadableAsset.staticAssetDictionary[typeof(Night)]).isHonor = false;
                };
                Main.AfterBuffContentPackLoaded += () =>
                {
                    RisingTidesContent.Buffs.RisingTides_AffixNight.iconSprite = Main.AssetBundle.LoadAsset<Sprite>("Assets/AnglesiteBuff.png");
                };
            }

            if (BoostRange.Value > 0)
            {
                Description = $"Attacks apply <style=cIsUtility>darkened vision</style> on hit, making you invisible to the enemy. Allies gain {NightSpeedBoost.movementSpeed.Value}% increased <style=cIsUtility>movement speed</style> and {NightSpeedBoost.attackSpeed.Value}% increased <style=cIsDamage>attack speed</style> while out of danger. On use, Gain <style=cIsUtility>invisibility</style> for <style=cIsUtility>{AffixNightEquipment.duration.Value}s</style>.";
                Main.SuperOverrides.Add("OOD_NIGHT_ATKSPD", "\nAllies gain {0} increased <style=cIsDamage>attack speed</style> while out of danger.");
                Main.SuperOverrides.Add("OOD_NIGHT_MOVSPD", "\nAllies gain {0} increased <style=cIsUtility>movement speed</style> and {1} increased <style=cIsDamage>attack speed</style> while out of danger.");
                Main.SuperOverrides.Add("OOD_NIGHT_BOTHSAMESPD", "\nAllies gain {0} increased <style=cIsUtility>movement speed</style> and <style=cIsDamage>attack speed</style> while out of danger.");
                Main.SuperOverrides.Add("OOD_NIGHT_BOTHDIFFSPD", "\nAllies gain {0} increased <style=cIsUtility>movement speed</style> and {1} increased <style=cIsDamage>attack speed</style> while out of danger.");
                Main.Harmony.PatchAll(typeof(PatchNightGained));
                Main.Harmony.PatchAll(typeof(PatchNightLost));
            }
            if (DisableOnUse.Value) Main.Harmony.PatchAll(typeof(PatchNightEquip));
            if (BetterBlindness.Value)
            {
                Main.SuperOverrides.Add("NIGHTBLIND_DETAIL", "");
                GameObject holder = new("Angelsite Fog");
                Main.Log.LogDebug(holder.ToString());
                Object.DontDestroyOnLoad(holder);
                holder.layer = LayerIndex.postProcess.intVal;
                AngelsitePPV = holder.AddComponent<PostProcessVolume>();
                Object.DontDestroyOnLoad(AngelsitePPV);
                AngelsitePPV.isGlobal = true;
                AngelsitePPV.weight = 0f;
                AngelsitePPV.priority = float.MaxValue;
                PostProcessProfile postProcessProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
                Object.DontDestroyOnLoad(postProcessProfile);
                postProcessProfile.name = "Angelsite Fog PP";
                RampFog fog = postProcessProfile.AddSettings<RampFog>();
                fog.SetAllOverridesTo(true, true);
                fog.fogColorStart.value = new Color32(0, 0, 0, 165);
                fog.fogColorMid.value = new Color32(1, 2, 44, byte.MaxValue);
                fog.fogColorEnd.value = new Color32(3, 49, 79, byte.MaxValue);
                fog.skyboxStrength.value = 0.02f;
                fog.fogPower.value = 0.35f;
                fog.fogIntensity.value = 1f;
                fog.fogZero.value = 0f;
                fog.fogOne.value = 0.3f;
                DepthOfField dof = postProcessProfile.AddSettings<DepthOfField>();
                dof.SetAllOverridesTo(true, true);
                dof.aperture.value = 5f;
                dof.focalLength.value = 68.31f;
                dof.focusDistance.value = 5f;
                AngelsitePPV.sharedProfile = postProcessProfile;

                On.RoR2.CharacterBody.GetVisibilityLevel_CharacterBody += HandleAIBlindness;
                On.RoR2.CharacterBody.FixedUpdate += HandlePlayerBlindness;
                Main.AfterBuffContentPackLoaded += () => { On.RoR2.CharacterBody.RecalculateStats -= ((NightReducedVision)BaseLoadableAsset.staticAssetDictionary[typeof(NightReducedVision)]).CharacterBody_RecalculateStats; };
            }
        }

        private static VisibilityLevel HandleAIBlindness(On.RoR2.CharacterBody.orig_GetVisibilityLevel_CharacterBody orig, CharacterBody self, CharacterBody observer)
        {
            VisibilityLevel ret = orig(self, observer);
            if (observer.HasBuff(RisingTidesContent.Buffs.RisingTides_NightReducedVision)) return VisibilityLevel.Cloaked;
            return ret;
        }

        private static void HandlePlayerBlindness(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody body)
        {
            orig(body);
            if (LocalUserManager.GetFirstLocalUser() != null && LocalUserManager.GetFirstLocalUser().cachedBody == body)
            {
                float weight = body.HasBuff(RisingTidesContent.Buffs.RisingTides_NightReducedVision) ? 1 : 0;
                if (weight == AngelsitePPV.weight) return;
                AngelsitePPV.weight = Mathf.Lerp(AngelsitePPV.weight, weight, 0.1f);
                if (Mathf.Abs(AngelsitePPV.weight - weight) < 0.001f) AngelsitePPV.weight = weight;
            }
        }

        public class NemesisAffixNightBehaviour : MonoBehaviour
        {
            public RisingTidesAffixMoneyAuraComponent aura;
            public CharacterBody body;
            public SphereSearch sphereSearch;
            public float auraRadius = 0f;

            public void Awake()
            {
                body = GetComponent<CharacterBody>();
                auraRadius += BoostRange.Value + body.radius;
                sphereSearch = new SphereSearch
                {
                    radius = auraRadius,
                    queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                    mask = LayerIndex.entityPrecise.mask
                };
            }

            public void Start()
            {
                GameObject gameObject = Main.MakeAura(body, BoostRange.Value + body.radius, new Color(0, 0, 0.5f, 0.5f));
                gameObject.transform.localScale = Vector3.one * auraRadius;
                aura = gameObject.GetComponent<RisingTidesAffixMoneyAuraComponent>();
            }

            public void FixedUpdate()
            {
                if (!body.healthComponent || !body.healthComponent.alive || !NetworkServer.active) return;
                sphereSearch.origin = body.corePosition;
                sphereSearch.RefreshCandidates();
                sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                TeamMask mask = default; mask.AddTeam(body.teamComponent.teamIndex);
                sphereSearch.FilterCandidatesByHurtBoxTeam(mask);
                sphereSearch.GetHurtBoxes().Do(hurtBox => {
                    if (hurtBox
                        && hurtBox.healthComponent
                        && hurtBox.healthComponent.body 
                        && hurtBox.healthComponent.body != body) 
                        hurtBox.healthComponent.body.AddTimedBuff(RisingTidesContent.Buffs.RisingTides_NightSpeedBoost, 4f);
                });
            }

            public void OnEnable() { if ((bool)aura) aura.gameObject.SetActive(value: true); }
            public void OnDisable() { if ((bool)aura) aura.gameObject.SetActive(value: false); }
        }

        [HarmonyPatch(typeof(AffixNight), nameof(AffixNight.CharacterBody_OnBuffFirstStackGained))]
        public class PatchNightGained
        {
            public static bool Prefix(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
            {
                orig(self, buffDef);
                if (buffDef == RisingTidesContent.Buffs.RisingTides_AffixNight)
                {
                    NemesisAffixNightBehaviour component = self.GetComponent<NemesisAffixNightBehaviour>();
                    if (!component) self.gameObject.AddComponent<NemesisAffixNightBehaviour>();
                    else if (!component.enabled) component.enabled = true;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(AffixNight), nameof(AffixNight.CharacterBody_OnBuffFinalStackLost))]
        public class PatchNightLost
        {
            public static bool Prefix(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
            {
                orig(self, buffDef);
                if (buffDef == RisingTidesContent.Buffs.RisingTides_AffixNight)
                {
                    NemesisAffixNightBehaviour component = self.GetComponent<NemesisAffixNightBehaviour>();
                    if (component && component.enabled) component.enabled = false;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(AffixNightEquipment), nameof(AffixNightEquipment.OnUse))]
        public class PatchNightEquip
        {
            public static bool Prefix(EquipmentSlot equipmentSlot)
            {
                if (equipmentSlot.characterBody.teamComponent.teamIndex == TeamIndex.Player) return true;
                return false;
            }
        }
    }
}
