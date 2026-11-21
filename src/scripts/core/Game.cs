using Godot;
using System;

namespace ClashOfThrones.Core;

/// <summary>Autoload "Game": input map, audio buses, hit-stop and screen shake. Lives for the whole session.</summary>
public partial class Game : Node
{
    public static Game I { get; private set; }

    /// <summary>Screen-shake trauma 0..1, read and decayed by the camera.</summary>
    public float Trauma { get; private set; }

    public override void _EnterTree()
    {
        I = this;
        ProcessMode = ProcessModeEnum.Always;
        InputSetup.Apply();
        SetupAudioBuses();
    }

    public void Shake(float amount) => Trauma = Mathf.Clamp(Trauma + amount, 0f, 1f);
    public void DecayTrauma(float delta) => Trauma = Mathf.Max(0f, Trauma - delta * 1.6f);

    int _stopId;
    /// <summary>Freezes the game for a few frames to sell an impact.</summary>
    public async void HitStop(double seconds, double scale = 0.03)
    {
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
