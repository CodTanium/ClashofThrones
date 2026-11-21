using Godot;
using System;
using System.Linq;
using ClashOfThrones.Core;

namespace ClashOfThrones.UI;

/// <summary>In-game menus: pause (resume, save, relics, options, quit), altar (rest, level up, travel), relics/equipment.</summary>
public partial class GameMenus : CanvasLayer
{
    Control _open;
    public bool IsOpen => _open != null;
    public event Action RestRequested;
    public event Action<string> TravelRequested;
    public Func<(string id, string name)[]> LitAltars;
    public Action<string> Toast;

    public override void _Ready() { Layer = 20; ProcessMode = ProcessModeEnum.Always; }

    void Show(Control c)
    {
        _open?.QueueFree();
        _open = c;
        AddChild(c);
        GetTree().Paused = true;
        World.Main.I?.Hud?.Prompt("");
        UiKit.FocusFirst(c);
    }

    public void Close()
    {
        _open?.QueueFree();
        _open = null;
        GetTree().Paused = false;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed(InputSetup.Pause))
        {
            if (_open != null) Close(); else ShowPause();
            GetViewport().SetInputAsHandled();
        }
        else if (_open != null && e.IsActionPressed("ui_cancel")) { Close(); GetViewport().SetInputAsHandled(); }
    }

    // ------------------------------------------------------------------ pause
    public void ShowPause()
    {
        var (root, body) = UiKit.Dialog("PAUSED", new Vector2(180, 150));
        body.AddChild(UiKit.Button("Resume", Close));
        body.AddChild(UiKit.Button("Save Game", () => { Game.I.SaveRun(); Toast?.Invoke("Game saved (you return at the last altar)"); }));
        body.AddChild(UiKit.Button("Relics & Items", () => Show(Equipment(ShowPause))));
        body.AddChild(UiKit.Button("Options", () => Show(OptionsPanel.Create(ShowPause))));
        body.AddChild(UiKit.Button("Quit to Menu", () => Game.I.QuitToMenu()));
        body.AddChild(UiKit.Button("Quit Game", () => { Game.I.SaveRun(); GetTree().Quit(); }));
        var run = Game.I.Run;
        if (run != null) body.AddChild(UiKit.Label($"Slot {run.Slot} · {TimeSpan.FromSeconds(run.PlaySeconds):h\\:mm\\:ss} · deaths {run.Deaths}", 8, UiKit.Dim, HorizontalAlignment.Center));
        Show(root);
    }

    // ------------------------------------------------------------------ altar
    public void ShowAltar(string altarName)
    {
        var (root, body) = UiKit.Dialog(altarName, new Vector2(200, 130));
        body.AddChild(UiKit.Label("Rest restores health and flasks, but the dead rise again.", 8, UiKit.Dim, HorizontalAlignment.Center));
        body.AddChild(UiKit.Button("Rest", () => { RestRequested?.Invoke(); Close(); }));
        body.AddChild(UiKit.Button("Level Up", () => Show(LevelUp(() => ShowAltar(altarName)))));
        body.AddChild(UiKit.Button("Travel", () => Show(Travel(() => ShowAltar(altarName)))));
        body.AddChild(UiKit.Button("Relics & Items", () => Show(Equipment(() => ShowAltar(altarName)))));
        body.AddChild(UiKit.Button("Leave", Close));
        Show(root);
    }

    Control LevelUp(Action back)
    {
        var run = Game.I.Run;
        var (root, body) = UiKit.Dialog("LEVEL UP", new Vector2(260, 170));
        var info = UiKit.Label("", 9, UiKit.Text, HorizontalAlignment.Center);
        body.AddChild(info);
        void Refresh() => info.Text = $"Level {run.Level_}   ·   Ashes {run.Ashes}   ·   Cost {run.LevelCost}\nHealth {run.MaxHp}   Stamina {run.MaxStamina:0}   Damage x{run.DamageMul:0.00}";
        Refresh();
        void Stat(string name, string desc, Func<int> get, Action inc)
        {
            var row = UiKit.Row();
            var b = UiKit.Button($"{name} {get()}", null, 120);
            b.Pressed += () =>
            {
                if (run.Ashes < run.LevelCost) { Toast?.Invoke("Not enough ashes"); return; }
                run.Ashes -= run.LevelCost; inc(); b.Text = $"{name} {get()}"; Refresh(); World.Sfx.LevelUp();
            };
            row.AddChild(b);
            row.AddChild(UiKit.Label(desc, 8, UiKit.Dim));
            body.AddChild(row);
        }
        Stat("Vitality", "+1 maximum health", () => run.Vitality, () => run.Vitality++);
        Stat("Strength", "+12% damage", () => run.Strength, () => run.Strength++);
        Stat("Endurance", "+20 stamina", () => run.Endurance, () => run.Endurance++);
        body.AddChild(UiKit.Button("Back", back));
        return root;
    }

    Control Travel(Action back)
    {
        var (root, body) = UiKit.Dialog("TRAVEL", new Vector2(200, 120));
        var list = LitAltars?.Invoke() ?? Array.Empty<(string, string)>();
        foreach (var (id, name) in list)
            body.AddChild(UiKit.Button(name, () => { Close(); TravelRequested?.Invoke(id); }));
        if (list.Length <= 1) body.AddChild(UiKit.Label("Light more altars to travel between them.", 8, UiKit.Dim, HorizontalAlignment.Center));
        body.AddChild(UiKit.Button("Back", back));
        return root;
    }

    Control Equipment(Action back)
    {
        var run = Game.I.Run;
        var (root, body) = UiKit.Dialog("RELICS & ITEMS", new Vector2(320, 190));
        body.AddChild(UiKit.Label($"Relics equipped: {run.Equipped.Count}/2", 9, UiKit.Dim, HorizontalAlignment.Center));
        var desc = UiKit.Label("", 8, UiKit.Dim, HorizontalAlignment.Center);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart; desc.CustomMinimumSize = new Vector2(300, 22);
        if (run.Relics.Count == 0) body.AddChild(UiKit.Label("No relics yet. Search chests and hidden places.", 8, UiKit.Dim, HorizontalAlignment.Center));
        foreach (var id in run.Relics)
        {
            string rid = id;
            var def = Items.All[rid];
            var b = UiKit.Button("", null, 300);
            void Label() => b.Text = (run.Equipped.Contains(rid) ? "[x] " : "[ ] ") + def.Name;
            Label();
            b.FocusEntered += () => desc.Text = def.Description;
            b.MouseEntered += () => desc.Text = def.Description;
            b.Pressed += () =>
            {
                if (run.Equipped.Contains(rid)) run.Equipped.Remove(rid);
                else if (run.Equipped.Count < 2) run.Equipped.Add(rid);
                else { Toast?.Invoke("Only two relics can be worn"); return; }
                Label();
                World.Main.I?.Player?.RefreshStats();
            };
            body.AddChild(b);
        }
        var keys = run.KeyItems.Select(Items.NameOf).ToList();
        body.AddChild(UiKit.Label("Key items: " + (keys.Count == 0 ? "none" : string.Join(", ", keys)), 8, UiKit.Text, HorizontalAlignment.Center));
        body.AddChild(UiKit.Label($"Flasks: {run.FlaskMax}", 8, UiKit.Text, HorizontalAlignment.Center));
        body.AddChild(desc);
        body.AddChild(UiKit.Button("Back", back));
        return root;
    }

    // ------------------------------------------------------------------ end screens
    public void ShowVictory()
    {
        var run = Game.I.Run;
        var (root, body) = UiKit.Dialog("THE NAVE IS SILENT", new Vector2(260, 120));
        body.AddChild(UiKit.Label("The Drowned Abbot has fallen. The bells will not ring again.", 9, UiKit.Text, HorizontalAlignment.Center));
        body.AddChild(UiKit.Label($"Time {TimeSpan.FromSeconds(run.PlaySeconds):h\\:mm\\:ss}   ·   Deaths {run.Deaths}   ·   Level {run.Level_}", 9, UiKit.Dim, HorizontalAlignment.Center));
        body.AddChild(UiKit.Button("Keep exploring", Close));
        body.AddChild(UiKit.Button("Return to Menu", () => Game.I.QuitToMenu()));
        Show(root);
    }
}
