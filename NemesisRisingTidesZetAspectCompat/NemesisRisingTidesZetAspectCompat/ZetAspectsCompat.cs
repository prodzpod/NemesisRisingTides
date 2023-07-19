using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using System;
using MysticsRisky2Utils.BaseAssetTypes;
using System.Reflection;
using UnityEngine;
using RoR2;
using static RisingTides.Buffs.AffixMoney;
using RisingTides;
using RoR2.ContentManagement;
using R2API;
using System.Linq;
using BepInEx.Bootstrap;

namespace NemesisRisingTides
{
    [BepInDependency(RisingTidesPlugin.PluginGUID)]
    [BepInDependency(Main.PluginGUID)]
    [BepInDependency("com.TPDespair.ZetAspects", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ZetAspectsCompat : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "prodzpod";
        public const string PluginName = "NemesisRisingTidesZetAspectCompat";
        public const string PluginVersion = "1.0.0";
        public static Harmony Harmony;

        public void Awake()
        {
            Harmony = new Harmony(PluginGUID);
            if (Chainloader.PluginInfos.ContainsKey("com.TPDespair.ZetAspects"))
            {
                Main.Log.LogDebug("ZetAspect compat loaded :3");
                Harmony.PatchAll(typeof(PatchSuperOverrides));
            }
        }
    }
}
