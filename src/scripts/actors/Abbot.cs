using Godot;
using System;
using ClashOfThrones.Art;
using ClashOfThrones.Core;
using ClashOfThrones.World;

namespace ClashOfThrones.Actors;

/// <summary>
/// Boss: the Drowned Abbot. Phase 1: staff slam (shockwaves run along the floor - jump them), wide sweep (unblockable,
/// red tell - roll through it), summons wisps. Phase 2 below 50%: roar (pushes the player back), faster, fire pillars
/// erupt under the player. Parrying the slam staggers him for an execution.
/// </summary>
public partial class Abbot : EnemyBase
{
    public string BossName => "THE DROWNED ABBOT";
    public bool Phase2 { get; private set; }
    public bool Awake { get; private set; }
    int _pattern;
    float _pillarCd = 3;

    public Abbot()
    {
        Sheet = "abbot"; Frame = ArtLibrary.AbbotFrame; Origin = ArtLibrary.AbbotOrigin;
        BaseHp = 700; AttackDamage = 2; AshDrop = 1500; BodyRadius = 22; BodyHeight = 110;
        PatrolSpeed = 0; ChaseSpeed = 55; SightRange = 600; AttackRange = 95; StaggerTime = 2.2f;
        HitboxSize = new Vector2(110, 90); HitboxOffset = new Vector2(70, -60); ActiveFrames = new[] { 1, 2 }; AttackLunge = 60;
    }

    public void Wake() { if (Awake) return; Awake = true; Enter(EState.Chase, "walk"); }

    protected override bool Think(float dt, ref Vector2 v, float dx, float dy, bool sees)
    {
        if (!Awake) { v.X = 0; Play("idle"); return true; }
        if (State == EState.Dead || State == EState.Executed) return false;
        if (Phase2 && State != EState.Special)
        {
            _pillarCd -= dt;
            if (_pillarCd <= 0 && Target != null) { _pillarCd = 3.2f; GetParent().AddChild(new FirePillar { GlobalPosition = new Vector2(Target.GlobalPosition.X, GlobalPosition.Y) }); }
        }
        if (State == EState.Chase)
        {
            SetFacing(MathF.Sign(dx));
            float d = MathF.Abs(dx);
            if (T > 0.6f)
            {
                int p = _pattern++ % (Phase2 ? 4 : 3);
                if (p == 2 && CountWisps() < 3) { Enter(EState.Special, "summon"); return true; }
                if (d <= AttackRange * 1.4f && p == 1) { Enter(EState.Windup, "sweep_windup"); Flash.SetShaderParameter("tint", new Color(1f, 0.1f, 0.05f, 0.35f)); return true; }
                if (d <= AttackRange) { Enter(EState.Windup, "windup"); return true; }
            }
            v.X = MathF.Sign(dx) * ChaseSpeed * (Phase2 ? 1.3f : 1f);
            Play("walk");
            return true;
        }
        if (State == EState.Special) { v.X = 0; return true; }
        return false;
    }

    int CountWisps()
    {
        int n = 0;
        foreach (var e in GetTree().GetNodesInGroup("enemies")) if (e is Wisp w && !w.IsDead) n++;
        return n;
    }

    protected override void TryMeleeHit()
    {
        bool sweep = Spr.Animation == "sweep";
        foreach (var body in Hitbox.GetOverlappingBodies())
        {
            if (body is not Player p) continue;
            HitDone = true;
            p.TakeHit(new HitInfo { Damage = 2, Attacker = this, Knockback = 260, Parryable = !sweep, Unblockable = sweep });
            return;
        }
    }

    protected override void OnFrame(string anim, int frame)
    {
        if (anim == "attack" && frame == 2)
        {
            // slam: shockwaves both ways along the floor
            Game.I.Shake(0.6f); Sfx.Slam(this);
            var at = GlobalPosition + new Vector2(Facing * 80, 0);
            Effects.Shockwave(GetParent(), at, 60);
            GetParent().AddChild(new Shockwave { GlobalPosition = at, Dir = 1 });
            GetParent().AddChild(new Shockwave { GlobalPosition = at, Dir = -1 });
        }
        if (anim == "sweep" && frame == 1) Sfx.Swing(this, 0.5f);
        if (anim == "summon" && frame == 3)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                var w = new Wisp { Position = Position + new Vector2(i * 70, -120) };
                GetParent().AddChild(w);
                w.Alert(false);
            }
            Sfx.Summon(this);
        }
        if (anim == "roar" && frame == 2)
        {
            Game.I.Shake(0.8f); Sfx.Roar(this);
            Effects.Shockwave(GetParent(), GlobalPosition, 160);
            if (Target != null && Target.GlobalPosition.DistanceTo(GlobalPosition) < 170)
                Target.TakeHit(new HitInfo { Damage = 1, Attacker = this, Knockback = 380, Unblockable = true });
        }
    }

    protected override void OnAnimFinished()
    {
        switch (Spr.Animation)
        {
            case "sweep_windup": HitDone = false; Enter(EState.Attack, "sweep"); return;
            case "sweep": Flash.SetShaderParameter("tint", new Color(0, 0, 0, 0)); Enter(EState.Recover, "recover"); return;
            case "summon": case "roar": Enter(EState.Chase, "walk"); return;
        }
        base.OnAnimFinished();
    }

    public override HitResult TakeHit(in HitInfo hit)
    {
        if (!Awake) Wake();
        if (State == EState.Special && Spr.Animation == "roar") return HitResult.Ignored;
        var r = base.TakeHit(hit);
        if (!Phase2 && Hp > 0 && Hp <= MaxHp / 2)
        {
            Phase2 = true; Spr.SpeedScale = 1.25f;
            Flash.SetShaderParameter("tint", new Color(0, 0, 0, 0));
            Enter(EState.Special, "roar");
            (World.Main.I)?.Hud.Message("THE ABBOT RISES IN WRATH", 1.4f);
        }
        return r;
    }

    public override void OnParried()
    {
        if (Spr.Animation == "sweep") return;
        base.OnParried();
        Flash.SetShaderParameter("tint", new Color(0, 0, 0, 0));
    }
}

/// <summary>Ground shockwave from the Abbot's slam: runs along the floor, jump over it.</summary>
public partial class Shockwave : Area2D
{
    public int Dir = 1;
    float _life = 1.6f;
    bool _hit;
    Polygon2D _shape;

    public override void _Ready()
    {
        CollisionLayer = 0; CollisionMask = Layers.Player;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(18, 22) }, Position = new Vector2(0, -11) });
        _shape = new Polygon2D
        {
            Polygon = new[] { new Vector2(-10, 0), new Vector2(0, -24), new Vector2(10, 0) }, Color = new Color(0.55f, 0.95f, 1f, 0.75f),
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded },
        };
        AddChild(_shape);
        BodyEntered += b => { if (!_hit && b is Player p) { _hit = true; p.TakeHit(new HitInfo { Damage = 1, Attacker = this, Knockback = 200, Unblockable = true }); } };
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += new Vector2(Dir * 210 * (float)delta, 0);
        _life -= (float)delta;
        _shape.Scale = new Vector2(1, 0.8f + MathF.Sin(_life * 30) * 0.2f);
        if (_life <= 0) QueueFree();
    }
}

/// <summary>Phase-2 fire pillar: 0.9 s glowing warning on the floor, then a column of fire.</summary>
public partial class FirePillar : Area2D
{
    float _t;
    bool _hit, _erupted;
    Polygon2D _warn, _fire;
    PointLight2D _light;

    public override void _Ready()
    {
        CollisionLayer = 0; CollisionMask = Layers.Player; Monitoring = true;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(26, 120) }, Position = new Vector2(0, -60) });
        var add = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        _warn = new Polygon2D { Polygon = new[] { new Vector2(-16, 0), new Vector2(16, 0), new Vector2(12, -4), new Vector2(-12, -4) }, Color = new Color(1f, 0.4f, 0.1f, 0.5f), Material = add };
        _fire = new Polygon2D { Polygon = new[] { new Vector2(-13, 0), new Vector2(13, 0), new Vector2(8, -130), new Vector2(0, -150), new Vector2(-8, -130) }, Color = new Color(1f, 0.55f, 0.15f, 0.85f), Material = add, Visible = false };
        _light = new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.5f, 0.2f), Energy = 0.4f, TextureScale = 0.6f, Position = new Vector2(0, -20) };
        AddChild(_warn); AddChild(_fire); AddChild(_light);
    }

    public override void _PhysicsProcess(double delta)
    {
        _t += (float)delta;
        _warn.Modulate = new Color(1, 1, 1, 0.5f + 0.5f * MathF.Sin(_t * 20));
        if (!_erupted && _t >= 0.9f) { _erupted = true; _fire.Visible = true; _light.Energy = 1.8f; _light.TextureScale = 1.2f; Sfx.Fire(this); Game.I.Shake(0.3f); }
        if (_erupted)
        {
            _fire.Scale = new Vector2(1 + MathF.Sin(_t * 40) * 0.1f, 1);
            if (!_hit) foreach (var b in GetOverlappingBodies()) if (b is Player p) { _hit = true; p.TakeHit(new HitInfo { Damage = 2, Attacker = this, Knockback = 150, Unblockable = true }); }
        }
        if (_t >= 1.6f) QueueFree();
    }
}
