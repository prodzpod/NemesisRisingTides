using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RisingTides;
using RisingTides.Buffs;
using RisingTides.Equipment;
using RoR2;
using static RisingTides.Buffs.AffixImpPlane;

namespace NemesisRisingTides.Changes
{
    public class Realgar
    {
        public static bool enabled;
        public static string Description;
        public static ConfigEntry<bool> DisableOnUse;
        public static ConfigEntry<float> OnUseCooldown;
        public static ConfigEntry<bool> ChangeOnUse;
        public static void Init()
        {
            Description = $"Create a <style=cIsDamage>red fissure</style> that spews projectiles every <style=cIsDamage>{riftProjectileInterval.Value}s</style> that deals <style=cIsDamage>{riftProjectileDamage.Value}%</style> base damage. Attacks <style=cIsDamage>scar</style> all enemies on hit, dealing damage for <style=cIsDamage>{100f * scarDuration.Value * ImpPlaneScar.dotDef.damageCoefficient / ImpPlaneScar.dotDef.interval}%</style> base damage. On use, Gain <style=cIsHealth>temporary immunity</style> to all <style=cIsHealth>damage-over-time effects</style> for <style=cIsHealth>{AffixImpPlaneEquipment.duration.Value}s</style>.";
            enabled = Main.Config.Bind(nameof(Realgar) + " Elites", "Enable Changes", true, "").Value;
            if (!enabled) return;
            Main.Log.LogInfo("Applying change to " + nameof(Realgar) + " Elite");
            LanguageAPI.AddOverlay("EQUIPMENT_RISINGTIDES_AFFIXIMPPLANE_NAME", "What Remains");
            Main.SuperOverrides.Add("AFFIX_REALGAR_NAME", "What Remains");

            Main.AfterEquipContentPackLoaded += () => { RisingTidesContent.Equipment.RisingTides_AffixImpPlane.cooldown = OnUseCooldown.Value; };
            DisableOnUse = Main.Config.Bind(nameof(Realgar) + " Elites", "Disable On-use for enemies", false, "");
            OnUseCooldown = Main.Config.Bind(nameof(Realgar) + " Elites", "On-use Cooldown", 20f, "in seconds");
            ChangeOnUse = Main.Config.Bind(nameof(Realgar) + " Elites", "Rework On-use", true, "to pillar spawn");
            if (ChangeOnUse.Value)
            {
                Description = $"Create a <style=cIsDamage>red fissure</style> that spews projectiles every <style=cIsDamage>{riftProjectileInterval.Value}s</style> that deals <style=cIsDamage>{riftProjectileDamage.Value}%</style> base damage. Attacks <style=cIsDamage>scar</style> all enemies on hit, dealing damage for <style=cIsDamage>{100f * scarDuration.Value * ImpPlaneScar.dotDef.damageCoefficient / ImpPlaneScar.dotDef.interval}%</style> base damage. On use, Move the fissure to your position.";
                Main.SuperOverrides.Add("AFFIX_REALGAR_ACTIVE", "Move the fissure to your position.");
                LanguageAPI.AddOverlay("EQUIPMENT_RISINGTIDES_AFFIXIMPPLANE_PICKUP", "Become an aspect of cruelty. On use, summon a pillar that spawns projectiles periodically.");
                Main.SuperOverrides.Add("AFFIX_REALGAR_PICKUP", "Become an aspect of cruelty.");
                Main.SuperOverrides.Add("ASPECT_OF_EXTRINSICALITY", "<style=cDeath>Aspect of Cruelty</style> :");
            }
            Main.Harmony.PatchAll(typeof(PatchImpPlaneEquip));
        }

        [HarmonyPatch(typeof(AffixImpPlaneEquipment), nameof(AffixImpPlaneEquipment.OnUse))]
        public class PatchImpPlaneEquip
        {
            public static bool Prefix(ref bool __result, EquipmentSlot equipmentSlot)
            {
                __result = false;
                if (DisableOnUse.Value && equipmentSlot.characterBody.teamComponent.teamIndex != TeamIndex.Player) return false;
                if (ChangeOnUse.Value)
                {
                    if (equipmentSlot
                        && equipmentSlot.characterBody
                        && equipmentSlot.characterBody.GetComponent<RisingTidesAffixImpPlaneBehaviour>())
                    {
                        RisingTidesAffixImpPlaneBehaviour component = equipmentSlot.characterBody.GetComponent<RisingTidesAffixImpPlaneBehaviour>();
                        if (component.riftObject) UnityEngine.Object.DestroyImmediate(component.riftObject);
                        component.CreateRift();
                        EffectData effectData = new()
                        {
                            origin = component.body.corePosition    
                        };
                        effectData.SetNetworkedObjectReference(component.gameObject);
                        EffectManager.SpawnEffect(scarVFX, effectData, transmit: true);
                        __result = true;
                    }
                    return false;
                }   
                return true;
            }
        }
    }
}
