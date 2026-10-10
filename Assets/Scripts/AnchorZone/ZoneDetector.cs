using System;
using UnityEngine;

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Compares the head position against all anchors and decides which zone the user is in.
    ///
    /// ENTER trigger: horizontal distance &lt; enterRadius, held continuously for dwellSeconds.
    /// EXIT trigger:  horizontal distance &gt; exitRadius (larger than enterRadius = hysteresis,
    ///                so standing on the border does not flicker between modes).
    /// LOST tracking: zone is kept, no new evaluation until tracking returns.
    /// </summary>
    public class ZoneDetector : MonoBehaviour
    {
        [SerializeField] private ZoneAnchorStore store;
        [SerializeField] private SlamSessionController slam;
        [Tooltip("Usually the XR Plugin camera. Leave empty to use Camera.main.")]
        [SerializeField] private Transform head;

        [Header("Trigger tuning (meters / seconds)")]
        [SerializeField] private float enterRadius = 1.0f;
        [SerializeField] private float exitRadius = 2.0f;  // leave the zone beyond 2 m
        [SerializeField] private float dwellSeconds = 2.0f;
        [Tooltip("Evaluation interval. Lower = more responsive, higher = less CPU.")]
        [SerializeField] private float evaluateInterval = 0.2f;

        public ZoneAnchorData CurrentAnchor { get; private set; }
        public ZoneAnchorData CandidateAnchor { get; private set; }
        public float CandidateProgress => dwellSeconds <= 0f ? 1f : Mathf.Clamp01(candidateTimer / dwellSeconds);
        public float NearestDistance { get; private set; } = float.PositiveInfinity;

        public event Action<ZoneAnchorData> OnZoneEntered;
        public event Action<ZoneAnchorData> OnZoneExited;

        private float candidateTimer;
        private float nextEvaluateTime;

        public Vector3 HeadPosition => head != null ? head.position : Vector3.zero;

        private void Start()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;
            store.OnAnchorsChanged += HandleAnchorsChanged;
        }

        private void OnDestroy()
        {
            if (store != null) store.OnAnchorsChanged -= HandleAnchorsChanged;
        }

        private void Update()
        {
            if (head == null || !slam.IsTracking) return;
            if (Time.unscaledTime < nextEvaluateTime) return;

            float dt = evaluateInterval;
            nextEvaluateTime = Time.unscaledTime + evaluateInterval;
            Evaluate(dt);
        }

        private void Evaluate(float dt)
        {
            Vector3 pos = head.position;

            // 1) Already inside a zone: only check the exit condition.
            if (CurrentAnchor != null)
            {
                float d = ZoneMath.HorizontalDistance(pos, CurrentAnchor.position);
                NearestDistance = d;
                if (d > exitRadius) ExitCurrent();
                return;
            }

            // 2) Not in a zone: find the nearest anchor.
            ZoneAnchorData nearest = null;
            float nearestDist = float.PositiveInfinity;
            foreach (var a in store.Anchors)
            {
                float d = ZoneMath.HorizontalDistance(pos, a.position);
                if (d < nearestDist) { nearestDist = d; nearest = a; }
            }
            NearestDistance = nearestDist;

            // 3) Dwell logic: must stay inside enterRadius of the SAME anchor.
            if (nearest != null && nearestDist < enterRadius)
            {
                if (CandidateAnchor != nearest) { CandidateAnchor = nearest; candidateTimer = 0f; }
                candidateTimer += dt;

                if (candidateTimer >= dwellSeconds)
                {
                    CurrentAnchor = nearest;
                    CandidateAnchor = null;
                    candidateTimer = 0f;
                    Debug.Log($"[ZoneDetector] ENTER {CurrentAnchor.zoneType} ({CurrentAnchor.id})");
                    OnZoneEntered?.Invoke(CurrentAnchor);
                }
            }
            else
            {
                CandidateAnchor = null;
                candidateTimer = 0f;
            }
        }

        private void ExitCurrent()
        {
            var old = CurrentAnchor;
            CurrentAnchor = null;
            Debug.Log($"[ZoneDetector] EXIT {old.zoneType} ({old.id})");
            OnZoneExited?.Invoke(old);
        }

        // If the anchor we are inside gets deleted, treat it as an exit.
        private void HandleAnchorsChanged()
        {
            if (CurrentAnchor == null) return;
            foreach (var a in store.Anchors) if (a.id == CurrentAnchor.id) return;
            ExitCurrent();
        }
    }
}
