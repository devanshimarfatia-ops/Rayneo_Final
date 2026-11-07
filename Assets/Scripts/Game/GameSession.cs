using System;
using RayneoIdle.Cards;
using RayneoIdle.Idle;
using RayneoIdle.Island;
using RayneoIdle.Save;
using UnityEngine;

namespace RayneoIdle.Game
{
    /// <summary>
    /// Owns the running game: IdleCore + CardSystem + the current save slot. One per scene.
    /// UI and gameplay read <see cref="Core"/> / <see cref="Cards"/> and subscribe to their events.
    ///
    /// Save / settle triggers (W2, D4):
    /// - Start: load slot; if a save existed, settle real time away (Launch) -> HUD shows the settle toast.
    /// - Every frame: Core.Tick() (live income x demo multiplier).
    /// - Every <see cref="autosaveSeconds"/>: save.
    /// - OnApplicationPause(true) (startTodo, screen off, glasses off): credit, save.
    /// - OnApplicationPause(false): settle the pause silently (Resume), no toast.
    /// - OnApplicationQuit: save. A system kill skips this; the last pause/autosave save is used instead.
    /// </summary>
    [DefaultExecutionOrder(-100)] // before UI scripts, so Core exists in their Awake/OnEnable
    public class GameSession : MonoBehaviour
    {
        private const string LogTag = "[GameSession]";

        public static GameSession Instance { get; private set; }

        [Tooltip("Economy numbers. Defaults = MVP column of Economy_Draft.xlsx.")]
        [SerializeField] private IdleConfig config = new IdleConfig();

        [Tooltip("Seconds between autosaves (W2: 30 s).")]
        [SerializeField] private float autosaveSeconds = 30f;

        [Tooltip("Fixed random seed for reproducible tests. 0 = random.")]
        [SerializeField] private int randomSeed;

        public IdleConfig Config => config;
        public IdleCore Core { get; private set; }
        public CardSystem Cards { get; private set; }
        public IslandMap Island { get; private set; }
        /// <summary>Character looks + pet shop (Profile tool).</summary>
        public RayneoIdle.Profile.ProfileSystem Profile { get; private set; }
        public SaveData Data { get; private set; }
        public int Slot { get; private set; } = SaveSystem.MainSlot;
        public bool IsDemoSlot => Slot == SaveSystem.DemoSlot;

        /// <summary>True when Start found no save (fresh install / after Reset): onboarding should run.</summary>
        public bool IsFreshSave { get; private set; }

        /// <summary>Result of the launch settle, kept so a HUD that starts later can still show the toast.</summary>
        public LaunchSettleInfo LastLaunchSettle { get; private set; }

        /// <summary>Fired after the launch settle (only when a save existed). HUD shows the settle toast.</summary>
        public event Action<LaunchSettleInfo> OnLaunchSettled;
        /// <summary>Fired after Reset or a slot switch replaced all state. UI should redraw everything.</summary>
        public event Action OnStateReplaced;

        private float autosaveTimer;
        private bool started;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{LogTag} second GameSession destroyed");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Data = SaveSystem.Load(Slot);
            IsFreshSave = Data == null;
            Data ??= new SaveData();

            IGameRandom random = randomSeed != 0 ? new SystemGameRandom(randomSeed) : new SystemGameRandom();
            Core = new IdleCore(config, Data.idle, new SystemGameClock());
            Cards = new CardSystem(Core, CardCatalog.CreateMvp(), Data.cards, random);
            Island = new IslandMap(Core, Cards, Data.island, random);
            Profile = new RayneoIdle.Profile.ProfileSystem(Core, Data.profile);
        }

        private void Start()
        {
            SettleLaunch();
            started = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            Core.Tick();

            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= autosaveSeconds) Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!started) return;
            if (paused)
            {
                Core.Tick();
                Save();
                Debug.Log($"{LogTag} paused, saved");
            }
            else
            {
                double coins = Core.Settle(SettleReason.Resume);
                Debug.Log($"{LogTag} resumed, +{coins:F4} coins (silent)");
            }
        }

        private void OnApplicationQuit()
        {
            if (!started) return;
            Core.Tick();
            Save();
        }

        // =====================================================================

        private void SettleLaunch()
        {
            bool hadSave = !IsFreshSave;
            double before = Core.State.coins;
            int chestsBefore = Core.State.chestsWaiting;
            long lastCredit = Core.State.lastCreditUtcMs;

            Core.Settle(SettleReason.Launch);

            LastLaunchSettle = new LaunchSettleInfo
            {
                hadSave = hadSave,
                coins = Core.State.coins - before,
                chestsFound = Core.State.chestsWaiting - chestsBefore,
                awaySeconds = lastCredit == 0 ? 0 : Math.Max(0, (Core.Clock.UtcNowMs - lastCredit) / 1000.0),
            };
            Debug.Log($"{LogTag} launch slot {Slot}, fresh={IsFreshSave}, away {LastLaunchSettle.awaySeconds:F0}s, " +
                      $"+{LastLaunchSettle.coins:F10} coins, +{LastLaunchSettle.chestsFound} chests");
            if (hadSave) OnLaunchSettled?.Invoke(LastLaunchSettle);
            Save();
        }

        public void Save()
        {
            autosaveTimer = 0;
            SaveSystem.Write(Slot, Data);
        }

        /// <summary>Reset (D10): wipes the current slot and starts fresh. Caller handles the two confirmations.</summary>
        public void ResetSlot()
        {
            SaveSystem.Delete(Slot);
            // D10: Reset also clears zone anchors, so the demo restarts from "set anchor" (step 3).
            var anchors = FindObjectOfType<Zones.ZoneAnchorStore>();
            if (anchors != null) anchors.ClearAll();
            LoadSlotState(new SaveData(), fresh: true);
            Debug.Log($"{LogTag} slot {Slot} reset");
        }

        /// <summary>Switch between the main and demo slot (W9). Saves the current one first.</summary>
        public void SwitchSlot(int slot)
        {
            if (slot == Slot) return;
            Core.Tick();
            Save();
            Slot = slot;
            var data = SaveSystem.Load(slot);
            LoadSlotState(data ?? new SaveData(), fresh: data == null);
            if (data != null) Core.Settle(SettleReason.Resume); // time away from this slot, silent
            Debug.Log($"{LogTag} switched to slot {slot}");
        }

        private void LoadSlotState(SaveData data, bool fresh)
        {
            Data = data;
            IsFreshSave = fresh;
            Core.ReplaceState(Data.idle);
            Cards.ReplaceState(Data.cards);
            Island.ReplaceState(Data.island);
            Profile.ReplaceState(Data.profile);
            Data.cards = Cards.State; // EnsureSize may have kept the same object; keep references in sync
            if (fresh) Core.Settle(SettleReason.Launch); // starts the clock
            Save();
            OnStateReplaced?.Invoke();
        }
    }

    public struct LaunchSettleInfo
    {
        public bool hadSave;
        /// <summary>Real seconds since the last credit (before the 24 h cap).</summary>
        public double awaySeconds;
        public double coins;
        public int chestsFound;
    }
}
