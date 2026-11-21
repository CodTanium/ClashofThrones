using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using ClashOfThrones.Actors;
using ClashOfThrones.Art;

namespace ClashOfThrones.World;

/// <summary>A parsed level file: the tile grid and its [meta] lines (see src/levels/*.txt for the legend).</summary>
public sealed class LevelData
{
    public const int Tile = 32;
    public string[] Rows = Array.Empty<string>();
    public readonly Dictionary<(int, int), string[]> Meta = new();
    public readonly List<string[]> MetaLines = new();
    public int Width => Rows.Length > 0 ? Rows[0].Length : 0;
    public int Height => Rows.Length;

    public char At(int x, int y) => x < 0 || y < 0 || y >= Rows.Length || x >= Rows[y].Length ? '#' : Rows[y][x];
    public bool Solid(int x, int y) => At(x, y) == '#';

    public static LevelData Load(string resPath)
    {
        var d = new LevelData();
        var rows = new List<string>();
        string section = "";
        foreach (var raw in FileAccess.GetFileAsString(resPath).Replace("\r", "").Split('\n'))
        {
            if (raw.StartsWith("#") && section != "map") continue;
            if (raw.StartsWith("[")) { section = raw.Trim('[', ']', ' '); continue; }
            if (section == "map" && raw.Length > 0) rows.Add(raw);
            else if (section == "meta" && raw.Trim().Length > 0)
            {
                var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                d.MetaLines.Add(parts);
                if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y)) d.Meta[(x, y)] = parts;
            }
        }
        d.Rows = rows.ToArray();
        return d;
    }

    public static Vector2 Foot(int x, int y) => new(x * Tile + Tile / 2, (y + 1) * Tile);
    public static string Id(int x, int y) => $"{x}_{y}";
}

/// <summary>Draws the solid tiles: lit top surfaces, inner stone, undersides, and edge shading on open sides.</summary>
public partial class Terrain : Node2D
{
    public LevelData Data;
    Texture2D _atlas;

    public override void _Ready() { _atlas = ArtLibrary.Get("terrain"); ZIndex = 1; }

    public override void _Draw()
    {
        if (Data == null) return;
        int T = LevelData.Tile;
        var edge = new Color(0.02f, 0.02f, 0.03f, 0.55f);
        var lit = new Color(0.55f, 0.52f, 0.62f, 0.35f);
        for (int y = 0; y < Data.Height; y++)
            for (int x = 0; x < Data.Width; x++)
            {
                if (!Data.Solid(x, y)) continue;
                bool airUp = !Data.Solid(x, y - 1) && y > 0, airDown = !Data.Solid(x, y + 1) && y < Data.Height - 1;
                int tile = airUp ? 0 : airDown ? 2 : ((x * 7 + y * 13) % 5 == 0 ? 3 : 1);
                var dst = new Rect2(x * T, y * T, T, T);
                DrawTextureRectRegion(_atlas, dst, new Rect2(tile * T, 0, T, T));
                if (x > 0 && !Data.Solid(x - 1, y)) DrawRect(new Rect2(x * T, y * T + (airUp ? 3 : 0), 1, T), lit);
                if (x < Data.Width - 1 && !Data.Solid(x + 1, y)) DrawRect(new Rect2(x * T + T - 2, y * T + (airUp ? 3 : 0), 2, T), edge);
            }
    }
}

/// <summary>Builds a level from <see cref="LevelData"/>: collision (merged rectangles), terrain, objects, enemies, decor.</summary>
public static class LevelBuilder
{
    public sealed class Result
    {
        public Vector2 PlayerStart;
        public readonly Dictionary<string, Altar> Altars = new();
        public readonly Dictionary<string, Gate> Gates = new();
        public readonly List<(char kind, Vector2 pos, string id)> EnemySpawns = new();
        public Gate BossGate;
        public Vector2 BossSpawn;
        public ExitShrine Exit;
        public Rect2 Bounds;
    }

    static string Arg(string[] m, int i, string def = "") => m != null && m.Length > i ? m[i] : def;
    static string Rest(string[] m, int from) => m == null || m.Length <= from ? "" : string.Join(' ', m, from, m.Length - from);

    public static Result Build(Node2D root, LevelData d)
    {
        var r = new Result { Bounds = new Rect2(0, 0, d.Width * LevelData.Tile, d.Height * LevelData.Tile) };
        int T = LevelData.Tile;

        // collision: horizontal runs per row, merged downward when identical
        var body = new StaticBody2D { Name = "Solids", CollisionLayer = Actors.Layers.World, CollisionMask = 0 };
        root.AddChild(body);
        var open = new Dictionary<(int x0, int x1), (int y0, int y1)>();
        void Flush((int, int) key, (int, int) span)
        {
            var (x0, x1) = key; var (y0, y1) = span;
            body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2((x1 - x0 + 1) * T, (y1 - y0 + 1) * T) }, Position = new Vector2((x0 + x1 + 1) * T / 2f, (y0 + y1 + 1) * T / 2f) });
        }
        for (int y = 0; y <= d.Height; y++)
        {
            var runs = new HashSet<(int, int)>();
            for (int x = 0; y < d.Height && x < d.Width;)
            {
                if (!d.Solid(x, y)) { x++; continue; }
                int x0 = x; while (x < d.Width && d.Solid(x, y)) x++;
                runs.Add((x0, x - 1));
            }
            foreach (var key in new List<(int, int)>(open.Keys))
                if (!runs.Contains(key)) { Flush(key, open[key]); open.Remove(key); }
            foreach (var run in runs)
                open[run] = open.TryGetValue(run, out var s) ? (s.y0, y) : (y, y);
        }

        root.AddChild(new Terrain { Name = "Terrain", Data = d });

        var props = new Node2D { Name = "Props", ZIndex = -10 };
        var objects = new Node2D { Name = "Objects", ZIndex = 2 };
        root.AddChild(props); root.AddChild(objects);

        // one-way ledges: merge horizontal runs of '='
        for (int y = 0; y < d.Height; y++)
            for (int x = 0; x < d.Width;)
            {
                if (d.At(x, y) != '=' && !(d.At(x, y) == 'H' && (d.At(x - 1, y) == '=' || d.At(x + 1, y) == '='))) { x++; continue; }
                int x0 = x; while (x < d.Width && (d.At(x, y) == '=' || (d.At(x, y) == 'H' && (d.At(x + 1, y) == '=' || d.At(x - 1, y) == '=')))) x++;
                var ledge = new StaticBody2D { CollisionLayer = Actors.Layers.OneWay, CollisionMask = 0, Position = new Vector2(x0 * T, y * T), ZIndex = 1 };
                ledge.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2((x - x0) * T, 8) }, Position = new Vector2((x - x0) * T / 2f, 4), OneWayCollision = true });
                for (int i = x0; i < x; i += 3)
                    ledge.AddChild(new Sprite2D { Texture = ArtLibrary.Get("ledge"), Centered = false, Position = new Vector2((i - x0) * T, 0), RegionEnabled = true, RegionRect = new Rect2(0, 0, Math.Min(96, (x - i) * T), 14) });
                root.AddChild(ledge);
            }

        var done = new HashSet<(int, int)>();
        for (int y = 0; y < d.Height; y++)
            for (int x = 0; x < d.Width; x++)
            {
                char c = d.At(x, y);
                if (done.Contains((x, y))) continue;
                d.Meta.TryGetValue((x, y), out var m);
                var foot = LevelData.Foot(x, y);
                string id = LevelData.Id(x, y);
                switch (c)
                {
                    case 'P': r.PlayerStart = foot; break;
                    case 'A':
                        var altar = new Altar { Id = id, DisplayName = m != null ? Rest(m, 3) : "Altar", Position = foot };
                        objects.AddChild(altar); r.Altars[id] = altar; break;
                    case 'c': objects.AddChild(new Chest { Id = id, Loot = Arg(m, 3, "ashes:100"), Position = foot }); break;
                    case 'i': objects.AddChild(new Pickup { Id = id, Loot = Arg(m, 3, "flask_shard"), Position = foot + new Vector2(0, -4) }); break;
                    case 'L':
                        objects.AddChild(new Lever { Id = id, GateId = m != null ? LevelData.Id(int.Parse(m[3]), int.Parse(m[4])) : "", Position = foot }); break;
                    case 'G':
                    {
                        bool boss = Arg(m, 0) == "bossgate";
                        var gate = new Gate { Id = id, StartOpen = boss, Position = new Vector2(x * T + T / 2, (y + 3) * T) };
                        objects.AddChild(gate); r.Gates[id] = gate;
                        if (boss) r.BossGate = gate;
                        break;
                    }
                    case 'D': objects.AddChild(new Door { Id = id, Key = Arg(m, 3, "rusted_key"), Position = new Vector2(x * T + T / 2, (y + 3) * T) }); break;
                    case 'B':
                    {
                        int tiles = int.TryParse(Arg(m, 3, "2"), out var t) ? t : 2;
                        objects.AddChild(new BreakableWall { Id = id, Tiles = tiles, Position = new Vector2(x * T, y * T) });
                        break;
                    }
                    case 'H':
                    {
                        int y1 = y; while (d.At(x, y1 + 1) == 'H') { y1++; done.Add((x, y1)); }
                        objects.AddChild(new Ladder { Tiles = y1 - y + 1, Position = new Vector2(x * T + T / 2, y * T) });
                        break;
                    }
                    case '^':
                    {
                        int x1 = x; while (d.At(x1 + 1, y) == '^') { x1++; done.Add((x1, y)); }
                        objects.AddChild(new Spikes { Tiles = x1 - x + 1, Position = new Vector2(x * T, (y + 1) * T) });
                        break;
                    }
                    case 'C':
                    {
                        int x1 = x; while (d.At(x1 + 1, y) == 'C') { x1++; done.Add((x1, y)); }
                        root.AddChild(new CrumblePlatform { Tiles = x1 - x + 1, Position = new Vector2(x * T, y * T), ZIndex = 1 });
                        break;
                    }
                    case 'M':
                    {
                        float dx = float.Parse(Arg(m, 3, "4"), CultureInfo.InvariantCulture), dy = float.Parse(Arg(m, 4, "0"), CultureInfo.InvariantCulture);
                        float period = float.Parse(Arg(m, 5, "4"), CultureInfo.InvariantCulture);
                        root.AddChild(new MovingPlatform { Travel = new Vector2(dx * T, dy * T), Period = period, Position = new Vector2(x * T, y * T), ZIndex = 1 });
                        break;
                    }
                    case 'o':
                    {
                        int fy = y + 1; while (fy < d.Height && !d.Solid(x, fy)) fy++;
                        float len = (fy - y) * T - 46;
                        float phase = float.Parse(Arg(m, 3, "0"), CultureInfo.InvariantCulture);
                        objects.AddChild(new Pendulum { Length = len, Phase = phase, Position = new Vector2(x * T + T / 2, y * T) });
                        break;
                    }
                    case 'j': objects.AddChild(new FireJet { Offset = float.Parse(Arg(m, 3, "0"), CultureInfo.InvariantCulture), Position = foot }); break;
                    case 'T': objects.AddChild(new BossTrigger { Position = foot }); break;
                    case 'E': r.Exit = new ExitShrine { Position = foot }; objects.AddChild(r.Exit); break;
                    case 'X': r.BossSpawn = foot; break;
                    case 'w': case 'g': case 'a': case 'f': r.EnemySpawns.Add((c, c == 'f' ? foot + new Vector2(0, -8) : foot, id)); break;
                    case 'k': props.AddChild(new Candles { Position = foot + new Vector2(0, -6) }); break;
                    case 'p': props.AddChild(new Sprite2D { Texture = ArtLibrary.Get("pillar"), Centered = false, Position = foot + new Vector2(-22, -300) }); break;
                    case 'b': props.AddChild(new Sprite2D { Texture = ArtLibrary.Get("banner"), Centered = false, Position = new Vector2(x * T + 4, y * T) }); break;
                    case 'l':
                        props.AddChild(new Sprite2D { Texture = ArtLibrary.Get("chain_lamp"), Centered = false, Position = new Vector2(x * T + 8, y * T) });
                        props.AddChild(new PointLight2D { Texture = ArtLibrary.RadialLight(), Color = new Color(1f, 0.68f, 0.38f), Energy = 1.1f, TextureScale = 1.1f, Position = new Vector2(x * T + 16, y * T + 46) });
                        break;
                    case 'x': props.AddChild(new Sprite2D { Texture = ArtLibrary.Get("bones"), Centered = false, Position = foot + new Vector2(-20, -12), ZIndex = 12 }); break;
                }
            }

        // falling out of the world
        root.AddChild(new KillZone { Size = new Vector2(r.Bounds.Size.X + 400, 60), Position = new Vector2(-200, r.Bounds.Size.Y + 40) });
        return r;
    }

    public static EnemyBase MakeEnemy(char kind) => kind switch
    {
        'w' => new Warden(), 'g' => new Ghoul(), 'a' => new Archer(), 'f' => new Wisp(), _ => new Warden(),
    };
}
