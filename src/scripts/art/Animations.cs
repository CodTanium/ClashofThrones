using Godot;
using System;
using System.Collections.Generic;

namespace ClashOfThrones.Art;

public sealed record AnimDef(string Name, int Frames, float Fps, bool Loop);

/// <summary>Frame lists and poses for every character animation. One row per animation in the sprite sheet.</summary>
public static class Animations
{
    public static readonly AnimDef[] Hero =
    {
        new("idle", 6, 7, true), new("run", 8, 13, true), new("jump", 2, 10, false), new("fall", 2, 8, true),
        new("land", 2, 18, false), new("attack1", 5, 18, false), new("attack2", 5, 18, false), new("attack3", 6, 15, false),
        new("dodge", 5, 15, false), new("parry", 4, 14, false), new("hurt", 2, 10, false), new("death", 7, 9, false),
    };

    public static readonly AnimDef[] Warden =
    {
        new("idle", 4, 5, true), new("walk", 6, 7, true), new("windup", 4, 6, false), new("attack", 4, 14, false),
        new("recover", 3, 5, false), new("hurt", 2, 10, false), new("stagger", 4, 6, true), new("death", 6, 8, false),
    };

    const float Tau = MathF.PI * 2f;

    static Pose Stand() => new();

    static Pose Leg(Pose p, float phase, float swing, float lift)
    {
        // front leg uses phase, back leg phase + pi. Knee bends while the leg swings forward.
        p.ThighF = swing * MathF.Sin(phase); p.KneeF = 8 + lift * MathF.Max(0, MathF.Cos(phase));
        p.ThighB = swing * MathF.Sin(phase + MathF.PI); p.KneeB = 8 + lift * MathF.Max(0, MathF.Cos(phase + MathF.PI));
        return p;
    }

    public static Pose HeroPose(string anim, int i, int n)
    {
        float t = n <= 1 ? 0 : i / (float)n;
        switch (anim)
        {
            case "idle":
            {
                float b = MathF.Sin(t * Tau);
                return new Pose
                {
                    Lean = 4, ThighF = 10, KneeF = 10 + 4 * b, ThighB = -10, KneeB = 6 + 4 * b,
                    ArmF = 18 + 2 * b, ElbowF = 22, Weapon = 58 + 2 * b, ArmB = -8, ElbowB = 34 + 2 * b,
                    CapeBack = 6, CapeDrop = 27, CapeWave = t,
                };
            }
            case "run":
            {
                float ph = t * Tau;
                var p = Leg(new Pose { Lean = 13 }, ph, 34, 46);
                p.ArmF = 28 - 26 * MathF.Sin(ph); p.ElbowF = 30; p.Weapon = p.ArmF + 62;
                p.ArmB = -10 + 22 * MathF.Sin(ph); p.ElbowB = 40;
                p.CapeBack = 16 + 3 * MathF.Sin(ph * 2); p.CapeDrop = 21; p.CapeWave = t * 2;
                return p;
            }
            case "jump":
                return new Pose
                {
                    Grounded = false, RootY = -6 - i * 2, Lean = 8, ThighF = 45, KneeF = 70, ThighB = 5, KneeB = 45,
                    ArmF = 70 + i * 20, ElbowF = 20, Weapon = 130 + i * 15, ArmB = -35, ElbowB = 40, CapeBack = 3, CapeDrop = 31,
                };
            case "fall":
                return new Pose
                {
                    Grounded = false, RootY = -8, Lean = 6, ThighF = 22 + i * 4, KneeF = 25, ThighB = -12, KneeB = 35,
                    ArmF = 85 + i * 5, ElbowF = 15, Weapon = 115, ArmB = -55 - i * 5, ElbowB = 30, CapeBack = 9, CapeDrop = 15, CapeWave = i * 0.5f,
                };
            case "land":
                return new Pose
                {
                    Lean = 20 - i * 8, ThighF = 45 - i * 15, KneeF = 80 - i * 30, ThighB = -15, KneeB = 60 - i * 25,
                    ArmF = 40, ElbowF = 30, Weapon = 80, ArmB = -20, ElbowB = 40, CapeBack = 5, CapeDrop = 22,
                };
            case "attack1": return Attack(i, AttackKeys1);
            case "attack2": return Attack(i, AttackKeys2);
            case "attack3": return Attack(i, AttackKeys3);
            case "dodge":
            {
                float lowT = i < 4 ? 1f : 0.35f;
                var p = new Pose
                {
                    Lean = 38 * lowT, ThighF = 65 * lowT, KneeF = 95 * lowT, ThighB = -45 * lowT, KneeB = 50 * lowT,
                    ArmF = 30, ElbowF = 40, Weapon = 290, ArmB = -50, ElbowB = 30,
                    CapeBack = 26 * lowT + 6, CapeDrop = 10 + 14 * (1 - lowT), CapeWave = t * 2, HeadTilt = 10,
                };
                return p;
            }
            case "parry":
            {
                float raise = i == 0 ? 0.5f : i == 3 ? 0.4f : 1f;
                return new Pose
                {
                    Lean = 6 + 4 * raise, ThighF = 25, KneeF = 18, ThighB = -22, KneeB = 10,
                    ArmF = -15, ElbowF = 25, Weapon = 25, ArmB = 20 + 48 * raise, ElbowB = 25, ShieldFront = raise > 0.45f,
                    CapeBack = 8, CapeDrop = 25,
                };
            }
            case "hurt":
                return new Pose
                {
                    Lean = -14 - i * 4, ThighF = -8, KneeF = 12, ThighB = -20, KneeB = 8,
                    ArmF = -15, ElbowF = 30, Weapon = 30, ArmB = -45, ElbowB = 20, CapeBack = 12, CapeDrop = 20, HeadTilt = -10,
                };
            case "death":
            {
                var hurt = HeroPose("hurt", 1, 2);
                var kneel = new Pose { Lean = 15, ThighF = 85, KneeF = 160, ThighB = 5, KneeB = 120, ArmF = 5, ElbowF = 10, Weapon = 15, ArmB = -10, ElbowB = 20, CapeBack = 4, CapeDrop = 18 };
                var slump = kneel.Clone(); slump.Lean = 45; slump.ArmF = 0; slump.Weapon = 5;
                var down = new Pose { Lean = 0, ThighF = 0, KneeF = 0, ThighB = -5, KneeB = 5, ArmF = 100, ElbowF = 0, Weapon = 110, ArmB = 60, ElbowB = 10, CapeBack = 2, CapeDrop = 30, Rot = 88, ShiftX = -40, ShiftY = -4 };
                return i switch
                {
                    0 => hurt,
                    1 => Pose.Lerp(hurt, kneel, 0.6f),
                    2 => kneel,
                    3 => slump,
                    4 => Pose.Lerp(slump, down, 0.45f),
                    5 => Pose.Lerp(slump, down, 0.8f),
                    _ => down,
                };
            }
        }
        return Stand();
    }

    // Attack keyframes: arm angle, weapon angle, lean, lunge, body stance.
    static readonly (float arm, float elbow, float weapon, float lean, float lunge)[] AttackKeys1 =
        { (200, 30, 225, 0, 0), (165, 20, 170, 6, 1), (85, 5, 85, 18, 5), (45, 10, 35, 20, 6), (30, 20, 55, 10, 3) };
    static readonly (float arm, float elbow, float weapon, float lean, float lunge)[] AttackKeys2 =
        { (15, 25, 15, 14, 2), (55, 10, 95, 8, 4), (135, 5, 160, 0, 6), (165, 10, 195, -4, 6), (70, 25, 95, 6, 3) };
    static readonly (float arm, float elbow, float weapon, float lean, float lunge)[] AttackKeys3 =
        { (205, 30, 235, -6, -2), (195, 25, 215, -8, -3), (140, 10, 140, 10, 4), (60, 0, 50, 28, 10), (30, 5, 18, 30, 10), (40, 20, 50, 14, 6) };

    static Pose Attack(int i, (float arm, float elbow, float weapon, float lean, float lunge)[] keys)
    {
        var k = keys[Math.Min(i, keys.Length - 1)];
        return new Pose
        {
            Lean = k.lean, RootX = k.lunge, ThighF = 30 + k.lunge, KneeF = 22 + k.lunge * 1.5f, ThighB = -26 - k.lunge, KneeB = 8,
            ArmF = k.arm, ElbowF = k.elbow, Weapon = k.weapon, ArmB = -25, ElbowB = 45, CapeBack = 10 + k.lunge, CapeDrop = 23, CapeWave = i * 0.2f,
        };
    }

    public static Pose WardenPose(string anim, int i, int n)
    {
        float t = n <= 1 ? 0 : i / (float)n;
        switch (anim)
        {
            case "idle":
            {
                float b = MathF.Sin(t * Tau);
                return new Pose { Lean = 24, ThighF = 15, KneeF = 25 + 4 * b, ThighB = -15, KneeB = 15 + 4 * b, ArmF = 25, ElbowF = 15, Weapon = 40 + 3 * b, ArmB = 5, ElbowB = 20 };
            }
            case "walk":
            {
                float ph = t * Tau;
                var p = Leg(new Pose { Lean = 26 }, ph, 22, 30);
                p.ArmF = 25 - 10 * MathF.Sin(ph); p.ElbowF = 15; p.Weapon = 40; p.ArmB = 5 + 15 * MathF.Sin(ph); p.ElbowB = 20;
                return p;
            }
            case "windup":
            {
                float u = (i + 1) / (float)n;
                return new Pose { Lean = 24 - 30 * u, ThighF = 25, KneeF = 25, ThighB = -25, KneeB = 15, ArmF = 25 + 185 * u, ElbowF = 15, Weapon = 40 + 195 * u, ArmB = 20 - 40 * u, ElbowB = 30, Flash = i == n - 1 ? 1 : 0 };
            }
            case "attack":
            {
                var keys = new (float a, float w, float l, float x)[] { (190, 210, -2, 2), (90, 90, 25, 10), (40, 25, 35, 12), (30, 15, 35, 12) };
                var k = keys[Math.Min(i, 3)];
                return new Pose { Lean = k.l, RootX = k.x, ThighF = 35, KneeF = 30, ThighB = -30, KneeB = 10, ArmF = k.a, ElbowF = 5, Weapon = k.w, ArmB = -20, ElbowB = 30 };
            }
            case "recover":
                return new Pose { Lean = 35 - i * 4, RootX = 8, ThighF = 32, KneeF = 30, ThighB = -28, KneeB = 12, ArmF = 30, ElbowF = 10, Weapon = 15 + i * 8, ArmB = -10, ElbowB = 25 };
            case "hurt":
                return new Pose { Lean = 4 - i * 6, ThighF = 5, KneeF = 20, ThighB = -20, KneeB = 10, ArmF = -10, ElbowF = 20, Weapon = 30, ArmB = -30, ElbowB = 20 };
            case "stagger":
            {
                float s = MathF.Sin(t * Tau);
                return new Pose { Lean = -10 + 6 * s, RootX = -2, ThighF = 0, KneeF = 25, ThighB = -25, KneeB = 25, ArmF = -25 + 10 * s, ElbowF = 30, Weapon = -10, ArmB = -50, ElbowB = 10 };
            }
            case "death":
            {
                var a = WardenPose("hurt", 1, 2);
                var kneel = new Pose { Lean = 30, ThighF = 85, KneeF = 150, ThighB = 10, KneeB = 120, ArmF = 0, ElbowF = 10, Weapon = 5, ArmB = -5, ElbowB = 10 };
                var down = new Pose { Lean = 0, ThighF = 0, KneeF = 0, ThighB = -5, KneeB = 5, ArmF = 100, ElbowF = 0, Weapon = 120, ArmB = 70, ElbowB = 0, Rot = 88, ShiftX = -44, ShiftY = -6 };
                return i switch { 0 => a, 1 => Pose.Lerp(a, kneel, 0.6f), 2 => kneel, 3 => Pose.Lerp(kneel, down, 0.4f), 4 => Pose.Lerp(kneel, down, 0.8f), _ => down };
            }
        }
        return Stand();
    }
}

/// <summary>A baked sprite sheet: one row per animation.</summary>
public sealed class SpriteSheet
{
    public readonly Image Image;
    public readonly int FrameW, FrameH;
    public readonly Vector2 Origin;
    public readonly AnimDef[] Defs;

    public SpriteSheet(Image img, int fw, int fh, Vector2 origin, AnimDef[] defs) { Image = img; FrameW = fw; FrameH = fh; Origin = origin; Defs = defs; }

    public static SpriteSheet Bake(AnimDef[] defs, Func<string, int, int, Pose> poser, Style style, int fw, int fh, Vector2 origin)
    {
        int cols = 0; foreach (var d in defs) cols = Math.Max(cols, d.Frames);
        var sheet = new PixelCanvas(cols * fw, defs.Length * fh);
        for (int r = 0; r < defs.Length; r++)
            for (int f = 0; f < defs[r].Frames; f++)
            {
                var frame = new PixelCanvas(fw, fh);
                var pose = poser(defs[r].Name, f, defs[r].Frames);
                CharacterRig.Draw(frame, origin, pose, style);
                frame.Outline(style.Outline);
                if (pose.Flash > 0) TintEdges(frame, Color.FromHtml("#ffe2b0"));
                sheet.Blit(frame, f * fw, r * fh);
            }
        return new SpriteSheet(sheet.ToImage(), fw, fh, origin, defs);
    }

    // windup "tell": a bright rim on the outline so the player can read the incoming attack
    static void TintEdges(PixelCanvas c, Color col)
    {
        var copy = new PixelCanvas(c.W, c.H); copy.Blit(c, 0, 0);
        for (int y = 1; y < c.H - 1; y++)
            for (int x = 1; x < c.W - 1; x++)
                if (copy.Opaque(x, y) && (!copy.Opaque(x - 1, y) || !copy.Opaque(x, y - 1))) c.Set(x, y, col);
    }

    public SpriteFrames BuildFrames(Texture2D tex)
    {
        var sf = new SpriteFrames();
        if (sf.HasAnimation("default")) sf.RemoveAnimation("default");
        for (int r = 0; r < Defs.Length; r++)
        {
            var d = Defs[r];
            sf.AddAnimation(d.Name); sf.SetAnimationLoopMode(d.Name, d.Loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None); sf.SetAnimationSpeed(d.Name, d.Fps);
            for (int f = 0; f < d.Frames; f++)
                sf.AddFrame(d.Name, new AtlasTexture { Atlas = tex, Region = new Rect2(f * FrameW, r * FrameH, FrameW, FrameH) });
        }
        return sf;
    }
}
