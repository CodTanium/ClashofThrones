using Godot;
using System;
using System.Collections.Generic;
using ClashOfThrones.Art;
using ClashOfThrones.Core;
using ClashOfThrones.World;

namespace ClashOfThrones.Actors;

/// <summary>
/// The knight. Movement: walk/run/sprint, variable jump, coyote + buffer, double jump (Wings of Ash), wall slide + wall jump,
/// ledge grab + climb, ladders, drop-through, roll, air dash, plunge. Combat: light combo, running/aerial attacks,
/// heavy + charged attack, block / perfect-block parry, shield bash, executions, Wrath, healing flasks, stamina.
/// Tuning constants are at the top.
/// </summary>
public partial class Player : CharacterBody2D, IDamageable
{
    // ---- movement (pixels, seconds) ----
    const float WalkSpeed = 70, RunSpeed = 150, SprintSpeed = 215, Accel = 1300, Decel = 1700, AirAccel = 950;
    const float Gravity = 1150, FallMul = 1.55f, MaxFall = 540, JumpVelocity = 400, DoubleJumpVelocity = 350, JumpCut = 0.45f;
    const float CoyoteTime = 0.1f, BufferTime = 0.13f, WallSlideSpeed = 70, WallJumpX = 200, WallJumpY = 380, LadderSpeed = 95;
    const float RollTime = 0.38f, RollSpeed = 300, DashTime = 0.17f, DashSpeed = 340, SprintHold = 0.22f;
    // ---- combat ----
    const float ParryWindow = 0.2f, HurtTime = 0.32f, HurtIFrames = 0.9f, ChargeMax = 1.0f, HealTime = 0.8f, WrathTime = 10f;
    const float StaminaRegen = 48, StaminaDelay = 0.55f;
    const float CostLight = 10, CostHeavy = 20, CostCharged = 30, CostRoll = 22, CostDash = 16, CostSprint = 18, CostBash = 14;

    enum State { Move, Attack, RunAttack, AirAttack, Plunge, Heavy, Roll, AirDash, Block, Bash, Hurt, Stagger, Dead, WallSlide, LedgeHang, LedgeClimb, Ladder, Heal, Execute }

    public int MaxHp { get; private set; } = 6;
    public int Hp { get; private set; } = 6;
    public float MaxStamina { get; private set; } = 100;
    public float Stamina { get; private set; } = 100;
    public float Wrath { get; private set; }
    public bool WrathActive => _wrathT > 0;
    public float WrathTimeLeft01 => _wrathT / WrathTime;
    public int Flasks => Run?.Flasks ?? 0;
    public int FlaskMax => Run?.FlaskMax ?? 0;
    public int Facing { get; private set; } = 1;
    public bool IsDead => _state == State.Dead;
    public string StateName => _state.ToString();
    public Vector2 LastSafe { get; private set; }
    public Interactable FocusedInteractable { get; private set; }

    public event Action Died;

    static SaveData Run => Game.I?.Run;

    AnimatedSprite2D _spr;
    ShaderMaterial _flash;
    Area2D _hitbox, _sensor;
    CollisionShape2D _hitShape;
    CpuParticles2D _wrathFx;
    State _state = State.Move;
    float _t, _coyote, _buffer, _iframes, _flashT, _landT, _staminaWait, _dodgeHeld, _wrathT, _noLedge, _dropT, _wallLock, _charge;
    int _combo, _airJumps, _airAttacks, _hitCount, _wallDir;
    bool _queued, _jumpCut, _wasOnFloor, _dashUsed, _sprinting, _dodgeArmed, _healDone, _execHit, _plungeLanded, _blocking;
    float _prevVy;
    Vector2 _ledgeTarget, _hangPos;
    Node2D _execTarget;
    readonly HashSet<Node> _hitSet = new();

    // attack table: active frames, damage, knockback, hitbox (x offset, y offset, w, h)
    static readonly Dictionary<string, (int from, int to, int dmg, float knock, Vector4 box)> Attacks = new()
    {
        ["attack1"] = (2, 3, 10, 120, new(38, -50, 56, 40)), ["attack2"] = (2, 3, 10, 140, new(36, -62, 56, 56)),
        ["attack3"] = (3, 4, 18, 260, new(40, -44, 64, 44)), ["run_attack"] = (2, 3, 16, 220, new(42, -46, 64, 40)),
        ["air_attack"] = (1, 2, 10, 120, new(30, -44, 62, 56)), ["heavy"] = (3, 4, 24, 280, new(42, -48, 70, 50)),
        ["bash"] = (1, 2, 6, 260, new(24, -50, 30, 44)),
    };

    public override void _Ready()
    {
        AddToGroup("player");
        CollisionLayer = Layers.Player; CollisionMask = Layers.World | Layers.OneWay;
        FloorSnapLength = 6; FloorMaxAngle = Mathf.DegToRad(50);
        AddChild(new CollisionShape2D { Shape = new CapsuleShape2D { Radius = 10, Height = 62 }, Position = new Vector2(0, -31) });

        _flash = FlashMaterial.Create();
        _spr = new AnimatedSprite2D { SpriteFrames = ArtLibrary.HeroFrames(), Centered = false, Offset = -ArtLibrary.HeroOrigin, Material = _flash };
        AddChild(_spr);
        _spr.AnimationFinished += OnAnimFinished;
        _spr.FrameChanged += OnFrameChanged;
        _spr.Play("idle");

        _hitbox = new Area2D { CollisionLayer = 0, CollisionMask = Layers.Enemy, Monitorable = false };
        _hitShape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(56, 40) } };
        _hitbox.AddChild(_hitShape);
        AddChild(_hitbox);
        _sensor = new Area2D { CollisionLayer = 0, CollisionMask = Layers.Interact | Layers.Hazard, Monitorable = false };
        _sensor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(18, 60) }, Position = new Vector2(0, -32) });
        AddChild(_sensor);

        _wrathFx = new CpuParticles2D
        {
            Emitting = false, Amount = 24, Lifetime = 0.7, Texture = ArtLibrary.Get("dust"), EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(12, 30), Position = new Vector2(0, -36), Direction = Vector2.Up, Spread = 20, Gravity = new Vector2(0, -60),
            InitialVelocityMin = 10, InitialVelocityMax = 40, ScaleAmountMin = 1, ScaleAmountMax = 2, Color = new Color(1f, 0.55f, 0.2f),
        };
        AddChild(_wrathFx);
        RefreshStats();
        Hp = MaxHp; Stamina = MaxStamina;
        LastSafe = GlobalPosition;
    }

    /// <summary>Recalculates max health / stamina from the save (level-ups, relics).</summary>
    public void RefreshStats()
    {
        var r = Run;
        MaxHp = (r?.MaxHp ?? 6) + (r != null && r.Equipped.Contains("tallow_heart") ? 2 : 0);
        MaxStamina = r?.MaxStamina ?? 100;
        Hp = Math.Min(Hp, MaxHp); Stamina = Math.Min(Stamina, MaxStamina);
    }

    bool HasRelic(string id) => Run?.Equipped.Contains(id) ?? false;

    void Play(string anim, float speed = 1f)
    {
        _spr.SpeedScale = speed;
        if (_spr.Animation != anim || !_spr.IsPlaying()) _spr.Play(anim);
    }
    void Restart(string anim, float speed = 1f) { _spr.SpeedScale = speed; _spr.Play(anim); _spr.Frame = 0; }

    void SetFacing(int f)
    {
        if (f == 0 || f == Facing) return;
        Facing = f; _spr.FlipH = f < 0;
    }

    void SetHitbox(Vector4 b) { _hitShape.Position = new Vector2(b.X * Facing, b.Y); ((RectangleShape2D)_hitShape.Shape).Size = new Vector2(b.Z, b.W); }

    bool Spend(float cost)
    {
        if (Stamina <= 0) return false;
        Stamina -= cost; _staminaWait = StaminaDelay;
        return true;
    }

    // =====================================================================================================
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _t += dt;
        _iframes -= dt; _landT -= dt; _noLedge -= dt; _wallLock -= dt;
        if (_dropT > 0) { _dropT -= dt; if (_dropT <= 0) CollisionMask |= Layers.OneWay; }
        if (_wrathT > 0) { _wrathT -= dt; if (_wrathT <= 0) { _wrathFx.Emitting = false; _flash.SetShaderParameter("tint", new Color(0, 0, 0, 0)); } }

        bool onFloor = IsOnFloor();
        if (onFloor) { _airJumps = 0; _dashUsed = false; _airAttacks = 0; }
        _coyote = onFloor ? CoyoteTime : _coyote - dt;
        _buffer = Input.IsActionJustPressed(InputSetup.Jump) ? BufferTime : _buffer - dt;
        float axis = Input.GetAxis(InputSetup.Left, InputSetup.Right);
        float axisY = Input.GetAxis(InputSetup.Up, InputSetup.Down);
        var v = Velocity;

        bool gravity = _state is not (State.LedgeHang or State.LedgeClimb or State.Ladder or State.AirDash or State.Execute);
        if (gravity) v.Y = Mathf.Min(v.Y + Gravity * (v.Y > 0 ? FallMul : 1f) * dt, MaxFall);

        UpdateStamina(dt);
        UpdateInteract();

        switch (_state)
        {
            case State.Move: v = Move(v, axis, axisY, onFloor, dt); break;
            case State.Attack: case State.RunAttack: case State.Heavy: case State.Bash: v = Attacking(v, onFloor, dt); break;
            case State.AirAttack:
                v.X = Mathf.MoveToward(v.X, axis * RunSpeed * 0.7f, AirAccel * dt);
                v.Y = Mathf.Min(v.Y, 60);
                DoActiveHits("air_attack");
                if (onFloor) EndAction();
                break;
            case State.Plunge:
                v.X = 0; v.Y = 560;
                if (onFloor && !_plungeLanded) PlungeLand();
                break;
            case State.Roll:
            {
                float k = _t / RollTime;
                v.X = Facing * Mathf.Lerp(RollSpeed, 70, k * k);
                if (_t >= RollTime) EndAction();
                break;
            }
            case State.AirDash:
                v = new Vector2(Facing * DashSpeed, 0);
                if (_t >= DashTime) { v.X *= 0.5f; EndAction(); }
                break;
            case State.Block:
                v.X = Mathf.MoveToward(v.X, 0, Decel * dt);
                if (!Input.IsActionPressed(InputSetup.Parry)) EndAction();
                else if (Input.IsActionJustPressed(InputSetup.Attack) && Spend(CostBash)) StartAttack("bash", State.Bash);
                break;
            case State.Hurt:
                v.X = Mathf.MoveToward(v.X, 0, 600 * dt);
                if (_t >= HurtTime) EndAction();
                break;
            case State.Stagger:
                v.X = Mathf.MoveToward(v.X, 0, 600 * dt);
                if (_t >= 0.8f) EndAction();
                break;
            case State.Dead: v.X = Mathf.MoveToward(v.X, 0, 800 * dt); break;
            case State.WallSlide: v = WallSlide(v, axis, onFloor); break;
            case State.LedgeHang:
                v = Vector2.Zero;
                if (Input.IsActionJustPressed(InputSetup.Jump) || axisY < -0.5f) { Enter(State.LedgeClimb); Restart("ledge_climb"); }
                else if (axisY > 0.5f) { _noLedge = 0.35f; EndAction(); }
                break;
            case State.LedgeClimb:
                v = Vector2.Zero;
                {
                    float k = Mathf.Clamp(_t / 0.36f, 0, 1);
                    var mid = new Vector2(_hangPos.X, _ledgeTarget.Y);
                    GlobalPosition = k < 0.65f ? _hangPos.Lerp(mid, k / 0.65f) : mid.Lerp(_ledgeTarget, (k - 0.65f) / 0.35f);
                    if (k >= 1) EndAction();
                }
                break;
            case State.Ladder: v = Ladder(v, axisY); break;
            case State.Heal:
                v.X = Mathf.MoveToward(v.X, axis * WalkSpeed * 0.5f, Accel * dt);
                if (!_healDone && _t >= HealTime * 0.6f)
                {
                    _healDone = true;
                    Hp = Math.Min(MaxHp, Hp + Math.Max(2, (int)MathF.Ceiling(MaxHp * 0.45f)));
                    Effects.Heal(GetParent(), GlobalPosition + new Vector2(0, -40));
                }
                if (_t >= HealTime) EndAction();
                break;
            case State.Execute:
                v = Vector2.Zero;
                if (!_execHit && _spr.Frame >= 4)
                {
                    _execHit = true;
                    if (_execTarget is IDamageable d && IsInstanceValid(_execTarget))
                        d.TakeHit(new HitInfo { Damage = Dmg(60), Attacker = this, Knockback = 200, Heavy = true, Execution = true });
                    Effects.Blood(GetParent(), _execTarget?.GlobalPosition + new Vector2(0, -40) ?? GlobalPosition, 2f);
                    Game.I.HitStop(0.18); Game.I.Shake(0.6f); GainWrath(0.3f);
                }
                break;
        }

        Velocity = v;
        _prevVy = v.Y;
        MoveAndSlide();

        bool nowFloor = IsOnFloor();
        if (nowFloor && !_wasOnFloor && _prevVy > 250 && _state == State.Move) { _landT = 0.11f; Restart("land"); Sfx.Land(this); }
        if (nowFloor && _state == State.Move && !OnHazardTile()) LastSafe = GlobalPosition;
        _wasOnFloor = nowFloor;

        _flashT = Mathf.Max(0, _flashT - dt * 6);
        _flash.SetShaderParameter("flash", _flashT);
        bool blink = _state != State.Dead && _state != State.Roll && _iframes > 0 && ((int)(_iframes * 20) % 2 == 0);
        _spr.Modulate = blink ? new Color(1, 1, 1, 0.45f) : Colors.White;
    }

    // ------------------------------------------------------------------ movement
    Vector2 Move(Vector2 v, float axis, float axisY, bool onFloor, float dt)
    {
        // dodge button: hold = sprint, tap (release before SprintHold) = roll, in the air = dash
        if (Input.IsActionJustPressed(InputSetup.Dodge)) { _dodgeHeld = 0; _dodgeArmed = true; if (!onFloor && !_dashUsed) { StartDash(axis); return v; } }
        if (Input.IsActionPressed(InputSetup.Dodge) && _dodgeArmed) _dodgeHeld += dt;
        _sprinting = onFloor && _dodgeArmed && _dodgeHeld >= SprintHold && MathF.Abs(axis) > 0.2f && Stamina > 0;
        if (Input.IsActionJustReleased(InputSetup.Dodge) && _dodgeArmed)
        {
            _dodgeArmed = false;
            if (_dodgeHeld < SprintHold && onFloor) { StartRoll(axis); return v; }
        }
        if (_sprinting) { Stamina -= CostSprint * (HasRelic("pilgrim_sandal") ? 0.7f : 1f) * dt; _staminaWait = StaminaDelay * 0.5f; }

        float target = MathF.Abs(axis) < 0.5f && MathF.Abs(axis) > 0.05f ? MathF.Sign(axis) * WalkSpeed : axis * (_sprinting ? SprintSpeed : RunSpeed);
        float accel = onFloor ? (MathF.Abs(axis) > 0.01f ? Accel : Decel) : AirAccel;
        if (_wallLock <= 0) v.X = Mathf.MoveToward(v.X, target, accel * dt);
        if (MathF.Abs(axis) > 0.2f && _wallLock <= 0) SetFacing(MathF.Sign(axis));

        // jump / drop-through / double jump
        if (_buffer > 0 && onFloor && axisY > 0.5f && StandingOnOneWay())
        {
            _buffer = 0; CollisionMask &= ~Layers.OneWay; _dropT = 0.25f; GlobalPosition += new Vector2(0, 2);
        }
        else if (_buffer > 0 && _coyote > 0) { v.Y = -JumpVelocity; _buffer = 0; _coyote = 0; _jumpCut = false; Restart("jump"); Sfx.Jump(this); }
        else if (_buffer > 0 && !onFloor && Run != null && Run.Has("wings_of_ash") && _airJumps < 1)
        {
            v.Y = -DoubleJumpVelocity; _buffer = 0; _airJumps++; _jumpCut = false; Restart("jump");
            Effects.Puff(GetParent(), GlobalPosition, 1.2f); Sfx.Jump(this, 1.3f);
        }
        if (!_jumpCut && v.Y < 0 && !Input.IsActionPressed(InputSetup.Jump)) { v.Y *= JumpCut; _jumpCut = true; }

        // ladders
        var ladder = CurrentLadder();
        if (ladder != null && (axisY < -0.5f || (axisY > 0.5f && !onFloor)))
        {
            Enter(State.Ladder); GlobalPosition = new Vector2(ladder.GlobalPosition.X, GlobalPosition.Y); CollisionMask &= ~Layers.OneWay;
            Play("climb"); return Vector2.Zero;
        }

        // actions
        if (Input.IsActionJustPressed(InputSetup.Attack))
        {
            var foe = ExecutableFoe();
            if (foe != null && onFloor) { StartExecute(foe); return Vector2.Zero; }
            if (!onFloor && axisY > 0.5f) { StartPlunge(); return v; }
            if (!onFloor && _airAttacks < 2 && Spend(CostLight * 0.6f)) { _airAttacks++; StartAttack("air_attack", State.AirAttack); return v; }
            if (onFloor && _sprinting && Spend(CostLight * 1.4f)) { StartAttack("run_attack", State.RunAttack); return v; }
            if (onFloor && Spend(CostLight)) { _combo = 1; StartAttack("attack1", State.Attack); return v; }
        }
        if (Input.IsActionJustPressed(InputSetup.Heavy) && onFloor) { Enter(State.Heavy); _charge = 0; _hitSet.Clear(); Restart("charge"); return v; }
        if (Input.IsActionJustPressed(InputSetup.Parry) && onFloor) { Enter(State.Block); _blocking = true; Restart("block"); Sfx.ShieldUp(this); return v; }
        if (Input.IsActionJustPressed(InputSetup.Heal) && onFloor) { StartHeal(); return v; }
        if (Input.IsActionJustPressed(InputSetup.Wrath) && Wrath >= 1f) ActivateWrath();
        if (Input.IsActionJustPressed(InputSetup.Interact) && FocusedInteractable != null && onFloor) { FocusedInteractable.Interact(this); return Vector2.Zero; }

        // walls + ledges (air only)
        if (!onFloor)
        {
            if (_noLedge <= 0 && v.Y >= 0 && TryLedge()) return Vector2.Zero;
            if (IsOnWall() && v.Y > 0 && MathF.Abs(axis) > 0.3f)
            {
                int wallDir = -MathF.Sign(GetWallNormal().X);
                if (wallDir == MathF.Sign(axis)) { _wallDir = wallDir; SetFacing(-wallDir); Enter(State.WallSlide); Play("wall_slide"); return v; }
            }
        }

        if (onFloor)
        {
            if (_landT > 0) { }
            else if (MathF.Abs(v.X) > 12) { Play("run", _sprinting ? 1.45f : MathF.Abs(v.X) < WalkSpeed + 5 ? 0.6f : 1f); }
            else Play("idle");
        }
        else Play(v.Y < 0 ? "jump" : "fall");
        return v;
    }

    Vector2 WallSlide(Vector2 v, float axis, bool onFloor)
    {
        v.Y = Mathf.Min(v.Y, WallSlideSpeed);
        v.X = _wallDir * 40;   // stay pressed against the wall so IsOnWall() keeps reporting contact
        if (onFloor || (!IsOnWall() && _t > 0.1f) || MathF.Sign(axis) != _wallDir) { EndAction(); return v; }
        if (IsOnWall()) _t = 0;
        if (Input.IsActionJustPressed(InputSetup.Jump))
        {
            v = new Vector2(-_wallDir * WallJumpX, -WallJumpY); _wallLock = 0.16f; _jumpCut = false;
            SetFacing(-_wallDir); EndAction(); Restart("jump"); Sfx.Jump(this, 1.1f);
            Effects.Puff(GetParent(), GlobalPosition + new Vector2(_wallDir * 10, -20), 0.8f);
        }
        if (Engine.GetPhysicsFrames() % 8 == 0) Effects.Puff(GetParent(), GlobalPosition + new Vector2(_wallDir * 10, -50), 0.4f);
        return v;
    }

    Vector2 Ladder(Vector2 v, float axisY)
    {
        var ladder = CurrentLadder();
        if (ladder == null) { CollisionMask |= Layers.OneWay; EndAction(); return axisY < 0 ? new Vector2(0, -170) : v; }
        if (Input.IsActionJustPressed(InputSetup.Jump)) { CollisionMask |= Layers.OneWay; EndAction(); return new Vector2(Input.GetAxis(InputSetup.Left, InputSetup.Right) * RunSpeed, -220); }
        v = new Vector2(0, axisY * LadderSpeed);
        _spr.SpeedScale = MathF.Abs(axisY) > 0.1f ? 1 : 0;
        if (axisY > 0.1f && IsOnFloor()) { CollisionMask |= Layers.OneWay; EndAction(); }
        return v;
    }

    bool TryLedge()
    {
        var space = GetWorld2D().DirectSpaceState;
        var p = GlobalPosition;
        var chest = PhysicsRayQueryParameters2D.Create(p + new Vector2(0, -52), p + new Vector2(Facing * 16, -52), Layers.World);
        var head = PhysicsRayQueryParameters2D.Create(p + new Vector2(0, -70), p + new Vector2(Facing * 16, -70), Layers.World);
        if (space.IntersectRay(chest).Count == 0 || space.IntersectRay(head).Count != 0) return false;
        var down = PhysicsRayQueryParameters2D.Create(p + new Vector2(Facing * 16, -72), p + new Vector2(Facing * 16, -50), Layers.World);
        var hit = space.IntersectRay(down);
        if (hit.Count == 0) return false;
        float top = ((Vector2)hit["position"]).Y;
        var wallHit = space.IntersectRay(chest);
        float wallX = ((Vector2)wallHit["position"]).X;
        GlobalPosition = new Vector2(wallX - Facing * 10, top + 90);
        _hangPos = GlobalPosition;
        _ledgeTarget = new Vector2(wallX + Facing * 12, top - 1);
        Enter(State.LedgeHang); Play("ledge_hang"); Sfx.Grab(this);
        return true;
    }

    bool StandingOnOneWay()
    {
        var q = PhysicsRayQueryParameters2D.Create(GlobalPosition + new Vector2(0, -2), GlobalPosition + new Vector2(0, 6), Layers.OneWay);
        return GetWorld2D().DirectSpaceState.IntersectRay(q).Count > 0;
    }

    bool OnHazardTile()
    {
        foreach (var a in _sensor.GetOverlappingAreas()) if (a is Hazard) return true;
        return false;
    }

    Ladder CurrentLadder()
    {
        foreach (var a in _sensor.GetOverlappingAreas()) if (a is Ladder l) return l;
        return null;
    }

    void UpdateInteract()
    {
        Interactable best = null;
        foreach (var a in _sensor.GetOverlappingAreas()) if (a is Interactable it && it.CanInteract) { best = it; break; }
        FocusedInteractable = best;
    }

    void UpdateStamina(float dt)
    {
        _staminaWait -= dt;
        if (_staminaWait <= 0 && !_sprinting) Stamina = Mathf.Min(MaxStamina, Stamina + StaminaRegen * (_state == State.Block ? 0.3f : 1f) * dt);
        Stamina = Mathf.Max(Stamina, -20);
    }

    // ------------------------------------------------------------------ actions
    void Enter(State s) { _state = s; _t = 0; if (s != State.Block) _blocking = false; }
    void EndAction() { Enter(State.Move); _spr.SpeedScale = 1; }

    void StartAttack(string anim, State s)
    {
        Enter(s); _queued = false; _hitSet.Clear();
        Restart(anim, WrathActive ? 1.15f : 1f);
        SetHitbox(Attacks[anim].box);
        float axis = Input.GetAxis(InputSetup.Left, InputSetup.Right);
        if (MathF.Abs(axis) > 0.2f && s != State.AirAttack) SetFacing(MathF.Sign(axis));
    }

    void StartRoll(float axis)
    {
        if (!Spend(CostRoll * (HasRelic("pilgrim_sandal") ? 0.7f : 1f))) return;
        if (MathF.Abs(axis) > 0.2f) SetFacing(MathF.Sign(axis));
        Enter(State.Roll); Restart("dodge"); Sfx.Roll(this);
    }

    void StartDash(float axis)
    {
        if (!Spend(CostDash)) return;
        if (MathF.Abs(axis) > 0.2f) SetFacing(MathF.Sign(axis));
        _dashUsed = true; Enter(State.AirDash); Restart("dodge", 1.6f); Sfx.Roll(this, 1.3f);
        Effects.Puff(GetParent(), GlobalPosition + new Vector2(-Facing * 10, -30), 1f);
    }

    void StartPlunge() { Enter(State.Plunge); _plungeLanded = false; _hitSet.Clear(); Restart("plunge"); }

    void PlungeLand()
    {
        _plungeLanded = true;
        Effects.Shockwave(GetParent(), GlobalPosition, 70);
        Game.I.Shake(0.5f); Sfx.Slam(this);
        foreach (var n in GetTree().GetNodesInGroup("enemies"))
            if (n is Node2D e && n is IDamageable d && e.GlobalPosition.DistanceTo(GlobalPosition) < 72 && MathF.Abs(e.GlobalPosition.Y - GlobalPosition.Y) < 40)
                if (d.TakeHit(new HitInfo { Damage = Dmg(20), Attacker = this, Knockback = 240, Heavy = true }) != HitResult.Ignored) OnHitLanded(false);
        _t = 0;
        GetTree().CreateTimer(0.28).Timeout += () => { if (_state == State.Plunge) EndAction(); };
    }

    void StartHeal()
    {
        if (Run == null || Run.Flasks <= 0) { Sfx.Denied(this); return; }
        Run.Flasks--; Enter(State.Heal); _healDone = false; Restart("heal"); Sfx.Drink(this);
    }

    void StartExecute(Node2D foe)
    {
        _execTarget = foe; _execHit = false;
        SetFacing(MathF.Sign(foe.GlobalPosition.X - GlobalPosition.X));
        Enter(State.Execute); Restart("execute");
        (foe as EnemyBase)?.BeginExecution(this);
    }

    Node2D ExecutableFoe()
    {
        foreach (var n in GetTree().GetNodesInGroup("enemies"))
            if (n is EnemyBase e && e.CanBeExecuted && e.GlobalPosition.DistanceTo(GlobalPosition) < 56 && MathF.Abs(e.GlobalPosition.Y - GlobalPosition.Y) < 30) return e;
        return null;
    }

    void ActivateWrath()
    {
        Wrath = 0; _wrathT = WrathTime; _wrathFx.Emitting = true;
        _flash.SetShaderParameter("tint", new Color(1f, 0.45f, 0.15f, 0.18f));
        Effects.Shockwave(GetParent(), GlobalPosition, 90);
        Game.I.Shake(0.5f); Game.I.HitStop(0.12); Sfx.Wrath(this);
        foreach (var n in GetTree().GetNodesInGroup("enemies"))
            if (n is Node2D e && n is IDamageable d && e.GlobalPosition.DistanceTo(GlobalPosition) < 90)
                d.TakeHit(new HitInfo { Damage = Dmg(14), Attacker = this, Knockback = 300, Heavy = true });
    }

    void GainWrath(float a) { if (!WrathActive) Wrath = Mathf.Min(1f, Wrath + a); }

    int Dmg(int baseDmg) => Mathf.RoundToInt(baseDmg * (Run?.DamageMul ?? 1f) * (WrathActive ? 1.5f : 1f));

    Vector2 Attacking(Vector2 v, bool onFloor, float dt)
    {
        string anim = _spr.Animation;
        if (_state == State.Heavy && anim == "charge")
        {
            v.X = Mathf.MoveToward(v.X, 0, Decel * dt);
            _charge = Mathf.Min(ChargeMax, _charge + dt);
            _flashT = _charge >= ChargeMax * 0.6f ? 0.25f + 0.25f * MathF.Sin(_t * 30) : _flashT;
            if (!Input.IsActionPressed(InputSetup.Heavy))
            {
                bool charged = _charge >= ChargeMax * 0.6f;
                if (!Spend(charged ? CostCharged : CostHeavy)) { EndAction(); return v; }
                StartAttack("heavy", State.Heavy);
                if (charged) _spr.SpeedScale = 1.2f;
            }
            return v;
        }
        if (!Attacks.TryGetValue(anim, out var a)) { EndAction(); return v; }
        bool active = _spr.Frame >= a.from && _spr.Frame <= a.to;
        float lunge = _state switch { State.RunAttack => 260, State.Heavy => 120, State.Bash => 160, _ => 80 };
        float target = active && onFloor ? Facing * lunge : 0;
        v.X = Mathf.MoveToward(v.X, target, Decel * 1.5f * dt);
        if (_state == State.Attack && Input.IsActionJustPressed(InputSetup.Attack) && _spr.Frame >= 1) _queued = true;
        if (Input.IsActionJustPressed(InputSetup.Dodge) && _spr.Frame >= a.to && onFloor) { StartRoll(Input.GetAxis(InputSetup.Left, InputSetup.Right)); return v; }
        DoActiveHits(anim);
        return v;
    }

    void DoActiveHits(string anim)
    {
        if (!Attacks.TryGetValue(anim, out var a) || _spr.Animation != anim) return;
        if (_spr.Frame < a.from || _spr.Frame > a.to) return;
        bool charged = anim == "heavy" && _charge >= ChargeMax * 0.6f;
        int dmg = charged ? 40 : a.dmg;
        foreach (var body in _hitbox.GetOverlappingBodies())
        {
            if (body == this || _hitSet.Contains(body) || body is not IDamageable target) continue;
            _hitSet.Add(body);
            var hit = new HitInfo
            {
                Damage = Dmg(dmg), Attacker = this, Knockback = charged ? 360 : a.knock,
                Heavy = anim is "heavy" or "attack3" or "bash" || charged, GuardBreak = charged || anim == "bash",
            };
            var res = target.TakeHit(hit);
            if (res == HitResult.Ignored) continue;
            var at = ((Node2D)body).GlobalPosition + new Vector2(-Facing * 10, -48);
            bool heavy = hit.Heavy;
            Effects.Spark(GetParent(), at, heavy ? 1.6f : 1f);
            if (res != HitResult.Blocked) Effects.Blood(GetParent(), at, heavy ? 1.3f : 0.8f);
            Game.I.HitStop(heavy ? 0.1 : 0.055);
            Game.I.Shake(heavy ? 0.45f : 0.22f);
            OnHitLanded(heavy);
        }
    }

    void OnHitLanded(bool heavy)
    {
        GainWrath(heavy ? 0.1f : 0.06f);
        _hitCount++;
        if (HasRelic("thirsting_blade") && _hitCount % 8 == 0) Hp = Math.Min(MaxHp, Hp + 1);
        if (WrathActive && _hitCount % 3 == 0) Hp = Math.Min(MaxHp, Hp + 1);
    }

    void OnAnimFinished()
    {
        switch (_state)
        {
            case State.Attack:
                if (_queued && _combo < 3 && Spend(CostLight)) { _combo++; StartAttack("attack" + _combo, State.Attack); }
                else EndAction();
                break;
            case State.RunAttack: case State.Bash: case State.AirAttack: case State.Execute: EndAction(); break;
            case State.Heavy: if (_spr.Animation == "heavy") EndAction(); break;
            case State.Dead: Died?.Invoke(); break;
        }
    }

    void OnFrameChanged()
    {
        if (Attacks.TryGetValue(_spr.Animation, out var a) && _spr.Frame == a.from) Sfx.Swing(this, _spr.Animation == "heavy" ? 0.75f : 1f);
        if (_spr.Animation == "run" && (_spr.Frame == 1 || _spr.Frame == 5)) Sfx.Step(this);
    }

    // ------------------------------------------------------------------ damage
    public HitResult TakeHit(in HitInfo hit)
    {
        if (_state == State.Dead) return HitResult.Ignored;
        if (hit.Hazard)
        {
            if (_iframes > 0 && hit.Damage < 99) return HitResult.Ignored;
            Damage(hit.Damage, 0);
            if (_state != State.Dead) { GlobalPosition = LastSafe; Velocity = Vector2.Zero; EndAction(); _iframes = 1f; }
            return HitResult.Hit;
        }
        if (_iframes > 0 || _state is State.Execute or State.LedgeClimb) return HitResult.Ignored;
        if (_state == State.Roll && _t >= 0.04f && _t <= 0.3f) return HitResult.Ignored;
        if (_state == State.AirDash) return HitResult.Ignored;
        int dir = hit.Attacker != null ? MathF.Sign(hit.Attacker.GlobalPosition.X - GlobalPosition.X) : -Facing;
        if (dir == 0) dir = Facing;

        if (_state == State.Block && dir == Facing && !hit.Unblockable)
        {
            if (_t <= ParryWindow && hit.Parryable)
            {
                (hit.Attacker as IParryable)?.OnParried();
                Effects.Spark(GetParent(), GlobalPosition + new Vector2(Facing * 22, -52), 2.2f);
                Game.I.HitStop(0.14); Game.I.Shake(0.4f); GainWrath(0.15f); Sfx.Parry(this);
                Velocity = new Vector2(-Facing * 60, Velocity.Y);
                return HitResult.Parried;
            }
            float cost = hit.Damage * 18f * (HasRelic("iron_vow") ? 0.5f : 1f);
            Stamina -= cost; _staminaWait = StaminaDelay;
            Effects.Spark(GetParent(), GlobalPosition + new Vector2(Facing * 20, -50), 1f); Sfx.Block(this);
            Velocity = new Vector2(-Facing * 120, Velocity.Y);
            if (Stamina <= 0 || hit.GuardBreak) { Enter(State.Stagger); Restart("hurt"); Sfx.GuardBreak(this); }
            return HitResult.Blocked;
        }

        Damage(hit.Damage, dir, hit.Knockback);
        return _state == State.Dead ? HitResult.Killed : HitResult.Hit;
    }

    void Damage(int amount, int dir, float knock = 170)
    {
        int dmg = Math.Max(1, Mathf.RoundToInt(amount * Settings.Current.EnemyDamageMul));
        Hp = Math.Max(0, Hp - dmg);
        _flashT = 1; GainWrath(0.1f);
        Game.I.HitStop(0.08); Game.I.Shake(0.5f); Sfx.Hurt(this);
        Effects.Blood(GetParent(), GlobalPosition + new Vector2(0, -40), 1f);
        if (Hp <= 0)
        {
            Enter(State.Dead); Restart("death");
            Velocity = new Vector2(-dir * 120, -120);
            CollisionMask |= Layers.OneWay;
            return;
        }
        if (_state is State.Ladder or State.LedgeHang or State.WallSlide) CollisionMask |= Layers.OneWay;
        Enter(State.Hurt); Restart("hurt");
        if (dir != 0) SetFacing(dir);
        Velocity = new Vector2(-dir * (knock > 0 ? knock : 170), -150);
        _iframes = HurtIFrames;
    }

    /// <summary>Test hook (cot call Player DebugDamage 6): damage without an attacker.</summary>
    public void DebugDamage(int amount) { _iframes = 0; Damage(amount, -Facing); }

    public void Heal() { Hp = MaxHp; Stamina = MaxStamina; }

    public void Respawn(Vector2 at)
    {
        GlobalPosition = at; Velocity = Vector2.Zero; CollisionMask = Layers.World | Layers.OneWay;
        Enter(State.Move); _iframes = 1.0f; _flashT = 0; Wrath = 0; _wrathT = 0; _wrathFx.Emitting = false;
        _flash.SetShaderParameter("tint", new Color(0, 0, 0, 0));
        RefreshStats(); Heal(); SetFacing(1); Restart("idle");
        LastSafe = at;
    }
}
