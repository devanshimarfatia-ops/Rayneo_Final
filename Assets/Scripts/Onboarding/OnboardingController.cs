using System.Linq;
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using RayneoIdle.Todo;
using RayneoIdle.UI;
using RayneoIdle.Zones;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.Onboarding
{
    /// <summary>Onboarding steps (MVP 9.2 demo steps 1-10). Saved as int: never reorder, only append.</summary>
    public enum OnboardingStep
    {
        Welcome = 0,          // demo step 1: intro card
        Tap = 1,              // step 2: one hint card per gesture
        Swipe = 2,
        DoubleTap = 3,
        SetAnchor = 4,        // step 3: Anchors panel, slot 1: place + name
        Todo = 5,             // steps 4-5: open To-do, create by voice or finish an existing todo (D5)
        Toolbar = 6,          // step 6
        TurnToMap = 7,        // step 7
        TutorialSearch = 8,   // step 7: fixed delay, progress bar fills
        OpenChest = 9,        // step 8: reward window
        OpenCollection = 10,  // step 10: open the Collection from the tool bar (after the pack)
        OpenPack = 11,        // step 9: open the pack in the chest reward window (or in the Collection)
        BrowseCollection = 12,// step 10: look at the cards, close the Collection
        Done = 13,
    }

    /// <summary>
    /// W11 onboarding. Runs once per save (SaveData.onboardingDone), after the Start screen.
    /// Visuals (device test 3): an intro card, then each step shows a framed hint marked "TUTORIAL k/12"
    /// in the bottom hint zone, plus a blinking arrow over the toolbar icon to use; an end card closes it.
    /// Each step advances on the gesture / event it teaches (see Update / event handlers).
    /// Skip: double tap while no panel is open shows "Skip tutorial?"; tap confirms. A skipped onboarding
    /// leaves the tutorial chest on the map (not auto-granted, MVP 9.2 step 8).
    /// The step is saved on every change, so a kill mid-onboarding resumes at the same step.
    /// </summary>
    [DefaultExecutionOrder(60)] // after MainHud / PanelHost / TreasureMapView built their UI
    public class OnboardingController : MonoBehaviour, IGameInputHandler
    {
        private const string LogTag = "[Onboarding]";

        [Tooltip("Hint card y (canvas px from the bottom) while the toolbar is visible.")]
        [SerializeField] private float hintAboveToolbarY = 152f; // above the pointer arrow (112..132) and progress row
        [Tooltip("Seconds the end card stays if not tapped away.")]
        [SerializeField] private float endCardSeconds = 6f;

        /// <summary>Steps in the order the player meets them (for "TUTORIAL k/12").</summary>
        private static readonly OnboardingStep[] Flow =
        {
            OnboardingStep.Tap, OnboardingStep.Swipe, OnboardingStep.DoubleTap, OnboardingStep.SetAnchor,
            OnboardingStep.Todo, OnboardingStep.Toolbar, OnboardingStep.TurnToMap, OnboardingStep.TutorialSearch,
            OnboardingStep.OpenChest, OnboardingStep.OpenPack, OnboardingStep.OpenCollection, OnboardingStep.BrowseCollection,
        };

        private const string IntroText =
            "Welcome, explorer!\n\nFinish your real tasks,\nand your little explorer\nhunts for treasure on the island.";
        private const string EndText =
            "Tutorial complete!\n\nYour explorer is out treasure hunting.\nHave a great day!";

        public OnboardingStep Step { get; private set; } = OnboardingStep.Done;
        public bool Active => Step != OnboardingStep.Done;
        public string CurrentHint => skipPrompt ? hint.text : cardShown ? cardText.text : hint != null ? hint.text : "";
        public bool CardShown => cardShown;

        private bool RewardsOpen => panels != null && panels.AnyOpen && panels.Current is RewardWindow;

        private GameSession session;
        private MainHud hud;
        private PanelHost panels;
        private TreasureMapView map;
        private ZoneTodoService todos;
        private Text hint, tag, cardText, cardHint;
        private RawImage hintFrame, pointer, cardFrame;
        private bool pushed, skipPrompt, todoCreated, sawAnchorPanel, cardShown, gateOpen;
        private float searchStart, cardHideAt;
        private int searchArea = -1;

        private void Start()
        {
            session = GameSession.Instance;
            hud = FindObjectOfType<MainHud>();
            panels = FindObjectOfType<PanelHost>();
            map = FindObjectOfType<TreasureMapView>();
            todos = FindObjectOfType<ZoneTodoService>();
            if (session == null || hud == null || hud.PanelParent == null || InputAdapter.Instance == null)
            {
                Debug.LogError($"{LogTag} needs GameSession, MainHud and InputAdapter");
                enabled = false;
                return;
            }
            BuildUi(hud.PanelParent);

            // Event triggers
            InputAdapter.Instance.OnAnyAction += ObserveAction;
            session.Island.OnChestOpened += (_, r) => { if (r.isTutorial) { session.Data.tutorialChestOpened = true; if (Step == OnboardingStep.OpenChest) Go(OnboardingStep.OpenPack); } };
            // Trigger: any pack opened (reward window or Collection) while the tutorial asks for it.
            session.Cards.OnPackOpened += _ => { if (Step == OnboardingStep.OpenPack) Go(OnboardingStep.OpenCollection); };
            if (todos != null)
            {
                todos.OnTodoCreated += _ => { todoCreated = true; };
                todos.OnTodoFinished += (_, __) => { if (Step == OnboardingStep.Todo) Go(OnboardingStep.Toolbar); };
            }
            session.OnStateReplaced += () => { if (gateOpen) BeginFromSave(); }; // Reset / slot switch

            // Trigger: the game (and the tutorial) begins only after Start on the launch screen.
            var start = panels != null ? panels.StartScreen : null;
            if (start != null && !start.Started) start.OnStarted += OpenGate;
            else OpenGate();
        }

        private void OpenGate()
        {
            gateOpen = true;
            BeginFromSave();
        }

        private void BuildUi(RectTransform parent)
        {
            // Framed hint (bottom zone) + "TUTORIAL k/12" tag above its left edge.
            var f = new PixelCanvas(145, 10);
            f.Clear();
            f.RectOutline(0, 0, f.Width, f.Height, Palette.Accent);
            hintFrame = UiFactory.CreatePixelImage(parent, "Tutorial frame", f.ToTexture(), 4f,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, hintAboveToolbarY));
            hint = UiFactory.CreateText(hintFrame.rectTransform, "Tutorial hint", 20, Palette.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 34));
            tag = UiFactory.CreateText(hintFrame.rectTransform, "Tutorial tag", 16, Palette.Accent, TextAnchor.LowerLeft,
                new Vector2(0, 1), new Vector2(0, 0), new Vector2(4, 2), new Vector2(200, 20));

            // Blinking pointer over the toolbar icon to use.
            pointer = UiFactory.CreatePixelImage(parent, "Tutorial pointer", PixelCanvas.FromSprite(PixelSprites.ArrowDown), 4f,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero);
            pointer.enabled = false;

            // Center card: intro and end message.
            var c = new PixelCanvas(120, 60);
            c.Clear();
            c.RectOutline(0, 0, c.Width, c.Height, Palette.Accent);
            c.RectOutline(1, 1, c.Width - 2, c.Height - 2, Palette.Accent);
            cardFrame = UiFactory.CreatePixelImage(parent, "Tutorial card", c.ToTexture(), 4f,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0));
            cardText = UiFactory.CreateText(cardFrame.rectTransform, "Card text", UiFactory.BodyFont, Palette.Main, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(440, 170));
            cardHint = UiFactory.CreateText(cardFrame.rectTransform, "Card hint", UiFactory.AuxFont, Palette.Accent, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(440, 24));
            ShowCard(null);
            SetHintVisible(false);
        }

        private void ShowCard(string text)
        {
            cardShown = text != null;
            if (hud != null) hud.CardOpen = cardShown;
            cardFrame.gameObject.SetActive(cardShown);
            if (!cardShown) return;
            cardText.text = text;
            cardHint.text = "Tap to continue";
        }

        private void SetHintVisible(bool visible)
        {
            hintFrame.gameObject.SetActive(visible);
            if (!visible) pointer.enabled = false;
        }

        private void BeginFromSave()
        {
            var data = session.Data;
            // Legacy saves from before onboarding existed: the island search already ran -> treat as done.
            if (!data.onboardingDone && session.Core.State.chestSearchActive) data.onboardingDone = true;

            if (data.onboardingDone) { SetStep(OnboardingStep.Done, save: false); SetHintVisible(false); return; }
            var step = (OnboardingStep)Mathf.Clamp(data.onboardingStep, 0, (int)OnboardingStep.Done);
            Debug.Log($"{LogTag} start at {step}");
            Go(step);
        }

        // ================================================================ steps

        private void Go(OnboardingStep step)
        {
            SetStep(step, save: true);
            if (step == OnboardingStep.Done) { Finish(skipped: false); return; }

            // On-enter actions
            switch (step)
            {
                case OnboardingStep.Welcome:
                    ShowCard(IntroText);
                    break;
                case OnboardingStep.SetAnchor:
                    // Device test 3: tapping used to place the anchor silently and jump to To-do. Now the Anchors
                    // panel opens on slot 1 and the player places + names the anchor there.
                    sawAnchorPanel = false;
                    hud.FocusTool(MainHud.ToolId.Anchors); // if the panel gets closed early, a tap reopens it
                    OpenAnchorSlot();
                    break;
                case OnboardingStep.Todo:
                    todoCreated = false;
                    if (todos == null) { Go(OnboardingStep.Toolbar); return; }
                    hud.FocusTool(MainHud.ToolId.Todo); // the player opens it (no auto-open)
                    break;
                case OnboardingStep.TutorialSearch:
                    if (session.Island.State.spots.Any(s => s.isTutorial && s.hasChest) || session.Data.tutorialChestOpened)
                    { Go(session.Data.tutorialChestOpened ? OnboardingStep.OpenPack : OnboardingStep.OpenChest); return; }
                    searchArea = session.Island.PickFreeArea();
                    searchStart = Time.unscaledTime;
                    break;
                case OnboardingStep.OpenChest:
                    // Resume guard: chest already opened -> next step; no tutorial chest on the map -> put it back.
                    if (session.Data.tutorialChestOpened) { Go(OnboardingStep.OpenPack); return; }
                    if (!session.Island.State.spots.Any(s => s.isTutorial && s.hasChest)) session.Island.PlaceTutorialChest();
                    break;
                case OnboardingStep.OpenCollection:
                    hud.FocusTool(MainHud.ToolId.Collection); // a tap opens it
                    break;
                case OnboardingStep.OpenPack:
                    if (session.Cards.State.unopenedPacks <= 0) { Go(OnboardingStep.OpenCollection); return; }
                    break;
            }
        }

        private void OpenAnchorSlot()
        {
            if (panels == null) return;
            panels.Open(MainHud.ToolId.Anchors);
            panels.Get<AnchorPanel>()?.ShowSlot(0);
        }

        private void SetStep(OnboardingStep step, bool save)
        {
            Step = step;
            skipPrompt = false;
            if (save)
            {
                session.Data.onboardingStep = (int)step;
                session.Save();
            }
            bool needInput = step != OnboardingStep.Done || cardShown;
            if (needInput && !pushed) { InputAdapter.Instance.Push(this); pushed = true; }
            if (!needInput && pushed) { InputAdapter.Instance.Pop(this); pushed = false; }
            Debug.Log($"{LogTag} step {step}");
        }

        private void Finish(bool skipped)
        {
            var data = session.Data;
            data.onboardingDone = true;
            data.onboardingStep = (int)OnboardingStep.Done;
            if (map != null) { map.TutorialSearchArea = -1; map.TutorialSearch01 = -1; }
            if (skipped && !data.tutorialChestOpened) session.Island.PlaceTutorialChest(); // stays, not auto-granted
            session.Core.StartChestSearch();
            session.Island.OnSearchStarted();
            if (panels != null) panels.HintOverride = null;
            hud.ForceMapArrow = false;
            SetHintVisible(false);
            if (skipped) { ShowCard(null); hud.Toast("Tutorial skipped"); }
            else { ShowCard(EndText); cardHideAt = Time.unscaledTime + endCardSeconds; } // "start your day" card
            SetStep(OnboardingStep.Done, save: false); // keeps the input while the end card shows
            session.Save();
            Debug.Log($"{LogTag} finished (skipped={skipped})");
        }

        private void HideEndCard()
        {
            ShowCard(null);
            if (pushed && !Active) { InputAdapter.Instance.Pop(this); pushed = false; }
        }

        public void Skip()
        {
            if (!Active) return;
            if (panels != null && panels.AnyOpen) panels.Current.Close();
            ShowCard(null);
            Finish(skipped: true);
        }

        // ================================================================ input

        /// <summary>Sits above the HUD; panels and the map push above it while open / in view.</summary>
        public bool HandleAction(GameAction action)
        {
            if (!Active)
            {
                // Only the end card is left: tap closes it.
                if (cardShown) { if (action == GameAction.Confirm || action == GameAction.Back) HideEndCard(); return true; }
                return false;
            }

            if (skipPrompt)
            {
                if (action == GameAction.Confirm) Skip();
                else skipPrompt = false;
                return true;
            }

            switch (Step)
            {
                // Teaching steps: the gesture only advances the tutorial, nothing else happens.
                case OnboardingStep.Welcome:
                    if (action == GameAction.Confirm) { ShowCard(null); Go(OnboardingStep.Tap); }
                    else if (action == GameAction.Back) skipPrompt = true;
                    return true;
                case OnboardingStep.Tap:
                    if (action == GameAction.Confirm) Go(OnboardingStep.Swipe);
                    else if (action == GameAction.Back) skipPrompt = true;
                    return true;
                case OnboardingStep.Swipe:
                    if (action == GameAction.Prev || action == GameAction.Next) Go(OnboardingStep.DoubleTap);
                    else if (action == GameAction.Back) skipPrompt = true;
                    return true;
                case OnboardingStep.DoubleTap:
                    if (action == GameAction.Back) Go(OnboardingStep.SetAnchor);
                    return true;
            }

            // Other steps: the game handles input normally (panels sit above this); Back with nothing open offers the skip.
            if (action == GameAction.Back) { skipPrompt = true; return true; }
            return false;
        }

        // Observes actions that the HUD also handles (toolbar step).
        private void ObserveAction(GameAction action)
        {
            if (Step == OnboardingStep.Toolbar && (action == GameAction.Prev || action == GameAction.Next)
                && (panels == null || !panels.AnyOpen))
                Go(OnboardingStep.TurnToMap);
        }

        // ================================================================ per frame

        private void Update()
        {
            if (session == null) return;
            if (!Active)
            {
                if (cardShown && Time.unscaledTime >= cardHideAt) HideEndCard();
                return;
            }
            bool mapInView = map != null && map.InView;
            bool panelOpen = panels != null && panels.AnyOpen;
            var store = FindAnchorStore();

            switch (Step)
            {
                case OnboardingStep.SetAnchor:
                    if (panelOpen && panels.Current is AnchorPanel) sawAnchorPanel = true;
                    // Trigger: the Anchors panel was used and closed with anchor 1 placed (or no anchor system: manual).
                    if (sawAnchorPanel && !panelOpen && (store == null || store.Get(ZoneType.Work) != null))
                    {
                        string name = todos != null ? todos.ZoneDisplayName(TodoZone.Work) : "Work";
                        hud.Toast(store != null ? $"Anchor 1 ready: {name}" : $"Anchor 1: {name} (manual)");
                        todos?.SetZone(TodoZone.Work); // the detector confirms it after its dwell time
                        Go(OnboardingStep.Todo);
                    }
                    break;
                case OnboardingStep.TurnToMap:
                    if (mapInView || map == null) Go(OnboardingStep.TutorialSearch);
                    break;
                case OnboardingStep.TutorialSearch:
                    // Trigger: tutorialChestSearchSeconds of real time after reaching the map (fixed onboarding delay).
                    float p = Mathf.Clamp01((Time.unscaledTime - searchStart) / (float)session.Config.tutorialChestSearchSeconds);
                    if (map != null) { map.TutorialSearchArea = searchArea; map.TutorialSearch01 = p; }
                    if (p >= 1f)
                    {
                        if (map != null) { map.TutorialSearchArea = -1; map.TutorialSearch01 = -1; }
                        session.Island.PlaceTutorialChest(searchArea);
                        Go(OnboardingStep.OpenChest);
                    }
                    break;
                case OnboardingStep.OpenCollection:
                    if (panelOpen && panels.Current is CollectionPanel) Go(OnboardingStep.BrowseCollection);
                    break;
                case OnboardingStep.BrowseCollection:
                    if (!panelOpen) Go(OnboardingStep.Done);
                    break;
            }
            if (!Active) return;

            // Trigger: a "look right" step while the map is out of view -> HUD arrow blinks (no text, Yaxin).
            hud.ForceMapArrow = !mapInView && (Step == OnboardingStep.TurnToMap || Step == OnboardingStep.TutorialSearch || Step == OnboardingStep.OpenChest);

            // Framed hint + tag + position (bottom hint zone). The intro card replaces it on Welcome.
            string text = skipPrompt ? "Skip tutorial?  Tap: skip   Swipe: stay" : cardShown ? "" : HintFor(Step, panelOpen, mapInView, store);
            hint.text = text;
            SetHintVisible(text.Length > 0);
            int k = System.Array.IndexOf(Flow, Step) + 1;
            tag.text = k > 0 ? $"TUTORIAL {k}/{Flow.Length}" : "TUTORIAL";
            // Map in view: under the HUD progress row (above the map). Panel open: the panel hint line.
            float y = panelOpen ? UiFactory.MarginPx : mapInView ? MainHud.ProgressMapViewY - 40f : hintAboveToolbarY;
            hintFrame.rectTransform.anchoredPosition = new Vector2(0, y);
            if (panels != null) panels.HintOverride = panelOpen && text.Length > 0 ? text : null;

            UpdatePointer(panelOpen, mapInView, store);
        }

        private static ZoneAnchorStore cachedStore;
        private static ZoneAnchorStore FindAnchorStore()
        {
            if (cachedStore == null) cachedStore = FindObjectOfType<ZoneAnchorStore>();
            return cachedStore;
        }

        /// <summary>Trigger: the step needs a toolbar tool and no panel / map is in the way -> blink an arrow over it.</summary>
        private void UpdatePointer(bool panelOpen, bool mapInView, ZoneAnchorStore store)
        {
            MainHud.ToolId? tool = null;
            if (!panelOpen && !mapInView && !skipPrompt)
            {
                if (Step == OnboardingStep.SetAnchor) tool = MainHud.ToolId.Anchors;
                else if (Step == OnboardingStep.Todo) tool = MainHud.ToolId.Todo;
                else if (Step == OnboardingStep.OpenCollection) tool = MainHud.ToolId.Collection;
            }
            bool blinkOn = Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.6f;
            pointer.enabled = tool.HasValue && blinkOn;
            if (tool.HasValue) pointer.rectTransform.anchoredPosition = new Vector2(hud.ToolSlotCenterX(tool.Value), hud.ToolPointerY);
        }

        private string HintFor(OnboardingStep step, bool panelOpen, bool mapInView, ZoneAnchorStore store)
        {
            switch (step)
            {
                case OnboardingStep.Welcome: return "";
                case OnboardingStep.Tap: return "Tap the touchpad once";
                case OnboardingStep.Swipe: return "Swipe forward or back on the touchpad";
                case OnboardingStep.DoubleTap: return "Double tap = back. Try it now";
                case OnboardingStep.SetAnchor:
                {
                    var anchors = panelOpen ? panels.Current as AnchorPanel : null;
                    bool placed = store == null || store.Get(ZoneType.Work) != null;
                    if (anchors == null) return placed ? "" : "Tap the diamond icon: Anchors";
                    if (!placed) return anchors.InSlotView ? "Stand at your desk, tap: Place anchor here" : "Tap anchor 1";
                    return "Swipe on Name to rename it. Double tap twice when done";
                }
                case OnboardingStep.Todo:
                    if (!panelOpen) return hud.FocusedTool == MainHud.ToolId.Todo ? "Tap to open To-do" : "Swipe to the pen icon, then tap";
                    return todoCreated ? "Tap your new task to finish it" : "Tap + to speak, or tap a task to finish";
                case OnboardingStep.Toolbar: return panelOpen ? "Double tap to close" : "These are your tools. Swipe to look";
                case OnboardingStep.TurnToMap: return ""; // the HUD arrow says it (Yaxin: arrow, not text)
                case OnboardingStep.TutorialSearch: return mapInView ? "Your explorer is hunting for treasure..." : "";
                case OnboardingStep.OpenChest: return mapInView ? "Chest found! Tap to open it" : "";
                case OnboardingStep.OpenCollection:
                    if (RewardsOpen) return "Tap to continue";
                    if (mapInView) return "Now look ahead again";
                    if (panelOpen) return "Double tap, then open Collection";
                    return hud.FocusedTool == MainHud.ToolId.Collection ? "Tap to open Collection" : "Swipe to the chest icon, then tap";
                case OnboardingStep.OpenPack:
                    if (RewardsOpen) return "Tap the pack to open it";
                    return panelOpen ? "Swipe down to Open pack, then tap" : "Your pack waits in Collection";
                case OnboardingStep.BrowseCollection: return "Your cards!  Double tap to go back";
                default: return "";
            }
        }
    }
}
