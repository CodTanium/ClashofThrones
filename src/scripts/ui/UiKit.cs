using Godot;
using System;

namespace ClashOfThrones.UI;

/// <summary>Shared look (dark stone panels, gold focus) and small builders for menus made in code.</summary>
public static class UiKit
{
    public static readonly Color Text = Color.FromHtml("#efe4c8"), Dim = Color.FromHtml("#8f8799"), Gold = Color.FromHtml("#f0c55a"),
        Blood = Color.FromHtml("#c23a33"), PanelBg = new(0.05f, 0.045f, 0.07f, 0.94f), Edge = Color.FromHtml("#4a3c30");

    static Theme _theme;
    public static Theme Theme => _theme ??= Build();

    static StyleBoxFlat Box(Color bg, Color border, int bw = 1, int pad = 4)
    {
        var s = new StyleBoxFlat { BgColor = bg, BorderColor = border, ContentMarginLeft = pad + 2, ContentMarginRight = pad + 2, ContentMarginTop = pad - 1, ContentMarginBottom = pad - 1 };
        s.SetBorderWidthAll(bw);
        return s;
    }

    static Theme Build()
    {
        var t = new Theme { DefaultFontSize = 10 };
        t.SetColor("font_color", "Label", Text);
        foreach (var type in new[] { "Button", "OptionButton", "CheckBox" })
        {
            t.SetColor("font_color", type, Text); t.SetColor("font_hover_color", type, Gold); t.SetColor("font_focus_color", type, Gold);
            t.SetColor("font_pressed_color", type, Gold); t.SetColor("font_disabled_color", type, new Color(Dim, 0.5f));
            t.SetStylebox("normal", type, Box(new Color(0.08f, 0.07f, 0.1f, 0.9f), Edge));
            t.SetStylebox("hover", type, Box(new Color(0.14f, 0.11f, 0.13f, 0.95f), Gold));
            t.SetStylebox("focus", type, Box(new Color(0f, 0f, 0f, 0f), Gold));
            t.SetStylebox("pressed", type, Box(new Color(0.2f, 0.12f, 0.1f, 1f), Gold));
            t.SetStylebox("disabled", type, Box(new Color(0.06f, 0.05f, 0.07f, 0.7f), new Color(Edge, 0.5f)));
        }
        t.SetStylebox("panel", "PanelContainer", Box(PanelBg, Edge, 1, 8));
        t.SetStylebox("panel", "Panel", Box(PanelBg, Edge, 1, 8));
        t.SetStylebox("panel", "TabContainer", Box(PanelBg, Edge, 1, 6));
        t.SetStylebox("tab_selected", "TabContainer", Box(new Color(0.16f, 0.12f, 0.12f), Gold, 1, 3));
        t.SetStylebox("tab_unselected", "TabContainer", Box(new Color(0.07f, 0.06f, 0.09f), Edge, 1, 3));
        t.SetStylebox("tab_hovered", "TabContainer", Box(new Color(0.12f, 0.1f, 0.12f), Gold, 1, 3));
        t.SetColor("font_selected_color", "TabContainer", Gold); t.SetColor("font_unselected_color", "TabContainer", Dim);
        t.SetColor("font_hovered_color", "TabContainer", Text);
        var grab = Box(Gold, Gold, 0, 3);
        t.SetStylebox("slider", "HSlider", Box(new Color(0.1f, 0.08f, 0.1f), Edge, 1, 2));
        t.SetStylebox("grabber_area", "HSlider", Box(Blood, Blood, 0, 2));
        t.SetStylebox("grabber_area_highlight", "HSlider", Box(Gold, Gold, 0, 2));
        t.SetStylebox("panel", "PopupMenu", Box(PanelBg, Edge, 1, 4));
        t.SetColor("font_color", "PopupMenu", Text); t.SetColor("font_hover_color", "PopupMenu", Gold);
        t.SetStylebox("hover", "PopupMenu", Box(new Color(0.16f, 0.12f, 0.12f), Gold, 1, 2));
        return t;
    }

    public static Label Label(string text, int size = 10, Color? color = null, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var l = new Label { Text = text, HorizontalAlignment = align, MouseFilter = Control.MouseFilterEnum.Ignore };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color ?? Text);
        l.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.85f));
        l.AddThemeConstantOverride("shadow_offset_x", 1); l.AddThemeConstantOverride("shadow_offset_y", 1);
        return l;
    }

    public static Button Button(string text, Action onPress, int minWidth = 140)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(minWidth, 16), FocusMode = Control.FocusModeEnum.All };
        b.Pressed += () => { World.Sfx.UiClick(); onPress?.Invoke(); };
        b.FocusEntered += World.Sfx.UiMove;
        return b;
    }

    public static VBoxContainer Column(int sep = 4)
    {
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", sep);
        return v;
    }

    public static HBoxContainer Row(int sep = 6)
    {
        var h = new HBoxContainer();
        h.AddThemeConstantOverride("separation", sep);
        return h;
    }

    /// <summary>A centred panel that fills the screen behind it with a dark veil.</summary>
    public static (Control root, VBoxContainer body) Dialog(string title, Vector2 minSize)
    {
        var root = new Control { Theme = Theme, MouseFilter = Control.MouseFilterEnum.Stop };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var veil = new ColorRect { Color = new Color(0, 0, 0, 0.6f), MouseFilter = Control.MouseFilterEnum.Ignore };
        veil.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(veil);
        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = minSize };
        center.AddChild(panel);
        var body = Column(5);
        panel.AddChild(body);
        if (!string.IsNullOrEmpty(title)) body.AddChild(Label(title, 14, Gold, HorizontalAlignment.Center));
        return (root, body);
    }

    public static void FocusFirst(Control c)
    {
        foreach (var n in c.FindChildren("*", "", true, false))
            if (n is BaseButton b && !b.Disabled && b.Visible) { b.CallDeferred(Control.MethodName.GrabFocus); return; }
    }
}
