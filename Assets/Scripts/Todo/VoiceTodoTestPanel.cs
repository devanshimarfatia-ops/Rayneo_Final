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
    /// Device validation panel for voice todos + the RayNeo system To-do sync.
    ///
    ///   Single tap       -> start talking / stop and recognise      (Editor: Space)
    ///   Double tap       -> cancel while listening; otherwise left to the system back/exit (Editor: Escape)
    ///   Swipe fwd/back   -> select an item of the system To-do list (Editor: Left / Right arrow)
    ///   Triple tap       -> mark the selected system item finished  (Editor: Backspace)
    ///
    /// Recognised sentences are only kept in memory here (last few shown); the real TodoStore comes later.
    /// The system list is re-read on start, after a change, and when the app regains focus.
    /// </summary>
    public class VoiceTodoTestPanel : MonoBehaviour
    {
        [SerializeField] private float toastSeconds = 2.5f;
        [Tooltip("Swipes closer together than this are ignored, so one swipe = one step.")]
        [SerializeField] private float swipeCooldownSeconds = 0.35f;
        [Tooltip("Flip if swiping forward on the temple moves the selection the wrong way.")]
        [SerializeField] private bool invertSwipe = false;

        // ---- Layout (RayNeo design specs, same canvas math as ZoneDebugHUD) ----
        private const float CanvasWidthPx = 640f;
        private const float CanvasHeightPx = 480f;
        private const float DisplayFovDeg = 30f;
        private const float HudDistanceM = 2f;
        private const float MarginPx = 24f;
        private const int ToastFontSize = 22;
        private const int VoiceFontSize = 26;
        private const int ListFontSize = 20;
        private const int HintFontSize = 18;
        // How many recognised sentences / system items are shown at once.
        private const int MaxRecognisedShown = 3;
        private const int MaxSystemShown = 4;

        private static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        private static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);
        private const string CyanHex = "#4DE2FF";
        private const string PurpleHex = "#8566FF";

        private VoiceInput voice;
        private Text toastText, voiceText, listText, hintText;
        private float toastHideTime;
        private float nextRefresh;
        private float lastSwipeTime = -999f;
        private bool touchpadBound;

        private string partialText = "";
        private readonly List<string> recognised = new List<string>();
        private List<RemoteTodo> systemTodos = new List<RemoteTodo>();
        private int selected;

        /// <summary>Runtime wiring, used by VoiceTodoTestBootstrap. Call before the component is enabled.</summary>
        public void Configure(VoiceInput voice, Transform head, bool invertSwipe)
        {
            this.voice = voice;
            this.invertSwipe = invertSwipe;
            BuildCanvas(head);
        }

        private void OnEnable()
        {
            voice.OnStateChanged += HandleVoiceState;
            voice.OnPartial += HandlePartial;
            voice.OnResult += HandleResult;
            voice.OnError += HandleError;
            BindTouchpad();
        }

        private void OnDisable()
        {
            voice.OnStateChanged -= HandleVoiceState;
            voice.OnPartial -= HandlePartial;
            voice.OnResult -= HandleResult;
            voice.OnError -= HandleError;
            UnbindTouchpad();
        }

        private void Start()
        {
            RefreshSystemList();
            if (!voice.IsAvailable && !Application.isEditor) ShowToast("No speech service found");
        }

        // Trigger: app regains focus (e.g. back from the system To-do app) -> re-read its list.
        private void OnApplicationFocus(bool focus)
        {
            if (focus && voice != null) RefreshSystemList();
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
            touch.OnDoubleTap.AddListener(HandleDoubleTap);
            touch.OnTripleTap.AddListener(HandleTripleTap);
            // Per-step swipe events; the *End variants never fire in ARDK 1.1.2 (see AnchorPlacementInput).
            touch.OnSwipeRight.AddListener(HandleSwipeRight);
            touch.OnSwipeLeft.AddListener(HandleSwipeLeft);
            touchpadBound = true;
        }

        private void UnbindTouchpad()
        {
            if (!touchpadBound || !SimpleTouchForLite.SingletonExist) return;
            var touch = SimpleTouchForLite.Instance;
            touch.OnSimpleTap.RemoveListener(HandleTap);
            touch.OnDoubleTap.RemoveListener(HandleDoubleTap);
            touch.OnTripleTap.RemoveListener(HandleTripleTap);
            touch.OnSwipeRight.RemoveListener(HandleSwipeRight);
            touch.OnSwipeLeft.RemoveListener(HandleSwipeLeft);
            touchpadBound = false;
        }

        // Trigger: SimpleTouchForLite.OnSimpleTap. Idle -> start; listening -> stop and recognise.
        private void HandleTap()
        {
            switch (voice.State)
            {
                case VoiceState.Idle:
                    partialText = "";
                    voice.StartListening();
                    break;
                case VoiceState.Starting:
                case VoiceState.Listening:
                    voice.StopListening();
                    break;
            }
        }

        // Trigger: SimpleTouchForLite.OnDoubleTap. Only cancels a running session.
        private void HandleDoubleTap()
        {
            if (voice.State == VoiceState.Idle) return;
            voice.Cancel();
            partialText = "";
            ShowToast("Cancelled");
        }

        // Trigger: SimpleTouchForLite.OnTripleTap. Marks the selected system item finished.
        private void HandleTripleTap()
        {
            if (voice.State != VoiceState.Idle || systemTodos.Count == 0) return;
            var item = systemTodos[selected];
            if (SystemTodoClient.SetFinished(item.timestamp)) ShowToast("Done: " + Shorten(item.text, 24));
            else ShowToast("System To-do update failed");
            RefreshSystemList();
        }

        private void HandleSwipeRight(Vector2 _) => HandleSwipe(invertSwipe ? -1 : +1);
        private void HandleSwipeLeft(Vector2 _) => HandleSwipe(invertSwipe ? +1 : -1);

        private void HandleSwipe(int dir)
        {
            // Threshold: ignore repeats within swipeCooldownSeconds of the last accepted swipe.
            if (Time.unscaledTime - lastSwipeTime < swipeCooldownSeconds) return;
            lastSwipeTime = Time.unscaledTime;
            if (systemTodos.Count == 0) return;
            selected = (selected + dir + systemTodos.Count) % systemTodos.Count;
            RefreshList();
        }

        // ------------------------------------------------------------------ voice events

        private void HandleVoiceState(VoiceState s)
        {
            if (s == VoiceState.Listening) ShowToast("Listening...");
            RefreshVoice();
        }

        private void HandlePartial(string text)
        {
            partialText = text;
            RefreshVoice();
        }

        private void HandleResult(string text)
        {
            partialText = "";
            recognised.Insert(0, text);
            if (recognised.Count > MaxRecognisedShown) recognised.RemoveAt(recognised.Count - 1);
            ShowToast("Todo added");
            RefreshVoice();
        }

        private void HandleError(string reason)
        {
            partialText = "";
            ShowToast(reason);
            RefreshVoice();
        }

        // ------------------------------------------------------------------ view

        private void RefreshSystemList()
        {
            systemTodos = SystemTodoClient.GetUnfinished();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, systemTodos.Count - 1));
            RefreshList();
        }

        private void Update()
        {
#if UNITY_EDITOR
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) HandleTap();
                if (kb.escapeKey.wasPressedThisFrame) HandleDoubleTap();
                if (kb.backspaceKey.wasPressedThisFrame) HandleTripleTap();
                if (kb.rightArrowKey.wasPressedThisFrame) HandleSwipe(+1);
                if (kb.leftArrowKey.wasPressedThisFrame) HandleSwipe(-1);
            }
#endif
            if (toastText.text.Length > 0 && Time.unscaledTime > toastHideTime) toastText.text = "";

            // ~5 Hz refresh for the mic level bar, saves power vs. every frame.
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            if (voice.State == VoiceState.Listening) RefreshVoice();
        }

        private void RefreshVoice()
        {
            var sb = new System.Text.StringBuilder();
            switch (voice.State)
            {
                case VoiceState.Idle:
                    sb.Append("<size=20>MY TODOS (voice)</size>\n");
                    if (recognised.Count == 0) sb.Append("<size=20>Tap and say a todo</size>");
                    for (int i = 0; i < recognised.Count; i++)
                        sb.Append(i == 0 ? $"<color={CyanHex}>{recognised[i]}</color>" : recognised[i]).Append('\n');
                    break;
                case VoiceState.WaitingPermission:
                    sb.Append("Allow microphone access");
                    break;
                case VoiceState.Starting:
                    sb.Append("Starting mic...");
                    break;
                case VoiceState.Listening:
                    // Mic level bar: onRmsChanged is roughly -2..10 dB -> 0..10 bars.
                    int bars = Mathf.Clamp(Mathf.RoundToInt((voice.Rms + 2f) / 12f * 10f), 0, 10);
                    sb.Append($"<color={CyanHex}>LISTENING  {new string('|', bars)}</color>\n");
                    sb.Append(string.IsNullOrEmpty(partialText) ? "..." : partialText);
                    break;
                case VoiceState.Processing:
                    sb.Append($"<color={CyanHex}>RECOGNISING...</color>\n").Append(partialText);
                    break;
            }
            voiceText.text = sb.ToString().TrimEnd();

            hintText.text = voice.State == VoiceState.Idle
                ? "Tap: talk   Swipe: select   3x tap: done"
                : "Tap: stop   2x tap: cancel";
        }

        private void RefreshList()
        {
            var sb = new System.Text.StringBuilder($"<color={PurpleHex}>SYSTEM TO-DO ({systemTodos.Count})</color>\n");
            if (systemTodos.Count == 0) sb.Append("<size=18>empty or not reachable</size>");
            // Scroll window keeps the selection visible.
            int start = Mathf.Clamp(selected - MaxSystemShown + 1, 0, Mathf.Max(0, systemTodos.Count - MaxSystemShown));
            for (int i = start; i < Mathf.Min(systemTodos.Count, start + MaxSystemShown); i++)
            {
                string t = Shorten(systemTodos[i].text, 30);
                sb.Append(i == selected ? $"<color={CyanHex}>[ {t} ]</color>" : t).Append('\n');
            }
            listText.text = sb.ToString().TrimEnd();
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
            // center, upper half = voice text / recognised todos
            voiceText = CreateText(canvasRt, "VoiceText", VoiceFontSize, Green, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(innerWidth, 150f));
            // center, lower half = system To-do list
            listText = CreateText(canvasRt, "SystemListText", ListFontSize, Green, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(innerWidth, 140f));
            // bottom zone = hints
            hintText = CreateText(canvasRt, "HintText", HintFontSize, Green, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, MarginPx), new Vector2(innerWidth, 26f));
            RefreshVoice();
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
