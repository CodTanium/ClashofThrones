using Godot;
using System;

namespace ClashOfThrones.Core;

/// <summary>
/// Autoload "Game": input map, audio buses, settings, the current save (run), scene changes, music, hit-stop, shake.
/// </summary>
public partial class Game : Node
{
    public static Game I { get; private set; }
    public const string MenuScene = "res://scenes/Menu.tscn", GameScene = "res://scenes/Main.tscn";

    /// <summary>The save slot being played (null on the main menu).</summary>
    public SaveData Run { get; private set; }
    public float Trauma { get; private set; }
    public Music Music { get; private set; }

    public override void _EnterTree()
    {
        I = this;
        ProcessMode = ProcessModeEnum.Always;
        InputSetup.Apply();
        SetupAudioBuses();
        Settings.Load();
    }

    public override void _Ready()
    {
        Settings.Apply(window: OS.GetCmdlineArgs().Length == 0 || Array.IndexOf(OS.GetCmdlineArgs(), "--resolution") < 0);
        Music = new Music();
        AddChild(Music);
    }

    public override void _Process(double delta)
    {
        if (Run != null && !GetTree().Paused) Run.PlaySeconds += delta;
    }

    // ------------------------------------------------------------------ runs
    public void NewGame(int slot)
    {
        Run = new SaveData { Slot = slot };
        SaveSystem.Write(Run);
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(GameScene);
    }

    public bool Continue(int slot)
    {
        var d = SaveSystem.Load(slot);
        if (d == null) return false;
        Run = d;
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(GameScene);
        return true;
    }

    public void SaveRun() { if (Run != null) SaveSystem.Write(Run); }

    /// <summary>When the game scene is started directly (editor, tests) there is no run yet: use the dev slot.</summary>
    public SaveData EnsureRun() => Run ??= new SaveData { Slot = SaveSystem.DevSlot };

    public void QuitToMenu(bool save = true)
    {
        if (save) SaveRun();
        Run = null;
        Engine.TimeScale = 1;
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(MenuScene);
    }

    // ------------------------------------------------------------------ feel
    public void Shake(float amount) { if (Settings.Current.ScreenShake) Trauma = Mathf.Clamp(Trauma + amount, 0f, 1f); }
    public void DecayTrauma(float delta) => Trauma = Mathf.Max(0f, Trauma - delta * 1.6f);

    int _stopId;
    /// <summary>Freezes the game for a few frames to sell an impact.</summary>
    public async void HitStop(double seconds, double scale = 0.03)
    {
        if (!Settings.Current.HitStop) return;
        int id = ++_stopId;
        Engine.TimeScale = scale;
        await ToSignal(GetTree().CreateTimer(seconds, true, false, true), SceneTreeTimer.SignalName.Timeout);
        if (id == _stopId) Engine.TimeScale = 1.0;
    }

    static void SetupAudioBuses()
    {
        foreach (var name in new[] { "Music", "SFX", "Ambience" })
        {
            if (AudioServer.GetBusIndex(name) >= 0) continue;
            AudioServer.AddBus();
            int idx = AudioServer.BusCount - 1;
            AudioServer.SetBusName(idx, name);
            AudioServer.SetBusSend(idx, "Master");
        }
    }
}

/// <summary>Synthesised music: a slow organ-like drone for exploration, a pulsing theme for the boss, a choir pad for menus.</summary>
public partial class Music : AudioStreamPlayer
{
    const int Rate = 22050;
    string _current = "";
    static AudioStreamWav _explore, _boss, _menu;

    public override void _Ready() { Bus = "Music"; ProcessMode = ProcessModeEnum.Always; }

    public void PlayTrack(string name)
    {
        if (name == _current) return;
        _current = name;
        var s = name switch { "boss" => _boss ??= Boss(), "menu" => _menu ??= Menu(), "explore" => _explore ??= Explore(), _ => null };
        if (s == null) { Stop(); return; }
        Stream = s; VolumeDb = -8; Play();
    }

    static AudioStreamWav Make(float seconds, Func<float, float> sample)
    {
        int n = (int)(Rate * seconds);
        var data = new byte[n * 2];
        for (int i = 0; i < n; i++)
        {
            short v = (short)(Math.Clamp(sample(i / (float)Rate), -1f, 1f) * 26000);
            data[i * 2] = (byte)(v & 0xff); data[i * 2 + 1] = (byte)((v >> 8) & 0xff);
        }
        return new AudioStreamWav { Data = data, Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = Rate, LoopMode = AudioStreamWav.LoopModeEnum.Forward, LoopEnd = n };
    }

    static float Organ(float t, float f) => MathF.Sin(t * MathF.Tau * f) * 0.6f + MathF.Sin(t * MathF.Tau * f * 2) * 0.25f + MathF.Sin(t * MathF.Tau * f * 3) * 0.1f;

    // D minor drone that moves between i and VI every 8 s (loop length 16 s: seamless because all frequencies complete whole cycles)
    static AudioStreamWav Explore() => Make(16f, t =>
    {
        bool second = t >= 8f;
        float root = second ? 58.27f : 73.42f; // Bb1 / D2
        float env = 0.5f + 0.5f * MathF.Sin(t / 16f * MathF.Tau - MathF.PI / 2) * 0f;
        float x = Organ(t, root) * 0.5f + Organ(t, root * 1.5f) * 0.25f + Organ(t, root * (second ? 2.52f : 2.38f)) * 0.15f;
        float fade = MathF.Min(1, MathF.Min(t % 8f, 8f - t % 8f) * 2f);
        return x * 0.35f * (0.7f + 0.3f * fade) + env * 0f;
    });

    static AudioStreamWav Boss() => Make(4f, t =>
    {
        float beat = t % 0.5f;
        float kick = MathF.Sin(t * MathF.Tau * (55 - beat * 60)) * MathF.Exp(-beat * 14);
        float bar = t % 2f;
        float root = bar < 1 ? 73.42f : 69.30f;
        float pad = Organ(t, root) * 0.3f + Organ(t, root * 1.189f) * 0.18f;
        return kick * 0.6f + pad * 0.35f;
    });

    static AudioStreamWav Menu() => Make(12f, t =>
    {
        float x = 0;
        foreach (var f in new[] { 146.83f, 174.61f, 220f, 293.66f })
            x += MathF.Sin(t * MathF.Tau * f + MathF.Sin(t * 0.5f + f) * 0.3f) * 0.2f;
        float swell = 0.6f + 0.4f * MathF.Sin(t / 12f * MathF.Tau);
        return x * 0.4f * swell;
    });
}
