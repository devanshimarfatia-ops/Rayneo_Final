using System;

namespace RayneoIdle.Idle
{
    /// <summary>Why coins were added. HUD / logs can show different feedback per source.</summary>
    public enum CoinSource { Passive, Settle, Todo, Chest, Duplicate, Tutorial, Debug }

    /// <summary>Why a settle ran (D4).</summary>
    public enum SettleReason
    {
        /// <summary>App start after a real exit (close / system kill): HUD shows the settle toast.</summary>
        Launch,
        /// <summary>Back from a pause (startTodo, screen off, glasses off): credited silently, no toast.</summary>
        Resume,
        /// <summary>Gap between two ticks was too long to be a frame (hitch / missed pause event).</summary>
        Gap,
        /// <summary>Demo "Skip offline N hours".</summary>
        DemoSkip,
    }

    /// <summary>
    /// W1 IdleCore: coins, passive income, Coin Rate upgrade, chest search, rewards, events.
    /// Plain C#: no Unity, no RayNeo SDK. All time comes from <see cref="IGameClock"/> (D9).
    ///
    /// Time rules:
    /// - Tick() runs every frame while the app is live. It pays real elapsed time x demo multiplier.
    /// - Settle() runs at launch and on resume. It pays real elapsed time at 1x, capped at offlineCapHours.
    ///   Demo multiplier never applies to time the app was not running (9.2b).
    /// - Elapsed time below 0 (system clock set back) counts as 0.
    /// </summary>
    public class IdleCore
    {
        /// <summary>
        /// Trigger for an automatic settle inside Tick(): if more than this many real seconds passed since the
        /// last credit, the gap is treated as a pause (1x, capped) instead of a live frame (x multiplier).
        /// Protects demo mode from paying 1800x for a pause whose OnApplicationPause event was missed.
        /// </summary>
        public const double MaxLiveTickGapSeconds = 2.0;

        /// <summary>Floating-point tolerance for "search reached the discovery time" (about 4 ms of game time).</summary>
        private const double SearchEpsilonHours = 1e-6;

        public IdleConfig Config { get; }
        public IdleState State { get; private set; }
        public IGameClock Clock { get; }

        /// <summary>Demo tier index into Config.demoIncomeMultipliers (0 = 1x). Not saved in the real slot.</summary>
        public int DemoTier { get; private set; }

        public double DemoMultiplier => Config.demoIncomeMultipliers[DemoTier];
        public double AnimationSpeed => Config.demoAnimationSpeeds[DemoTier];

        public double CoinsPerHour => Config.CoinsPerHourAt(State.coinRateLevel);
        public bool CoinRateMaxed => State.coinRateLevel >= Config.coinRateMaxLevel;
        public double NextCoinRateCost => CoinRateMaxed ? double.PositiveInfinity : Config.CoinRateCost(State.coinRateLevel + 1);
        public bool CanBuyCoinRate => !CoinRateMaxed && State.coins >= NextCoinRateCost;

        public bool ChestSlotsFull => State.chestsWaiting >= Config.chestSlots;

        /// <summary>0..1 progress of the next chest. 0 while all slots are full or search is off.</summary>
        public double ChestProgress01 =>
            State.chestSearchActive && !ChestSlotsFull ? State.chestSearchHours / Config.chestDiscoveryHours : 0;

        // ---------- Events ----------
        /// <summary>(amount, source). Fired for every credit except the per-frame passive drip.</summary>
        public event Action<double, CoinSource> OnCoinsAdded;
        /// <summary>(amount). Fired on every spend.</summary>
        public event Action<double> OnCoinsSpent;
        /// <summary>(new level)</summary>
        public event Action<int> OnCoinRateUpgraded;
        /// <summary>(chests waiting after the find)</summary>
        public event Action<int> OnChestFound;
        /// <summary>(chests waiting after the open)</summary>
        public event Action<int> OnChestTaken;
        /// <summary>(reason, real seconds credited after the cap, coins gained, chests found)</summary>
        public event Action<SettleReason, double, double, int> OnSettled;
        /// <summary>(new tier)</summary>
        public event Action<int> OnDemoTierChanged;

        public IdleCore(IdleConfig config, IdleState state, IGameClock clock)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            State = state ?? new IdleState();
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Swap in a loaded / reset state (Reset, demo slot switch). Does not settle.</summary>
        public void ReplaceState(IdleState state)
        {
            State = state ?? new IdleState();
        }

        // =====================================================================
        // Time
        // =====================================================================

        /// <summary>Call every frame while the app is in the foreground.</summary>
        public void Tick()
        {
            long now = Clock.UtcNowMs;
            if (State.lastCreditUtcMs == 0) { State.lastCreditUtcMs = now; return; } // fresh save: start the clock

            double realSeconds = Math.Max(0, (now - State.lastCreditUtcMs) / 1000.0);
            if (realSeconds > MaxLiveTickGapSeconds)
            {
                Settle(SettleReason.Gap);
                return;
            }

            State.lastCreditUtcMs = now;
            if (realSeconds <= 0) return;

            double gameHours = realSeconds * DemoMultiplier / 3600.0;
            AdvanceGameHours(gameHours, CoinSource.Passive, out _, out _);
        }

        /// <summary>
        /// Credit real time since the last credit at 1x, capped at offlineCapHours.
        /// Call once at launch (reason Launch) and on every resume (reason Resume). Returns coins gained.
        /// </summary>
        public double Settle(SettleReason reason)
        {
            long now = Clock.UtcNowMs;
            if (State.lastCreditUtcMs == 0)
            {
                // Fresh save: nothing to pay, start the clock here.
                State.lastCreditUtcMs = now;
                OnSettled?.Invoke(reason, 0, 0, 0);
                return 0;
            }

            double realSeconds = Math.Max(0, (now - State.lastCreditUtcMs) / 1000.0); // clock set back = 0 (D9)
            State.lastCreditUtcMs = now;
            return SettleSeconds(reason, realSeconds);
        }

        /// <summary>Demo "Skip offline N hours": one normal settle of N hours, same cap. Does not touch the clock.</summary>
        public double DemoSkipHours(double hours) => SettleSeconds(SettleReason.DemoSkip, Math.Max(0, hours) * 3600.0);

        private double SettleSeconds(SettleReason reason, double realSeconds)
        {
            double cappedSeconds = Math.Min(realSeconds, Config.offlineCapHours * 3600.0);
            AdvanceGameHours(cappedSeconds / 3600.0, CoinSource.Settle, out double coins, out int chests);
            if (coins > 0) OnCoinsAdded?.Invoke(coins, CoinSource.Settle);
            OnSettled?.Invoke(reason, cappedSeconds, coins, chests);
            return coins;
        }

        /// <summary>Pays passive coins and advances chest search for the given game hours.</summary>
        private void AdvanceGameHours(double gameHours, CoinSource source, out double coins, out int chestsFound)
        {
            coins = CoinsPerHour * gameHours;
            State.coins += coins;
            State.totalCoinsEarned += coins;
            chestsFound = AdvanceChestSearch(gameHours);
        }

        // =====================================================================
        // Chests
        // =====================================================================

        /// <summary>Start the island search (called when onboarding ends). Search progress starts at 0.</summary>
        public void StartChestSearch()
        {
            if (State.chestSearchActive) return;
            State.chestSearchActive = true;
            State.chestSearchHours = 0;
        }

        /// <summary>
        /// Trigger: search progress reaches chestDiscoveryHours -> one chest found.
        /// Search pauses (progress held at 0) while all chestSlots are full; it restarts when a chest is taken.
        /// </summary>
        private int AdvanceChestSearch(double gameHours)
        {
            if (!State.chestSearchActive || ChestSlotsFull) return 0;

            int found = 0;
            State.chestSearchHours += gameHours;
            // Small tolerance: many tiny per-frame additions can land just below an exact boundary.
            while (State.chestSearchHours >= Config.chestDiscoveryHours - SearchEpsilonHours && !ChestSlotsFull)
            {
                State.chestSearchHours = Math.Max(0, State.chestSearchHours - Config.chestDiscoveryHours);
                State.chestsWaiting++;
                State.totalChestsFound++;
                found++;
                OnChestFound?.Invoke(State.chestsWaiting);
            }
            if (ChestSlotsFull) State.chestSearchHours = 0; // leftover search time is lost while full
            return found;
        }

        /// <summary>Remove one waiting chest so it can be opened. Rewards are rolled by the card/chest system (W4).</summary>
        public bool TryTakeChest()
        {
            if (State.chestsWaiting <= 0) return false;
            State.chestsWaiting--;
            OnChestTaken?.Invoke(State.chestsWaiting);
            return true;
        }

        // =====================================================================
        // Coins
        // =====================================================================

        public void AddCoins(double amount, CoinSource source)
        {
            if (amount <= 0) return;
            State.coins += amount;
            State.totalCoinsEarned += amount;
            OnCoinsAdded?.Invoke(amount, source);
        }

        public bool TrySpend(double amount)
        {
            if (amount < 0 || State.coins < amount) return false;
            State.coins -= amount;
            OnCoinsSpent?.Invoke(amount);
            return true;
        }

        /// <summary>Trigger: a To-do was marked finished in game (W7). Pays Config.todoReward.</summary>
        public double RewardTodo()
        {
            State.totalTodosRewarded++;
            AddCoins(Config.todoReward, CoinSource.Todo);
            return Config.todoReward;
        }

        public bool TryBuyCoinRate()
        {
            if (!CanBuyCoinRate) return false;
            TrySpend(NextCoinRateCost);
            State.coinRateLevel++;
            OnCoinRateUpgraded?.Invoke(State.coinRateLevel);
            return true;
        }

        // =====================================================================
        // Demo Mode (9.2b)
        // =====================================================================

        /// <summary>Set the demo tier. Credits the time so far at the old multiplier first.</summary>
        public void SetDemoTier(int tier)
        {
            tier = Math.Max(0, Math.Min(tier, Config.demoIncomeMultipliers.Length - 1));
            if (tier == DemoTier) return;
            Tick();
            DemoTier = tier;
            OnDemoTierChanged?.Invoke(DemoTier);
        }
    }
}
