using Godot;
using System;
using ClashOfThrones.Actors;
using ClashOfThrones.Art;
using ClashOfThrones.Core;

namespace ClashOfThrones.World;

/// <summary>Wall candelabrum: flame frames + a flickering warm light.</summary>
public partial class Candles : Node2D
{
    Sprite2D _spr;
    PointLight2D _light;
    float _t, _seed;

    public override void _Ready()
    {
        _seed = GlobalPosition.X * 0.137f;
        _spr = new Sprite2D { Texture = ArtLibrary.Get("candles"), Hframes = 4, Offset = new Vector2(0, -20) };
        AddChild(_spr);
        _light = new PointLight2D
        {
            Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.66f, 0.36f), Energy = 1.25f, TextureScale = 1.35f,
            Position = new Vector2(0, -34),
        };
        AddChild(_light);
    }

    public override void _Process(double delta)
    {
        _t += (float)delta;
        _spr.Frame = (int)(_t * 8 + _seed) % 4;
        float f = MathF.Sin(_t * 9 + _seed) * 0.5f + MathF.Sin(_t * 23 + _seed * 2) * 0.3f;
        _light.Energy = 1.2f + f * 0.12f;
        _light.TextureScale = 1.35f + f * 0.03f;
    }
}

/// <summary>Follow camera: look-ahead, smoothing, whole-pixel positions (no sub-pixel blur) and trauma shake.</summary>
public partial class CameraRig : Camera2D
{
    public Node2D Target;
    Vector2 _pos;
    readonly FastNoiseLite _noise = new() { Frequency = 0.9f };
    float _t;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 100;   // after the player moved
        PositionSmoothingEnabled = false;
        if (Target != null) _pos = Target.GlobalPosition;
    }

    public void Snap() { if (Target != null) { _pos = Goal(); GlobalPosition = _pos.Round(); } }

    Vector2 Goal()
    {
        int facing = Target is Player p ? p.Facing : 1;
        return Target.GlobalPosition + new Vector2(facing * 36, -70);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Target == null) return;
        float dt = (float)delta;
        _t += dt * 60;
        var goal = Goal();
        _pos.X = Mathf.Lerp(_pos.X, goal.X, 1 - MathF.Exp(-5f * dt));
        _pos.Y = Mathf.Lerp(_pos.Y, goal.Y, 1 - MathF.Exp(-3.5f * dt));
        float tr = Game.I?.Trauma ?? 0;
        Game.I?.DecayTrauma(dt);
        var shake = new Vector2(_noise.GetNoise2D(_t, 0), _noise.GetNoise2D(0, _t)) * tr * tr * 10f;
        GlobalPosition = (_pos + shake).Round();
    }
}
