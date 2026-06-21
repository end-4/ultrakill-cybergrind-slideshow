using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Notiffy.Utils;
using NukeLib.Assets;
using UnityEngine;
using VolumetricSkyboxes;
using VolumetricSkyboxes.Components;
using VolumetricSkyboxes.Utils;
using ReflectionUtils = NukeLib.Reflection.ReflectionUtils;

namespace CybergrindSlideshow;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency("com.eternalUnion.pluginConfigurator")]
public class Plugin : BaseUnityPlugin {
    // Logger
    internal static ManualLogSource Log;

    // Plugin config
    public static string workingPath = Assembly.GetExecutingAssembly().Location;
    public static string workingDir = Path.GetDirectoryName(workingPath);
    public const string PluginGUID = "com.github.end-4.cybergrindSlideshow";
    public const string PluginName = "CybergrindSlideshow";
    public const string PluginVersion = "1.0.0";

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

    private static string SelectSkyboxFile(string folderPath, ConfigManager.SelectionOrder selectionOrder) {
        string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".cgvsb" };
        if (!Directory.Exists(folderPath)) return "";
        string[] files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        string chosen = "";
        int fileIndex = 0;
        switch (selectionOrder) {
            case ConfigManager.SelectionOrder.Random:
                fileIndex = UnityEngine.Random.Range(0, files.Count());
                Log.LogInfo($"Random index {fileIndex}");
                break;
            case ConfigManager.SelectionOrder.Sequential:
            default:
                fileIndex = MonoSingleton<EndlessGrid>.Instance.currentWave % files.Count();
                Log.LogInfo($"Sequential index {fileIndex}");
                break;
        }

        chosen = files[fileIndex];
        return chosen;
    }

    public static void UnloadAllVolumetricSkyboxes() {
        var manager = MonoSingleton<VolumetricSkyboxesManager>.Instance;
        if (manager == null) return;
        Dictionary<string, VolumetricSkyboxContainer> skyboxesDict =
            manager.GetPrivate<Dictionary<string, VolumetricSkyboxContainer>>("_skyboxes");
        if (skyboxesDict == null) return;
        var keys = skyboxesDict?.Keys;
        if (keys == null) return;
        var guidsToRemove = new List<string>(keys);

        foreach (var guid in guidsToRemove) {
            if (manager == null) break;
            manager.RemoveSkybox(guid);
        }
    }

    private static void LoadVolumetricSkybox(string guid) {
        bool prevUseGrid = ConfigManager.ForceDisableVolumetricSkyboxGridTexture.value
            ? PrefsManager.Instance.GetBoolLocal("cyberGrind.volumetricSkyboxes.useSkyboxesGridTextures", true)
            : false;
        if (prevUseGrid)
            PrefsManager.Instance.SetBoolLocal("cyberGrind.volumetricSkyboxes.useSkyboxesGridTextures", false);
        MonoSingleton<VolumetricSkyboxesManager>.Instance?.AddSkybox(guid);
        if (prevUseGrid)
            PrefsManager.Instance.SetBoolLocal("cyberGrind.volumetricSkyboxes.useSkyboxesGridTextures", true);
    }

    private static async void ChangeSkybox() {
        try {
            string filePath =
                SelectSkyboxFile(ConfigManager.SkyboxDir.value, ConfigManager.SkyboxChangeOrder.value);
            if (filePath == "") return;

            Log.LogInfo($"Switching skybox to {filePath}");
            try {
                UnloadAllVolumetricSkyboxes();
            } catch (Exception e) {
                Log.LogWarning(e);
            }
            if (filePath.EndsWith(".cgvsb")) {
                // Volumetric
                var bundleData = SkyboxFileUtility.UnpackSkybox(filePath);
                LoadVolumetricSkybox(bundleData.guid);
            } else {
                Material mat =
                    ((OutdoorLightMaster)FindObjectsOfType(typeof(OutdoorLightMaster))[0]).GetPrivate<Material>(
                        "skyboxMaterial");
                Material mat2 =
                    ReflectionUtils.GetPrivate<Material>(
                        ((OutdoorLightMaster)FindObjectsOfType(typeof(OutdoorLightMaster))[0]), "tempSkybox");
                Texture2D tex = await FileAssetHelper.LoadTextureAsync(filePath);
                mat.mainTexture = tex;
                mat2.mainTexture = tex;
            }
        } catch (Exception e) {
            Log.LogWarning(e);
        }
    }

    private static void ChangeTheme() {
        if (ConfigManager.SkyboxEnabled.value) ChangeSkybox();
    }

    [HarmonyPatch(typeof(EndlessGrid))]
    [HarmonyPatch("NextWave")]
    public class EndlessGridPatch {
        public static void Postfix() {
            Log.LogInfo("Cycling stuff");
            ChangeTheme();
        }
    }
}
