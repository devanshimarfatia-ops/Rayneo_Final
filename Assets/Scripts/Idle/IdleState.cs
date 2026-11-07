using System;

namespace RayneoIdle.Idle
{
    /// <summary>
    /// Everything IdleCore needs to resume. Plain public fields so JsonUtility can save it (W2).
    /// Coins are a double (D4): fractions are kept so a 10 s away settle is not lost.
    /// </summary>
    [Serializable]
    public class IdleState
    {
        public double coins;

        /// <summary>0 = not upgraded. Coins per hour = base + bonus * level.</summary>
        public int coinRateLevel;

        // ---------- Island search ----------
        /// <summary>False until onboarding ends; then the character searches for chests.</summary>
        public bool chestSearchActive;

        /// <summary>Chests found and waiting to be opened (0..chestSlots).</summary>
        public int chestsWaiting;

        /// <summary>Game hours searched toward the next chest (0..chestDiscoveryHours).</summary>
        public double chestSearchHours;

        // ---------- Time ----------
        /// <summary>UTC ms of the last time income was credited. 0 = never (fresh save).</summary>
        public long lastCreditUtcMs;

        // ---------- Lifetime stats (QA / playtest logs) ----------
        public double totalCoinsEarned;
        public int totalChestsFound;
        public int totalTodosRewarded;
    }
}
