using System;
using System.Collections.Generic;
using RayNeo;              // SimpleTouchForLite
using RayneoIdle;          // SwipeMap
using UnityEngine;
using UnityEngine.InputSystem;

namespace RayneoIdle.GameInput
{
    /// <summary>Game-level actions. Gameplay never reads the touchpad or keyboard directly.</summary>
    public enum GameAction
    {
        Confirm, // tap
        Back,    // double tap / Android back key (global back, never gameplay - RayNeo spec)
        Prev,    // horizontal swipe toward "previous" (physical forward, see SwipeMap)
        Next,    // horizontal swipe toward "next"
        Up,      // vertical swipe up (lists / params)
        Down,    // vertical swipe down
    }

    /// <summary>Something that wants input. Return true when the action was used (stops it there).</summary>
    public interface IGameInputHandler
    {
        bool HandleAction(GameAction action);
    }

    /// <summary>
    /// W8 InputAdapter: one input route for the whole game.
    /// Sources:
    ///   Device: SimpleTouchForLite OnSimpleTap -> Confirm, OnDoubleTap -> Back,
    ///           OnSwipeRight / OnSwipeLeft (per step, ~200 touch px) -> Prev / Next through SwipeMap,
    ///           OnSwipeUp / OnSwipeDown -> Up / Down, Android back key (Escape) -> Back.
    ///   Editor: Space / Enter = Confirm, Escape / Backspace = Back, Left / Right = Prev / Next,
    ///           Up / Down arrows = Up / Down. (Letter keys are left free for IdleDebugKeys.)
    /// Routing: handlers form a stack. The top handler gets the action first; if it returns false the
    /// action goes to the next one down. Modals (Reset confirm, pack reveal) push themselves on open
    /// and pop on close. <see cref="OnAnyAction"/> fires for every action (hints, idle timers).
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class InputAdapter : MonoBehaviour
    {
        private const string LogTag = "[Input]";

        public static InputAdapter Instance { get; private set; }

        [Tooltip("Flip horizontal swipe direction (on top of the SwipeMap convention).")]
        [SerializeField] private bool invertHorizontalSwipe;

        [Tooltip("Ignore a second action that arrives within this many seconds (touchpad step bursts).")]
        [SerializeField] private float swipeRepeatGuardSeconds = 0.18f; // device log: one swipe fires 2 steps, sometimes > 120 ms apart

        [SerializeField] private bool logActions = true;

        public event Action<GameAction> OnAnyAction;

        private readonly List<IGameInputHandler> stack = new List<IGameInputHandler>();
        private bool touchpadBound;
        private float lastSwipeTime = -1f;

        // Actions are queued and dispatched in Update, so touchpad callbacks never run game code mid-SDK-callback.
        private readonly Queue<GameAction> pending = new Queue<GameAction>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ handler stack

        public void Push(IGameInputHandler handler)
        {
            stack.Remove(handler);
            stack.Add(handler);
        }

        public void Pop(IGameInputHandler handler) => stack.Remove(handler);

        /// <summary>Inject an action (tests, on-screen debug buttons).</summary>
        public void Send(GameAction action) => pending.Enqueue(action);

        // ------------------------------------------------------------------ touchpad

        private void OnEnable()
        {
            if (Application.isEditor) return; // Editor uses the keyboard in Update
            SimpleTouchForLite touch;
            try { touch = SimpleTouchForLite.Instance; }
            catch (Exception e)
            {
                Debug.LogError($"{LogTag} Touchpad init failed, check RayNeoGeneralSettings touchConfigs: {e.Message}");
                return;
            }
            touch.OnSimpleTap.AddListener(HandleTap);
            touch.OnDoubleTap.AddListener(HandleDoubleTap);
            touch.OnSwipeRight.AddListener(HandleSwipeRight);
            touch.OnSwipeLeft.AddListener(HandleSwipeLeft);
            touch.OnSwipeUp.AddListener(HandleSwipeUp);
            touch.OnSwipeDown.AddListener(HandleSwipeDown);
            touchpadBound = true;
        }

        private void OnDisable()
        {
            if (!touchpadBound || !SimpleTouchForLite.SingletonExist) return;
            var touch = SimpleTouchForLite.Instance;
            touch.OnSimpleTap.RemoveListener(HandleTap);
            touch.OnDoubleTap.RemoveListener(HandleDoubleTap);
            touch.OnSwipeRight.RemoveListener(HandleSwipeRight);
            touch.OnSwipeLeft.RemoveListener(HandleSwipeLeft);
            touch.OnSwipeUp.RemoveListener(HandleSwipeUp);
            touch.OnSwipeDown.RemoveListener(HandleSwipeDown);
            touchpadBound = false;
        }

        private void HandleTap() => pending.Enqueue(GameAction.Confirm);
        private void HandleDoubleTap() => pending.Enqueue(GameAction.Back);
        private void HandleSwipeRight(Vector2 _) => EnqueueSwipe(SwipeMap.OnSwipeRightStep(invertHorizontalSwipe) < 0 ? GameAction.Prev : GameAction.Next);
        private void HandleSwipeLeft(Vector2 _) => EnqueueSwipe(SwipeMap.OnSwipeLeftStep(invertHorizontalSwipe) < 0 ? GameAction.Prev : GameAction.Next);
        private void HandleSwipeUp(Vector2 _) => EnqueueSwipe(GameAction.Up);
        private void HandleSwipeDown(Vector2 _) => EnqueueSwipe(GameAction.Down);

        // Trigger guard: per-step swipe events can arrive in bursts; keep at most one per swipeRepeatGuardSeconds.
        private void EnqueueSwipe(GameAction action)
        {
            if (Time.unscaledTime - lastSwipeTime < swipeRepeatGuardSeconds) return;
            lastSwipeTime = Time.unscaledTime;
            pending.Enqueue(action);
        }

        // ------------------------------------------------------------------ keyboard + dispatch

        private void Update()
        {
#if UNITY_EDITOR
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) pending.Enqueue(GameAction.Confirm);
                if (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) pending.Enqueue(GameAction.Back);
                if (kb.leftArrowKey.wasPressedThisFrame) pending.Enqueue(GameAction.Prev);
                if (kb.rightArrowKey.wasPressedThisFrame) pending.Enqueue(GameAction.Next);
                if (kb.upArrowKey.wasPressedThisFrame) pending.Enqueue(GameAction.Up);
                if (kb.downArrowKey.wasPressedThisFrame) pending.Enqueue(GameAction.Down);
            }
#else
            // Trigger: Android back key (legacy Input maps it to Escape; Active Input Handling = Both).
            if (Input.GetKeyDown(KeyCode.Escape)) pending.Enqueue(GameAction.Back);
#endif
            while (pending.Count > 0) Dispatch(pending.Dequeue());
        }

        private void Dispatch(GameAction action)
        {
            if (logActions) Debug.Log($"{LogTag} {action}");
            OnAnyAction?.Invoke(action);
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                if (i >= stack.Count) continue; // a handler popped others while handling
                if (stack[i].HandleAction(action)) return;
            }
        }
    }
}
