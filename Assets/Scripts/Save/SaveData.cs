using System;
using RayneoIdle.Cards;
using RayneoIdle.Idle;

namespace RayneoIdle.Save
{
    /// <summary>
    /// One save slot (D10: MVP uses slot 0, Demo Mode uses its own slot). Saved as JSON with JsonUtility.
    /// Bump <see cref="CurrentVersion"/> when a field changes meaning, and migrate in SaveSystem.Load.
    /// Zone tags are not here: they live in the system To-do text (D3). Anchors have their own store.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        /// <summary>UTC ms of the last write (debug / QA only; income uses idle.lastCreditUtcMs).</summary>
        public long savedUtcMs;

        public IdleState idle = new IdleState();
        public CardCollectionState cards = new CardCollectionState();
        public Island.IslandState island = new Island.IslandState();

        /// <summary>
        /// System To-do timestamps already paid (W7). A todo pays once, even if it is set unfinished
        /// and finished again, or finished in the system app and seen again here.
        /// </summary>
        public System.Collections.Generic.List<long> rewardedTodoTimestamps = new System.Collections.Generic.List<long>();

        /// <summary>Anchor slot names (slot 1..3 = TodoZone Work / Bedroom / Kitchen). Used for the todo prefix (D3).</summary>
        public string[] zoneNames = { "Work", "Bedroom", "Kitchen" };

        /// <summary>Character looks and pet: equipped + bought (Profile tool).</summary>
        public Profile.ProfileState profile = new Profile.ProfileState();

        // ---------- Onboarding (W11) ----------
        public bool onboardingDone;
        /// <summary>Last onboarding step reached, so a kill mid-onboarding resumes there.</summary>
        public int onboardingStep;
        public bool tutorialChestOpened;
    }
}
