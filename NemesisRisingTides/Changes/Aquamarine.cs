using BepInEx.Configuration;
using HarmonyLib;
using RisingTides.Buffs;
using RisingTides.Equipment;
using RoR2;
using UnityEngine.Networking;
using UnityEngine;
using static RisingTides.Buffs.AffixMoney;
using R2API;
using UnityEngine.AddressableAssets;
using RisingTides;

namespace NemesisRisingTides.Changes
{
    public class Aquamarine
    {
        public static bool enabled;
        public static string Description;
        public static ConfigEntry<float> Range;
        public static ConfigEntry<int> NullifyHits;
        public static ConfigEntry<bool> IncludeSelf;
        public static ConfigEntry<bool> DisableOnUse;
        public static ConfigEntry<float> OnUseCooldown;

        public static BuffDef AffectedBuff;
        public static BuffDef StackBuff;
        public static void Init()
        {
            Description = $"Become invulnerable while not attacking. Attacks <style=cIsUtility>imprison</style> enemies in a bubble on hit. On Use, <style=cIsUtility>Cleanse</style> all debuffs and <style=cIsHealth>heal</style> for <style=cIsHealth>{AffixWaterEquipment.healAmount.Value}%</style> of <style=cIsHealth>maximum health</style>.";
            enabled = Main.Config.Bind(nameof(Aquamarine) + " Elites", "Enable Changes", true, "").Value;
            if (!enabled) return;
            Main.Log.LogInfo("Applying change to " + nameof(Aquamarine) + " Elite");

            Range = Main.Config.Bind(nameof(Aquamarine) + " Elites", "Range", 35f, "in meters. set to 0 to disable.");
            NullifyHits = Main.Config.Bind(nameof(Aquamarine) + " Elites", "Nullify Stack", 3, "every N hits is nullified");
            IncludeSelf = Main.Config.Bind(nameof(Aquamarine) + " Elites", "Include Self", true, "Whether to inflict itself with stack nullify");
            DisableOnUse = Main.Config.Bind(nameof(Aquamarine) + " Elites", "Disable On-use for enemies", true, "");
            OnUseCooldown = Main.Config.Bind(nameof(Aquamarine) + " Elites", "On-use Cooldown", 30f, "in seconds");
            Main.AfterEquipContentPackLoaded += () => { RisingTidesContent.Equipment.RisingTides_AffixWater.cooldown = OnUseCooldown.Value; };

            Main.Harmony.PatchAll(typeof(PatchWaterGained));
            Main.Harmony.PatchAll(typeof(PatchWaterLost));
            if (Range.Value > 0)
            {
                Description = $"<style=cIsHealth>Block</style> every <style=cIsHealth>3</style> hits. Attacks <style=cIsUtility>imprison</style> enemies in a bubble on hit. On Use, <style=cIsUtility>Cleanse</style> all debuffs and <style=cIsHealth>gain some health</style>.";
                Main.SuperOverrides.Add("PASSIVE_IDLE_INVULN", "\n<style=cIsHealth>Block</style> every <style=cIsHealth>3</style> hits.");

                AffectedBuff = ScriptableObject.CreateInstance<BuffDef>();
                AffectedBuff.canStack = false;
                AffectedBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Junk/Common/texBuffBodyArmorIcon.tif").WaitForCompletion();
                AffectedBuff.buffColor = Color.cyan;
                ContentAddition.AddBuffDef(AffectedBuff);

                StackBuff = ScriptableObject.CreateInstance<BuffDef>();
                StackBuff.canStack = true;
                StackBuff.isCooldown = true;
                StackBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion();
                StackBuff.buffColor = Color.cyan;
                ContentAddition.AddBuffDef(StackBuff);

                On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
                {
                    CharacterBody victim = self?.body;
                    if (!damageInfo.rejected && damageInfo.procCoefficient > 0f && victim != null && (victim.HasBuff(AffectedBuff) || (IncludeSelf.Value && victim.HasBuff(RisingTidesContent.Buffs.RisingTides_AffixWater))))
                    {
                        victim.AddBuff(StackBuff);
                        if (victim.GetBuffCount(StackBuff) >= NullifyHits.Value)
                        {
                            victim.SetBuffCount(StackBuff.buffIndex, 0);
                            damageInfo.rejected = true;
                            EffectData effectData = new()
                            {
                                origin = damageInfo.position,
                                rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : Random.onUnitSphere)
                            };
                            EffectManager.SpawnEffect(HealthComponent.AssetReferences.bearEffectPrefab, effectData, transmit: true);
                        }
                    }
                    orig(self, damageInfo);
                };
            }
            if (DisableOnUse.Value) Main.Harmony.PatchAll(typeof(PatchWaterEquip));
        }

        public class NemesisAffixWaterBehaviour : MonoBehaviour
        {
            public RisingTidesAffixMoneyAuraComponent aura;
            public CharacterBody body;
            public SphereSearch sphereSearch;
            public float auraRadius = 0f;

            public void Awake()
            {
                body = GetComponent<CharacterBody>();
                auraRadius += Range.Value + body.radius;
                sphereSearch = new SphereSearch
                {
                    radius = auraRadius,
                    queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                    mask = LayerIndex.entityPrecise.mask
                };
            }

            public void Start()
            {
                GameObject gameObject = Main.MakeAura(body, Range.Value + body.radius, new Color(0, 0.5f, 0.5f, 0.5f));
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
                    if (hurtBox?.healthComponent?.body != null && hurtBox.healthComponent.body != body) hurtBox.healthComponent.body.AddTimedBuff(AffectedBuff, 4f);
                });
            }

            public void OnEnable() { if ((bool)aura) aura.gameObject.SetActive(value: true); }
            public void OnDisable() { if ((bool)aura) aura.gameObject.SetActive(value: false); }
        }

        [HarmonyPatch(typeof(AffixWater), nameof(AffixWater.CharacterBody_OnBuffFirstStackGained))]
        public class PatchWaterGained
        {
            public static bool Prefix(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
            {
                orig(self, buffDef);
                if (Range.Value > 0 && buffDef == RisingTidesContent.Buffs.RisingTides_AffixWater)
                {
                    NemesisAffixWaterBehaviour component = self.GetComponent<NemesisAffixWaterBehaviour>();
                    if (!component) self.gameObject.AddComponent<NemesisAffixWaterBehaviour>();
                    else if (!component.enabled) component.enabled = true;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(AffixWater), nameof(AffixWater.CharacterBody_OnBuffFinalStackLost))]
        public class PatchWaterLost
        {
            public static bool Prefix(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
            {
                orig(self, buffDef);
                if (Range.Value > 0 && buffDef == RisingTidesContent.Buffs.RisingTides_AffixWater)
                {
                    NemesisAffixWaterBehaviour component = self.GetComponent<NemesisAffixWaterBehaviour>();
                    if (component?.enabled ?? false) component.enabled = false;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(AffixWaterEquipment), nameof(AffixWaterEquipment.OnUse))]
        public class PatchWaterEquip
        {
            public static bool Prefix(EquipmentSlot equipmentSlot)
            {
                if (equipmentSlot.characterBody.teamComponent.teamIndex == TeamIndex.Player) return true;
                return false;
            }
        }
    }
}
