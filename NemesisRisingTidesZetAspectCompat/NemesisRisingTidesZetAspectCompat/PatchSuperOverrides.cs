using HarmonyLib;

namespace NemesisRisingTides
{
    [HarmonyPatch(typeof(TPDespair.ZetAspects.Language), nameof(TPDespair.ZetAspects.Language.TextFragment))]
    public class PatchSuperOverrides
    {
        public static bool Prefix(string key, ref string __result)
        {
            if (Main.SuperOverrides.ContainsKey(key))
            {
                __result = Main.SuperOverrides[key];
                return false;
            }
            return true;
        }
    }
}