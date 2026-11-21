using Godot;
using System;
using System.Text.Json;

namespace ClashOfThrones.Core;

public enum Difficulty { Easy, Normal, Hard }

/// <summary>Player options (graphics, audio, gameplay, accessibility). Stored in user://settings.json.</summary>
public sealed class Settings
{
    public int WindowMode { get; set; } = 0;          // 0 windowed, 1 fullscreen, 2 borderless fullscreen
    public int WindowScale { get; set; } = 3;         // windowed: 2x, 3x, 4x of 640x360
    public bool VSync { get; set; } = true;
    public int MaxFps { get; set; } = 0;              // 0 = unlimited
    public bool ScreenShake { get; set; } = true;
    public bool HitStop { get; set; } = true;
    public bool ShowFps { get; set; } = false;
    public float MasterVolume { get; set; } = 0.8f;
    public float MusicVolume { get; set; } = 0.6f;
    public float SfxVolume { get; set; } = 0.8f;
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    const string Path = "user://settings.json";
    public static Settings Current { get; private set; } = new();

    public static void Load()
    {
        try
        {
            if (FileAccess.FileExists(Path))
                Current = JsonSerializer.Deserialize<Settings>(FileAccess.GetFileAsString(Path)) ?? new Settings();
        }
        catch (Exception e) { GD.PushWarning($"settings unreadable, using defaults: {e.Message}"); Current = new Settings(); }
    }

    public static void Save()
    {
        using var f = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        f?.StoreString(JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void Apply(bool window = true)
    {
        var s = Current;
        if (window && DisplayServer.GetName() != "headless")
        {
            switch (s.WindowMode)
            {
                case 1: DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen); break;
                case 2: DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen); break;
                default:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                    var size = new Vector2I(640, 360) * Math.Clamp(s.WindowScale, 1, 6);
                    DisplayServer.WindowSetSize(size);
                    var screen = DisplayServer.ScreenGetUsableRect();
                    DisplayServer.WindowSetPosition(screen.Position + (screen.Size - size) / 2);
                    break;
            }
            DisplayServer.WindowSetVsyncMode(s.VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        }
        Engine.MaxFps = s.MaxFps;
        SetBus("Master", s.MasterVolume); SetBus("Music", s.MusicVolume); SetBus("SFX", s.SfxVolume); SetBus("Ambience", s.MusicVolume);
    }

    static void SetBus(string name, float linear)
    {
        int i = AudioServer.GetBusIndex(name);
        if (i < 0) return;
        AudioServer.SetBusVolumeDb(i, Mathf.LinearToDb(Mathf.Max(linear, 0.0001f)));
        AudioServer.SetBusMute(i, linear <= 0.001f);
    }

    public float EnemyDamageMul => Difficulty switch { Difficulty.Easy => 0.6f, Difficulty.Hard => 1.5f, _ => 1f };
    public float EnemyHpMul => Difficulty switch { Difficulty.Easy => 0.8f, Difficulty.Hard => 1.3f, _ => 1f };
}
