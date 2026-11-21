using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClashOfThrones.Art;

/// <summary>
/// Turns AI-generated (or painted) high-resolution images into clean game pixel art:
/// background removal, crop, downscale to the pixel grid, palette quantize, 1 px outline, frame placement.
/// Run headless: godot --headless --path src -- --art-clean=&lt;in.png&gt; --out=&lt;out.png&gt; [--height=96] [--colors=28]
/// Rig restyle:  -- --art-restyle=&lt;dir with frame_*.png&gt; --out=&lt;sheet.png&gt; (uses the original rig silhouettes as masks)
/// </summary>
public static class ArtClean
{
    // ------------------------------------------------------------------ background removal
    /// <summary>Flood-fills from the image border, clearing pixels close to the border colour.</summary>
    public static void RemoveBackground(Image img, float tolerance = 0.12f)
    {
        int w = img.GetWidth(), h = img.GetHeight();
        var bg = AverageBorder(img);
        var seen = new bool[w * h];
        var q = new Queue<(int, int)>();
        for (int x = 0; x < w; x++) { q.Enqueue((x, 0)); q.Enqueue((x, h - 1)); }
        for (int y = 0; y < h; y++) { q.Enqueue((0, y)); q.Enqueue((w - 1, y)); }
        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            if (x < 0 || y < 0 || x >= w || y >= h || seen[y * w + x]) continue;
            seen[y * w + x] = true;
            var c = img.GetPixel(x, y);
            if (Dist(c, bg) > tolerance && c.A > 0.05f) continue;
            img.SetPixel(x, y, new Color(0, 0, 0, 0));
            q.Enqueue((x + 1, y)); q.Enqueue((x - 1, y)); q.Enqueue((x, y + 1)); q.Enqueue((x, y - 1));
        }
    }

    static Color AverageBorder(Image img)
    {
        int w = img.GetWidth(), h = img.GetHeight(); float r = 0, g = 0, b = 0; int n = 0;
        for (int x = 0; x < w; x += 4) foreach (int y in new[] { 0, h - 1 }) { var c = img.GetPixel(x, y); r += c.R; g += c.G; b += c.B; n++; }
        for (int y = 0; y < h; y += 4) foreach (int x in new[] { 0, w - 1 }) { var c = img.GetPixel(x, y); r += c.R; g += c.G; b += c.B; n++; }
        return new Color(r / n, g / n, b / n);
    }

    static float Dist(Color a, Color b)
    {
        float dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return MathF.Sqrt(dr * dr * 0.3f + dg * dg * 0.59f + db * db * 0.11f) * 1.6f;
    }

    public static Rect2I OpaqueBounds(Image img)
    {
        int w = img.GetWidth(), h = img.GetHeight(), x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                if (img.GetPixel(x, y).A > 0.5f) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
        return x1 < 0 ? new Rect2I(0, 0, w, h) : new Rect2I(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    // ------------------------------------------------------------------ downscale
    /// <summary>Box-filter downscale that averages only opaque pixels (keeps edges crisp, no dark fringes).</summary>
    public static Image Downscale(Image src, int tw, int th)
    {
        int sw = src.GetWidth(), sh = src.GetHeight();
        var dst = Image.CreateEmpty(tw, th, false, Image.Format.Rgba8);
        for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                int ax = x * sw / tw, bx = Math.Max(ax + 1, (x + 1) * sw / tw), ay = y * sh / th, by = Math.Max(ay + 1, (y + 1) * sh / th);
                float r = 0, g = 0, b = 0; int n = 0, total = 0;
                for (int yy = ay; yy < by; yy++) for (int xx = ax; xx < bx; xx++)
                    {
                        total++;
                        var c = src.GetPixel(xx, yy);
                        if (c.A < 0.5f) continue;
                        r += c.R; g += c.G; b += c.B; n++;
                    }
                if (n * 2 >= total) dst.SetPixel(x, y, new Color(r / n, g / n, b / n, 1));
            }
        return dst;
    }

    // ------------------------------------------------------------------ palette
    /// <summary>k-means palette from the opaque pixels of one or more images.</summary>
    public static List<Color> BuildPalette(IEnumerable<Image> imgs, int k)
    {
        var px = new List<Vector3>();
        foreach (var img in imgs)
            for (int y = 0; y < img.GetHeight(); y++) for (int x = 0; x < img.GetWidth(); x++)
                { var c = img.GetPixel(x, y); if (c.A > 0.5f) px.Add(new Vector3(c.R, c.G, c.B)); }
        if (px.Count == 0) return new List<Color> { Colors.Black };
        var rng = new Random(1);
        var cent = Enumerable.Range(0, k).Select(_ => px[rng.Next(px.Count)]).ToArray();
        var assign = new int[px.Count];
        for (int it = 0; it < 12; it++)
        {
            for (int i = 0; i < px.Count; i++)
            {
                int best = 0; float bd = float.MaxValue;
                for (int j = 0; j < k; j++) { float d = (px[i] - cent[j]).LengthSquared(); if (d < bd) { bd = d; best = j; } }
                assign[i] = best;
            }
            var sum = new Vector3[k]; var cnt = new int[k];
            for (int i = 0; i < px.Count; i++) { sum[assign[i]] += px[i]; cnt[assign[i]]++; }
            for (int j = 0; j < k; j++) cent[j] = cnt[j] > 0 ? sum[j] / cnt[j] : px[rng.Next(px.Count)];
        }
        return cent.Select(v => new Color(v.X, v.Y, v.Z)).Distinct().ToList();
    }

    public static void Quantize(Image img, List<Color> pal)
    {
        for (int y = 0; y < img.GetHeight(); y++)
            for (int x = 0; x < img.GetWidth(); x++)
            {
                var c = img.GetPixel(x, y);
                if (c.A < 0.5f) { img.SetPixel(x, y, new Color(0, 0, 0, 0)); continue; }
                Color best = pal[0]; float bd = float.MaxValue;
                foreach (var p in pal) { float d = Dist(c, p); if (d < bd) { bd = d; best = p; } }
                img.SetPixel(x, y, best);
            }
    }

    public static void Outline(Image img, Color col)
    {
        int w = img.GetWidth(), h = img.GetHeight();
        var copy = (Image)img.Duplicate();
        bool Op(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && copy.GetPixel(x, y).A > 0.5f;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                if (!Op(x, y) && (Op(x - 1, y) || Op(x + 1, y) || Op(x, y - 1) || Op(x, y + 1))) img.SetPixel(x, y, col);
    }

    /// <summary>Removes single stray pixels (noise left by the AI image).</summary>
    public static void Despeckle(Image img)
    {
        int w = img.GetWidth(), h = img.GetHeight();
        var copy = (Image)img.Duplicate();
        for (int y = 1; y < h - 1; y++) for (int x = 1; x < w - 1; x++)
            {
                if (copy.GetPixel(x, y).A < 0.5f) continue;
                int n = 0;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if ((dx != 0 || dy != 0) && copy.GetPixel(x + dx, y + dy).A > 0.5f) n++;
                if (n <= 1) img.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
    }

    static readonly Color OutlineColor = Color.FromHtml("#0b0a0f");

    // ------------------------------------------------------------------ pipelines
    /// <summary>One high-res image -> a character frame (feet at origin) of the game's frame size.</summary>
    public static Image CleanCharacter(Image src, int targetHeight, int colors, Vector2I frame, Vector2 origin)
    {
        src.Convert(Image.Format.Rgba8);
        RemoveBackground(src);
        var b = OpaqueBounds(src);
        var crop = src.GetRegion(b);
        int th = targetHeight, tw = Math.Max(1, (int)MathF.Round(b.Size.X * (float)th / b.Size.Y));
        var small = Downscale(crop, tw, th);
        Despeckle(small);
        Quantize(small, BuildPalette(new[] { small }, colors));
        var outImg = Image.CreateEmpty(frame.X, frame.Y, false, Image.Format.Rgba8);
        outImg.BlendRect(small, new Rect2I(0, 0, tw, th), new Vector2I((int)origin.X - tw / 2, (int)origin.Y - th + 1));
        Outline(outImg, OutlineColor);
        return outImg;
    }

    /// <summary>
    /// Restyled rig frames (AI img2img output, upscaled N times) -> a sprite sheet. Each frame is downscaled back
    /// to the rig frame size, cut with the original rig silhouette (so every frame keeps its exact shape and pose)
    /// and mapped to ONE shared palette (so colours stay consistent across the whole animation set).
    /// </summary>
    public static Image RestyleSheet(string dir, SpriteSheet rig, int colors, Color? background = null)
    {
        var bgCol = background ?? Color.FromHtml("#7a7680");
        var frames = new List<(int r, int f, Image img)>();
        for (int r = 0; r < rig.Defs.Length; r++)
            for (int f = 0; f < rig.Defs[r].Frames; f++)
            {
                string p = System.IO.Path.Combine(dir, $"{rig.Defs[r].Name}_{f}.png");
                if (!System.IO.File.Exists(p)) continue;
                var big = Image.LoadFromFile(p); big.Convert(Image.Format.Rgba8);
                var mask = rig.Image.GetRegion(new Rect2I(f * rig.FrameW, r * rig.FrameH, rig.FrameW, rig.FrameH));
                var small = Downscale(big, rig.FrameW, rig.FrameH);
                for (int y = 0; y < rig.FrameH; y++) for (int x = 0; x < rig.FrameW; x++)
                    {
                        var m = mask.GetPixel(x, y);
                        if (m.A < 0.5f) small.SetPixel(x, y, new Color(0, 0, 0, 0));
                        else if (small.GetPixel(x, y).A < 0.5f) small.SetPixel(x, y, m);   // hole -> keep the rig pixel
                        else if (Dist(small.GetPixel(x, y), bgCol) < 0.09f) small.SetPixel(x, y, m); // AI background leaked in -> rig pixel
                        else if (m.R + m.G + m.B < 0.12f) small.SetPixel(x, y, m);          // keep the rig outline
                    }
                frames.Add((r, f, small));
            }
        var pal = BuildPalette(frames.Select(t => t.img), colors);
        pal.Add(OutlineColor);
        var sheet = (Image)rig.Image.Duplicate();
        foreach (var (r, f, img) in frames)
        {
            Quantize(img, pal);
            sheet.FillRect(new Rect2I(f * rig.FrameW, r * rig.FrameH, rig.FrameW, rig.FrameH), new Color(0, 0, 0, 0));
            sheet.BlitRect(img, new Rect2I(0, 0, rig.FrameW, rig.FrameH), new Vector2I(f * rig.FrameW, r * rig.FrameH));
        }
        return sheet;
    }

    /// <summary>Writes every rig frame upscaled (nearest) on a flat background, ready for img2img.</summary>
    public static int ExportRigFrames(SpriteSheet rig, string dir, int scale, Color bg)
    {
        System.IO.Directory.CreateDirectory(dir);
        int n = 0;
        for (int r = 0; r < rig.Defs.Length; r++)
            for (int f = 0; f < rig.Defs[r].Frames; f++)
            {
                var fr = rig.Image.GetRegion(new Rect2I(f * rig.FrameW, r * rig.FrameH, rig.FrameW, rig.FrameH));
                var canvas = Image.CreateEmpty(rig.FrameW, rig.FrameH, false, Image.Format.Rgba8);
                canvas.Fill(bg);
                canvas.BlendRect(fr, new Rect2I(0, 0, rig.FrameW, rig.FrameH), Vector2I.Zero);
                canvas.Resize(rig.FrameW * scale, rig.FrameH * scale, Image.Interpolation.Nearest);
                canvas.SavePng(System.IO.Path.Combine(dir, $"{rig.Defs[r].Name}_{f}.png"));
                n++;
            }
        return n;
    }
}
