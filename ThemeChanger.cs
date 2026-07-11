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
    private static string LastSkyboxPath = "";
    private static Color CachedSkyboxColor = Color.black;
    private static CustomTextures? CustomTexObj;
    private static GameObject LightObj;
    private static Light LightComp;

    private static bool IsVolumetricSkybox(string filePath) {
        return filePath.EndsWith(".cgvsb");
    }

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly string[] AudioExtensions = [".mp3", ".wav", ".ogg"];

    private static readonly string[] GridFaceSuffixes =
        ["", "base", "top", "topRow", "toprow", "_base", "_top", "_topRow", "_toprow", "_BASE", "_TOP", "_TOP ROW"];


    private record struct GridSet {
        public string Top;
        public string Base;
        public string TopRow;

        /// <summary>
        /// Gets a grid set around a given file
        /// </summary>
        /// <param name="filePath">The given file that exists. It can be any of the set</param>
        /// <returns>A set of grids that go together, bound through naming</returns>
        public static GridSet FromSingleFile(string filePath) {
            string fileExt = Path.GetExtension(filePath);
            string baseFileName = Path.GetFileNameWithoutExtension(filePath);
            string baseDir = Path.GetDirectoryName(filePath) ?? "";

            // Strip out suffix
            string baseName = Regex.Replace(baseFileName, @"(base|topRow|toprow|top)$", "");

            // Suffix map stuff
            string GetPath(string suffix) => Path.Join(baseDir, $"{baseName}{suffix}{fileExt}");
            var suffixMap = new[] {
                (Property: nameof(Top), Suffixes: new[] { "top", "TOP" }),
                (Property: nameof(Base), Suffixes: new[] { "base", "BASE" }),
                (Property: nameof(TopRow), Suffixes: new[] { "topRow", "toprow", "TOPROW", "TOP ROW" })
            };

            string fallbackPath = File.Exists(GetPath("")) ? GetPath("") : filePath;

            var gs = new GridSet();
            foreach (var rule in suffixMap) {
                string resolvedPath = fallbackPath;

                foreach (var suffix in rule.Suffixes) {
                    string candidatePath = GetPath(suffix);
                    if (File.Exists(candidatePath)) {
                        resolvedPath = candidatePath;
                        break;
                    }
                }

                // Assign to the field
                switch (rule.Property) {
                    case nameof(Top): gs.Top = resolvedPath; break;
                    case nameof(Base): gs.Base = resolvedPath; break;
                    case nameof(TopRow): gs.TopRow = resolvedPath; break;
                }
            }

            return gs;
        }

        public static GridSet FromPrefs() {
            var prefs = MonoSingleton<PrefsManager>.Instance;
            GridSet gs = new GridSet();
            if (prefs == null) return gs;
            gs.Top = prefs.GetStringLocal("cyberGrind.customGrid_1");
            gs.TopRow = prefs.GetStringLocal("cyberGrind.customGrid_2");
            gs.Base = prefs.GetStringLocal("cyberGrind.customGrid_0");
            return gs;
        }
    }

    private static string SelectFile(string folderPath, ConfigManager.SelectionMode selectionMode,
        string[] allowedExtensions, bool recursive = false) {

        if (!Directory.Exists(folderPath)) return "";

        SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] files = Directory.EnumerateFiles(folderPath, "*.*", searchOption)
            .Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (files.Length == 0) return "";

        string chosen = "";
        int fileIndex = 0;
        switch (selectionMode) {
            case ConfigManager.SelectionMode.Random:
                fileIndex = UnityEngine.Random.Range(0, files.Length);
                // Plugin.Log.LogInfo($"Random index {fileIndex}");
                break;
            case ConfigManager.SelectionMode.DeterministicSequential:
            default:
                fileIndex = MonoSingleton<EndlessGrid>.Instance.currentWave % files.Length;
                // Plugin.Log.LogInfo($"Sequential index {fileIndex}");
                break;
        }

        if (files[fileIndex] == LastSkyboxPath) fileIndex = (fileIndex + 1) % files.Length;
        chosen = files[fileIndex];
        LastSkyboxPath = chosen;
        return chosen;
    }

    private static string SelectSkyboxFile(string folderPath, ConfigManager.SelectionMode selectionMode) {
        return SelectFile(folderPath, selectionMode,
            (Plugin.VolumetricAvailable && ConfigManager.SkyboxAllowVolumetric.value)
                ? [".jpg", ".jpeg", ".png", ".cgvsb"]
                : [".jpg", ".jpeg", ".png"],
            recursive: ConfigManager.SkyboxDirRecursive.value
        );
    }

    private static string SelectGridFile(string folderPath, ConfigManager.SelectionMode selectionMode, bool recursive) {
        // TODO maybe support AnimatedCybergrindTextures
        return SelectFile(folderPath, selectionMode, [".jpg", ".jpeg", ".png"], recursive: recursive);
    }

    private static string SelectMatchingFile(string searchDir, string baseFilePath, string[] suffixes,
        string[] extensions) {
        // Trim allows arbitrarily many skyboxes to link to one grid
        string baseName = Path.GetFileNameWithoutExtension(baseFilePath).Trim();

        foreach (string suffix in suffixes) {
            foreach (string ext in extensions) {
                string path = Path.Join(searchDir, baseName + suffix + ext);
                // Plugin.Log.LogInfo($"Check {path}");
                if (File.Exists(path)) return path;
            }
        }

        return "";
    }

    private static void ChangeSkybox(string filePath) {
        Plugin.Log.LogInfo($"Skybox -> {filePath}");
        Material mat =
            ((OutdoorLightMaster)Object.FindObjectsOfType(typeof(OutdoorLightMaster))[0]).GetPrivate<Material>(
                "skyboxMaterial");
        Material mat2 =
            ((OutdoorLightMaster)Object.FindObjectsOfType(typeof(OutdoorLightMaster))[0]).GetPrivate<Material>(
                "tempSkybox");
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

            // Plugin.Log.LogInfo($"Changed cached skybox color to {CachedSkyboxColor}");
        }
    }

    private static async void ChangeTechnicalGridTexture(string[] files, string textureName) {
        if (CustomTexObj == null) {
            Plugin.Log.LogWarning("CustomTextures not found");
            return;
        }

        if (CustomTexObj.gameObject != null && !CustomTexObj.gameObject.activeSelf)
            CustomTexObj.gameObject.SetActive(true);


        var mats = CustomTexObj.GetPrivate<Material[]>("gridMaterials");
        if (mats == null) {
            Plugin.Log.LogWarning("CustomTextures::gridMaterials not found");
            return;
        }

        for (int i = 0; i < files.Length && i < mats.Length; i++) {
            var file = files[i];
            if (!File.Exists(file)) continue;
            mats[i].SetTexture(textureName, await FileAssetHelper.LoadTextureAsync(file));
        }
    }

    private static async void ChangeGridTexture(GridSet gridSet) {
        ChangeTechnicalGridTexture([gridSet.Base, gridSet.Top, gridSet.TopRow], "_MainTex");
    }

    private static void ChangeGlowTexture(GridSet gridSet) {
        ChangeTechnicalGridTexture([gridSet.Base, gridSet.Top, gridSet.TopRow], "_EmissiveTex");
    }

    // private static void ChangeMusicFile(string filePath) {
    //     var musicBrowser = Object.FindObjectsOfType<CustomMusicFileBrowser>();
    // }

    private static async void ChangeGrid(string skyboxFilePath) {
        // Plugin.Log.LogInfo($"-- Changing grid. [skybox={skyboxFilePath}]");
        string targetFile;
        GridSet gridSet = new GridSet();
        targetFile = SelectMatchingFile(ConfigManager.GridDir.value, skyboxFilePath,
            GridFaceSuffixes, ImageExtensions);
        if (targetFile != "") {
            gridSet = GridSet.FromSingleFile(targetFile);
        } else {
            switch (ConfigManager.GridSelectionMode.value) {
                case ConfigManager.SecondarySelectionMode.Independent:
                    targetFile = SelectGridFile(ConfigManager.GridDir.value, ConfigManager.SkyboxChangeOrder.value, ConfigManager.GridDirRecursive.value);
                    gridSet = GridSet.FromSingleFile(targetFile);
                    break;
                case ConfigManager.SecondarySelectionMode.StrictlyMatchSkyboxName:
                    gridSet = GridSet.FromPrefs();
                    break;
                case ConfigManager.SecondarySelectionMode.ClosestColorToSkybox:
                    // var targetColor = IsVolumetricSkybox(skyboxFilePath) ? CachedSkyboxColor : ImageUtils.GetDominantColor(skyboxFilePath);
                    // var targetColor = CachedSkyboxColor == Color.black ? ImageUtils.GetDominantColor(skyboxFilePath) : CachedSkyboxColor;
                    var targetColor = CachedSkyboxColor;
                    // Plugin.Log.LogInfo($"Target color: {targetColor}");
                    var closestImage = ImageUtils.FindClosestColorImage(targetColor, ConfigManager.GridDir.value);
                    // Plugin.Log.LogInfo($"Closest image: {closestImage}");
                    gridSet = GridSet.FromSingleFile(closestImage);
                    break;
            }
        }

        Plugin.Log.LogInfo($"Grid   -> {gridSet.Base}\n           + {gridSet.Top}\n           + {gridSet.TopRow}");
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
                ? Math.Clamp(CachedSkyboxColor.PerceivedLightness(), 0.2f, 0.5f)
                : 1);
            light.intensity = ConfigManager.LightingIntensity.value * multiplier;
        }
    }

    private static async void ChangeGlow(string skyboxFilePath) {
        // Plugin.Log.LogInfo($"-- Changing glow. [skybox={skyboxFilePath}]");
        string targetFile = SelectMatchingFile(ConfigManager.GlowDir.value, skyboxFilePath,
            GridFaceSuffixes, ImageExtensions);
        // Plugin.Log.LogInfo($"-- Matching target = {targetFile}");
        if (targetFile == "") {
            switch (ConfigManager.GlowSelectionMode.value) {
                case ConfigManager.MonochromeSelectionMode.Independent:
                    targetFile = SelectGridFile(ConfigManager.GlowDir.value, ConfigManager.SkyboxChangeOrder.value, ConfigManager.GlowDirRecursive.value);
                    break;
            }
        }

        GridSet gs = GridSet.FromSingleFile(targetFile);
        ChangeGlowTexture(gs);
    }

    // public static async void ChangeMusic(string skyboxFilePath) {
    //     string targetFile = SelectMatchingFile(ConfigManager.MusicDir.value, skyboxFilePath,
    //         [""], AudioExtensions);
    //     // Plugin.Log.LogInfo($"-- Matching target = {targetFile}");
    //     if (targetFile == "") {
    //         switch (ConfigManager.MusicSelectionMode.value) {
    //             case ConfigManager.SecondarySelectionMode.Independent:
    //                 targetFile = SelectFile(ConfigManager.MusicDir.value, ConfigManager.SkyboxChangeOrder.value, AudioExtensions, ConfigManager.MusicDirRecursive.value);
    //                 break;
    //             case ConfigManager.SecondarySelectionMode.StrictlyMatchSkyboxName:
    //                 return;
    //             case ConfigManager.SecondarySelectionMode.ClosestColorToSkybox:
    //                 var targetColor = CachedSkyboxColor;
    //                 targetFile = ImageUtils.FindClosestColorFile(targetColor, ConfigManager.GridDir.value);
    //                 break;
    //         }
    //     }
    //
    //     ChangeMusicFile(targetFile);
    // }

    private static bool FirstWaveChange = true;
    private static int wavesPassed = 0;

    public static async Task ChangeTheme() {
        if (FirstWaveChange) {
            FirstWaveChange = false;
            return;
        }

        wavesPassed++;
        if (wavesPassed < ConfigManager.ThemeChangeWaveInterval.value) return;
        wavesPassed = 0;

        string filePath = SelectSkyboxFile(ConfigManager.SkyboxDir.value, ConfigManager.SkyboxChangeOrder.value);
        if (filePath == "") return;
        Plugin.Log.LogInfo("---- New wave ----");
        if (ConfigManager.SkyboxEnabled.value) ChangeSkybox(filePath);
        if (ConfigManager.LightingEnabled.value) ChangeLighting(filePath);
        if (ConfigManager.GridEnabled.value) ChangeGrid(filePath);
        if (ConfigManager.GlowEnabled.value) ChangeGlow(filePath);
        // if (ConfigManager.MusicEnabled.value) ChangeMusic(filePath);
    }

    public static void SetupScene() {
        FirstWaveChange = true;
        // Add arena lighting (I realized there's a more proper way than point light)
        // LightObj = new GameObject("SkyboxShine");
        // LightObj.transform.position = new Vector3(0, 90, 62);
        // LightComp = LightObj.AddComponent<Light>();
        // LightComp.range = 1000;
    }

    public static void SetupFirstWaveIfNecessary() {
        // Hack to make sure we always have a CustomTextures component (it gets unloaded at some point usually)
        if (CustomTexObj == null) {
            CustomTextures customTextures = Object.FindObjectOfType<CustomTextures>();
            CustomTexObj = Object.Instantiate(customTextures.gameObject).GetComponent<CustomTextures>();
            Plugin.Log.LogInfo("Duped CustomTextures");
        }
    }
}
