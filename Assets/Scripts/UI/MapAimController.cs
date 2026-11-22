using System;
using RayNeo;              // SimpleTouchForLite
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using RayneoIdle.Zones;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Head-aim pointer on the treasure map (game scene; same rules as the aim test, AimTestController).
    /// Flow: turn to the map -> reticle appears -> aim at a chest or trash -> it gets a cyan frame + hint ->
    /// temple tap runs it (chest: open; trash: turns green = ordered, the explorer stops exploring, walks there, clears
    /// it and "+N" coins float over his head) -> turn back -> reticle hides.
    /// Trash errands: FIFO queue; while it is not empty IdleCore.SearchPaused is on (trash has priority over exploring).
    /// The SDK gaze dot stays hidden (GazeDotHider); main view has no pointer.
    ///
    /// Triggers (Inspector):
    /// - Enter map view: angle between head forward and the map center below enterAngleDeg for enterHoldSeconds,
    ///   no visible panel (a panel suspended by PanelHost does not block), tracking not lost.
    /// - Exit: angle above exitAngleDeg, a panel becomes visible (reward window, Start screen), tracking lost, pause.
    /// - In map view every tap goes to the map (also on empty space, so the toolbar never gets it); swipes are
    ///   ignored; double tap (Back) falls through.
    /// - Tap target = the target under the reticle at finger-down (SimpleTouchForLite.OnTouchStart), used if the tap
    ///   arrives within touchSnapshotMaxAge (the SDK delivers a tap ~0.4 s after the finger lifts; DEVICE_FINDINGS
    ///   2026-11-21). Editor: the target under the reticle at the tap.
    /// - A tap that started in map view but arrives after exit is swallowed (no toolbar action).
    /// - A target picks the reticle if its aim box/radius contains it; the nearest one wins.
    /// Replaces the old chest focus (swipe) + tap on the map: TreasureMapView.ChestInputEnabled is switched off.
    /// Log tag [MapAim].
    /// </summary>
    [DefaultExecutionOrder(70)] // after TreasureMapView / MainHud / PanelHost built their UI in Start
    public class MapAimController : MonoBehaviour, IGameInputHandler
    {
        private const string LogTag = "[MapAim]";

        [Header("Map view (head direction vs map center)")]
        [SerializeField] private float enterAngleDeg = 15f;
        [SerializeField] private float enterHoldSeconds = 0.2f;
        [Tooltip("Larger than enterAngleDeg so the reticle does not flicker at the edge.")]
        [SerializeField] private float exitAngleDeg = 20f;

        [Header("Taps")]
        [SerializeField] private float touchSnapshotMaxAge = 1.2f;
        [Tooltip("Seconds the +N coin text floats over the explorer.")]
        [SerializeField] private float coinFloatSeconds = 1.2f;
        [Tooltip("Texels the +N coin text rises while it floats.")]
        [SerializeField] private float coinFloatRiseTexels = 8f;
        [SerializeField] private float resultSeconds = 1.5f;

        [Tooltip("Extra texels around a chest's art that still count as aiming at it.")]
        [SerializeField] private float chestAimMargin = 3f;

        private enum Kind { None, Chest, Trash }

        private struct Target
        {
            public Kind kind;
            public int area;                     // chest
            public TrashTestOverlay.Item trash;  // trash
            public Rect box;                     // highlight box, texels
            public bool Same(Target o) => kind == o.kind && area == o.area && trash == o.trash;
            public string Name => kind == Kind.Chest ? $"chest {area}" : kind == Kind.Trash ? $"trash x{trash.trash}" : "empty";
        }

        private TreasureMapView map;
        private TrashTestOverlay trash;
        private PanelHost panels;
        private GameSession session;
        private Transform head;
        private RectTransform mapRt;
        private RawImage reticle, frame;
        private Text hintText;

        private bool aimMode, pushed, trackingLost;
        private float enterSince = -1f;
        private Target hover;
        private string resultText;
        private float resultUntil;
        // Trash errands: tapped trash waits here; "errand" is the one the explorer walks to now.
        private readonly System.Collections.Generic.List<TrashTestOverlay.Item> queue = new System.Collections.Generic.List<TrashTestOverlay.Item>();
        private TrashTestOverlay.Item errand;
        private class CoinFloat { public Text text; public float start; public Vector2 texel; }
        private readonly System.Collections.Generic.List<CoinFloat> floats = new System.Collections.Generic.List<CoinFloat>();

        private bool touchValid, touchInAim;
        private Target touchTarget;
        private float touchTime;
        private float swallowUntil = -1f;

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
            map.ChestInputEnabled = false; // the pointer replaces swipe focus + tap on chests
            trash = map.GetComponent<TrashTestOverlay>();
            if (trash == null) trash = map.gameObject.AddComponent<TrashTestOverlay>();
            if (FindObjectOfType<GazeDotHider>() == null) gameObject.AddComponent<GazeDotHider>();

            var ring = new PixelCanvas(7, 7);
            ring.Clear();
            ring.RectOutline(1, 0, 5, 7, Palette.Main);
            ring.RectOutline(0, 1, 7, 5, Palette.Main);
            ring.Set(3, 3, Palette.Main);
            reticle = UiFactory.CreatePixelImage(mapRt, "Aim reticle", ring.ToTexture(), 4f, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            reticle.enabled = false;
            frame = UiFactory.CreatePixelImage(mapRt, "Aim highlight", null, 1f, Vector2.zero, Vector2.zero, Vector2.zero);
            frame.enabled = false;
            // Hint inside the map, above the bottom rod. Font 24 at map size M (2 / 2.4 m) = ~20 px on the glasses.
            hintText = UiFactory.CreateText(mapRt, "Aim hint", 24, Palette.Main, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 10 * TreasureMapView.TexelPx), new Vector2(480, 32));

            if (!Application.isEditor)
            {
                try { SimpleTouchForLite.Instance.OnTouchStart.AddListener(HandleTouchStart); }
                catch (Exception e) { Debug.LogError($"{LogTag} touchpad not available: {e.Message}"); }
            }
            var slam = FindObjectOfType<SlamSessionController>();
            if (slam != null) slam.OnTrackingChanged += HandleTrackingChanged;
            Debug.Log($"{LogTag} ready: enter < {enterAngleDeg} deg for {enterHoldSeconds} s, exit > {exitAngleDeg} deg");
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            float angle = AngleToMapCenter();
            bool blocked = panels.AnyShown || trackingLost;

            if (!aimMode)
            {
                if (!blocked && angle < enterAngleDeg)
                {
                    if (enterSince < 0) enterSince = now;
                    if (now - enterSince >= enterHoldSeconds) EnterAim(angle);
                }
                else enterSince = -1f;
            }
            else if (blocked) ExitAim(trackingLost ? "tracking lost" : "panel shown");
            else if (angle > exitAngleDeg) ExitAim($"angle {angle:F1}");

            if (aimMode) UpdateAim();
            if (!aimMode && swallowUntil > 0 && now > swallowUntil) { swallowUntil = -1f; PopHandler(); }

            UpdateErrands();
            UpdateFloats(now);
            if (resultText != null && now >= resultUntil) resultText = null;
            RefreshHint();
        }

        private float AngleToMapCenter()
        {
            Vector3 center = mapRt.TransformPoint(mapRt.rect.center);
            return Vector3.Angle(head.forward, center - head.position);
        }

        private void EnterAim(float angle)
        {
            aimMode = true;
            enterSince = -1f;
            swallowUntil = -1f;
            map.FollowLocked = true; // the map never moves while the player aims
            InputAdapter.Instance.Push(this);
            pushed = true;
            Debug.Log($"{LogTag} enter map view (angle {angle:F1})");
        }

        private void ExitAim(string reason)
        {
            if (!aimMode) return;
            aimMode = false;
            map.FollowLocked = false;
            SetHover(default);
            reticle.enabled = false;
            bool pendingTap = touchValid && touchInAim && Time.unscaledTime - touchTime <= touchSnapshotMaxAge && reason != "pause";
            if (pendingTap) swallowUntil = touchTime + touchSnapshotMaxAge;
            else PopHandler();
            Debug.Log($"{LogTag} exit map view ({reason})");
        }

        private void PopHandler()
        {
            if (pushed && InputAdapter.Instance != null) InputAdapter.Instance.Pop(this);
            pushed = false;
        }

        /// <summary>Head ray -> map plane -> texel -> nearest chest / trash target that contains it.</summary>
        private void UpdateAim()
        {
            var plane = new Plane(mapRt.forward, mapRt.position);
            var ray = new Ray(head.position, head.forward);
            if (!plane.Raycast(ray, out float dist) || dist <= 0) { reticle.enabled = false; SetHover(default); return; }
            Vector2 texel = ((Vector2)mapRt.InverseTransformPoint(ray.GetPoint(dist)) - mapRt.rect.min) / TreasureMapView.TexelPx;
            reticle.enabled = true;
            reticle.rectTransform.anchoredPosition = texel * TreasureMapView.TexelPx;

            Target best = default;
            float bestD = float.MaxValue;
            var spots = session.Island.State.spots;
            for (int i = 0; i < spots.Count; i++)
            {
                if (!spots[i].hasChest) continue;
                var r = map.ChestRectTexels(i);
                var aim = new Rect(r.x - chestAimMargin, r.y - chestAimMargin, r.width + 2 * chestAimMargin, r.height + 2 * chestAimMargin);
                if (!aim.Contains(texel)) continue;
                float d = Vector2.Distance(texel, r.center);
                if (d < bestD) { bestD = d; best = new Target { kind = Kind.Chest, area = i, box = r }; }
            }
            var t = trash.Pick(texel);
            if (t != null)
            {
                float d = Vector2.Distance(texel, t.center);
                if (d < bestD)
                {
                    var size = t.image.rectTransform.sizeDelta / TreasureMapView.TexelPx;
                    best = new Target { kind = Kind.Trash, trash = t, box = new Rect(t.center - size / 2f, size) };
                }
            }
            SetHover(best);
        }

        private void SetHover(Target t)
        {
            if (t.Same(hover)) return;
            hover = t;
            if (t.kind == Kind.None) { frame.enabled = false; }
            else
            {
                // Frame 1 texel outside the art on every side.
                var box = new Rect(t.box.x - 2, t.box.y - 2, t.box.width + 4, t.box.height + 4);
                int w = Mathf.CeilToInt(box.width), h = Mathf.CeilToInt(box.height);
                var pc = new PixelCanvas(w, h);
                pc.Clear();
                pc.RectOutline(0, 0, w, h, Palette.Main);
                if (frame.texture != null) Destroy(frame.texture);
                frame.texture = pc.ToTexture();
                frame.rectTransform.sizeDelta = new Vector2(w, h) * TreasureMapView.TexelPx;
                frame.rectTransform.anchoredPosition = new Vector2(Mathf.Round(box.x), Mathf.Round(box.y)) * TreasureMapView.TexelPx;
                frame.enabled = true;
            }
            Debug.Log($"{LogTag} aim {t.Name}");
        }

        // Trigger: finger touches the temple (no SDK delay): remember the target under the reticle.
        private void HandleTouchStart(Vector2 pos)
        {
            touchValid = true;
            touchInAim = aimMode;
            touchTarget = aimMode ? hover : default;
            touchTime = Time.unscaledTime;
        }

        public bool HandleAction(GameAction action)
        {
            if (!aimMode)
            {
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
                case GameAction.Confirm: HandleTap(); return true;
                case GameAction.Back: touchValid = false; return false; // double tap keeps its global meaning
                default: touchValid = false; return true;               // swipes do nothing in map view
            }
        }

        private void HandleTap()
        {
            Target t;
            if (touchValid && Time.unscaledTime - touchTime <= touchSnapshotMaxAge)
            {
                if (!touchInAim) { touchValid = false; return; }
                t = touchTarget;
            }
            else t = hover;
            touchValid = false;

            switch (t.kind)
            {
                case Kind.Chest:
                    if (!session.Island.State.spots[t.area].hasChest) return;
                    Debug.Log($"{LogTag} tap chest {t.area}");
                    session.Island.OpenAt(t.area); // PanelHost opens the reward window, which ends map view
                    break;
                case Kind.Trash:
                    if (t.trash.queued || t.trash.image == null) return;
                    t.trash.queued = true;
                    trash.Flash(t.trash); // green = ordered
                    queue.Add(t.trash);
                    resultText = "Explorer on the way";
                    resultUntil = Time.unscaledTime + resultSeconds;
                    SetHover(default);
                    Debug.Log($"{LogTag} tap trash x{t.trash.trash}, queue {queue.Count}");
                    break;
                default:
                    Debug.Log($"{LogTag} tap on empty space: nothing");
                    break;
            }
        }

        /// <summary>
        /// Errand loop. Trigger: queue not empty and no errand running -> send the explorer to the oldest tapped trash.
        /// Trigger on arrival: remove the trash, pay IdleCore.RewardTrash, float "+N" over the explorer, next errand.
        /// Trash that vanished meanwhile (Settings refill / style switch) is skipped.
        /// </summary>
        private void UpdateErrands()
        {
            queue.RemoveAll(it => it.image == null);
            if (errand != null && errand.image == null) errand = null;
            if (errand == null && queue.Count > 0)
            {
                errand = queue[0];
                queue.RemoveAt(0);
                var target = errand;
                map.SendExplorer(target.bottom + new Vector2(-4f, 0f), () => ArriveAt(target)); // stand just left of it
            }
            session.Core.SearchPaused = errand != null || queue.Count > 0;
        }

        private void ArriveAt(TrashTestOverlay.Item it)
        {
            if (errand == it) errand = null;
            int units = trash.Remove(it);
            if (units <= 0) return;
            double coins = session.Core.RewardTrash(units);
            var text = UiFactory.CreateText(mapRt, "Coin float", 26, Palette.Accent, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(120, 32));
            text.text = $"+{coins:0}";
            floats.Add(new CoinFloat { text = text, start = Time.unscaledTime, texel = map.ExplorerTexel + new Vector2(0, 12f) });
            Debug.Log($"{LogTag} explorer cleared x{units}: +{coins:0} coins, queue {queue.Count}");
        }

        /// <summary>"+N" rises coinFloatRiseTexels over coinFloatSeconds above the explorer's head, then disappears.</summary>
        private void UpdateFloats(float now)
        {
            for (int i = floats.Count - 1; i >= 0; i--)
            {
                var f = floats[i];
                float k = (now - f.start) / coinFloatSeconds;
                if (k >= 1f) { Destroy(f.text.gameObject); floats.RemoveAt(i); continue; }
                f.text.rectTransform.anchoredPosition = new Vector2(f.texel.x, f.texel.y + Mathf.Round(k * coinFloatRiseTexels)) * TreasureMapView.TexelPx;
            }
        }

        private void RefreshHint()
        {
            string hint = "";
            Color color = Palette.Main;
            if (resultText != null) { hint = resultText; color = Palette.Accent; }
            else if (aimMode && hover.kind == Kind.Chest) hint = "Chest  -  tap to open";
            else if (aimMode && hover.kind == Kind.Trash) hint = hover.trash.trash > 1 ? "Trash pile  -  tap to clear" : "Trash  -  tap to clear";
            hintText.text = hint;
            hintText.color = color;
        }

        private void HandleTrackingChanged(bool tracking)
        {
            trackingLost = !tracking;
            if (trackingLost) ExitAim("tracking lost");
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
            if (session != null) session.Core.SearchPaused = false;
            PopHandler();
        }
    }
}
