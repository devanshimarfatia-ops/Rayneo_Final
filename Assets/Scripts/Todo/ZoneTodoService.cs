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

        /// <summary>Current anchor slot names (save data), or the defaults without a session.</summary>
        public IReadOnlyList<string> Names =>
            GameSession.Instance != null && GameSession.Instance.Data.zoneNames != null ? GameSession.Instance.Data.zoneNames : ZoneTag.DefaultNames;

        /// <summary>Name the player gave the zone's anchor slot ("To-do" area header, panel titles).</summary>
        public string ZoneDisplayName(TodoZone zone)
        {
            int slot = ZoneTag.ToSlot(zone);
            if (slot < 0) return "General";
            var n = Names;
            return slot < n.Count && !string.IsNullOrWhiteSpace(n[slot]) ? n[slot] : ZoneTag.DefaultNames[slot];
        }

        /// <summary>
        /// Renames an anchor slot and rewrites the "(Name)-" prefix of that slot's open todos with todoEdit,
        /// so they stay in the zone. Returns the cleaned name that was stored.
        /// </summary>
        public string RenameSlot(int slot, string newName)
        {
            var session = GameSession.Instance;
            newName = ZoneTag.Clean(newName);
            if (session == null || slot < 0 || slot > 2 || string.IsNullOrEmpty(newName)) return null;

            var oldNames = (string[])session.Data.zoneNames.Clone();
            session.Data.zoneNames[slot] = newName;
            session.Save();

            var zone = ZoneTag.FromSlot(slot);
            int retagged = 0;
            foreach (var t in SystemTodoClient.GetUnfinished())
            {
                // Strip the prefix with the OLD names, then add the new one. (Device test 4: formatting with the new
                // names could not see the old prefix, so renames stacked: "(Work)-(Study)-(Office)-text".)
                if (ZoneTag.Parse(t.text, oldNames, out string body) != zone) continue;
                string text = ZoneTag.Format(zone, ZoneTag.StripAllPrefixes(body), session.Data.zoneNames);
                if (text != t.text && SystemTodoClient.EditText(t, text)) retagged++;
            }
            Debug.Log($"{LogTag} slot {slot + 1} renamed to {newName}, {retagged} todos retagged");
            Refresh();
            OnZoneChanged?.Invoke(CurrentZone); // names changed: headers redraw
            return newName;
        }

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
                var zone = ZoneTag.Parse(t.text, Names, out string body);
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

        // ------------------------------------------------------------------ voice text capture (anchor rename)

        /// <summary>
        /// Free speech is not available on X3 Pro (Android SpeechRecognizer is a stub), so a spoken anchor name
        /// is captured through the system To-do voice flow: a temporary todo is created, its text is returned to
        /// <paramref name="onText"/> (null when nothing came back), and the todo is deleted again. Needs network.
        /// </summary>
        public bool StartVoiceCapture(Action<string> onText)
        {
            captureCallback = onText;
            if (StartVoiceCreate()) return true;
            captureCallback = null;
            return false;
        }

        private Action<string> captureCallback;

        private void CompleteVoiceCreate()
        {
            var snapshot = voiceSnapshot;
            voiceSnapshot = null;
            var now = SystemTodoClient.GetUnfinished();

            RemoteTodo created = null;
            foreach (var t in now)
                if (!snapshot.Contains(t.timestamp) && (created == null || t.timestamp > created.timestamp)) created = t;

            // Capture mode: hand the text over and delete the temporary todo (it is not a real task).
            if (captureCallback != null)
            {
                var cb = captureCallback;
                captureCallback = null;
                if (created != null && !SystemTodoClient.Delete(created.timestamp))
                    Debug.LogWarning($"{LogTag} could not delete the temporary todo {created.timestamp}");
                Debug.Log($"{LogTag} voice capture: {(created != null ? created.text : "(nothing)")}");
                Refresh();
                cb(created?.text);
                return;
            }

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
                string tagged = ZoneTag.Format(voiceZone, created.text, Names);
                if (SystemTodoClient.EditText(created, tagged)) created.text = tagged;
                else Debug.LogWarning($"{LogTag} todoEdit failed, todo stays General: {created.text}");
            }

            Refresh();
            var zone = ZoneTag.Parse(created.text, Names, out string body);
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
