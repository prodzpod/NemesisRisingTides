using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using NemesisRisingTides.Changes;
using System;
using MysticsRisky2Utils.BaseAssetTypes;
using System.Reflection;
using UnityEngine;
using RoR2;
using static RisingTides.Buffs.AffixMoney;
using RisingTides;
using RoR2.ContentManagement;
using System.Collections.Generic;
using R2API;
using System.Linq;
using NemesisRisingTides.Contents;

namespace NemesisRisingTides
{
    [BepInDependency(RisingTidesPlugin.PluginGUID)]
    [BepInDependency("com.Wolfo.WolfoQualityOfLife", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "prodzpod";
        public const string PluginName = "NemesisRisingTides";
        public const string PluginVersion = "1.0.9";
        public static ManualLogSource Log;
        public static PluginInfo pluginInfo;
        public static Harmony Harmony;
        public static ConfigFile Config;
        public static Dictionary<string, string> SuperOverrides = [];

        private static AssetBundle _assetBundle;
        public static AssetBundle AssetBundle
        {
            get
            {
                if (_assetBundle == null)
                    _assetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pluginInfo.Location), "nemesisrisingtides"));
                return _assetBundle;
            }
        }

        public static event Action AfterBuffContentPackLoaded;
        public static event Action AfterEquipContentPackLoaded;
        public static event Action AfterEliteContentPackLoaded;

        public static readonly string[] Pickups = new[] { "EQUIPMENT_RISINGTIDES_AFFIXMONEY_PICKUP", "EQUIPMENT_RISINGTIDES_AFFIXNIGHT_PICKUP", "EQUIPMENT_RISINGTIDES_AFFIXWATER_PICKUP", "EQUIPMENT_RISINGTIDES_AFFIXBARRIER_PICKUP", "EQUIPMENT_RISINGTIDES_AFFIXIMPPLANE_PICKUP", "EQUIPMENT_RISINGTIDES_AFFIXBLACKHOLE_PICKUP", "EQUIPMENT_RISINGTIDES_AFFIXMIRROR_PICKUP" };

        public void Awake()
        {
            pluginInfo = Info;
            Log = Logger;
            Harmony = new Harmony(PluginGUID);
            Config = new ConfigFile(System.IO.Path.Combine(Paths.ConfigPath, PluginGUID + ".cfg"), true);
            Harmony.PatchAll(typeof(PatchPostinits));

            Magnetic.Init(); LanguageAPI.Add("EQUIPMENT_RISINGTIDES_AFFIXMONEY_DESC", Magnetic.Description);
            Nocturnal.Init(); LanguageAPI.Add("EQUIPMENT_RISINGTIDES_AFFIXNIGHT_DESC", Nocturnal.Description);
            Aquamarine.Init(); LanguageAPI.Add("EQUIPMENT_RISINGTIDES_AFFIXWATER_DESC", Aquamarine.Description);
            Bismuth.Init(); LanguageAPI.Add("EQUIPMENT_RISINGTIDES_AFFIXBARRIER_DESC", Bismuth.Description);
            Onyx.Init(); LanguageAPI.Add("EQUIPMENT_RISINGTIDES_AFFIXBLACKHOLE_DESC", Onyx.Description); 
            Realgar.Init(); LanguageAPI.Add("EQUIPMENT_RISINGTIDES_AFFIXIMPPLANE_DESC", Realgar.Description);
            if (Config.Bind("Misc", "Change Blighted Name", true, "").Value) LanguageAPI.AddOverlay("ELITE_MODIFIER_BLIGHTED_MOFFEIN", "Obsidian {0}");

            Assembly executingAssembly = Assembly.GetExecutingAssembly(); // :3
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<BaseEquipment>(executingAssembly);
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<BaseBuff>(executingAssembly);
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<BaseElite>(executingAssembly);
            ContentManager.collectContentPackProviders += delegate (ContentManager.AddContentPackProviderDelegate addContentPackProvider) { addContentPackProvider(new Content()); };
            AfterEquipContentPackLoaded += () =>
            {
                EquipmentDef[] defs = new EquipmentDef[] { RisingTidesContent.Equipment.RisingTides_AffixBlackHole, RisingTidesContent.Equipment.RisingTides_AffixNight, RisingTidesContent.Equipment.RisingTides_AffixWater, RisingTidesContent.Equipment.RisingTides_AffixMoney, RisingTidesContent.Equipment.RisingTides_AffixBarrier, RisingTidesContent.Equipment.RisingTides_AffixImpPlane, Buffered.EquipBuffered.instance.equipmentDef, Oppressive.EquipOppressive.instance.equipmentDef };
                foreach (EquipmentDef def in defs) def.dropOnDeathChance = 0.0003f;
            };
            On.RoR2.Language.GetLocalizedStringByToken += (orig, self, token) =>
            {
                if (self.TokenIsRegistered("EQUIPMENT_AFFIXRED_DESC") && Pickups.Contains(token)) return orig(self, token).Split('.')[0] + "."; // Pickup truncation if descmod is enabled - Only the first sentence!
                return orig(self, token);
            };
        }

        [HarmonyPatch(typeof(MysticsRisky2Utils.ContentManagement.ContentLoadHelper), nameof(MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded), typeof(Assembly), typeof(Type))]
        public class PatchPostinits
        {
            public static void Postfix(Assembly assembly, Type loadType)
            {
                if (assembly != RisingTidesPlugin.executingAssembly) return;
                if (loadType == typeof(BaseBuff) && AfterBuffContentPackLoaded != null) AfterBuffContentPackLoaded();
                else if (loadType == typeof(BaseEquipment) && AfterEquipContentPackLoaded != null) AfterEquipContentPackLoaded();
                else if (loadType == typeof(BaseElite) && AfterEliteContentPackLoaded != null) AfterEliteContentPackLoaded();
            }
        }

        public static GameObject MakeAura(CharacterBody body, float radius, Color color, Texture texture = null)
        {
            GameObject ret = Instantiate(RisingTidesAffixMoneyBehaviour.auraPrefab, body.transform);
            ret.name = "Aura";
            ret.transform.localScale = Vector3.one * radius;
            RisingTidesAffixMoneyAuraComponent component = ret.GetComponent<RisingTidesAffixMoneyAuraComponent>();
            Material mat1 = Instantiate(component.outerMaterial);
            Material mat2 = Instantiate(component.innerMaterial);
            ParticleSystemRenderer psr = ret.transform.Find("Stars").GetComponent<ParticleSystemRenderer>();
            Material mat3 = Instantiate(psr.sharedMaterial);
            mat1.color = color;
            mat2.color = color;
            Color color2 = new(color.r, color.g, color.b); // un-alpha'ed
            mat3.color = color2;
            mat2.SetTexture("_Cloud1Tex", Texture2D.blackTexture);
            mat2.SetTexture("_Cloud2Tex", texture ?? Texture2D.blackTexture);
            component.outerMaterial = mat1;
            component.innerMaterial = mat2;
            psr.sharedMaterial = mat3;
            component.outerSphereRenderer.sharedMaterial = mat1;
            component.innerSphereRenderer.sharedMaterial = mat2;
            ret.transform.SetParent(body.transform);
            return ret;
        }

        public class Content : IContentPackProvider
        {
            public static ContentPack contentPack = new();
            public string identifier => "Nemesis Rising Tides";

            public System.Collections.IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
            {
                contentPack.identifier = identifier;
                MysticsRisky2Utils.ContentManagement.ContentLoadHelper contentLoadHelper = new MysticsRisky2Utils.ContentManagement.ContentLoadHelper();
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                Action[] loadDispatchers = new Action[]
                {
                    delegate { contentLoadHelper.DispatchLoad(executingAssembly, typeof(BaseEquipment), delegate(EquipmentDef[] x) { contentPack.equipmentDefs.Add(x); }); },
                    delegate { contentLoadHelper.DispatchLoad(executingAssembly, typeof(BaseBuff), delegate(BuffDef[] x) { contentPack.buffDefs.Add(x); }); },
                    delegate { contentLoadHelper.DispatchLoad(executingAssembly, typeof(BaseElite), delegate(EliteDef[] x) { contentPack.eliteDefs.Add(x); }); }
                };
                int k = 0;
                while (k < loadDispatchers.Length)
                {
                    loadDispatchers[k]();
                    args.ReportProgress(Util.Remap(k + 1, 0f, loadDispatchers.Length, 0f, 0.05f));
                    yield return null;
                    int num = k + 1;
                    k = num;
                }
                while (contentLoadHelper.coroutine.MoveNext())
                {
                    args.ReportProgress(Util.Remap(contentLoadHelper.progress.value, 0f, 1f, 0.05f, 0.9f));
                    yield return contentLoadHelper.coroutine.Current;
                }
                loadDispatchers = new Action[]
                {
                    delegate { MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded<BaseEquipment>(executingAssembly); },
                    delegate { MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded<BaseBuff>(executingAssembly); },
                    delegate { MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded<BaseElite>(executingAssembly); }
                };
                int i = 0;
                while (i < loadDispatchers.Length)
                {
                    loadDispatchers[i]();
                    args.ReportProgress(Util.Remap(i + 1, 0f, loadDispatchers.Length, 0.95f, 0.99f));
                    yield return null;
                    int num = i + 1;
                    i = num;
                }
            }

            public System.Collections.IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
            {
                ContentPack.Copy(contentPack, args.output);
                args.ReportProgress(1f);
                yield break;
            }

            public System.Collections.IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
            {
                args.ReportProgress(1f);
                yield break;
            }
        }
    }
}
