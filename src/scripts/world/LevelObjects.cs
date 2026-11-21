using Godot;
using System;
using ClashOfThrones.Actors;
using ClashOfThrones.Art;
using ClashOfThrones.Core;

namespace ClashOfThrones.World;

// =====================================================================================================  interactables
/// <summary>Something the player can use with Interact (E). Detected by the player's sensor (Interact layer).</summary>
public partial class Interactable : Area2D
{
    public string Id = "";
    public virtual bool CanInteract => true;
    public virtual string PromptText => "";
    public virtual void Interact(Player p) { }

    protected void Area(Vector2 size)
    {
        CollisionLayer = Layers.Interact; CollisionMask = 0; Monitoring = false; Monitorable = true;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size }, Position = new Vector2(0, -size.Y / 2) });
    }

    protected static bool Taken(string key) => Game.I?.Run?.Taken.Contains(key) ?? false;
    protected static void Take(string key) { var r = Game.I?.Run; if (r != null && !r.Taken.Contains(key)) r.Taken.Add(key); }
}

/// <summary>Altar: kindle it, rest (heal, refill, enemies return, autosave), level up, travel.</summary>
public partial class Altar : Interactable
{
    public string DisplayName = "Altar";
    public bool Lit => Game.I?.Run?.LitAltars.Contains(Id) ?? false;
    Sprite2D _spr;
    PointLight2D _light;

    public override string PromptText => Lit ? "[E] Rest" : "[E] Kindle the altar";

    public override void _Ready()
    {
        Area(new Vector2(56, 60));
        _spr = new Sprite2D { Texture = ArtLibrary.Get("altar"), Hframes = 2, Offset = new Vector2(0, -24) };
        AddChild(_spr);
        _light = new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.72f, 0.42f), Energy = 0f, TextureScale = 1.1f, Position = new Vector2(0, -28) };
        AddChild(_light);
        Refresh(false);
    }

    public void Refresh(bool animate = true)
    {
        _spr.Frame = Lit ? 1 : 0;
        if (animate) CreateTween().TweenProperty(_light, "energy", Lit ? 1.3f : 0f, 0.6); else _light.Energy = Lit ? 1.3f : 0f;
    }

    public override void Interact(Player p)
    {
        if (!Lit) { Game.I.Run.LitAltars.Add(Id); Refresh(); Sfx.Rest(this); Main.I.Hud.Message("ALTAR KINDLED"); }
        Main.I.OpenAltar(this);
    }
}

/// <summary>Chest with a reward (ashes:N, relic:id, key:id, item:id, flask_shard).</summary>
public partial class Chest : Interactable
{
    public string Loot = "ashes:100";
    Sprite2D _spr;
    bool Open => Taken("chest_" + Id);
    public override bool CanInteract => !Open;
    public override string PromptText => "[E] Open";

    public override void _Ready()
    {
        Area(new Vector2(40, 30));
        _spr = new Sprite2D { Texture = ArtLibrary.Get("chest"), Hframes = 2, Offset = new Vector2(0, -14), Frame = Open ? 1 : 0 };
        AddChild(_spr);
    }

    public override void Interact(Player p)
    {
        if (Open) return;
        Take("chest_" + Id); _spr.Frame = 1; Sfx.Chest(this);
        Main.I.Grant(Loot, GlobalPosition + new Vector2(0, -24));
    }
}

/// <summary>Lever: opens its linked gate(s) once.</summary>
public partial class Lever : Interactable
{
    public string GateId = "";
    Sprite2D _spr;
    bool Pulled => Taken("lever_" + Id);
    public override bool CanInteract => !Pulled;
    public override string PromptText => "[E] Pull the lever";

    public override void _Ready()
    {
        Area(new Vector2(30, 40));
        _spr = new Sprite2D { Texture = ArtLibrary.Get("lever"), Hframes = 2, Offset = new Vector2(0, -12), Frame = Pulled ? 1 : 0 };
        AddChild(_spr);
    }

    public override void Interact(Player p)
    {
        Take("lever_" + Id); _spr.Frame = 1; Sfx.Mechanism(this);
        Main.I.OpenGate(GateId);
    }
}

/// <summary>Locked door: opens with a key item, stays open.</summary>
public partial class Door : Interactable
{
    public string Key = "rusted_key";
    StaticBody2D _body;
    bool Opened => Taken("door_" + Id);
    public override bool CanInteract => !Opened;
    public override string PromptText => Game.I.Run.Has(Key) ? "[E] Unlock" : "Locked. It needs a key.";

    public override void _Ready()
    {
        Area(new Vector2(70, 96));
        _body = new StaticBody2D { CollisionLayer = Layers.World, CollisionMask = 0 };
        _body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(30, 96) }, Position = new Vector2(0, -48) });
        _body.AddChild(new Sprite2D { Texture = ArtLibrary.Get("door"), Offset = new Vector2(0, -48) });
        AddChild(_body);
        if (Opened) _body.QueueFree();
    }

    public override void Interact(Player p)
    {
        if (!Game.I.Run.Has(Key)) { Sfx.Denied(this); return; }
        Take("door_" + Id); Sfx.Mechanism(this);
        Main.I.Hud.Toast("Used: " + Items.NameOf(Key));
        var tw = CreateTween();
        tw.TweenProperty(_body, "modulate:a", 0f, 0.6);
        tw.TweenCallback(Callable.From(_body.QueueFree));
    }
}

/// <summary>Exit shrine: after the boss falls, ring the bell to finish the level.</summary>
public partial class ExitShrine : Interactable
{
    PointLight2D _light;
    public override bool CanInteract => Game.I.Run.BossDefeated;
    public override string PromptText => "[E] Ring the bell";

    public override void _Ready()
    {
        Area(new Vector2(60, 90));
        AddChild(new Sprite2D { Texture = ArtLibrary.Get("exit"), Offset = new Vector2(0, -48) });
        _light = new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.9f, 0.7f), Energy = Game.I.Run.BossDefeated ? 1.5f : 0.2f, TextureScale = 1.4f, Position = new Vector2(0, -50) };
        AddChild(_light);
    }

    public void Awaken() => CreateTween().TweenProperty(_light, "energy", 1.5f, 1.5);

    public override void Interact(Player p) => Main.I.Victory();
}

// =====================================================================================================  world pieces
/// <summary>Iron gate: closed until a lever (or the boss) opens it.</summary>
public partial class Gate : StaticBody2D
{
    public string Id = "";
    public bool StartOpen;
    Sprite2D _spr;
    CollisionShape2D _shape;
    public bool IsOpen { get; private set; }

    public override void _Ready()
    {
        CollisionLayer = Layers.World; CollisionMask = 0;
        _shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(24, 96) }, Position = new Vector2(0, -48) };
        AddChild(_shape);
        _spr = new Sprite2D { Texture = ArtLibrary.Get("gate"), Offset = new Vector2(0, -48) };
        AddChild(_spr);
        if (StartOpen || (Game.I?.Run?.Taken.Contains("gate_" + Id) ?? false)) SetOpen(true, false);
    }

    public void SetOpen(bool open, bool animate = true)
    {
        IsOpen = open;
        _shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, open);
        float y = open ? -84 : 0;
        if (animate) { CreateTween().TweenProperty(_spr, "position:y", y, 0.8).SetTrans(Tween.TransitionType.Quad); Sfx.Mechanism(this); }
        else _spr.Position = new Vector2(0, y);
    }
}

/// <summary>Cracked wall: three hits and it crumbles, revealing a secret.</summary>
public partial class BreakableWall : StaticBody2D, IDamageable
{
    public string Id = "";
    public int Tiles = 2;
    int _hp = 3;

    public override void _Ready()
    {
        if (Game.I?.Run?.Taken.Contains("wall_" + Id) ?? false) { QueueFree(); return; }
        CollisionLayer = Layers.World | Layers.Enemy; CollisionMask = 0;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(32, 32 * Tiles) }, Position = new Vector2(16, 16 * Tiles) });
        for (int i = 0; i < Tiles; i++) AddChild(new Sprite2D { Texture = ArtLibrary.Get("breakable"), Centered = false, Position = new Vector2(0, i * 32) });
    }

    public HitResult TakeHit(in HitInfo hit)
    {
        if (hit.Attacker is not Player) return HitResult.Ignored;
        _hp -= hit.Heavy ? 2 : 1;
        Effects.Puff(GetParent(), GlobalPosition + new Vector2(16, 16 * Tiles), 1.5f);
        Sfx.Break(this);
        var tw = CreateTween();
        tw.TweenProperty(this, "position:x", Position.X + 2, 0.04); tw.TweenProperty(this, "position:x", Position.X, 0.04);
        if (_hp <= 0)
        {
            var r = Game.I?.Run; if (r != null) r.Taken.Add("wall_" + Id);
            Main.I?.Hud.Toast("A hidden passage!");
            Effects.Puff(GetParent(), GlobalPosition + new Vector2(16, 16 * Tiles), 3f);
            QueueFree();
        }
        return HitResult.Hit;
    }
}

/// <summary>Item on the floor (relic, key item, flask shard): walk over it to take it.</summary>
public partial class Pickup : Area2D
{
    public string Id = "", Loot = "flask_shard";
    Sprite2D _spr;
    float _t;

    public override void _Ready()
    {
        if (Game.I?.Run?.Taken.Contains("item_" + Id) ?? false) { QueueFree(); return; }
        CollisionLayer = 0; CollisionMask = Layers.Player;
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 14 }, Position = new Vector2(0, -14) });
        _spr = new Sprite2D { Texture = ArtLibrary.Get("pickup"), Hframes = 4, Offset = new Vector2(0, -16) };
        AddChild(_spr);
        AddChild(new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.85f, 0.5f), Energy = 1f, TextureScale = 0.5f, Position = new Vector2(0, -16) });
        BodyEntered += b =>
        {
            if (b is not Player) return;
            Game.I.Run.Taken.Add("item_" + Id);
            Main.I.Grant(Loot, GlobalPosition + new Vector2(0, -20));
            QueueFree();
        };
    }

    public override void _Process(double delta)
    {
        _t += (float)delta;
        _spr.Frame = (int)(_t * 6) % 4;
        _spr.Position = new Vector2(0, MathF.Sin(_t * 2.5f) * 3);
    }
}

/// <summary>Climbable ladder zone.</summary>
public partial class Ladder : Area2D
{
    public int Tiles = 4;
    public override void _Ready()
    {
        CollisionLayer = Layers.Interact; CollisionMask = 0; Monitoring = false; Monitorable = true;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(14, Tiles * 32 + 16) }, Position = new Vector2(0, Tiles * 16 - 8) });
        for (int i = 0; i < Tiles; i++) AddChild(new Sprite2D { Texture = ArtLibrary.Get("ladder"), Position = new Vector2(0, i * 32 + 16), ZIndex = -1 });
    }
}

/// <summary>Invisible trigger that wakes the boss and closes the arena.</summary>
public partial class BossTrigger : Area2D
{
    public override void _Ready()
    {
        CollisionLayer = 0; CollisionMask = Layers.Player;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 400) }, Position = new Vector2(0, -200) });
        BodyEntered += b => { if (b is Player) Main.I.StartBoss(); };
    }
}

// =====================================================================================================  hazards
/// <summary>Hazard base: touching it hurts and puts the player back on safe ground.</summary>
public partial class Hazard : Area2D
{
    public int Damage = 1;
    protected void Setup(Vector2 size, Vector2 offset)
    {
        CollisionLayer = Layers.Hazard; CollisionMask = Layers.Player; Monitorable = true;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size }, Position = offset });
        BodyEntered += b => { if (b is Player p) p.TakeHit(new HitInfo { Damage = Damage, Hazard = true }); };
    }
}

public partial class Spikes : Hazard
{
    public int Tiles = 1;
    public override void _Ready()
    {
        Setup(new Vector2(Tiles * 32 - 6, 10), new Vector2(Tiles * 16, -5));
        for (int i = 0; i < Tiles; i++) AddChild(new Sprite2D { Texture = ArtLibrary.Get("spikes"), Centered = false, Position = new Vector2(i * 32, -16) });
    }
}

public partial class KillZone : Hazard
{
    public Vector2 Size = new(4000, 40);
    public override void _Ready() { Damage = 2; Setup(Size, Size / 2); }
}

/// <summary>Crumbling ledge: shakes when stood on, falls, comes back after a while.</summary>
public partial class CrumblePlatform : StaticBody2D
{
    public int Tiles = 2;
    Node2D _gfx;
    CollisionShape2D _shape;
    Area2D _sense;
    bool _busy;

    public override void _Ready()
    {
        CollisionLayer = Layers.World; CollisionMask = 0;
        _shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(Tiles * 32, 12) }, Position = new Vector2(Tiles * 16, 6) };
        AddChild(_shape);
        _gfx = new Node2D();
        for (int i = 0; i < Tiles; i++) _gfx.AddChild(new Sprite2D { Texture = ArtLibrary.Get("crumble"), Centered = false, Position = new Vector2(i * 32, 0) });
        AddChild(_gfx);
        _sense = new Area2D { CollisionLayer = 0, CollisionMask = Layers.Player };
        _sense.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(Tiles * 32 - 4, 6) }, Position = new Vector2(Tiles * 16, -3) });
        AddChild(_sense);
        _sense.BodyEntered += b => { if (b is Player && !_busy) Crumble(); };
    }

    async void Crumble()
    {
        _busy = true;
        var shake = CreateTween().SetLoops(6);
        shake.TweenProperty(_gfx, "position:x", 1.5f, 0.04); shake.TweenProperty(_gfx, "position:x", -1.5f, 0.04);
        Sfx.Break(this);
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        _shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        Effects.Puff(GetParent(), GlobalPosition + new Vector2(Tiles * 16, 6), 1.5f);
        var fall = CreateTween();
        fall.TweenProperty(_gfx, "position:y", 120f, 0.6).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        fall.Parallel().TweenProperty(_gfx, "modulate:a", 0f, 0.6);
        await ToSignal(GetTree().CreateTimer(3.5), SceneTreeTimer.SignalName.Timeout);
        _gfx.Position = Vector2.Zero; _gfx.Modulate = Colors.White;
        _shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _busy = false;
    }
}

/// <summary>Moving iron platform (one-way from below), ping-pongs between its start and start + Travel.</summary>
public partial class MovingPlatform : AnimatableBody2D
{
    public Vector2 Travel = new(128, 0);
    public float Period = 4f;
    Vector2 _start;
    float _t;

    public override void _Ready()
    {
        _start = Position;
        SyncToPhysics = true;
        CollisionLayer = Layers.OneWay; CollisionMask = 0;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(64, 10) }, Position = new Vector2(32, 7), OneWayCollision = true });
        AddChild(new Sprite2D { Texture = ArtLibrary.Get("platform"), Centered = false });
    }

    public override void _PhysicsProcess(double delta)
    {
        _t += (float)delta;
        float k = (1 - MathF.Cos(_t / Period * MathF.Tau)) * 0.5f;
        Position = (_start + Travel * k).Round();
    }
}

/// <summary>Swinging pendulum blade hanging from the ceiling.</summary>
public partial class Pendulum : Node2D
{
    public float Length = 200, Amplitude = 38, Phase;
    Node2D _arm;
    Area2D _blade;
    Line2D _chain;
    float _t, _cd;

    public override void _Ready()
    {
        _arm = new Node2D();
        AddChild(_arm);
        _chain = new Line2D { Width = 2, DefaultColor = new Color(0.25f, 0.23f, 0.28f), Points = new[] { Vector2.Zero, new Vector2(0, Length) } };
        _arm.AddChild(_chain);
        _blade = new Area2D { CollisionLayer = 0, CollisionMask = Layers.Player, Position = new Vector2(0, Length) };
        _blade.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(40, 14) }, Position = new Vector2(0, 8) });
        _blade.AddChild(new Sprite2D { Texture = ArtLibrary.Get("pendulum"), Offset = new Vector2(0, 8) });
        _arm.AddChild(_blade);
    }

    public override void _PhysicsProcess(double delta)
    {
        _t += (float)delta; _cd -= (float)delta;
        _arm.Rotation = Mathf.DegToRad(Amplitude * MathF.Sin(_t * 1.6f + Phase));
        if (_cd > 0) return;
        foreach (var b in _blade.GetOverlappingBodies())
            if (b is Player p) { _cd = 0.6f; p.TakeHit(new HitInfo { Damage = 1, Attacker = _blade, Knockback = 220, Unblockable = true }); }
    }
}

/// <summary>Fire jet: on/off cycle with a short sputter before it fires.</summary>
public partial class FireJet : Area2D
{
    public float Offset;
    Sprite2D _spr;
    PointLight2D _light;
    float _t, _cd;

    public override void _Ready()
    {
        CollisionLayer = 0; CollisionMask = Layers.Player;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(14, 50) }, Position = new Vector2(0, -32) });
        _spr = new Sprite2D { Texture = ArtLibrary.Get("firejet"), Hframes = 4, Offset = new Vector2(0, -32) };
        AddChild(_spr);
        _light = new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.55f, 0.2f), Energy = 0, TextureScale = 0.8f, Position = new Vector2(0, -30) };
        AddChild(_light);
        _t = Offset;
    }

    public override void _PhysicsProcess(double delta)
    {
        _t += (float)delta; _cd -= (float)delta;
        float cyc = _t % 3f;
        bool on = cyc > 1.5f;
        bool sputter = cyc > 1.2f && cyc <= 1.5f;
        _spr.Frame = on ? 1 + (int)(_t * 14) % 3 : sputter ? ((int)(_t * 20) % 2 == 0 ? 1 : 0) : 0;
        _light.Energy = on ? 1.4f : sputter ? 0.4f : 0;
        if (!on || _cd > 0) return;
        foreach (var b in GetOverlappingBodies())
            if (b is Player p) { _cd = 0.7f; p.TakeHit(new HitInfo { Damage = 1, Attacker = this, Knockback = 160, Unblockable = true }); }
    }
}

// =====================================================================================================  loot
public static class Loot
{
    public static void SpawnAshes(Node parent, Vector2 at, int amount)
    {
        if (parent == null || amount <= 0) return;
        int n = Math.Clamp(amount / 10, 1, 14);
        int each = amount / n, rest = amount - each * n;
        for (int i = 0; i < n; i++)
        {
            var o = new AshOrb { Value = each + (i == 0 ? rest : 0) };
            parent.CallDeferred(Node.MethodName.AddChild, o);
            o.Position = at;
            o.Velocity = new Vector2((float)GD.RandRange(-90, 90), (float)GD.RandRange(-200, -80));
        }
    }
}

/// <summary>Ash orb: bursts out, then flies to the player and adds to the ashes count.</summary>
public partial class AshOrb : Node2D
{
    public int Value = 10;
    public Vector2 Velocity;
    float _t;

    public override void _Ready()
    {
        ZIndex = 15;
        AddChild(new Sprite2D { Texture = ArtLibrary.Get("orb") });
        AddChild(new PointLight2D { Texture = ArtLibrary.RadialLight(64), Color = new Color(1f, 0.7f, 0.35f), Energy = 0.7f, TextureScale = 0.6f });
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _t += dt;
        var p = Main.I?.Player;
        if (_t < 0.45f || p == null || p.IsDead) { Velocity.Y += 500 * dt; Velocity *= 0.97f; }
        else
        {
            var to = p.GlobalPosition + new Vector2(0, -36) - GlobalPosition;
            Velocity = Velocity.Lerp(to.Normalized() * 420, 8 * dt);
            if (to.Length() < 14)
            {
                if (Game.I?.Run != null) Game.I.Run.Ashes += Value;
                Sfx.Ash(this);
                QueueFree();
            }
        }
        GlobalPosition += Velocity * dt;
    }
}

/// <summary>Where the player died: holds the ashes they carried. Touch it to take them back; dying again loses them.</summary>
public partial class Remnant : Area2D
{
    public int Ashes;
    public override void _Ready()
    {
        CollisionLayer = 0; CollisionMask = Layers.Player;
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 14 }, Position = new Vector2(0, -12) });
        AddChild(new Sprite2D { Texture = ArtLibrary.Get("remnant"), Offset = new Vector2(0, -12) });
        AddChild(new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.6f, 0.3f), Energy = 1f, TextureScale = 0.5f, Position = new Vector2(0, -12) });
        BodyEntered += b =>
        {
            if (b is not Player) return;
            var r = Game.I.Run;
            r.Ashes += r.RemnantAshes; Main.I.Hud.Toast($"Recovered {r.RemnantAshes} ashes");
            r.RemnantAshes = 0;
            Sfx.Pickup(this);
            QueueFree();
        };
    }
}
