using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Minimal debug HUD following RayNeo display specs:
    /// black (transparent) background, bold text >= 16px, cyan/green palette,
    /// toast in the top area that hides after ~2.5 s, anchor menu just above the center dot.
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
        [SerializeField] private Text menuText;    // center, above the gaze dot; empty when the menu is closed

        [SerializeField] private float toastSeconds = 2.5f;

        private static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        private static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);
        private static readonly Color Purple = new Color32(0x85, 0x66, 0xFF, 0xFF);
        private const string CyanHex = "#4DE2FF";

        private float toastHideTime;
        private float nextRefresh;

        private void OnEnable()
        {
            input.OnToast += ShowToast;
            input.OnMenuChanged += RefreshMenu;
            modes.OnModeChanged += HandleModeChanged;
        }

        private void OnDisable()
        {
            input.OnToast -= ShowToast;
            input.OnMenuChanged -= RefreshMenu;
            modes.OnModeChanged -= HandleModeChanged;
        }

        private void Start()
        {
            toastText.text = "";
            RefreshMenu();
            HandleModeChanged(AppMode.Default, modes.CurrentMode);
        }

        private void Update()
        {
            if (toastText.text.Length > 0 && Time.unscaledTime > toastHideTime) toastText.text = "";

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;

            // s = raw Algorithm.GetSlamStatus(): 0 init, 1 success, 2 fail, -1 not polled (Editor)
            string track = (slam.IsTracking ? "TRACKING" : "NO TRACKING") +
                           (slam.RawStatus >= 0 ? $" s{slam.RawStatus}" : "");
            string dist = float.IsInfinity(detector.NearestDistance) ? "-" : $"{detector.NearestDistance:F1}m";
            string dwell = detector.CandidateAnchor != null
                ? $"  entering {detector.CandidateAnchor.zoneType} {detector.CandidateProgress * 100f:F0}%"
                : "";
            statusText.text = $"{track} | anchors {store.Anchors.Count} | near {dist}\n" +
                              (input.IsMenuOpen ? "Swipe: choose  Tap: confirm" : "Tap: set anchor  3x tap: remove") +
                              dwell;
        }

        private void HandleModeChanged(AppMode prev, AppMode mode)
        {
            modeText.text = mode == AppMode.Default ? "" : $"{mode.ToString().ToUpper()} MODE";
            modeText.color = mode == AppMode.Work ? Cyan : mode == AppMode.Bedroom ? Purple : Green;
            if (mode != AppMode.Default) ShowToast($"Entered {mode} zone");
            else if (prev != AppMode.Default) ShowToast($"Left {prev} zone");
        }

        // Trigger: AnchorPlacementInput.OnMenuChanged (open / close / selection moved).
        // Selected option = cyan with brackets, others = the text's base color (green).
        private void RefreshMenu()
        {
            if (menuText == null) return;
            if (!input.IsMenuOpen) { menuText.text = ""; return; }

            var sb = new System.Text.StringBuilder("<size=20>SET ANCHOR</size>\n");
            foreach (var type in AnchorPlacementInput.MenuOrder)
            {
                string name = type.ToString().ToUpper();
                sb.Append(type == input.SelectedType ? $"<color={CyanHex}>[ {name} ]</color>" : name);
                sb.Append("    ");
            }
            menuText.text = sb.ToString().TrimEnd();
        }

        private void ShowToast(string msg)
        {
            toastText.text = msg;
            toastText.color = Cyan;
            toastHideTime = Time.unscaledTime + toastSeconds;
        }
    }
}
