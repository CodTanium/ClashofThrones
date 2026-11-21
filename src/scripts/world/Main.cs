using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ClashOfThrones.Actors;
using ClashOfThrones.Art;
using ClashOfThrones.Core;
using ClashOfThrones.UI;

namespace ClashOfThrones.World;

/// <summary>
/// The game scene: builds the level from its map file, spawns the player and enemies, and runs the loop:
/// altars (rest / level up / travel / autosave), loot, gates, death -> remnant -> respawn, the boss fight, victory.
/// </summary>
public partial class Main : Node2D
{
    public static Main I { get; private set; }
    public const string LevelPath = "res://levels/sunken_nave.txt";

    public Player Player { get; private set; }
    public CameraRig Camera { get; private set; }
    public Hud Hud { get; private set; }
    public GameMenus Menus { get; private set; }
    public LevelBuilder.Result Level { get; private set; }
    public Abbot Boss { get; private set; }

    Node2D _actors;
    Remnant _remnant;
    bool _respawning, _bossActive;
    SaveData Run => Game.I.Run;

    public override void _Ready()
    {
        I = this;
        Game.I.EnsureRun();
        AddChild(new CanvasModulate { Color = new Color(0.56f, 0.53f, 0.64f) });
        BuildBackground();

        var data = LevelData.Load(LevelPath);
        Level = LevelBuilder.Build(this, data);

        _actors = new Node2D { Name = "Actors", ZIndex = 5 };
        AddChild(_actors);

        Player = new Player { Name = "Player", Position = SpawnPoint(), ZIndex = 10 };
        AddChild(Player);
        Player.Died += OnPlayerDied;
        SpawnEnemies();
        SpawnRemnant();

        BuildForeground();
        var b = Level.Bounds;
        Camera = new CameraRig { Name = "Camera", Target = Player, LimitLeft = 0, LimitTop = 0, LimitRight = (int)b.End.X, LimitBottom = (int)b.End.Y };
        AddChild(Camera);
        Camera.MakeCurrent();
        Camera.Snap();
        AddDust();

        Hud = new Hud { Name = "Hud" };
        AddChild(Hud);
        Hud.Bind(Player);
        Menus = new GameMenus { Name = "Menus" };
        AddChild(Menus);
        Menus.Toast = t => Hud.Toast(t);
        Menus.RestRequested += () => Rest(_lastAltar);
        Menus.TravelRequested += Travel;
        Menus.LitAltars = () => Level.Altars.Values.Where(a => a.Lit).Select(a => (a.Id, a.DisplayName)).ToArray();

        Game.I.Music.PlayTrack("explore");
        Hud.Message(Run.BossDefeated ? "THE SUNKEN NAVE (silent)" : "THE SUNKEN NAVE", 2.2f);
        if (Run.PlaySeconds < 1) Hud.Toast("Esc: pause, options, controls");
    }

    Vector2 SpawnPoint()
    {
        if (!string.IsNullOrEmpty(Run.Altar) && Level.Altars.TryGetValue(Run.Altar, out var a)) return a.GlobalPosition + new Vector2(36, 0);
        return Level.PlayerStart;
    }

    public override void _Process(double delta)
    {
        if (Player == null || Hud == null) return;
        Hud.Prompt(Menus.IsOpen ? "" : Player.FocusedInteractable?.PromptText ?? "");
        if (_bossActive && Boss != null && IsInstanceValid(Boss)) Hud.SetBossHealth(Boss.Hp / (float)Boss.MaxHp);
    }

    // ------------------------------------------------------------------ background
    void Layer(string tex, Vector2 scroll, int z, Vector2 offset = default, Vector2? autoscroll = null, float alpha = 1f, bool repeatY = true)
    {
        var p = new Parallax2D { ScrollScale = scroll, RepeatSize = new Vector2(640, repeatY ? 360 : 0), RepeatTimes = 3, ZIndex = z };
        if (autoscroll.HasValue) p.Autoscroll = autoscroll.Value;
        p.AddChild(new Sprite2D { Texture = ArtLibrary.Get(tex), Centered = false, Position = offset, Modulate = new Color(1, 1, 1, alpha) });
        AddChild(p);
    }

    void BuildBackground()
    {
        Layer("layer_far", new Vector2(0.1f, 0.1f), -50);
        Layer("layer_mid", new Vector2(0.3f, 0.3f), -40);
        Layer("layer_wall", new Vector2(0.55f, 0.55f), -30);
        Layer("fog", new Vector2(0.75f, 0.75f), -25, new Vector2(0, 240), new Vector2(-6, 0), 0.6f, false);
    }

    void BuildForeground()
    {
        Layer("fog", new Vector2(1.1f, 1f), 15, new Vector2(0, 260), new Vector2(-14, 0), 0.3f, true);
        Layer("layer_fore", new Vector2(1.3f, 1f), 20, default, null, 1f, false);
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

    // ------------------------------------------------------------------ actors
    void SpawnEnemies()
    {
        foreach (var n in GetTree().GetNodesInGroup("enemies")) ((Node)n).QueueFree();
        foreach (var n in _actors.GetChildren()) if (n is Arrow or Shockwave or FirePillar or AshOrb) n.QueueFree();
        foreach (var (kind, pos, id) in Level.EnemySpawns)
        {
            var e = LevelBuilder.MakeEnemy(kind);
            e.Position = pos; e.SpawnId = id;
            _actors.AddChild(e);
        }
        Boss = null; _bossActive = false;
        if (!Run.BossDefeated && Level.BossSpawn != Vector2.Zero)
        {
            Boss = new Abbot { Position = Level.BossSpawn, Name = "Boss" };
            Boss.Died += _ => OnBossDefeated();
            _actors.AddChild(Boss);
        }
        Level.BossGate?.SetOpen(true, false);
    }

    public Node2D SpawnEnemyAt(Vector2 at, char kind = 'w')
    {
        var e = LevelBuilder.MakeEnemy(kind);
        e.Position = at;
        _actors.AddChild(e);
        return e;
    }

    void SpawnRemnant()
    {
        if (_remnant != null && IsInstanceValid(_remnant)) _remnant.QueueFree();
        _remnant = null;
        if (Run.RemnantAshes <= 0) return;
        _remnant = new Remnant { Position = new Vector2(Run.RemnantX, Run.RemnantY), ZIndex = 6 };
        AddChild(_remnant);
    }

    // ------------------------------------------------------------------ altars
    Altar _lastAltar;
    public void OpenAltar(Altar a) { _lastAltar = a; Menus.ShowAltar(a.DisplayName.ToUpperInvariant()); }

    void Rest(Altar a)
    {
        if (a == null) return;
        Run.Altar = a.Id;
        Run.Flasks = Run.FlaskMax;
        Player.RefreshStats(); Player.Heal();
        SpawnEnemies();
        Game.I.SaveRun();
        Effects.Heal(this, Player.GlobalPosition + new Vector2(0, -40));
        Sfx.Rest(a);
        Hud.Toast("Rested. The dead rise again. (saved)");
    }

    async void Travel(string id)
    {
        if (!Level.Altars.TryGetValue(id, out var a)) return;
        GetTree().Paused = true;
        await ToSignal(Hud.Fade(1f, 0.5f), Tween.SignalName.Finished);
        Player.Respawn(a.GlobalPosition + new Vector2(36, 0));
        _lastAltar = a;
        Rest(a);
        Camera.Snap();
        GetTree().Paused = false;
        await ToSignal(Hud.Fade(0f, 0.5f), Tween.SignalName.Finished);
    }

    // ------------------------------------------------------------------ loot + world state
    public void Grant(string loot, Vector2 at)
    {
        var parts = loot.Split(':');
        string kind = parts[0], val = parts.Length > 1 ? parts[1] : "";
        switch (kind)
        {
            case "ashes": Loot.SpawnAshes(this, at, int.Parse(val)); break;
            case "relic":
                if (!Run.Relics.Contains(val)) Run.Relics.Add(val);
                if (Run.Equipped.Count < 2 && !Run.Equipped.Contains(val)) Run.Equipped.Add(val);
                Player.RefreshStats();
                Hud.Toast($"Relic: {Items.NameOf(val)} - {Items.All[val].Description}");
                break;
            case "key": case "item":
                if (!Run.KeyItems.Contains(val)) Run.KeyItems.Add(val);
                Hud.Toast($"{Items.NameOf(val)}: {Items.All[val].Description}");
                if (val == "wings_of_ash") Hud.Message("WINGS OF ASH - jump again in mid-air", 2.5f);
                break;
            case "flask_shard":
                Run.FlaskMax++; Run.Flasks++;
                Hud.Toast("Flask Shard: +1 healing flask");
                break;
        }
        Sfx.Pickup(Player);
        Effects.Spark(this, at, 1.2f);
    }

    public void OpenGate(string id)
    {
        if (!Level.Gates.TryGetValue(id, out var g)) return;
        g.SetOpen(true);
        if (!Run.Taken.Contains("gate_" + id)) Run.Taken.Add("gate_" + id);
        Hud.Toast("Somewhere, a gate grinds open.");
    }

    // ------------------------------------------------------------------ boss
    public void StartBoss()
    {
        if (_bossActive || Boss == null || !IsInstanceValid(Boss) || Boss.IsDead || Run.BossDefeated) return;
        _bossActive = true;
        Level.BossGate?.SetOpen(false);
        Boss.Wake();
        Hud.ShowBoss(Boss.BossName);
        Game.I.Music.PlayTrack("boss");
    }

    void OnBossDefeated()
    {
        _bossActive = false;
        Run.BossDefeated = true;
        Hud.HideBoss();
        Hud.Message("THE ABBOT IS SILENCED", 3f);
        Level.BossGate?.SetOpen(true);
        Level.Exit?.Awaken();
        Game.I.Music.PlayTrack("explore");
        Game.I.HitStop(0.4, 0.1);
        Game.I.SaveRun();
    }

    public void Victory()
    {
        Run.Completed = true;
        Game.I.SaveRun();
        Menus.ShowVictory();
    }

    // ------------------------------------------------------------------ death loop
    async void OnPlayerDied()
    {
        if (_respawning) return;
        _respawning = true;
        Run.Deaths++;
        // carried ashes stay where you fell; an older remnant is lost
        Run.RemnantAshes = Run.Ashes; Run.Ashes = 0;
        var at = Player.LastSafe;
        Run.RemnantX = at.X; Run.RemnantY = at.Y;
        Hud.Message("YOU HAVE FALLEN", 1.4f);
        await ToSignal(GetTree().CreateTimer(1.6), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(Hud.Fade(1f, 0.7f), Tween.SignalName.Finished);
        if (_bossActive) { Hud.HideBoss(); Game.I.Music.PlayTrack("explore"); }
        Run.Flasks = Run.FlaskMax;
        SpawnEnemies();
        SpawnRemnant();
        Player.Respawn(SpawnPoint());
        Camera.Snap();
        Game.I.SaveRun();
        await ToSignal(Hud.Fade(0f, 0.7f), Tween.SignalName.Finished);
        _respawning = false;
    }
}
