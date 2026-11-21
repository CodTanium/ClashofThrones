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

/// <summary>Checkpoint altar: touch it to light it, heal and set the respawn point.</summary>
public partial class Checkpoint : Area2D
{
    [Signal] public delegate void ActivatedEventHandler(Checkpoint cp);

    public bool Lit { get; private set; }
    Sprite2D _spr;
    PointLight2D _light;

    public override void _Ready()
    {
        CollisionLayer = 0; CollisionMask = Layers.Player; Monitoring = true;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(48, 60) }, Position = new Vector2(0, -30) });
        _spr = new Sprite2D { Texture = ArtLibrary.Get("altar"), Hframes = 2, Offset = new Vector2(0, -24) };
        AddChild(_spr);
        _light = new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.72f, 0.42f), Energy = 0f, TextureScale = 1.1f, Position = new Vector2(0, -28) };
        AddChild(_light);
        BodyEntered += b => { if (b is Player p && !p.IsDead) Activate(p); };
    }

    public void SetLit(bool lit)
    {
        Lit = lit; _spr.Frame = lit ? 1 : 0;
        var tw = CreateTween();
        tw.TweenProperty(_light, "energy", lit ? 1.3f : 0f, 0.6);
    }

    void Activate(Player p)
    {
        p.Heal();
        if (Lit) return;
        SetLit(true);
        Sfx.Rest(this);
        EmitSignal(SignalName.Activated, this);
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
