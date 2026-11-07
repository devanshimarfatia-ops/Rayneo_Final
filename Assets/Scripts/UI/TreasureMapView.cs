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
    /// W3 visuals: the island as a flat line-art treasure map that FOLLOWS THE BODY: it stays mapYawDeg to the
    /// right of the body direction, outside the head-locked HUD, so it is only seen by turning the head.
    /// Reads GameSession.Island / Core; never changes rules itself. Status + progress are shown by the HUD
    /// (bottom center), read from StatusText / Progress01 here.
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

        [Header("Placement (body-follow)")]
        [Tooltip("Degrees to the right of the body direction where the map sits.")]
        [SerializeField] private float mapYawDeg = 55f;
        [Tooltip("Map distance. 2.9 m makes the 600x416 px map cover ~69% of the view: room for the HUD progress " +
                 "bar + hints below it and toasts above it (2 m would fill the whole view).")]
        [SerializeField] private float distanceM = 2.9f;
        [Tooltip("Height offset from the eyes, meters (negative = lower). -0.42 m at 2.9 m = about 8 deg below eye level (device test 2: 3 deg was still too high); follows the CURRENT " +
                 "eye height (first build used the start height, too high after sitting down).")]
        [SerializeField] private float heightOffsetM = -0.42f;
        [Tooltip("Body-follow: the body direction is kept while the head yaw stays within this many degrees to the " +
                 "LEFT of it (looking right at the map never moves it).")]
        [SerializeField] private float followLeftDeg = 35f;
        [Tooltip("Body-follow: ... and this many degrees PAST the map on the right.")]
        [SerializeField] private float followPastMapDeg = 25f;
        [Tooltip("Body-follow trigger: the head must stay outside that range this long (s) before the map follows.")]
        [SerializeField] private float followDelaySeconds = 1.5f;
        [Tooltip("Body-follow speed once triggered, degrees per second.")]
        [SerializeField] private float followSpeedDegPerSec = 90f;
        [Tooltip("Position smoothing toward the walking head (1/s). Higher = tighter.")]
        [SerializeField] private float positionSmoothing = 6f;
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
        private static readonly Rect AreaBox = new Rect(34, 25, 72, 42); // shrunk so the highest chest (10 texels) stays inside the coast

        public bool InView { get; private set; }

        /// <summary>Onboarding tutorial search: area shown as searched (-1 = none). Set by OnboardingController.</summary>
        public int TutorialSearchArea { get; set; } = -1;
        /// <summary>Onboarding tutorial search progress 0..1 for the status bar (-1 = off, use the island search).</summary>
        public float TutorialSearch01 { get; set; } = -1f;

        private int ShownSearchArea => session.Island.State.searchArea >= 0 ? session.Island.State.searchArea : TutorialSearchArea;

        private Transform head;
        private GameSession session;
        private RectTransform canvasRt, mapRt;
        private readonly List<RawImage> chestImages = new List<RawImage>();
        private readonly List<RawImage> areaOutlines = new List<RawImage>();
        private readonly List<float> dropStart = new List<float>();
        private RawImage explorer, pet, focusBrackets, mapBarFill;
        private Vector2 petPos = IslandCenter;
        private bool petFacingLeft;
        private Text mapStatus;
        private GameObject[] mapProgressRow;
        private Texture2D chestTex, tutorialTex, explorerA, explorerB;
        private int focusArea = -1;

        private Vector2 explorerPos = IslandCenter, explorerTarget = IslandCenter;
        private float explorerPauseUntil, walkPhase;
        private bool handlerPushed;

        // Body-follow state: the body direction the map hangs off, and since when the head is outside the keep range.
        private float bodyYaw;
        private float outsideSince = -1f;
        private bool following;

        /// <summary>Search status for the HUD progress row ("Searching 42%", "Chests full", "Tap to open").</summary>
        public string StatusText { get; private set; } = "";
        /// <summary>Search progress 0..1 for the HUD bar (1 while all chest slots are full).</summary>
        public float Progress01 { get; private set; }
        /// <summary>False before the search starts (onboarding not done and no tutorial search): HUD hides the row.</summary>
        public bool ShowProgress { get; private set; }

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
            Recenter();

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
            canvasRt = UiFactory.CreateCanvas("Treasure Map Canvas", null, new Vector2(600, 464), ScaleReferenceM, cam);

            var mapTex = DrawStaticMap();
            var mapImg = UiFactory.CreatePixelImage(canvasRt, "Map", mapTex, PxPerTexel,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -56)); // top 48 px: the map's own progress row
            mapRt = mapImg.rectTransform;

            // Progress row ON the map (device test 4): world-locked with it, so it is there whenever the map is.
            // Font 26 at 2 / 2.9 scale = ~18 px on the glasses (spec min 16). The HUD row hides while the map is in view.
            mapStatus = UiFactory.CreateText(canvasRt, "Map status", 26, Palette.Main, TextAnchor.MiddleRight,
                new Vector2(0.5f, 1f), new Vector2(1f, 0.5f), new Vector2(-12, -26), new Vector2(280, 36));
            var barPc = new PixelCanvas(60, 7);
            barPc.Clear();
            barPc.RectOutline(0, 0, barPc.Width, barPc.Height, Palette.Secondary);
            UiFactory.CreatePixelImage(canvasRt, "Map bar frame", barPc.ToTexture(), 4f,
                new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(12, -26));
            var fillPc = new PixelCanvas(1, 1);
            fillPc.Set(0, 0, Palette.Main);
            mapBarFill = UiFactory.CreatePixelImage(canvasRt, "Map bar fill", fillPc.ToTexture(), 1f,
                new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(12 + 8, -26));
            mapBarFill.rectTransform.sizeDelta = new Vector2(0, 7 * 4 - 16);
            mapProgressRow = new[] { mapStatus.gameObject, canvasRt.Find("Map bar frame").gameObject, mapBarFill.gameObject };

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

            pet = UiFactory.CreatePixelImage(mapRt, "Pet", null, PxPerTexel, Vector2.zero, new Vector2(0.5f, 0f), IslandCenter * PxPerTexel);
            pet.rectTransform.sizeDelta = new Vector2(6, 5) * PxPerTexel;
            pet.enabled = false;
            explorer = UiFactory.CreatePixelImage(mapRt, "Explorer", explorerA, PxPerTexel,
                Vector2.zero, new Vector2(0.5f, 0f), IslandCenter * PxPerTexel);
        }

        private float HeadYaw()
        {
            Vector3 f = head.forward;
            f.y = 0;
            return f.sqrMagnitude < 1e-6f ? bodyYaw : Quaternion.LookRotation(f).eulerAngles.y;
        }

        /// <summary>
        /// Body-follow. The map hangs mapYawDeg to the right of bodyYaw and follows the walking head position.
        /// Trigger: the head yaw stays outside [bodyYaw - followLeftDeg, bodyYaw + mapYawDeg + followPastMapDeg]
        /// for followDelaySeconds -> the user faces a new direction -> bodyYaw turns toward the head at
        /// followSpeedDegPerSec until the head is "forward" again. Looking right at the map never moves it.
        /// </summary>
        private void UpdatePlacement(bool snap)
        {
            float headYaw = HeadYaw();
            float delta = Mathf.DeltaAngle(bodyYaw, headYaw);
            bool outside = delta < -followLeftDeg || delta > mapYawDeg + followPastMapDeg;

            if (outside) { if (outsideSince < 0) outsideSince = Time.unscaledTime; }
            else if (!following) outsideSince = -1f;

            if (!following && outsideSince >= 0 && Time.unscaledTime - outsideSince >= followDelaySeconds) following = true;
            if (following)
            {
                bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, headYaw, followSpeedDegPerSec * Time.unscaledDeltaTime);
                if (Mathf.Abs(Mathf.DeltaAngle(bodyYaw, headYaw)) < 2f) { following = false; outsideSince = -1f; }
            }

            var dir = Quaternion.Euler(0, bodyYaw + mapYawDeg, 0) * Vector3.forward;
            var target = head.position + dir * distanceM + Vector3.up * heightOffsetM;
            canvasRt.position = snap ? target
                : Vector3.Lerp(canvasRt.position, target, 1f - Mathf.Exp(-positionSmoothing * Time.unscaledDeltaTime));
            canvasRt.rotation = Quaternion.LookRotation(dir, Vector3.up); // camera looks along +Z of the canvas
        }

        /// <summary>Hide the map visuals (reward window open: a transparent window over the map lines is unreadable).</summary>
        public void SetHidden(bool hidden)
        {
            if (canvasRt != null && canvasRt.gameObject.activeSelf == hidden) canvasRt.gameObject.SetActive(!hidden);
        }

        /// <summary>Put the map right of the current view at once (start, Settings > Recenter map).</summary>
        public void Recenter()
        {
            bodyYaw = HeadYaw();
            following = false;
            outsideSince = -1f;
            UpdatePlacement(snap: true);
        }

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
                pc.RectOutline(3, y, 134, 5, Palette.Main);
                pc.RectOutline(0, y - 1, 3, 7, Palette.Secondary);
                pc.RectOutline(137, y - 1, 3, 7, Palette.Secondary);
            }
            for (int y = 7; y <= 92; y++)
            {
                bool notch = y % 9 == 0;
                pc.Set(notch ? 9 : 8, y, Palette.Secondary);
                pc.Set(notch ? 130 : 131, y, Palette.Secondary);
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
                if (i > 0) pc.Line(prev.x, prev.y, p.x, p.y, Palette.Main);
                prev = p;
            }

            // Sparse dot texture on land (staggered grid), kept away from the coast.
            for (int y = 10; y < 90; y += 5)
            for (int x = 12 + (y / 5 % 2) * 3; x < 128; x += 6)
            {
                float nx = (x - IslandCenter.x) / IslandRadius.x, ny = (y - IslandCenter.y) / IslandRadius.y;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                if (d < CoastRadius(Mathf.Atan2(ny, nx)) * 0.88f) pc.Set(x, y, Palette.Secondary);
            }

            // Sea waves in the corners.
            var wave = new[] { ".I.I.", "I.I.." };
            foreach (var w in new[] { new Vector2Int(13, 13), new Vector2Int(116, 12), new Vector2Int(14, 84), new Vector2Int(98, 86) })
                pc.Sprite(wave, w.x, w.y);

            // Compass (top-right): cross + N.
            pc.HLine(117, 125, 76, Palette.Secondary);
            pc.VLine(121, 72, 80, Palette.Secondary);
            pc.Sprite(new[] { "S..S", "SS.S", "S.SS", "S..S" }, 120, 82);

            return pc.ToTexture();
        }

        private static Texture2D DrawAreaOutline(int w, int h)
        {
            var pc = new PixelCanvas(w, h);
            pc.Clear();
            pc.DashedRect(0, 0, w, h, 3, 2, Palette.Secondary);
            pc.Sprite(PixelSprites.Question, w / 2 - 2, h / 2 - 3);
            return pc.ToTexture();
        }

        // ================================================================ runtime

        private void HandleChestPlaced(int area)
        {
            if (area >= 0 && area < dropStart.Count) dropStart[area] = Time.unscaledTime;
            RefreshChests();
            // Trigger: a chest drops while the map is in view and nothing is focused -> focus it (shows "Tap to open").
            if (InView && focusArea < 0) focusArea = area;
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
            UpdatePlacement(snap: false);
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
            int shownArea = ShownSearchArea;
            for (int i = 0; i < areaOutlines.Count; i++) areaOutlines[i].enabled = i == shownArea && blinkOn;

            bool showFocus = InView && focusArea >= 0 && focusArea < st.spots.Count && st.spots[focusArea].hasChest;
            focusBrackets.enabled = showFocus;
            if (showFocus)
            {
                // Follow the chest image (also while it is still dropping in), 2 texels below its base.
                focusBrackets.rectTransform.anchoredPosition =
                    chestImages[focusArea].rectTransform.anchoredPosition - new Vector2(0, 2 * PxPerTexel);
            }
        }

        private void UpdateExplorer()
        {
            var st = session.Island.State;
            float speed = walkTexelsPerSecond * (float)session.Core.AnimationSpeed;
            int area = ShownSearchArea;
            bool searching = area >= 0;

            if (Time.unscaledTime >= explorerPauseUntil && Vector2.Distance(explorerPos, explorerTarget) < 0.5f)
            {
                if (searching)
                {
                    var r = AreaTexelRect(session.Island.Areas[area]);
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
            // Look from the Profile (outfit + color); frame 1 = legs together while walking.
            var look = session.Profile.State;
            explorer.texture = CharacterArt.Character(look, walking && Mathf.Repeat(walkPhase, 2f) >= 1f ? 1 : 0);
            explorer.rectTransform.anchoredPosition = new Vector2(Mathf.Round(explorerPos.x), Mathf.Round(explorerPos.y)) * PxPerTexel;

            // Pet (GDD: follows the character): trails a few texels behind, a bit slower, faces its direction.
            var petTex = CharacterArt.Pet(look.pet, petFacingLeft);
            pet.enabled = petTex != null;
            if (petTex != null)
            {
                var target = explorerPos + new Vector2(explorerPos.x >= petPos.x ? -6f : 6f, 0);
                var step = Vector2.MoveTowards(petPos, target, speed * 0.9f * Time.unscaledDeltaTime) - petPos;
                if (Mathf.Abs(step.x) > 0.01f) petFacingLeft = step.x < 0;
                petPos += step;
                pet.texture = petTex;
                pet.rectTransform.anchoredPosition = new Vector2(Mathf.Round(petPos.x), Mathf.Round(petPos.y)) * PxPerTexel;
            }
        }

        private void UpdateStatus()
        {
            var core = session.Core;
            bool tutorial = TutorialSearch01 >= 0;
            float progress = tutorial ? TutorialSearch01 : (float)core.ChestProgress01;
            string text;
            if (tutorial) text = $"Treasure hunt  {Mathf.FloorToInt(progress * 100)}%";
            else if (!core.State.chestSearchActive) text = "Map ready";
            else if (core.ChestSlotsFull) text = "Chests full - open one";
            else text = $"Treasure hunt  {Mathf.FloorToInt(progress * 100)}%";
            if (InView && focusArea >= 0) text = "Tap to open";

            StatusText = text;
            Progress01 = core.ChestSlotsFull && !tutorial ? 1f : Mathf.Clamp01(progress);
            ShowProgress = tutorial || core.State.chestSearchActive || (InView && focusArea >= 0);

            foreach (var go in mapProgressRow) if (go.activeSelf != ShowProgress) go.SetActive(ShowProgress);
            mapStatus.text = StatusText;
            float inner = 60 * 4 - 16;
            mapBarFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(Progress01 * inner / 4f) * 4f, 7 * 4 - 16);
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
                // Card names in their rarity color (GDD: silver-gray / gold / purple).
                if (s.kind == ChestSlotKind.Card && s.cards[0].isNew)
                    items.Add($"<color={CardArt.RarityHex(s.cards[0].card.rarity)}>NEW {s.cards[0].card.displayName}</color>");
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
