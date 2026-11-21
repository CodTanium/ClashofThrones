using Godot;
using System;
using ClashOfThrones.Core;

namespace ClashOfThrones.UI;

/// <summary>Options: Graphics, Audio, Gameplay, Controls. Changes apply at once and are saved on Back.</summary>
public static class OptionsPanel
{
    public static Control Create(Action onBack)
    {
        var (root, body) = UiKit.Dialog("OPTIONS", new Vector2(400, 250));
        var s = Settings.Current;
        var tabs = new TabContainer { CustomMinimumSize = new Vector2(380, 200) };
        body.AddChild(tabs);

        // ---- graphics
        var g = Grid(tabs, "Graphics");
        Option(g, "Window", new[] { "Windowed", "Fullscreen", "Borderless" }, s.WindowMode, i => { s.WindowMode = i; Settings.Apply(); });
        Option(g, "Window scale", new[] { "2x (1280x720)", "3x (1920x1080)", "4x (2560x1440)" }, Math.Clamp(s.WindowScale - 2, 0, 2), i => { s.WindowScale = i + 2; Settings.Apply(); });
        Check(g, "V-Sync", s.VSync, v => { s.VSync = v; Settings.Apply(); });
        var fps = new[] { 0, 30, 60, 120, 144 };
        Option(g, "Max FPS", new[] { "Unlimited", "30", "60", "120", "144" }, Math.Max(0, Array.IndexOf(fps, s.MaxFps)), i => { s.MaxFps = fps[i]; Settings.Apply(false); });
        Check(g, "Show FPS", s.ShowFps, v => s.ShowFps = v);

        // ---- audio
        var a = Grid(tabs, "Audio");
        Slider(a, "Master", s.MasterVolume, v => { s.MasterVolume = v; Settings.Apply(false); });
        Slider(a, "Music", s.MusicVolume, v => { s.MusicVolume = v; Settings.Apply(false); });
        Slider(a, "Effects", s.SfxVolume, v => { s.SfxVolume = v; Settings.Apply(false); World.Sfx.UiMove(); });

        // ---- gameplay + accessibility
        var p = Grid(tabs, "Gameplay");
        Option(p, "Difficulty", new[] { "Easy", "Normal", "Hard" }, (int)s.Difficulty, i => s.Difficulty = (Difficulty)i);
        Check(p, "Screen shake", s.ScreenShake, v => s.ScreenShake = v);
        Check(p, "Hit-stop (impact freeze)", s.HitStop, v => s.HitStop = v);
        Check(p, "Controller vibration", s.Vibration, v => { s.Vibration = v; if (v) Game.I?.Rumble(0.6f, 0.2f); });

        // ---- controls (reference)
        var scroll = new ScrollContainer { Name = "Controls", CustomMinimumSize = new Vector2(370, 170) };
        tabs.AddChild(scroll);
        var cg = new GridContainer { Columns = 3 };
        cg.AddThemeConstantOverride("h_separation", 10);
        scroll.AddChild(cg);
        cg.AddChild(UiKit.Label("Action", 9, UiKit.Gold)); cg.AddChild(UiKit.Label("Keyboard", 9, UiKit.Gold)); cg.AddChild(UiKit.Label("Gamepad", 9, UiKit.Gold));
        foreach (var (act, keys, pad) in InputSetup.Help) { cg.AddChild(UiKit.Label(act, 8)); cg.AddChild(UiKit.Label(keys, 8, UiKit.Dim)); cg.AddChild(UiKit.Label(pad, 8, UiKit.Dim)); }

        var back = UiKit.Button("Back", () => { Settings.Save(); onBack(); });
        back.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        body.AddChild(back);
        UiKit.FocusFirst(root);
        return root;
    }

    static GridContainer Grid(TabContainer tabs, string name)
    {
        var g = new GridContainer { Name = name, Columns = 2 };
        g.AddThemeConstantOverride("h_separation", 16); g.AddThemeConstantOverride("v_separation", 6);
        tabs.AddChild(g);
        return g;
    }

    static void Option(GridContainer g, string label, string[] items, int selected, Action<int> changed)
    {
        g.AddChild(UiKit.Label(label));
        var o = new OptionButton { CustomMinimumSize = new Vector2(150, 16) };
        foreach (var it in items) o.AddItem(it);
        o.Selected = Math.Clamp(selected, 0, items.Length - 1);
        o.ItemSelected += i => changed((int)i);
        g.AddChild(o);
    }

    static void Check(GridContainer g, string label, bool value, Action<bool> changed)
    {
        g.AddChild(UiKit.Label(label));
        var c = new CheckBox { ButtonPressed = value, Text = value ? "On" : "Off" };
        c.Toggled += v => { c.Text = v ? "On" : "Off"; changed(v); };
        g.AddChild(c);
    }

    static void Slider(GridContainer g, string label, float value, Action<float> changed)
    {
        g.AddChild(UiKit.Label(label));
        var s = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = value, CustomMinimumSize = new Vector2(150, 14), FocusMode = Control.FocusModeEnum.All };
        s.ValueChanged += v => changed((float)v);
        g.AddChild(s);
    }
}
