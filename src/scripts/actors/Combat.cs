using Godot;

namespace ClashOfThrones.Actors;

public static class Layers
{
    public const uint World = 1, Player = 2, Enemy = 4;
}

public enum HitResult { Ignored, Hit, Parried, Killed }

public struct HitInfo
{
    public int Damage;
    public Node2D Attacker;
    public float Knockback;
    public bool Parryable;
    public bool Heavy;
}

public interface IDamageable { HitResult TakeHit(in HitInfo hit); }
public interface IParryable { void OnParried(); }

/// <summary>White hit-flash shader shared by all characters.</summary>
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
void fragment() {
    vec4 c = texture(TEXTURE, UV);
    COLOR = vec4(mix(c.rgb * COLOR.rgb, flash_color.rgb, flash), c.a * COLOR.a);
}"
        };
        return new ShaderMaterial { Shader = _shader };
    }
}
