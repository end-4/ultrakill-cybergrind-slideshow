using System;
using System.IO;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using TMPro;
using UnityEngine;

namespace CybergrindSlideshow;

public class ConfigManager {
    public enum SelectionMode {
        Random,
        DeterministicSequential
    }

    public enum SecondarySelectionMode {
        Independent,
        StrictlyMatchSkyboxName,
        ClosestColorToSkybox
    }

    public enum MonochromeSelectionMode {
        Independent
    }

    internal static readonly string ApplicationPath =
        Path.Combine(Directory.GetParent(Application.dataPath)?.FullName);

    internal static readonly string SkyboxesPath = Path.Combine(ApplicationPath, "Cybergrind", "Textures", "Skyboxes");
    internal static readonly string GridPath = Path.Combine(ApplicationPath, "Cybergrind", "Textures");
    internal static readonly string GlowPath = Path.Combine(ApplicationPath, "Cybergrind", "Textures", "Glows");
    internal static readonly string MusicPath = Path.Combine(ApplicationPath, "Cybergrind", "Music");

    internal static readonly string VolumetricSkyboxesPath =
        Path.Combine(ApplicationPath, "Cybergrind", "VolumetricSkyboxes");

    private static PluginConfigurator config;

    public static IntField ThemeChangeWaveInterval;
    public static BoolField ThemeChangeWhenMusicChanges;

    public static BoolField SkyboxEnabled;
    public static StringField SkyboxDir;
    public static BoolField SkyboxDirRecursive;
    public static EnumField<SelectionMode> SkyboxChangeOrder;
    public static BoolField SkyboxAllowVolumetric;
    public static BoolField ForceDisableVolumetricSkyboxGridTexture;

    public static BoolField LightingEnabled;
    public static FloatField LightingIntensity;
    public static BoolField LightingAdjustment;

    public static BoolField GridEnabled;
    public static StringField GridDir;
    public static BoolField GridDirRecursive;
    public static EnumField<SecondarySelectionMode> GridSelectionMode;

    public static BoolField GlowEnabled;
    public static StringField GlowDir;
    public static BoolField GlowDirRecursive;
    public static EnumField<MonochromeSelectionMode> GlowSelectionMode;

    // public static BoolField MusicEnabled;
    // public static StringField MusicDir;
    // public static BoolField MusicDirRecursive;
    // public static EnumField<SecondarySelectionMode> MusicSelectionMode;

    public static StringField LastVersion;

    public static void Initialize() {
    }

    static ConfigManager() {
        config = PluginConfigurator.Create("Cybergrind Slideshow", Plugin.PluginGUID);
        string iconPath = Path.Combine(Plugin.workingDir, "icon.png");
        if (File.Exists(iconPath)) config.SetIconWithURL(iconPath);
        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#95f857>USAGE NOTES</color> --", 24);
        new ConfigHeader(config.rootPanel,
            "- <color=#95f857><u>Folders</u></color>: The defaults follow the default organization of the base game, adjust if needed\n" +
            "- <color=#95f857><u>Grid</u></color>:\n" +
            "  - By default, the texture that has the closest color to the skybox will be chosen. To force a specific tile for a certain skybox, name it exactly the same except the file extension. Example: Skybox is SpAcE.png or SpAcE.cgvsb -> Matching grid is SpAcE.jpg or SpAcE.png\n" +
            "  - You can create grid sets by adding \"_base\", \"_top\", \"_topRow\" at the end of each texture (and make sure they're of same file extension), for example \"griddy_base.png\", \"griddy_top.png\", and \"griddy_topRow.png\" are always loaded together.\n" +
            "- <color=#95f857><u>Glow</u></color>: You can also match with skybox with precise file naming like grids, but other than that it's random\n" +
            "- <color=#95f857><u>Volumetric skyboxes</u></color>: Can be used, but they cause Slideshow to be inconsistent in dominant color detection & grid swapping, so they're disabled by default."
            , 13, TextAlignmentOptions.Left);

        new ConfigHeader(config.rootPanel, "Quick open", 18, TextAlignmentOptions.Left);
        var openDirs = new ButtonArrayField(config.rootPanel, "openDirButtons", 3, [0.33f, 0.33f, 0.33f], ["Skybox folder", "Grid folder", "Glow folder"], 10f);
        openDirs.OnClickEventHandler(0).onClick += () => Application.OpenURL(SkyboxDir.value);
        openDirs.OnClickEventHandler(1).onClick += () => Application.OpenURL(GridDir.value);
        openDirs.OnClickEventHandler(2).onClick += () => Application.OpenURL(GlowDir.value);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#69fff7>GENERAL</color> --", 24);
        ThemeChangeWaveInterval = new IntField(config.rootPanel, "Change theme every x wave, where x=", "themeChangeWaveInterval", 1);
        ThemeChangeWhenMusicChanges = new BoolField(config.rootPanel, "Change theme when music changes", "themeChangeWhenMusicChanges", false);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#55bcec>SKYBOX</color> --", 24);

        SkyboxEnabled = new BoolField(config.rootPanel, "Change skybox", "skyboxEnabled", true);
        SkyboxDir = new StringField(config.rootPanel, "Skybox folder", "skyboxDir", SkyboxesPath);
        SkyboxDirRecursive = new BoolField(config.rootPanel, "Consider subfolders", "skyboxDirRecursive", false);
        SkyboxChangeOrder = new EnumField<SelectionMode>(config.rootPanel, "Skybox change order", "skyboxChangeOrder",
            SelectionMode.Random);
        SkyboxAllowVolumetric =
            new BoolField(config.rootPanel, "Allow volumetric skyboxes", "skyboxAllowVolumetric", false);
        ForceDisableVolumetricSkyboxGridTexture = new BoolField(config.rootPanel,
            "Disallow grid change from volumetric skyboxes", "forceDisableVolumetricSkyboxGridTexture", false);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#f1c40f>LIGHTING</color> --", 24);
        LightingEnabled = new BoolField(config.rootPanel, "Sync with skybox", "lightingEnabled", true);
        LightingIntensity = new FloatField(config.rootPanel, "Lighting intensity", "lightingIntensity", 5f);
        LightingAdjustment = new BoolField(config.rootPanel, "Adjust intensity based on lightness",
            "lightingAdjustment", true);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#9c87f4>GRID</color> --", 24);
        GridEnabled = new BoolField(config.rootPanel, "Change grid", "gridEnabled", true);
        GridDir = new StringField(config.rootPanel, "Grid folder", "gridDir", GridPath);
        GridDirRecursive = new BoolField(config.rootPanel, "Consider subfolders", "gridDirRecursive", false);
        GridSelectionMode = new EnumField<SecondarySelectionMode>(config.rootPanel, "Grid selection mode",
            "gridSelectionMode", SecondarySelectionMode.ClosestColorToSkybox);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#f77428>GLOW</color> --", 24);
        GlowEnabled = new BoolField(config.rootPanel, "Change glow", "glowEnabled", true);
        GlowDir = new StringField(config.rootPanel, "Glow folder", "growDir", GlowPath);
        GlowDirRecursive = new BoolField(config.rootPanel, "Consider subfolders", "glowDirRecursive", false);
        GlowSelectionMode = new EnumField<MonochromeSelectionMode>(config.rootPanel, "Glow selection mode",
            "glowSelectionMode", MonochromeSelectionMode.Independent);

        // new ConfigHeader(config.rootPanel, "", 10);
        // new ConfigHeader(config.rootPanel, "-- <color=#e51af0>MUSIC</color> --", 24);
        // MusicEnabled = new BoolField(config.rootPanel, "Change music", "musicEnabled", true);
        // MusicDir = new StringField(config.rootPanel, "Music folder", "musicDir", MusicPath);
        // MusicDirRecursive = new BoolField(config.rootPanel, "Consider subfolders", "musicDirRecursive", true);
        // MusicSelectionMode = new EnumField<SecondarySelectionMode>(config.rootPanel, "Music selection mode",
        //     "musicSelectionMode", SecondarySelectionMode.StrictlyMatchSkyboxName);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- DEBUG --", 24);
        var devPanel = new ConfigPanel(config.rootPanel, "Developer options", "devOptions");
        new ConfigHeader(devPanel, "", 10);
        new ConfigHeader(devPanel, "These don't do anything exciting. Don't bother.", 13);
        LastVersion = new StringField(devPanel, "Last version", "lastVersion", "0.0.0");
    }
}
