using Godot;
using MegaCrit.Sts2.Core.Saves;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>统一音效加载与播放；资源缺失时只记录一次警告。</summary>
public static class yylAudio
{
    private static readonly Dictionary<string, AudioStream> Cache = [];
    private static readonly Dictionary<string, List<AudioStreamPlayer>> ActivePlayers = [];
    private static readonly HashSet<string> MissingResources = [];

    public static string Sfx(string fileName) => $"{MainFile.ResPath}/audio/sfx/{fileName}";

    public static string Ambience(string fileName) => $"{MainFile.ResPath}/audio/ambience/{fileName}";

    public static string Voice(string fileName) => $"{MainFile.ResPath}/audio/voice/{fileName}";

    public static void PlaySfx(string pathOrFile, float volume = 1f, float pitch = 1f)
    {
        PlayInternal(ResolveSfxPath(pathOrFile), volume, pitch, false);
    }

    public static void PlayVoice(string pathOrFile, float volume = 1f, float pitch = 1f)
    {
        PlayInternal(ResolveVoicePath(pathOrFile), volume, pitch, false);
    }

    public static void PlayLooped(string pathOrFile, float volume = 0.5f, float pitch = 1f)
    {
        PlayInternal(ResolvePath(pathOrFile), volume, pitch, true);
    }

    public static void Stop(string pathOrFile, float fadeDuration = 0f)
    {
        var path = ResolvePath(pathOrFile);
        if (!ActivePlayers.Remove(path, out var players)) return;

        foreach (var player in players.ToList())
        {
            if (!GodotObject.IsInstanceValid(player)) continue;
            if (fadeDuration <= 0f)
            {
                player.Stop();
                player.QueueFree();
                continue;
            }

            var tween = player.CreateTween();
            tween.TweenMethod(
                Callable.From<float>(value => player.VolumeDb = value),
                player.VolumeDb,
                -80f,
                fadeDuration
            );
            tween.TweenCallback(Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(player)) return;
                player.Stop();
                player.QueueFree();
            }));
        }
    }

    public static void StopAll(float fadeDuration = 0f)
    {
        foreach (var path in ActivePlayers.Keys.ToList())
            Stop(path, fadeDuration);
    }

    private static void PlayInternal(string path, float volume, float pitch, bool loop)
    {
        if (!ResourceLoader.Exists(path))
        {
            if (MissingResources.Add(path))
                MainFile.Logger.Warn($"Audio resource not found: {path}");
            return;
        }

        if (!Cache.TryGetValue(path, out var stream))
        {
            stream = GD.Load<AudioStream>(path);
            if (stream != null) Cache[path] = stream;
        }
        if (stream == null) return;

        volume *= SaveManager.Instance.SettingsSave.VolumeSfx;
        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = volume <= 0f ? -80f : Mathf.LinearToDb(volume),
            PitchScale = pitch,
            Autoplay = true
        };

        (Engine.GetMainLoop() as SceneTree)?.Root.AddChild(player);
        if (!ActivePlayers.TryGetValue(path, out var players))
        {
            players = [];
            ActivePlayers[path] = players;
        }
        players.Add(player);

        if (loop)
        {
            player.Finished += () =>
            {
                if (GodotObject.IsInstanceValid(player) && !player.Playing) player.Play();
            };
            return;
        }

        player.Finished += () =>
        {
            if (ActivePlayers.TryGetValue(path, out var list))
            {
                list.Remove(player);
                if (list.Count == 0) ActivePlayers.Remove(path);
            }
            if (GodotObject.IsInstanceValid(player)) player.QueueFree();
        };
    }

    private static string ResolveSfxPath(string pathOrFile) =>
        pathOrFile.StartsWith("res://", StringComparison.Ordinal) ? pathOrFile : Sfx(pathOrFile);

    private static string ResolveVoicePath(string pathOrFile) =>
        pathOrFile.StartsWith("res://", StringComparison.Ordinal) ? pathOrFile : Voice(pathOrFile);

    private static string ResolvePath(string pathOrFile) =>
        pathOrFile.StartsWith("res://", StringComparison.Ordinal) ? pathOrFile : $"{MainFile.ResPath}/audio/{pathOrFile}";
}
