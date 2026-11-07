using System;
using System.Collections.Generic;
using RayneoIdle.Game;
using RayneoIdle.Zones;
using UnityEngine;

namespace RayneoIdle.Todo
{
    /// <summary>A system todo as the game shows it.</summary>
    public struct ZoneTodo
    {
        public long timestamp;
        public TodoZone zone;
        /// <summary>Text without the "(Zone)-" prefix.</summary>
        public string displayText;
        public RemoteTodo raw;
    }

    /// <summary>
    /// W7 zone -> To-do glue.
    /// - Current zone comes ONLY from ModeManager.OnModeChanged (never from the detector), so detection can be swapped.
    /// - Todos live in the system To-do app (SystemTodoClient). The zone is a text prefix (D3, see ZoneTag).
    /// - Visible list = todos of the current zone, plus General todos (if showGeneralInsideZones).
    ///   Outside any zone (AppMode.Default) only General todos show.
    /// - New todo: StartVoiceCreate() opens the system voice screen; our app pauses. On resume the list is
    ///   re-read and any new timestamp is renamed to "(Zone)-text" with todoEdit (unless the zone is General).
    /// - Finish: todoSetFinished + IdleCore.RewardTodo(), paid once per todo timestamp (saved list).
    /// </summary>
    public class ZoneTodoService : MonoBehaviour
    {
        private const string LogTag = "[ZoneTodo]";

        [Tooltip("Optional. If empty, found in the scene. Without a ModeManager the zone is always General.")]
        [SerializeField] private ModeManager modeManager;

        [Tooltip("Show General (untagged) todos while inside a zone too. Off = a zone shows only its own todos.")]
        [SerializeField] private bool showGeneralInsideZones = true;

        [Tooltip("Write the (Zone)- prefix with todoEdit after a voice create. Off if todoEdit fails on device.")]
        [SerializeField] private bool tagNewTodos = true;

        public TodoZone CurrentZone { get; private set; } = TodoZone.General;

        /// <summary>Todos visible in the current zone, oldest first.</summary>
        public IReadOnlyList<ZoneTodo> Visible => visible;

        public bool WaitingForVoice => voiceSnapshot != null;

        /// <summary>(new zone)</summary>
        public event Action<TodoZone> OnZoneChanged;
        /// <summary>Visible list was rebuilt (zone change, refresh, create, finish).</summary>
        public event Action OnListChanged;
        /// <summary>A todo created through the voice flow came back (already tagged).</summary>
        public event Action<ZoneTodo> OnTodoCreated;
        /// <summary>(todo, coins paid; 0 if it had been paid before)</summary>
        public event Action<ZoneTodo, double> OnTodoFinished;
        /// <summary>The voice flow returned without a new todo (cancelled, no network, not understood).</summary>
        public event Action OnVoiceCreateFailed;

        private readonly List<ZoneTodo> visible = new List<ZoneTodo>();
        private List<RemoteTodo> unfinished = new List<RemoteTodo>();
        private HashSet<long> voiceSnapshot; // timestamps before startTodo; null = not waiting
        private TodoZone voiceZone;

        private bool modeBound;

        private void OnEnable()
        {
            BindModeManager();
            Refresh();
        }

        // AnchorZoneBootstrap creates the ModeManager in its own Awake; Awake order between objects is not
        // defined, so try again in Start when it was not there yet in OnEnable.
        private void Start() => BindModeManager();

        private void BindModeManager()
        {
            if (modeBound) return;
            if (modeManager == null) modeManager = FindObjectOfType<ModeManager>();
            if (modeManager == null) return;
            modeManager.OnModeChanged += HandleModeChanged;
            modeBound = true;
            if (modeManager.CurrentMode != AppMode.Default) SetZone(MapMode(modeManager.CurrentMode));
        }

        private void OnDisable()
        {
            if (modeManager != null && modeBound) modeManager.OnModeChanged -= HandleModeChanged;
            modeBound = false;
        }

        // Trigger: ModeManager confirmed a zone enter / exit.
        private void HandleModeChanged(AppMode previous, AppMode current) => SetZone(MapMode(current));

        public static TodoZone MapMode(AppMode mode)
        {
            switch (mode)
            {
                case AppMode.Work: return TodoZone.Work;
                case AppMode.Bedroom: return TodoZone.Bedroom;
                case AppMode.Kitchen: return TodoZone.Kitchen;
                default: return TodoZone.General;
            }
        }

        /// <summary>Also usable as the manual fallback (R1: pick the zone from a list if anchors do not persist).</summary>
        public void SetZone(TodoZone zone)
        {
            if (zone == CurrentZone) return;
            CurrentZone = zone;
            Debug.Log($"{LogTag} zone -> {zone}");
            RebuildVisible();
            OnZoneChanged?.Invoke(zone);
        }

        // ------------------------------------------------------------------ list

        /// <summary>Re-reads the system To-do list (blocking binder call, a few ms).</summary>
        public void Refresh()
        {
            unfinished = SystemTodoClient.GetUnfinished();
            RebuildVisible();
        }

        private void RebuildVisible()
        {
            visible.Clear();
            foreach (var t in unfinished)
            {
                var zone = ZoneTag.Parse(t.text, out string body);
                bool show = zone == CurrentZone || (zone == TodoZone.General && (CurrentZone == TodoZone.General || showGeneralInsideZones));
                if (show) visible.Add(new ZoneTodo { timestamp = t.timestamp, zone = zone, displayText = body, raw = t });
            }
            visible.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            OnListChanged?.Invoke();
        }

        // ------------------------------------------------------------------ create (voice)

        /// <summary>Opens the system voice-create screen. The new todo is tagged with the zone at this moment.</summary>
        public bool StartVoiceCreate()
        {
            var before = SystemTodoClient.GetUnfinished();
            voiceSnapshot = new HashSet<long>();
            foreach (var t in before) voiceSnapshot.Add(t.timestamp);
            voiceZone = CurrentZone;

            if (SystemTodoClient.StartVoiceCreate())
            {
                Debug.Log($"{LogTag} voice create started (zone {voiceZone})");
#if UNITY_EDITOR
                CompleteVoiceCreate(); // Editor fake adds the item at once; there is no pause
#endif
                return true;
            }
            voiceSnapshot = null;
            OnVoiceCreateFailed?.Invoke();
            return false;
        }

        // Trigger: app resumes after the system voice screen (pause false / focus true).
        private void OnApplicationPause(bool paused)
        {
            if (!paused && voiceSnapshot != null) CompleteVoiceCreate();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus && voiceSnapshot != null) CompleteVoiceCreate();
        }

        private void CompleteVoiceCreate()
        {
            var snapshot = voiceSnapshot;
            voiceSnapshot = null;
            var now = SystemTodoClient.GetUnfinished();

            RemoteTodo created = null;
            foreach (var t in now)
                if (!snapshot.Contains(t.timestamp) && (created == null || t.timestamp > created.timestamp)) created = t;

            if (created == null)
            {
                Debug.Log($"{LogTag} voice create returned with no new todo");
                unfinished = now;
                RebuildVisible();
                OnVoiceCreateFailed?.Invoke();
                return;
            }

            if (tagNewTodos && voiceZone != TodoZone.General)
            {
                string tagged = ZoneTag.Format(voiceZone, created.text);
                if (SystemTodoClient.EditText(created, tagged)) created.text = tagged;
                else Debug.LogWarning($"{LogTag} todoEdit failed, todo stays General: {created.text}");
            }

            Refresh();
            var zone = ZoneTag.Parse(created.text, out string body);
            var item = new ZoneTodo { timestamp = created.timestamp, zone = zone, displayText = body, raw = created };
            Debug.Log($"{LogTag} created [{zone}] {body}");
            OnTodoCreated?.Invoke(item);
        }

        // ------------------------------------------------------------------ finish

        /// <summary>Marks the todo finished in the system app and pays the To-do reward once.</summary>
        public double Finish(ZoneTodo todo)
        {
            if (!SystemTodoClient.SetFinished(todo.timestamp))
            {
                Debug.LogWarning($"{LogTag} todoSetFinished failed for {todo.timestamp}");
                return -1;
            }

            double paid = 0;
            var session = GameSession.Instance;
            if (session != null)
            {
                var rewarded = session.Data.rewardedTodoTimestamps;
                if (!rewarded.Contains(todo.timestamp))
                {
                    rewarded.Add(todo.timestamp);
                    paid = session.Core.RewardTodo();
                    session.Save();
                }
            }

            Debug.Log($"{LogTag} finished [{todo.zone}] {todo.displayText}, +{paid}");
            Refresh();
            OnTodoFinished?.Invoke(todo, paid);
            return paid;
        }
    }
}
