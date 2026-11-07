using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RayneoIdle.Todo
{
    /// <summary>
    /// Zone a todo belongs to. General = no (valid) prefix. Work / Bedroom / Kitchen are the three anchor SLOTS
    /// (slot 1 / 2 / 3, same order as ZoneType); the name the player sees is the slot's name (ZoneNames), not
    /// the enum name. Saved as int: never reorder.
    /// </summary>
    public enum TodoZone { General = 0, Work = 1, Bedroom = 2, Kitchen = 3 }

    /// <summary>
    /// D3: the zone lives in the todo text itself as a "(Name)-" prefix, e.g. "(Work)-Write report",
    /// so the system To-do app shows it and Reset / reinstall keeps it.
    /// Names: the current slot names (anchors can be renamed). The default names "Work" / "Bedroom" / "Kitchen"
    /// are also accepted for their slot, so todos tagged before a rename still map to the same slot.
    /// Parsing is lenient: case-insensitive, spaces allowed ("( work ) - text"). Anything else
    /// (no prefix, unknown name, broken brackets) counts as General. Plain C# so it can be unit-tested.
    /// </summary>
    public static class ZoneTag
    {
        public static readonly string[] DefaultNames = { "Work", "Bedroom", "Kitchen" };

        /// <summary>Preset anchor names (Anchors panel). Also used to clean stacked prefixes.</summary>
        public static readonly string[] PresetNames = { "Work", "Study", "Office", "Desk", "Kitchen", "Bedroom", "Living room", "Gym" };

        /// <summary>
        /// Removes leading "(Name)-" prefixes whose name is a preset or default zone name, repeatedly.
        /// Repairs texts tagged several times by the rename bug of device test 4 ("(Work)-(Study)-text").
        /// Unknown brackets ("(urgent)-call") are kept: they are the user's own text.
        /// </summary>
        public static string StripAllPrefixes(string body)
        {
            body ??= "";
            for (int guard = 0; guard < 8; guard++)
            {
                var m = Prefix.Match(body);
                if (!m.Success) break;
                string tag = m.Groups[1].Value.Trim();
                if (Array.FindIndex(PresetNames, n => string.Equals(n, tag, StringComparison.OrdinalIgnoreCase)) < 0) break;
                body = m.Groups[2].Value.Trim();
            }
            return body;
        }

        private static readonly Regex Prefix =
            new Regex(@"^\s*\(\s*([^()]{1,40}?)\s*\)\s*-\s*(.*)$", RegexOptions.Singleline);

        /// <summary>Slot index (0..2) to zone.</summary>
        public static TodoZone FromSlot(int slot) => slot >= 0 && slot < 3 ? (TodoZone)(slot + 1) : TodoZone.General;
        /// <summary>Zone to slot index (0..2), -1 for General.</summary>
        public static int ToSlot(TodoZone zone) => zone == TodoZone.General ? -1 : (int)zone - 1;

        /// <summary>Splits a system todo text into zone + the text to show (prefix removed).</summary>
        public static TodoZone Parse(string text, IReadOnlyList<string> names, out string body)
        {
            text ??= "";
            var m = Prefix.Match(text);
            if (m.Success)
            {
                string tag = m.Groups[1].Value.Trim();
                int slot = FindSlot(tag, names);
                if (slot < 0) slot = FindSlot(tag, DefaultNames);
                if (slot >= 0)
                {
                    body = StripAllPrefixes(m.Groups[2].Value.Trim()); // also hides stacked prefixes in the game
                    return FromSlot(slot);
                }
            }
            body = text.Trim();
            return TodoZone.General;
        }

        public static TodoZone Parse(string text, out string body) => Parse(text, DefaultNames, out body);
        public static TodoZone Parse(string text) => Parse(text, DefaultNames, out _);

        /// <summary>Text to write back with todoEdit. Replaces any existing valid prefix. General = no prefix.</summary>
        public static string Format(TodoZone zone, string text, IReadOnlyList<string> names)
        {
            Parse(text, names, out string body);
            int slot = ToSlot(zone);
            if (slot < 0) return body;
            string name = names != null && slot < names.Count && !string.IsNullOrWhiteSpace(names[slot]) ? names[slot] : DefaultNames[slot];
            return $"({Clean(name)})-{body}";
        }

        public static string Format(TodoZone zone, string text) => Format(zone, text, DefaultNames);

        /// <summary>A zone name usable inside "( )": no brackets, trimmed, max 16 chars.</summary>
        public static string Clean(string name)
        {
            name = (name ?? "").Replace("(", "").Replace(")", "").Trim().TrimEnd('.', '!', '?', ',');
            return name.Length > 16 ? name.Substring(0, 16).Trim() : name;
        }

        private static int FindSlot(string tag, IReadOnlyList<string> names)
        {
            if (names == null) return -1;
            for (int i = 0; i < names.Count && i < 3; i++)
                if (!string.IsNullOrWhiteSpace(names[i]) && string.Equals(Clean(names[i]), tag, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }
    }
}
