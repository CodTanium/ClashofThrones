using Godot;
using System;

namespace ClashOfThrones.Art;

/// <summary>Four-step colour ramp (highlight, base, shadow, deep shadow). Light always comes from the top left.</summary>
public readonly struct Ramp
{
    public readonly Color Hi, Mid, Lo, Deep;
    public Ramp(string hi, string mid, string lo, string deep)
    {
        Hi = Color.FromHtml(hi); Mid = Color.FromHtml(mid); Lo = Color.FromHtml(lo); Deep = Color.FromHtml(deep);
    }
    public Ramp(Color hi, Color mid, Color lo, Color deep) { Hi = hi; Mid = mid; Lo = lo; Deep = deep; }
    public Ramp Darker(float f) => new(Hi.Darkened(f), Mid.Darkened(f), Lo.Darkened(f), Deep.Darkened(f * 0.5f));
}

/// <summary>A small software canvas for building pixel art in code. RGBA8, no anti-aliasing.</summary>
public sealed class PixelCanvas
{
    public readonly int W, H;
    readonly byte[] _px;
    public static readonly Vector2 Light = new Vector2(-0.6f, -0.8f).Normalized();

    public PixelCanvas(int w, int h) { W = w; H = h; _px = new byte[w * h * 4]; }

    public byte[] Data => _px;
    public Image ToImage() => Image.CreateFromData(W, H, false, Image.Format.Rgba8, _px);

    public Color Get(int x, int y)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return new Color(0, 0, 0, 0);
        int i = (y * W + x) * 4;
        return Color.Color8(_px[i], _px[i + 1], _px[i + 2], _px[i + 3]);
    }

    public bool Opaque(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && _px[(y * W + x) * 4 + 3] > 0;

    public void Set(int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= W || y >= H || c.A <= 0f) return;
        int i = (y * W + x) * 4;
        if (c.A < 1f && _px[i + 3] > 0)
        {
            var d = Color.Color8(_px[i], _px[i + 1], _px[i + 2], _px[i + 3]);
            c = d.Blend(c);
        }
        _px[i] = (byte)Mathf.RoundToInt(c.R * 255); _px[i + 1] = (byte)Mathf.RoundToInt(c.G * 255);
        _px[i + 2] = (byte)Mathf.RoundToInt(c.B * 255); _px[i + 3] = (byte)Mathf.RoundToInt(c.A * 255);
    }

    public void Clear(int x, int y)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return;
        int i = (y * W + x) * 4; _px[i] = _px[i + 1] = _px[i + 2] = _px[i + 3] = 0;
    }

    public static Color Shade(in Ramp r, float v) => v > 0.45f ? r.Hi : v > -0.2f ? r.Mid : v > -0.65f ? r.Lo : r.Deep;

    /// <summary>Filled capsule (thick line) with cylinder shading.</summary>
    public void Capsule(Vector2 a, Vector2 b, float r, in Ramp ramp, float bias = 0f)
    {
        int x0 = (int)MathF.Floor(MathF.Min(a.X, b.X) - r - 1), x1 = (int)MathF.Ceiling(MathF.Max(a.X, b.X) + r + 1);
        int y0 = (int)MathF.Floor(MathF.Min(a.Y, b.Y) - r - 1), y1 = (int)MathF.Ceiling(MathF.Max(a.Y, b.Y) + r + 1);
        var ab = b - a; float len2 = ab.LengthSquared();
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float t = len2 < 1e-4f ? 0f : Mathf.Clamp((p - a).Dot(ab) / len2, 0f, 1f);
                var d = p - (a + ab * t);
                if (d.LengthSquared() > r * r) continue;
                var n = d / MathF.Max(r, 0.01f);
                Set(x, y, Shade(ramp, n.Dot(Light) * 1.3f + bias));
            }
    }

    public void Circle(Vector2 c, float r, in Ramp ramp, float bias = 0f) => Capsule(c, c, r, ramp, bias);

    public void Ellipse(Vector2 c, float rx, float ry, Color col)
    {
        for (int y = (int)(c.Y - ry - 1); y <= (int)(c.Y + ry + 1); y++)
            for (int x = (int)(c.X - rx - 1); x <= (int)(c.X + rx + 1); x++)
            {
                float dx = (x + 0.5f - c.X) / rx, dy = (y + 0.5f - c.Y) / ry;
                if (dx * dx + dy * dy <= 1f) Set(x, y, col);
            }
    }

    static bool Inside(Vector2[] pts, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
        {
            if ((pts[i].Y > p.Y) != (pts[j].Y > p.Y) &&
                p.X < (pts[j].X - pts[i].X) * (p.Y - pts[i].Y) / (pts[j].Y - pts[i].Y) + pts[i].X) inside = !inside;
        }
        return inside;
    }

    /// <summary>Filled polygon, shaded by position relative to its centre (top left lit).</summary>
    public void Poly(Vector2[] pts, in Ramp ramp, float bias = 0f, float strength = 1.3f)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        var c = Vector2.Zero;
        foreach (var p in pts) { minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y); maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y); c += p; }
        c /= pts.Length;
        float ext = MathF.Max(1f, MathF.Max(maxX - minX, maxY - minY) * 0.5f);
        for (int y = (int)minY; y <= (int)maxY + 1; y++)
            for (int x = (int)minX; x <= (int)maxX + 1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                if (!Inside(pts, p)) continue;
                Set(x, y, Shade(ramp, (p - c).Dot(Light) / ext * strength + bias));
            }
    }

    public void PolyFlat(Vector2[] pts, Color col)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in pts) { minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y); maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y); }
        for (int y = (int)minY; y <= (int)maxY + 1; y++)
            for (int x = (int)minX; x <= (int)maxX + 1; x++)
                if (Inside(pts, new Vector2(x + 0.5f, y + 0.5f))) Set(x, y, col);
    }

    public void Rect(int x, int y, int w, int h, Color c)
    {
        for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Set(i, j, c);
    }

    public void Line(Vector2 a, Vector2 b, Color c)
    {
        int x0 = (int)MathF.Floor(a.X), y0 = (int)MathF.Floor(a.Y), x1 = (int)MathF.Floor(b.X), y1 = (int)MathF.Floor(b.Y);
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            Set(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// <summary>1 px outline around every opaque area (selective: outline is a very dark tint, not pure black).</summary>
    public void Outline(Color c)
    {
        var src = (byte[])_px.Clone();
        bool Op(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && src[(y * W + x) * 4 + 3] > 0;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (!Op(x, y) && (Op(x - 1, y) || Op(x + 1, y) || Op(x, y - 1) || Op(x, y + 1))) Set(x, y, c);
    }

    public void Blit(PixelCanvas src, int ox, int oy)
    {
        for (int y = 0; y < src.H; y++) for (int x = 0; x < src.W; x++) { var c = src.Get(x, y); if (c.A > 0) Set(ox + x, oy + y, c); }
    }

    // ---------- deterministic noise ----------
    public static float Hash(int x, int y, int seed = 0)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
        }
    }

    /// <summary>Smooth value noise that wraps every <paramref name="period"/> cells horizontally.</summary>
    public static float ValueNoise(float x, float y, int period, int seed)
    {
        int xi = (int)MathF.Floor(x), yi = (int)MathF.Floor(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        int Wrap(int v) => ((v % period) + period) % period;
        float a = Hash(Wrap(xi), yi, seed), b = Hash(Wrap(xi + 1), yi, seed), c = Hash(Wrap(xi), yi + 1, seed), d = Hash(Wrap(xi + 1), yi + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    public static float Fbm(float x, float y, int period, int seed, int octaves = 4)
    {
        float sum = 0, amp = 0.5f, norm = 0;
        for (int o = 0; o < octaves; o++)
        {
            sum += ValueNoise(x, y, period, seed + o * 31) * amp; norm += amp;
            x *= 2; y *= 2; period *= 2; amp *= 0.5f;
        }
        return sum / norm;
    }
}
