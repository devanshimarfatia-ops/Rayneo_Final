using System;
using UnityEngine;
using UnityEngine.InputSystem;
using RayNeo; // SimpleTouchForLite (ARDK SDK/Runtime/Scripts/Comps/SimpleTouchForLite.cs)

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Test controls for placing anchors.
    ///
    ///   Swipe forward/back -> cycle selected ZoneType   (Editor: Left / Right arrow)
    ///   Single tap         -> place anchor at your current standing spot (Editor: Space)
    ///   Triple tap         -> remove nearest anchor     (Editor: Backspace)
    ///   Double tap         -> RESERVED for system back/exit, not used here.
    ///
    /// The anchor is placed at the HEAD position projected to the floor plane,
    /// so the workflow is: sit/stand where you usually work, select Work, tap.
    /// </summary>
    public class AnchorPlacementInput : MonoBehaviour
    {
        [SerializeField] private ZoneAnchorStore store;
        [SerializeField] private ZoneDetector detector;
        [SerializeField] private SlamSessionController slam;

        [Header("Touchpad")]
        [Tooltip("Flip if swiping forward on the X3 Pro temple selects the previous type instead of the next.")]
        [SerializeField] private bool invertSwipe = false;
        [Tooltip("Swipe events fire repeatedly during one long swipe (every ~200 touch px). " +
                 "Swipes closer together than this are ignored, so one swipe = one step.")]
        [SerializeField] private float swipeCooldownSeconds = 0.35f;
        [Tooltip("Also listen to the touchpad in the Editor (mouse clicks in the Game view count as taps).")]
        [SerializeField] private bool useTouchpadInEditor = false;

        public ZoneType SelectedType { get; private set; } = ZoneType.Work;
        public event Action<string> OnToast; // short status messages for the HUD

        private static readonly ZoneType[] AllTypes = (ZoneType[])Enum.GetValues(typeof(ZoneType));

        private float lastSwipeTime = -999f;
        private bool touchpadBound;

        private bool UseTouchpad => !Application.isEditor || useTouchpadInEditor;

        private void OnEnable()
        {
            if (!UseTouchpad) return;

            // SimpleTouchForLite.Instance auto-creates the singleton if the scene has none.
            var touch = SimpleTouchForLite.Instance;
            touch.OnSimpleTap.AddListener(PlaceAnchor);
            touch.OnTripleTap.AddListener(RemoveNearest);
            // NOTE: use the per-step OnSwipeLeft/Right events. The OnSwipe*End events never
            // fire in ARDK 1.1.2 (their internal LEFT_SWIPE/RIGHT_SWIPE state is never set).
            // The ARDK also maps LEFT_SWIPE_STEP -> OnSwipeRight, so the physical direction
            // must be confirmed on device; use invertSwipe to flip it.
            touch.OnSwipeRight.AddListener(HandleSwipeRight);
            touch.OnSwipeLeft.AddListener(HandleSwipeLeft);
            // OnDoubleTap is intentionally NOT used: double tap = system back/exit.
            touchpadBound = true;
        }

        private void OnDisable()
        {
            // Only unbind if the singleton still exists; touching Instance here would
            // re-create it during scene unload / app quit.
            if (!touchpadBound || !SimpleTouchForLite.SingletonExist) return;
            var touch = SimpleTouchForLite.Instance;
            touch.OnSimpleTap.RemoveListener(PlaceAnchor);
            touch.OnTripleTap.RemoveListener(RemoveNearest);
            touch.OnSwipeRight.RemoveListener(HandleSwipeRight);
            touch.OnSwipeLeft.RemoveListener(HandleSwipeLeft);
            touchpadBound = false;
        }

        private void Update()
        {
#if UNITY_EDITOR
            // The project uses the new Input System only (Active Input Handling = Input System),
            // so the legacy UnityEngine.Input API would throw here.
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) PlaceAnchor();
            if (kb.backspaceKey.wasPressedThisFrame) RemoveNearest();
            if (kb.rightArrowKey.wasPressedThisFrame) NextType();
            if (kb.leftArrowKey.wasPressedThisFrame) PrevType();
#endif
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
            Cycle(dir);
        }

        public void PlaceAnchor()
        {
            // Guard: never save an anchor while SLAM is not tracking (position would be garbage).
            if (!slam.IsTracking) { OnToast?.Invoke("Tracking lost - look around"); return; }

            Vector3 p = detector.HeadPosition;
            p.y = 0f;
            store.Add(SelectedType, p);
            OnToast?.Invoke($"{SelectedType} anchor placed");
        }

        public void RemoveNearest()
        {
            if (store.RemoveNearest(detector.HeadPosition)) OnToast?.Invoke("Nearest anchor removed");
            else OnToast?.Invoke("No anchors");
        }

        public void NextType() => Cycle(+1);
        public void PrevType() => Cycle(-1);

        private void Cycle(int dir)
        {
            int i = Array.IndexOf(AllTypes, SelectedType);
            i = (i + dir + AllTypes.Length) % AllTypes.Length;
            SelectedType = AllTypes[i];
            OnToast?.Invoke($"Selected: {SelectedType}");
        }
    }
}
