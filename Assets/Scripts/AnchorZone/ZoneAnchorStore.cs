using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Holds all placed anchors and saves them as JSON in persistentDataPath.
    /// NOTE: saved positions are only valid if the next session shares the same
    /// SLAM origin. This is exactly what Test 3 in the README is meant to verify.
    /// </summary>
    public class ZoneAnchorStore : MonoBehaviour
    {
        [Serializable]
        private class SaveFile { public List<ZoneAnchorData> anchors = new List<ZoneAnchorData>(); }

        [Tooltip("Load saved anchors on Start. Turn off to always begin with an empty set.")]
        [SerializeField] private bool loadOnStart = true;

        private readonly List<ZoneAnchorData> anchors = new List<ZoneAnchorData>();
        public IReadOnlyList<ZoneAnchorData> Anchors => anchors;

        public event Action OnAnchorsChanged;

        private string SavePath => Path.Combine(Application.persistentDataPath, "zone_anchors.json");

        private void Start()
        {
            if (loadOnStart) Load();
        }

        public ZoneAnchorData Add(ZoneType type, Vector3 position)
        {
            var anchor = new ZoneAnchorData(type, position);
            anchors.Add(anchor);
            Save();
            OnAnchorsChanged?.Invoke();
            Debug.Log($"[ZoneAnchorStore] Added {type} anchor {anchor.id} at {position}");
            return anchor;
        }

        /// <summary>Removes the anchor closest to the given position (horizontal distance).</summary>
        public bool RemoveNearest(Vector3 position)
        {
            if (anchors.Count == 0) return false;

            int bestIndex = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < anchors.Count; i++)
            {
                float d = ZoneMath.HorizontalDistance(position, anchors[i].position);
                if (d < bestDist) { bestDist = d; bestIndex = i; }
            }

            Debug.Log($"[ZoneAnchorStore] Removed anchor {anchors[bestIndex].id} ({bestDist:F2} m away)");
            anchors.RemoveAt(bestIndex);
            Save();
            OnAnchorsChanged?.Invoke();
            return true;
        }

        public void ClearAll()
        {
            anchors.Clear();
            Save();
            OnAnchorsChanged?.Invoke();
        }

        private void Save()
        {
            try
            {
                var file = new SaveFile { anchors = anchors };
                File.WriteAllText(SavePath, JsonUtility.ToJson(file, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[ZoneAnchorStore] Save failed: {e.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(SavePath));
                anchors.Clear();
                if (file?.anchors != null) anchors.AddRange(file.anchors);
                OnAnchorsChanged?.Invoke();
                Debug.Log($"[ZoneAnchorStore] Loaded {anchors.Count} anchors from {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ZoneAnchorStore] Load failed: {e.Message}");
            }
        }
    }

    public static class ZoneMath
    {
        /// <summary>
        /// Distance on the XZ plane only, so sitting vs standing (head height change)
        /// does not affect zone detection.
        /// </summary>
        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
