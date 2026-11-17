using System.Collections.Generic;
using UnityEngine;

namespace RayneoIdle.Todo
{
    /// <summary>
    /// Remembers which system to-dos were already used to unlock an artwork, so they never show up again
    /// (not on the island card, not in the voice TO-DOS list), even if the RayNeo To-do app did not accept
    /// the delete / finish call. Saved in PlayerPrefs, so it survives an app restart.
    /// </summary>
    public static class TodoArchive
    {
        private const string Key = "IslandGallery.UsedTodos";
        private const int MaxKept = 300; // oldest entries are dropped beyond this

        private static List<long> list;
        private static HashSet<long> set;

        private static void Load()
        {
            if (set != null) return;
            list = new List<long>();
            set = new HashSet<long>();
            string raw = PlayerPrefs.GetString(Key, "");
            foreach (string part in raw.Split(','))
                if (long.TryParse(part, out long ts) && set.Add(ts)) list.Add(ts);
        }

        public static bool Contains(long timestamp)
        {
            Load();
            return set.Contains(timestamp);
        }

        public static void Add(long timestamp)
        {
            if (timestamp == 0) return;
            Load();
            if (!set.Add(timestamp)) return;
            list.Add(timestamp);
            while (list.Count > MaxKept) { set.Remove(list[0]); list.RemoveAt(0); }
            PlayerPrefs.SetString(Key, string.Join(",", list));
            PlayerPrefs.Save();
        }

        /// <summary>Removes every archived item from the list (in place) and returns it.</summary>
        public static List<RemoteTodo> FilterOut(List<RemoteTodo> todos)
        {
            Load();
            todos.RemoveAll(t => set.Contains(t.timestamp));
            return todos;
        }

        /// <summary>Forget everything (e.g. for testing).</summary>
        public static void Clear()
        {
            Load();
            list.Clear();
            set.Clear();
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}