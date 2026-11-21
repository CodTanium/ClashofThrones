using Godot;
using System;
using ClashOfThrones.Art;

namespace ClashOfThrones.World;

/// <summary>Short-lived visual effects.</summary>
public static class Effects
{
    public static void Spark(Node parent, Vector2 at, float size = 1f)
    {
        if (parent == null) return;
        var root = new Node2D { GlobalPosition = at, ZIndex = 20 };
        parent.AddChild(root);
        root.GlobalPosition = at;
        var flash = new Sprite2D { Texture = ArtLibrary.Get("spark"), Scale = Vector2.One * 2 * size };
        root.AddChild(flash);
        var light = new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.85f, 0.6f), Energy = 1.6f * size, TextureScale = 0.35f * size };
        root.AddChild(light);
        var bits = new CpuParticles2D
        {
            Emitting = true, OneShot = true, Amount = (int)(10 * size), Lifetime = 0.35, Explosiveness = 1f,
            Texture = ArtLibrary.Get("dust"), Spread = 180, InitialVelocityMin = 60, InitialVelocityMax = 170 * size,
            Gravity = new Vector2(0, 400), ScaleAmountMin = 1, ScaleAmountMax = 2, Color = new Color(1f, 0.85f, 0.55f),
        };
        root.AddChild(bits);
        var tw = root.CreateTween();
        tw.TweenProperty(flash, "scale", Vector2.One * 0.5f, 0.12);
        tw.Parallel().TweenProperty(light, "energy", 0f, 0.2);
        tw.TweenInterval(0.3);
        tw.TweenCallback(Callable.From(root.QueueFree));
    }
}

/// <summary>Placeholder sound effects synthesised in code (no audio files needed yet). Bus "SFX".</summary>
public static class Sfx
{
    static AudioStreamWav _swing, _hit, _heavy, _parry, _rest;
    const int Rate = 22050;

    static AudioStreamWav Make(float seconds, Func<float, float, float> sample)
    {
        int n = (int)(Rate * seconds);
        var data = new byte[n * 2];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate, u = i / (float)n;
            short s = (short)(Math.Clamp(sample(t, u), -1f, 1f) * 30000);
            data[i * 2] = (byte)(s & 0xff); data[i * 2 + 1] = (byte)((s >> 8) & 0xff);
        }
        return new AudioStreamWav { Data = data, Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = Rate, Stereo = false };
    }

    static readonly Random Rng = new(7);
    static float Noise() => (float)(Rng.NextDouble() * 2 - 1);

    static AudioStreamWav SwingStream => _swing ??= Make(0.18f, (t, u) => Noise() * MathF.Sin(u * MathF.PI) * 0.35f);
    static AudioStreamWav HitStream => _hit ??= Make(0.16f, (t, u) => (Noise() * 0.6f + MathF.Sin(t * 2 * MathF.PI * 110) * 0.8f) * MathF.Pow(1 - u, 3));
    static AudioStreamWav HeavyStream => _heavy ??= Make(0.28f, (t, u) => (Noise() * 0.5f + MathF.Sin(t * 2 * MathF.PI * (70 - 30 * u)) * 1f) * MathF.Pow(1 - u, 2.5f));
    static AudioStreamWav ParryStream => _parry ??= Make(0.6f, (t, u) =>
        (MathF.Sin(t * 2 * MathF.PI * 1240) * 0.5f + MathF.Sin(t * 2 * MathF.PI * 1870) * 0.3f + MathF.Sin(t * 2 * MathF.PI * 2930) * 0.2f + Noise() * 0.3f * MathF.Max(0, 1 - u * 20)) * MathF.Pow(1 - u, 4));
    static AudioStreamWav RestStream => _rest ??= Make(1.4f, (t, u) =>
        (MathF.Sin(t * 2 * MathF.PI * 220) + MathF.Sin(t * 2 * MathF.PI * 330) * 0.6f + MathF.Sin(t * 2 * MathF.PI * 440) * 0.3f) * 0.3f * MathF.Min(1, u * 8) * MathF.Pow(1 - u, 2));

    static void Play(Node2D at, AudioStream s, float pitch = 1f, float db = -6f)
    {
        if (at == null || !at.IsInsideTree()) return;
        var p = new AudioStreamPlayer2D { Stream = s, Bus = "SFX", PitchScale = pitch * (0.94f + (float)Rng.NextDouble() * 0.12f), VolumeDb = db };
        at.GetParent().AddChild(p);
        p.GlobalPosition = at.GlobalPosition;
        p.Finished += p.QueueFree;
        p.Play();
    }

    public static void Swing(Node2D at, float pitch = 1f) => Play(at, SwingStream, pitch, -10);
    public static void Hit(Node2D at, bool heavy) => Play(at, heavy ? HeavyStream : HitStream, 1f, -4);
    public static void Parry(Node2D at) => Play(at, ParryStream, 1f, -6);
    public static void Rest(Node2D at) => Play(at, RestStream, 1f, -8);
}
