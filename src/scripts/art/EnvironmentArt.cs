using Godot;
using System;

namespace ClashOfThrones.Art;

/// <summary>Procedural environment art: gothic brick dungeon, parallax layers, props. Everything original, generated in code.</summary>
public static class EnvironmentArt
{
    static Color C(string h) => Color.FromHtml(h);
    static readonly Color Outline = C("#0b0a0f");

    /// <summary>Brick block colours with per-brick variation; lit edge top-left, shadow bottom-right.</summary>
    static void Bricks(PixelCanvas c, int x0, int y0, int w, int h, int bw, int bh, int seed, float bright = 0f)
    {
        var mortar = C("#121117");
        for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
            {
                int row = (y - y0) / bh;
                int off = (row % 2) * (bw / 2);
                int bx = (x - x0 + off) / bw, lx = (x - x0 + off) % bw, ly = (y - y0) % bh;
                if (lx == 0 || ly == bh - 1) { c.Set(x, y, mortar); continue; }
                float v = PixelCanvas.Hash(bx, row, seed);
                float n = PixelCanvas.Hash(x, y, seed + 7);
                float baseL = 0.15f + v * 0.06f + bright;
                if (ly == 0 || lx == 1) baseL += 0.05f;              // lit edges
                if (ly == bh - 2 || lx == bw - 1) baseL -= 0.04f;     // shadow edges
                if (n > 0.93f) baseL -= 0.04f; else if (n < 0.05f) baseL += 0.03f;   // grain
                // chipped corners
                if (v > 0.8f && lx > bw - 4 && ly < 3) { c.Set(x, y, mortar); continue; }
                c.Set(x, y, Color.FromHsv(0.68f, 0.12f + v * 0.05f, baseL));
            }
        // a few cracks
        for (int k = 0; k < w * h / 2500; k++)
        {
            float sx = x0 + PixelCanvas.Hash(k, 1, seed) * w, sy = y0 + PixelCanvas.Hash(k, 2, seed) * h;
            var p = new Vector2(sx, sy);
            for (int s = 0; s < 6; s++)
            {
                var q = p + new Vector2(PixelCanvas.Hash(k, s + 3, seed) * 6 - 3, PixelCanvas.Hash(k, s + 9, seed) * 5);
                c.Line(p, q, mortar); p = q;
            }
        }
    }

    public static Image BrickTile()
    {
        var c = new PixelCanvas(96, 40);
        Bricks(c, 0, 0, 96, 40, 24, 10, 11);
        return c.ToImage();
    }

    /// <summary>Back wall with two tall gothic arches cut out (640 wide, repeats).</summary>
    public static Image WallLayer()
    {
        int W = 640, H = 360;
        var c = new PixelCanvas(W, H);
        Bricks(c, 0, 0, W, 300, 24, 10, 23);
        // vertical darkening toward the floor + top
        for (int y = 0; y < 300; y++)
            for (int x = 0; x < W; x++)
            {
                float shade = y < 60 ? (60 - y) / 60f * 0.5f : y > 220 ? (y - 220) / 80f * 0.35f : 0f;
                if (shade > 0) c.Set(x, y, new Color(0.02f, 0.02f, 0.04f, shade));
            }
        foreach (int cx in new[] { 160, 480 })
        {
            int half = 52, top = 70, spring = 140;
            // stone frame (lighter blocks) then cut the opening
            ArchShape(c, cx, half + 8, top - 10, spring, 300, (x, y) =>
            {
                int ly = (y / 12) % 2;
                bool joint = (y % 12 == 0) || ((x - cx + 200 + ly * 6) % 14 == 0);
                c.Set(x, y, joint ? C("#16151c") : Color.FromHsv(0.7f, 0.1f, 0.24f + PixelCanvas.Hash(x / 14, y / 12, 5) * 0.05f));
            });
            ArchShape(c, cx, half, top, spring, 300, (x, y) => c.Clear(x, y));
        }
        return c.ToImage();
    }

    static void ArchShape(PixelCanvas c, int cx, int half, int top, int spring, int bottom, Action<int, int> plot)
    {
        // pointed (gothic) arch: two circle arcs meeting at the top
        float r = half * 1.6f;
        for (int y = top; y < bottom; y++)
            for (int x = cx - half; x <= cx + half; x++)
            {
                if (y >= spring) { plot(x, y); continue; }
                float lx = cx + half - r, rx = cx - half + r; // arc centres
                bool inL = (x - lx) * (x - lx) + (y - spring) * (y - spring) <= r * r;
                bool inR = (x - rx) * (x - rx) + (y - spring) * (y - spring) <= r * r;
                if (inL && inR) plot(x, y);
            }
    }

    /// <summary>Far layer: dark sky, distant cathedral silhouettes with a few warm windows.</summary>
    public static Image FarLayer()
    {
        int W = 640, H = 360;
        var c = new PixelCanvas(W, H);
        var top = C("#0d0c16"); var bot = C("#2a2234");
        for (int y = 0; y < H; y++)
        {
            var row = top.Lerp(bot, MathF.Pow(y / (float)H, 1.3f));
            for (int x = 0; x < W; x++)
            {
                float n = PixelCanvas.Fbm(x / 80f, y / 40f, 8, 3);
                c.Set(x, y, row.Lightened(n * 0.06f));
            }
        }
        // pale moon haze top-left (light source)
        for (int y = 0; y < 160; y++) for (int x = 0; x < 220; x++)
            {
                float d = new Vector2(x - 70, y - 50).Length();
                if (d < 140) c.Set(x, y, new Color(0.75f, 0.72f, 0.85f, (1 - d / 140f) * 0.10f));
            }
        var sil = C("#120f19"); var sil2 = C("#181421");
        var rng = new Random(4);
        // back row, then front row of spires
        for (int pass = 0; pass < 2; pass++)
        {
            var col = pass == 0 ? sil2 : sil;
            int baseY = pass == 0 ? 250 : 290;
            for (int x0 = -20; x0 < W; x0 += 50 + rng.Next(40))
            {
                int w = 30 + rng.Next(40), h = 60 + rng.Next(pass == 0 ? 110 : 80);
                int x1 = Math.Min(x0 + w, W - 1);
                c.Rect(x0, baseY - h, x1 - x0, H - (baseY - h), col);
                int mid = (x0 + x1) / 2;
                c.PolyFlat(new[] { new Vector2(x0 - 2, baseY - h), new Vector2(x1 + 2, baseY - h), new Vector2(mid, baseY - h - 30 - rng.Next(30)) }, col);
                if (rng.Next(3) == 0) c.Line(new Vector2(mid, baseY - h - 40), new Vector2(mid, baseY - h - 60), col);
                // windows
                for (int k = 0; k < 2; k++)
                    if (rng.Next(3) > 0)
                    {
                        int wx = x0 + 6 + rng.Next(Math.Max(1, w - 12)), wy = baseY - h + 15 + rng.Next(Math.Max(1, h - 30));
                        var glow = pass == 0 ? new Color(0.85f, 0.55f, 0.28f, 0.35f) : new Color(0.95f, 0.62f, 0.3f, 0.6f);
                        c.Rect(wx, wy, 2, 4, glow); c.Set(wx, wy - 1, glow * new Color(1, 1, 1, 0.6f));
                    }
            }
        }
        return c.ToImage();
    }

    /// <summary>Middle layer: colonnade of pointed arches, dark silhouettes with a faint rim light.</summary>
    public static Image MidLayer()
    {
        int W = 640, H = 360;
        var c = new PixelCanvas(W, H);
        var body = C("#1a1822"); var rim = C("#2c2a38");
        // upper wall band with arches cut in
        c.Rect(0, 0, W, 120, body);
        for (int cx = 40; cx < W; cx += 80)
            ArchShape(c, cx, 30, 40, 90, 120, (x, y) => c.Clear(x, y));
        for (int x = 0; x < W; x += 80)
        {
            c.Rect(x - 7, 100, 14, 260, body);             // column shaft
            c.Rect(x - 10, 96, 20, 6, rim);                 // capital
            c.Rect(x - 7, 102, 2, 258, rim);                // lit edge (top-left light)
            c.Rect(x - 11, 290, 22, 8, body);               // base
        }
        // lit edge on the arch band
        for (int x = 0; x < W; x++) for (int y = 0; y < 120; y++)
                if (c.Opaque(x, y) && !c.Opaque(x - 1, y)) c.Set(x, y, rim);
        return c.ToImage();
    }

    /// <summary>Foreground: hanging chains and a big dark column, almost black (parallax faster than the world).</summary>
    public static Image ForeLayer()
    {
        int W = 640, H = 360;
        var c = new PixelCanvas(W, H);
        var dark = C("#07060a"); var link = C("#16141c");
        foreach (var (x, len) in new[] { (90, 120), (130, 70), (420, 150) })
        {
            for (int y = 0; y < len; y += 6)
            {
                bool even = (y / 6) % 2 == 0;
                if (even) { c.Rect(x - 1, y, 3, 5, dark); c.Set(x, y + 1, link); c.Set(x, y + 3, link); }
                else { c.Rect(x, y, 1, 6, dark); }
            }
            // weight / hook at the end
            c.Circle(new Vector2(x + 0.5f, len + 4), 4, new Ramp(link, dark, dark, dark));
        }
        // column
        c.Rect(560, 0, 46, H, dark);
        c.Rect(554, 330, 58, 30, dark);
        c.Rect(560, 0, 2, H, link);
        return c.ToImage();
    }

    /// <summary>Soft drifting fog band, wraps horizontally.</summary>
    public static Image Fog()
    {
        int W = 640, H = 120;
        var c = new PixelCanvas(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float n = PixelCanvas.Fbm(x / 64f, y / 32f, 10, 9);
                float fall = MathF.Sin(y / (float)H * MathF.PI);
                float a = MathF.Max(0, n - 0.42f) * 0.9f * fall;
                // stepped alpha keeps it "pixel" instead of a smooth blur
                a = MathF.Floor(a * 8) / 8f * 0.5f;
                if (a > 0) c.Set(x, y, new Color(0.62f, 0.6f, 0.72f, a));
            }
        return c.ToImage();
    }

    /// <summary>Floor tile: stone slabs with a lit top lip, fading into darkness.</summary>
    public static Image FloorTile()
    {
        int W = 64, H = 64;
        var c = new PixelCanvas(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int slab = x / 32;
                float v = PixelCanvas.Hash(slab, y / 16, 77);
                float l = 0.2f + v * 0.04f - y * 0.0028f;
                if (y < 2) l = 0.36f; else if (y == 2) l = 0.28f; else if (y == 3) l = 0.12f;
                if (x % 32 == 0 && y > 3) l = 0.08f;
                if (y > 3 && y % 16 == 0) l = 0.09f;
                if (PixelCanvas.Hash(x, y, 78) > 0.95f) l -= 0.03f;
                c.Set(x, y, Color.FromHsv(0.7f, 0.1f, MathF.Max(0.04f, l)));
            }
        return c.ToImage();
    }

    public static Image Ledge()
    {
        var c = new PixelCanvas(96, 14);
        var stone = new Ramp("#5b5866", "#3e3c48", "#2a2832", "#17161c");
        c.Poly(new[] { new Vector2(0, 0), new Vector2(96, 0), new Vector2(92, 13), new Vector2(4, 13) }, stone, 0.1f, 0.9f);
        c.Rect(0, 0, 96, 1, stone.Hi);
        for (int x = 16; x < 96; x += 24) c.Line(new Vector2(x, 2), new Vector2(x, 12), stone.Deep);
        c.Outline(Outline);
        return c.ToImage();
    }

    /// <summary>Gothic column for the play layer (behind the hero).</summary>
    public static Image Pillar()
    {
        int W = 44, H = 300;
        var c = new PixelCanvas(W, H);
        var stone = new Ramp("#4a4756", "#33313d", "#23222b", "#141319");
        for (int y = 24; y < H - 22; y++)
            for (int x = 8; x < W - 8; x++)
            {
                float u = (x - 8) / (float)(W - 16) * 2 - 1;  // -1..1 across the shaft
                var col = PixelCanvas.Shade(stone, -u * 1.1f + 0.1f);
                if ((x - 8) % 7 == 0) col = stone.Deep;      // fluting
                c.Set(x, y, col);
            }
        c.Poly(new[] { new Vector2(2, 0), new Vector2(42, 0), new Vector2(38, 14), new Vector2(34, 24), new Vector2(10, 24), new Vector2(6, 14) }, stone, 0.1f, 1f);
        c.Poly(new[] { new Vector2(6, H - 22), new Vector2(38, H - 22), new Vector2(42, H - 8), new Vector2(44, H), new Vector2(0, H), new Vector2(2, H - 8) }, stone, 0.1f, 1f);
        c.Rect(2, 0, 40, 1, stone.Hi);
        c.Outline(Outline);
        return c.ToImage();
    }

    /// <summary>Wall candelabrum, 4 flame frames side by side (24x40 each).</summary>
    public static Image Candles()
    {
        int fw = 24, fh = 40, n = 4;
        var c = new PixelCanvas(fw * n, fh);
        var iron = new Ramp("#4a4552", "#2e2a35", "#1d1a22", "#0f0d12");
        var wax = new Ramp("#efe4c8", "#cdbd98", "#a08f6c", "#5e5240");
        for (int f = 0; f < n; f++)
        {
            int ox = f * fw;
            // bracket
            c.Rect(ox + 4, 30, 16, 2, iron.Mid); c.Rect(ox + 11, 30, 2, 9, iron.Mid); c.Rect(ox + 9, 37, 6, 2, iron.Lo);
            int[] xs = { 6, 12, 18 }; int[] hs = { 10, 14, 8 };
            for (int k = 0; k < 3; k++)
            {
                int x = ox + xs[k], h = hs[k];
                c.Capsule(new Vector2(x, 29 - h), new Vector2(x, 29), 1.6f, wax);
                // wax drip
                if (k == 1) c.Set(x + 1, 29 - h + 3, wax.Hi);
                // flame: wobble per frame + per candle
                float wob = MathF.Sin((f + k * 1.7f) * 1.6f);
                int fx = x + (wob > 0.4f ? 1 : wob < -0.4f ? -1 : 0);
                int fy = 29 - h - 2;
                int len = 4 + ((f + k) % 2);
                c.Ellipse(new Vector2(fx + 0.5f, fy - len * 0.4f), 1.8f, len * 0.7f, C("#ff8a2a"));
                c.Ellipse(new Vector2(fx + 0.5f, fy - len * 0.3f), 1.0f, len * 0.45f, C("#ffd27a"));
                c.Set(fx, fy, C("#fff4d6"));
            }
        }
        return c.ToImage();
    }

    /// <summary>Checkpoint altar: frame 0 unlit, frame 1 lit (56x48 each).</summary>
    public static Image Altar()
    {
        int fw = 56, fh = 48;
        var c = new PixelCanvas(fw * 2, fh);
        var stone = new Ramp("#6a6676", "#47444f", "#2f2d37", "#19181e");
        var wax = new Ramp("#efe4c8", "#cdbd98", "#a08f6c", "#5e5240");
        for (int f = 0; f < 2; f++)
        {
            int ox = f * fw;
            c.Poly(new[] { new Vector2(ox + 6, 24), new Vector2(ox + 50, 24), new Vector2(ox + 46, 48), new Vector2(ox + 10, 48) }, stone, 0.05f, 1f);
            c.Rect(ox + 3, 20, 50, 5, stone.Mid); c.Rect(ox + 3, 20, 50, 1, stone.Hi);
            // carved sigil: a sword inside a circle
            var cen = new Vector2(ox + 28, 35);
            for (int a = 0; a < 40; a++) { float r = a / 40f * MathF.Tau; c.Set((int)(cen.X + MathF.Cos(r) * 6), (int)(cen.Y + MathF.Sin(r) * 6), f == 1 ? C("#f2b45a") : stone.Deep); }
            c.Line(cen + new Vector2(0, -7), cen + new Vector2(0, 6), f == 1 ? C("#ffd890") : stone.Deep);
            c.Line(cen + new Vector2(-3, -3), cen + new Vector2(3, -3), f == 1 ? C("#ffd890") : stone.Deep);
            // candles
            foreach (var (x, h) in new[] { (12, 8), (18, 12), (40, 10), (45, 6) })
            {
                c.Capsule(new Vector2(ox + x, 19 - h), new Vector2(ox + x, 19), 1.6f, wax);
                if (f == 1)
                {
                    c.Ellipse(new Vector2(ox + x + 0.5f, 19 - h - 3), 1.6f, 2.8f, C("#ff8a2a"));
                    c.Set(ox + x, 19 - h - 2, C("#fff0c8"));
                }
                else c.Set(ox + x, 19 - h - 1, C("#2a2622"));
            }
        }
        c.Outline(Outline);
        return c.ToImage();
    }

    public static Image Dot(int size, Color col)
    {
        var c = new PixelCanvas(size, size);
        c.Rect(0, 0, size, size, col);
        return c.ToImage();
    }

    public static Image Spark()
    {
        var c = new PixelCanvas(7, 7);
        var w = C("#fff6dc");
        c.Rect(3, 0, 1, 7, w); c.Rect(0, 3, 7, 1, w); c.Set(3, 3, C("#ffffff"));
        return c.ToImage();
    }
}
