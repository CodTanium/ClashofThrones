using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClashOfThrones.Art;

namespace ClashOfThrones.Dev;

/// <summary>
/// Autoload "DevBridge" (debug builds only, localhost only). Lets tools see, change and test the running game:
/// screenshots, scene-tree dump, get/set any property, call methods, spawn/free scenes, run GDScript snippets,
/// simulated input, scene reload and log tail. Protocol: one JSON object per line over TCP 127.0.0.1:47800,
/// e.g. {"cmd":"get","node":"/root/Main/Player","prop":"position"}. Client: tools/Cot (cot.exe).
///
/// Command line (after "--"):  --script="wait 1; press move_right; wait 0.8; tap attack; shot out.png; quit"
///                             --bake-art=&lt;dir&gt;   write every placeholder texture as PNG and quit
/// </summary>
public partial class DevBridge : Node
{
    public const int DefaultPort = 47800;
    TcpServer _server;
    readonly List<Client> _clients = new();
    readonly Queue<string> _script = new();
    double _wait;
    readonly List<(string action, double until)> _taps = new();

    sealed class Client { public StreamPeerTcp Peer; public readonly StringBuilder Buf = new(); }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        var args = ParseArgs();
        if (args.TryGetValue("bake-art", out var bakeDir))
        {
            int n = ArtLibrary.BakeAll(ProjectSettings.GlobalizePath(bakeDir));
            GD.Print($"[DevBridge] baked {n} textures to {bakeDir}");
            GetTree().Quit();
            return;
        }
        if (RunArtTool(args)) { GetTree().Quit(); return; }
        if (!OS.IsDebugBuild()) { SetProcess(false); return; }

        int port = DefaultPort;
        if (args.TryGetValue("port", out var p) && int.TryParse(p, out var pp)) port = pp;
        _server = new TcpServer();
        var err = _server.Listen((ushort)port, "127.0.0.1");
        GD.Print(err == Error.Ok ? $"[DevBridge] listening on 127.0.0.1:{port}" : $"[DevBridge] could not listen on {port}: {err}");

        if (args.TryGetValue("script", out var script))
            foreach (var step in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) _script.Enqueue(step);
    }

    /// <summary>Offline art commands (art pipeline). Returns true when one ran.</summary>
    static bool RunArtTool(Dictionary<string, string> a)
    {
        string Abs(string p) => p.StartsWith("res://") || p.StartsWith("user://") ? ProjectSettings.GlobalizePath(p) : System.IO.Path.GetFullPath(p);
        int I(string k, int d) => a.TryGetValue(k, out var v) && int.TryParse(v, out var n) ? n : d;
        SpriteSheet Sheet() => ArtLibrary.Sheet(a.TryGetValue("rig", out var r) ? r : "hero");
        if (a.ContainsKey("selftest"))
        {
            // works in release builds too: proves the packed game has its level and art
            bool level = FileAccess.FileExists(World.Main.LevelPath);
            int rows = level ? World.LevelData.Load(World.Main.LevelPath).Height : 0;
            bool art = ArtLibrary.Get("hero") != null && ArtLibrary.Get("abbot") != null && ArtLibrary.Get("terrain") != null;
            GD.Print($"[selftest] debug={OS.IsDebugBuild()} level={level} rows={rows} art={art} -> {(level && rows > 0 && art ? "PASS" : "FAIL")}");
            return true;
        }
        if (a.TryGetValue("export-rig", out var exp))
        {
            int n = ArtClean.ExportRigFrames(Sheet(), Abs(exp), I("scale", 8), Color.FromHtml(a.GetValueOrDefault("bg", "#7a7680")));
            GD.Print($"[DevBridge] exported {n} rig frames to {Abs(exp)}");
            return true;
        }
        if (a.TryGetValue("art-restyle", out var dir))
        {
            var sheet = ArtClean.RestyleSheet(Abs(dir), Sheet(), I("colors", 32));
            string outp = Abs(a.GetValueOrDefault("out", "res://art/hero.png"));
            sheet.SavePng(outp);
            GD.Print($"[DevBridge] restyled sheet {outp}");
            return true;
        }
        if (a.TryGetValue("art-clean", out var src))
        {
            var img = Image.LoadFromFile(Abs(src));
            var res = ArtClean.CleanCharacter(img, I("height", 96), I("colors", 28), ArtLibrary.HeroFrame, ArtLibrary.HeroOrigin);
            string outp = Abs(a.GetValueOrDefault("out", "user://clean.png"));
            res.SavePng(outp);
            GD.Print($"[DevBridge] cleaned {src} -> {outp}");
            return true;
        }
        return false;
    }

    static Dictionary<string, string> ParseArgs()
    {
        var d = new Dictionary<string, string>();
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            if (!a.StartsWith("--")) continue;
            int eq = a.IndexOf('=');
            if (eq < 0) d[a[2..]] = "true"; else d[a[2..eq]] = a[(eq + 1)..].Trim('"');
        }
        return d;
    }

    public override void _Process(double delta)
    {
        double now = Time.GetTicksMsec() / 1000.0;
        for (int i = _taps.Count - 1; i >= 0; i--)
            if (now >= _taps[i].until) { Input.ActionRelease(_taps[i].action); _taps.RemoveAt(i); }

        RunScript(delta);

        if (_server == null) return;
        while (_server.IsConnectionAvailable()) _clients.Add(new Client { Peer = _server.TakeConnection() });
        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var c = _clients[i];
            c.Peer.Poll();
            if (c.Peer.GetStatus() != StreamPeerTcp.Status.Connected) { _clients.RemoveAt(i); continue; }
            int avail = c.Peer.GetAvailableBytes();
            if (avail > 0)
            {
                var res = c.Peer.GetData(avail);
                c.Buf.Append(Encoding.UTF8.GetString(res[1].AsByteArray()));
                string all = c.Buf.ToString();
                int nl;
                while ((nl = all.IndexOf('\n')) >= 0)
                {
                    string line = all[..nl].Trim();
                    all = all[(nl + 1)..];
                    if (line.Length > 0) _ = HandleLine(c, line);
                }
                c.Buf.Clear().Append(all);
            }
        }
    }

    // ------------------------------------------------------------------ startup script (offline tests)
    async void RunScript(double delta)
    {
        if (_script.Count == 0) return;
        if (_wait > 0) { _wait -= delta; return; }
        var step = _script.Dequeue();
        var parts = step.Split(' ', 2, StringSplitOptions.TrimEntries);
        string arg = parts.Length > 1 ? parts[1] : "";
        GD.Print($"[DevBridge] script: {step}");
        switch (parts[0])
        {
            case "wait": _wait = double.Parse(arg, CultureInfo.InvariantCulture); break;
            case "press": Input.ActionPress(arg); break;
            case "release": Input.ActionRelease(arg); break;
            case "tap": Tap(arg, 0.08); break;
            case "shot": _wait = 1e9; await Screenshot(arg); _wait = 0; break;
            case "set":
            {
                var a = arg.Split(' ', 3);
                var node = GetNodeOrNull(a[0]);
                if (node != null) node.Set(a[1], ConvertJson(JsonDocument.Parse(a[2]).RootElement, node.Get(a[1])));
                break;
            }
            case "print": GD.Print("[DevBridge] " + arg); break;
            case "newgame": Core.Game.I.NewGame(Core.SaveSystem.DevSlot); break;
            case "continue": Core.Game.I.Continue(int.Parse(arg)); break;
            case "menu": Core.Game.I.QuitToMenu(false); break;
            case "give": Core.Game.I.EnsureRun(); World.Main.I?.Grant(arg, World.Main.I.Player.GlobalPosition); break;
            case "tp": { var a = arg.Split(' '); World.Main.I.Player.GlobalPosition = new Vector2(float.Parse(a[0], CultureInfo.InvariantCulture), float.Parse(a[1], CultureInfo.InvariantCulture)); break; }
            case "quit": GetTree().Quit(); break;
        }
    }

    void Tap(string action, double seconds)
    {
        Input.ActionPress(action);
        _taps.Add((action, Time.GetTicksMsec() / 1000.0 + seconds));
    }

    async Task<string> Screenshot(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var img = GetViewport().GetTexture().GetImage();
        if (string.IsNullOrEmpty(path)) path = "user://shot.png";
        string abs = path.StartsWith("res://") || path.StartsWith("user://") ? ProjectSettings.GlobalizePath(path) : path;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(abs))!);
        img.SavePng(abs);
        GD.Print($"[DevBridge] screenshot {abs} ({img.GetWidth()}x{img.GetHeight()})");
        return abs;
    }

    // ------------------------------------------------------------------ TCP commands
    async Task HandleLine(Client c, string line)
    {
        object result;
        try
        {
            using var doc = JsonDocument.Parse(line);
            result = await Execute(doc.RootElement);
        }
        catch (Exception e) { result = new { ok = false, error = e.Message }; }
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result) + "\n");
        if (c.Peer.GetStatus() == StreamPeerTcp.Status.Connected) c.Peer.PutData(bytes);
    }

    static string S(JsonElement e, string key, string def = null) => e.TryGetProperty(key, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()) : def;

    Node Find(string path)
    {
        if (string.IsNullOrEmpty(path)) return GetTree().CurrentScene;
        var n = path.StartsWith("/") ? GetNodeOrNull(path) : GetTree().CurrentScene?.GetNodeOrNull(path);
        return n ?? throw new Exception($"node not found: {path}");
    }

    async Task<object> Execute(JsonElement e)
    {
        string cmd = S(e, "cmd") ?? throw new Exception("missing cmd");
        switch (cmd)
        {
            case "ping": return new { ok = true, engine = Engine.GetVersionInfo()["string"].ToString(), scene = GetTree().CurrentScene?.Name.ToString(), fps = Engine.GetFramesPerSecond() };
            case "shot": return new { ok = true, path = await Screenshot(S(e, "path")) };
            case "tree":
            {
                int depth = int.TryParse(S(e, "depth"), out var dd) ? dd : 3;
                var sb = new StringBuilder();
                Dump(Find(S(e, "node")), 0, depth, sb);
                return new { ok = true, tree = sb.ToString() };
            }
            case "get":
            {
                var n = Find(S(e, "node"));
                string prop = S(e, "prop");
                if (string.IsNullOrEmpty(prop))
                {
                    var props = n.GetPropertyList().Select(d => d["name"].AsString()).Where(s => !s.Contains('/') && char.IsLower(s.FirstOrDefault())).ToArray();
                    return new { ok = true, properties = props };
                }
                return new { ok = true, value = n.Get(prop).ToString(), type = n.Get(prop).VariantType.ToString() };
            }
            case "set":
            {
                var n = Find(S(e, "node"));
                string prop = S(e, "prop");
                n.Set(prop, ConvertJson(e.GetProperty("value"), n.Get(prop)));
                return new { ok = true, value = n.Get(prop).ToString() };
            }
            case "call":
            {
                var n = Find(S(e, "node"));
                var args = new List<Variant>();
                if (e.TryGetProperty("args", out var arr)) foreach (var a in arr.EnumerateArray()) args.Add(ConvertJson(a, default));
                var r = n.Call(S(e, "method"), args.ToArray());
                return new { ok = true, value = r.ToString() };
            }
            case "spawn":
            {
                var scene = GD.Load<PackedScene>(S(e, "scene")) ?? throw new Exception("scene not found");
                var parent = Find(S(e, "parent"));
                var inst = scene.Instantiate();
                if (inst is Node2D n2 && e.TryGetProperty("x", out var x)) n2.Position = new Vector2(x.GetSingle(), e.GetProperty("y").GetSingle());
                parent.AddChild(inst);
                return new { ok = true, path = inst.GetPath().ToString() };
            }
            case "spawn_enemy":
            {
                var kind = (S(e, "kind", "w") ?? "w")[0];
                var w = World.Main.I?.SpawnEnemyAt(new Vector2(e.GetProperty("x").GetSingle(), e.TryGetProperty("y", out var y) ? y.GetSingle() : World.Main.I.Player.GlobalPosition.Y), kind);
                return new { ok = w != null, path = w?.GetPath().ToString() };
            }
            case "free": { var n = Find(S(e, "node")); n.QueueFree(); return new { ok = true }; }
            case "gd":
            {
                string code = S(e, "code") ?? "";
                var lines = code.Replace("\r", "").Split('\n');
                bool single = lines.Length == 1 && !code.TrimStart().StartsWith("return") && !code.Contains('=') ;
                var body = string.Join("\n", lines.Select(l => "\t" + l));
                if (single) body = "\treturn " + code.Trim();
                var src = "extends RefCounted\nfunc run(root: Node, scene: Node):\n" + body + "\n\treturn null\n";
                var gs = new GDScript { SourceCode = src };
                var err = gs.Reload();
                if (err != Error.Ok) throw new Exception("GDScript compile error: " + err);
                var obj = gs.New().AsGodotObject();
                var r = obj.Call("run", GetTree().Root, GetTree().CurrentScene);
                return new { ok = true, value = r.ToString() };
            }
            case "press": Input.ActionPress(S(e, "action")); return new { ok = true };
            case "release": Input.ActionRelease(S(e, "action")); return new { ok = true };
            case "tap":
            {
                double secs = double.TryParse(S(e, "seconds"), NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ? s : 0.08;
                Tap(S(e, "action"), secs);
                return new { ok = true };
            }
            case "actions": return new { ok = true, actions = InputMap.GetActions().Select(a => a.ToString()).Where(a => !a.StartsWith("ui_")).ToArray() };
            case "reload": GetTree().ReloadCurrentScene(); return new { ok = true };
            case "newgame": Core.Game.I.NewGame(Core.SaveSystem.DevSlot); return new { ok = true };
            case "give": World.Main.I?.Grant(S(e, "loot"), World.Main.I.Player.GlobalPosition); return new { ok = World.Main.I != null };
            case "pause": GetTree().Paused = S(e, "on", "true") != "false"; return new { ok = true, paused = GetTree().Paused };
            case "timescale": Engine.TimeScale = double.Parse(S(e, "value", "1"), CultureInfo.InvariantCulture); return new { ok = true, value = Engine.TimeScale };
            case "log":
            {
                int n = int.TryParse(S(e, "lines"), out var nn) ? nn : 40;
                string file = System.IO.Path.Combine(OS.GetUserDataDir(), "logs", "godot.log");
                if (!System.IO.File.Exists(file)) return new { ok = false, error = "no log file: " + file };
                using var fs = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                using var sr = new System.IO.StreamReader(fs);
                var all = sr.ReadToEnd().Replace("\r", "").Split('\n');
                return new { ok = true, log = string.Join("\n", all.Skip(Math.Max(0, all.Length - n))) };
            }
            case "status":
            {
                var m = World.Main.I;
                if (m == null) return new { ok = false, error = "no Main scene" };
                var enemies = GetTree().GetNodesInGroup("enemies").OfType<Actors.EnemyBase>().Select(w => new { type = w.GetType().Name, path = w.GetPath().ToString(), x = w.GlobalPosition.X, y = w.GlobalPosition.Y, hp = w.Hp, state = w.StateName }).ToArray();
                return new { ok = true, player = new { x = m.Player.GlobalPosition.X, y = m.Player.GlobalPosition.Y, hp = m.Player.Hp, stamina = (int)m.Player.Stamina, wrath = m.Player.Wrath, state = m.Player.StateName, facing = m.Player.Facing }, run = new { Core.Game.I.Run.Slot, Core.Game.I.Run.Ashes, Core.Game.I.Run.Flasks, Core.Game.I.Run.Altar, Core.Game.I.Run.BossDefeated, keys = Core.Game.I.Run.KeyItems, relics = Core.Game.I.Run.Equipped }, enemies };
            }
            default: throw new Exception("unknown cmd: " + cmd);
        }
    }

    static void Dump(Node n, int level, int max, StringBuilder sb)
    {
        sb.Append(new string(' ', level * 2)).Append(n.Name).Append(" (").Append(n.GetClass()).Append(')');
        if (n is Node2D n2) sb.Append(" @").Append(n2.GlobalPosition.ToString());
        sb.Append('\n');
        if (level >= max) { if (n.GetChildCount() > 0) sb.Append(new string(' ', level * 2 + 2)).Append($"... {n.GetChildCount()} children\n"); return; }
        foreach (var c in n.GetChildren()) Dump(c, level + 1, max, sb);
    }

    /// <summary>JSON -> Variant, shaped like the current value when one is known (e.g. [x,y] -> Vector2).</summary>
    static Variant ConvertJson(JsonElement v, Variant current)
    {
        var t = current.VariantType;
        switch (v.ValueKind)
        {
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.String: return v.GetString();
            case JsonValueKind.Number:
                if (t == Variant.Type.Int) return v.GetInt64();
                return v.GetDouble();
            case JsonValueKind.Array:
            {
                var f = v.EnumerateArray().Select(x => x.GetSingle()).ToArray();
                if (f.Length == 2) return new Vector2(f[0], f[1]);
                if (f.Length == 3) return new Vector3(f[0], f[1], f[2]);
                if (f.Length == 4) return t == Variant.Type.Color ? new Color(f[0], f[1], f[2], f[3]) : new Rect2(f[0], f[1], f[2], f[3]);
                return new Godot.Collections.Array(f.Select(x => Variant.From(x)).ToArray());
            }
            default: return default;
        }
    }
}
