using System;
using System.IO;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using UnityEngine;

namespace CybergrindSlideshow;

public class ConfigManager {
    public enum SelectionOrder {
        Random, Sequential
    }

    internal static readonly string ApplicationPath =
        Path.Combine(Directory.GetParent(Application.dataPath)?.FullName);
    internal static readonly string SkyboxesPath = Path.Combine(ApplicationPath, "Cybergrind", "Textures", "Skyboxes");
    internal static readonly string VolumetricSkyboxesPath = Path.Combine(ApplicationPath, "Cybergrind", "VolumetricSkyboxes");

    private static PluginConfigurator config;
    public static BoolField SkyboxEnabled;
    public static StringField SkyboxDir;
    public static EnumField<SelectionOrder> SkyboxChangeOrder;

    public static BoolField ForceDisableVolumetricSkyboxGridTexture;

    public static BoolField FirstRun;

    public static void Initialize() {}

    static ConfigManager() {
        config = PluginConfigurator.Create("Cybergrind Slideshow", Plugin.PluginGUID);
        string iconPath = Path.Combine(Plugin.workingDir, "icon.png");
        if (File.Exists(iconPath)) config.SetIconWithURL(iconPath);

        new ConfigHeader(config.rootPanel, "", 10);
        new ConfigHeader(config.rootPanel, "-- CYBERGRIND SLIDESHOW --", 24);

        SkyboxEnabled = new BoolField(config.rootPanel, "Change skybox", "skyboxEnabled", true);
        SkyboxDir = new StringField(config.rootPanel, "Skybox folder", "skyboxDir", SkyboxesPath);
        SkyboxChangeOrder = new EnumField<SelectionOrder>(config.rootPanel, "Skybox change order", "skyboxChangeOrder", SelectionOrder.Sequential);

        ForceDisableVolumetricSkyboxGridTexture = new BoolField(config.rootPanel, "Disallow grid change from volumetric skyboxes", "forceDisableVolumetricSkyboxGridTexture", true);

        FirstRun = new BoolField(config.rootPanel, "First run", "firstRun", true);
        FirstRun.hidden = true;
    }
}
