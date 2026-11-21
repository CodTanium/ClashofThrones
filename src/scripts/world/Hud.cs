using Godot;
using System;
using ClashOfThrones.Core;

namespace ClashOfThrones.World;

/// <summary>Health, short messages, fade-to-black and the pause overlay. Drawn in code at native 640x360.</summary>
public partial class Hud : CanvasLayer
{
    int _hp = 6, _max = 6;
    float _hurtPulse;
    Control _canvas;
    Label _msg, _pause, _help;
    ColorRect _fade;

    public override void _Ready()
    {
        Layer = 10;
        ProcessMode = ProcessModeEnum.Always;
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _canvas.Draw += DrawHud;
        AddChild(_canvas);

        _msg = MakeLabel(14, new Color(0.95f, 0.85f, 0.62f));
        _msg.Position = new Vector2(0, 70); _msg.Size = new Vector2(640, 20);
        _msg.HorizontalAlignment = HorizontalAlignment.Center;
        _msg.Modulate = new Color(1, 1, 1, 0);
        AddChild(_msg);

        _help = MakeLabel(8, new Color(0.75f, 0.72f, 0.8f, 0.8f));
        _help.Position = new Vector2(8, 344);
        _help.Text = "A/D move  SPACE jump  J attack  K dodge  L parry  ESC pause";
        AddChild(_help);

        _fade = new ColorRect { Color = new Color(0, 0, 0, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        _fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_fade);

        _pause = MakeLabel(20, new Color(0.95f, 0.85f, 0.62f));
        _pause.Text = "PAUSED"; _pause.Position = new Vector2(0, 160); _pause.Size = new Vector2(640, 30);
        _pause.HorizontalAlignment = HorizontalAlignment.Center; _pause.Visible = false;
        AddChild(_pause);
    }

    static Label MakeLabel(int size, Color col)
    {
        var l = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", col);
        l.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
        l.AddThemeConstantOverride("shadow_offset_x", 1); l.AddThemeConstantOverride("shadow_offset_y", 1);
        return l;
    }

    public void SetHealth(int hp, int max)
    {
        if (hp < _hp) _hurtPulse = 1;
        _hp = hp; _max = max; _canvas.QueueRedraw();
    }

    public void Message(string text, float hold = 1.6f)
    {
        _msg.Text = text;
        var tw = CreateTween();
        tw.TweenProperty(_msg, "modulate:a", 1f, 0.35);
        tw.TweenInterval(hold);
        tw.TweenProperty(_msg, "modulate:a", 0f, 0.6);
    }

    public Tween Fade(float to, float seconds)
    {
        var tw = CreateTween();
        tw.TweenProperty(_fade, "color:a", to, seconds);
        return tw;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed(InputSetup.Pause))
        {
            GetTree().Paused = !GetTree().Paused;
            _pause.Visible = GetTree().Paused;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (_hurtPulse > 0) { _hurtPulse = Mathf.Max(0, _hurtPulse - (float)delta * 3); _canvas.QueueRedraw(); }
        if (_help.Modulate.A > 0 && Time.GetTicksMsec() > 12000) _help.Modulate = new Color(1, 1, 1, Mathf.Max(0, _help.Modulate.A - (float)delta * 0.5f));
    }

    void DrawHud()
    {
        // ornate frame + blood "beads" (diamonds), top left
        var frame = new Color(0.36f, 0.3f, 0.24f); var dark = new Color(0.05f, 0.04f, 0.06f, 0.85f);
        var full = new Color(0.78f, 0.14f, 0.12f); var hi = new Color(1f, 0.45f, 0.35f); var empty = new Color(0.2f, 0.08f, 0.09f);
        float x0 = 12, y0 = 12;
        float w = 18 + _max * 13;
        _canvas.DrawRect(new Rect2(x0 - 3, y0 - 3, w, 20), dark);
        _canvas.DrawRect(new Rect2(x0 - 3, y0 - 3, w, 20), frame, false, 1);
        // a small sword emblem
        _canvas.DrawLine(new Vector2(x0 + 4, y0 + 1), new Vector2(x0 + 4, y0 + 13), new Color(0.82f, 0.84f, 0.9f), 1);
        _canvas.DrawLine(new Vector2(x0 + 1, y0 + 9), new Vector2(x0 + 7, y0 + 9), new Color(0.85f, 0.65f, 0.25f), 1);
        for (int i = 0; i < _max; i++)
        {
            var c = new Vector2(x0 + 18 + i * 13, y0 + 7);
            var pts = new[] { c + new Vector2(0, -6), c + new Vector2(5, 0), c + new Vector2(0, 6), c + new Vector2(-5, 0) };
            bool on = i < _hp;
            var col = on ? full : empty;
            if (on && i == _hp - 1 && _hurtPulse > 0) col = col.Lerp(Colors.White, _hurtPulse * 0.5f);
            _canvas.DrawColoredPolygon(pts, col);
            if (on) _canvas.DrawLine(c + new Vector2(-2, -2), c + new Vector2(0, -4), hi, 1);
            _canvas.DrawPolyline(new[] { pts[0], pts[1], pts[2], pts[3], pts[0] }, frame, 1);
        }
    }
}
