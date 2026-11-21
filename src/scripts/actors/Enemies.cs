using Godot;
using System;
using ClashOfThrones.Art;
using ClashOfThrones.World;

namespace ClashOfThrones.Actors;

/// <summary>Grave Warden: heavy, slow, poise during swings, long stagger when parried.</summary>
public partial class Warden : EnemyBase
{
    public Warden()
    {
        Sheet = "warden"; Frame = ArtLibrary.WardenFrame; Origin = ArtLibrary.WardenOrigin;
        BaseHp = 80; AttackDamage = 2; AshDrop = 60; BodyRadius = 14; BodyHeight = 70;
    }
}

/// <summary>Ghoul: fast, fragile, lunges with claws, then leaps back out of reach.</summary>
public partial class Ghoul : EnemyBase
{
    public Ghoul()
    {
        Sheet = "ghoul"; Frame = ArtLibrary.HeroFrame; Origin = ArtLibrary.HeroOrigin;
        BaseHp = 35; AttackDamage = 1; AshDrop = 25; BodyRadius = 10; BodyHeight = 52;
        PatrolSpeed = 40; ChaseSpeed = 115; SightRange = 230; AttackRange = 70; HasPoise = false; StaggerTime = 1.2f;
        HitboxSize = new Vector2(46, 34); HitboxOffset = new Vector2(30, -32); ActiveFrames = new[] { 0, 1 }; AttackLunge = 260;
    }

    protected override bool Think(float dt, ref Vector2 v, float dx, float dy, bool sees)
    {
        if (State != EState.Retreat) return false;
        v.X = -Facing * 150;
        if (T > 0.35f) Enter(EState.Chase, "walk");
        return true;
    }

    protected override void OnAnimFinished()
    {
        if (State == EState.Recover) { Enter(EState.Retreat, "walk"); Velocity = new Vector2(-Facing * 150, -180); return; }
        base.OnAnimFinished();
    }
}

/// <summary>Bone Archer: keeps its distance, shoots arrows (parry sends them back), backs away when you close in.</summary>
public partial class Archer : EnemyBase
{
    public Archer()
    {
        Sheet = "archer"; Frame = ArtLibrary.HeroFrame; Origin = ArtLibrary.HeroOrigin;
        BaseHp = 30; AttackDamage = 1; AshDrop = 30; BodyRadius = 10; BodyHeight = 58;
        PatrolSpeed = 24; ChaseSpeed = 50; SightRange = 300; AttackRange = 280; HasPoise = false;
    }

    protected override bool Think(float dt, ref Vector2 v, float dx, float dy, bool sees)
    {
        if (State != EState.Chase) return false;
        SetFacing(MathF.Sign(dx));
        if (!sees) { Enter(EState.Search, "walk"); return true; }
        float d = MathF.Abs(dx);
        if (d < 100 && GroundAhead(-Facing)) { v.X = -Facing * ChaseSpeed; Play("walk"); return true; }   // back off
        if (d <= AttackRange && MathF.Abs(dy) < 70) { v.X = 0; StartAttack(); return true; }
        v.X = GroundAhead(Facing) ? Facing * ChaseSpeed : 0; Play(v.X != 0 ? "walk" : "idle");
        return true;
    }

    protected override void TryMeleeHit() { }

    protected override void OnFrame(string anim, int frame)
    {
        if (anim != "attack" || frame != 0 || State != EState.Attack) return;
        var from = GlobalPosition + new Vector2(Facing * 16, -38);
        var aim = Target != null ? (Target.GlobalPosition + new Vector2(0, -36) - from).Normalized() : new Vector2(Facing, 0);
        if (MathF.Sign(aim.X) != Facing) aim = new Vector2(Facing, 0);
        GetParent().AddChild(new Arrow { GlobalPosition = from, Direction = aim, Shooter = this });
        Sfx.Bow(this);
    }
}

/// <summary>Wisp: a burning skull that hovers, then swoops through the player.</summary>
public partial class Wisp : EnemyBase
{
    Vector2 _home, _swoopDir;

    public Wisp()
    {
        Sheet = "wisp"; Frame = new Vector2(32, 32); Origin = new Vector2(16, 24);
        BaseHp = 20; AttackDamage = 1; AshDrop = 15; BodyRadius = 8; BodyHeight = 18; Flying = true; HasPoise = false;
        SightRange = 220; AttackRange = 200; PatrolSpeed = 30; ChaseSpeed = 60; StaggerTime = 1f;
    }

    protected override SpriteFrames BuildFrames()
    {
        var tex = ArtLibrary.Get("wisp");
        var sf = new SpriteFrames();
        sf.RemoveAnimation("default");
        void Add(string name, int from, int to, float fps, bool loop)
        {
            sf.AddAnimation(name); sf.SetAnimationSpeed(name, fps); sf.SetAnimationLoopMode(name, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
            for (int f = from; f <= to; f++) sf.AddFrame(name, new AtlasTexture { Atlas = tex, Region = new Rect2(f * 32, 0, 32, 32) });
        }
        Add("idle", 0, 3, 8, true); Add("walk", 0, 3, 10, true); Add("windup", 4, 5, 6, true); Add("attack", 4, 5, 10, true);
        Add("recover", 0, 3, 8, true); Add("hurt", 4, 4, 10, false); Add("stagger", 0, 3, 4, true); Add("death", 6, 7, 6, false);
        return sf;
    }

    public override void _Ready()
    {
        base._Ready();
        _home = GlobalPosition;
        AddChild(new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(0.5f, 0.9f, 1f), Energy = 0.9f, TextureScale = 0.45f, Position = new Vector2(0, -12) });
    }

    protected override bool Think(float dt, ref Vector2 v, float dx, float dy, bool sees)
    {
        switch (State)
        {
            case EState.Patrol: case EState.Search:
            {
                var goal = _home + new Vector2(MathF.Sin(T * 0.7f) * 50, MathF.Sin(T * 1.9f) * 10);
                v = (goal - GlobalPosition) * 2f;
                SetFacing(MathF.Sign(v.X));
                Play("idle");
                if (sees) Alert();
                return true;
            }
            case EState.Chase:
            {
                if (Target == null) return true;
                var goal = Target.GlobalPosition + new Vector2(-MathF.Sign(dx) * 90, -90 + MathF.Sin(T * 3) * 8);
                v = (goal - GlobalPosition).LimitLength(ChaseSpeed * 2);
                SetFacing(MathF.Sign(dx));
                if (T > 1.4f && sees) { Enter(EState.Windup, "windup"); _swoopDir = (Target.GlobalPosition + new Vector2(0, -30) - GlobalPosition).Normalized(); }
                if (!sees && T > 4) Enter(EState.Search, "idle");
                return true;
            }
            case EState.Windup:
                v = -_swoopDir * 20 + new Vector2(MathF.Sin(T * 60) * 30, 0);
                if (T > 0.5f) { Enter(EState.Attack, "attack"); HitDone = false; Sfx.Swing(this, 1.4f); }
                return true;
            case EState.Attack:
                v = _swoopDir * 230;
                if (!HitDone) TryContact();
                if (T > 0.65f) Enter(EState.Recover, "recover");
                return true;
            case EState.Recover:
                v = v.MoveToward(new Vector2(0, -60), 400 * dt);
                if (T > 0.7f) Enter(EState.Chase, "walk");
                return true;
            case EState.Hurt:
                v = v.MoveToward(Vector2.Zero, 500 * dt);
                if (T > 0.3f) Enter(EState.Chase, "walk");
                return true;
            case EState.Dead:
                v = new Vector2(0, 40);
                return true;
        }
        return false;
    }

    void TryContact()
    {
        if (Target == null || Target.GlobalPosition.DistanceTo(GlobalPosition + new Vector2(0, 20)) > 30) return;
        HitDone = true;
        Target.TakeHit(new HitInfo { Damage = AttackDamage, Attacker = this, Knockback = 160, Parryable = true });
    }

    protected override void OnAnimFinished()
    {
        if (State == EState.Dead) { var tw = CreateTween(); tw.TweenProperty(this, "modulate:a", 0f, 0.4); tw.TweenCallback(Callable.From(QueueFree)); }
    }
}

/// <summary>Arrow: flies straight; blocked by shields; a parry turns it around against enemies.</summary>
public partial class Arrow : Area2D, IParryable
{
    public Vector2 Direction = Vector2.Right;
    public Node2D Shooter;
    bool _reflected;
    float _life = 3;

    public override void _Ready()
    {
        CollisionLayer = Layers.Projectile; CollisionMask = Layers.Player | Layers.World;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(14, 4) } });
        AddChild(new Sprite2D { Texture = ArtLibrary.Get("arrow") });
        Rotation = Direction.Angle();
        BodyEntered += OnBody;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += Direction * (_reflected ? 330 : 240) * (float)delta;
        _life -= (float)delta;
        if (_life <= 0) QueueFree();
    }

    void OnBody(Node2D body)
    {
        if (body is IDamageable d && body != Shooter)
        {
            var res = d.TakeHit(new HitInfo { Damage = _reflected ? 25 : 1, Attacker = this, Knockback = 120, Parryable = !_reflected });
            if (res == HitResult.Parried) return;
            if (res == HitResult.Ignored && body is Player) return;
        }
        if (body is Player || body is EnemyBase || body is StaticBody2D || body is TileMapLayer) QueueFree();
    }

    public void OnParried()
    {
        _reflected = true; Direction = -Direction; Rotation = Direction.Angle(); _life = 2;
        CollisionMask = Layers.Enemy | Layers.World;
    }
}
