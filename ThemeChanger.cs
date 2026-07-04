using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NukeLib.Assets;
using NukeLib.ImageUtils;
using NukeLib.Reflection;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;
using Object = UnityEngine.Object;
using ReflectionUtils = NukeLib.Reflection.ReflectionUtils;
using RenderSettings = UnityEngine.RenderSettings;

namespace CybergrindSlideshow;

public static class ThemeChanger {
    private static Color CachedSkyboxColor = Color.black;
    private static CustomTextures? CustomTexObj;
    private static GameObject LightObj;
    private static Light LightComp;

    private static bool IsVolumetricSkybox(string filePath) {
        return filePath.EndsWith(".cgvsb");
    }

    private record struct GridSet {
        public string Top;
        public string Base;
        public string TopRow;

        public static GridSet FromSingleFile(string filePath) {
            var gs = new GridSet();
            string fileExt = Path.GetExtension(filePath);
            string baseFileName = Path.GetFileNameWithoutExtension(filePath);
            string baseDir = Path.GetDirectoryName(filePath);
            string baserName = Regex.Replace(baseFileName, @"(base|topRow|top)$", "");

            string baseSuffixPath = Path.Join(baseDir, baserName + "base" + fileExt);
            string topSuffixPath = Path.Join(baseDir, baserName + "top" + fileExt);
            string topRowSuffixPath = Path.Join(baseDir, baserName + "top" + fileExt);
            string noSuffixPath = Path.Join(baseDir, baserName + fileExt);

            string fallbackPath = File.Exists(noSuffixPath) ? noSuffixPath : filePath;
            gs.Top = File.Exists(topSuffixPath) ? topSuffixPath : fallbackPath;
            gs.Base = File.Exists(baseSuffixPath) ? baseSuffixPath : fallbackPath;
            gs.TopRow = File.Exists(topRowSuffixPath) ? topRowSuffixPath : fallbackPath;
            return gs;
        }

        public static GridSet FromPrefs() {
            var prefs = MonoSingleton<PrefsManager>.Instance;
            GridSet gs = new GridSet();
            gs.Top = prefs.GetStringLocal("cyberGrind.customGrid_1");
            gs.TopRow = prefs.GetStringLocal("cyberGrind.customGrid_2");
            gs.Base = prefs.GetStringLocal("cyberGrind.customGrid_0");
            return gs;
        }
    }

    private static string SelectFile(string folderPath, ConfigManager.SelectionMode selectionMode,
        string[] allowedExtensions) {
        if (!Directory.Exists(folderPath)) return "";
        string[] files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        string chosen = "";
        int fileIndex = 0;
        switch (selectionMode) {
            case ConfigManager.SelectionMode.Random:
                fileIndex = UnityEngine.Random.Range(0, files.Count());
                Plugin.Log.LogInfo($"Random index {fileIndex}");
                break;
            case ConfigManager.SelectionMode.DeterministicSequential:
            default:
                fileIndex = MonoSingleton<EndlessGrid>.Instance.currentWave % files.Count();
                Plugin.Log.LogInfo($"Sequential index {fileIndex}");
                break;
        }

        chosen = files[fileIndex];
        return chosen;
    }

    private static string SelectSkyboxFile(string folderPath, ConfigManager.SelectionMode selectionMode) {
        return SelectFile(folderPath, selectionMode,
            (Plugin.VolumetricAvailable && ConfigManager.SkyboxAllowVolumetric.value)
                ? [".jpg", ".jpeg", ".png", ".cgvsb"]
                : [".jpg", ".jpeg", ".png"]
        );
    }

    private static string SelectGridFile(string folderPath, ConfigManager.SelectionMode selectionMode) {
        // TODO maybe support AnimatedCybergrindTextures
        return SelectFile(folderPath, selectionMode, [".jpg", ".jpeg", ".png"]);
    }

    private static string SelectMatchingGridFile(string basePath) {
        string baseName = Path.GetFileNameWithoutExtension(basePath);
        string[] suffixes = { "", "base", "top", "topRow" };
        string[] extensions = { ".jpg", ".jpeg", ".png" };

        foreach (string suffix in suffixes) {
            foreach (string ext in extensions) {
                string path = Path.Join(ConfigManager.GridDir.value, baseName + suffix + ext);
                if (File.Exists(path)) return path;
            }
        }

        return "";
    }

    private static void ChangeSkybox(string filePath) {
        Plugin.Log.LogInfo($"-- Changing skybox -> {filePath}");
        Material mat =
            ((OutdoorLightMaster)Object.FindObjectsOfType(typeof(OutdoorLightMaster))[0]).GetPrivate<Material>(
                "skyboxMaterial");
        Material mat2 =
            ((OutdoorLightMaster)Object.FindObjectsOfType(typeof(OutdoorLightMaster))[0]).GetPrivate<Material>("tempSkybox");
        if (Plugin.VolumetricAvailable) VolumetricSkyboxHelper.UnloadAllVolumetricSkyboxes();
        if (IsVolumetricSkybox(filePath)) {
            // Volumetric
            try {
                string guid = VolumetricSkyboxHelper.GuidForVolumetricSkyboxFile(filePath);
                if (Plugin.VolumetricAvailable) VolumetricSkyboxHelper.LoadVolumetricSkybox(guid);
            } catch (Exception e) {
                Plugin.Log.LogWarning(e);
            }
        } else {
            Texture2D tex = FileAssetHelper.LoadTexture(filePath);
            mat.mainTexture = tex;
            mat2.mainTexture = tex;
        }

        if (ConfigManager.GridSelectionMode.value == ConfigManager.SecondarySelectionMode.ClosestColorToSkybox ||
            ConfigManager.LightingEnabled.value) {
            try {
                CachedSkyboxColor = ImageUtils.GetDominantColor((Texture2D)mat.mainTexture);
            } catch (Exception e) {
            }

            Plugin.Log.LogInfo($"Changed cached skybox color to {CachedSkyboxColor}");
        }
    }

    private static async void ChangeGridTexture(GridSet gridSet) {
        if (CustomTexObj == null) {
            Plugin.Log.LogWarning("CustomTextures not found");
            return;
        }
        if (CustomTexObj != null && CustomTexObj.gameObject != null && !CustomTexObj.gameObject.activeSelf)
            CustomTexObj.gameObject.SetActive(true);


        var mats = CustomTexObj.GetPrivate<Material[]>("gridMaterials");
        if (mats == null) {
            Plugin.Log.LogWarning("CustomTextures::gridMaterials not found");
            return;
        }

        if (File.Exists(gridSet.Base)) mats[0].mainTexture = await FileAssetHelper.LoadTextureAsync(gridSet.Base);
        if (File.Exists(gridSet.Top)) mats[1].mainTexture = await FileAssetHelper.LoadTextureAsync(gridSet.Top);
        if (File.Exists(gridSet.TopRow)) mats[2].mainTexture = await FileAssetHelper.LoadTextureAsync(gridSet.TopRow);
    }

    private static async void ChangeGrid(string skyboxFilePath) {
        Plugin.Log.LogInfo($"-- Changing grid. [skybox={skyboxFilePath}]");
        string targetFile;
        GridSet gridSet = new GridSet();
        targetFile = SelectMatchingGridFile(skyboxFilePath);
        if (targetFile != "") {
            gridSet = GridSet.FromSingleFile(targetFile);
        }
        else {
            switch (ConfigManager.GridSelectionMode.value) {
                case ConfigManager.SecondarySelectionMode.Independent:
                    targetFile = SelectGridFile(ConfigManager.GridDir.value, ConfigManager.SkyboxChangeOrder.value);
                    gridSet = GridSet.FromSingleFile(targetFile);
                    break;
                case ConfigManager.SecondarySelectionMode.StrictlyMatchSkyboxName:
                    gridSet = GridSet.FromPrefs();
                    break;
                case ConfigManager.SecondarySelectionMode.ClosestColorToSkybox:
                    // var targetColor = IsVolumetricSkybox(skyboxFilePath) ? CachedSkyboxColor : ImageUtils.GetDominantColor(skyboxFilePath);
                    // var targetColor = CachedSkyboxColor == Color.black ? ImageUtils.GetDominantColor(skyboxFilePath) : CachedSkyboxColor;
                    var targetColor = CachedSkyboxColor;
                    Plugin.Log.LogInfo($"Target color: {targetColor}");
                    var closestImage = ImageUtils.FindClosestColorImage(targetColor, ConfigManager.GridDir.value);
                    Plugin.Log.LogInfo($"Closest image: {closestImage}");
                    gridSet = GridSet.FromSingleFile(closestImage);
                    break;
            }
        }

        Plugin.Log.LogInfo($"Changing grid to {gridSet.Base}\n  + {gridSet.Top}\n  + {gridSet.TopRow}");
        ChangeGridTexture(gridSet);
    }

    private static async void ChangeLighting(string skyboxFilePath) {
        // RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        // RenderSettings.ambientSkyColor = CachedSkyboxColor;

        var outdoorLightMaster = MonoSingleton<OutdoorLightMaster>.Instance;
        if (outdoorLightMaster == null) return;
        var lights = outdoorLightMaster.gameObject.GetComponentsInChildren<Light>()
            .Where(light => light.type == LightType.Directional)
            .ToList();


        foreach (var light in lights) {
            // Plugin.Log.LogInfo($"LIGHT {light.name} from {light.gameObject?.name}");
            light.color = CachedSkyboxColor;
            float multiplier = (ConfigManager.LightingAdjustment.value
                ? Math.Max(CachedSkyboxColor.PerceivedLightness(), 0.2f)
                : 1);
            light.intensity = ConfigManager.LightingIntensity.value * multiplier;
        }
    }

    public static async Task ChangeTheme() {
        string filePath = SelectSkyboxFile(ConfigManager.SkyboxDir.value, ConfigManager.SkyboxChangeOrder.value);
        if (filePath == "") return;
        Plugin.Log.LogInfo("---- New wave ----");
        if (ConfigManager.SkyboxEnabled.value) ChangeSkybox(filePath);
        if (ConfigManager.LightingEnabled.value) ChangeLighting(filePath);
        if (ConfigManager.GridEnabled.value) ChangeGrid(filePath);
    }

    public static void SetupScene() {
        // Hack to make sure we always have a CustomTextures component (it gets unloaded at some point usually)
        if (CustomTexObj == null) {
            CustomTextures customTextures = Object.FindObjectOfType<CustomTextures>();
            CustomTexObj = Object.Instantiate(customTextures.gameObject).GetComponent<CustomTextures>();
        }

        // Add arena lighting
        // LightObj = new GameObject("SkyboxShine");
        // LightObj.transform.position = new Vector3(0, 90, 62);
        // LightComp = LightObj.AddComponent<Light>();
        // LightComp.range = 1000;

    }
}
