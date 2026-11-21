using Godot;

namespace ClashOfThrones.Core;

/// <summary>Keyboard + gamepad bindings, defined in code so they are easy to read and change.</summary>
public static class InputSetup
{
    public const string Left = "move_left", Right = "move_right", Up = "move_up", Down = "move_down";
    public const string Jump = "jump", Attack = "attack", Heavy = "heavy", Dodge = "dodge", Parry = "parry";
    public const string Heal = "heal", Wrath = "wrath", Interact = "interact", Pause = "pause";

    public static void Apply()
    {
        Bind(Left, Keys(Key.A, Key.Left), Pad(JoyButton.DpadLeft), Axis(JoyAxis.LeftX, -1));
        Bind(Right, Keys(Key.D, Key.Right), Pad(JoyButton.DpadRight), Axis(JoyAxis.LeftX, 1));
        Bind(Up, Keys(Key.W, Key.Up), Pad(JoyButton.DpadUp), Axis(JoyAxis.LeftY, -1));
        Bind(Down, Keys(Key.S, Key.Down), Pad(JoyButton.DpadDown), Axis(JoyAxis.LeftY, 1));
        Bind(Jump, Keys(Key.Space, Key.Z), Pad(JoyButton.A));
        Bind(Attack, Keys(Key.J, Key.X), Pad(JoyButton.X), new InputEvent[] { new InputEventMouseButton { ButtonIndex = MouseButton.Left } });
        Bind(Heavy, Keys(Key.I, Key.C), Pad(JoyButton.Y), new InputEvent[] { new InputEventMouseButton { ButtonIndex = MouseButton.Right } });
        Bind(Dodge, Keys(Key.K, Key.Shift), Pad(JoyButton.B));          // tap = roll / air dash, hold = sprint
        Bind(Parry, Keys(Key.L, Key.V), Pad(JoyButton.LeftShoulder));   // hold = block, first 0.2 s = parry
        Bind(Heal, Keys(Key.R), Pad(JoyButton.RightShoulder));
        Bind(Wrath, Keys(Key.F), Axis(JoyAxis.TriggerRight, 1));
        Bind(Interact, Keys(Key.E), Axis(JoyAxis.TriggerLeft, 1));
        Bind(Pause, Keys(Key.Escape), Pad(JoyButton.Start));

        // menus: Xbox A = press the focused button, B = back (Godot's defaults only cover the keyboard here)
        AddOnce("ui_accept", new InputEventJoypadButton { ButtonIndex = JoyButton.A });
        AddOnce("ui_cancel", new InputEventJoypadButton { ButtonIndex = JoyButton.B });
        foreach (var (act, btn, axis, dir) in new[] { ("ui_left", JoyButton.DpadLeft, JoyAxis.LeftX, -1f), ("ui_right", JoyButton.DpadRight, JoyAxis.LeftX, 1f), ("ui_up", JoyButton.DpadUp, JoyAxis.LeftY, -1f), ("ui_down", JoyButton.DpadDown, JoyAxis.LeftY, 1f) })
        {
            AddOnce(act, new InputEventJoypadButton { ButtonIndex = btn });
            AddOnce(act, new InputEventJoypadMotion { Axis = axis, AxisValue = dir });
        }
    }

    static void AddOnce(string action, InputEvent e)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        foreach (var have in InputMap.ActionGetEvents(action)) if (have.IsMatch(e, true)) return;
        InputMap.ActionAddEvent(action, e);
    }

    public static readonly (string action, string keys, string pad)[] Help =
    {
        ("Move", "A / D", "Left stick"), ("Jump / double jump / wall jump", "Space", "A"),
        ("Light attack (combo, run, air, down+attack = plunge)", "J", "X"), ("Heavy attack (hold = charge)", "I", "Y"),
        ("Roll (tap) / Sprint (hold) / Air dash", "K / Shift", "B"), ("Block (hold) / Parry (just before the hit)", "L", "LB"),
        ("Shield bash", "L + J", "LB + X"), ("Drink flask", "R", "RB"), ("Wrath", "F", "RT"),
        ("Interact / execute staggered foe", "E / J", "LT / X"), ("Drop through ledge", "S + Space", "Down + A"), ("Pause", "Esc", "Start"),
    };

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
