using Godot;
using System;
using System.Collections.Generic;
using ClashOfThrones.Actors;
using ClashOfThrones.Art;

namespace ClashOfThrones.World;

/// <summary>
/// First playable scene: a gothic brick dungeon built in code. Parallax layers (far, colonnade, wall, fog, foreground),
/// candle lights, dust, two altars (checkpoints), two Grave Wardens, the knight, camera and HUD.
/// </summary>
public partial class Main : Node2D
{
    public static Main I { get; private set; }
    public const float LevelWidth = 2560, FloorY = 300;

    public Player Player { get; private set; }
    public CameraRig Camera { get; private set; }
    public Hud Hud { get; private set; }
    Vector2 _respawn = new(200, FloorY);
    readonly List<Vector2> _enemySpawns = new() { new(900, FloorY), new(2050, FloorY) };
    Node2D _actors;
    bool _respawning;

    public override void _Ready()
    {
        I = this;
        AddChild(new CanvasModulate { Color = new Color(0.44f, 0.41f, 0.52f) });
        BuildBackground();
        BuildGeometry();
        BuildProps();

        _actors = new Node2D { Name = "Actors", ZIndex = 5 };
        AddChild(_actors);
        SpawnEnemies();

        Player = new Player { Name = "Player", Position = _respawn, ZIndex = 10 };
        AddChild(Player);

        BuildForeground();

        Camera = new CameraRig
        {
            Name = "Camera", Target = Player, LimitLeft = 0, LimitRight = (int)LevelWidth, LimitTop = -120, LimitBottom = 380,
        };
        AddChild(Camera);
        Camera.MakeCurrent();
        Camera.Snap();
        AddDust();

        Hud = new Hud { Name = "Hud" };
        AddChild(Hud);
        Player.HpChanged += Hud.SetHealth;
        Player.Died += OnPlayerDied;
        Hud.SetHealth(Player.Hp, Player.MaxHp);
        Hud.Message("THE SUNKEN NAVE", 2.2f);
    }

    // ------------------------------------------------------------------ background
    void Layer(string tex, float scroll, int z, Vector2 offset = default, Vector2? autoscroll = null, float alpha = 1f)
    {
        var p = new Parallax2D { ScrollScale = new Vector2(scroll, 1f), RepeatSize = new Vector2(640, 0), RepeatTimes = 3, ZIndex = z };
        if (autoscroll.HasValue) p.Autoscroll = autoscroll.Value;
        p.AddChild(new Sprite2D { Texture = ArtLibrary.Get(tex), Centered = false, Position = offset, Modulate = new Color(1, 1, 1, alpha) });
        AddChild(p);
    }

    void BuildBackground()
    {
        Layer("layer_far", 0.12f, -50);
        Layer("layer_mid", 0.35f, -40);
        Layer("layer_wall", 0.62f, -30);
        // light shafts from the top-left through the arches (additive, unaffected by the dark ambient)
        var shafts = new Parallax2D { ScrollScale = new Vector2(0.62f, 1f), RepeatSize = new Vector2(640, 0), RepeatTimes = 3, ZIndex = -29 };
        foreach (int cx in new[] { 160, 480 })
        {
            var poly = new Polygon2D
            {
                Polygon = new[] { new Vector2(cx - 40, 90), new Vector2(cx + 10, 80), new Vector2(cx + 150, 300), new Vector2(cx + 40, 300) },
                VertexColors = new[] { new Color(1f, 0.9f, 0.75f, 0.16f), new Color(1f, 0.9f, 0.75f, 0.16f), new Color(1f, 0.85f, 0.7f, 0f), new Color(1f, 0.85f, 0.7f, 0f) },
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded },
            };
            shafts.AddChild(poly);
        }
        AddChild(shafts);
        Layer("fog", 0.8f, -25, new Vector2(0, 205), new Vector2(-6, 0), 0.8f);
    }

    // ------------------------------------------------------------------ collision + floor visuals
    void Solid(Rect2 r, string tex)
    {
        var body = new StaticBody2D { CollisionLayer = Layers.World, CollisionMask = 0 };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = r.Size }, Position = r.Position + r.Size / 2 });
        if (tex != null)
            body.AddChild(new Sprite2D
            {
                Texture = ArtLibrary.Get(tex), Centered = false, Position = r.Position, RegionEnabled = true,
                RegionRect = new Rect2(0, 0, r.Size), TextureRepeat = TextureRepeatEnum.Enabled,
            });
        AddChild(body);
    }

    void Ledge(float x, float y)
    {
        var body = new StaticBody2D { CollisionLayer = Layers.World, CollisionMask = 0, Position = new Vector2(x, y) };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(92, 8) }, Position = new Vector2(48, 4), OneWayCollision = true });
        body.AddChild(new Sprite2D { Texture = ArtLibrary.Get("ledge"), Centered = false });
        AddChild(body);
    }

    void BuildGeometry()
    {
        Solid(new Rect2(0, FloorY, 1080, 220), "floor");
        Solid(new Rect2(1080, FloorY - 32, 340, 252), "floor");
        Solid(new Rect2(1420, FloorY, LevelWidth - 1420, 220), "floor");
        Solid(new Rect2(-60, -500, 60, 1100), null);
        Solid(new Rect2(LevelWidth, -500, 60, 1100), null);
        Ledge(560, 226); Ledge(720, 186); Ledge(1760, 232); Ledge(1900, 194);
    }

    void BuildProps()
    {
        var props = new Node2D { Name = "Props", ZIndex = -10 };
        AddChild(props);
        foreach (float x in new[] { 260f, 620f, 980f, 1250f, 1700f, 2060f, 2420f })
        {
            float floor = x > 1080 && x < 1420 ? FloorY - 32 : FloorY;
            props.AddChild(new Sprite2D { Texture = ArtLibrary.Get("pillar"), Centered = false, Position = new Vector2(x - 22, floor - 300) });
        }
        foreach (float x in new[] { 110f, 440f, 870f, 1160f, 1540f, 1880f, 2240f })
            props.AddChild(new Candles { Position = new Vector2(x, 214) });

        foreach (var (x, lit) in new[] { (140f, true), (1500f, false) })
        {
            var cp = new Checkpoint { Position = new Vector2(x, FloorY), ZIndex = 2 };
            AddChild(cp);
            if (lit) cp.CallDeferred(Checkpoint.MethodName.SetLit, true);
            cp.Activated += c =>
            {
                _respawn = c.GlobalPosition + new Vector2(36, 0);
                Hud.Message("ALTAR KINDLED");
            };
        }
    }

    void BuildForeground()
    {
        Layer("fog", 1.1f, 15, new Vector2(0, 230), new Vector2(-14, 0), 0.35f);
        Layer("layer_fore", 1.3f, 20);
    }

    void AddDust()
    {
        var dust = new CpuParticles2D
        {
            Amount = 70, Lifetime = 9, Preprocess = 9, Texture = ArtLibrary.Get("dust"),
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new Vector2(380, 200),
            Direction = new Vector2(1, -0.2f), Spread = 40, Gravity = new Vector2(0, 2),
            InitialVelocityMin = 2, InitialVelocityMax = 8, ScaleAmountMin = 1, ScaleAmountMax = 1,
            Color = new Color(1f, 0.85f, 0.65f, 0.55f), ZIndex = 12, LocalCoords = false,
        };
        var fade = new Gradient();
        fade.SetColor(0, new Color(1, 1, 1, 0)); fade.SetColor(1, new Color(1, 1, 1, 0)); fade.AddPoint(0.5f, new Color(1, 1, 1, 1));
        dust.ColorRamp = fade;
        Camera.AddChild(dust);
    }

    // ------------------------------------------------------------------ enemies + death loop
    void SpawnEnemies()
    {
        foreach (var n in _actors.GetChildren()) n.QueueFree();
        foreach (var at in _enemySpawns) _actors.AddChild(new Warden { Position = at });
    }

    public Node2D SpawnEnemyAt(Vector2 at)
    {
        var w = new Warden { Position = at };
        _actors.AddChild(w);
        return w;
    }

    async void OnPlayerDied()
    {
        if (_respawning) return;
        _respawning = true;
        Hud.Message("YOU HAVE FALLEN", 1.2f);
        await ToSignal(GetTree().CreateTimer(1.4), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(Hud.Fade(1f, 0.7f), Tween.SignalName.Finished);
        SpawnEnemies();
        Player.Respawn(_respawn);
        Camera.Snap();
        await ToSignal(Hud.Fade(0f, 0.7f), Tween.SignalName.Finished);
        _respawning = false;
    }
}
