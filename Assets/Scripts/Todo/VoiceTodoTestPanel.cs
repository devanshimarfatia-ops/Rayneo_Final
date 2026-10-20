using System;
using System.Collections.Generic;
using FfalconXR.InputModule; // XRGraphicRaycaster (ARDK UnityXRSDKCore.dll)
using RayNeo;                // SimpleTouchForLite
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RayneoIdle.Todo
{
    /// <summary>
    /// Device validation panel for voice todos through the RayNeo system To-do app.
    ///
    ///   Single tap       -> add a todo by voice: opens the system To-do voice screen (Editor: Space)
    ///   Swipe up / down  -> move the selection up / down the system To-do list      (Editor: Up / Down arrow)
    ///   Triple tap       -> mark the selected system item finished                  (Editor: Backspace)
    ///   Swipe fwd / back -> ask to DELETE the selected item; tap = delete, any swipe = keep
    ///                       (Editor: Left / Right arrow, then Space). Auto-cancels after deleteConfirmSeconds.
    ///   Double tap       -> not used (system back / exit)
    ///
    /// List order: oldest first, so a higher number = added later; a new todo appears at the end.
    ///
    /// Voice flow: tap -> SystemTodoClient.StartVoiceCreate() -> the system screen covers this app
    /// (focus lost) -> user speaks, the system saves the todo and returns here (focus back) -> the list
    /// is re-read and any item whose timestamp was not there before the tap is "new": an "Added: ..."
    /// toast confirms it and the selection jumps to it.
    /// Platform SpeechRecognizer is NOT used: on the X3 Pro the default RecognitionService is a stub.
    ///
    /// Island Gallery: when ContinueEnabled is set (by FeatureLauncher), a "START ISLAND" row sits under
    /// the list. Swipe down onto it and tap -> OnContinue fires and the launcher opens the island game.
    /// </summary>
    public class VoiceTodoTestPanel : MonoBehaviour
    {
        [SerializeField] private float toastSeconds = 2.5f;
        [Tooltip("Swipes closer together than this are ignored, so one swipe = one step.")]
        [SerializeField] private float swipeCooldownSeconds = 0.35f;
        [Tooltip("Flip if swiping up on the temple moves the selection down instead of up.")]
        [SerializeField] private bool invertSwipe = false;
        [Tooltip("OPEN TIMEOUT (s): after a tap, if this app has not lost focus to the system voice screen " +
                 "within this time, the screen is considered not opened and the tap is reset.")]
        [SerializeField] private float systemOpenTimeoutSeconds = 3f;
        [Tooltip("DELETE CONFIRM (s): after a sideways swipe the selected todo waits this long for a tap, then is kept.")]
        [SerializeField] private float deleteConfirmSeconds = 4f;

        // ---- Layout (RayNeo design specs, same canvas math as ZoneDebugHUD) ----
        private const float CanvasWidthPx = 640f;
        private const float CanvasHeightPx = 480f;
        private const float DisplayFovDeg = 30f;
        private const float HudDistanceM = 2f;
        private const float MarginPx = 24f;
        private const int ToastFontSize = 22;
        private const int ListFontSize = 20;
        private const int HintFontSize = 18;
        private const int ToastMaxChars = 36;     // "Added: <text>" toast, longer texts are cut with "…"
        // System list = the core content, centered on the canvas. Rows visible at once:
        private const int MaxSystemShown = 5;
        // System list rows: the selected row is drawn as a "button" (2 px cyan frame, spec: lines >= 2 px).
        private const float ListTopPx = 105f;     // header top, relative to the canvas center
        private const float ListHeaderPx = 26f;
        private const float RowHeightPx = 32f;
        private const float RowGapPx = 4f;
        private const float RowWidthPx = 520f;
        private const float RowPaddingPx = 12f;
        private const float FrameLinePx = 2f;
        private const int RowMaxChars = 34;       // longer texts are cut with "…"

        private static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        private static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);
        private static readonly Color Red = new Color32(0xFF, 0x66, 0x66, 0xFF);
        private const string CyanHex = "#4DE2FF";
        private const string PurpleHex = "#8566FF";

        private enum Flow { Idle, OpeningSystemScreen, InSystemScreen }

        private Flow flow = Flow.Idle;
        private float openDeadline;
        private readonly HashSet<long> knownBeforeTap = new HashSet<long>();

        private Text toastText, listHeaderText, hintText;
        private GameObject canvasGo; // parented to the head, so it is hidden / shown in OnDisable / OnEnable

        /// <summary>One visible line of the system list: number + text, frame shown when selected.</summary>
        private class ListRow
        {
            public GameObject root;
            public Text text;
            public GameObject frame;
        }
        private ListRow[] rows;
        private float toastHideTime;
        private float lastSwipeTime = -999f;
        private bool touchpadBound;

        private List<RemoteTodo> systemTodos = new List<RemoteTodo>();
        private int selected;
        private int scrollStart; // first visible row index; only moves when the selection leaves the window

        /// <summary>Show the START ISLAND row under the list (set by FeatureLauncher).</summary>
        public bool ContinueEnabled
        {
            get => continueEnabled;
            set { continueEnabled = value; if (rows != null) RefreshList(); }
        }
        private bool continueEnabled;

        /// <summary>Fires when the user taps the START ISLAND row (only with ContinueEnabled and >= 1 todo).</summary>
        public event Action OnContinue;

        private ListRow startRow;

        // Delete flow: sideways swipe arms it for the selected todo, tap confirms, any swipe / timeout cancels.
        private bool deleteArmed;
        private long deleteTimestamp;
        private float deleteDeadline;
        // The START row is selected when selected == systemTodos.Count.
        // Only reachable once there is at least one todo: with an empty list, a tap must always open voice input.
        private bool StartSelected => continueEnabled && systemTodos.Count > 0 && selected == systemTodos.Count;
        private int MaxIndex => continueEnabled && systemTodos.Count > 0 ? systemTodos.Count : Mathf.Max(0, systemTodos.Count - 1);

        /// <summary>Runtime wiring, used by VoiceTodoTestBootstrap. Call before the component is enabled.</summary>
        public void Configure(Transform head, bool invertSwipe, float systemOpenTimeoutSeconds)
        {
            this.invertSwipe = invertSwipe;
            this.systemOpenTimeoutSeconds = systemOpenTimeoutSeconds;
            BuildCanvas(head);
        }

        private void OnEnable()
        {
            BindTouchpad();
            if (canvasGo != null) canvasGo.SetActive(true);
            // Re-read the system list when shown again (it may have changed while hidden).
            if (rows != null) RefreshSystemList();
        }

        // Trigger: component disabled (e.g. FeatureLauncher went back to its home screen).
        // The canvas lives under the head camera, not under this object, so hide it explicitly.
        private void OnDisable()
        {
            UnbindTouchpad();
            if (canvasGo != null) canvasGo.SetActive(false);
        }

        private void Start() => RefreshSystemList();

        // Trigger: focus changes. Losing focus right after a tap = the system voice screen opened.
        // Getting focus back from it = the system is done (todo saved, or user backed out).
        private void OnApplicationFocus(bool focus)
        {
            if (toastText == null) return; // not configured yet
            Debug.Log($"[VoiceTest] Focus {focus} (flow {flow})");
            if (!focus)
            {
                if (flow == Flow.OpeningSystemScreen) flow = Flow.InSystemScreen;
                return;
            }
            if (flow == Flow.InSystemScreen) FinishVoiceCreate();
            else RefreshSystemList();
        }

        // ------------------------------------------------------------------ input

        private void BindTouchpad()
        {
            if (Application.isEditor) return;
            SimpleTouchForLite touch;
            try { touch = SimpleTouchForLite.Instance; }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceTest] Touchpad init failed, check RayNeoGeneralSettings touchConfigs: {e.Message}");
                return;
            }
            touch.OnSimpleTap.AddListener(HandleTap);
            touch.OnTripleTap.AddListener(HandleTripleTap);
            // Vertical per-step swipe events (X3 Pro only): one event each time the finger travels
            // SwipYStepTriggerDistance (25 touch px, half the horizontal step), so the cooldown matters.
            // The *End variants are not used, same as the horizontal ones in AnchorPlacementInput.
            touch.OnSwipeUp.AddListener(HandleSwipeUp);
            touch.OnSwipeDown.AddListener(HandleSwipeDown);
            // Horizontal per-step swipes: ask to delete the selected todo.
            touch.OnSwipeRight.AddListener(HandleSwipeSide);
            touch.OnSwipeLeft.AddListener(HandleSwipeSide);
            touchpadBound = true;
        }

        private void UnbindTouchpad()
        {
            if (!touchpadBound || !SimpleTouchForLite.SingletonExist) return;
            var touch = SimpleTouchForLite.Instance;
            touch.OnSimpleTap.RemoveListener(HandleTap);
            touch.OnTripleTap.RemoveListener(HandleTripleTap);
            touch.OnSwipeUp.RemoveListener(HandleSwipeUp);
            touch.OnSwipeDown.RemoveListener(HandleSwipeDown);
            touch.OnSwipeRight.RemoveListener(HandleSwipeSide);
            touch.OnSwipeLeft.RemoveListener(HandleSwipeSide);
            touchpadBound = false;
            deleteArmed = false;
        }

        // Trigger: SimpleTouchForLite.OnSimpleTap while idle. Snapshots the current list, then opens
        // the system voice screen.
        private void HandleTap()
        {
            if (flow != Flow.Idle) return;

            // Delete was asked for: this tap confirms it.
            if (deleteArmed) { ConfirmDelete(); return; }

            // START ISLAND row: go on to the island game instead of opening voice input.
            if (StartSelected)
            {
                if (systemTodos.Count == 0) { ShowToast("Add a todo first (tap on a list row)"); return; }
                Debug.Log($"[VoiceTest] Continue with {systemTodos.Count} todos");
                OnContinue?.Invoke();
                return;
            }

            RefreshSystemList();
            knownBeforeTap.Clear();
            foreach (var t in systemTodos) knownBeforeTap.Add(t.timestamp);

            if (!SystemTodoClient.StartVoiceCreate())
            {
                ShowToast("System To-do not available");
                return;
            }
            flow = Flow.OpeningSystemScreen;
            openDeadline = Time.unscaledTime + systemOpenTimeoutSeconds;
            ShowToast("Say your todo");
            RefreshHint();
#if UNITY_EDITOR
            // No system screen and no focus change in the Editor: the fake item already exists.
            flow = Flow.InSystemScreen;
            FinishVoiceCreate();
#endif
        }

        // Trigger: SimpleTouchForLite.OnTripleTap while idle. Marks the selected system item finished.
        private void HandleTripleTap()
        {
            if (deleteArmed) { CancelDelete(); return; }
            if (flow != Flow.Idle || systemTodos.Count == 0 || selected >= systemTodos.Count) return;
            var item = systemTodos[selected];
            if (SystemTodoClient.SetFinished(item.timestamp)) ShowToast("Done: " + Shorten(item.text, 24));
            else ShowToast("System To-do update failed");
            RefreshSystemList();
        }

        // Trigger: SimpleTouchForLite.OnSwipeUp / OnSwipeDown. Up = previous (smaller number) row.
        // Physical direction still to confirm on device; invertSwipe flips it.
        private void HandleSwipeUp(Vector2 _) => HandleSwipe(invertSwipe ? +1 : -1);
        private void HandleSwipeDown(Vector2 _) => HandleSwipe(invertSwipe ? -1 : +1);

        private void HandleSwipe(int dir)
        {
            // Threshold: ignore repeats within swipeCooldownSeconds of the last accepted swipe.
            if (Time.unscaledTime - lastSwipeTime < swipeCooldownSeconds) return;
            lastSwipeTime = Time.unscaledTime;
            if (deleteArmed) { CancelDelete(); return; } // swipe = keep, selection stays
            if (systemTodos.Count == 0 && !continueEnabled) return;
            // Clamped, not wrapped: at the top / bottom row the selection stays put, like a list.
            // With ContinueEnabled the last step down is the START ISLAND row.
            selected = Mathf.Clamp(selected + dir, 0, MaxIndex);
            RefreshList();
        }

        // Trigger: SimpleTouchForLite.OnSwipeRight / OnSwipeLeft (either direction). First swipe asks to delete
        // the selected todo, a second sideways swipe keeps it.
        private void HandleSwipeSide(Vector2 _) => HandleSwipeSide();

        private void HandleSwipeSide()
        {
            if (Time.unscaledTime - lastSwipeTime < swipeCooldownSeconds) return;
            lastSwipeTime = Time.unscaledTime;
            if (flow != Flow.Idle) return;
            if (deleteArmed) { CancelDelete(); return; }
            if (systemTodos.Count == 0 || selected >= systemTodos.Count) return; // START row: nothing to delete

            deleteArmed = true;
            deleteTimestamp = systemTodos[selected].timestamp;
            deleteDeadline = Time.unscaledTime + deleteConfirmSeconds;
            ShowToast("Delete? Tap = yes, swipe = keep");
            RefreshList();
        }

        private void ConfirmDelete()
        {
            deleteArmed = false;
            var item = systemTodos.Find(t => t.timestamp == deleteTimestamp);
            if (item == null) { RefreshSystemList(); return; }
            if (SystemTodoClient.Delete(item.timestamp)) ShowToast("Deleted: " + Shorten(item.text, 24));
            else ShowToast("System To-do delete failed");
            RefreshSystemList();
        }

        private void CancelDelete()
        {
            deleteArmed = false;
            ShowToast("Kept");
            RefreshList();
        }

        // ------------------------------------------------------------------ voice flow

        /// <summary>Back from the system screen: diff the list against the snapshot taken at the tap.</summary>
        private void FinishVoiceCreate()
        {
            flow = Flow.Idle;
            RefreshHint();
            RefreshSystemList();

            var added = new List<RemoteTodo>();
            foreach (var t in systemTodos) if (!knownBeforeTap.Contains(t.timestamp)) added.Add(t);

            if (added.Count == 0)
            {
                ShowToast("No todo created");
                return;
            }

            foreach (var t in added) Debug.Log($"[VoiceTest] New system todo {t.timestamp}: \"{t.text}\"");
            // List is oldest first, so the newest added item is the last one: select it and show it.
            var newest = added[added.Count - 1];
            selected = systemTodos.IndexOf(newest);
            RefreshList();
            // Feedback toast (top zone, hides after toastSeconds); text cut to fit one line.
            ShowToast("Added: " + Shorten(newest.text, ToastMaxChars));
        }

        // ------------------------------------------------------------------ view

        private void RefreshSystemList()
        {
            systemTodos = SystemTodoClient.GetUnfinished();
            // Oldest first: number 1 = oldest, highest number = most recently added.
            systemTodos.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            selected = Mathf.Clamp(selected, 0, MaxIndex);
            RefreshList();
        }

        private void Update()
        {
#if UNITY_EDITOR
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) HandleTap();
                if (kb.backspaceKey.wasPressedThisFrame) HandleTripleTap();
                if (kb.downArrowKey.wasPressedThisFrame) HandleSwipe(+1);
                if (kb.upArrowKey.wasPressedThisFrame) HandleSwipe(-1);
                if (kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) HandleSwipeSide();
            }
#endif
            // Trigger: delete asked for but no tap within deleteConfirmSeconds -> keep the todo.
            if (deleteArmed && Time.unscaledTime > deleteDeadline) CancelDelete();
            // Trigger: tapped, StartVoiceCreate returned true, but focus was never lost within
            // systemOpenTimeoutSeconds -> the system screen did not open (e.g. notes app in a bad state).
            if (flow == Flow.OpeningSystemScreen && Time.unscaledTime > openDeadline)
            {
                flow = Flow.Idle;
                ShowToast("System To-do did not open");
                RefreshHint();
            }

            if (toastText.text.Length > 0 && Time.unscaledTime > toastHideTime) toastText.text = "";
        }

        private void RefreshHint()
        {
            if (flow != Flow.Idle) hintText.text = "Opening voice input...";
            else if (deleteArmed) hintText.text = "Tap: DELETE   Swipe: keep";
            else if (StartSelected) hintText.text = "Tap: start island   Swipe up: back to list";
            else hintText.text = continueEnabled
                ? "Tap: add by voice   3x tap: done   Swipe sideways: delete\nSwipe down to START ISLAND"
                : "Tap: add by voice   3x tap: done   Swipe sideways: delete";
        }

        /// <summary>
        /// Numbered rows (number = position in the whole list, so it stays meaningful while scrolling).
        /// Selected row: cyan 2 px frame + cyan text + ">" marker. Others: plain green.
        /// Header: count / position, plus up / down arrows when rows are hidden above / below.
        /// </summary>
        private void RefreshList()
        {
            int count = systemTodos.Count;
            // Scroll window: stays still while the selection moves inside it, shifts by one row when the
            // selection would leave it (no jumping).
            if (selected < scrollStart) scrollStart = selected;
            if (selected >= scrollStart + MaxSystemShown) scrollStart = selected - MaxSystemShown + 1;
            scrollStart = Mathf.Clamp(scrollStart, 0, Mathf.Max(0, count - MaxSystemShown));
            int start = scrollStart;
            int end = Mathf.Min(count, start + MaxSystemShown);

            string header = count == 0
                ? "YOUR TO-DO  <size=18>empty - tap to add by voice</size>"
                : $"YOUR TO-DO  {Mathf.Min(selected + 1, count)}/{count}";
            if (start > 0) header += "  ▲";
            if (end < count) header += "  ▼";
            listHeaderText.text = $"<color={PurpleHex}>{header}</color>";

            for (int r = 0; r < rows.Length; r++)
            {
                int i = start + r;
                var row = rows[r];
                bool visible = i < end;
                row.root.SetActive(visible);
                if (!visible) continue;

                bool isSelected = i == selected;
                bool asking = deleteArmed && systemTodos[i].timestamp == deleteTimestamp;
                string marker = isSelected ? ">" : " ";
                row.text.text = asking
                    ? $"{marker} DELETE?   {Shorten(systemTodos[i].text, RowMaxChars - 8)}"
                    : $"{marker} {i + 1}   {Shorten(systemTodos[i].text, RowMaxChars)}";
                row.text.color = asking ? Red : isSelected ? Cyan : Green;
                row.frame.SetActive(isSelected);
            }

            // START ISLAND row (Island Gallery flow).
            if (startRow != null)
            {
                startRow.root.SetActive(continueEnabled);
                bool sel = StartSelected;
                startRow.text.text = (sel ? "> " : "  ") + "START ISLAND  >>";
                startRow.text.color = count == 0 ? new Color(Green.r, Green.g, Green.b, 0.5f) : sel ? Cyan : Green;
                startRow.frame.SetActive(sel);
            }
            if (hintText != null) RefreshHint();
        }

        private void ShowToast(string msg)
        {
            toastText.text = msg;
            toastHideTime = Time.unscaledTime + toastSeconds;
        }

        private static string Shorten(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }

        // ------------------------------------------------------------------ canvas

        private void BuildCanvas(Transform head)
        {
            var go = new GameObject("Voice Todo Canvas", typeof(RectTransform));
            canvasGo = go;
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;
            var canvasRt = (RectTransform)go.transform;
            canvasRt.SetParent(head, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = head.GetComponent<Camera>();
            go.AddComponent<XRGraphicRaycaster>();

            // Visible diagonal at HudDistanceM = 2 * d * tan(FOV/2); divide by the canvas diagonal in px.
            float diagPx = Mathf.Sqrt(CanvasWidthPx * CanvasWidthPx + CanvasHeightPx * CanvasHeightPx);
            float metersPerPx = 2f * HudDistanceM * Mathf.Tan(DisplayFovDeg * 0.5f * Mathf.Deg2Rad) / diagPx;
            canvasRt.sizeDelta = new Vector2(CanvasWidthPx, CanvasHeightPx);
            canvasRt.localPosition = new Vector3(0f, 0f, HudDistanceM);
            canvasRt.localRotation = Quaternion.identity;
            canvasRt.localScale = Vector3.one * metersPerPx;

            float innerWidth = CanvasWidthPx - 2 * MarginPx;
            // top zone = toasts
            toastText = CreateText(canvasRt, "ToastText", ToastFontSize, Cyan, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -MarginPx), new Vector2(innerWidth, 32f));
            // center zone = system To-do list: header + MaxSystemShown row "buttons"
            listHeaderText = CreateText(canvasRt, "SystemListHeader", ListFontSize, Green, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, ListTopPx), new Vector2(innerWidth, ListHeaderPx));
            rows = new ListRow[MaxSystemShown];
            for (int r = 0; r < rows.Length; r++)
            {
                float y = ListTopPx - ListHeaderPx - RowGapPx - r * (RowHeightPx + RowGapPx);
                rows[r] = CreateRow(canvasRt, r, y);
            }
            // START ISLAND row, one extra gap below the last list row (hidden unless ContinueEnabled).
            float startY = ListTopPx - ListHeaderPx - RowGapPx - MaxSystemShown * (RowHeightPx + RowGapPx) - 8f;
            startRow = CreateRow(canvasRt, MaxSystemShown, startY);
            startRow.root.name = "StartIslandRow";
            startRow.root.SetActive(false);
            // bottom zone = hints
            hintText = CreateText(canvasRt, "HintText", HintFontSize, Green, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, MarginPx), new Vector2(innerWidth, 26f));
            RefreshHint();
        }

        /// <summary>Row = left-aligned text + a frame of four 2 px cyan lines (outline only, no fill).</summary>
        private static ListRow CreateRow(RectTransform parent, int index, float topY)
        {
            var root = new GameObject($"Row{index}", typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            var rt = (RectTransform)root.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topY);
            rt.sizeDelta = new Vector2(RowWidthPx, RowHeightPx);

            var text = CreateText(rt, "Text", ListFontSize, Green, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(RowPaddingPx, 0f),
                new Vector2(RowWidthPx - 2 * RowPaddingPx, RowHeightPx));
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // one line; Shorten() limits the length

            var frame = new GameObject("Frame", typeof(RectTransform));
            frame.layer = root.layer;
            var frt = (RectTransform)frame.transform;
            frt.SetParent(rt, false);
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            CreateLine(frt, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, FrameLinePx));
            CreateLine(frt, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, FrameLinePx));
            CreateLine(frt, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(FrameLinePx, 0f));
            CreateLine(frt, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(FrameLinePx, 0f));
            frame.SetActive(false);

            return new ListRow { root = root, text = text, frame = frame };
        }

        // Stretched along one edge of the parent; size = line thickness on the other axis.
        private static void CreateLine(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>(); // no sprite = solid quad
            img.color = Cyan;
            img.raycastTarget = false;
        }

        private static Text CreateText(RectTransform parent, string name, int fontSize, Color color, TextAnchor align,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = align;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = "";
            return text;
        }
    }
}