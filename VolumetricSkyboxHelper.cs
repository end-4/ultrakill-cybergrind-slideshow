using System.Collections.Generic;
using VolumetricSkyboxes;
using VolumetricSkyboxes.Components;
using VolumetricSkyboxes.Utils;
using ReflectionUtils = NukeLib.Reflection.ReflectionUtils;

namespace CybergrindSlideshow;

/// <summary>
/// Everything Volumetric-related is proxied through here so it works as a soft dependency
/// </summary>
public static class VolumetricSkyboxHelper {
    public static void UnloadAllVolumetricSkyboxes() {
        var manager = MonoSingleton<VolumetricSkyboxesManager>.Instance;
        if (manager == null) return;
        Dictionary<string, VolumetricSkyboxContainer> skyboxesDict =
            ReflectionUtils.GetPrivate<Dictionary<string, VolumetricSkyboxContainer>>(manager, "_skyboxes");
        if (skyboxesDict == null) return;
        var keys = skyboxesDict?.Keys;
        if (keys == null) return;
        var guidsToRemove = new List<string>(keys);

        foreach (var guid in guidsToRemove) {
            if (manager == null) break;
            manager.RemoveSkybox(guid);
        }
    }

    public static void LoadVolumetricSkybox(string guid) {
        bool prevUseGrid = ConfigManager.ForceDisableVolumetricSkyboxGridTexture.value
            ? PrefsManager.Instance.GetBoolLocal("cyberGrind.volumetricSkyboxes.useSkyboxesGridTextures", true)
            : false;
        if (prevUseGrid) {
            PrefsManager.Instance.SetBoolLocal("cyberGrind.volumetricSkyboxes.useSkyboxesGridTextures", false);
        }

        var vsbman = MonoSingleton<VolumetricSkyboxesManager>.Instance;
        if (vsbman != null) vsbman.AddSkybox(guid);
        if (prevUseGrid) {
            PrefsManager.Instance.SetBoolLocal("cyberGrind.volumetricSkyboxes.useSkyboxesGridTextures", true);
        }
    }

    public static VolumetricSkyboxBundleData UnpackFile(string filePath) {
        return SkyboxFileUtility.UnpackSkybox(filePath);
    }

    public static string GuidForVolumetricSkyboxFile(string filePath) {
        return UnpackFile(filePath).guid;
    }
}
