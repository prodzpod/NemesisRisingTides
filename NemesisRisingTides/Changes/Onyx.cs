using BepInEx.Configuration;
using HarmonyLib;
using RisingTides;
using RisingTides.Buffs;
using RisingTides.Equipment;
using RoR2;

namespace NemesisRisingTides.Changes
{
    public class Onyx
    {
        public static bool enabled;
        public static string Description;
        public static ConfigEntry<bool> DisableOnUse;
        public static ConfigEntry<float> OnUseCooldown;
        public static void Init()
        {
            Description = $"Attacks apply a <style=cIsDamage>mark</style> on hit, detonating for <style=cIsDamage>{AffixBlackHole.markBaseDamage.Value}%</style> <style=cStack>(+{AffixBlackHole.markBaseDamage.Value * 0.2f}% per level)</style> base damage when <style=cIsDamage>7</style> stacks are applied. Marked enemies are <style=cIsUtility>pulled</style> towards you. On use, Fire a <style=cIsDamage>homing attack</style> at all marked enemies that deals <style=cIsDamage>{AffixBlackHoleEquipment.detonationDamagePerMark.Value}%</style> base damage per stack of mark.";
            enabled = Main.Config.Bind(nameof(Onyx) + " Elites", "Enable Changes", true, "").Value;
            if (!enabled) return;
            Main.Log.LogInfo("Applying change to " + nameof(Onyx) + " Elite");

            Main.AfterEquipContentPackLoaded += () => { RisingTidesContent.Equipment.RisingTides_AffixBlackHole.cooldown = OnUseCooldown.Value; };
            DisableOnUse = Main.Config.Bind(nameof(Onyx) + " Elites", "Disable On-use for enemies", false, "");
            OnUseCooldown = Main.Config.Bind(nameof(Onyx) + " Elites", "On-use Cooldown", 10f, "in seconds");
            Main.Harmony.PatchAll(typeof(PatchBlackHoleEquip));
        }

        [HarmonyPatch(typeof(AffixBlackHoleEquipment), nameof(AffixBlackHoleEquipment.OnUse))]
        public class PatchBlackHoleEquip
        {
            public static bool Prefix(EquipmentSlot equipmentSlot)
            {
                if (!DisableOnUse.Value || equipmentSlot.characterBody.teamComponent.teamIndex == TeamIndex.Player) return true;
                return false;
            }
        }
    }   
}
