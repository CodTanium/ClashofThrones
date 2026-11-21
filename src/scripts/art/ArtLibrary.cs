using Godot;
using System;
using System.Collections.Generic;

namespace ClashOfThrones.Art;

/// <summary>
/// Single source of every texture. Uses an edited PNG from res://art/ when one exists (so artists can paint over the
/// baked placeholder in Pixelorama/Krita), otherwise generates the placeholder in memory. Character sheets are merged:
/// rows the PNG has come from the PNG, newer rows (added later) come from the generator.
/// </summary>
public static class ArtLibrary
{
    public const string ArtDir = "res://art/";
    static readonly Dictionary<string, Texture2D> Cache = new();
    static readonly Dictionary<string, SpriteSheet> Sheets = new();

    public static readonly Vector2I HeroFrame = new(128, 112);
    public static readonly Vector2 HeroOrigin = new(64, 108);
    public static readonly Vector2I WardenFrame = new(160, 128);
    public static readonly Vector2 WardenOrigin = new(80, 124);
    public static readonly Vector2I AbbotFrame = new(256, 208);
    public static readonly Vector2 AbbotOrigin = new(128, 204);

    /// <summary>name -> (defs, poser, style, frame, origin)</summary>
    static readonly Dictionary<string, Func<SpriteSheet>> SheetMakers = new()
    {
        ["hero"] = () => SpriteSheet.Bake(Animations.Hero, Animations.HeroPose, Style.Knight(), HeroFrame.X, HeroFrame.Y, HeroOrigin),
        ["warden"] = () => SpriteSheet.Bake(Animations.Warden, Animations.WardenPose, Style.Warden(), WardenFrame.X, WardenFrame.Y, WardenOrigin),
        ["ghoul"] = () => SpriteSheet.Bake(Animations.Ghoul, Animations.GhoulPose, Style.Ghoul(), HeroFrame.X, HeroFrame.Y, HeroOrigin),
        ["archer"] = () => SpriteSheet.Bake(Animations.Archer, Animations.ArcherPose, Style.Archer(), HeroFrame.X, HeroFrame.Y, HeroOrigin),
        ["abbot"] = () => SpriteSheet.Bake(Animations.Abbot, Animations.AbbotPose, Style.Abbot(), AbbotFrame.X, AbbotFrame.Y, AbbotOrigin),
    };

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
        ["terrain"] = LevelArt.Terrain,
        ["spikes"] = LevelArt.Spikes,
        ["crumble"] = LevelArt.Crumble,
        ["platform"] = LevelArt.MovingPlatform,
        ["ladder"] = LevelArt.Ladder,
        ["chest"] = LevelArt.Chest,
        ["lever"] = LevelArt.Lever,
        ["gate"] = LevelArt.Gate,
        ["door"] = LevelArt.Door,
        ["breakable"] = LevelArt.Breakable,
        ["pendulum"] = LevelArt.Pendulum,
        ["firejet"] = LevelArt.FireJet,
        ["pickup"] = LevelArt.Pickup,
        ["orb"] = LevelArt.Orb,
        ["remnant"] = LevelArt.Remnant,
        ["arrow"] = LevelArt.Arrow,
        ["wisp"] = LevelArt.Wisp,
        ["exit"] = LevelArt.ExitShrine,
        ["banner"] = LevelArt.Banner,
        ["chain_lamp"] = LevelArt.ChainLamp,
        ["bones"] = LevelArt.Bones,
    };

    public static IEnumerable<string> Names { get { foreach (var k in Generators.Keys) yield return k; foreach (var k in SheetMakers.Keys) yield return k; } }

    public static SpriteSheet Sheet(string name) => Sheets.TryGetValue(name, out var s) ? s : Sheets[name] = SheetMakers[name]();
    public static SpriteSheet Hero => Sheet("hero");
    public static SpriteSheet Warden => Sheet("warden");

    public static Texture2D Get(string name)
    {
        if (Cache.TryGetValue(name, out var t)) return t;
        string path = ArtDir + name + ".png";
        bool isSheet = SheetMakers.ContainsKey(name);
        if (isSheet)
        {
            var gen = Sheet(name).Image;
            if (ResourceLoader.Exists(path))
            {
                // painted rows on top of the generated sheet (new rows keep the generated look until painted)
                var painted = GD.Load<Texture2D>(path).GetImage();
                painted.Convert(Image.Format.Rgba8);
                var merged = (Image)gen.Duplicate();
                int h = Math.Min(painted.GetHeight(), merged.GetHeight()), w = Math.Min(painted.GetWidth(), merged.GetWidth());
                merged.FillRect(new Rect2I(0, 0, w, h), new Color(0, 0, 0, 0));
                merged.BlitRect(painted, new Rect2I(0, 0, w, h), Vector2I.Zero);
                t = ImageTexture.CreateFromImage(merged);
            }
            else t = ImageTexture.CreateFromImage(gen);
        }
        else if (ResourceLoader.Exists(path)) t = GD.Load<Texture2D>(path);
        else t = ImageTexture.CreateFromImage(Generators[name]());
        Cache[name] = t;
        return t;
    }

    public static SpriteFrames Frames(string sheet) => Sheet(sheet).BuildFrames(Get(sheet));
    public static SpriteFrames HeroFrames() => Frames("hero");
    public static SpriteFrames WardenFrames() => Frames("warden");

    /// <summary>Writes every generated placeholder as PNG into <paramref name="dir"/> (absolute path).</summary>
    public static int BakeAll(string dir)
    {
        System.IO.Directory.CreateDirectory(dir);
        int n = 0;
        foreach (var (name, gen) in Generators) { gen().SavePng(System.IO.Path.Combine(dir, name + ".png")); n++; }
        foreach (var name in SheetMakers.Keys) { Sheet(name).Image.SavePng(System.IO.Path.Combine(dir, name + ".png")); n++; }
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
