using System.Collections.Generic;
using System.Linq;
using System.Text;
using RayneoIdle.Cards;
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using RayneoIdle.Island;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// W3 visuals: the island as a flat line-art treasure map, world-locked to the RIGHT of the start view
    /// (the user turns the head to see it). Reads GameSession.Island / Core; never changes rules itself.
    ///
    /// Triggers:
    /// - Map "in view": angle between head forward and map direction (both flattened) below <see cref="inViewAngleDeg"/>.
    ///   While in view the map takes input (pushed on InputAdapter): Prev / Next = focus chest, Confirm = open it.
    /// - Search outline ("?" + dashed area) shows the area IslandMap is searching; it blinks every <see cref="blinkSeconds"/>.
    /// - A chest placed by the island drops in over <see cref="dropSeconds"/>.
    /// - The explorer walks to random points inside the searched area (speed x demo animation speed).
    /// </summary>
    public class TreasureMapView : MonoBehaviour, IGameInputHandler
    {
        private const string LogTag = "[TreasureMap]";

        [Header("Placement (world-locked)")]
        [Tooltip("Degrees to the right of the start view where the map is placed.")]
        [SerializeField] private float mapYawDeg = 55f;
        [Tooltip("Map distance. 2.6 m makes the 600x470 px map cover ~75% of the view, leaving the safety margin " +
                 "and room for HUD toasts above it (2 m would fill the whole view).")]
        [SerializeField] private float distanceM = 2.6f;
        [Tooltip("Height offset from the head at start, meters (negative = lower).")]
        [SerializeField] private float heightOffsetM = 0f;
        [Tooltip("Map counts as 'in view' (gets input) below this angle between gaze and map, degrees.")]
        [SerializeField] private float inViewAngleDeg = 20f;

        [Header("Animation")]
        [SerializeField] private float blinkSeconds = 0.8f;
        [SerializeField] private float dropSeconds = 0.35f;
        [Tooltip("Explorer speed in map texels per second at 1x.")]
        [SerializeField] private float walkTexelsPerSecond = 8f;

        // ---- Map geometry (texels). 1 texel = PxPerTexel canvas px, so 1-texel lines are 4 px (spec: >= 2 px). ----
        private const float ScaleReferenceM = 2f;
        private const int MapW = 140, MapH = 100;
        private const float PxPerTexel = 4f;
        private static readonly Vector2 IslandCenter = new Vector2(70, 50);
        private static readonly Vector2 IslandRadius = new Vector2(46, 32);
        /// <summary>Normalized chest-area coordinates (IslandMap.Areas) map into this texel box (inside the coast).</summary>
        private static readonly Rect AreaBox = new Rect(30, 24, 80, 52);

        public bool InView { get; private set; }

        private Transform head;
        private GameSession session;
        private RectTransform canvasRt, mapRt;
        private readonly List<RawImage> chestImages = new List<RawImage>();
        private readonly List<RawImage> areaOutlines = new List<RawImage>();
        private readonly List<float> dropStart = new List<float>();
        private RawImage explorer, focusBrackets, barFill;
        private Text statusText;
        private Texture2D chestTex, tutorialTex, explorerA, explorerB;
        private int focusArea = -1;

        private Vector2 explorerPos = IslandCenter, explorerTarget = IslandCenter;
        private float explorerPauseUntil, walkPhase;
        private bool handlerPushed;

        private const float BarWidthPx = 240f, BarHeightPx = 16f;

        private void Start()
        {
            session = GameSession.Instance;
            head = UiFactory.FindHead();
            if (session == null || head == null)
            {
                Debug.LogError($"{LogTag} needs a GameSession and a head camera");
                enabled = false;
                return;
            }
            Build();
            PlaceInWorld();

            var island = session.Island;
            island.OnChestPlaced += HandleChestPlaced;
            island.OnChestOpened += (_, __) => RefreshChests();
            session.OnStateReplaced += RefreshChests;
            RefreshChests();
        }

        private void OnDisable()
        {
            if (handlerPushed && InputAdapter.Instance != null) InputAdapter.Instance.Pop(this);
            handlerPushed = false;
            InView = false;
        }

        // ================================================================ build

        private void Build()
        {
            var cam = head.GetComponent<Camera>();
            // Scale is fixed at the HUD reference distance (1 canvas px = 1 HUD px at 2 m); placing the map farther
            // away (distanceM) then makes it look smaller: apparent size = 2 m / distanceM.
            canvasRt = UiFactory.CreateCanvas("Treasure Map Canvas", null, new Vector2(600, 470), ScaleReferenceM, cam);

            var mapTex = DrawStaticMap();
            var mapImg = UiFactory.CreatePixelImage(canvasRt, "Map", mapTex, PxPerTexel,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8));
            mapRt = mapImg.rectTransform;

            chestTex = PixelCanvas.FromSprite(PixelSprites.Chest);
            tutorialTex = PixelCanvas.FromSprite(PixelSprites.TutorialChest);
            explorerA = PixelCanvas.FromSprite(PixelSprites.ExplorerA);
            explorerB = PixelCanvas.FromSprite(PixelSprites.ExplorerB);

            var areas = session.Island.Areas;
            for (int i = 0; i < areas.Count; i++)
            {
                var r = AreaTexelRect(areas[i]);
                var outline = UiFactory.CreatePixelImage(mapRt, $"Area {i} outline", DrawAreaOutline((int)r.width, (int)r.height),
                    PxPerTexel, Vector2.zero, Vector2.zero, r.position * PxPerTexel);
                outline.enabled = false;
                areaOutlines.Add(outline);

                var chest = UiFactory.CreatePixelImage(mapRt, $"Chest {i}", chestTex, PxPerTexel,
                    Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero);
                chest.enabled = false;
                chestImages.Add(chest);
                dropStart.Add(-99f);
            }

            focusBrackets = UiFactory.CreatePixelImage(mapRt, "Focus", PixelCanvas.FromSprite(PixelSprites.FocusBrackets), PxPerTexel,
                Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero);
            focusBrackets.enabled = false;

            explorer = UiFactory.CreatePixelImage(mapRt, "Explorer", explorerA, PxPerTexel,
                Vector2.zero, new Vector2(0.5f, 0f), IslandCenter * PxPerTexel);

            // Bottom row: status text (left) + progress bar (right)
            statusText = UiFactory.CreateText(canvasRt, "Status", UiFactory.AuxFont, Palette.Sand, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28, 14), new Vector2(300, 32));
            var frame = new PixelCanvas((int)(BarWidthPx / PxPerTexel), (int)(BarHeightPx / PxPerTexel));
            frame.RectOutline(0, 0, frame.Width, frame.Height, Palette.Ink);
            UiFactory.CreatePixelImage(canvasRt, "Bar frame", frame.ToTexture(), PxPerTexel,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28, 22));
            var fillPc = new PixelCanvas(1, 1);
            fillPc.Set(0, 0, Palette.Sand);
            barFill = UiFactory.CreatePixelImage(canvasRt, "Bar fill", fillPc.ToTexture(), 1f,
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(-28 - BarWidthPx + PxPerTexel, 22 + PxPerTexel));
            barFill.rectTransform.sizeDelta = new Vector2(0, BarHeightPx - 2 * PxPerTexel);
        }

        private void PlaceInWorld()
        {
            Vector3 fwd = head.forward;
            fwd.y = 0;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            var dir = Quaternion.Euler(0, mapYawDeg, 0) * fwd.normalized;
            canvasRt.position = head.position + dir * distanceM + Vector3.up * heightOffsetM;
            canvasRt.rotation = Quaternion.LookRotation(dir, Vector3.up); // camera looks along +Z of the canvas
        }

        /// <summary>Move the map back to the right of the current view (e.g. after SLAM drift or recenter).</summary>
        public void Recenter() => PlaceInWorld();

        /// <summary>Direction from the head to the map, flattened (for the HUD "look right" hint).</summary>
        public float SignedAngleFromGaze()
        {
            if (head == null || canvasRt == null) return 0;
            Vector3 f = head.forward; f.y = 0;
            Vector3 d = canvasRt.position - head.position; d.y = 0;
            return Vector3.SignedAngle(f, d, Vector3.up);
        }

        private static Rect AreaTexelRect(MapArea a) => new Rect(
            Mathf.Round(AreaBox.x + a.x * AreaBox.width), Mathf.Round(AreaBox.y + a.y * AreaBox.height),
            Mathf.Round(a.width * AreaBox.width), Mathf.Round(a.height * AreaBox.height));

        private static Vector2 SpotTexel(ChestSpot s) => new Vector2(AreaBox.x + s.x * AreaBox.width, AreaBox.y + s.y * AreaBox.height);

        // ================================================================ static drawing

        private static float CoastRadius(float theta) =>
            1f + 0.10f * Mathf.Sin(3 * theta + 0.7f) + 0.06f * Mathf.Sin(5 * theta + 2.1f) + 0.04f * Mathf.Sin(8 * theta + 0.3f);

        private static Texture2D DrawStaticMap()
        {
            var pc = new PixelCanvas(MapW, MapH);
            pc.Clear();

            // Scroll rods (top / bottom) with end knobs, paper edges with small notches.
            foreach (int y in new[] { 1, 94 })
            {
                pc.RectOutline(3, y, 134, 5, Palette.Sand);
                pc.RectOutline(0, y - 1, 3, 7, Palette.Ink);
                pc.RectOutline(137, y - 1, 3, 7, Palette.Ink);
            }
            for (int y = 7; y <= 92; y++)
            {
                bool notch = y % 9 == 0;
                pc.Set(notch ? 9 : 8, y, Palette.Ink);
                pc.Set(notch ? 130 : 131, y, Palette.Ink);
            }

            // Coastline: perturbed ellipse, closed polyline.
            const int samples = 96;
            Vector2Int prev = default;
            for (int i = 0; i <= samples; i++)
            {
                float t = i * Mathf.PI * 2f / samples;
                float r = CoastRadius(t);
                var p = new Vector2Int(Mathf.RoundToInt(IslandCenter.x + IslandRadius.x * r * Mathf.Cos(t)),
                                       Mathf.RoundToInt(IslandCenter.y + IslandRadius.y * r * Mathf.Sin(t)));
                if (i > 0) pc.Line(prev.x, prev.y, p.x, p.y, Palette.Sand);
                prev = p;
            }

            // Sparse dot texture on land (staggered grid), kept away from the coast.
            for (int y = 10; y < 90; y += 5)
            for (int x = 12 + (y / 5 % 2) * 3; x < 128; x += 6)
            {
                float nx = (x - IslandCenter.x) / IslandRadius.x, ny = (y - IslandCenter.y) / IslandRadius.y;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                if (d < CoastRadius(Mathf.Atan2(ny, nx)) * 0.88f) pc.Set(x, y, Palette.Ink);
            }

            // Sea waves in the corners.
            var wave = new[] { ".I.I.", "I.I.." };
            foreach (var w in new[] { new Vector2Int(13, 13), new Vector2Int(116, 12), new Vector2Int(14, 84), new Vector2Int(98, 86) })
                pc.Sprite(wave, w.x, w.y);

            // Compass (top-right): cross + N.
            pc.HLine(117, 125, 76, Palette.Ink);
            pc.VLine(121, 72, 80, Palette.Ink);
            pc.Sprite(new[] { "S..S", "SS.S", "S.SS", "S..S" }, 120, 82);

            return pc.ToTexture();
        }

        private static Texture2D DrawAreaOutline(int w, int h)
        {
            var pc = new PixelCanvas(w, h);
            pc.Clear();
            pc.DashedRect(0, 0, w, h, 3, 2, Palette.Ink);
            pc.Sprite(PixelSprites.Question, w / 2 - 2, h / 2 - 3);
            return pc.ToTexture();
        }

        // ================================================================ runtime

        private void HandleChestPlaced(int area)
        {
            if (area >= 0 && area < dropStart.Count) dropStart[area] = Time.unscaledTime;
            RefreshChests();
        }

        private void RefreshChests()
        {
            var spots = session.Island.State.spots;
            for (int i = 0; i < chestImages.Count && i < spots.Count; i++)
            {
                var img = chestImages[i];
                img.enabled = spots[i].hasChest;
                if (!spots[i].hasChest) continue;
                img.texture = spots[i].isTutorial ? tutorialTex : chestTex;
                img.rectTransform.sizeDelta = new Vector2(img.texture.width, img.texture.height) * PxPerTexel;
            }
            if (focusArea >= 0 && (focusArea >= spots.Count || !spots[focusArea].hasChest)) focusArea = NextChest(focusArea, +1);
        }

        private void Update()
        {
            if (session == null) return;
            UpdateInView();
            UpdateChestsAndOutline();
            UpdateExplorer();
            UpdateStatus();
        }

        private void UpdateInView()
        {
            bool inView = Mathf.Abs(SignedAngleFromGaze()) < inViewAngleDeg;
            if (inView == InView) return;
            InView = inView;
            var input = InputAdapter.Instance;
            if (input != null)
            {
                if (inView) { input.Push(this); handlerPushed = true; }
                else { input.Pop(this); handlerPushed = false; }
            }
            if (inView && focusArea < 0) focusArea = NextChest(-1, +1);
        }

        private void UpdateChestsAndOutline()
        {
            var st = session.Island.State;
            for (int i = 0; i < chestImages.Count && i < st.spots.Count; i++)
            {
                if (!st.spots[i].hasChest) continue;
                float t = Mathf.Clamp01((Time.unscaledTime - dropStart[i]) / dropSeconds);
                float dropTexels = Mathf.Round((1 - t) * (1 - t) * 12f); // eases in, snapped to texels
                var p = SpotTexel(st.spots[i]);
                chestImages[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y) + dropTexels) * PxPerTexel;
            }

            bool blinkOn = Mathf.Repeat(Time.unscaledTime, blinkSeconds * 2) < blinkSeconds;
            for (int i = 0; i < areaOutlines.Count; i++) areaOutlines[i].enabled = i == st.searchArea && blinkOn;

            bool showFocus = InView && focusArea >= 0 && focusArea < st.spots.Count && st.spots[focusArea].hasChest;
            focusBrackets.enabled = showFocus;
            if (showFocus)
            {
                var p = SpotTexel(st.spots[focusArea]);
                focusBrackets.rectTransform.anchoredPosition = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y) - 2) * PxPerTexel;
            }
        }

        private void UpdateExplorer()
        {
            var st = session.Island.State;
            float speed = walkTexelsPerSecond * (float)session.Core.AnimationSpeed;
            bool searching = st.searchArea >= 0;

            if (Time.unscaledTime >= explorerPauseUntil && Vector2.Distance(explorerPos, explorerTarget) < 0.5f)
            {
                if (searching)
                {
                    var r = AreaTexelRect(session.Island.Areas[st.searchArea]);
                    explorerTarget = new Vector2(Random.Range(r.xMin + 3, r.xMax - 7), Random.Range(r.yMin + 2, r.yMax - 10));
                    explorerPauseUntil = Time.unscaledTime + 0.6f / (float)session.Core.AnimationSpeed;
                }
                else explorerTarget = IslandCenter + new Vector2(-3, -8);
            }

            bool walking = Vector2.Distance(explorerPos, explorerTarget) >= 0.5f && Time.unscaledTime >= explorerPauseUntil;
            if (walking)
            {
                explorerPos = Vector2.MoveTowards(explorerPos, explorerTarget, speed * Time.unscaledDeltaTime);
                walkPhase += Time.unscaledDeltaTime * 4f * (float)session.Core.AnimationSpeed;
            }
            explorer.texture = walking && Mathf.Repeat(walkPhase, 2f) >= 1f ? explorerB : explorerA;
            explorer.rectTransform.anchoredPosition = new Vector2(Mathf.Round(explorerPos.x), Mathf.Round(explorerPos.y)) * PxPerTexel;
        }

        private void UpdateStatus()
        {
            var core = session.Core;
            float progress = (float)core.ChestProgress01;
            string text;
            if (!core.State.chestSearchActive) text = "Map ready";
            else if (core.ChestSlotsFull) text = "Chests full - open one";
            else text = $"Searching  {Mathf.FloorToInt(progress * 100)}%";
            if (InView && focusArea >= 0) text = "Tap to open";
            statusText.text = text;

            float inner = BarWidthPx - 2 * PxPerTexel;
            float w = core.ChestSlotsFull ? inner : Mathf.Round(progress * inner / PxPerTexel) * PxPerTexel;
            barFill.rectTransform.sizeDelta = new Vector2(w, BarHeightPx - 2 * PxPerTexel);
        }

        // ================================================================ input

        private int NextChest(int from, int step)
        {
            var spots = session.Island.State.spots;
            for (int k = 1; k <= spots.Count; k++)
            {
                int i = ((from + step * k) % spots.Count + spots.Count) % spots.Count;
                if (spots[i].hasChest) return i;
            }
            return -1;
        }

        /// <summary>Only while the map is in view (pushed on the InputAdapter stack).</summary>
        public bool HandleAction(GameAction action)
        {
            switch (action)
            {
                case GameAction.Prev:
                case GameAction.Next:
                    int n = NextChest(focusArea, action == GameAction.Next ? +1 : -1);
                    if (n < 0) return false;
                    focusArea = n;
                    return true;
                case GameAction.Confirm:
                    return OpenFocused();
                default:
                    return false; // Back etc. fall through to the HUD
            }
        }

        /// <summary>Opens the focused chest. Also used by tests.</summary>
        public bool OpenFocused()
        {
            if (focusArea < 0) focusArea = NextChest(-1, +1);
            if (focusArea < 0) return false;
            var result = session.Island.OpenAt(focusArea);
            if (result == null) return false;
            // The reward summary is shown by the HUD toast (MainHud listens to IslandMap.OnChestOpened).
            focusArea = NextChest(focusArea, +1);
            return true;
        }

        /// <summary>Short reward summary for a toast: "+200 coins" / "NEW Special 1  Card pack". Max 2 lines.</summary>
        public static string Summary(ChestOpenResult r)
        {
            double coins = r.slots.Where(s => s.kind == ChestSlotKind.Coins).Sum(s => s.coins)
                           + r.slots.SelectMany(s => s.cards).Sum(c => c.duplicateCoins);
            var items = new List<string>();
            foreach (var s in r.slots)
            {
                if (s.kind == ChestSlotKind.Card && s.cards[0].isNew) items.Add($"NEW {s.cards[0].card.displayName}");
                if (s.kind == ChestSlotKind.Pack) items.Add("Card pack");
            }
            var sb = new StringBuilder();
            if (coins > 0) sb.Append($"+{coins:0} coins");
            if (items.Count > 0) sb.Append(sb.Length > 0 ? "\n" : "").Append(string.Join("  ", items));
            return sb.ToString();
        }

        public int FocusArea => focusArea;
    }
}
