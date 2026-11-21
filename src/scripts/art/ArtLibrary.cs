using Godot;
using System;
using System.Collections.Generic;

namespace ClashOfThrones.Art;

/// <summary>
/// Single source of every texture. Uses an edited PNG from res://art/ when one exists (so artists can paint over the
/// baked placeholder in Pixelorama/Krita), otherwise generates the placeholder in memory.
/// </summary>
public static class ArtLibrary
{
    public const string ArtDir = "res://art/";
    static readonly Dictionary<string, Texture2D> Cache = new();
    static SpriteSheet _hero, _warden;

    public static readonly Vector2I HeroFrame = new(128, 112);
    public static readonly Vector2 HeroOrigin = new(64, 108);
    public static readonly Vector2I WardenFrame = new(160, 128);
    public static readonly Vector2 WardenOrigin = new(80, 124);

    static readonly Dictionary<string, Func<Image>> Generators = new()
    {
        ["bricks"] = EnvironmentArt.BrickTile,
        ["layer_far"] = EnvironmentArt.FarLayer,
        ["layer_mid"] = EnvironmentArt.MidLayer,
        ["layer_wall"] = EnvironmentArt.WallLayer,
        ["layer_fore"] = EnvironmentArt.ForeLayer,
        ["fog"] = EnvironmentArt.Fog,
        ["floor"] = EnvironmentArt.FloorTile,
        ["ledge"] = EnvironmentArt.Ledge,
        ["pillar"] = EnvironmentArt.Pillar,
        ["candles"] = EnvironmentArt.Candles,
        ["altar"] = EnvironmentArt.Altar,
        ["dust"] = () => EnvironmentArt.Dot(1, Colors.White),
        ["spark"] = EnvironmentArt.Spark,
        ["hero"] = () => Hero.Image,
        ["warden"] = () => Warden.Image,
    };

    public static IEnumerable<string> Names => Generators.Keys;

    public static SpriteSheet Hero => _hero ??= SpriteSheet.Bake(Animations.Hero, Animations.HeroPose, Style.Knight(), HeroFrame.X, HeroFrame.Y, HeroOrigin);
    public static SpriteSheet Warden => _warden ??= SpriteSheet.Bake(Animations.Warden, Animations.WardenPose, Style.Warden(), WardenFrame.X, WardenFrame.Y, WardenOrigin);

    public static Texture2D Get(string name)
    {
        if (Cache.TryGetValue(name, out var t)) return t;
        string path = ArtDir + name + ".png";
        if (ResourceLoader.Exists(path)) t = GD.Load<Texture2D>(path);
        else t = ImageTexture.CreateFromImage(Generators[name]());
        Cache[name] = t;
        return t;
    }

    public static SpriteFrames HeroFrames() => Hero.BuildFrames(Get("hero"));
    public static SpriteFrames WardenFrames() => Warden.BuildFrames(Get("warden"));

    /// <summary>Writes every generated placeholder as PNG into <paramref name="dir"/> (absolute path).</summary>
    public static int BakeAll(string dir)
    {
        System.IO.Directory.CreateDirectory(dir);
        int n = 0;
        foreach (var (name, gen) in Generators) { gen().SavePng(System.IO.Path.Combine(dir, name + ".png")); n++; }
        return n;
    }

    public static Texture2D RadialLight(int size = 256)
    {
        if (Cache.TryGetValue("__light" + size, out var t)) return t;
        var g = new Gradient();
        g.SetColor(0, new Color(1, 1, 1, 1)); g.SetColor(1, new Color(1, 1, 1, 0));
        g.AddPoint(0.35f, new Color(1, 1, 1, 0.55f));
        var tex = new GradientTexture2D
        {
            Gradient = g, Width = size, Height = size, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
        };
        Cache["__light" + size] = tex;
        return tex;
    }
}
