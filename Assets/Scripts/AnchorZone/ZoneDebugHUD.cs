using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Minimal debug HUD following RayNeo display specs:
    /// black (transparent) background, bold text >= 16px, cyan/green palette,
    /// toast in the top area that hides after ~2.5 s, keeps the center clear.
    /// Updates text at ~5 Hz to save power.
    /// </summary>
    public class ZoneDebugHUD : MonoBehaviour
    {
        [SerializeField] private ZoneDetector detector;
        [SerializeField] private ModeManager modes;
        [SerializeField] private SlamSessionController slam;
        [SerializeField] private AnchorPlacementInput input;
        [SerializeField] private ZoneAnchorStore store;

        [Header("UI (legacy Text, font size >= 20, Bold)")]
        [SerializeField] private Text modeText;    // top-left
        [SerializeField] private Text statusText;  // bottom
        [SerializeField] private Text toastText;   // top-center

        [SerializeField] private float toastSeconds = 2.5f;

        private static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        private static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);

        private float toastHideTime;
        private float nextRefresh;

        private void OnEnable()
        {
            input.OnToast += ShowToast;
            modes.OnModeChanged += HandleModeChanged;
        }

        private void OnDisable()
        {
            input.OnToast -= ShowToast;
            modes.OnModeChanged -= HandleModeChanged;
        }

        private void Start()
        {
            toastText.text = "";
            HandleModeChanged(AppMode.Default, modes.CurrentMode);
        }

        private void Update()
        {
            if (toastText.text.Length > 0 && Time.unscaledTime > toastHideTime) toastText.text = "";

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;

            string track = slam.IsTracking ? "TRACKING" : "NO TRACKING";
            string dist = float.IsInfinity(detector.NearestDistance) ? "-" : $"{detector.NearestDistance:F1}m";
            string dwell = detector.CandidateAnchor != null
                ? $"  entering {detector.CandidateAnchor.zoneType} {detector.CandidateProgress * 100f:F0}%"
                : "";
            statusText.text = $"{track} | anchors {store.Anchors.Count} | near {dist}\n" +
                              $"Tap: place {input.SelectedType}  Swipe: type{dwell}";
        }

        private void HandleModeChanged(AppMode prev, AppMode mode)
        {
            modeText.text = mode == AppMode.Default ? "" : $"{mode.ToString().ToUpper()} MODE";
            modeText.color = mode == AppMode.Work ? Cyan : Green;
            if (mode != AppMode.Default) ShowToast($"Entered {mode} zone");
            else if (prev != AppMode.Default) ShowToast($"Left {prev} zone");
        }

        private void ShowToast(string msg)
        {
            toastText.text = msg;
            toastText.color = Cyan;
            toastHideTime = Time.unscaledTime + toastSeconds;
        }
    }
}
