using Godot;
using System;
using System.Collections.Generic;
using ClashOfThrones.Art;
using ClashOfThrones.UI;

namespace ClashOfThrones.World;

/// <summary>Short-lived visual effects.</summary>
public static class Effects
{
    static readonly CanvasItemMaterial Additive = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    static Node2D Root(Node parent, Vector2 at, int z = 20)
    {
        var root = new Node2D { ZIndex = z };
        parent.AddChild(root);
        root.GlobalPosition = at;
        return root;
    }

    static void FreeLater(Node2D root, double seconds) => root.GetTree().CreateTimer(seconds).Timeout += () => { if (GodotObject.IsInstanceValid(root)) root.QueueFree(); };

    static CpuParticles2D Burst(int amount, double life, float vmin, float vmax, Vector2 gravity, Color col, float spread = 180, float scaleMax = 2)
        => new()
        {
            Emitting = true, OneShot = true, Amount = Math.Max(1, amount), Lifetime = life, Explosiveness = 1f, Texture = ArtLibrary.Get("dust"),
            Spread = spread, InitialVelocityMin = vmin, InitialVelocityMax = vmax, Gravity = gravity, ScaleAmountMin = 1, ScaleAmountMax = scaleMax, Color = col,
        };

    public static void Spark(Node parent, Vector2 at, float size = 1f)
    {
        if (parent == null) return;
        var root = Root(parent, at);
        var flash = new Sprite2D { Texture = ArtLibrary.Get("spark"), Scale = Vector2.One * 2 * size, Material = Additive };
        root.AddChild(flash);
        var light = new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.85f, 0.6f), Energy = 1.6f * size, TextureScale = 0.35f * size };
        root.AddChild(light);
        root.AddChild(Burst((int)(10 * size), 0.35, 60, 170 * size, new Vector2(0, 400), new Color(1f, 0.85f, 0.55f)));
        var tw = root.CreateTween();
        tw.TweenProperty(flash, "scale", Vector2.One * 0.5f, 0.12);
        tw.Parallel().TweenProperty(light, "energy", 0f, 0.2);
        FreeLater(root, 0.6);
    }

    public static void Blood(Node parent, Vector2 at, float size = 1f)
    {
        if (parent == null) return;
        var root = Root(parent, at, 19);
        root.AddChild(Burst((int)(12 * size), 0.5, 40, 150 * size, new Vector2(0, 500), new Color(0.55f, 0.06f, 0.06f), 70, 2));
        FreeLater(root, 0.8);
    }

    public static void Puff(Node parent, Vector2 at, float size = 1f)
    {
        if (parent == null) return;
        var root = Root(parent, at, 9);
        root.AddChild(Burst((int)(8 * size), 0.4, 15, 50 * size, new Vector2(0, -20), new Color(0.75f, 0.72f, 0.8f, 0.6f), 180, 3));
        FreeLater(root, 0.6);
    }

    public static void Heal(Node parent, Vector2 at)
    {
        var root = Root(parent, at);
        var b = Burst(24, 0.9, 10, 40, new Vector2(0, -80), new Color(1f, 0.75f, 0.35f), 180, 2);
        b.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle; b.EmissionRectExtents = new Vector2(12, 24);
        root.AddChild(b);
        root.AddChild(new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.75f, 0.4f), Energy = 1.2f, TextureScale = 0.6f });
        FreeLater(root, 1.0);
    }

    public static void Shockwave(Node parent, Vector2 at, float radius)
    {
        if (parent == null) return;
        var root = Root(parent, at, 18);
        var ring = new Line2D { Width = 3, DefaultColor = new Color(1f, 0.85f, 0.6f, 0.8f), Material = Additive, Closed = true };
        for (int i = 0; i < 24; i++) { float a = i / 24f * MathF.Tau; ring.AddPoint(new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.35f) * 6); }
        root.AddChild(ring);
        root.AddChild(Burst(18, 0.5, 60, 160, new Vector2(0, 300), new Color(0.7f, 0.65f, 0.6f), 60, 3));
        var tw = root.CreateTween();
        tw.TweenProperty(ring, "scale", Vector2.One * (radius / 6f), 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        tw.Parallel().TweenProperty(ring, "modulate:a", 0f, 0.3);
        FreeLater(root, 0.7);
    }

    public static void Exclaim(Node2D who)
    {
        var l = UiKit.Label("!", 14, new Color(1f, 0.8f, 0.3f), HorizontalAlignment.Center);
        l.Position = new Vector2(-10, -110); l.Size = new Vector2(20, 16); l.ZIndex = 30;
        who.AddChild(l);
        var tw = l.CreateTween();
        tw.TweenProperty(l, "position:y", -122f, 0.3); tw.TweenInterval(0.4); tw.TweenProperty(l, "modulate:a", 0f, 0.3); tw.TweenCallback(Callable.From(l.QueueFree));
    }

    public static void DamageNumber(Node parent, Vector2 at, int dmg, bool big)
    {
        if (parent == null) return;
        var l = UiKit.Label(dmg.ToString(), big ? 14 : 9, big ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 0.95f, 0.85f), HorizontalAlignment.Center);
        l.ZIndex = 40; l.Size = new Vector2(40, 16);
        parent.AddChild(l);
        l.GlobalPosition = at + new Vector2(-20 + (float)GD.RandRange(-6, 6), 0);
        var tw = l.CreateTween();
        tw.TweenProperty(l, "position:y", l.Position.Y - 18, 0.6).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(l, "modulate:a", 0f, 0.6).SetDelay(0.3);
        tw.TweenCallback(Callable.From(l.QueueFree));
    }
}

/// <summary>Placeholder sound effects synthesised in code (no audio files needed yet). Bus "SFX".</summary>
public static class Sfx
{
    const int Rate = 22050;
    static readonly Dictionary<string, AudioStreamWav> Cache = new();
    static readonly Random Rng = new(7);
    static float Noise() => (float)(Rng.NextDouble() * 2 - 1);
    static float Sin(float t, float f) => MathF.Sin(t * MathF.Tau * f);

    static AudioStreamWav Make(string key, float seconds, Func<float, float, float> sample)
    {
        if (Cache.TryGetValue(key, out var s)) return s;
        int n = (int)(Rate * seconds);
        var data = new byte[n * 2];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate, u = i / (float)n;
            short v = (short)(Math.Clamp(sample(t, u), -1f, 1f) * 30000);
            data[i * 2] = (byte)(v & 0xff); data[i * 2 + 1] = (byte)((v >> 8) & 0xff);
        }
        return Cache[key] = new AudioStreamWav { Data = data, Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = Rate, Stereo = false };
    }

    static void Play(Node2D at, AudioStream s, float pitch = 1f, float db = -6f)
    {
        if (at == null || !at.IsInsideTree()) return;
        var p = new AudioStreamPlayer2D { Stream = s, Bus = "SFX", PitchScale = pitch * (0.94f + (float)Rng.NextDouble() * 0.12f), VolumeDb = db, ProcessMode = Node.ProcessModeEnum.Always };
        at.GetParent().AddChild(p);
        p.GlobalPosition = at.GlobalPosition;
        p.Finished += p.QueueFree;
        p.Play();
    }

    static AudioStreamPlayer _ui;
    static void PlayUi(AudioStream s, float db)
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree?.Root == null) return;
        if (_ui == null || !GodotObject.IsInstanceValid(_ui)) { _ui = new AudioStreamPlayer { Bus = "SFX", ProcessMode = Node.ProcessModeEnum.Always }; tree.Root.AddChild(_ui); }
        _ui.Stream = s; _ui.VolumeDb = db; _ui.Play();
    }

    public static void Swing(Node2D at, float pitch = 1f) => Play(at, Make("swing", 0.18f, (t, u) => Noise() * MathF.Sin(u * MathF.PI) * 0.35f), pitch, -10);
    public static void Hit(Node2D at, bool heavy) => Play(at, heavy
        ? Make("heavy", 0.28f, (t, u) => (Noise() * 0.5f + Sin(t, 70 - 30 * u)) * MathF.Pow(1 - u, 2.5f))
        : Make("hit", 0.16f, (t, u) => (Noise() * 0.6f + Sin(t, 110) * 0.8f) * MathF.Pow(1 - u, 3)), 1f, -4);
    public static void Parry(Node2D at) => Play(at, Make("parry", 0.6f, (t, u) => (Sin(t, 1240) * 0.5f + Sin(t, 1870) * 0.3f + Sin(t, 2930) * 0.2f + Noise() * 0.3f * MathF.Max(0, 1 - u * 20)) * MathF.Pow(1 - u, 4)), 1f, -6);
    public static void Block(Node2D at) => Play(at, Make("block", 0.25f, (t, u) => (Sin(t, 620) * 0.4f + Sin(t, 930) * 0.2f + Noise() * 0.4f * MathF.Max(0, 1 - u * 8)) * MathF.Pow(1 - u, 3)), 1f, -6);
    public static void GuardBreak(Node2D at) => Play(at, Make("gbreak", 0.4f, (t, u) => (Noise() * 0.6f + Sin(t, 180 - 100 * u) * 0.6f) * MathF.Pow(1 - u, 2)), 1f, -4);
    public static void Rest(Node2D at) => Play(at, Make("rest", 1.4f, (t, u) => (Sin(t, 220) + Sin(t, 330) * 0.6f + Sin(t, 440) * 0.3f) * 0.3f * MathF.Min(1, u * 8) * MathF.Pow(1 - u, 2)), 1f, -8);
    public static void Step(Node2D at) => Play(at, Make("step", 0.07f, (t, u) => Noise() * 0.5f * MathF.Pow(1 - u, 4)), 1f, -20);
    public static void Land(Node2D at) => Play(at, Make("land", 0.12f, (t, u) => (Noise() * 0.6f + Sin(t, 80) * 0.4f) * MathF.Pow(1 - u, 3)), 1f, -12);
    public static void Jump(Node2D at, float pitch = 1f) => Play(at, Make("jump", 0.12f, (t, u) => Noise() * 0.4f * MathF.Sin(u * MathF.PI)), pitch, -16);
    public static void Roll(Node2D at, float pitch = 1f) => Play(at, Make("roll", 0.3f, (t, u) => Noise() * 0.35f * MathF.Sin(u * MathF.PI)), pitch, -14);
    public static void Slam(Node2D at) => Play(at, Make("slam", 0.5f, (t, u) => (Noise() * 0.6f + Sin(t, 50 - 20 * u)) * MathF.Pow(1 - u, 2)), 1f, -2);
    public static void Drink(Node2D at) => Play(at, Make("drink", 0.6f, (t, u) => Sin(t, 300 + 80 * MathF.Sin(t * 30)) * 0.25f * MathF.Sin(u * MathF.PI)), 1f, -10);
    public static void Denied(Node2D at) => Play(at, Make("denied", 0.15f, (t, u) => Sin(t, 140) * 0.4f * (1 - u)), 1f, -12);
    public static void ShieldUp(Node2D at) => Play(at, Make("shield", 0.1f, (t, u) => (Noise() * 0.3f + Sin(t, 400) * 0.2f) * (1 - u)), 1f, -16);
    public static void Hurt(Node2D at) => Play(at, Make("hurt", 0.25f, (t, u) => (Noise() * 0.4f + Sin(t, 160 - 60 * u) * 0.7f) * MathF.Pow(1 - u, 2)), 1f, -4);
    public static void Wrath(Node2D at) => Play(at, Make("wrath", 0.9f, (t, u) => (Noise() * 0.4f * (1 - u) + Sin(t, 90 + 200 * u) * 0.5f) * MathF.Min(1, u * 10) * (1 - u)), 1f, -4);
    public static void Grab(Node2D at) => Play(at, Make("grab", 0.08f, (t, u) => Noise() * 0.4f * (1 - u)), 0.8f, -14);
    public static void Bow(Node2D at) => Play(at, Make("bow", 0.2f, (t, u) => (Sin(t, 220 - 100 * u) * 0.4f + Noise() * 0.2f) * MathF.Pow(1 - u, 2)), 1f, -8);
    public static void Summon(Node2D at) => Play(at, Make("summon", 1.0f, (t, u) => Sin(t, 300 + 400 * u) * 0.3f * MathF.Sin(u * MathF.PI) + Noise() * 0.1f * (1 - u)), 1f, -6);
    public static void Roar(Node2D at) => Play(at, Make("roar", 1.2f, (t, u) => (Noise() * 0.5f + Sin(t, 60 + 20 * MathF.Sin(t * 20)) * 0.7f) * MathF.Min(1, u * 6) * MathF.Pow(1 - u, 1.5f)), 1f, 0);
    public static void Fire(Node2D at) => Play(at, Make("fire", 0.6f, (t, u) => Noise() * 0.5f * MathF.Min(1, u * 10) * (1 - u)), 1f, -8);
    public static void Pickup(Node2D at) => Play(at, Make("pickup", 0.35f, (t, u) => (Sin(t, 880) * 0.3f + Sin(t, 1320) * 0.2f * (u > 0.4f ? 1 : 0)) * (1 - u)), 1f, -10);
    public static void Ash(Node2D at) => Play(at, Make("ash", 0.08f, (t, u) => Sin(t, 1500 + 500 * u) * 0.2f * (1 - u)), 1f, -18);
    public static void Chest(Node2D at) => Play(at, Make("chest", 0.5f, (t, u) => (Noise() * 0.3f + Sin(t, 120) * 0.3f) * MathF.Sin(u * MathF.PI)), 1f, -8);
    public static void Mechanism(Node2D at) => Play(at, Make("mech", 0.8f, (t, u) => (Noise() * 0.3f * (MathF.Sin(t * 60) > 0 ? 1 : 0.3f) + Sin(t, 70) * 0.3f) * MathF.Sin(u * MathF.PI)), 1f, -6);
    public static void Break(Node2D at) => Play(at, Make("break", 0.5f, (t, u) => (Noise() * 0.8f + Sin(t, 90) * 0.3f) * MathF.Pow(1 - u, 2)), 1f, -3);
    public static void LevelUp() => PlayUi(Make("lvl", 0.6f, (t, u) => (Sin(t, 440 + 220 * MathF.Floor(u * 3)) * 0.3f) * (1 - u)), -8);
    public static void UiClick() => PlayUi(Make("click", 0.06f, (t, u) => Sin(t, 900) * 0.25f * (1 - u)), -12);
    public static void UiMove() => PlayUi(Make("move", 0.04f, (t, u) => Sin(t, 1400) * 0.15f * (1 - u)), -18);
}
