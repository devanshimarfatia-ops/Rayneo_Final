using System;
using System.IO;
using UnityEngine;

namespace RayneoIdle.Save
{
    /// <summary>
    /// W2: JSON save per slot in Application.persistentDataPath.
    /// Writes go to a temp file first and then replace the real file, so a kill mid-write keeps the old save.
    /// The previous good save is kept as .bak and used when the main file is unreadable.
    /// </summary>
    public static class SaveSystem
    {
        private const string LogTag = "[SaveSystem]";

        /// <summary>Real game slot (D10).</summary>
        public const int MainSlot = 0;
        /// <summary>Demo Mode slot: demo never writes the real save (9.2b).</summary>
        public const int DemoSlot = 1;

        public static string PathFor(int slot) => Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");

        public static bool Exists(int slot) => File.Exists(PathFor(slot)) || File.Exists(PathFor(slot) + ".bak");

        /// <summary>Loads a slot. Returns null when there is no save (fresh install / after Reset).</summary>
        public static SaveData Load(int slot)
        {
            string path = PathFor(slot);
            var data = TryRead(path) ?? TryRead(path + ".bak");
            if (data == null) return null;

            // Migrations go here when SaveData.CurrentVersion is bumped.
            data.version = SaveData.CurrentVersion;
            data.idle ??= new Idle.IdleState();
            data.cards ??= new Cards.CardCollectionState();
            data.island ??= new Island.IslandState();
            data.rewardedTodoTimestamps ??= new System.Collections.Generic.List<long>();
            return data;
        }

        public static void Write(int slot, SaveData data)
        {
            string path = PathFor(slot);
            string tmp = path + ".tmp";
            try
            {
                data.savedUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                File.WriteAllText(tmp, JsonUtility.ToJson(data));
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogTag} write slot {slot} failed: {e.Message}");
            }
        }

        /// <summary>Reset (D10): deletes the slot and its backup.</summary>
        public static void Delete(int slot)
        {
            string path = PathFor(slot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
            {
                try { if (File.Exists(p)) File.Delete(p); }
                catch (Exception e) { Debug.LogError($"{LogTag} delete {p} failed: {e.Message}"); }
            }
            Debug.Log($"{LogTag} slot {slot} deleted");
        }

        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{LogTag} read {path} failed: {e.Message}");
                return null;
            }
        }
    }
}
