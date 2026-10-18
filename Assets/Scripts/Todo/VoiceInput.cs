using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace RayneoIdle.Todo
{
    public enum VoiceState { Idle, WaitingPermission, Starting, Listening, Processing }

    /// <summary>
    /// Speech-to-text through the Android platform SpeechRecognizer (no Java plugin).
    /// On the X3 Pro the system default RecognitionService is com.rayneo.live.ai.
    ///
    /// Flow: StartListening() -> OnPartial (live text) -> OnResult (final text) or OnError.
    ///
    /// Threading: SpeechRecognizer must be created and driven on the Android UI thread, so every
    /// recognizer call goes through runOnUiThread. RecognitionListener callbacks arrive on that Java
    /// thread and are queued, then raised as C# events from Update() on the Unity main thread.
    ///
    /// Editor: there is no recognizer; StartListening() fires OnResult with Editor Fake Text after
    /// Editor Fake Delay, so the rest of the flow can be tested without the glasses.
    /// </summary>
    public class VoiceInput : MonoBehaviour
    {
        [Tooltip("BCP-47 language tag passed to the recognizer. The game is English-only, keep en-US. Empty = system language.")]
        [SerializeField] private string language = "en-US";
        [Tooltip("SAFETY TIMEOUT: if neither a result nor an error arrives this many seconds after " +
                 "StartListening, the session is cancelled and OnError(\"Timeout\") fires.")]
        [SerializeField] private float sessionTimeoutSeconds = 15f;
#pragma warning disable 0414, 0067 // editor-only fields / events unused in some build targets
        [Tooltip("Editor only: text returned instead of real speech.")]
        [SerializeField] private string editorFakeText = "buy milk tomorrow";
        [Tooltip("Editor only: seconds between StartListening and the fake result.")]
        [SerializeField] private float editorFakeDelay = 1.5f;

        public VoiceState State { get; private set; } = VoiceState.Idle;
        /// <summary>Latest microphone level from onRmsChanged (dB, roughly -2..10). 0 when idle.</summary>
        public float Rms { get; private set; }
        /// <summary>False if the platform reports no recognition service (see Init log).</summary>
        public bool IsAvailable { get; private set; }

        public event Action<VoiceState> OnStateChanged;
        public event Action<string> OnPartial;  // live text while speaking (may be empty on some engines)
        public event Action<string> OnResult;   // final best text
        public event Action<string> OnError;    // short human-readable reason
#pragma warning restore 0414, 0067

        public void Configure(string language, float sessionTimeoutSeconds)
        {
            this.language = language;
            this.sessionTimeoutSeconds = sessionTimeoutSeconds;
        }

        // Callbacks from Java threads, drained in Update().
        private readonly Queue<Action> mainThreadQueue = new Queue<Action>();
        private float sessionDeadline;

        private void Enqueue(Action a)
        {
            lock (mainThreadQueue) mainThreadQueue.Enqueue(a);
        }

        private void SetState(VoiceState s)
        {
            if (State == s) return;
            State = s;
            if (s == VoiceState.Idle) Rms = 0f;
            Debug.Log($"[Voice] State -> {s}");
            OnStateChanged?.Invoke(s);
        }

        private void Update()
        {
            while (true)
            {
                Action a;
                lock (mainThreadQueue)
                {
                    if (mainThreadQueue.Count == 0) break;
                    a = mainThreadQueue.Dequeue();
                }
                a();
            }

            // Trigger: session running longer than sessionTimeoutSeconds without result/error.
            if (State != VoiceState.Idle && State != VoiceState.WaitingPermission &&
                Time.unscaledTime > sessionDeadline)
            {
                Cancel();
                Fail("Timeout");
            }

#if UNITY_EDITOR
            if (State == VoiceState.Listening && Time.unscaledTime > editorResultTime)
            {
                SetState(VoiceState.Idle);
                Debug.Log($"[Voice] (Editor) result: {editorFakeText}");
                OnResult?.Invoke(editorFakeText);
            }
#endif
        }

        private void Fail(string reason)
        {
            SetState(VoiceState.Idle);
            Debug.LogWarning($"[Voice] Error: {reason}");
            OnError?.Invoke(reason);
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Start one recognition session. Ignored while a session is already running.</summary>
        public void StartListening()
        {
            if (State != VoiceState.Idle) return;
            sessionDeadline = Time.unscaledTime + sessionTimeoutSeconds;
#if UNITY_EDITOR
            editorResultTime = Time.unscaledTime + editorFakeDelay;
            SetState(VoiceState.Listening);
#elif UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                RequestMicPermission();
                return;
            }
            StartAndroid();
#else
            Fail("Unsupported platform");
#endif
        }

        /// <summary>Stop recording and let the engine finish recognising what was said so far.</summary>
        public void StopListening()
        {
            if (State != VoiceState.Listening && State != VoiceState.Starting) return;
#if UNITY_EDITOR
            editorResultTime = Time.unscaledTime; // deliver the fake result now
#elif UNITY_ANDROID
            SetState(VoiceState.Processing);
            RunOnUi(() => recognizer?.Call("stopListening"));
#endif
        }

        /// <summary>Abort the session, no result is delivered.</summary>
        public void Cancel()
        {
            if (State == VoiceState.Idle) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            RunOnUi(() => recognizer?.Call("cancel"));
#endif
            SetState(VoiceState.Idle);
        }

#if UNITY_EDITOR
        private float editorResultTime;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        // ------------------------------------------------------------------ Android

        // android.speech constants (SpeechRecognizer / RecognizerIntent)
        private const string ActionRecognizeSpeech = "android.speech.action.RECOGNIZE_SPEECH";
        private const string ExtraLanguageModel = "android.speech.extra.LANGUAGE_MODEL";
        private const string LanguageModelFreeForm = "free_form";
        private const string ExtraLanguage = "android.speech.extra.LANGUAGE";
        private const string ExtraLanguagePreference = "android.speech.extra.LANGUAGE_PREFERENCE";
        private const string ExtraOnlyReturnLanguagePreference = "android.speech.extra.ONLY_RETURN_LANGUAGE_PREFERENCE";
        private const string ExtraPartialResults = "android.speech.extra.PARTIAL_RESULTS";
        private const string ExtraMaxResults = "android.speech.extra.MAX_RESULTS";
        private const string ExtraCallingPackage = "calling_package";
        private const string ResultsRecognition = "results_recognition";

        private AndroidJavaObject activity;
        private AndroidJavaObject recognizer; // created and used on the UI thread only
        private ListenerProxy listener;

        private void Awake()
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                activity = player.GetStatic<AndroidJavaObject>("currentActivity");

            using (var sr = new AndroidJavaClass("android.speech.SpeechRecognizer"))
                IsAvailable = sr.CallStatic<bool>("isRecognitionAvailable", activity);
            Debug.Log($"[Voice] isRecognitionAvailable = {IsAvailable}" +
                      (IsAvailable ? "" : " (check <queries> RecognitionService in AndroidManifest)"));

            listener = new ListenerProxy(this);
            RunOnUi(() =>
            {
                using (var sr = new AndroidJavaClass("android.speech.SpeechRecognizer"))
                    recognizer = sr.CallStatic<AndroidJavaObject>("createSpeechRecognizer", activity);
                recognizer.Call("setRecognitionListener", listener);
                Debug.Log("[Voice] SpeechRecognizer created");
            });
        }

        private void OnDestroy()
        {
            var r = recognizer;
            recognizer = null;
            if (r != null && activity != null)
                RunOnUi(() => { r.Call("destroy"); r.Dispose(); });
        }

        private void OnApplicationPause(bool paused)
        {
            // Trigger: app goes to background. Release the microphone immediately.
            if (paused) Cancel();
        }

        private void RunOnUi(Action a)
        {
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try { a(); }
                catch (Exception e) { Enqueue(() => Fail("Recognizer: " + e.Message)); }
            }));
        }

        private void RequestMicPermission()
        {
            SetState(VoiceState.WaitingPermission);
            var cb = new PermissionCallbacks();
            cb.PermissionGranted += _ => { SetState(VoiceState.Idle); StartListening(); };
            cb.PermissionDenied += _ => Fail("Mic permission denied");
            cb.PermissionDeniedAndDontAskAgain += _ => Fail("Mic permission denied (enable in Settings)");
            Permission.RequestUserPermission(Permission.Microphone, cb);
        }

        private void StartAndroid()
        {
            if (!IsAvailable) { Fail("No speech service"); return; }
            SetState(VoiceState.Starting);
            string pkg = Application.identifier;
            string lang = language;
            RunOnUi(() =>
            {
                using (var intent = new AndroidJavaObject("android.content.Intent", ActionRecognizeSpeech))
                {
                    intent.Call<AndroidJavaObject>("putExtra", ExtraLanguageModel, LanguageModelFreeForm);
                    intent.Call<AndroidJavaObject>("putExtra", ExtraPartialResults, true);
                    intent.Call<AndroidJavaObject>("putExtra", ExtraMaxResults, 1);
                    intent.Call<AndroidJavaObject>("putExtra", ExtraCallingPackage, pkg);
                    if (!string.IsNullOrEmpty(lang))
                    {
                        // Lock the engine to this language (no auto-detect into the system language).
                        intent.Call<AndroidJavaObject>("putExtra", ExtraLanguage, lang);
                        intent.Call<AndroidJavaObject>("putExtra", ExtraLanguagePreference, lang);
                        intent.Call<AndroidJavaObject>("putExtra", ExtraOnlyReturnLanguagePreference, true);
                    }
                    recognizer.Call("startListening", intent);
                }
            });
        }

        private static string FirstResult(AndroidJavaObject bundle)
        {
            if (bundle == null) return "";
            using (var list = bundle.Call<AndroidJavaObject>("getStringArrayList", ResultsRecognition))
            {
                if (list == null || list.Call<int>("size") == 0) return "";
                // ArrayList.get returns Object, so fetch it as an object and convert with toString.
                using (var first = list.Call<AndroidJavaObject>("get", 0))
                    return first?.Call<string>("toString") ?? "";
            }
        }

        // SpeechRecognizer.ERROR_* codes
        private static string ErrorName(int code)
        {
            switch (code)
            {
                case 1: return "Network timeout";
                case 2: return "Network error";
                case 3: return "Audio error";
                case 4: return "Server error";
                case 5: return "Client error";
                case 6: return "No speech heard";
                case 7: return "Not recognised";
                case 8: return "Recognizer busy";
                case 9: return "No permission (service)";
                case 10: return "Too many requests";
                case 11: return "Server disconnected";
                case 12: return "Language not supported";
                case 13: return "Language unavailable";
                default: return "Error " + code;
            }
        }

        /// <summary>
        /// Implements android.speech.RecognitionListener. Uses the low-level Invoke override so every
        /// argument arrives as AndroidJavaObject (boxed primitives) and no signature matching can fail.
        /// Runs on the Java UI thread: only reads Java objects here, Unity work is queued.
        /// </summary>
        private class ListenerProxy : AndroidJavaProxy
        {
            private readonly VoiceInput owner;
            public ListenerProxy(VoiceInput owner) : base("android.speech.RecognitionListener") { this.owner = owner; }

            public override AndroidJavaObject Invoke(string methodName, AndroidJavaObject[] args)
            {
                switch (methodName)
                {
                    case "onReadyForSpeech":
                        owner.Enqueue(() => { if (owner.State == VoiceState.Starting) owner.SetState(VoiceState.Listening); });
                        return null;
                    case "onBeginningOfSpeech":
                        return null;
                    case "onRmsChanged":
                        float rms = args[0].Call<float>("floatValue");
                        owner.Enqueue(() => owner.Rms = rms);
                        return null;
                    case "onBufferReceived":
                        return null;
                    case "onEndOfSpeech":
                        // Engine detected silence: recording stopped, waiting for the final result.
                        owner.Enqueue(() => { if (owner.State == VoiceState.Listening) owner.SetState(VoiceState.Processing); });
                        return null;
                    case "onError":
                        int code = args[0].Call<int>("intValue");
                        owner.Enqueue(() => { if (owner.State != VoiceState.Idle) owner.Fail(ErrorName(code)); });
                        return null;
                    case "onPartialResults":
                        string partial = FirstResult(args[0]);
                        owner.Enqueue(() => owner.OnPartial?.Invoke(partial));
                        return null;
                    case "onResults":
                        string text = FirstResult(args[0]);
                        owner.Enqueue(() =>
                        {
                            if (owner.State == VoiceState.Idle) return; // cancelled meanwhile
                            owner.SetState(VoiceState.Idle);
                            Debug.Log($"[Voice] Result: \"{text}\"");
                            if (string.IsNullOrWhiteSpace(text)) owner.OnError?.Invoke("Not recognised");
                            else owner.OnResult?.Invoke(text);
                        });
                        return null;
                    case "onEvent":
                    case "onSegmentResults":
                    case "onEndOfSegmentedSession":
                    case "onLanguageDetection":
                        return null;
                }
                return base.Invoke(methodName, args); // equals / hashCode / toString
            }
        }
#endif
    }
}
