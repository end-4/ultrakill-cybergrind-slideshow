using Jukebox;
using Jukebox.Components;
using UnityEngine;
using VolumetricSkyboxes.Utils;

namespace CybergrindSlideshow;

/// <summary>
/// Everything Jukebox-related is proxied through here so it works as a soft dependency
/// </summary>
public static class JukeboxHelper {
    private static JukeboxMusicPlayer? _cachedCustomMusicPlayer;

    public static AudioClip? GetCurrentAudioClip() {
        if (_cachedCustomMusicPlayer == null) {
            // Plugin.Log.LogWarning("Playlist null, finding (Jukebox)");
            _cachedCustomMusicPlayer = Object.FindObjectOfType<JukeboxMusicPlayer>();
            if (_cachedCustomMusicPlayer == null) return null;
        }
        return _cachedCustomMusicPlayer.Source.clip;
    }

    public static void ResetCache() {
        _cachedCustomMusicPlayer = null;
    }
}
