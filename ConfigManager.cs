using System;
using System.IO;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
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

    internal static readonly string VolumetricSkyboxesPath =
        Path.Combine(ApplicationPath, "Cybergrind", "VolumetricSkyboxes");

    private static PluginConfigurator config;
    public static BoolField SkyboxEnabled;
    public static StringField SkyboxDir;
    public static EnumField<SelectionMode> SkyboxChangeOrder;
    public static BoolField SkyboxAllowVolumetric;
    public static BoolField ForceDisableVolumetricSkyboxGridTexture;

    public static BoolField LightingEnabled;
    public static FloatField LightingIntensity;
    public static BoolField LightingAdjustment;

    public static BoolField GridEnabled;
    public static StringField GridDir;
    public static EnumField<SecondarySelectionMode> GridSelectionMode;

    public static BoolField GlowEnabled;
    public static StringField GlowDir;
    public static EnumField<MonochromeSelectionMode> GlowSelectionMode;

    public static BoolField FirstRun;

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
            "- <color=#95f857><u>Glow</u></color>: You can also match with skybox with precise file naming like grids, but other than that it's random"
            , 13, TextAlignmentOptions.Left);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#55bcec>SKYBOX</color> --", 24);

        SkyboxEnabled = new BoolField(config.rootPanel, "Change skybox", "skyboxEnabled", true);
        SkyboxDir = new StringField(config.rootPanel, "Skybox folder", "skyboxDir", SkyboxesPath);
        SkyboxChangeOrder = new EnumField<SelectionMode>(config.rootPanel, "Skybox change order", "skyboxChangeOrder",
            SelectionMode.DeterministicSequential);
        SkyboxAllowVolumetric =
            new BoolField(config.rootPanel, "Allow volumetric skyboxes", "skyboxAllowVolumetric", false);
        ForceDisableVolumetricSkyboxGridTexture = new BoolField(config.rootPanel,
            "Disallow grid change from volumetric skyboxes", "forceDisableVolumetricSkyboxGridTexture", false);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#f1c40f>LIGHTING</color> --", 24);
        LightingEnabled = new BoolField(config.rootPanel, "Sync with skybox", "lightingEnabled", true);
        LightingIntensity = new FloatField(config.rootPanel, "Lighting intensity", "lightingIntensity", 20f);
        LightingAdjustment = new BoolField(config.rootPanel, "Adjust intensity based on lightness", "lightingAdjustment", true);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#9c87f4>GRID</color> --", 24);
        GridEnabled = new BoolField(config.rootPanel, "Change grid", "gridEnabled", true);
        GridDir = new StringField(config.rootPanel, "Grid folder", "gridDir", GridPath);
        GridSelectionMode = new EnumField<SecondarySelectionMode>(config.rootPanel, "Grid selection mode",
            "gridSelectionMode", SecondarySelectionMode.ClosestColorToSkybox);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#f77428>GLOW</color> --", 24);
        GlowEnabled = new BoolField(config.rootPanel, "Change glow", "glowEnabled", true);
        GlowDir = new StringField(config.rootPanel, "Glow folder", "growDir", GlowPath);
        GlowSelectionMode = new EnumField<MonochromeSelectionMode>(config.rootPanel, "Glow selection mode", "glowSelectionMode", MonochromeSelectionMode.Independent);


        FirstRun = new BoolField(config.rootPanel, "First run", "firstRun", true);
        FirstRun.hidden = true;
    }
}
