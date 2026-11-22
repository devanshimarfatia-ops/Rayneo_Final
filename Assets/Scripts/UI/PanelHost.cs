using System.Collections.Generic;
using RayneoIdle.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Opens the toolbar panels (To-do, Collection, Settings) inside the HUD canvas.
    /// Triggers: MainHud.OnToolOpened(tool) opens the matching panel (one at a time).
    /// While a panel is open the toolbar is hidden and a bottom hint shows its controls.
    /// While the map is in view an open panel is suspended (hidden, input passes to the map) and comes back as it was.
    /// </summary>
    [DefaultExecutionOrder(50)] // after MainHud built its canvas
    public class PanelHost : MonoBehaviour
    {
        private MainHud hud;
        private TreasureMapView map;
        private Text hintText;
        private readonly Dictionary<MainHud.ToolId, PanelBase> panels = new Dictionary<MainHud.ToolId, PanelBase>();

        public PanelBase Current { get; private set; }

        /// <summary>When set (onboarding), the panel's own bottom hint is hidden.</summary>
        public string HintOverride { get; set; }

        public bool AnyOpen => Current != null && Current.IsOpen;

        /// <summary>A panel is open AND visible (not suspended while the user looks at the map).</summary>
        public bool AnyShown => AnyOpen && !Current.Suspended;

        /// <summary>Chest reward window (modal, opened by any chest).</summary>
        public RewardWindow Rewards { get; private set; }

        /// <summary>Launch modal (offline earnings + Start). Onboarding waits for StartScreen.OnStarted.</summary>
        public StartScreen StartScreen { get; private set; }

        private void Start()
        {
            hud = FindObjectOfType<MainHud>();
            var session = GameSession.Instance;
            if (hud == null || session == null || hud.PanelParent == null)
            {
                Debug.LogError("[PanelHost] needs MainHud and GameSession");
                enabled = false;
                return;
            }
            map = FindObjectOfType<TreasureMapView>();

            panels[MainHud.ToolId.Anchors] = new AnchorPanel();
            panels[MainHud.ToolId.Todo] = new TodoPanel();
            panels[MainHud.ToolId.Profile] = new ProfilePanel();
            panels[MainHud.ToolId.Collection] = new CollectionPanel();
            panels[MainHud.ToolId.Settings] = new SettingsPanel();
            foreach (var p in panels.Values) p.Init(hud.PanelParent, hud, session);
            // Launch flow: offline earnings window + Start screen, before the game HUD (device test 3).
            StartScreen = new StartScreen();
            StartScreen.Init(hud.PanelParent, hud, session);
            Current = StartScreen;
            StartScreen.ShowOnLaunch(session.LastLaunchSettle);

            Rewards = new RewardWindow();
            Rewards.Init(hud.PanelParent, hud, session);
            // Trigger: any chest opened (map tap, tests) -> reward window (replaces the old summary toast).
            session.Island.OnChestOpened += (_, result) =>
            {
                if (Current != null && Current.IsOpen && Current != Rewards) Current.Close();
                Current = Rewards;
                Rewards.Show(result);
            };

            hintText = UiFactory.CreateText(hud.PanelParent, "Panel hint", UiFactory.AuxFont, Palette.Secondary, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, UiFactory.MarginPx), new Vector2(560, 24));

            hud.OnToolOpened += Open;
            session.OnStateReplaced += () => { if (Current != null && Current.IsOpen) Current.Refresh(); };
        }

        public void Open(MainHud.ToolId tool)
        {
            if (!panels.TryGetValue(tool, out var panel)) { hud.Toast("Coming soon"); return; }
            if (Current != null && Current.IsOpen) Current.Close();
            Current = panel;
            panel.Open();
        }

        public T Get<T>() where T : PanelBase
        {
            foreach (var p in panels.Values) if (p is T t) return t;
            return null;
        }

        private void Update()
        {
            bool open = Current != null && Current.IsOpen;
            // Trigger: the map comes into view (map.InView, within 20 deg) while a tool panel is open -> the panel is
            // SUSPENDED (hidden, input passes to the map), not closed; looking back shows it again as it was.
            // Only Back (double tap) or the panel's own actions close it (Yaxin 2026-11-22: no reopening after a look
            // at the map). The reward window and Start screen are never suspended.
            if (open)
            {
                bool suspend = map != null && map.InView && Current != Rewards && Current != StartScreen;
                Current.SetSuspended(suspend);
            }
            bool shown = open && !Current.Suspended;
            if (shown) Current.Tick();
            hud.SetPanelMode(shown);
            // Onboarding shows its own hint card in this line while a panel is open.
            hintText.text = shown && string.IsNullOrEmpty(HintOverride) ? Current.Hint : "";
        }
    }
}
