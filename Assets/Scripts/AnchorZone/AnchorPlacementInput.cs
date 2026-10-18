using System;
using UnityEngine;
using UnityEngine.InputSystem;
using RayNeo; // SimpleTouchForLite, HeadTrackedPoseDriver (ARDK SDK/Runtime/Scripts/Comps/)

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Anchor placement flow (two states):
    ///
    ///   Idle:
    ///     Single tap   -> open the "Set anchor" menu          (Editor: Space)
    ///     Triple tap   -> remove nearest anchor               (Editor: Backspace)
    ///   Menu open:
    ///     Swipe fwd/back -> choose Work / Kitchen / Bedroom   (Editor: Left / Right arrow)
    ///     Single tap     -> confirm, mark the spot where the user STANDS (Editor: Space)
    ///     Double tap     -> cancel, close without placing (Editor: Escape)
    ///     No input for menuTimeoutSeconds -> menu closes
    ///
    ///   Double tap while the menu is CLOSED is ignored here (left to the system back/exit).
    ///   Editor only: WASD walk, Q / E turn (simulated head pose).
    ///
    /// The anchor is placed at the USER'S OWN POSITION (head projected straight down), not at
    /// whatever the user looks at: the zone is "the area within Enter Radius of where I stood".
    /// Workflow: stand / sit where you usually work, tap, choose Work, tap.
    /// </summary>
    public class AnchorPlacementInput : MonoBehaviour
    {
        [SerializeField] private ZoneAnchorStore store;
        [SerializeField] private ZoneDetector detector;
        [SerializeField] private SlamSessionController slam;

        [Header("Menu")]
        [Tooltip("Menu closes by itself after this many seconds without a tap or swipe.")]
        [SerializeField] private float menuTimeoutSeconds = 10f;

        [Header("Placement")]
        [Tooltip("Anchor height = eye height minus this (m), roughly waist / desk height. Only affects where " +
                 "the marker is drawn (seen once you step away); zone detection uses horizontal distance.")]
        [SerializeField] private float markerDropMeters = 0.8f;

        [Header("Touchpad")]
        [Tooltip("Flip if swiping forward on the X3 Pro temple moves the menu selection the wrong way.")]
        [SerializeField] private bool invertSwipe = false;
        [Tooltip("Swipe events fire repeatedly during one long swipe (every ~200 touch px). " +
                 "Swipes closer together than this are ignored, so one swipe = one step.")]
        [SerializeField] private float swipeCooldownSeconds = 0.35f;
        [Tooltip("Also listen to the touchpad in the Editor (mouse clicks in the Game view count as taps).")]
        [SerializeField] private bool useTouchpadInEditor = false;

        /// <summary>Display / swipe order of the menu. Add new ZoneTypes here too.</summary>
        public static readonly ZoneType[] MenuOrder = { ZoneType.Work, ZoneType.Kitchen, ZoneType.Bedroom };

        public bool IsMenuOpen { get; private set; }
        public ZoneType SelectedType { get; private set; } = ZoneType.Work;

        public event Action<string> OnToast;   // short status messages for the HUD
        public event Action OnMenuChanged;     // menu opened / closed / selection moved

        private float lastSwipeTime = -999f;
        private float menuCloseTime;
        private bool touchpadBound;

        private bool UseTouchpad => !Application.isEditor || useTouchpadInEditor;

        /// <summary>Runtime wiring, used by AnchorZoneBootstrap. Call before the component is enabled.</summary>
        public void Configure(ZoneAnchorStore store, ZoneDetector detector, SlamSessionController slam, bool invertSwipe)
        {
            this.store = store;
            this.detector = detector;
            this.slam = slam;
            this.invertSwipe = invertSwipe;
        }

        private void OnEnable()
        {
            if (!UseTouchpad) return;

            // SimpleTouchForLite.Instance auto-creates the singleton if the scene has none.
            // Its OnEnable throws KeyNotFoundException when Assets/XR/RayNeoGeneralSettings.asset
            // has no touch config for the current device type (then the touchpad is dead).
            SimpleTouchForLite touch;
            try { touch = SimpleTouchForLite.Instance; }
            catch (Exception e)
            {
                Debug.LogError($"[AnchorInput] Touchpad init failed, check RayNeoGeneralSettings touchConfigs: {e.Message}");
                return;
            }
            touch.OnSimpleTap.AddListener(HandleTap);
            touch.OnTripleTap.AddListener(HandleTripleTap);
            touch.OnDoubleTap.AddListener(HandleDoubleTap);
            // NOTE: use the per-step OnSwipeLeft/Right events. The OnSwipe*End events never
            // fire in ARDK 1.1.2 (their internal LEFT_SWIPE/RIGHT_SWIPE state is never set).
            // The ARDK also maps LEFT_SWIPE_STEP -> OnSwipeRight, so the physical direction
            // must be confirmed on device; use invertSwipe to flip it.
            touch.OnSwipeRight.AddListener(HandleSwipeRight);
            touch.OnSwipeLeft.AddListener(HandleSwipeLeft);
            touchpadBound = true;
        }

        private void OnDisable()
        {
            // Only unbind if the singleton still exists; touching Instance here would
            // re-create it during scene unload / app quit.
            if (!touchpadBound || !SimpleTouchForLite.SingletonExist) return;
            var touch = SimpleTouchForLite.Instance;
            touch.OnSimpleTap.RemoveListener(HandleTap);
            touch.OnTripleTap.RemoveListener(HandleTripleTap);
            touch.OnDoubleTap.RemoveListener(HandleDoubleTap);
            touch.OnSwipeRight.RemoveListener(HandleSwipeRight);
            touch.OnSwipeLeft.RemoveListener(HandleSwipeLeft);
            touchpadBound = false;
        }

        private void Update()
        {
            // Trigger: menu open and no input for menuTimeoutSeconds -> close without placing.
            if (IsMenuOpen && Time.unscaledTime >= menuCloseTime) CloseMenu("Cancelled");

#if UNITY_EDITOR
            // Editor keyboard stand-ins for the touchpad (new Input System API).
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) HandleTap();
            if (kb.backspaceKey.wasPressedThisFrame) HandleTripleTap();
            if (kb.rightArrowKey.wasPressedThisFrame) MoveSelection(+1);
            if (kb.leftArrowKey.wasPressedThisFrame) MoveSelection(-1);
            if (kb.escapeKey.wasPressedThisFrame && IsMenuOpen) CloseMenu("Cancelled");
            UpdateEditorWalk(kb);
#endif
        }

#if UNITY_EDITOR
        // ---- Editor-only head simulation: WASD = walk (relative to facing), Q / E = turn ----
        // In the Editor the ARDK's HeadTrackedPoseDriver overwrites the camera transform every
        // frame with its debug pose, so the pose is fed through its static SetPosition /
        // SetQuaternion hooks instead of moving the transform. Compiled out of device builds.
        private const float EditorWalkSpeed = 1.2f; // m/s, roughly walking pace
        private const float EditorTurnSpeed = 90f;  // deg/s
        private Vector3 editorHeadPos;
        private float editorHeadYaw;

        private void UpdateEditorWalk(Keyboard kb)
        {
            float turn = (kb.eKey.isPressed ? 1f : 0f) - (kb.qKey.isPressed ? 1f : 0f);
            editorHeadYaw += turn * EditorTurnSpeed * Time.deltaTime;
            Quaternion rot = Quaternion.Euler(0f, editorHeadYaw, 0f);

            Vector3 dir = new Vector3(
                (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f),
                0f,
                (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f));
            editorHeadPos += rot * dir.normalized * EditorWalkSpeed * Time.deltaTime;

            HeadTrackedPoseDriver.SetPosition(editorHeadPos);
            HeadTrackedPoseDriver.SetQuaternion(rot);
        }
#endif

        // Trigger: SimpleTouchForLite.OnSimpleTap (fires ~0.4 s after the tap, once the SDK
        // is sure it was not a double/triple tap).
        private void HandleTap()
        {
            if (!IsMenuOpen) OpenMenu();
            else ConfirmPlacement();
        }

        // Trigger: SimpleTouchForLite.OnDoubleTap. Only acts while the menu is open (= back / cancel);
        // with the menu closed the double tap is left to the system.
        private void HandleDoubleTap()
        {
            if (IsMenuOpen) CloseMenu("Cancelled");
        }

        // Trigger: SimpleTouchForLite.OnTripleTap. Only acts when the menu is closed.
        private void HandleTripleTap()
        {
            if (IsMenuOpen) return;
            RemoveNearest();
        }

        // Trigger: SimpleTouchForLite.OnSwipeRight (fires each time a swipe travels ~200 touch px).
        private void HandleSwipeRight(Vector2 _) => HandleSwipe(invertSwipe ? -1 : +1);

        // Trigger: SimpleTouchForLite.OnSwipeLeft.
        private void HandleSwipeLeft(Vector2 _) => HandleSwipe(invertSwipe ? +1 : -1);

        private void HandleSwipe(int dir)
        {
            // Threshold: ignore repeats within swipeCooldownSeconds of the last accepted swipe.
            if (Time.unscaledTime - lastSwipeTime < swipeCooldownSeconds) return;
            lastSwipeTime = Time.unscaledTime;
            MoveSelection(dir);
        }

        private void OpenMenu()
        {
            IsMenuOpen = true;
            menuCloseTime = Time.unscaledTime + menuTimeoutSeconds;
            Debug.Log("[AnchorInput] Menu opened");
            OnMenuChanged?.Invoke();
        }

        private void CloseMenu(string toast)
        {
            IsMenuOpen = false;
            Debug.Log($"[AnchorInput] Menu closed ({toast})");
            if (!string.IsNullOrEmpty(toast)) OnToast?.Invoke(toast);
            OnMenuChanged?.Invoke();
        }

        /// <summary>Swipe only changes the selection while the menu is open.</summary>
        private void MoveSelection(int dir)
        {
            if (!IsMenuOpen) return;
            int i = Array.IndexOf(MenuOrder, SelectedType);
            if (i < 0) i = 0;
            i = (i + dir + MenuOrder.Length) % MenuOrder.Length;
            SelectedType = MenuOrder[i];
            menuCloseTime = Time.unscaledTime + menuTimeoutSeconds; // any input resets the timeout
            OnMenuChanged?.Invoke();
        }

        private void ConfirmPlacement()
        {
            // Guard: never save an anchor while SLAM is not tracking (position would be garbage).
            // The menu stays open so the user can look around and tap again.
            if (!slam.IsTracking)
            {
                menuCloseTime = Time.unscaledTime + menuTimeoutSeconds;
                OnToast?.Invoke(slam.IsDrifting ? "Tracking drift - hold still" : "No tracking - look around");
                return;
            }

            // Anchor = the user's own position (head projected down); look direction is ignored.
            Vector3 p = detector.HeadPosition;
            p.y -= markerDropMeters;
            store.Add(SelectedType, p);
            CloseMenu($"{SelectedType} zone set here");
        }

        public void RemoveNearest()
        {
            if (store.RemoveNearest(detector.HeadPosition)) OnToast?.Invoke("Nearest anchor removed");
            else OnToast?.Invoke("No anchors");
        }
    }
}
