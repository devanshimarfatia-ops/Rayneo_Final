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
    /// The panel closes by itself when the map comes into view (the user turned the head away).
    /// </summary>
    [DefaultExecutionOrder(50)] // after MainHud built its canvas
    public class PanelHost : MonoBehaviour
    {
        private MainHud hud;
        private TreasureMapView map;
        private Text hintText;
        private readonly Dictionary<MainHud.ToolId, PanelBase> panels = new Dictionary<MainHud.ToolId, PanelBase>();

        public PanelBase Current { get; private set; }

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

            panels[MainHud.ToolId.Todo] = new TodoPanel();
            panels[MainHud.ToolId.Collection] = new CollectionPanel();
            panels[MainHud.ToolId.Settings] = new SettingsPanel();
            foreach (var p in panels.Values) p.Init(hud.PanelParent, hud, session);

            hintText = UiFactory.CreateText(hud.PanelParent, "Panel hint", UiFactory.AuxFont, Palette.Ink, TextAnchor.LowerCenter,
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
            if (open && map != null && map.InView) { Current.Close(); open = false; }
            if (open) Current.Tick();
            hud.SetPanelMode(open);
            hintText.text = open ? Current.Hint : "";
        }
    }
}
