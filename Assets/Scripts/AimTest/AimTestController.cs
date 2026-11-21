using System;
using System.Collections.Generic;
using RayNeo;              // SimpleTouchForLite
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using RayneoIdle.UI;
using RayneoIdle.Zones;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.AimTest
{
    /// <summary>
    /// Aim-to-select test (AimTest.unity only; research doc Gaze_Pointer_Research.md in the working folder).
    /// Flow: turn to the map -> our reticle appears -> aim at an object -> it highlights -> temple tap runs it ->
    /// turn back -> reticle hides. Uses the normal HUD, panels and treasure map; adds three test objects (Tree, Rock,
    /// building Site) plus a small RESET sign, and counters kept in memory only.
    ///
    /// Triggers (all tunable in the Inspector):
    /// - Enter map view: 3D angle between head forward and the map center below enterAngleDeg for enterHoldSeconds,
    ///   no panel open (PanelHost.AnyOpen also covers the Start screen and the reward window), tracking not lost.
    /// - Exit map view: angle above exitAngleDeg, a panel opens, tracking lost (SlamSessionController drift guard)
    ///   or the app pauses.
    /// - In map view this handler sits on the InputAdapter stack: every tap goes to the map (also on empty space,
    ///   so the toolbar never gets it), swipes are ignored, double tap (Back) falls through to the HUD.
    /// - Tap target (decision A): the object under the reticle at finger-down (SimpleTouchForLite.OnTouchStart),
    ///   used if the tap arrives within touchSnapshotMaxAge. The tap itself arrives about 0.4 s after the finger
    ///   lifts (SDK double-tap window, unchanged). Editor (no touchpad): the object under the reticle at the tap.
    /// - A tap that was started in map view but arrives after map view ended is swallowed (no toolbar action).
    /// - While an object plays its feedback (workingSeconds) it shows "Working..." and ignores taps (never queued).
    /// The SDK gaze dot is hidden all the time by GazeDotHider; this reticle is drawn on the map canvas only.
    /// Editor: Q / E look forward / right (EditorHeadLook), hold J / L / I / K to turn the head slowly, Space = tap.
    /// Log tag [AimTest]: one line per enter / exit, target change, touch-down, tap and result.
    /// </summary>
    [DefaultExecutionOrder(70)] // after TreasureMapView / MainHud / PanelHost built their UI in Start
    public class AimTestController : MonoBehaviour, IGameInputHandler
    {
        private const string LogTag = "[AimTest]";

        [Header("Map view (head direction vs map center)")]
        [Tooltip("Enter map view when the angle to the map center stays below this (degrees).")]
        [SerializeField] private float enterAngleDeg = 15f;
        [Tooltip("Seconds the angle must stay below enterAngleDeg before map view starts.")]
        [SerializeField] private float enterHoldSeconds = 0.2f;
        [Tooltip("Leave map view when the angle goes above this (degrees). Larger than enter = no flicker at the edge.")]
        [SerializeField] private float exitAngleDeg = 20f;

        [Header("Taps")]
        [Tooltip("A finger-down target is used for a tap that arrives within this many seconds (tap delay is ~0.4 s).")]
        [SerializeField] private float touchSnapshotMaxAge = 1.2f;
        [Tooltip("Seconds an object plays its feedback and ignores taps (shows Working...).")]
        [SerializeField] private float workingSeconds = 0.5f;
        [Tooltip("Seconds the result text (Wood +1, Built ...) stays on the map.")]
        [SerializeField] private float resultSeconds = 1.5f;

        [Header("Editor")]
        [Tooltip("Head turn speed for J / L / I / K in the Editor (degrees per second).")]
        [SerializeField] private float editorTurnDegPerSec = 12f;

        private enum Kind { Tree, Rock, Site, Reset }

        private class Target
        {
            public Kind kind;
            public string name;
            public RectInt hit;            // map texels
            public RawImage art, outline;
            public Texture2D normal, flash, built, builtFlash;
            public float busyUntil;
        }

        // Sprite codes: S = main cyan, I = secondary, A = green accent (Palette.FromCode). Top row first.
        private static readonly string[] TreeSprite =
        {
            ".....S.....", "....S.S....", "...S...S...", "..SSS.SSS..", "...S...S...", "..S.....S..",
            ".SSSS.SSSS.", "..S.....S..", ".S.......S.", "SSSSSSSSSSS", ".....I.....", ".....I.....", "....III....",
        };
        private static readonly string[] RockSprite =
        {
            "...SSSS.....", "..S....SS...", ".S.......S..", "S.....S...S.", "S....S.....S", "S..........S", "SSSSSSSSSSSS",
        };
        private static readonly string[] SiteSprite =
        {
            "S...........S", "S...........S", "S.S.S.S.S.S.S", "I...........I", "I...........I", "IIIIIIIIIIIII",
        };
        private static readonly string[] BuildingSprite =
        {
            "......S......", ".....S.S.....", "....S...S....", "...S.....S...", "..S.......S..", ".SSSSSSSSSSS.",
            ".S.........S.", ".S..SS..SS.S.", ".S..SS..SS.S.", ".S.........S.", ".S...SSS...S.", "SSSSSSSSSSSSS",
        };
        private const float ArtPxPerTexel = 8f; // sprite pixel = 2 map texels, so objects are about 2.5 deg wide

        private TreasureMapView map;
        private PanelHost panels;
        private GameSession session;
        private Transform head;
        private RectTransform mapRt;
        private readonly List<Target> targets = new List<Target>();
        private RawImage reticle;
        private Text hintText, countText;

        private bool aimMode, pushed, trackingLost;
        private float enterSince = -1f;
        private Target hover;
        private int wood, stone;
        private bool siteBuilt;
        private string resultText;
        private float resultUntil;

        // Finger-down snapshot (decision A).
        private bool touchValid, touchInAim;
        private Target touchTarget;
        private float touchTime;
        private float swallowUntil = -1f; // a tap started in map view that arrives after exit is swallowed until then

        private float editorYaw, editorPitch;

        // ================================================================ setup

        private void Start()
        {
            map = FindObjectOfType<TreasureMapView>();
            panels = FindObjectOfType<PanelHost>();
            session = GameSession.Instance;
            head = UiFactory.FindHead();
            if (map == null || panels == null || session == null || head == null || map.MapRect == null || InputAdapter.Instance == null)
            {
                Debug.LogError($"{LogTag} needs TreasureMapView, PanelHost, GameSession, InputAdapter and a head camera");
                enabled = false;
                return;
            }
            mapRt = map.MapRect;
            map.ChestInputEnabled = false; // the old chest focus / open logic must never take a tap here

            BuildOverlay();

            if (!Application.isEditor)
            {
                try { SimpleTouchForLite.Instance.OnTouchStart.AddListener(HandleTouchStart); }
                catch (Exception e) { Debug.LogError($"{LogTag} touchpad not available: {e.Message}"); }
            }
            var slam = FindObjectOfType<SlamSessionController>();
            if (slam != null) slam.OnTrackingChanged += HandleTrackingChanged;
            session.OnStateReplaced += ResetTest; // Settings > Reset also resets the test

            var e0 = head.rotation.eulerAngles;
            editorYaw = e0.y; editorPitch = e0.x;
            Debug.Log($"{LogTag} ready: enter < {enterAngleDeg} deg for {enterHoldSeconds} s, exit > {exitAngleDeg} deg, " +
                      $"targets {string.Join(", ", targets.ConvertAll(t => $"{t.name} {t.hit}"))}");
        }

        private void BuildOverlay()
        {
            AddTarget(Kind.Tree, "Tree", TreeSprite, new Vector2Int(36, 62));
            AddTarget(Kind.Rock, "Rock", RockSprite, new Vector2Int(100, 60));
            AddTarget(Kind.Site, "Site", SiteSprite, new Vector2Int(70, 28), BuildingSprite);
            AddResetSign(new RectInt(104, 7, 24, 10));

            var ring = new PixelCanvas(7, 7);
            ring.Clear();
            ring.RectOutline(1, 0, 5, 7, Palette.Main);
            ring.RectOutline(0, 1, 7, 5, Palette.Main);
            ring.Set(3, 3, Palette.Main);
            reticle = UiFactory.CreatePixelImage(mapRt, "Aim reticle", ring.ToTexture(), 4f, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            reticle.enabled = false;

            // Hint above the map (the map's own progress row area, unused here). Font 26 at 2 / 2.9 m = ~18 px.
            var canvas = (RectTransform)mapRt.parent;
            hintText = UiFactory.CreateText(canvas, "Aim hint", 26, Palette.Main, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(560, 40));
            countText = UiFactory.CreateText(mapRt, "Aim counters", 26, Palette.Secondary, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(0f, 0.5f), new Vector2(14 * TreasureMapView.TexelPx, 88 * TreasureMapView.TexelPx), new Vector2(300, 36));
            RefreshTexts();
        }

        private void AddTarget(Kind kind, string name, string[] sprite, Vector2Int centerTexel, string[] builtSprite = null)
        {
            var t = new Target { kind = kind, name = name };
            t.normal = PixelCanvas.FromSprite(sprite);
            t.flash = PixelCanvas.FromSprite(ToAccent(sprite));
            if (builtSprite != null)
            {
                t.built = PixelCanvas.FromSprite(builtSprite);
                t.builtFlash = PixelCanvas.FromSprite(ToAccent(builtSprite));
            }
            float texelsPerSpritePx = ArtPxPerTexel / TreasureMapView.TexelPx;
            // Hit area: the largest art of this object plus 2 texels on each side.
            int w = Mathf.CeilToInt(Mathf.Max(t.normal.width, t.built != null ? t.built.width : 0) * texelsPerSpritePx) + 4;
            int h = Mathf.CeilToInt(Mathf.Max(t.normal.height, t.built != null ? t.built.height : 0) * texelsPerSpritePx) + 4;
            t.hit = new RectInt(centerTexel.x - w / 2, centerTexel.y - h / 2, w, h);

            t.outline = UiFactory.CreatePixelImage(mapRt, $"{name} highlight", DrawFrame(w, h), TreasureMapView.TexelPx,
                Vector2.zero, Vector2.zero, new Vector2(t.hit.x, t.hit.y) * TreasureMapView.TexelPx);
            t.outline.enabled = false;
            // Art stands on the bottom of the hit area (2 texels up), centered.
            t.art = UiFactory.CreatePixelImage(mapRt, name, t.normal, ArtPxPerTexel,
                Vector2.zero, new Vector2(0.5f, 0f), new Vector2(centerTexel.x, t.hit.y + 2) * TreasureMapView.TexelPx);
            targets.Add(t);
        }

        private void AddResetSign(RectInt hit)
        {
            var t = new Target { kind = Kind.Reset, name = "Reset", hit = hit };
            var frame = new PixelCanvas(hit.width, hit.height);
            frame.Clear();
            frame.RectOutline(0, 0, hit.width, hit.height, Palette.Secondary);
            t.normal = frame.ToTexture();
            var flashPc = new PixelCanvas(hit.width, hit.height);
            flashPc.Clear();
            flashPc.RectOutline(0, 0, hit.width, hit.height, Palette.Accent);
            t.flash = flashPc.ToTexture();
            t.art = UiFactory.CreatePixelImage(mapRt, "Reset sign", t.normal, TreasureMapView.TexelPx,
                Vector2.zero, Vector2.zero, new Vector2(hit.x, hit.y) * TreasureMapView.TexelPx);
            UiFactory.CreateText(t.art.rectTransform, "Reset label", 24, Palette.Secondary, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, t.art.rectTransform.sizeDelta).text = "RESET";
            t.outline = UiFactory.CreatePixelImage(mapRt, "Reset highlight", DrawFrame(hit.width, hit.height), TreasureMapView.TexelPx,
                Vector2.zero, Vector2.zero, new Vector2(hit.x, hit.y) * TreasureMapView.TexelPx);
            t.outline.enabled = false;
            targets.Add(t);
        }

        /// <summary>Cyan highlight frame, 1 texel (4 px) lines, 1 texel inset so it does not touch the art.</summary>
        private static Texture2D DrawFrame(int w, int h)
        {
            var pc = new PixelCanvas(w, h);
            pc.Clear();
            pc.RectOutline(0, 0, w, h, Palette.Main);
            return pc.ToTexture();
        }

        private static string[] ToAccent(string[] rows)
        {
            var o = new string[rows.Length];
            for (int i = 0; i < rows.Length; i++) o[i] = rows[i].Replace('S', 'A').Replace('I', 'A');
            return o;
        }

        // ================================================================ loop

        private void Update()
        {
            EditorTurn();
            float now = Time.unscaledTime;
            float angle = AngleToMapCenter();
            bool blocked = panels.AnyOpen || trackingLost;

            if (!aimMode)
            {
                // Trigger: angle < enterAngleDeg held for enterHoldSeconds, nothing blocking.
                if (!blocked && angle < enterAngleDeg)
                {
                    if (enterSince < 0) enterSince = now;
                    if (now - enterSince >= enterHoldSeconds) EnterAim(angle);
                }
                else enterSince = -1f;
            }
            else if (blocked) ExitAim(panels.AnyOpen ? "panel open" : "tracking lost");
            else if (angle > exitAngleDeg) ExitAim($"angle {angle:F1}");

            if (aimMode) UpdateAim();

            // Swallow window over: stop listening for the late tap.
            if (!aimMode && swallowUntil > 0 && now > swallowUntil) { swallowUntil = -1f; PopHandler(); }

            UpdateFeedback(now);
            RefreshTexts();
        }

        private float AngleToMapCenter()
        {
            var r = mapRt.rect;
            Vector3 center = mapRt.TransformPoint(r.center);
            return Vector3.Angle(head.forward, center - head.position);
        }

        private void EnterAim(float angle)
        {
            aimMode = true;
            enterSince = -1f;
            swallowUntil = -1f;
            map.FollowLocked = true;
            InputAdapter.Instance.Push(this);
            pushed = true;
            Debug.Log($"{LogTag} enter map view (angle {angle:F1})");
        }

        private void ExitAim(string reason)
        {
            if (!aimMode) return;
            aimMode = false;
            map.FollowLocked = false;
            SetHover(null);
            reticle.enabled = false;
            // A tap whose finger went down in map view may still arrive (~0.4 s): swallow it instead of letting the
            // toolbar run it. Otherwise leave the input stack now.
            bool pendingTap = touchValid && touchInAim && Time.unscaledTime - touchTime <= touchSnapshotMaxAge && reason != "pause";
            if (pendingTap) swallowUntil = touchTime + touchSnapshotMaxAge;
            else PopHandler();
            Debug.Log($"{LogTag} exit map view ({reason}){(pendingTap ? ", waiting to swallow a late tap" : "")}");
        }

        private void PopHandler()
        {
            if (pushed && InputAdapter.Instance != null) InputAdapter.Instance.Pop(this);
            pushed = false;
        }

        /// <summary>Head ray -> map plane -> map texels -> first target whose hit area contains the point.</summary>
        private void UpdateAim()
        {
            var plane = new Plane(mapRt.forward, mapRt.position);
            var ray = new Ray(head.position, head.forward);
            if (!plane.Raycast(ray, out float dist) || dist <= 0)
            {
                reticle.enabled = false;
                SetHover(null);
                return;
            }
            Vector3 local = mapRt.InverseTransformPoint(ray.GetPoint(dist));
            Vector2 texel = ((Vector2)local - mapRt.rect.min) / TreasureMapView.TexelPx;
            reticle.enabled = true;
            reticle.rectTransform.anchoredPosition = texel * TreasureMapView.TexelPx;

            Target hit = null;
            foreach (var t in targets)
                if (texel.x >= t.hit.xMin && texel.x < t.hit.xMax && texel.y >= t.hit.yMin && texel.y < t.hit.yMax) { hit = t; break; }
            SetHover(hit);
        }

        private void SetHover(Target t)
        {
            if (t == hover) return;
            if (hover != null) hover.outline.enabled = false;
            hover = t;
            if (hover != null) hover.outline.enabled = true;
            Debug.Log($"{LogTag} aim {(hover != null ? hover.name : "empty")}");
        }

        // ================================================================ input

        // Trigger: finger touches the temple (SDK, no delay). Remember what the reticle is on (decision A).
        private void HandleTouchStart(Vector2 pos)
        {
            touchValid = true;
            touchInAim = aimMode;
            touchTarget = aimMode ? hover : null;
            touchTime = Time.unscaledTime;
            Debug.Log($"{LogTag} touch down: {(aimMode ? (touchTarget != null ? touchTarget.name : "empty") : "outside map view")}");
        }

        /// <summary>On the stack only in map view (plus a short swallow window after exit).</summary>
        public bool HandleAction(GameAction action)
        {
            if (!aimMode)
            {
                // Swallow window: only the late tap is eaten, everything else passes down.
                if (action == GameAction.Confirm && swallowUntil > 0)
                {
                    Debug.Log($"{LogTag} tap ignored: started in map view, arrived after exit");
                    touchValid = false;
                    swallowUntil = -1f;
                    PopHandler();
                    return true;
                }
                return false;
            }

            switch (action)
            {
                case GameAction.Confirm:
                    HandleTap();
                    return true;
                case GameAction.Back:
                    touchValid = false; // a double tap is never a gameplay tap
                    return false;       // Back keeps its normal meaning (falls through to the HUD)
                default:
                    touchValid = false;
                    Debug.Log($"{LogTag} swipe {action} ignored in map view");
                    return true;        // no swipe selection in map view (decision B)
            }
        }

        private void HandleTap()
        {
            float now = Time.unscaledTime;
            Target t;
            string source;
            if (touchValid && now - touchTime <= touchSnapshotMaxAge)
            {
                if (!touchInAim)
                {
                    Debug.Log($"{LogTag} tap ignored: finger went down outside map view");
                    touchValid = false;
                    return;
                }
                t = touchTarget;
                source = "touch-down";
            }
            else { t = hover; source = Application.isEditor ? "current (Editor)" : "current (no touch-down)"; }
            touchValid = false;

            string current = hover != null ? hover.name : "empty";
            if (t == null) { Debug.Log($"{LogTag} tap on empty space ({source}; current {current}): nothing"); return; }
            if (now < t.busyUntil) { Debug.Log($"{LogTag} tap ignored: {t.name} working ({source}; current {current})"); return; }
            Run(t, source, current);
        }

        private void Run(Target t, string source, string current)
        {
            string result;
            switch (t.kind)
            {
                case Kind.Tree: wood++; result = "Wood +1"; break;
                case Kind.Rock: stone++; result = "Stone +1"; break;
                case Kind.Site:
                    if (!siteBuilt) { siteBuilt = true; result = "Built"; }
                    else result = "Inspected";
                    break;
                default: ResetTest(); result = "Test reset"; break;
            }
            t.busyUntil = Time.unscaledTime + workingSeconds;
            ShowArt(t, flash: true);
            resultText = result;
            resultUntil = Time.unscaledTime + resultSeconds;
            Debug.Log($"{LogTag} tap {t.name} ({source}; current {current}) -> {result}; wood {wood}, stone {stone}, built {siteBuilt}");
        }

        private void ShowArt(Target t, bool flash)
        {
            bool built = t.kind == Kind.Site && siteBuilt;
            var tex = built ? (flash ? t.builtFlash : t.built) : (flash ? t.flash : t.normal);
            t.art.texture = tex;
            float scale = t.kind == Kind.Reset ? TreasureMapView.TexelPx : ArtPxPerTexel;
            t.art.rectTransform.sizeDelta = new Vector2(tex.width, tex.height) * scale;
        }

        private void UpdateFeedback(float now)
        {
            foreach (var t in targets)
                if (t.busyUntil > 0 && now >= t.busyUntil) { t.busyUntil = 0; ShowArt(t, flash: false); }
            if (resultText != null && now >= resultUntil) resultText = null;
        }

        private void RefreshTexts()
        {
            string hint = "";
            Color color = Palette.Main;
            if (resultText != null) { hint = resultText; color = Palette.Accent; }
            else if (aimMode && hover != null)
            {
                if (Time.unscaledTime < hover.busyUntil) hint = "Working...";
                else switch (hover.kind)
                {
                    case Kind.Tree: hint = "Tree · Tap to collect"; break;
                    case Kind.Rock: hint = "Rock · Tap to mine"; break;
                    case Kind.Site: hint = siteBuilt ? "Building · Tap to inspect" : "Site · Tap to build"; break;
                    default: hint = "Reset · Tap to reset the test"; break;
                }
            }
            hintText.text = hint;
            hintText.color = color;
            countText.text = $"Wood {wood}   Stone {stone}";
        }

        private void ResetTest()
        {
            wood = 0; stone = 0; siteBuilt = false;
            foreach (var t in targets) { t.busyUntil = 0; ShowArt(t, flash: false); }
            Debug.Log($"{LogTag} test reset");
        }

        // ================================================================ interruptions

        private void HandleTrackingChanged(bool tracking)
        {
            trackingLost = !tracking;
            if (trackingLost) ExitAim("tracking lost");
            Debug.Log($"{LogTag} tracking {(tracking ? "back" : "lost")}");
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            touchValid = false;
            ExitAim("pause");
            swallowUntil = -1f;
            PopHandler();
        }

        private void OnDisable()
        {
            if (map != null) map.FollowLocked = false;
            PopHandler();
        }

        // Editor only: hold J / L (yaw) and I / K (pitch) to aim slowly; Q / E snaps stay in EditorHeadLook.
        private void EditorTurn()
        {
            if (!Application.isEditor) return;
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
            {
                // EditorHeadLook snaps this frame; read the result next frame.
                editorYaw = float.NaN;
                return;
            }
            if (float.IsNaN(editorYaw)) { var e = head.rotation.eulerAngles; editorYaw = e.y; editorPitch = e.x; }
            float d = editorTurnDegPerSec * Time.unscaledDeltaTime;
            bool moved = false;
            if (Input.GetKey(KeyCode.J)) { editorYaw -= d; moved = true; }
            if (Input.GetKey(KeyCode.L)) { editorYaw += d; moved = true; }
            if (Input.GetKey(KeyCode.I)) { editorPitch -= d; moved = true; }
            if (Input.GetKey(KeyCode.K)) { editorPitch += d; moved = true; }
            if (moved) head.rotation = Quaternion.Euler(editorPitch, editorYaw, 0);
        }
    }
}
