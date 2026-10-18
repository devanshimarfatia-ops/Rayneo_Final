using UnityEngine;
using RayNeo; // HeadTrackedPoseDriver

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Drop-in component: add it to any GameObject in a scene that already has the RayNeo
    /// "XR Plugin" prefab. On Awake it creates the whole anchor-zone system at runtime:
    ///   ZoneSystem (SlamSessionController, ZoneAnchorStore, ZoneDetector, ModeManager,
    ///   AnchorPlacementInput, ZoneDebugHUD, ZoneAnchorMarkers) + head-locked HUD canvas, all wired.
    /// Editor testing (WASD walk, Space / arrows for the menu) is handled by AnchorPlacementInput.
    /// All tuning lives on this component, so nothing else has to be set up in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class AnchorZoneBootstrap : MonoBehaviour
    {
        [Tooltip("Head camera. Leave empty to use the XR Plugin's HeadTrackedPoseDriver camera (or Camera.main).")]
        [SerializeField] private Transform head;

        [Header("Zone triggers (meters / seconds)")]
        [Tooltip("ENTER: closer than this (horizontal distance) to an anchor...")]
        [SerializeField] private float enterRadius = 1.0f;
        [Tooltip("...continuously for this long.")]
        [SerializeField] private float dwellSeconds = 2.0f;
        [Tooltip("EXIT: farther than this from the current anchor. Keep it larger than Enter Radius.")]
        [SerializeField] private float exitRadius = 2.0f;

        [Header("Input")]
        [Tooltip("Flip if swiping forward on the temple moves the menu selection the wrong way.")]
        [SerializeField] private bool invertSwipe = false;
        [Tooltip("Hide the XR Plugin gaze dot (LaserBeamDot) while the anchor menu is open, so it is not " +
                 "mistaken for an object pointer: anchors mark where the user stands.")]
        [SerializeField] private bool hideGazeDotInMenu = true;

        [Header("SLAM / debug")]
        [Tooltip("Debug fallback: treat SLAM as tracking even if GetSlamStatus() never reports SUCCESS.")]
        [SerializeField] private bool ignoreSlamStatus = true;
        [Tooltip("DRIFT trigger: head moving faster than this (m/s) = runaway pose, detection pauses.")]
        [SerializeField] private float maxHeadSpeed = 3f;
        [Tooltip("DRIFT recovery: speed must stay normal for this long (s) before detection resumes.")]
        [SerializeField] private float driftRecoverSeconds = 2f;
        [Tooltip("Force the camera to clear to solid black (#000000 = transparent on the glasses).")]
        [SerializeField] private bool forceBlackBackground = true;

        private void Awake()
        {
            // Guard: never build a second system (e.g. the scene already has one from the setup menu).
            if (FindObjectOfType<ZoneDetector>() != null)
            {
                Debug.LogWarning("[AnchorZoneBootstrap] A ZoneDetector already exists in the scene, skipping.");
                return;
            }

            if (head == null) head = FindHead();
            if (head == null)
            {
                Debug.LogError("[AnchorZoneBootstrap] No head camera found. Add the RayNeo 'XR Plugin' prefab to the scene.");
                return;
            }

            var cam = head.GetComponent<Camera>();
            if (forceBlackBackground && cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }

            // Build inactive, wire everything, then activate. Otherwise OnEnable of
            // ModeManager / ZoneDebugHUD would run before their references are set.
            var system = new GameObject("ZoneSystem");
            system.SetActive(false);
            system.transform.SetParent(transform, false);

            var slam = system.AddComponent<SlamSessionController>();
            var store = system.AddComponent<ZoneAnchorStore>();
            var detector = system.AddComponent<ZoneDetector>();
            var modes = system.AddComponent<ModeManager>();
            var input = system.AddComponent<AnchorPlacementInput>();
            var hud = system.AddComponent<ZoneDebugHUD>();
            var markers = system.AddComponent<ZoneAnchorMarkers>();

            slam.Configure(head, ignoreSlamStatus, maxHeadSpeed, driftRecoverSeconds);
            detector.Configure(store, slam, head, enterRadius, exitRadius, dwellSeconds);
            modes.Configure(detector);
            input.Configure(store, detector, slam, invertSwipe);
            hud.Configure(detector, modes, slam, input, store, head, hideGazeDotInMenu); // also builds the HUD canvas
            markers.Configure(store, slam, head);

            system.SetActive(true);
            Debug.Log($"[AnchorZoneBootstrap] Zone system created (enter {enterRadius} m / {dwellSeconds} s, exit {exitRadius} m)");
        }

        private static Transform FindHead()
        {
            var driver = FindObjectOfType<HeadTrackedPoseDriver>();
            if (driver != null) return driver.transform;
            return Camera.main != null ? Camera.main.transform : null;
        }
    }
}
