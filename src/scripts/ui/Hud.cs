using Godot;
using System;
using System.Collections.Generic;
using ClashOfThrones.Actors;
using ClashOfThrones.Core;

namespace ClashOfThrones.UI;

/// <summary>Gameplay HUD: health, stamina, wrath, flasks, ashes, boss bar, prompts, toasts, centre messages, fade.</summary>
public partial class Hud : CanvasLayer
{
    Player _player;
    Control _canvas;
    Label _msg, _prompt, _ashes, _fps, _bossName;
    VBoxContainer _toasts;
    ColorRect _fade;
    string _boss; float _bossHp01 = 1, _bossShown;
    float _hpShown, _hurtPulse;
    int _lastAshes = -1; float _ashPulse;

    public override void _Ready()
    {
        Layer = 10;
        ProcessMode = ProcessModeEnum.Always;
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _canvas.Draw += DrawBars;
        AddChild(_canvas);

        _ashes = UiKit.Label("0", 10, UiKit.Text, HorizontalAlignment.Right);
        _ashes.Position = new Vector2(540, 10); _ashes.Size = new Vector2(80, 14);
        AddChild(_ashes);
        _msg = UiKit.Label("", 16, new Color(0.95f, 0.85f, 0.62f), HorizontalAlignment.Center);
        _msg.Position = new Vector2(0, 70); _msg.Size = new Vector2(640, 22); _msg.Modulate = new Color(1, 1, 1, 0);
        AddChild(_msg);
        _prompt = UiKit.Label("", 10, UiKit.Text, HorizontalAlignment.Center);
        _prompt.Position = new Vector2(0, 300); _prompt.Size = new Vector2(640, 14); _prompt.Visible = false;
        AddChild(_prompt);
        _bossName = UiKit.Label("", 10, UiKit.Text, HorizontalAlignment.Center);
        _bossName.Position = new Vector2(0, 318); _bossName.Size = new Vector2(640, 14); _bossName.Visible = false;
        AddChild(_bossName);
        _toasts = UiKit.Column(2);
        _toasts.Position = new Vector2(10, 250);
        AddChild(_toasts);
        _fps = UiKit.Label("", 8, UiKit.Dim);
        _fps.Position = new Vector2(600, 348);
        AddChild(_fps);
        _fade = new ColorRect { Color = new Color(0, 0, 0, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        _fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_fade);
    }

    public void Bind(Player p) { _player = p; _hpShown = p.Hp; }

    public void Message(string text, float hold = 1.6f)
    {
        _msg.Text = text;
        var tw = CreateTween();
        tw.TweenProperty(_msg, "modulate:a", 1f, 0.35);
        tw.TweenInterval(hold);
        tw.TweenProperty(_msg, "modulate:a", 0f, 0.6);
    }

    public void Toast(string text, Color? color = null)
    {
        var l = UiKit.Label(Pad(text), 9, color ?? UiKit.Gold);
        _toasts.AddChild(l);
        var tw = l.CreateTween();
        tw.TweenInterval(3.0); tw.TweenProperty(l, "modulate:a", 0f, 0.8); tw.TweenCallback(Callable.From(l.QueueFree));
    }

    public void Prompt(string text) { _prompt.Text = Pad(text); _prompt.Visible = !string.IsNullOrEmpty(text); }

    /// <summary>Swaps keyboard hints for Xbox button names when a controller is in use.</summary>
    public static string Pad(string text) => Game.I != null && Game.I.UsingPad && text != null
        ? text.Replace("[E]", "[LT]").Replace("Esc:", "Start:").Replace("[J]", "[X]").Replace("[R]", "[RB]").Replace("[F]", "[RT]")
        : text;

    public void ShowBoss(string name) { _boss = name; _bossName.Text = name; _bossName.Visible = true; _bossShown = 0; }
    public void SetBossHealth(float v) => _bossHp01 = Mathf.Clamp(v, 0, 1);
    public void HideBoss() { _boss = null; _bossName.Visible = false; }

    public Tween Fade(float to, float seconds)
    {
        var tw = CreateTween();
        tw.TweenProperty(_fade, "color:a", to, seconds);
        return tw;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_player != null)
        {
            if (_player.Hp < _hpShown - 0.01f) _hurtPulse = 1;
            _hpShown = Mathf.MoveToward(_hpShown, _player.Hp, dt * 4);
            int ash = Game.I?.Run?.Ashes ?? 0;
            if (ash != _lastAshes) { if (_lastAshes >= 0) _ashPulse = 1; _lastAshes = ash; _ashes.Text = ash.ToString(); }
        }
        _ashPulse = Mathf.Max(0, _ashPulse - dt * 2);
        _ashes.Modulate = Colors.White.Lerp(UiKit.Gold, _ashPulse);
        _hurtPulse = Mathf.Max(0, _hurtPulse - dt * 3);
        if (_boss != null) _bossShown = Mathf.MoveToward(_bossShown, _bossHp01, dt * 0.8f);
        _fps.Visible = Settings.Current.ShowFps;
        if (_fps.Visible) _fps.Text = $"{Engine.GetFramesPerSecond()} fps";
        _canvas.QueueRedraw();
    }

    void Bar(float x, float y, float w, float h, float v, float ghost, Color full, Color back)
    {
        _canvas.DrawRect(new Rect2(x - 1, y - 1, w + 2, h + 2), new Color(0.03f, 0.02f, 0.04f, 0.9f));
        _canvas.DrawRect(new Rect2(x, y, w, h), back);
        if (ghost > v) _canvas.DrawRect(new Rect2(x, y, w * ghost, h), new Color(1f, 0.9f, 0.75f, 0.6f));
        _canvas.DrawRect(new Rect2(x, y, w * v, h), full);
        _canvas.DrawRect(new Rect2(x, y, w * v, 1), full.Lightened(0.35f));
        _canvas.DrawRect(new Rect2(x - 1, y - 1, w + 2, h + 2), UiKit.Edge, false, 1);
    }

    void DrawBars()
    {
        if (_player == null) return;
        float hpW = 60 + _player.MaxHp * 9;
        Bar(30, 12, hpW, 6, _player.Hp / (float)_player.MaxHp, _hpShown / _player.MaxHp, new Color(0.72f, 0.12f, 0.11f).Lerp(Colors.White, _hurtPulse * 0.4f), new Color(0.18f, 0.06f, 0.07f));
        Bar(30, 21, _player.MaxStamina * 0.8f, 3, _player.Stamina / _player.MaxStamina, 0, new Color(0.45f, 0.62f, 0.32f), new Color(0.08f, 0.12f, 0.07f));
        bool wrathFull = _player.Wrath >= 1f;
        var wrathCol = _player.WrathActive ? new Color(1f, 0.5f, 0.15f) : wrathFull ? new Color(1f, 0.45f, 0.2f).Lerp(Colors.White, (MathF.Sin(Time.GetTicksMsec() / 120f) + 1) * 0.2f) : new Color(0.75f, 0.3f, 0.12f);
        Bar(30, 28, 80, 2, _player.WrathActive ? _player.WrathTimeLeft01 : _player.Wrath, 0, wrathCol, new Color(0.1f, 0.05f, 0.03f));
        // emblem: sword in a ring
        var c = new Vector2(17, 20);
        _canvas.DrawArc(c, 10, 0, MathF.Tau, 24, UiKit.Edge, 2);
        _canvas.DrawLine(c + new Vector2(0, -8), c + new Vector2(0, 8), new Color(0.85f, 0.87f, 0.92f), 2);
        _canvas.DrawLine(c + new Vector2(-4, 3), c + new Vector2(4, 3), UiKit.Gold, 2);
        // flasks
        for (int i = 0; i < _player.FlaskMax; i++)
        {
            var p = new Vector2(32 + i * 9, 36);
            bool full = i < _player.Flasks;
            _canvas.DrawRect(new Rect2(p.X + 2, p.Y, 3, 2), new Color(0.6f, 0.55f, 0.5f));
            _canvas.DrawRect(new Rect2(p.X, p.Y + 2, 7, 7), full ? new Color(0.85f, 0.55f, 0.2f) : new Color(0.15f, 0.12f, 0.12f));
            _canvas.DrawRect(new Rect2(p.X, p.Y + 2, 7, 7), new Color(0.05f, 0.04f, 0.05f), false, 1);
        }
        // ashes icon
        _canvas.DrawCircle(new Vector2(628, 17), 4, new Color(0.9f, 0.6f, 0.3f));
        _canvas.DrawCircle(new Vector2(628, 17), 2, new Color(1f, 0.9f, 0.7f));
        _ashes.Position = new Vector2(536, 10);
        if (_boss != null) Bar(120, 334, 400, 5, _bossHp01, _bossShown, new Color(0.65f, 0.1f, 0.12f), new Color(0.15f, 0.05f, 0.06f));
    }
}
