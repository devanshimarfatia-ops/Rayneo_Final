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

#pragma warning disable 0414 // device-only fields, unused in the Editor
        [Tooltip("Debug fallback: treat SLAM as tracking regardless of GetSlamStatus(). " +
                 "Use only if the status API never reports SUCCESS but the head position clearly moves.")]
        [SerializeField] private bool ignoreSlamStatus = false;

        [Tooltip("Head transform, only used for the periodic pose log. Leave empty to use Camera.main.")]
        [SerializeField] private Transform head;
        [Tooltip("Seconds between [SlamSession] pose log lines on device. 0 = off.")]
        [SerializeField] private float poseLogInterval = 2f;
#pragma warning restore 0414

        public bool IsTracking { get; private set; }

        /// <summary>Last raw value returned by Algorithm.GetSlamStatus() (0 init, 1 success, 2 fail), -1 before the first poll.</summary>
        public int RawStatus { get; private set; } = -1;

        /// <summary>Fires with true when tracking succeeds, false when it is lost.</summary>
        public event Action<bool> OnTrackingChanged;

        private float nextPollTime;
        private float nextPoseLogTime;
        private bool started;

        /// <summary>Runtime wiring, used by AnchorZoneBootstrap. Call before the component is enabled.</summary>
        public void Configure(Transform head, bool ignoreSlamStatus)
        {
            this.head = head;
            this.ignoreSlamStatus = ignoreSlamStatus;
        }

        private void Start()
        {
            // Enabled in Start (not OnEnable), same as the ARDK SlamDemoCtrl sample,
            // so the XR session is already running when SLAM is requested.
            if (head == null && Camera.main != null) head = Camera.main.transform;
            started = true;
            EnableSlam();
        }

        private void OnEnable()
        {
            // Re-enable after a manual disable/enable; the first enable happens in Start.
            if (started) EnableSlam();
        }

        private void OnDisable()
        {
            // Trigger: call before leaving SLAM 6DoF (per RayNeo docs). No-op in the Editor.
            Algorithm.DisableSlamHeadTracker();
            SetTracking(false);
            Debug.Log("[SlamSession] SLAM disabled");
        }

        private void EnableSlam()
        {
            // Trigger: call before entering SLAM 6DoF (per RayNeo docs).
            // The ARDK already turns this into a no-op inside the Editor.
            Algorithm.EnableSlamHeadTracker();
            Debug.Log("[SlamSession] SLAM enabled");
        }

        private void Update()
        {
            if (Time.unscaledTime < nextPollTime) return;
            nextPollTime = Time.unscaledTime + pollInterval;

#if UNITY_EDITOR
            // The ARDK returns FFVINS_TRACKING_FAIL in the Editor, so simulate success here.
            SetTracking(true);
#else
            int raw = (int)Algorithm.GetSlamStatus();
            if (raw != RawStatus)
            {
                Debug.Log($"[SlamSession] Raw status {RawStatus} -> {raw} ({(Algorithm.SlamState)raw})");
                RawStatus = raw;
            }

            if (ignoreSlamStatus) SetTracking(true);
            // INITIALIZING keeps the previous value; only SUCCESS/FAIL change it
            else if (raw == (int)Algorithm.SlamState.FFVINS_TRACKING_SUCCESS) SetTracking(true);
            else if (raw == (int)Algorithm.SlamState.FFVINS_TRACKING_FAIL) SetTracking(false);

            // Periodic pose log: shows whether 6DoF position actually changes when walking.
            if (poseLogInterval > 0f && head != null && Time.unscaledTime >= nextPoseLogTime)
            {
                nextPoseLogTime = Time.unscaledTime + poseLogInterval;
                Debug.Log($"[SlamSession] status={raw} headPos={head.position.ToString("F2")}");
            }
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
