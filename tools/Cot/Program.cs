// cot - command-line client for the game's DevBridge (debug builds, 127.0.0.1:47800).
// Lets a developer (or an automation script) see, change and test the running game from the terminal.
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

static string Usage() => """
cot - talk to the running game (start it with scripts\run.ps1 first)

  cot ping                                 engine version, scene, fps
  cot status                               player + enemies (position, hp, state)
  cot shot [file.png]                      screenshot of the game view
  cot tree [node] [depth]                  scene tree dump
  cot get <node> [prop]                    read a property (no prop = list properties)
  cot set <node> <prop> <json>             write a property, e.g. cot set Player position [400,300]
  cot call <node> <method> [json args...]  call a method
  cot spawn <res://scene.tscn> [parent] [x y]
  cot enemy <x> [y]                        spawn a Grave Warden
  cot free <node>                          delete a node
  cot give <loot>                          ashes:500 | relic:iron_vow | item:wings_of_ash | key:rusted_key | flask_shard
  cot newgame                              start a new game in the test slot
  cot gd "<gdscript>"                      run a GDScript snippet (vars: root, scene)
  cot press|release <action>               hold / let go of an input action
  cot tap <action> [seconds]               short press
  cot actions                              list input actions
  cot reload | pause [on|off] | timescale <x> | log [lines]
  cot raw '<json>'                         send a raw command

  options: --port <n> (default 47800, env COT_PORT)
  node paths are relative to the current scene (Player, Actors/@CharacterBody2D@3) or absolute (/root/Main/Player).
""";

var argv = args.ToList();
int port = int.TryParse(Environment.GetEnvironmentVariable("COT_PORT"), out var envPort) ? envPort : 47800;
int pi = argv.IndexOf("--port");
if (pi >= 0 && pi + 1 < argv.Count) { port = int.Parse(argv[pi + 1]); argv.RemoveRange(pi, 2); }
if (argv.Count == 0 || argv[0] is "-h" or "--help" or "help") { Console.WriteLine(Usage()); return 0; }

static JsonNode? J(string s) { try { return JsonNode.Parse(s); } catch { return JsonValue.Create(s); } }
string A(int i, string def = "") => i < argv.Count ? argv[i] : def;

var cmd = new JsonObject();
switch (argv[0])
{
    case "ping": case "status": case "reload": case "actions": cmd["cmd"] = argv[0]; break;
    case "shot":
        cmd["cmd"] = "shot";
        cmd["path"] = Path.GetFullPath(A(1, Path.Combine("reports", "screenshots", $"cot-{DateTime.Now:HHmmss}.png")));
        break;
    case "tree": cmd["cmd"] = "tree"; cmd["node"] = A(1); cmd["depth"] = A(2, "3"); break;
    case "get": cmd["cmd"] = "get"; cmd["node"] = A(1); cmd["prop"] = A(2); break;
    case "set": cmd["cmd"] = "set"; cmd["node"] = A(1); cmd["prop"] = A(2); cmd["value"] = J(A(3)); break;
    case "call":
        cmd["cmd"] = "call"; cmd["node"] = A(1); cmd["method"] = A(2);
        cmd["args"] = new JsonArray(argv.Skip(3).Select(J).ToArray());
        break;
    case "spawn":
        cmd["cmd"] = "spawn"; cmd["scene"] = A(1); cmd["parent"] = A(2);
        if (argv.Count >= 5) { cmd["x"] = float.Parse(A(3)); cmd["y"] = float.Parse(A(4)); }
        break;
    case "enemy": cmd["cmd"] = "spawn_enemy"; cmd["x"] = float.Parse(A(1, "600")); if (argv.Count > 2) cmd["y"] = float.Parse(A(2)); break;
    case "free": cmd["cmd"] = "free"; cmd["node"] = A(1); break;
    case "give": cmd["cmd"] = "give"; cmd["loot"] = A(1); break;          // ashes:500, relic:iron_vow, item:wings_of_ash, key:rusted_key, flask_shard
    case "newgame": cmd["cmd"] = "newgame"; break;
    case "gd": cmd["cmd"] = "gd"; cmd["code"] = string.Join(' ', argv.Skip(1)); break;
    case "press": case "release": cmd["cmd"] = argv[0]; cmd["action"] = A(1); break;
    case "tap": cmd["cmd"] = "tap"; cmd["action"] = A(1); cmd["seconds"] = A(2, "0.08"); break;
    case "pause": cmd["cmd"] = "pause"; cmd["on"] = A(1, "on") == "off" ? "false" : "true"; break;
    case "timescale": cmd["cmd"] = "timescale"; cmd["value"] = A(1, "1"); break;
    case "log": cmd["cmd"] = "log"; cmd["lines"] = A(1, "40"); break;
    case "raw": cmd = JsonNode.Parse(string.Join(' ', argv.Skip(1)))!.AsObject(); break;
    default: Console.Error.WriteLine($"unknown command: {argv[0]}\n"); Console.WriteLine(Usage()); return 2;
}

try
{
    using var tcp = new TcpClient();
    var connect = tcp.ConnectAsync("127.0.0.1", port);
    if (!connect.Wait(2000)) throw new TimeoutException();
    using var stream = tcp.GetStream();
    var bytes = Encoding.UTF8.GetBytes(cmd.ToJsonString() + "\n");
    stream.Write(bytes);
    stream.ReadTimeout = 15000;
    using var reader = new StreamReader(stream, Encoding.UTF8);
    var line = reader.ReadLine() ?? throw new IOException("no reply");
    var reply = JsonNode.Parse(line)!.AsObject();
    bool ok = reply["ok"]?.GetValue<bool>() ?? false;
    // pretty output: multi-line text fields printed raw, everything else as indented JSON
    foreach (var key in new[] { "tree", "log" })
        if (reply[key] is JsonValue v) { Console.WriteLine(v.GetValue<string>()); reply.Remove(key); }
    reply.Remove("ok");
    if (reply.Count > 0) Console.WriteLine(reply.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    return ok ? 0 : 1;
}
catch (Exception e) when (e is SocketException or TimeoutException or AggregateException)
{
    Console.Error.WriteLine($"no game on 127.0.0.1:{port} - start it with scripts\\run.ps1 (debug build)");
    return 3;
}
