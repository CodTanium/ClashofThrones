using Godot;
using System;

namespace ClashOfThrones.Art;

/// <summary>One pose of the side-view character rig. Angles in degrees: 0 = straight down, 90 = forward, 180 = straight up.</summary>
public sealed class Pose
{
    public float RootX, RootY;            // hip offset (airborne poses)
    public float Lean = 4;                // torso tilt, + = forward
    public float ThighF = 6, KneeF = 6, ThighB = -6, KneeB = 6;
    public float ArmF = 20, ElbowF = 20, ArmB = -10, ElbowB = 30;
    public float Weapon = 60;             // absolute blade angle
    public bool ShieldFront;              // shield raised in front of the body (parry)
    public float CapeBack = 8, CapeDrop = 26, CapeWave;
    public bool Grounded = true;          // feet are pushed onto the ground line
    public float Rot;                     // whole-body rotation around the feet (death)
    public float ShiftX, ShiftY;
    public float HeadTilt;                // helmet nod (+ forward)
    public float Flash;                   // 0..1 white flash (hit) - baked only for the windup tell

    public Pose Clone() => (Pose)MemberwiseClone();

    public static Pose Lerp(Pose a, Pose b, float t)
    {
        var p = a.Clone();
        float L(float x, float y) => x + (y - x) * t;
        p.RootX = L(a.RootX, b.RootX); p.RootY = L(a.RootY, b.RootY); p.Lean = L(a.Lean, b.Lean);
        p.ThighF = L(a.ThighF, b.ThighF); p.KneeF = L(a.KneeF, b.KneeF); p.ThighB = L(a.ThighB, b.ThighB); p.KneeB = L(a.KneeB, b.KneeB);
        p.ArmF = L(a.ArmF, b.ArmF); p.ElbowF = L(a.ElbowF, b.ElbowF); p.ArmB = L(a.ArmB, b.ArmB); p.ElbowB = L(a.ElbowB, b.ElbowB);
        p.Weapon = L(a.Weapon, b.Weapon); p.CapeBack = L(a.CapeBack, b.CapeBack); p.CapeDrop = L(a.CapeDrop, b.CapeDrop);
        p.CapeWave = L(a.CapeWave, b.CapeWave); p.Rot = L(a.Rot, b.Rot); p.ShiftX = L(a.ShiftX, b.ShiftX); p.ShiftY = L(a.ShiftY, b.ShiftY);
        p.HeadTilt = L(a.HeadTilt, b.HeadTilt); p.Flash = L(a.Flash, b.Flash);
        p.ShieldFront = t < 0.5f ? a.ShieldFront : b.ShieldFront; p.Grounded = t < 0.5f ? a.Grounded : b.Grounded;
        return p;
    }
}

public enum HeadKind { Helmet, Hood, Skull, Mitre }
public enum WeaponKind { Sword, Cleaver, None, Bow, Staff }

/// <summary>Colours and parts of one character built on the rig.</summary>
public sealed class Style
{
    public float Scale = 1f;
    public Ramp Armor, ArmorFar, Limb, LimbFar, Cloth, ClothFar, Leather, Gold, Weapon, Head, Skirt;
    public Color Outline = Color.FromHtml("#0b0a0f");
    public Color Eye = Color.FromHtml("#0b0a0f");
    public bool Cape = true, Shield = true, Tabard = true;
    public HeadKind HeadKind = HeadKind.Helmet;
    public WeaponKind WeaponKind = WeaponKind.Sword;
    public float Bulk = 1f;   // torso width multiplier

    public static Style Knight()
    {
        var steel = new Ramp("#d2d6df", "#8f95a3", "#565b69", "#2d303b");
        var red = new Ramp("#c23a33", "#8a2420", "#561718", "#2e0d10");
        return new Style
        {
            Armor = steel, ArmorFar = steel.Darker(0.28f),
            Limb = new Ramp("#a9aebb", "#727887", "#474b58", "#272a33"),
            LimbFar = new Ramp("#a9aebb", "#727887", "#474b58", "#272a33").Darker(0.3f),
            Cloth = red, ClothFar = red.Darker(0.3f),
            Leather = new Ramp("#83603f", "#5c412b", "#3c2a1c", "#22170f"),
            Gold = new Ramp("#f0c55a", "#c38d2c", "#8a5d1c", "#4a300e"),
            Weapon = new Ramp("#eef1f6", "#b8bdc8", "#7d828f", "#454955"),
            Head = steel,
            Skirt = new Ramp("#7d828e", "#5d616d", "#3f424c", "#24262d"),
        };
    }

    public static Style Ghoul()
    {
        var skin = new Ramp("#b9b6a0", "#87856f", "#5a5948", "#30302a");
        var rag = new Ramp("#4a4f45", "#33372f", "#22251f", "#121410");
        return new Style
        {
            Scale = 0.95f, Bulk = 0.8f,
            Armor = rag, ArmorFar = rag.Darker(0.3f), Limb = skin, LimbFar = skin.Darker(0.32f), Cloth = rag, ClothFar = rag.Darker(0.3f),
            Leather = new Ramp("#4a3c30", "#33291f", "#211a14", "#110d0a"), Gold = rag, Weapon = new Ramp("#e8e2c8", "#b8b090", "#807a60", "#403c30"),
            Head = skin, Skirt = rag, Eye = Color.FromHtml("#ff5a3c"),
            Cape = false, Shield = false, Tabard = false, HeadKind = HeadKind.Skull, WeaponKind = WeaponKind.None,
        };
    }

    public static Style Archer()
    {
        var bone = new Ramp("#e6dcc0", "#b5a888", "#7f745a", "#423b2c");
        var cloth = new Ramp("#5a3a4a", "#3e2733", "#281922", "#140c11");
        return new Style
        {
            Scale = 0.98f, Bulk = 0.85f,
            Armor = cloth, ArmorFar = cloth.Darker(0.3f), Limb = bone, LimbFar = bone.Darker(0.32f), Cloth = cloth, ClothFar = cloth.Darker(0.3f),
            Leather = new Ramp("#7a5a3c", "#57402a", "#3a2a1c", "#1e150e"), Gold = new Ramp("#d8c070", "#a8903e", "#705e24", "#3a3010"),
            Weapon = new Ramp("#f0e8d0", "#c8bfa0", "#8f876c", "#4a4536"), Head = bone, Skirt = cloth, Eye = Color.FromHtml("#7ae0ff"),
            Cape = true, Shield = false, Tabard = false, HeadKind = HeadKind.Skull, WeaponKind = WeaponKind.Bow,
        };
    }

    public static Style Abbot()
    {
        var robe = new Ramp("#3e5a5e", "#2a3f44", "#1b2a2e", "#0d1517");
        var flesh = new Ramp("#9fb3a8", "#71857b", "#4b5a52", "#28302b");
        return new Style
        {
            Scale = 1.65f, Bulk = 1.4f,
            Armor = robe, ArmorFar = robe.Darker(0.3f), Limb = flesh, LimbFar = flesh.Darker(0.3f),
            Cloth = new Ramp("#8a2e3a", "#5e1d27", "#3c1219", "#1e080c"), ClothFar = new Ramp("#8a2e3a", "#5e1d27", "#3c1219", "#1e080c").Darker(0.3f),
            Leather = new Ramp("#5a4632", "#3e3022", "#281f16", "#140f0a"), Gold = new Ramp("#f0d070", "#c09a38", "#86681e", "#463408"),
            Weapon = robe, Head = new Ramp("#2a3f44", "#1b2a2e", "#111b1e", "#070c0d"), Skirt = robe, Eye = Color.FromHtml("#8ff6ff"),
            Cape = true, Shield = false, Tabard = true, HeadKind = HeadKind.Mitre, WeaponKind = WeaponKind.Staff,
        };
    }

    public static Style Warden()
    {
        var robe = new Ramp("#6e6253", "#4e4539", "#342d25", "#1c1813");
        var flesh = new Ramp("#d8c9a6", "#a8977a", "#76664f", "#433829");
        return new Style
        {
            Scale = 1.12f, Bulk = 1.35f,
            Armor = robe, ArmorFar = robe.Darker(0.3f),
            Limb = flesh, LimbFar = flesh.Darker(0.32f),
            Cloth = robe, ClothFar = robe.Darker(0.3f),
            Leather = new Ramp("#5a4a3c", "#3e3228", "#28201a", "#15100c"),
            Gold = new Ramp("#9b8f7a", "#6f6553", "#4a4236", "#28231c"),
            Weapon = new Ramp("#b9a796", "#8c6a52", "#5e4433", "#33241a"),
            Head = new Ramp("#4d443a", "#3a322a", "#26201b", "#14110e"),
            Skirt = robe,
            Eye = Color.FromHtml("#ff9a3c"),
            Cape = false, Shield = false, Tabard = false,
            HeadKind = HeadKind.Hood, WeaponKind = WeaponKind.Cleaver,
        };
    }
}

/// <summary>Draws a <see cref="Style"/> in a <see cref="Pose"/> onto a frame. Facing right; origin = point between the feet.</summary>
public static class CharacterRig
{
    static Vector2 Dir(float deg) { float r = Mathf.DegToRad(deg); return new Vector2(MathF.Sin(r), MathF.Cos(r)); }

    sealed class Joints
    {
        public Vector2 Hip, KneeF, FootF, KneeB, FootB, Chest, ShoulderF, ShoulderB, Neck, Head, ElbowF, HandF, ElbowB, HandB;
        public Vector2 WeaponTip, WeaponBase;
        public void Transform(Func<Vector2, Vector2> f)
        {
            Hip = f(Hip); KneeF = f(KneeF); FootF = f(FootF); KneeB = f(KneeB); FootB = f(FootB); Chest = f(Chest);
            ShoulderF = f(ShoulderF); ShoulderB = f(ShoulderB); Neck = f(Neck); Head = f(Head); ElbowF = f(ElbowF);
            HandF = f(HandF); ElbowB = f(ElbowB); HandB = f(HandB); WeaponTip = f(WeaponTip); WeaponBase = f(WeaponBase);
        }
    }

    public static void Draw(PixelCanvas c, Vector2 origin, Pose p, Style s)
    {
        float k = s.Scale;
        float thigh = 21 * k, shin = 21 * k, torso = 26 * k, upper = 15 * k, fore = 13 * k;
        var j = new Joints();
        j.Hip = new Vector2(p.RootX, -42 * k + p.RootY);
        var up = Dir(180 - p.Lean);
        j.Chest = j.Hip + up * (torso * 0.55f);
        var shoulder = j.Hip + up * torso;
        j.ShoulderF = shoulder + new Vector2(2, 1) * k;
        j.ShoulderB = shoulder + new Vector2(-3, 0) * k;
        j.Neck = j.Hip + up * (torso + 4 * k);
        j.Head = j.Hip + up * (torso + 10 * k) + new Vector2(1.5f * k, 0);
        j.KneeF = j.Hip + Dir(p.ThighF) * thigh; j.FootF = j.KneeF + Dir(p.ThighF - p.KneeF) * shin;
        j.KneeB = j.Hip + Dir(p.ThighB) * thigh; j.FootB = j.KneeB + Dir(p.ThighB - p.KneeB) * shin;
        j.ElbowF = j.ShoulderF + Dir(p.ArmF) * upper; j.HandF = j.ElbowF + Dir(p.ArmF + p.ElbowF) * fore;
        j.ElbowB = j.ShoulderB + Dir(p.ArmB) * upper; j.HandB = j.ElbowB + Dir(p.ArmB + p.ElbowB) * fore;
        float blade = s.WeaponKind switch { WeaponKind.Sword => 44, WeaponKind.Staff => 58, WeaponKind.None => 6, _ => 34 } * k;
        j.WeaponBase = j.HandF - Dir(p.Weapon) * 5 * k;
        j.WeaponTip = j.HandF + Dir(p.Weapon) * blade;

        if (p.Grounded)
        {
            float low = MathF.Max(j.FootF.Y, j.FootB.Y);
            j.Transform(v => v + new Vector2(0, -low - 2.5f * k));
        }
        if (MathF.Abs(p.Rot) > 0.01f) { float r = Mathf.DegToRad(p.Rot); j.Transform(v => v.Rotated(r)); }
        var shift = origin + new Vector2(p.ShiftX, p.ShiftY);
        j.Transform(v => v + shift);
        var bodyDir = (j.Neck - j.Hip).Normalized();
        var side = new Vector2(bodyDir.Y, -bodyDir.X); // points backward (left) for an upright body facing right

        // 1. cape, behind everything
        if (s.Cape) DrawCape(c, j, p, s, bodyDir);
        // 2. far arm (+ shield when not raised)
        DrawLimb(c, j.ShoulderB, j.ElbowB, j.HandB, 3.2f * k, s.LimbFar, s.LimbFar, s.Leather.Darker(0.3f));
        if (s.Shield && !p.ShieldFront) DrawShield(c, j.HandB, s, false, p.Rot);
        // 3. far leg
        DrawLeg(c, j.Hip + side * 2.5f * k, j.KneeB, j.FootB, k, s, true);
        // 4. torso
        DrawTorso(c, j, p, s, bodyDir, side);
        // 5. head
        DrawHead(c, j, p, s, bodyDir);
        // 6. near leg
        DrawLeg(c, j.Hip - side * 1.5f * k, j.KneeF, j.FootF, k, s, false);
        // 7. shield raised
        if (s.Shield && p.ShieldFront) DrawShield(c, j.HandB + new Vector2(-1 * k, -2 * k), s, true, p.Rot);
        // 8. weapon, then near arm over the grip
        DrawWeapon(c, j, p, s);
        DrawLimb(c, j.ShoulderF, j.ElbowF, j.HandF, 3.6f * k, s.Limb, s.Armor, s.Leather);
        if (s.HeadKind == HeadKind.Helmet) c.Circle(j.ShoulderF + new Vector2(0, -0.5f * k), 5.2f * k, s.Armor, 0.1f); // pauldron
        else c.Circle(j.ShoulderF, 4.5f * k, s.Cloth);
    }

    static void DrawCape(PixelCanvas c, Joints j, Pose p, Style s, Vector2 bodyDir)
    {
        float k = s.Scale;
        var top1 = j.ShoulderB + new Vector2(-3 * k, 1 * k);
        var top2 = j.ShoulderF + new Vector2(-1 * k, 2 * k);
        var down = -bodyDir;
        var back = new Vector2(-1, 0);
        float w = p.CapeWave;
        var b1 = j.Hip + down * p.CapeDrop * k + back * (p.CapeBack + 6) * k + new Vector2(0, MathF.Sin(w * 6.28f) * 2 * k);
        var b2 = j.Hip + down * (p.CapeDrop - 4) * k + back * (p.CapeBack * 0.5f - 2) * k;
        var mid = (b1 + b2) * 0.5f + down * 2 * k;
        var pts = new[] { top2, top1, top1 + back * 3 * k + down * 8 * k, b1, b1 + new Vector2(2, -3) * k, mid, mid + new Vector2(2, -4) * k, b2 };
        c.Poly(pts, s.ClothFar, -0.15f);
        // inner fold line
        c.Line(top1 + down * 6 * k, (b1 + mid) * 0.5f, s.ClothFar.Deep);
    }

    static void DrawLimb(PixelCanvas c, Vector2 a, Vector2 b, Vector2 h, float r, in Ramp limb, in Ramp joint, in Ramp hand)
    {
        c.Capsule(a, b, r, limb);
        c.Capsule(b, h, r * 0.95f, limb);
        c.Circle(b, r * 1.05f, joint, 0.1f);
        c.Circle(h, r * 0.9f, hand);
    }

    static void DrawLeg(PixelCanvas c, Vector2 hip, Vector2 knee, Vector2 foot, float k, Style s, bool far)
    {
        var limb = far ? s.LimbFar : s.Limb;
        var armor = far ? s.ArmorFar : s.Armor;
        var boot = far ? s.Leather.Darker(0.3f) : s.Leather;
        c.Capsule(hip, knee, 4.6f * k, limb);
        c.Capsule(knee, foot, 3.9f * k, s.HeadKind == HeadKind.Helmet ? armor : limb);
        c.Circle(knee, 4.1f * k, armor, 0.15f);
        // boot: from ankle forward
        var toe = foot + new Vector2(6.5f * k, 0.5f * k);
        c.Capsule(foot + new Vector2(0, -1.5f * k), toe, 3.2f * k, boot);
    }

    static void DrawTorso(PixelCanvas c, Joints j, Pose p, Style s, Vector2 bodyDir, Vector2 side)
    {
        float k = s.Scale, b = s.Bulk;
        var down = -bodyDir;
        // skirt (chainmail / robe) from hip to above the knees
        var h = j.Hip;
        float skirtLen = (s.Tabard ? 16 : 22) * k;
        var skirt = new[]
        {
            h + side * 8 * k * b, h - side * 8.5f * k * b,
            h - side * 10 * k * b + down * skirtLen, h + side * 1 * k + down * (skirtLen + 2 * k), h + side * 10 * k * b + down * skirtLen,
        };
        c.Poly(skirt, s.Skirt, -0.1f);
        // chest
        c.Capsule(j.Hip + bodyDir * 4 * k, j.Neck - bodyDir * 4 * k, 8.6f * k * b, s.Armor, 0.05f);
        if (s.Tabard)
        {
            // red tabard strip hanging in front from the belt
            var t = new[] { h - side * 4 * k + bodyDir * 1 * k, h - side * 9 * k + bodyDir * 1 * k, h - side * 9.5f * k + down * 20 * k, h - side * 6 * k + down * 17 * k, h - side * 3 * k + down * 21 * k };
            c.Poly(t, s.Cloth, 0f);
            // breastplate ridge highlight
            c.Line(j.Chest + bodyDir * 6 * k - side * 2 * k, j.Hip + bodyDir * 4 * k - side * 2 * k, s.Armor.Hi);
        }
        else
        {
            // hunched back hump + rope belt
            c.Circle(j.ShoulderB + down * 2 * k + side * 2 * k, 7 * k, s.ArmorFar, 0.1f);
        }
        // belt
        c.Capsule(h + side * 8.5f * k * b + bodyDir * 1.5f * k, h - side * 9 * k * b + bodyDir * 1.5f * k, 1.8f * k, s.Leather);
        c.Circle(h - side * 5 * k + bodyDir * 1.5f * k, 1.6f * k, s.Gold, 0.2f);
    }

    static void DrawHead(PixelCanvas c, Joints j, Pose p, Style s, Vector2 bodyDir)
    {
        float k = s.Scale;
        var head = j.Head;
        var fwd = new Vector2(-bodyDir.Y, bodyDir.X); // forward (+x when upright)
        if (s.HeadKind == HeadKind.Helmet)
        {
            c.Capsule(j.Neck, j.Neck + bodyDir * 2 * k, 5 * k, s.Skirt); // gorget / mail
            c.Circle(head, 8.3f * k, s.Head, 0.05f);
            // spike on top
            var top = head + bodyDir * 8 * k;
            c.Poly(new[] { top + fwd * -2.5f * k, top + fwd * 2.5f * k, top + bodyDir * 6 * k }, s.Head, 0.2f);
            c.Circle(top, 1.6f * k, s.Gold, 0.2f);
            // visor: dark slit + breathing holes, on the front half
            var v0 = head + fwd * 1.5f * k + bodyDir * 0.5f * k;
            c.Line(v0, v0 + fwd * 7 * k, s.Outline);
            c.Line(v0 + bodyDir * 1 * k, v0 + fwd * 6 * k + bodyDir * 1 * k, s.Head.Deep);
            c.Set((int)(head.X + fwd.X * 4 * k - bodyDir.X * 4 * k), (int)(head.Y + fwd.Y * 4 * k - bodyDir.Y * 4 * k), s.Head.Deep);
            c.Set((int)(head.X + fwd.X * 6 * k - bodyDir.X * 4 * k), (int)(head.Y + fwd.Y * 6 * k - bodyDir.Y * 4 * k), s.Head.Deep);
            // rim highlight top-left
            c.Line(head + bodyDir * 6 * k - fwd * 4 * k, head + bodyDir * 7.5f * k + fwd * 1 * k, s.Head.Hi);
        }
        else if (s.HeadKind == HeadKind.Skull)
        {
            // gaunt bald skull, jaw jutting forward, hollow eyes
            c.Circle(head, 7f * k, s.Head, 0.1f);
            c.Capsule(head + fwd * 3 * k - bodyDir * 4 * k, head + fwd * 7 * k - bodyDir * 6 * k, 2.6f * k, s.Head);
            c.Circle(head + fwd * 3.5f * k + bodyDir * 0.5f * k, 1.8f * k, new Ramp(s.Head.Deep, s.Head.Deep, s.Head.Deep, s.Head.Deep));
            var e = head + fwd * 3.5f * k + bodyDir * 0.5f * k;
            c.Set((int)e.X, (int)e.Y, s.Eye);
            c.Line(head + fwd * 4 * k - bodyDir * 6 * k, head + fwd * 8 * k - bodyDir * 6 * k, s.Head.Deep);
        }
        else if (s.HeadKind == HeadKind.Mitre)
        {
            // drowned abbot: tall split mitre over a shadowed face, gold trim, glowing eyes
            c.Circle(head, 8f * k, s.Head, 0.05f);
            var baseL = head + bodyDir * 4 * k - fwd * 7 * k; var baseR = head + bodyDir * 4 * k + fwd * 7 * k;
            c.Poly(new[] { baseL, baseR, head + bodyDir * 20 * k + fwd * 2 * k, head + bodyDir * 16 * k, head + bodyDir * 22 * k - fwd * 3 * k }, s.Gold, 0.1f);
            c.Line(baseL, baseR, s.Gold.Deep);
            c.Line(head + bodyDir * 6 * k, head + bodyDir * 18 * k, s.Cloth.Mid);
            c.Ellipse(head + fwd * 3.5f * k - bodyDir * 1 * k, 4f * k, 5f * k, s.Head.Deep);
            var e = head + fwd * 4.5f * k - bodyDir * 0.5f * k;
            c.Rect((int)e.X, (int)e.Y, 2, 1, s.Eye); c.Rect((int)e.X + 3, (int)e.Y, 2, 1, s.Eye);
        }
        else
        {
            // hood: a pointed cowl leaning forward, dark face opening with glowing eyes
            var tip = head + bodyDir * 9 * k - fwd * 5 * k;
            c.Poly(new[] { head - fwd * 8 * k - bodyDir * 6 * k, tip, head + fwd * 7 * k + bodyDir * 3 * k, head + fwd * 8 * k - bodyDir * 6 * k }, s.Head, 0.1f);
            c.Ellipse(head + fwd * 3.5f * k - bodyDir * 1 * k, 3.5f * k, 4.5f * k, s.Head.Deep);
            var e = head + fwd * 4.5f * k - bodyDir * 0.5f * k;
            c.Set((int)e.X, (int)e.Y, s.Eye); c.Set((int)e.X + 2, (int)e.Y, s.Eye);
        }
    }

    static void DrawShield(PixelCanvas c, Vector2 hand, Style s, bool front, float rot)
    {
        float k = s.Scale;
        float w = 11 * k, hgt = 17 * k;
        var cen = hand + new Vector2(front ? 3 * k : -1 * k, 3 * k);
        var r = Mathf.DegToRad(rot);
        Vector2 P(float x, float y) => cen + new Vector2(x, y).Rotated(r);
        var pts = new[] { P(-w, -hgt), P(w, -hgt), P(w, hgt * 0.35f), P(0, hgt), P(-w, hgt * 0.35f) };
        var rim = new Ramp(s.Armor.Hi, s.Armor.Mid, s.Armor.Lo, s.Armor.Deep);
        c.Poly(pts, rim, 0.1f);
        var inner = new[] { P(-w + 2, -hgt + 2), P(w - 2, -hgt + 2), P(w - 2, hgt * 0.35f - 1), P(0, hgt - 3), P(-w + 2, hgt * 0.35f - 1) };
        c.Poly(inner, front ? s.Cloth : s.ClothFar, 0.05f);
        // boss + band
        c.Line(P(0, -hgt + 2), P(0, hgt - 3), s.Gold.Lo);
        c.Circle(P(0, -2), 2.2f * k, s.Gold, 0.2f);
    }

    static void DrawWeapon(PixelCanvas c, Joints j, Pose p, Style s)
    {
        float k = s.Scale;
        var d = (j.WeaponTip - j.HandF).Normalized();
        var n = new Vector2(-d.Y, d.X);
        if (s.WeaponKind == WeaponKind.Sword)
        {
            var guard = j.HandF + d * 3 * k;
            c.Capsule(guard, j.WeaponTip, 1.4f * k, s.Weapon, 0.2f);
            c.Line(guard + n * 0.5f, j.WeaponTip - d * 2, s.Weapon.Hi);
            c.Capsule(guard - n * 4.5f * k, guard + n * 4.5f * k, 1.3f * k, s.Gold);
            c.Capsule(j.WeaponBase, guard, 1.2f * k, s.Leather);
            c.Circle(j.WeaponBase, 1.8f * k, s.Gold, 0.2f);
        }
        else if (s.WeaponKind == WeaponKind.None)
        {
            // claws: three short hooks from the hand
            for (int i = -1; i <= 1; i++) c.Line(j.HandF + n * i * 1.5f, j.HandF + d * 6 * k + n * i * 2.5f, s.Weapon.Hi);
        }
        else if (s.WeaponKind == WeaponKind.Bow)
        {
            // recurve bow held vertically in the front hand, string to the back hand
            var ad = (j.HandF - j.ElbowF).Normalized();
            var up = new Vector2(-ad.Y, ad.X); if (up.Y > 0) up = -up;
            d = ad;
            var top = j.HandF + up * 16 * k - d * 2 * k; var bot = j.HandF - up * 16 * k - d * 2 * k;
            var mid = j.HandF + d * 4 * k;
            c.Capsule(top, mid, 1.2f * k, s.Leather); c.Capsule(mid, bot, 1.2f * k, s.Leather);
            var pull = p.Weapon > 100 ? j.HandB : j.HandF; // Weapon > 100 = drawn
            c.Line(top, pull, s.Weapon.Hi); c.Line(pull, bot, s.Weapon.Hi);
            if (p.Weapon > 100) c.Line(pull, j.HandF + d * 12 * k, s.Gold.Mid);  // nocked arrow
        }
        else if (s.WeaponKind == WeaponKind.Staff)
        {
            // long crozier staff with a hooked, censer-like head
            var tail = j.HandF - d * 18 * k;
            c.Capsule(tail, j.WeaponTip, 1.6f * k, s.Leather);
            var hook = j.WeaponTip;
            c.Circle(hook, 5 * k, s.Gold, 0.15f);
            c.Circle(hook + n * 4 * k + d * 3 * k, 2.4f * k, s.Gold, 0.2f);
            c.Circle(hook, 2 * k, new Ramp("#9cf0ff", "#4fb8d0", "#2a6f86", "#123744"));
        }
        else
        {
            var b0 = j.HandF + d * 6 * k;
            var tip = j.WeaponTip;
            var pts = new[] { b0 + n * 2 * k, tip + n * 2 * k, tip + n * 11 * k - d * 4 * k, b0 + n * 12 * k };
            c.Poly(pts, s.Weapon, 0.1f);
            c.Line(tip + n * 11 * k - d * 4 * k, b0 + n * 12 * k, s.Weapon.Hi);
            c.Line(b0 + n * 4 * k, tip + n * 4 * k, s.Weapon.Deep); // fuller / rust streak
            c.Capsule(j.WeaponBase, b0, 1.6f * k, s.Leather);
        }
    }
}
