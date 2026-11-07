using System;
using RayneoIdle.Save;
using UnityEngine;

namespace RayneoIdle.Game
{
    /// <summary>
    /// W9 Demo Mode (MVP 9.2b). Speeds up income only (passive coins + chest search); time itself never changes.
    /// - Off in normal builds: <see cref="allowDemoMode"/> must be on (Inspector / dev build).
    /// - Hidden toggle: <see cref="RegisterSecretTap"/> called by the HUD when the clock icon is tapped;
    ///   <see cref="secretTapCount"/> taps within <see cref="secretTapWindowSeconds"/> toggle demo mode.
    /// - Entering demo switches to the demo save slot (never writes the real save); leaving switches back.
    /// - Tier 1x / 60x / 1800x via <see cref="CycleTier"/>; badge text via <see cref="BadgeText"/>.
    /// - "Skip offline N hours" = one normal settle (same 24 h cap).
    /// </summary>
    public class DemoMode : MonoBehaviour
    {
        private const string LogTag = "[DemoMode]";

        [Tooltip("Master switch. Off = demo mode can never be entered (normal / playtest builds).")]
        [SerializeField] private bool allowDemoMode = true;

        [Tooltip("Taps on the hidden target (clock icon) needed to toggle demo mode.")]
        [SerializeField] private int secretTapCount = 5;

        [Tooltip("All secret taps must land within this many seconds.")]
        [SerializeField] private float secretTapWindowSeconds = 3f;

        [Tooltip("Hours paid by one 'Skip offline' press.")]
        [SerializeField] private double skipOfflineHours = 8;

        public bool IsActive { get; private set; }
        public int Tier => GameSession.Instance != null ? GameSession.Instance.Core.DemoTier : 0;

        /// <summary>Empty when no badge should show (tier 1x or demo off). Else e.g. "60x DEMO".</summary>
        public string BadgeText
        {
            get
            {
                if (!IsActive || GameSession.Instance == null) return "";
                var core = GameSession.Instance.Core;
                return core.DemoTier == 0 ? "DEMO" : $"{core.DemoMultiplier:0}x DEMO";
            }
        }

        public event Action<bool> OnActiveChanged;

        private int tapsInWindow;
        private float windowStart;

        /// <summary>Hidden toggle input. Returns true when this tap toggled demo mode.</summary>
        public bool RegisterSecretTap()
        {
            if (!allowDemoMode) return false;
            float now = Time.unscaledTime;
            if (tapsInWindow == 0 || now - windowStart > secretTapWindowSeconds)
            {
                tapsInWindow = 0;
                windowStart = now;
            }
            tapsInWindow++;
            if (tapsInWindow < secretTapCount) return false;

            tapsInWindow = 0;
            SetActive(!IsActive);
            return true;
        }

        public void SetActive(bool active)
        {
            var session = GameSession.Instance;
            if (!allowDemoMode || session == null || active == IsActive) return;

            if (!active) session.Core.SetDemoTier(0); // credit the time so far at the demo tier first
            session.SwitchSlot(active ? SaveSystem.DemoSlot : SaveSystem.MainSlot);
            IsActive = active;
            Debug.Log($"{LogTag} {(active ? "ON (demo slot)" : "OFF (main slot)")}");
            OnActiveChanged?.Invoke(IsActive);
        }

        /// <summary>1x -> 60x -> 1800x -> 1x. Only while demo mode is active.</summary>
        public void CycleTier()
        {
            if (!IsActive) return;
            var core = GameSession.Instance.Core;
            core.SetDemoTier((core.DemoTier + 1) % core.Config.demoIncomeMultipliers.Length);
        }

        public void SetTier(int tier)
        {
            if (IsActive) GameSession.Instance.Core.SetDemoTier(tier);
        }

        /// <summary>Demo "Skip offline N hours". Returns coins paid.</summary>
        public double SkipOffline()
        {
            if (!IsActive) return 0;
            return GameSession.Instance.Core.DemoSkipHours(skipOfflineHours);
        }
    }
}
