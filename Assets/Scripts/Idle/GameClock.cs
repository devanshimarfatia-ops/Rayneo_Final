using System;

namespace RayneoIdle.Idle
{
    /// <summary>
    /// The only time source for game logic (MVP decision D9). IdleCore and OfflineProgress never call
    /// DateTime.Now directly, so tests and the Editor can drive time by hand.
    /// Time is read as UTC to avoid time-zone and daylight-saving jumps.
    /// </summary>
    public interface IGameClock
    {
        /// <summary>Current UTC time in milliseconds since the Unix epoch.</summary>
        long UtcNowMs { get; }
    }

    /// <summary>Real clock: follows the glasses' system time.</summary>
    public sealed class SystemGameClock : IGameClock
    {
        public long UtcNowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>Test clock: time only moves when Advance / Set is called.</summary>
    public sealed class ManualGameClock : IGameClock
    {
        public long UtcNowMs { get; private set; }

        public ManualGameClock(long startUtcMs = 0) => UtcNowMs = startUtcMs;

        public void AdvanceSeconds(double seconds) => UtcNowMs += (long)Math.Round(seconds * 1000.0);

        /// <summary>Also allows moving backwards, to test the "clock set back" rule.</summary>
        public void Set(long utcMs) => UtcNowMs = utcMs;
    }
}
