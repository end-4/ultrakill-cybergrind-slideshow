using System;
using System.IO;
using System.Linq;
using NukeLib.Assets;
using NukeLib.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using ReflectionUtils = NukeLib.Reflection.ReflectionUtils;

namespace CybergrindSlideshow;

public static class ThemeChanger {
    private static string SelectFile(string folderPath, ConfigManager.SelectionOrder selectionOrder,
        string[] allowedExtensions) {
        if (!Directory.Exists(folderPath)) return "";
        string[] files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        string chosen = "";
        int fileIndex = 0;
        switch (selectionOrder) {
            case ConfigManager.SelectionOrder.Random:
                fileIndex = UnityEngine.Random.Range(0, files.Count());
                Plugin.Log.LogInfo($"Random index {fileIndex}");
                break;
            case ConfigManager.SelectionOrder.Sequential:
            default:
                fileIndex = MonoSingleton<EndlessGrid>.Instance.currentWave % files.Count();
                Plugin.Log.LogInfo($"Sequential index {fileIndex}");
                break;
        }

        chosen = files[fileIndex];
        return chosen;
    }

    private static string SelectSkyboxFile(string folderPath, ConfigManager.SelectionOrder selectionOrder) {
        return SelectFile(folderPath, selectionOrder,
            Plugin.VolumetricAvailable ? [".jpg", ".jpeg", ".png", ".cgvsb"] : [".jpg", ".jpeg", ".png"]
        );
    }

    private static string SelectGridFile(string folderPath, ConfigManager.SelectionOrder selectionOrder) {
        return SelectFile(folderPath, selectionOrder, [".jpg", ".jpeg", ".png"]);
    }

    private static async void ChangeSkybox(string filePath) {
        Plugin.Log.LogInfo($"Switching skybox to {filePath}");
        if (Plugin.VolumetricAvailable) VolumetricSkyboxHelper.UnloadAllVolumetricSkyboxes();
        if (filePath.EndsWith(".cgvsb")) {
            // Volumetric
            try {
                string guid = VolumetricSkyboxHelper.GuidForVolumetricSkyboxFile(filePath);
                if (Plugin.VolumetricAvailable) VolumetricSkyboxHelper.LoadVolumetricSkybox(guid);
            } catch (Exception e) {
                Plugin.Log.LogWarning(e);
            }
        } else {
            Material mat =
                ((OutdoorLightMaster)Object.FindObjectsOfType(typeof(OutdoorLightMaster))[0]).GetPrivate<Material>(
                    "skyboxMaterial");
            Material mat2 =
                ReflectionUtils.GetPrivate<Material>(
                    ((OutdoorLightMaster)Object.FindObjectsOfType(typeof(OutdoorLightMaster))[0]), "tempSkybox");
            Texture2D tex = await FileAssetHelper.LoadTextureAsync(filePath);
            mat.mainTexture = tex;
            mat2.mainTexture = tex;
        }
    }

    public static void ChangeTheme() {
        string filePath = SelectSkyboxFile(ConfigManager.SkyboxDir.value, ConfigManager.SkyboxChangeOrder.value);
        if (filePath == "") return;
        if (ConfigManager.SkyboxEnabled.value) ChangeSkybox(filePath);
    }

    public static void RecordDefaults() {
    }
}
