using Godot;
using System;
using ClashOfThrones.Art;
using ClashOfThrones.Core;

namespace ClashOfThrones.UI;

/// <summary>Title screen: Continue, New Game, Load Game, Options, Quit. Root of Menu.tscn.</summary>
public partial class MainMenu : Control
{
    Control _main, _overlay;

    public override void _Ready()
    {
        Theme = UiKit.Theme;
        SetAnchorsPreset(LayoutPreset.FullRect);
        // background: the cathedral layers + a slow drifting fog
        foreach (var (tex, mod) in new[] { ("layer_far", 1f), ("layer_mid", 0.9f), ("layer_wall", 0.8f) })
            AddChild(new TextureRect { Texture = ArtLibrary.Get(tex), Modulate = new Color(mod, mod, mod), StretchMode = TextureRect.StretchModeEnum.Tile, Size = new Vector2(640, 360), MouseFilter = MouseFilterEnum.Ignore });
        var fog = new TextureRect { Texture = ArtLibrary.Get("fog"), Position = new Vector2(0, 230), Size = new Vector2(1280, 120), StretchMode = TextureRect.StretchModeEnum.Tile, Modulate = new Color(1, 1, 1, 0.7f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(fog);
        var tw = CreateTween().SetLoops();
        tw.TweenProperty(fog, "position:x", -640f, 40); tw.TweenProperty(fog, "position:x", 0f, 0);
        var veil = new ColorRect { Color = new Color(0.02f, 0.02f, 0.04f, 0.45f), MouseFilter = MouseFilterEnum.Ignore };
        veil.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(veil);

        var title = UiKit.Label("CLASH OF THRONES", 30, UiKit.Text, HorizontalAlignment.Center);
        title.Position = new Vector2(0, 54); title.Size = new Vector2(640, 40);
        AddChild(title);
        var sub = UiKit.Label("~ the sunken nave ~", 11, UiKit.Dim, HorizontalAlignment.Center);
        sub.Position = new Vector2(0, 92); sub.Size = new Vector2(640, 16);
        AddChild(sub);
        var ver = UiKit.Label("v" + ProjectSettings.GetSetting("application/config/version", "0.3.0").AsString(), 8, UiKit.Dim);
        ver.Position = new Vector2(6, 346);
        AddChild(ver);

        BuildMain();
        Game.I?.Music.PlayTrack("menu");
    }

    void BuildMain()
    {
        _main?.QueueFree();
        var col = UiKit.Column(5);
        col.Position = new Vector2(250, 140);
        int last = SaveSystem.LastUsedSlot();
        var cont = UiKit.Button("Continue", () => Game.I.Continue(last));
        cont.Disabled = last < 0;
        col.AddChild(cont);
        col.AddChild(UiKit.Button("New Game", () => ShowSlots(true)));
        col.AddChild(UiKit.Button("Load Game", () => ShowSlots(false)));
        col.AddChild(UiKit.Button("Options", () => ShowOverlay(OptionsPanel.Create(CloseOverlay))));
        col.AddChild(UiKit.Button("Quit", () => GetTree().Quit()));
        AddChild(col);
        _main = col;
        UiKit.FocusFirst(col);
    }

    void ShowOverlay(Control c) { _overlay?.QueueFree(); _overlay = c; AddChild(c); _main.Visible = false; }
    void CloseOverlay() { _overlay?.QueueFree(); _overlay = null; _main.Visible = true; BuildMain(); }

    void ShowSlots(bool isNew)
    {
        var (root, body) = UiKit.Dialog(isNew ? "NEW GAME - choose a slot" : "LOAD GAME", new Vector2(380, 160));
        for (int i = 1; i <= SaveSystem.Slots; i++)
        {
            int slot = i;
            var data = SaveSystem.Load(slot);
            var row = UiKit.Row();
            bool armed = false;
            var b = UiKit.Button($"Slot {slot}   {SaveSystem.Describe(data)}", null, 300);
            b.Alignment = HorizontalAlignment.Left;
            b.Disabled = !isNew && data == null;
            b.Pressed += () =>
            {
                if (!isNew) { Game.I.Continue(slot); return; }
                if (data != null && !armed) { armed = true; b.Text = $"Slot {slot}   overwrite? press again"; return; }
                Game.I.NewGame(slot);
            };
            row.AddChild(b);
            if (data != null)
            {
                bool delArmed = false;
                Button del = null;
                del = UiKit.Button("Delete", () =>
                {
                    if (!delArmed) { delArmed = true; del.Text = "Sure?"; return; }
                    SaveSystem.Delete(slot); ShowSlots(isNew);
                }, 60);
                row.AddChild(del);
            }
            body.AddChild(row);
        }
        var back = UiKit.Button("Back", CloseOverlay);
        back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        body.AddChild(back);
        ShowOverlay(root);
        UiKit.FocusFirst(root);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_overlay != null && e.IsActionPressed("ui_cancel")) { CloseOverlay(); GetViewport().SetInputAsHandled(); }
    }
}
