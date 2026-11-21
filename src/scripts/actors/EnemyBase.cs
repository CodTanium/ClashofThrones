using Godot;
using System;
using ClashOfThrones.Art;
using ClashOfThrones.Core;
using ClashOfThrones.World;

namespace ClashOfThrones.Actors;

public enum EState { Patrol, Chase, Windup, Attack, Recover, Hurt, Stagger, Dead, Retreat, Search, Executed, Special }

/// <summary>
/// Shared enemy brain: patrol, detect (sight + line of sight), call nearby allies, chase, telegraphed attack, recover,
/// search the last known position, return to patrol. Hurt, poise, stagger (after parry / guard break), execution,
/// death with ash drop, small health bar. Subclasses tune the numbers and the attack.
/// </summary>
public abstract partial class EnemyBase : CharacterBody2D, IDamageable, IParryable
{
    // ---- tuning (subclasses override in their constructor / _Ready) ----
    protected string Sheet = "warden";
    protected Vector2 Frame = ArtLibrary.WardenFrame, Origin = ArtLibrary.WardenOrigin;
    protected float BodyRadius = 14, BodyHeight = 70;
    protected int BaseHp = 80, ContactDamage = 0, AttackDamage = 2, AshDrop = 60;
    protected float PatrolSpeed = 28, ChaseSpeed = 46, SightRange = 210, AttackRange = 62, PatrolRange = 70, StaggerTime = 1.6f;
    protected float Gravity = 1150;
    protected bool HasPoise = true, Flying;
    protected Vector2 HitboxSize = new(64, 56), HitboxOffset = new(40, -44);
    protected int[] ActiveFrames = { 1, 2 };
    protected float AttackLunge = 110;

    public int MaxHp { get; protected set; }
    public int Hp { get; protected set; }
    public string StateName => State.ToString();
    public bool IsDead => State == EState.Dead;
    public bool CanBeExecuted => State == EState.Stagger;
    public string SpawnId { get; set; } = "";
    public event Action<EnemyBase> Died;

    protected AnimatedSprite2D Spr;
    protected ShaderMaterial Flash;
    protected Area2D Hitbox;
    protected CollisionShape2D HitShape;
    protected EState State = EState.Patrol;
    protected float T, FlashT, HomeX, BarT;
    protected int Facing = -1, PatrolDir = -1;
    protected bool HitDone;
    protected Player Target;
    protected Vector2 LastSeen;
    float _alertCd;

    public override void _Ready()
    {
        AddToGroup("enemies");
        MaxHp = Hp = Mathf.RoundToInt(BaseHp * Settings.Current.EnemyHpMul);
        HomeX = GlobalPosition.X;
        CollisionLayer = Layers.Enemy; CollisionMask = Flying ? 0u : Layers.World | Layers.OneWay;
        FloorSnapLength = 6;
        AddChild(new CollisionShape2D { Shape = new CapsuleShape2D { Radius = BodyRadius, Height = BodyHeight }, Position = new Vector2(0, -BodyHeight / 2) });
        Flash = FlashMaterial.Create();
        Spr = new AnimatedSprite2D { SpriteFrames = BuildFrames(), Centered = false, Offset = -Origin, Material = Flash };
        AddChild(Spr);
        Spr.AnimationFinished += OnAnimFinished;
        Spr.FrameChanged += () => OnFrame(Spr.Animation, Spr.Frame);
        Spr.Play("idle");
        Hitbox = new Area2D { CollisionLayer = 0, CollisionMask = Layers.Player, Monitorable = false };
        HitShape = new CollisionShape2D { Shape = new RectangleShape2D { Size = HitboxSize } };
        Hitbox.AddChild(HitShape);
        AddChild(Hitbox);
        SetFacing(-1, true);
    }

    protected virtual SpriteFrames BuildFrames() => ArtLibrary.Frames(Sheet);

    protected void SetFacing(int f, bool force = false)
    {
        if (f == 0 || (f == Facing && !force)) return;
        Facing = f; Spr.FlipH = f < 0; HitShape.Position = new Vector2(HitboxOffset.X * f, HitboxOffset.Y);
    }

    protected void Enter(EState s, string anim)
    {
        State = s; T = 0;
        if (anim != null && Spr.SpriteFrames.HasAnimation(anim)) { Spr.Play(anim); Spr.Frame = 0; }
    }

    protected bool Sees(out float dx, out float dy)
    {
        dx = Target != null ? Target.GlobalPosition.X - GlobalPosition.X : 9999;
        dy = Target != null ? Target.GlobalPosition.Y - GlobalPosition.Y : 9999;
        if (Target == null || Target.IsDead || MathF.Abs(dx) > SightRange || MathF.Abs(dy) > 90) return false;
        var q = PhysicsRayQueryParameters2D.Create(GlobalPosition + new Vector2(0, -BodyHeight * 0.7f), Target.GlobalPosition + new Vector2(0, -40), Layers.World);
        return GetWorld2D().DirectSpaceState.IntersectRay(q).Count == 0;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        T += dt; _alertCd -= dt; BarT -= dt;
        Target ??= GetTree().GetFirstNodeInGroup("player") as Player;
        var v = Velocity;
        if (!Flying) v.Y = Mathf.Min(v.Y + Gravity * dt, 540);
        bool sees = Sees(out float dx, out float dy);
        if (sees) LastSeen = Target.GlobalPosition;

        if (!Think(dt, ref v, dx, dy, sees))
        {
            switch (State)
            {
                case EState.Patrol:
                    if (GlobalPosition.X < HomeX - PatrolRange) PatrolDir = 1;
                    else if (GlobalPosition.X > HomeX + PatrolRange) PatrolDir = -1;
                    if (!Flying && IsOnWall()) PatrolDir = -PatrolDir;
                    if (!Flying && IsOnFloor() && !GroundAhead(PatrolDir)) PatrolDir = -PatrolDir;
                    SetFacing(PatrolDir);
                    v.X = PatrolDir * PatrolSpeed;
                    Play("walk");
                    if (sees) Alert();
                    break;
                case EState.Chase:
                    SetFacing(MathF.Sign(dx));
                    if (!sees) { Enter(EState.Search, "walk"); break; }
                    if (MathF.Abs(dx) <= AttackRange && MathF.Abs(dy) < 50) { v.X = 0; StartAttack(); break; }
                    v.X = Flying || GroundAhead(Facing) ? MathF.Sign(dx) * ChaseSpeed : 0;
                    Play(MathF.Abs(v.X) > 1 ? "walk" : "idle");
                    break;
                case EState.Search:
                {
                    float sx = LastSeen.X - GlobalPosition.X;
                    if (sees) { Enter(EState.Chase, "walk"); break; }
                    if (T > 3.5f) { Enter(EState.Patrol, "walk"); break; }
                    if (MathF.Abs(sx) > 8 && (Flying || GroundAhead(MathF.Sign(sx)))) { SetFacing(MathF.Sign(sx)); v.X = MathF.Sign(sx) * PatrolSpeed * 1.3f; Play("walk"); }
                    else { v.X = 0; Play("idle"); if ((int)(T * 1.2f) % 2 == 1) SetFacing(-Facing); }
                    break;
                }
                case EState.Windup: v.X = Mathf.MoveToward(v.X, 0, 600 * dt); break;
                case EState.Attack:
                    v.X = Spr.Frame == ActiveFrames[0] ? Facing * AttackLunge : Mathf.MoveToward(v.X, 0, 900 * dt);
                    break;
                case EState.Recover: case EState.Hurt: case EState.Stagger: case EState.Dead: case EState.Executed:
                    v.X = Mathf.MoveToward(v.X, 0, 600 * dt);
                    if (State == EState.Hurt && T > 0.28f) Enter(EState.Chase, "walk");
                    if (State == EState.Stagger && T > StaggerTime) Enter(EState.Chase, "walk");
                    break;
            }
        }

        Velocity = v;
        MoveAndSlide();
        FlashT = Mathf.Max(0, FlashT - dt * 7);
        Flash.SetShaderParameter("flash", FlashT);
        if (State is EState.Attack && !HitDone && Array.IndexOf(ActiveFrames, Spr.Frame) >= 0) TryMeleeHit();
        if (BarT > 0 || State == EState.Stagger) QueueRedraw();
    }

    /// <summary>Custom behaviour. Return true to skip the default state logic this frame.</summary>
    protected virtual bool Think(float dt, ref Vector2 v, float dx, float dy, bool sees) => false;

    protected void Play(string anim) { if (Spr.Animation != anim) Spr.Play(anim); }

    protected bool GroundAhead(int dir)
    {
        var from = GlobalPosition + new Vector2(dir * (BodyRadius + 8), -8);
        var q = PhysicsRayQueryParameters2D.Create(from, from + new Vector2(0, 40), Layers.World | Layers.OneWay);
        return GetWorld2D().DirectSpaceState.IntersectRay(q).Count > 0;
    }

    protected virtual void StartAttack() { HitDone = false; Enter(EState.Windup, "windup"); }

    /// <summary>Wakes this enemy and calls allies within 170 px (coordinate / call for assistance).</summary>
    public void Alert(bool spread = true)
    {
        if (State is EState.Dead or EState.Executed) return;
        if (State is EState.Patrol or EState.Search) { Enter(EState.Chase, "walk"); Effects.Exclaim(this); }
        if (!spread || _alertCd > 0) return;
        _alertCd = 4;
        foreach (var n in GetTree().GetNodesInGroup("enemies"))
            if (n is EnemyBase e && e != this && e.GlobalPosition.DistanceTo(GlobalPosition) < 170) e.Alert(false);
    }

    protected virtual void TryMeleeHit()
    {
        foreach (var body in Hitbox.GetOverlappingBodies())
        {
            if (body is not Player p) continue;
            HitDone = true;
            p.TakeHit(new HitInfo { Damage = AttackDamage, Attacker = this, Knockback = 200, Parryable = true });
            return;
        }
    }

    protected virtual void OnFrame(string anim, int frame)
    {
        if (anim == "attack" && frame == ActiveFrames[0]) Sfx.Swing(this, 0.75f);
    }

    protected virtual void OnAnimFinished()
    {
        switch (State)
        {
            case EState.Windup: HitDone = false; Enter(EState.Attack, "attack"); break;
            case EState.Attack: Enter(EState.Recover, "recover"); break;
            case EState.Recover: Enter(EState.Chase, "walk"); break;
            case EState.Dead:
            {
                var tw = CreateTween();
                tw.TweenInterval(2.5);
                tw.TweenProperty(this, "modulate:a", 0f, 1.2);
                tw.TweenCallback(Callable.From(QueueFree));
                break;
            }
        }
    }

    public virtual void OnParried()
    {
        if (State is EState.Dead or EState.Executed) return;
        Enter(EState.Stagger, "stagger");
        Velocity = new Vector2(-Facing * 120, Velocity.Y);
    }

    /// <summary>Test hook: cot call Actors/Boss DebugDamage 400</summary>
    public void DebugDamage(int amount) => TakeHit(new HitInfo { Damage = amount });

    public void BeginExecution(Player p) { if (State == EState.Stagger) { State = EState.Executed; T = 0; Spr.Pause(); } }

    public virtual HitResult TakeHit(in HitInfo hit)
    {
        if (State == EState.Dead) return HitResult.Ignored;
        int dmg = State == EState.Stagger ? hit.Damage * 2 : hit.Damage;
        if (hit.Execution) dmg = Math.Max(dmg, hit.Damage);
        Hp = Math.Max(0, Hp - dmg);
        FlashT = 1; BarT = 3;
        Sfx.Hit(this, hit.Heavy);
        Effects.DamageNumber(GetParent(), GlobalPosition + new Vector2(0, -BodyHeight - 10), dmg, hit.Execution);
        int dir = hit.Attacker != null ? MathF.Sign(GlobalPosition.X - hit.Attacker.GlobalPosition.X) : 1;
        if (State is EState.Patrol or EState.Search) Alert();
        if (Hp <= 0) { Die(dir); return HitResult.Killed; }
        if (State == EState.Executed) { Enter(EState.Hurt, "hurt"); return HitResult.Hit; }
        bool interrupt = State switch
        {
            EState.Attack => !HasPoise,
            EState.Windup => hit.Heavy || !HasPoise,
            EState.Stagger => false,
            EState.Special => false,
            _ => true,
        };
        if (hit.GuardBreak && State is EState.Windup or EState.Attack) { Enter(EState.Stagger, "stagger"); return HitResult.Hit; }
        if (interrupt)
        {
            Enter(EState.Hurt, "hurt");
            Velocity = new Vector2(dir * hit.Knockback * (Flying ? 0.8f : 0.5f), Flying ? 0 : -40);
            SetFacing(-dir);
        }
        else Velocity = new Vector2(Velocity.X + dir * 20, Velocity.Y);
        return HitResult.Hit;
    }

    protected virtual void Die(int dir)
    {
        Enter(EState.Dead, "death");
        CollisionLayer = 0;
        Velocity = new Vector2(dir * 90, Flying ? 0 : -60);
        Loot.SpawnAshes(GetParent(), GlobalPosition + new Vector2(0, -BodyHeight * 0.5f), AshDrop);
        Died?.Invoke(this);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (State == EState.Dead) return;
        if (State == EState.Stagger)
        {
            // execution prompt: a pulsing gold ring
            float p = (MathF.Sin(T * 10) + 1) * 0.5f;
            DrawArc(new Vector2(0, -BodyHeight - 18), 5 + p * 2, 0, MathF.Tau, 16, new Color(1f, 0.8f, 0.3f, 0.8f), 1);
        }
        if (BarT <= 0 || this is Abbot) return;
        float w = 30, y = -BodyHeight - 10;
        DrawRect(new Rect2(-w / 2 - 1, y - 1, w + 2, 4), new Color(0, 0, 0, 0.7f));
        DrawRect(new Rect2(-w / 2, y, w * Hp / MaxHp, 2), new Color(0.75f, 0.15f, 0.12f));
    }
}
