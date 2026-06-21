using System.IO;
using Notiffy.API;
using UnityEngine.SceneManagement;

namespace CybergrindSlideshow;

internal static class UserHints {
    private static void IssueFirstRunNoticeIfNecessary() {
        if (!ConfigManager.FirstRun.value) return;
        ConfigManager.FirstRun.value = false;
        NotificationSystem.NotifySend("CybergrindSlideshow",
            $"This is a reminder that you should set the folder path containing skyboxes in <color=#55c7f6>Options > Plugin Config > Cybergrind Slideshow</color>", iconFilePath: Path.Combine(Plugin.workingDir, "icon.png"));
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m) {
        IssueFirstRunNoticeIfNecessary();
    }

    public static void Initialize() {
    }

    static UserHints() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
}
