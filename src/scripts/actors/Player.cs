using Godot;
using System;
using System.Collections.Generic;
using ClashOfThrones.Art;
using ClashOfThrones.Core;
using ClashOfThrones.World;

namespace ClashOfThrones.Actors;

/// <summary>
/// The knight. Movement (acceleration, variable jump, coyote time, jump buffer), 3-hit combo, dodge with i-frames,
/// shield parry, hurt knockback and death. Everything tunable sits in the constants at the top.
/// </summary>
public partial class Player : CharacterBody2D, IDamageable
{
    // ---- movement tuning (pixels, seconds) ----
    const float RunSpeed = 150, Accel = 1300, Decel = 1700, AirAccel = 950;
    const float Gravity = 1150, FallMul = 1.55f, MaxFall = 540, JumpVelocity = 400, JumpCut = 0.45f;
    const float CoyoteTime = 0.1f, BufferTime = 0.13f;
    // ---- combat tuning ----
    const float DodgeTime = 0.34f, DodgeSpeed = 330, DodgeCooldown = 0.18f, IFrameStart = 0.02f, IFrameEnd = 0.28f;
    const float ParryWindow = 0.2f, HurtTime = 0.32f, HurtIFrames = 0.9f;

    enum State { Move, Attack, Dodge, Parry, Hurt, Dead }

    public int MaxHp { get; private set; } = 6;
    public int Hp { get; private set; } = 6;
    public int Facing { get; private set; } = 1;
    public bool IsDead => _state == State.Dead;
    public string StateName => _state.ToString();

    public event Action<int, int> HpChanged;
    public event Action Died;

    AnimatedSprite2D _spr;
    ShaderMaterial _flash;
    Area2D _hitbox;
    CollisionShape2D _hitShape;
    State _state = State.Move;
    float _t, _coyote, _buffer, _iframes, _dodgeCd, _flashT, _landT;
    int _combo;
    bool _queued, _jumpCut, _wasOnFloor;
    float _prevVy;
    readonly HashSet<Node> _hitSet = new();

    static readonly Dictionary<int, (int from, int to, int dmg, float knock, float lunge)> AttackData = new()
    {
        [1] = (2, 3, 1, 120, 70), [2] = (2, 3, 1, 140, 80), [3] = (3, 4, 2, 260, 140),
    };

    public override void _Ready()
    {
        AddToGroup("player");
        CollisionLayer = Layers.Player; CollisionMask = Layers.World;
        FloorSnapLength = 6; FloorMaxAngle = Mathf.DegToRad(50);
        AddChild(new CollisionShape2D { Shape = new CapsuleShape2D { Radius = 10, Height = 62 }, Position = new Vector2(0, -31) });

        _flash = FlashMaterial.Create();
        _spr = new AnimatedSprite2D
        {
            SpriteFrames = ArtLibrary.HeroFrames(), Centered = false, Offset = -ArtLibrary.HeroOrigin, Material = _flash,
        };
        AddChild(_spr);
        _spr.AnimationFinished += OnAnimFinished;
        _spr.FrameChanged += OnFrameChanged;
        _spr.Play("idle");

        _hitbox = new Area2D { CollisionLayer = 0, CollisionMask = Layers.Enemy, Monitoring = true, Monitorable = false };
        _hitShape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(56, 40) } };
        _hitbox.AddChild(_hitShape);
        AddChild(_hitbox);
        UpdateHitbox();
    }

    void UpdateHitbox() => _hitShape.Position = new Vector2(38 * Facing, -50);

    void Play(string anim)
    {
        if (_spr.Animation != anim || !_spr.IsPlaying()) _spr.Play(anim);
    }

    void SetFacing(int f)
    {
        if (f == 0 || f == Facing) return;
        Facing = f; _spr.FlipH = f < 0; UpdateHitbox();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _t += dt;
        _iframes -= dt; _dodgeCd -= dt; _landT -= dt;
        bool onFloor = IsOnFloor();
        _coyote = onFloor ? CoyoteTime : _coyote - dt;
        _buffer = Input.IsActionJustPressed(InputSetup.Jump) ? BufferTime : _buffer - dt;
        float axis = Input.GetAxis(InputSetup.Left, InputSetup.Right);
        var v = Velocity;

        v.Y = Mathf.Min(v.Y + Gravity * (v.Y > 0 ? FallMul : 1f) * dt, MaxFall);

        switch (_state)
        {
            case State.Move: v = Move(v, axis, onFloor, dt); break;
            case State.Attack:
            {
                var d = AttackData[_combo];
                bool active = _spr.Frame >= d.from && _spr.Frame <= d.to;
                float target = active && onFloor ? Facing * d.lunge : 0;
                v.X = Mathf.MoveToward(v.X, target, Decel * 1.5f * dt);
                if (Input.IsActionJustPressed(InputSetup.Attack) && _spr.Frame >= 1) _queued = true;
                if (Input.IsActionJustPressed(InputSetup.Dodge) && _spr.Frame >= d.to && onFloor) { StartDodge(axis); break; }
                if (active) DoHits(d.dmg, d.knock, _combo == 3);
                break;
            }
            case State.Dodge:
            {
                float k = _t / DodgeTime;
                v.X = Facing * Mathf.Lerp(DodgeSpeed, 60, k * k);
                if (_t >= DodgeTime) EndAction();
                break;
            }
            case State.Parry:
                v.X = Mathf.MoveToward(v.X, 0, Decel * dt);
                break;
            case State.Hurt:
                v.X = Mathf.MoveToward(v.X, 0, 600 * dt);
                if (_t >= HurtTime) EndAction();
                break;
            case State.Dead:
                v.X = Mathf.MoveToward(v.X, 0, 800 * dt);
                break;
        }

        Velocity = v;
        _prevVy = v.Y;
        MoveAndSlide();

        bool nowFloor = IsOnFloor();
        if (nowFloor && !_wasOnFloor && _prevVy > 250 && _state == State.Move) { _landT = 0.11f; _spr.Play("land"); }
        _wasOnFloor = nowFloor;

        // hit flash + i-frame blink
        _flashT = Mathf.Max(0, _flashT - dt * 6);
        _flash.SetShaderParameter("flash", _flashT);
        _spr.Modulate = _state != State.Dead && _state != State.Dodge && _iframes > 0 && ((int)(_iframes * 20) % 2 == 0) ? new Color(1, 1, 1, 0.45f) : Colors.White;
    }

    Vector2 Move(Vector2 v, float axis, bool onFloor, float dt)
    {
        float accel = onFloor ? (MathF.Abs(axis) > 0.01f ? Accel : Decel) : AirAccel;
        v.X = Mathf.MoveToward(v.X, axis * RunSpeed, accel * dt);
        if (MathF.Abs(axis) > 0.2f) SetFacing(MathF.Sign(axis));

        if (_buffer > 0 && _coyote > 0)
        {
            v.Y = -JumpVelocity; _buffer = 0; _coyote = 0; _jumpCut = false;
            _spr.Play("jump");
        }
        if (!_jumpCut && v.Y < 0 && !Input.IsActionPressed(InputSetup.Jump)) { v.Y *= JumpCut; _jumpCut = true; }

        if (Input.IsActionJustPressed(InputSetup.Attack)) { StartAttack(1); return v; }
        if (Input.IsActionJustPressed(InputSetup.Dodge) && onFloor && _dodgeCd <= 0) { StartDodge(axis); return v; }
        if (Input.IsActionJustPressed(InputSetup.Parry) && onFloor) { StartParry(); return v; }

        if (onFloor)
        {
            if (_landT > 0) { }
            else if (MathF.Abs(v.X) > 12) Play("run");
            else Play("idle");
        }
        else Play(v.Y < 0 ? "jump" : "fall");
        return v;
    }

    void Enter(State s) { _state = s; _t = 0; }

    void StartAttack(int n)
    {
        Enter(State.Attack); _combo = n; _queued = false; _hitSet.Clear();
        _spr.Play("attack" + n);
        float axis = Input.GetAxis(InputSetup.Left, InputSetup.Right);
        if (MathF.Abs(axis) > 0.2f) SetFacing(MathF.Sign(axis));
    }

    void StartDodge(float axis)
    {
        if (MathF.Abs(axis) > 0.2f) SetFacing(MathF.Sign(axis));
        Enter(State.Dodge); _spr.Play("dodge"); _dodgeCd = DodgeCooldown + DodgeTime;
        _iframes = 0; // dodge i-frames are time-windowed in TakeHit
    }

    void StartParry() { Enter(State.Parry); _spr.Play("parry"); }

    void EndAction() { Enter(State.Move); }

    void OnAnimFinished()
    {
        switch (_state)
        {
            case State.Attack:
                if (_queued && _combo < 3) StartAttack(_combo + 1); else EndAction();
                break;
            case State.Parry: EndAction(); break;
            case State.Dead: Died?.Invoke(); break;
        }
    }

    void OnFrameChanged()
    {
        if (_state == State.Attack && _spr.Frame == AttackData[_combo].from)
            Sfx.Swing(this);
    }

    void DoHits(int dmg, float knock, bool heavy)
    {
        foreach (var body in _hitbox.GetOverlappingBodies())
        {
            if (body == this || _hitSet.Contains(body) || body is not IDamageable target) continue;
            _hitSet.Add(body);
            var res = target.TakeHit(new HitInfo { Damage = dmg, Attacker = this, Knockback = knock, Heavy = heavy });
            if (res == HitResult.Ignored) continue;
            var at = ((Node2D)body).GlobalPosition + new Vector2(-Facing * 10, -48);
            Effects.Spark(GetParent(), at, heavy ? 1.6f : 1f);
            Game.I.HitStop(heavy ? 0.1 : 0.055);
            Game.I.Shake(heavy ? 0.45f : 0.22f);
        }
    }

    public HitResult TakeHit(in HitInfo hit)
    {
        if (_state == State.Dead || _iframes > 0) return HitResult.Ignored;
        if (_state == State.Dodge && _t >= IFrameStart && _t <= IFrameEnd) return HitResult.Ignored;
        int dir = hit.Attacker != null ? MathF.Sign(hit.Attacker.GlobalPosition.X - GlobalPosition.X) : -Facing;
        if (dir == 0) dir = Facing;

        if (_state == State.Parry && _t <= ParryWindow && hit.Parryable && dir == Facing)
        {
            (hit.Attacker as IParryable)?.OnParried();
            Effects.Spark(GetParent(), GlobalPosition + new Vector2(Facing * 22, -52), 2.2f);
            Game.I.HitStop(0.14); Game.I.Shake(0.4f);
            Velocity = new Vector2(-Facing * 60, Velocity.Y);
            return HitResult.Parried;
        }

        Hp = Math.Max(0, Hp - hit.Damage);
        HpChanged?.Invoke(Hp, MaxHp);
        _flashT = 1;
        Game.I.HitStop(0.08); Game.I.Shake(0.5f);
        if (Hp <= 0)
        {
            Enter(State.Dead); _spr.Play("death");
            Velocity = new Vector2(-dir * 120, -120);
            return HitResult.Killed;
        }
        Enter(State.Hurt); _spr.Play("hurt");
        SetFacing(dir);
        Velocity = new Vector2(-dir * (hit.Knockback > 0 ? hit.Knockback : 170), -150);
        _iframes = HurtIFrames;
        return HitResult.Hit;
    }

    /// <summary>Test hook (cot call Player DebugDamage 6): damage without an attacker.</summary>
    public void DebugDamage(int amount) { _iframes = 0; TakeHit(new HitInfo { Damage = amount }); }

    public void Heal() { Hp = MaxHp; HpChanged?.Invoke(Hp, MaxHp); }

    public void Respawn(Vector2 at)
    {
        GlobalPosition = at; Velocity = Vector2.Zero;
        Enter(State.Move); _iframes = 1.0f; _flashT = 0;
        Heal(); SetFacing(1); _spr.Play("idle");
    }
}
