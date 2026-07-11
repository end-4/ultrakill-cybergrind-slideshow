using NukeLib.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CybergrindSlideshow;

public static class PlaylistListener {
    private static bool _listening = false;
    private static CustomMusicPlayer? _cachedCustomMusicPlayer;
    private static AudioClip? _lastClip;

    public static void Initialize() {
        _listening = ConfigManager.ThemeChangeWhenMusicChanges.value;
        ConfigManager.ThemeChangeWhenMusicChanges.postValueChangeEvent += (bool val) => {
            _listening = val;
        };
        SceneManager.sceneLoaded += (Scene _, LoadSceneMode __) => {
            _cachedCustomMusicPlayer = null;
            if (Plugin.JukeboxAvailable) JukeboxHelper.ResetCache();
        };
    }

    private static AudioClip? GetCurrentVanillaAudioClip() {
        if (_cachedCustomMusicPlayer == null) {
            // Plugin.Log.LogWarning("Playlist null, finding (Normal)");
            _cachedCustomMusicPlayer = Object.FindObjectOfType<CustomMusicPlayer>();
            if (_cachedCustomMusicPlayer == null) return null;
        }

        return _cachedCustomMusicPlayer.source.clip;
    }

    public static void OnUpdate() {
        if (!_listening) return;

        var newClip = Plugin.JukeboxAvailable ? JukeboxHelper.GetCurrentAudioClip() : GetCurrentVanillaAudioClip();
        // Plugin.Log.LogWarning($"New clip {newClip}, null={newClip == null}, name {newClip?.name}");

        if (newClip != _lastClip) {
            ThemeChanger.ChangeTheme();
            _lastClip = newClip;
        }
    }
}
