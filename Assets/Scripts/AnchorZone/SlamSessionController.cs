using System;
using UnityEngine;
using RayNeo.API; // Algorithm + Algorithm.SlamState (ARDK SDK/Runtime/Scripts/APIs/Algorithm.cs)

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Starts/stops SLAM 6DoF and reports tracking state.
    /// Requires Camera Attitude Type = SLAM in
    /// Project Settings > XR Plug-in Management > OpenXR > RayNeoSupport.
    /// In the Editor, tracking is simulated as always successful.
    /// </summary>
    public class SlamSessionController : MonoBehaviour
    {
        [Tooltip("How often SLAM status is polled, in seconds.")]
        [SerializeField] private float pollInterval = 0.5f;

        public bool IsTracking { get; private set; }

        /// <summary>Fires with true when tracking succeeds, false when it is lost.</summary>
        public event Action<bool> OnTrackingChanged;

        private float nextPollTime;

        private void OnEnable()
        {
            // Trigger: call before entering SLAM 6DoF (per RayNeo docs).
            // The ARDK already turns this into a no-op inside the Editor.
            Algorithm.EnableSlamHeadTracker();
            Debug.Log("[SlamSession] SLAM enabled");
        }

        private void OnDisable()
        {
            // Trigger: call before leaving SLAM 6DoF (per RayNeo docs). No-op in the Editor.
            Algorithm.DisableSlamHeadTracker();
            SetTracking(false);
            Debug.Log("[SlamSession] SLAM disabled");
        }

        private void Update()
        {
            if (Time.unscaledTime < nextPollTime) return;
            nextPollTime = Time.unscaledTime + pollInterval;

#if UNITY_EDITOR
            // The ARDK returns FFVINS_TRACKING_FAIL in the Editor, so simulate success here.
            SetTracking(true);
#else
            var state = Algorithm.GetSlamStatus();
            // INITIALIZING keeps the previous value; only SUCCESS/FAIL change it
            if (state == Algorithm.SlamState.FFVINS_TRACKING_SUCCESS) SetTracking(true);
            else if (state == Algorithm.SlamState.FFVINS_TRACKING_FAIL) SetTracking(false);
#endif
        }

        private void SetTracking(bool tracking)
        {
            if (IsTracking == tracking) return;
            IsTracking = tracking;
            Debug.Log($"[SlamSession] Tracking = {tracking}");
            OnTrackingChanged?.Invoke(tracking);
        }
    }
}
