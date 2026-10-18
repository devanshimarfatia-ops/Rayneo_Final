using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Draws one world-locked marker per anchor: a small diamond at the anchor point
    /// with "ZONE  distance" above it (e.g. "WORK 1.4m"), so with several anchors you can see which one
    /// is which and how far each is. Distance = horizontal distance head -> anchor, the same value
    /// ZoneDetector uses for ENTER / EXIT. Also shows by eye how far an anchor drifts. Built from UI elements (no meshes, no physics, no extra shaders).
    /// Rebuilt on ZoneAnchorStore.OnAnchorsChanged; facing, size and distance text update at ~5 Hz.
    /// The anchor marks where the user stood, so the marker is usually seen after stepping away.
    /// </summary>
    public class ZoneAnchorMarkers : MonoBehaviour
    {
        // Marker canvas in px. Markers keep a CONSTANT ANGULAR SIZE: 1 px covers the same visual angle
        // as 1 px of the HUD (640x480 canvas over a 30 deg diagonal FOV), so a 24 px label looks like
        // HUD body text at any distance instead of filling the view when the anchor is close.
        private const float CanvasWidthPx = 200f;
        private const float CanvasHeightPx = 80f;
        // = 2 * tan(15 deg) / 800 px diagonal: meters per px at 1 m distance (scaled by distance).
        private static readonly float MetersPerPxAt1m = 2f * Mathf.Tan(15f * Mathf.Deg2Rad) / 800f;
        private const float DiamondPx = 20f;
        private const int LabelFontSize = 24; // body size per RayNeo specs
        private const float FaceInterval = 0.2f;

        private static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        private static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);
        private static readonly Color Purple = new Color32(0x85, 0x66, 0xFF, 0xFF);

        private ZoneAnchorStore store;
        private Transform head;
        private class Marker
        {
            public ZoneAnchorData anchor;
            public Transform root;
            public Text label;
        }

        private readonly List<Marker> markers = new List<Marker>();
        private float nextFaceTime;
        private SlamSessionController slam;
        private bool markersVisible = true;

        /// <summary>Runtime wiring, used by AnchorZoneBootstrap. Call before the component is enabled.</summary>
        public void Configure(ZoneAnchorStore store, SlamSessionController slam, Transform head)
        {
            this.store = store;
            this.slam = slam;
            this.head = head;
        }

        private void OnEnable() => store.OnAnchorsChanged += Rebuild;
        private void OnDisable() => store.OnAnchorsChanged -= Rebuild;
        private void Start() => Rebuild();

        private void Update()
        {
            if (head == null || Time.unscaledTime < nextFaceTime) return;
            nextFaceTime = Time.unscaledTime + FaceInterval;

            // Trigger: SLAM tracking lost / drifting (e.g. system screen recording takes the camera).
            // The head pose is wrong then, so markers would sit in wrong places: hide them until it recovers.
            SetMarkersVisible(slam == null || slam.IsTracking);
            if (!markersVisible) return;

            foreach (var m in markers)
            {
                FaceHead(m.root);
                // Distance text: horizontal, same metric as ZoneDetector (sitting/standing does not change it).
                float d = ZoneMath.HorizontalDistance(head.position, m.anchor.position);
                m.label.text = $"{m.anchor.zoneType.ToString().ToUpper()} {d:F1}m";
            }
        }

        private void SetMarkersVisible(bool visible)
        {
            if (markersVisible == visible) return;
            markersVisible = visible;
            foreach (var m in markers) if (m.root != null) m.root.gameObject.SetActive(visible);
        }

        // Yaw-only billboard (labels stay upright) + constant angular size (scale grows with distance).
        private void FaceHead(Transform m)
        {
            Vector3 toMarker = m.position - head.position;
            m.localScale = Vector3.one * (MetersPerPxAt1m * Mathf.Max(toMarker.magnitude, 0.2f));
            toMarker.y = 0f;
            if (toMarker.sqrMagnitude > 0.0001f) m.rotation = Quaternion.LookRotation(toMarker);
        }

        // Trigger: anchors added / removed / loaded.
        private void Rebuild()
        {
            foreach (var m in markers) if (m.root != null) Destroy(m.root.gameObject);
            markers.Clear();
            foreach (var a in store.Anchors)
            {
                var m = CreateMarker(a);
                m.root.gameObject.SetActive(markersVisible); // respect the tracking-lost state
                markers.Add(m);
            }
            nextFaceTime = 0f;
        }

        private static Color ZoneColor(ZoneType type) =>
            type == ZoneType.Work ? Cyan : type == ZoneType.Bedroom ? Purple : Green;

        private Marker CreateMarker(ZoneAnchorData anchor)
        {
            Color color = ZoneColor(anchor.zoneType);
            var go = new GameObject($"Marker {anchor.zoneType} {anchor.id}", typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = head != null ? head.GetComponent<Camera>() : null;
            // Display only: no raycaster, nothing on the marker is clickable.

            // Pivot at the bottom center, so the diamond sits exactly on the anchor point.
            rt.sizeDelta = new Vector2(CanvasWidthPx, CanvasHeightPx);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.position = anchor.position;
            rt.localScale = Vector3.one * MetersPerPxAt1m; // real scale is set by FaceHead()

            // Diamond = small square turned 45 deg (Image without sprite = solid quad).
            var diamond = new GameObject("Diamond", typeof(RectTransform));
            diamond.layer = go.layer;
            var drt = (RectTransform)diamond.transform;
            drt.SetParent(rt, false);
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.anchoredPosition = new Vector2(0f, DiamondPx * 0.75f);
            drt.sizeDelta = new Vector2(DiamondPx, DiamondPx);
            drt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var diamondImage = diamond.AddComponent<Image>();
            diamondImage.color = color;
            diamondImage.raycastTarget = false;

            // Label above the diamond.
            var label = new GameObject("Label", typeof(RectTransform));
            label.layer = go.layer;
            var lrt = (RectTransform)label.transform;
            lrt.SetParent(rt, false);
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, DiamondPx * 1.6f);
            lrt.sizeDelta = new Vector2(CanvasWidthPx, 32f);
            var labelTextComp = label.AddComponent<Text>();
            labelTextComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelTextComp.fontStyle = FontStyle.Bold;
            labelTextComp.fontSize = LabelFontSize;
            labelTextComp.color = color;
            labelTextComp.alignment = TextAnchor.LowerCenter;
            labelTextComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelTextComp.raycastTarget = false;
            labelTextComp.text = anchor.zoneType.ToString().ToUpper(); // distance is added by Update()

            return new Marker { anchor = anchor, root = rt, label = labelTextComp };
        }
    }
}
