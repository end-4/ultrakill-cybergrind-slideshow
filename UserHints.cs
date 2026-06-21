using System.Collections.Generic;
using System.IO;
using Notiffy.API;
using UnityEngine.SceneManagement;

namespace CybergrindSlideshow;

internal static class UserHints {
    private static Dictionary<string, string> volumetricCopyActions = new Dictionary<string, string>() {
        { "yes", "Yes" },
        { "no", "No" },
        { "remind", "Remind me later" },
    };

    private static uint notificationId;

    private static void Hook() {
        NotificationSystem.ActionInvoked += OnActionInvoked;
        NotificationSystem.NotificationDeleted += OnNotificationDeleted;
    }

    private static void Unhook() {
        NotificationSystem.ActionInvoked -= OnActionInvoked;
        NotificationSystem.NotificationDeleted -= OnNotificationDeleted;
    }

    private static void OnActionInvoked(uint id, string actionIdentifier) {
        if (id == notificationId) {
            switch (actionIdentifier) {
                case "yes":
                    CopyVolumetricSkyboxes();
                    break;
                case "no":
                    break;
                case "remind":
                    ConfigManager.FirstRun.value = true;
                    break;
            }

            Unhook();
        }
    }

    private static void OnNotificationDeleted(uint id) {
        if (id == notificationId) Unhook();
    }

    private static void IssueFirstRunNoticeIfNecessary() {
        if (!ConfigManager.FirstRun.value) return;
        ConfigManager.FirstRun.value = false;
        notificationId = NotificationSystem.NotifySend("CybergrindSlideshow",
            $"1. The folder for slideshow files can be adjusted in <color=#55c7f6>Options > Plugin Config > Cybergrind Slideshow</color>\n2. Would you like to copy volumetric skyboxes to the slideshow folder?",
            iconFilePath: Path.Combine(Plugin.workingDir, "icon.png"),
            appName: Plugin.PluginName,
            expireTime: 20000,
            actions: volumetricCopyActions
        );
        Hook();
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m) {
        if (SceneHelper.CurrentScene == "Endless") IssueFirstRunNoticeIfNecessary();
    }

    private static void CopyVolumetricSkyboxes() {
        string sourceDir = ConfigManager.VolumetricSkyboxesPath;
        string destDir = ConfigManager.SkyboxDir.value;
        string[] files = Directory.GetFiles(sourceDir);
        foreach (string file in files) {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile);
            Plugin.Log.LogInfo($"Copied: {fileName}");
        }
    }

    public static void Initialize() {
    }

    static UserHints() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
}
