using System;
using UnityEngine;
using RayNeo.API; // Algorithm + Algorithm.SlamState (ARDK SDK/Runtime/Scripts/APIs/Algorithm.cs)

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Starts/stops SLAM 6DoF and reports tracking state.
    /// Requires Camera Attitude Type = SLAM in
    /// Project Settings > XR Plug-in Management > OpenXR > RayNeoSupport.
    /// In the Editor, the SLAM status is simulated as always successful.
    ///
    /// IsTracking = SLAM status OK AND no pose drift detected.
    /// Drift guard: when SLAM silently loses visual tracking, the head position keeps
    /// integrating IMU data and runs away (distance to anchors grows while standing still).
    /// GetSlamStatus() does not report this on X3 Pro (it stays 0), so the guard watches the
    /// head speed instead: faster than maxHeadSpeed = drifting.
    /// </summary>
    public class SlamSessionController : MonoBehaviour
    {
        [Tooltip("How often SLAM status and the drift guard are polled, in seconds.")]
        [SerializeField] private float pollInterval = 0.5f;

#pragma warning disable 0414 // device-only fields, unused in the Editor
        [Tooltip("Debug fallback: treat SLAM as tracking regardless of GetSlamStatus(). " +
                 "Use only if the status API never reports SUCCESS but the head position clearly moves.")]
        [SerializeField] private bool ignoreSlamStatus = false;

        [Tooltip("Seconds between [SlamSession] pose log lines on device. 0 = off.")]
        [SerializeField] private float poseLogInterval = 2f;
#pragma warning restore 0414

        [Tooltip("Head transform, used by the drift guard and the pose log. Leave empty to use Camera.main.")]
        [SerializeField] private Transform head;

        [Header("Drift guard")]
        [Tooltip("DRIFT trigger: horizontal head speed above this (m/s) between two polls. " +
                 "Walking is ~1.4 m/s, so anything faster is treated as a runaway pose.")]
        [SerializeField] private float maxHeadSpeed = 3f;
        [Tooltip("DRIFT recovery: head speed must stay below maxHeadSpeed for this long (s).")]
        [SerializeField] private float driftRecoverSeconds = 2f;

        public bool IsTracking { get; private set; }

        /// <summary>True while the drift guard thinks the head pose is running away.</summary>
        public bool IsDrifting { get; private set; }

        /// <summary>Last measured horizontal head speed in m/s (drift guard input).</summary>
        public float HeadSpeed { get; private set; }

        /// <summary>Last raw value returned by Algorithm.GetSlamStatus() (0 init, 1 success, 2 fail), -1 before the first poll.</summary>
        public int RawStatus { get; private set; } = -1;

        /// <summary>Fires with true when tracking succeeds, false when it is lost.</summary>
        public event Action<bool> OnTrackingChanged;

        private float nextPollTime;
        private float nextPoseLogTime;
        private bool started;
        private bool statusOk;        // SLAM status part of IsTracking
        private bool hasLastSample;
        private Vector3 lastHeadPos;
        private float lastSampleTime;
        private float driftCalmTimer; // time spent below maxHeadSpeed while drifting

        /// <summary>Runtime wiring, used by AnchorZoneBootstrap. Call before the component is enabled.</summary>
        public void Configure(Transform head, bool ignoreSlamStatus, float maxHeadSpeed, float driftRecoverSeconds)
        {
            this.head = head;
            this.ignoreSlamStatus = ignoreSlamStatus;
            this.maxHeadSpeed = maxHeadSpeed;
            this.driftRecoverSeconds = driftRecoverSeconds;
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
            statusOk = false;
            hasLastSample = false;
            UpdateTracking();
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
            statusOk = true;
#else
            int raw = (int)Algorithm.GetSlamStatus();
            if (raw != RawStatus)
            {
                Debug.Log($"[SlamSession] Raw status {RawStatus} -> {raw} ({(Algorithm.SlamState)raw})");
                RawStatus = raw;
            }

            if (ignoreSlamStatus) statusOk = true;
            // INITIALIZING keeps the previous value; only SUCCESS/FAIL change it
            else if (raw == (int)Algorithm.SlamState.FFVINS_TRACKING_SUCCESS) statusOk = true;
            else if (raw == (int)Algorithm.SlamState.FFVINS_TRACKING_FAIL) statusOk = false;
#endif

            UpdateDriftGuard();
            UpdateTracking();

#if !UNITY_EDITOR
            // Periodic pose log: shows whether 6DoF position actually changes when walking.
            if (poseLogInterval > 0f && head != null && Time.unscaledTime >= nextPoseLogTime)
            {
                nextPoseLogTime = Time.unscaledTime + poseLogInterval;
                Debug.Log($"[SlamSession] status={raw} headPos={head.position.ToString("F2")} " +
                          $"speed={HeadSpeed:F2}m/s drift={IsDrifting}");
            }
#endif
        }

        private void UpdateDriftGuard()
        {
            if (head == null) return;
            Vector3 pos = head.position;
            float now = Time.unscaledTime;

            if (hasLastSample)
            {
                float dt = Mathf.Max(now - lastSampleTime, 0.001f);
                HeadSpeed = ZoneMath.HorizontalDistance(pos, lastHeadPos) / dt;

                if (HeadSpeed > maxHeadSpeed)
                {
                    // Trigger: runaway pose (faster than a person can walk).
                    driftCalmTimer = 0f;
                    if (!IsDrifting)
                    {
                        IsDrifting = true;
                        Debug.LogWarning($"[SlamSession] DRIFT detected: {HeadSpeed:F1} m/s at {pos.ToString("F2")}");
                    }
                }
                else if (IsDrifting)
                {
                    // Recovery: speed must stay plausible for driftRecoverSeconds in a row.
                    driftCalmTimer += dt;
                    if (driftCalmTimer >= driftRecoverSeconds)
                    {
                        IsDrifting = false;
                        Debug.Log($"[SlamSession] Drift recovered at {pos.ToString("F2")}");
                    }
                }
            }

            lastHeadPos = pos;
            lastSampleTime = now;
            hasLastSample = true;
        }

        private void UpdateTracking()
        {
            bool tracking = statusOk && !IsDrifting;
            if (IsTracking == tracking) return;
            IsTracking = tracking;
            Debug.Log($"[SlamSession] Tracking = {tracking}");
            OnTrackingChanged?.Invoke(tracking);
        }
    }
}
