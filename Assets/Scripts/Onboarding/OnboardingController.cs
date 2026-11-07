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
    /// <summary>Onboarding steps (MVP 9.2 demo steps 1-10). Saved as int: only append new values.</summary>
    public enum OnboardingStep
    {
        Welcome = 0,          // demo step 1
        Tap = 1,              // step 2: one hint card per gesture
        Swipe = 2,
        DoubleTap = 3,
        SetAnchor = 4,        // step 3
        Todo = 5,             // steps 4-5: create by voice or finish an existing todo (D5: never requires voice)
        Toolbar = 6,          // step 6
        TurnToMap = 7,        // step 7
        TutorialSearch = 8,   // step 7: fixed delay, progress bar fills
        OpenChest = 9,        // step 8
        OpenCollection = 10,  // step 9-10
        OpenPack = 11,        // step 9
        BrowseCollection = 12,// step 10
        Done = 13,
    }

    /// <summary>
    /// W11 onboarding. Runs once per save (SaveData.onboardingDone). One hint card at a time in the bottom
    /// hint zone; each step advances on the gesture / event it teaches (triggers listed per step below).
    /// Skip: double tap while no panel is open shows "Skip tutorial?"; tap confirms. A skipped onboarding
    /// leaves the tutorial chest on the map (not auto-granted, MVP 9.2 step 8).
    /// The step is saved on every change, so a kill mid-onboarding resumes at the same step.
    /// </summary>
    [DefaultExecutionOrder(60)] // after MainHud / PanelHost / TreasureMapView built their UI
    public class OnboardingController : MonoBehaviour, IGameInputHandler
    {
        private const string LogTag = "[Onboarding]";

        [Tooltip("Hint card y (canvas px from the bottom) while the toolbar is visible: just above the tool label.")]
        [SerializeField] private float hintAboveToolbarY = 118f;

        public OnboardingStep Step { get; private set; } = OnboardingStep.Done;
        public bool Active => Step != OnboardingStep.Done;
        public string CurrentHint => hint != null ? hint.text : "";

        private GameSession session;
        private MainHud hud;
        private PanelHost panels;
        private TreasureMapView map;
        private ZoneTodoService todos;
        private Transform head;
        private Text hint;
        private bool pushed, skipPrompt, todoCreated;
        private float searchStart;
        private int searchArea = -1;

        private void Start()
        {
            session = GameSession.Instance;
            hud = FindObjectOfType<MainHud>();
            panels = FindObjectOfType<PanelHost>();
            map = FindObjectOfType<TreasureMapView>();
            todos = FindObjectOfType<ZoneTodoService>();
            head = UiFactory.FindHead();
            if (session == null || hud == null || hud.PanelParent == null || InputAdapter.Instance == null)
            {
                Debug.LogError($"{LogTag} needs GameSession, MainHud and InputAdapter");
                enabled = false;
                return;
            }

            hint = UiFactory.CreateText(hud.PanelParent, "Onboarding hint", 20, Palette.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, hintAboveToolbarY), new Vector2(580, 30));

            // Event triggers
            InputAdapter.Instance.OnAnyAction += ObserveAction;
            session.Island.OnChestOpened += (_, r) => { if (r.isTutorial) { session.Data.tutorialChestOpened = true; if (Step == OnboardingStep.OpenChest) Go(OnboardingStep.OpenCollection); } };
            session.Cards.OnPackOpened += _ => { if (Step == OnboardingStep.OpenPack) Go(OnboardingStep.BrowseCollection); };
            if (todos != null)
            {
                todos.OnTodoCreated += _ => { todoCreated = true; };
                todos.OnTodoFinished += (_, __) => { if (Step == OnboardingStep.Todo) Go(OnboardingStep.Toolbar); };
            }
            session.OnStateReplaced += BeginFromSave; // Reset / slot switch

            BeginFromSave();
        }

        private void BeginFromSave()
        {
            var data = session.Data;
            // Legacy saves from before onboarding existed: the island search already ran -> treat as done.
            if (!data.onboardingDone && session.Core.State.chestSearchActive) data.onboardingDone = true;

            if (data.onboardingDone) { SetStep(OnboardingStep.Done, save: false); return; }
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
                case OnboardingStep.Todo:
                    todoCreated = false;
                    if (todos == null) { Go(OnboardingStep.Toolbar); return; }
                    panels?.Open(MainHud.ToolId.Todo);
                    break;
                case OnboardingStep.TutorialSearch:
                    if (session.Island.State.spots.Any(s => s.isTutorial && s.hasChest) || session.Data.tutorialChestOpened)
                    { Go(session.Data.tutorialChestOpened ? OnboardingStep.OpenCollection : OnboardingStep.OpenChest); return; }
                    searchArea = session.Island.PickFreeArea();
                    searchStart = Time.unscaledTime;
                    break;
                case OnboardingStep.OpenCollection:
                    hud.FocusTool(MainHud.ToolId.Collection); // a tap opens it
                    break;
                case OnboardingStep.OpenPack:
                    if (session.Cards.State.unopenedPacks <= 0) { Go(OnboardingStep.BrowseCollection); return; }
                    break;
            }
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
            bool needInput = step != OnboardingStep.Done;
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
            SetStep(OnboardingStep.Done, save: false);
            session.Save();
            if (panels != null) panels.HintOverride = null;
            hint.text = "";
            hud.Toast(skipped ? "Tutorial skipped" : "Tutorial done!");
            Debug.Log($"{LogTag} finished (skipped={skipped})");
        }

        public void Skip()
        {
            if (!Active) return;
            if (panels != null && panels.AnyOpen) panels.Current.Close();
            Finish(skipped: true);
        }

        private void SetAnchor()
        {
            // Trigger: tap during SetAnchor. Anchor = where the user stands (no hit test on X3 Pro).
            var store = FindObjectOfType<ZoneAnchorStore>();
            if (store != null && head != null)
            {
                if (!store.Anchors.Any(a => a.zoneType == ZoneType.Work)) store.Add(ZoneType.Work, head.position);
                hud.Toast("Work zone set");
            }
            else hud.Toast("Work zone set (manual)"); // R1 fallback: no anchor system in the scene
            todos?.SetZone(TodoZone.Work); // the detector confirms it after its dwell time
            Go(OnboardingStep.Todo);
        }

        // ================================================================ input

        /// <summary>Sits above the HUD; panels and the map push above it while open / in view.</summary>
        public bool HandleAction(GameAction action)
        {
            if (!Active) return false;

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
                    if (action == GameAction.Confirm) Go(OnboardingStep.Tap);
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
                case OnboardingStep.SetAnchor:
                    if (action == GameAction.Confirm) SetAnchor();
                    else if (action == GameAction.Back) skipPrompt = true;
                    return true;
            }

            // Other steps: the game handles input normally; Back with nothing open offers the skip.
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
            if (session == null || !Active) return;
            bool mapInView = map != null && map.InView;
            bool panelOpen = panels != null && panels.AnyOpen;

            switch (Step)
            {
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
                    if (panelOpen && panels.Current is CollectionPanel) Go(OnboardingStep.OpenPack);
                    break;
                case OnboardingStep.BrowseCollection:
                    if (!panelOpen) Go(OnboardingStep.Done);
                    break;
            }
            if (!Active) return;

            // Hint card text + position (bottom hint zone).
            hint.text = skipPrompt ? "Skip tutorial?  Tap: skip   Swipe: stay" : HintFor(Step, panelOpen, mapInView);
            float y = panelOpen || mapInView ? UiFactory.MarginPx : hintAboveToolbarY;
            hint.rectTransform.anchoredPosition = new Vector2(0, y);
            if (panels != null) panels.HintOverride = panelOpen ? hint.text : null;
        }

        private string HintFor(OnboardingStep step, bool panelOpen, bool mapInView)
        {
            switch (step)
            {
                case OnboardingStep.Welcome: return "Welcome, explorer!  Tap to start";
                case OnboardingStep.Tap: return "Tap the touchpad once";
                case OnboardingStep.Swipe: return "Swipe forward or back on the touchpad";
                case OnboardingStep.DoubleTap: return "Double tap = back. Try it now";
                case OnboardingStep.SetAnchor: return "Stand at your desk. Tap: set Work zone";
                case OnboardingStep.Todo:
                    if (!panelOpen) return "Open To-do from the tool bar";
                    return todoCreated ? "Tap your new task to finish it" : "Tap + to speak, or tap a task to finish";
                case OnboardingStep.Toolbar: return panelOpen ? "Double tap to close" : "Your tools are below. Swipe to browse";
                case OnboardingStep.TurnToMap: return "Turn your head right: your map";
                case OnboardingStep.TutorialSearch: return mapInView ? "Your explorer is searching..." : "Turn right to watch the search";
                case OnboardingStep.OpenChest: return mapInView ? "Chest found! Tap to open it" : "Turn right: a chest is waiting";
                case OnboardingStep.OpenCollection:
                    if (mapInView) return "Now look ahead again";
                    return panelOpen ? "Double tap, then open Collection" : "Tap to open Collection";
                case OnboardingStep.OpenPack: return "Swipe down to Open pack, then tap";
                case OnboardingStep.BrowseCollection: return "Your cards! Swipe to look, double tap to close";
                default: return "";
            }
        }
    }
}
