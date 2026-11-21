using System.Collections.Generic;

namespace ClashOfThrones.Core;

public enum ItemKind { Relic, KeyItem, Consumable }

public sealed record ItemDef(string Id, string Name, ItemKind Kind, string Description);

/// <summary>Every item in the game. Relics are equipped (2 slots), key items unlock abilities or doors.</summary>
public static class Items
{
    public static readonly Dictionary<string, ItemDef> All = new()
    {
        ["ember_sigil"] = new("ember_sigil", "Ember Sigil", ItemKind.Relic, "A coal that never cools. +15% damage."),
        ["tallow_heart"] = new("tallow_heart", "Tallow Heart", ItemKind.Relic, "Wax shaped like a heart. +2 maximum health."),
        ["iron_vow"] = new("iron_vow", "Iron Vow", ItemKind.Relic, "An oath hammered into a ring. Blocking costs half stamina."),
        ["thirsting_blade"] = new("thirsting_blade", "Thirsting Blade", ItemKind.Relic, "A notch that drinks. Every 8th hit restores 1 health."),
        ["pilgrim_sandal"] = new("pilgrim_sandal", "Pilgrim's Sandal", ItemKind.Relic, "Worn thin by the road. Rolls and sprinting cost 30% less stamina."),
        ["wings_of_ash"] = new("wings_of_ash", "Wings of Ash", ItemKind.KeyItem, "Grey feathers that hold the air. Jump again in mid-air."),
        ["rusted_key"] = new("rusted_key", "Rusted Key", ItemKind.KeyItem, "Opens the reliquary door in the lower crypt."),
        ["flask_shard"] = new("flask_shard", "Flask Shard", ItemKind.Consumable, "A piece of a holy vessel. +1 healing flask."),
    };

    public static string NameOf(string id) => All.TryGetValue(id, out var d) ? d.Name : id;
}
