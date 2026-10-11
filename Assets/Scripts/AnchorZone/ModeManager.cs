using System;
using UnityEngine;

namespace RayneoIdle.Zones
{
    public enum AppMode
    {
        Default = 0, // outside any zone
        Work = 1,
        Bedroom = 2,
        Kitchen = 3
    }

    /// <summary>
    /// Turns zone events into app modes. Gameplay systems should listen to
    /// OnModeChanged instead of ZoneDetector, so detection can be swapped later
    /// (SLAM proximity, QR marker, Wi-Fi, etc.) without touching gameplay code.
    /// </summary>
    public class ModeManager : MonoBehaviour
    {
        [SerializeField] private ZoneDetector detector;

        public AppMode CurrentMode { get; private set; } = AppMode.Default;

        /// <summary>(previousMode, newMode)</summary>
        public event Action<AppMode, AppMode> OnModeChanged;

        /// <summary>Runtime wiring, used by AnchorZoneBootstrap. Call before the component is enabled.</summary>
        public void Configure(ZoneDetector detector) => this.detector = detector;

        private void OnEnable()
        {
            detector.OnZoneEntered += HandleEntered;
            detector.OnZoneExited += HandleExited;
        }

        private void OnDisable()
        {
            detector.OnZoneEntered -= HandleEntered;
            detector.OnZoneExited -= HandleExited;
        }

        // Trigger: ZoneDetector confirmed the user stayed inside an anchor's enter radius.
        private void HandleEntered(ZoneAnchorData anchor) => SetMode(MapZoneToMode(anchor.zoneType));

        // Trigger: ZoneDetector saw the user leave the exit radius.
        private void HandleExited(ZoneAnchorData anchor) => SetMode(AppMode.Default);

        /// <summary>Edit this when adding new ZoneTypes.</summary>
        public static AppMode MapZoneToMode(ZoneType type)
        {
            switch (type)
            {
                case ZoneType.Work: return AppMode.Work;
                case ZoneType.Bedroom: return AppMode.Bedroom;
                case ZoneType.Kitchen: return AppMode.Kitchen;
                default: return AppMode.Default;
            }
        }

        private void SetMode(AppMode mode)
        {
            if (mode == CurrentMode) return;
            var prev = CurrentMode;
            CurrentMode = mode;
            Debug.Log($"[ModeManager] {prev} -> {mode}");
            OnModeChanged?.Invoke(prev, mode);
        }
    }
}
