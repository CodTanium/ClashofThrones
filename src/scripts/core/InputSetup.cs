using Godot;

namespace ClashOfThrones.Core;

/// <summary>Keyboard + gamepad bindings, defined in code so they are easy to read and change.</summary>
public static class InputSetup
{
    public const string Left = "move_left", Right = "move_right", Up = "move_up", Down = "move_down";
    public const string Jump = "jump", Attack = "attack", Dodge = "dodge", Parry = "parry", Interact = "interact", Pause = "pause";

    public static void Apply()
    {
        Bind(Left, Keys(Key.A, Key.Left), Pad(JoyButton.DpadLeft), Axis(JoyAxis.LeftX, -1));
        Bind(Right, Keys(Key.D, Key.Right), Pad(JoyButton.DpadRight), Axis(JoyAxis.LeftX, 1));
        Bind(Up, Keys(Key.W, Key.Up), Pad(JoyButton.DpadUp), Axis(JoyAxis.LeftY, -1));
        Bind(Down, Keys(Key.S, Key.Down), Pad(JoyButton.DpadDown), Axis(JoyAxis.LeftY, 1));
        Bind(Jump, Keys(Key.Space, Key.Z), Pad(JoyButton.A));
        Bind(Attack, Keys(Key.J, Key.X), Pad(JoyButton.X), new InputEvent[] { new InputEventMouseButton { ButtonIndex = MouseButton.Left } });
        Bind(Dodge, Keys(Key.K, Key.C, Key.Shift), Pad(JoyButton.B), Pad(JoyButton.RightShoulder));
        Bind(Parry, Keys(Key.L, Key.V), Pad(JoyButton.LeftShoulder), Pad(JoyButton.Y));
        Bind(Interact, Keys(Key.E));
        Bind(Pause, Keys(Key.Escape), Pad(JoyButton.Start));
    }

    static InputEvent[] Keys(params Key[] keys)
    {
        var r = new InputEvent[keys.Length];
        for (int i = 0; i < keys.Length; i++) r[i] = new InputEventKey { PhysicalKeycode = keys[i] };
        return r;
    }
    static InputEvent[] Pad(JoyButton b) => new InputEvent[] { new InputEventJoypadButton { ButtonIndex = b } };
    static InputEvent[] Axis(JoyAxis a, float v) => new InputEvent[] { new InputEventJoypadMotion { Axis = a, AxisValue = v } };

    static void Bind(string action, params InputEvent[][] groups)
    {
        if (InputMap.HasAction(action)) InputMap.EraseAction(action);
        InputMap.AddAction(action, 0.35f);
        foreach (var g in groups) foreach (var e in g) InputMap.ActionAddEvent(action, e);
    }
}
