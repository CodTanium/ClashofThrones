using Godot;

namespace ClashOfThrones.Actors;

public static class Layers
{
    public const uint World = 1, Player = 2, Enemy = 4, OneWay = 8, Hazard = 16, Interact = 32, Projectile = 64;
}

public enum HitResult { Ignored, Hit, Blocked, Parried, Killed }

public struct HitInfo
{
    public int Damage;
    public Node2D Attacker;
    public float Knockback;
    public bool Parryable;      // shield parry can deflect it (normal enemy attacks, arrows)
    public bool Unblockable;    // must be dodged (red flash)
    public bool Heavy;          // breaks guards / interrupts windups
    public bool GuardBreak;
    public bool Execution;
    public bool Hazard;         // spikes, pits: no attacker
}

public interface IDamageable { HitResult TakeHit(in HitInfo hit); }
public interface IParryable { void OnParried(); }

/// <summary>White hit-flash shader shared by all characters (flash = 0..1, tint for wrath/unblockable).</summary>
public static class FlashMaterial
{
    static Shader _shader;
    public static ShaderMaterial Create()
    {
        _shader ??= new Shader
        {
            Code = @"shader_type canvas_item;
uniform float flash : hint_range(0.0, 1.0) = 0.0;
uniform vec4 flash_color : source_color = vec4(1.0, 0.96, 0.9, 1.0);
uniform vec4 tint : source_color = vec4(0.0, 0.0, 0.0, 0.0);
void fragment() {
    vec4 c = texture(TEXTURE, UV);
    vec3 base = mix(c.rgb * COLOR.rgb, tint.rgb, tint.a * step(0.01, c.a));
    COLOR = vec4(mix(base, flash_color.rgb, flash), c.a * COLOR.a);
}"
        };
        return new ShaderMaterial { Shader = _shader };
    }
}
