using Godot;
using System;

namespace ClashOfThrones.Art;

/// <summary>Procedural art for level pieces: terrain tiles, hazards, interactables, pickups, small creatures, decor.</summary>
public static class LevelArt
{
    static Color C(string h) => Color.FromHtml(h);
    static readonly Color Outline = C("#0b0a0f");
    static readonly Ramp Stone = new("#5f5b6a", "#423f4c", "#2c2a34", "#17161c");
    static readonly Ramp Iron = new("#6b6775", "#45414f", "#2b2833", "#14121a");
    static readonly Ramp Wood = new("#7a5636", "#563b24", "#3a2717", "#1e140b");
    static readonly Ramp Gold = new("#f0c55a", "#c38d2c", "#8a5d1c", "#4a300e");

    /// <summary>Terrain atlas, 32x32 tiles: 0 top surface, 1 inner, 2 underside, 3 inner variant.</summary>
    public static Image Terrain()
    {
        var c = new PixelCanvas(128, 32);
        for (int tile = 0; tile < 4; tile++)
        {
            int ox = tile * 32;
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    int bx = (x + ((y / 8) % 2) * 8) / 16, by = y / 8;
                    float v = PixelCanvas.Hash(bx + tile * 7, by, 41);
                    float l = 0.2f + v * 0.05f;
                    bool mortar = (x + ((y / 8) % 2) * 8) % 16 == 0 || y % 8 == 7;
                    if (mortar) l = 0.1f;
                    if (PixelCanvas.Hash(x + ox, y, 42) > 0.94f) l -= 0.02f;
                    if (tile == 0)
                    {
                        if (y < 2) l = 0.55f; else if (y == 2) l = 0.42f; else if (y < 5) l = 0.3f - (y - 3) * 0.04f; else l -= 0.02f;
                        if (y < 5 && x % 16 == 0) l = 0.16f;
                    }
                    if (tile == 2 && y > 28) l = 0.08f;
                    if (tile == 3 && PixelCanvas.Hash(x / 4, y / 4, 43) > 0.85f && !mortar) l += 0.03f;
                    c.Set(ox + x, y, Color.FromHsv(0.7f, 0.11f, MathF.Max(0.03f, l)));
                }
            if (tile == 0) for (int x = 0; x < 32; x += 3) if (PixelCanvas.Hash(x, 0, 44) > 0.6f) c.Set(ox + x, 0, C("#7a7488"));
        }
        return c.ToImage();
    }

    public static Image Spikes()
    {
        var c = new PixelCanvas(32, 16);
        for (int i = 0; i < 4; i++)
        {
            float x0 = i * 8;
            c.Poly(new[] { new Vector2(x0 + 0.5f, 16), new Vector2(x0 + 4, 2), new Vector2(x0 + 7.5f, 16) }, Iron, 0.1f, 1.2f);
            c.Set((int)x0 + 3, 4, C("#b0aab8"));
            c.Set((int)x0 + 4, 3, C("#c8c2d0"));
        }
        c.Rect(0, 14, 32, 2, Iron.Deep);
        c.Set(5, 8, C("#6a1a1a")); c.Set(13, 10, C("#6a1a1a"));
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image Crumble()
    {
        var c = new PixelCanvas(32, 14);
        var st = new Ramp("#6a6070", "#4a4352", "#322d38", "#1a171e");
        c.Poly(new[] { new Vector2(0, 0), new Vector2(32, 0), new Vector2(30, 13), new Vector2(2, 13) }, st, 0.1f, 1f);
        c.Rect(0, 0, 32, 1, st.Hi);
        c.Line(new Vector2(9, 1), new Vector2(13, 8), st.Deep); c.Line(new Vector2(13, 8), new Vector2(11, 13), st.Deep);
        c.Line(new Vector2(22, 1), new Vector2(19, 6), st.Deep); c.Line(new Vector2(19, 6), new Vector2(24, 12), st.Deep);
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image MovingPlatform()
    {
        var c = new PixelCanvas(64, 14);
        c.Poly(new[] { new Vector2(0, 2), new Vector2(64, 2), new Vector2(60, 13), new Vector2(4, 13) }, Iron, 0.1f, 1f);
        c.Rect(0, 2, 64, 1, Iron.Hi);
        for (int x = 6; x < 64; x += 14) { c.Circle(new Vector2(x, 8), 1.5f, Gold, 0.3f); }
        c.Rect(28, 0, 8, 3, Iron.Lo);
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image Ladder()
    {
        var c = new PixelCanvas(24, 32);
        c.Rect(2, 0, 3, 32, Wood.Mid); c.Rect(2, 0, 1, 32, Wood.Hi); c.Rect(19, 0, 3, 32, Wood.Mid); c.Rect(19, 0, 1, 32, Wood.Hi);
        for (int y = 3; y < 32; y += 8) { c.Rect(5, y, 14, 2, Wood.Lo); c.Rect(5, y, 14, 1, Wood.Mid); }
        return c.ToImage();
    }

    /// <summary>Chest: frame 0 closed, frame 1 open (32x28 each).</summary>
    public static Image Chest()
    {
        var c = new PixelCanvas(64, 28);
        for (int f = 0; f < 2; f++)
        {
            int ox = f * 32;
            c.Poly(new[] { new Vector2(ox + 3, 14), new Vector2(ox + 29, 14), new Vector2(ox + 29, 27), new Vector2(ox + 3, 27) }, Wood, 0.05f, 0.8f);
            c.Rect(ox + 3, 18, 26, 2, Iron.Mid); c.Rect(ox + 8, 14, 2, 13, Iron.Lo); c.Rect(ox + 22, 14, 2, 13, Iron.Lo);
            if (f == 0)
            {
                c.Poly(new[] { new Vector2(ox + 3, 14), new Vector2(ox + 6, 6), new Vector2(ox + 26, 6), new Vector2(ox + 29, 14) }, Wood, 0.2f, 0.8f);
                c.Rect(ox + 14, 12, 4, 5, Gold.Mid); c.Set(ox + 15, 14, Gold.Deep);
            }
            else
            {
                c.Poly(new[] { new Vector2(ox + 3, 14), new Vector2(ox + 2, 3), new Vector2(ox + 7, 1), new Vector2(ox + 8, 14) }, Wood, 0.2f, 0.8f);
                c.Rect(ox + 9, 13, 18, 2, C("#ffd27a")); c.Rect(ox + 11, 12, 12, 1, C("#fff0c0"));
            }
        }
        c.Outline(Outline);
        return c.ToImage();
    }

    /// <summary>Wall lever: frame 0 up, frame 1 pulled down (16x24).</summary>
    public static Image Lever()
    {
        var c = new PixelCanvas(32, 24);
        for (int f = 0; f < 2; f++)
        {
            int ox = f * 16;
            c.Rect(ox + 4, 12, 8, 10, Iron.Lo); c.Rect(ox + 4, 12, 8, 1, Iron.Hi);
            var pivot = new Vector2(ox + 8, 16);
            var tip = f == 0 ? pivot + new Vector2(-4, -12) : pivot + new Vector2(4, -12);
            c.Capsule(pivot, tip, 1.2f, Iron);
            c.Circle(tip, 2.2f, new Ramp("#e05040", "#a02a20", "#601812", "#300a08"));
        }
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image Gate()
    {
        var c = new PixelCanvas(32, 96);
        c.Rect(0, 0, 32, 6, Iron.Lo); c.Rect(0, 0, 32, 1, Iron.Hi);
        for (int x = 3; x < 32; x += 7) { c.Capsule(new Vector2(x, 4), new Vector2(x, 90), 1.4f, Iron); c.Poly(new[] { new Vector2(x - 2, 90), new Vector2(x + 2, 90), new Vector2(x, 96) }, Iron, 0.1f); }
        c.Rect(0, 40, 32, 3, Iron.Lo);
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image Door()
    {
        var c = new PixelCanvas(32, 96);
        c.Poly(new[] { new Vector2(1, 12), new Vector2(16, 0), new Vector2(31, 12), new Vector2(31, 96), new Vector2(1, 96) }, Wood, 0.05f, 0.7f);
        for (int x = 8; x < 32; x += 8) c.Line(new Vector2(x, 6), new Vector2(x, 95), Wood.Deep);
        c.Rect(1, 30, 30, 3, Iron.Mid); c.Rect(1, 70, 30, 3, Iron.Mid);
        c.Rect(20, 50, 6, 8, Gold.Mid); c.Set(22, 53, Outline); c.Set(22, 54, Outline);
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image Breakable()
    {
        var c = new PixelCanvas(32, 32);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                bool mortar = (x + ((y / 8) % 2) * 8) % 16 == 0 || y % 8 == 7;
                float l = mortar ? 0.06f : 0.12f + PixelCanvas.Hash(x / 16, y / 8, 51) * 0.04f;
                c.Set(x, y, Color.FromHsv(0.7f, 0.11f, l));
            }
        // subtle cracks: the hint that this wall can be broken
        c.Line(new Vector2(6, 4), new Vector2(14, 14), C("#24222a")); c.Line(new Vector2(14, 14), new Vector2(11, 24), C("#24222a"));
        c.Line(new Vector2(14, 14), new Vector2(24, 18), C("#24222a")); c.Line(new Vector2(24, 18), new Vector2(27, 28), C("#24222a"));
        return c.ToImage();
    }

    /// <summary>Pendulum blade (crescent) - drawn pointing down, pivot at the top centre of a 48x16 image; the chain is drawn in code.</summary>
    public static Image Pendulum()
    {
        var c = new PixelCanvas(48, 20);
        var steel = new Ramp("#e8ecf2", "#a8adb8", "#6c717c", "#3a3d45");
        var pts = new Vector2[20];
        for (int i = 0; i < 10; i++) { float a = MathF.PI * i / 9f; pts[i] = new Vector2(24 + MathF.Cos(a) * 22, 2 + MathF.Sin(a) * 16); }
        for (int i = 0; i < 10; i++) { float a = MathF.PI * (9 - i) / 9f; pts[10 + i] = new Vector2(24 + MathF.Cos(a) * 16, 2 + MathF.Sin(a) * 9); }
        c.Poly(pts, steel, 0.1f, 1f);
        c.Rect(21, 0, 6, 6, Iron.Lo);
        c.Outline(Outline);
        return c.ToImage();
    }

    /// <summary>Fire jet: nozzle + 4 flame frames (16x64 each; frame 0 = off).</summary>
    public static Image FireJet()
    {
        var c = new PixelCanvas(64, 64);
        for (int f = 0; f < 4; f++)
        {
            int ox = f * 16;
            c.Rect(ox + 3, 56, 10, 8, Iron.Lo); c.Rect(ox + 3, 56, 10, 1, Iron.Hi); c.Rect(ox + 5, 54, 6, 2, Iron.Deep);
            if (f == 0) continue;
            for (int y = 0; y < 52; y++)
            {
                float u = y / 52f;
                float w = (2 + u * 4) * (0.8f + 0.2f * MathF.Sin(y * 0.5f + f * 2));
                var col = u < 0.3f ? C("#ff6a20") : u < 0.7f ? C("#ffb040") : C("#fff0b0");
                for (int x = (int)(8 - w); x <= (int)(8 + w); x++) if (PixelCanvas.Hash(x + ox, y + f * 7, 60) > u * 0.4f) c.Set(ox + x, y + 2, col);
            }
        }
        return c.ToImage();
    }

    /// <summary>Relic / item pickup: glowing reliquary, 4 shimmer frames (16x16).</summary>
    public static Image Pickup()
    {
        var c = new PixelCanvas(64, 16);
        for (int f = 0; f < 4; f++)
        {
            int ox = f * 16;
            c.Poly(new[] { new Vector2(ox + 8, 1), new Vector2(ox + 14, 8), new Vector2(ox + 8, 15), new Vector2(ox + 2, 8) }, Gold, 0.15f, 1f);
            c.Circle(new Vector2(ox + 8, 8), 2.5f, new Ramp("#fffbe6", "#ffe8a0", "#e0b050", "#a07020"));
            int sx = ox + 3 + f * 3; c.Set(sx, 3, Colors.White); c.Set(sx + 1, 3, new Color(1, 1, 1, 0.6f));
        }
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image Orb()
    {
        var c = new PixelCanvas(8, 8);
        c.Circle(new Vector2(4, 4), 3.2f, new Ramp("#fff2c8", "#ffb650", "#d0702a", "#80380e"));
        return c.ToImage();
    }

    public static Image Remnant()
    {
        var c = new PixelCanvas(16, 24);
        c.Ellipse(new Vector2(8, 20), 6, 2.5f, new Color(0.9f, 0.5f, 0.2f, 0.6f));
        c.Ellipse(new Vector2(8, 12), 3, 9, new Color(1f, 0.7f, 0.3f, 0.55f));
        c.Ellipse(new Vector2(8, 13), 1.5f, 6, new Color(1f, 0.95f, 0.8f, 0.9f));
        return c.ToImage();
    }

    public static Image Arrow()
    {
        var c = new PixelCanvas(18, 5);
        c.Rect(0, 2, 14, 1, C("#c8b48a"));
        c.Poly(new[] { new Vector2(13, 0), new Vector2(18, 2.5f), new Vector2(13, 5) }, Iron, 0.2f);
        c.Rect(0, 1, 3, 1, C("#e0e0e0")); c.Rect(0, 3, 3, 1, C("#e0e0e0"));
        return c.ToImage();
    }

    /// <summary>Wisp: burning skull, 8 frames of 32x32 (0-3 float, 4-5 attack, 6-7 death).</summary>
    public static Image Wisp()
    {
        var c = new PixelCanvas(256, 32);
        var bone = new Ramp("#f2ead2", "#c8bc9c", "#8c8068", "#4a4234");
        for (int f = 0; f < 8; f++)
        {
            int ox = f * 32;
            float wob = MathF.Sin(f * 1.6f);
            // flame trail behind/above
            for (int k = 0; k < 14; k++)
            {
                float y = 4 + k * 0.9f + wob, x = ox + 10 - k * 0.6f + MathF.Sin(k + f) * 1.5f;
                var col = k < 5 ? new Color(0.55f, 0.95f, 1f, 0.9f) : new Color(0.3f, 0.6f, 1f, 0.6f - k * 0.03f);
                if (f >= 6) col = new Color(col, col.A * (f == 6 ? 0.6f : 0.25f));
                c.Ellipse(new Vector2(x, y), 3.5f - k * 0.15f, 2.5f, col);
            }
            if (f >= 7) continue;
            var head = new Vector2(ox + 18, 16 + wob);
            c.Circle(head, 7, bone, 0.1f);
            bool open = f is 4 or 5;
            c.Rect((int)head.X - 2, (int)head.Y + 4, 8, open ? 5 : 3, bone.Mid);
            if (open) c.Rect((int)head.X - 1, (int)head.Y + 6, 6, 2, Outline);
            c.Circle(head + new Vector2(2, -1), 2, new Ramp(Outline, Outline, Outline, Outline));
            c.Set((int)head.X + 2, (int)head.Y - 1, C("#8ff6ff"));
            c.Set((int)head.X - 3, (int)head.Y - 1, Outline);
        }
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image ExitShrine()
    {
        var c = new PixelCanvas(64, 96);
        c.Poly(new[] { new Vector2(6, 96), new Vector2(6, 30), new Vector2(32, 4), new Vector2(58, 30), new Vector2(58, 96) }, Stone, 0.05f, 1f);
        c.Poly(new[] { new Vector2(16, 96), new Vector2(16, 38), new Vector2(32, 20), new Vector2(48, 38), new Vector2(48, 96) }, new Ramp("#fff6d8", "#ffd890", "#e0a050", "#a06020"), 0.3f, 0.6f);
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image Banner()
    {
        var c = new PixelCanvas(24, 72);
        var red = new Ramp("#a8302c", "#7a201e", "#4e1416", "#2a0a0c");
        c.Rect(0, 0, 24, 3, Wood.Mid);
        c.Poly(new[] { new Vector2(2, 3), new Vector2(22, 3), new Vector2(22, 64), new Vector2(17, 58), new Vector2(12, 70), new Vector2(7, 58), new Vector2(2, 64) }, red, 0f, 1f);
        c.Line(new Vector2(12, 14), new Vector2(12, 44), Gold.Mid); c.Line(new Vector2(6, 22), new Vector2(18, 22), Gold.Mid);
        c.Ellipse(new Vector2(12, 22), 4, 4, new Color(Gold.Lo, 0.6f));
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image ChainLamp()
    {
        var c = new PixelCanvas(16, 56);
        for (int y = 0; y < 40; y += 4) { c.Rect(7, y, 2, 3, Iron.Lo); }
        c.Poly(new[] { new Vector2(2, 40), new Vector2(14, 40), new Vector2(11, 52), new Vector2(5, 52) }, Iron, 0.1f, 1f);
        c.Ellipse(new Vector2(8, 46), 3, 3, C("#ffb040"));
        c.Ellipse(new Vector2(8, 46), 1.5f, 1.5f, C("#fff0c0"));
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image Bones()
    {
        var c = new PixelCanvas(40, 12);
        var bone = new Ramp("#e0d8c0", "#b0a68a", "#7a7058", "#3e382a");
        c.Capsule(new Vector2(4, 9), new Vector2(18, 7), 1.4f, bone);
        c.Capsule(new Vector2(14, 10), new Vector2(26, 10), 1.3f, bone);
        c.Circle(new Vector2(30, 7), 4.2f, bone, 0.1f);
        c.Set(31, 6, Outline); c.Set(29, 6, Outline);
        c.Capsule(new Vector2(22, 6), new Vector2(36, 11), 1.1f, bone);
        c.Outline(Outline);
        return c.ToImage();
    }
}
