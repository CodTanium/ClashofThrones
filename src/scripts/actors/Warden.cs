using Godot;
using System;
using ClashOfThrones.Art;
using ClashOfThrones.Core;
using ClashOfThrones.World;

namespace ClashOfThrones.Actors;

/// <summary>
/// Grave Warden: a slow, heavy melee enemy. Patrols, chases, telegraphs a big cleaver swing (bright rim on the last
/// windup frame), keeps its poise during the swing, and is staggered for a long time when the swing is parried.
/// </summary>
public partial class Warden : CharacterBody2D, IDamageable, IParryable
{
    const float Gravity = 1150, PatrolSpeed = 28, ChaseSpeed = 46, SightRange = 210, AttackRange = 62, PatrolRange = 70;
    const float StaggerTime = 1.5f, HurtTime = 0.28f;

    enum State { Patrol, Chase, Windup, Attack, Recover, Hurt, Stagger, Dead }

    public int MaxHp = 7;
    public int Hp { get; private set; }
    public string StateName => _state.ToString();
    public bool IsDead => _state == State.Dead;

    AnimatedSprite2D _spr;
    ShaderMaterial _flash;
    Area2D _hitbox;
    CollisionShape2D _hitShape;
    State _state = State.Patrol;
    float _t, _flashT, _homeX;
    int _facing = -1, _patrolDir = -1;
    bool _hitDone;
    Player _player;

    public override void _Ready()
    {
        AddToGroup("enemies");
        Hp = MaxHp;
        _homeX = GlobalPosition.X;
        CollisionLayer = Layers.Enemy; CollisionMask = Layers.World;
        FloorSnapLength = 6;
        AddChild(new CollisionShape2D { Shape = new CapsuleShape2D { Radius = 14, Height = 70 }, Position = new Vector2(0, -35) });

        _flash = FlashMaterial.Create();
        _spr = new AnimatedSprite2D { SpriteFrames = ArtLibrary.WardenFrames(), Centered = false, Offset = -ArtLibrary.WardenOrigin, Material = _flash };
        AddChild(_spr);
        _spr.AnimationFinished += OnAnimFinished;
        _spr.Play("idle");

        _hitbox = new Area2D { CollisionLayer = 0, CollisionMask = Layers.Player, Monitorable = false };
        _hitShape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(64, 56) } };
        _hitbox.AddChild(_hitShape);
        AddChild(_hitbox);
        SetFacing(-1, true);
    }

    void SetFacing(int f, bool force = false)
    {
        if (f == 0 || (f == _facing && !force)) return;
        _facing = f; _spr.FlipH = f < 0; _hitShape.Position = new Vector2(40 * f, -44);
    }

    void Enter(State s, string anim)
    {
        _state = s; _t = 0;
        if (anim != null) _spr.Play(anim);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _t += dt;
        _player ??= GetTree().GetFirstNodeInGroup("player") as Player;
        var v = Velocity;
        v.Y = Mathf.Min(v.Y + Gravity * dt, 540);
        float dx = _player != null ? _player.GlobalPosition.X - GlobalPosition.X : 9999;
        float dy = _player != null ? MathF.Abs(_player.GlobalPosition.Y - GlobalPosition.Y) : 9999;
        bool sees = _player != null && !_player.IsDead && MathF.Abs(dx) < SightRange && dy < 70;

        switch (_state)
        {
            case State.Patrol:
                if (GlobalPosition.X < _homeX - PatrolRange) _patrolDir = 1;
                else if (GlobalPosition.X > _homeX + PatrolRange) _patrolDir = -1;
                SetFacing(_patrolDir);
                v.X = _patrolDir * PatrolSpeed;
                _spr.Play("walk");
                if (sees) Enter(State.Chase, "walk");
                break;
            case State.Chase:
                SetFacing(MathF.Sign(dx));
                if (!sees && MathF.Abs(dx) > SightRange * 1.4f) { Enter(State.Patrol, "walk"); break; }
                if (MathF.Abs(dx) <= AttackRange) { v.X = 0; Enter(State.Windup, "windup"); break; }
                v.X = MathF.Sign(dx) * ChaseSpeed;
                break;
            case State.Windup:
                v.X = Mathf.MoveToward(v.X, 0, 600 * dt);
                break;
            case State.Attack:
                v.X = _spr.Frame == 1 ? _facing * 110 : Mathf.MoveToward(v.X, 0, 900 * dt);
                if (!_hitDone && (_spr.Frame == 1 || _spr.Frame == 2)) TryHit();
                break;
            case State.Recover:
                v.X = Mathf.MoveToward(v.X, 0, 900 * dt);
                break;
            case State.Hurt:
                v.X = Mathf.MoveToward(v.X, 0, 500 * dt);
                if (_t > HurtTime) Enter(State.Chase, "walk");
                break;
            case State.Stagger:
                v.X = Mathf.MoveToward(v.X, 0, 400 * dt);
                if (_t > StaggerTime) Enter(State.Chase, "walk");
                break;
            case State.Dead:
                v.X = Mathf.MoveToward(v.X, 0, 600 * dt);
                break;
        }

        Velocity = v;
        MoveAndSlide();
        _flashT = Mathf.Max(0, _flashT - dt * 7);
        _flash.SetShaderParameter("flash", _flashT);
    }

    void TryHit()
    {
        foreach (var body in _hitbox.GetOverlappingBodies())
        {
            if (body is not Player p) continue;
            _hitDone = true;
            var res = p.TakeHit(new HitInfo { Damage = 1, Attacker = this, Knockback = 200, Parryable = true });
            if (res == HitResult.Hit || res == HitResult.Killed) Sfx.Hit(this, true);
            return;
        }
    }

    void OnAnimFinished()
    {
        switch (_state)
        {
            case State.Windup: _hitDone = false; Enter(State.Attack, "attack"); Sfx.Swing(this, 0.7f); break;
            case State.Attack: Enter(State.Recover, "recover"); break;
            case State.Recover: Enter(State.Chase, "walk"); break;
            case State.Dead:
            {
                var tw = CreateTween();
                tw.TweenInterval(2.5);
                tw.TweenProperty(this, "modulate:a", 0f, 1.2);
                break;
            }
        }
    }

    public void OnParried()
    {
        if (_state == State.Dead) return;
        Enter(State.Stagger, "stagger");
        Velocity = new Vector2(-_facing * 120, Velocity.Y);
        Sfx.Parry(this);
    }

    public HitResult TakeHit(in HitInfo hit)
    {
        if (_state == State.Dead) return HitResult.Ignored;
        int dmg = _state == State.Stagger ? hit.Damage * 2 : hit.Damage;
        Hp = Math.Max(0, Hp - dmg);
        _flashT = 1;
        Sfx.Hit(this, hit.Heavy);
        int dir = hit.Attacker != null ? MathF.Sign(GlobalPosition.X - hit.Attacker.GlobalPosition.X) : 1;
        if (Hp <= 0)
        {
            Enter(State.Dead, "death");
            CollisionLayer = 0;
            Velocity = new Vector2(dir * 90, -60);
            return HitResult.Killed;
        }
        // poise: the swing itself is never interrupted, only a heavy hit breaks the windup
        bool interrupt = _state switch
        {
            State.Attack => false,
            State.Windup => hit.Heavy,
            State.Stagger => false,
            _ => true,
        };
        if (interrupt)
        {
            Enter(State.Hurt, "hurt");
            Velocity = new Vector2(dir * hit.Knockback * 0.5f, -40);
            SetFacing(-dir);
        }
        else Velocity = new Vector2(Velocity.X + dir * 20, Velocity.Y);
        return HitResult.Hit;
    }
}
