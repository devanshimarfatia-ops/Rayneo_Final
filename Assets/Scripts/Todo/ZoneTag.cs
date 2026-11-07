using System.Text.RegularExpressions;

namespace RayneoIdle.Todo
{
    /// <summary>Zone a todo belongs to. General = no (valid) prefix.</summary>
    public enum TodoZone { General = 0, Work = 1, Bedroom = 2, Kitchen = 3 }

    /// <summary>
    /// D3: the zone lives in the todo text itself as a "(Zone)-" prefix, e.g. "(Work)-Write report",
    /// so the system To-do app shows it and Reset / reinstall keeps it.
    /// Parsing is lenient: case-insensitive, spaces allowed around the parts ("( work ) - text").
    /// Anything else (no prefix, unknown zone, broken brackets) counts as General.
    /// Plain C# so it can be unit-tested.
    /// </summary>
    public static class ZoneTag
    {
        private static readonly Regex Prefix =
            new Regex(@"^\s*\(\s*(work|bedroom|kitchen)\s*\)\s*-\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>Splits a system todo text into zone + the text to show (prefix removed).</summary>
        public static TodoZone Parse(string text, out string body)
        {
            text ??= "";
            var m = Prefix.Match(text);
            if (!m.Success)
            {
                body = text.Trim();
                return TodoZone.General;
            }
            body = m.Groups[2].Value.Trim();
            switch (m.Groups[1].Value.ToLowerInvariant())
            {
                case "work": return TodoZone.Work;
                case "bedroom": return TodoZone.Bedroom;
                default: return TodoZone.Kitchen;
            }
        }

        public static TodoZone Parse(string text) => Parse(text, out _);

        /// <summary>Text to write back with todoEdit. Replaces any existing valid prefix. General = no prefix.</summary>
        public static string Format(TodoZone zone, string text)
        {
            Parse(text, out string body);
            return zone == TodoZone.General ? body : $"({zone})-{body}";
        }
    }
}
