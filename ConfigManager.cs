using System;
using System.IO;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using UnityEngine;

namespace CybergrindSlideshow;

public class ConfigManager {
    public enum SelectionMode {
        Random, DeterministicSequential
    }

    public enum SecondarySelectionMode {
        Independent, StrictlyMatchSkyboxName, ClosestColorToSkybox
    }

    internal static readonly string ApplicationPath =
        Path.Combine(Directory.GetParent(Application.dataPath)?.FullName);
    internal static readonly string SkyboxesPath = Path.Combine(ApplicationPath, "Cybergrind", "Textures", "Skyboxes");
    internal static readonly string GridPath = Path.Combine(ApplicationPath, "Cybergrind", "Textures");
    internal static readonly string VolumetricSkyboxesPath = Path.Combine(ApplicationPath, "Cybergrind", "VolumetricSkyboxes");

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

    public static BoolField FirstRun;

    public static void Initialize() {}

    static ConfigManager() {
        config = PluginConfigurator.Create("Cybergrind Slideshow", Plugin.PluginGUID);
        string iconPath = Path.Combine(Plugin.workingDir, "icon.png");
        if (File.Exists(iconPath)) config.SetIconWithURL(iconPath);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#55bcec>SKYBOX</color> --", 24);

        SkyboxEnabled = new BoolField(config.rootPanel, "Change skybox", "skyboxEnabled", true);
        SkyboxDir = new StringField(config.rootPanel, "Skybox folder", "skyboxDir", SkyboxesPath);
        SkyboxChangeOrder = new EnumField<SelectionMode>(config.rootPanel, "Skybox change order", "skyboxChangeOrder", SelectionMode.DeterministicSequential);
        SkyboxAllowVolumetric = new BoolField(config.rootPanel, "Allow volumetric skyboxes", "skyboxAllowVolumetric", true);
        ForceDisableVolumetricSkyboxGridTexture = new BoolField(config.rootPanel, "Disallow grid change from volumetric skyboxes", "forceDisableVolumetricSkyboxGridTexture", true);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#f1c40f>LIGHTING</color> --", 24);
        LightingEnabled = new BoolField(config.rootPanel, "Sync with skybox", "lightingEnabled", true);
        LightingIntensity = new FloatField(config.rootPanel, "Lighting intensity", "lightingIntensity", 10f);
        LightingAdjustment = new BoolField(config.rootPanel, "Adjust based on lightness", "lightingAdjustment", true);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- <color=#9c87f4>GRID</color> --", 24);
        GridEnabled = new BoolField(config.rootPanel, "Change grid", "gridEnabled", true);
        GridDir = new StringField(config.rootPanel, "Grid folder", "gridDir", GridPath);
        GridSelectionMode = new EnumField<SecondarySelectionMode>(config.rootPanel, "Grid selection mode", "gridSelectionMode", SecondarySelectionMode.ClosestColorToSkybox);

        FirstRun = new BoolField(config.rootPanel, "First run", "firstRun", true);
        FirstRun.hidden = true;
    }
}
