using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ClashOfThrones.Core;

/// <summary>Everything a save slot remembers. Ids of world objects are their map tile "x_y".</summary>
public sealed class SaveData
{
    public int Slot { get; set; }
    public string Level { get; set; } = "sunken_nave";
    public string Altar { get; set; } = "";            // id of the altar to respawn at ("" = level start)
    public List<string> LitAltars { get; set; } = new();
    public int Vitality { get; set; } = 1;
    public int Strength { get; set; } = 1;
    public int Endurance { get; set; } = 1;
    public int Ashes { get; set; }
    public float RemnantX { get; set; }
    public float RemnantY { get; set; }
    public int RemnantAshes { get; set; }
    public int FlaskMax { get; set; } = 3;
    public int Flasks { get; set; } = 3;
    public List<string> Relics { get; set; } = new();       // owned
    public List<string> Equipped { get; set; } = new();     // max 2
    public List<string> KeyItems { get; set; } = new();     // double_jump, rusted_key ...
    public List<string> Taken { get; set; } = new();        // opened chests, picked items, broken walls, pulled levers
    public bool BossDefeated { get; set; }
    public bool Completed { get; set; }
    public double PlaySeconds { get; set; }
    public string SavedAt { get; set; } = "";
    public int Deaths { get; set; }

    public int Level_ => Vitality + Strength + Endurance - 2;
    public int MaxHp => 5 + Vitality;                      // 6 at start
    public float MaxStamina => 80 + Endurance * 20;        // 100 at start
    public float DamageMul => 1f + (Strength - 1) * 0.12f + (Equipped.Contains("ember_sigil") ? 0.15f : 0f);
    public int LevelCost => (int)(60 * MathF.Pow(1.18f, Level_ - 1));
    public bool Has(string item) => KeyItems.Contains(item);
}

/// <summary>3 save slots as JSON in user://. Autosave at altars, manual save from the pause menu.</summary>
public static class SaveSystem
{
    public const int Slots = 3;
    public const int DevSlot = 9;   // used by automated tests, never shown in the menu
    static string PathOf(int slot) => $"user://save_{slot}.json";
    static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static bool Exists(int slot) => FileAccess.FileExists(PathOf(slot));

    public static SaveData Load(int slot)
    {
        try
        {
            if (!Exists(slot)) return null;
            var d = JsonSerializer.Deserialize<SaveData>(FileAccess.GetFileAsString(PathOf(slot)));
            if (d != null) d.Slot = slot;
            return d;
        }
        catch (Exception e) { GD.PushWarning($"save slot {slot} unreadable: {e.Message}"); return null; }
    }

    public static void Write(SaveData d)
    {
        d.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        using var f = FileAccess.Open(PathOf(d.Slot), FileAccess.ModeFlags.Write);
        f?.StoreString(JsonSerializer.Serialize(d, Opts));
    }

    public static void Delete(int slot) { if (Exists(slot)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(PathOf(slot))); }

    public static int LastUsedSlot()
    {
        int best = -1; string bestAt = "";
        for (int i = 1; i <= Slots; i++)
        {
            var d = Load(i);
            if (d != null && string.CompareOrdinal(d.SavedAt, bestAt) > 0) { best = i; bestAt = d.SavedAt; }
        }
        return best;
    }

    public static string Describe(SaveData d) => d == null ? "Empty"
        : $"Lv {d.Level_}  ·  {d.Ashes} ashes  ·  {TimeSpan.FromSeconds(d.PlaySeconds):h\\:mm}  ·  {d.SavedAt}{(d.Completed ? "  ·  cleared" : "")}";
}
