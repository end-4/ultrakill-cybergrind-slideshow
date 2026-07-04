using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CybergrindSlideshow;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency("com.eternalUnion.pluginConfigurator")]
[BepInDependency("dev.flazhik.volumetric-skyboxes", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin {
    // Logger
    internal static ManualLogSource Log;

    // Plugin config
    public static string workingPath = Assembly.GetExecutingAssembly().Location;
    public static string workingDir = Path.GetDirectoryName(workingPath);
    public const string PluginGUID = "com.github.end-4.cybergrindSlideshow";
    public const string PluginName = "CybergrindSlideshow";
    public const string PluginVersion = "1.1.0";

    // Volumetric skyboxes soft dep
    internal static bool VolumetricAvailable = false;

    private void Awake() {
        Log = Logger;

        // Config
        ConfigManager.Initialize();
        UserHints.Initialize();

        // Patch stuff
        Harmony harmony = new Harmony(PluginName);
        harmony.PatchAll();

        Log.LogInfo($"{PluginName} loaded");
    }

    private void Start() {
        if (Chainloader.PluginInfos.ContainsKey("dev.flazhik.volumetric-skyboxes")) VolumetricAvailable = true;
        Plugin.Log.LogInfo($"Volumetric available: {VolumetricAvailable}");
    }

    [HarmonyPatch(typeof(EndlessGrid))]
    [HarmonyPatch("NextWave")]
    public class EndlessGridPatch {
        public static void Postfix() {
            Log.LogInfo("Cycling stuff");
            ThemeChanger.ChangeTheme();
        }
    }

    [HarmonyPatch(typeof(EndlessGrid))]
    [HarmonyPatch("Start")]
    public class EndlessGridStart {
        public static void Postfix() {
            ThemeChanger.SetupScene();
        }
    }
}
