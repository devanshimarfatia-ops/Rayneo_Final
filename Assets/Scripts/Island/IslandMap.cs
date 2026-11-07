using System;
using System.Collections.Generic;
using RayneoIdle.Cards;
using RayneoIdle.Idle;

namespace RayneoIdle.Island
{
    /// <summary>A chest area on the treasure map, in normalized map coordinates (0..1, origin bottom-left).</summary>
    [Serializable]
    public struct MapArea
    {
        public float x, y, width, height;
        public MapArea(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
    }

    /// <summary>Saved state of one chest area.</summary>
    [Serializable]
    public class ChestSpot
    {
        public bool hasChest;
        public bool isTutorial;
        /// <summary>Chest position in normalized map coordinates (random inside the area when placed).</summary>
        public float x, y;
    }

    [Serializable]
    public class IslandState
    {
        public List<ChestSpot> spots = new List<ChestSpot>();
        /// <summary>Area the character is searching now (dashed "?" outline). -1 = none (all full / search off).</summary>
        public int searchArea = -1;
    }

    /// <summary>
    /// W3 island logic (the treasure map panel). Plain C#: visuals read this and animate.
    /// Rules (GDD full-game decisions 2026-11-01):
    /// - One chest area per chest slot; a chest spawns at a random spot inside its area.
    /// - While searching, only the searched area shows (dashed outline + "?"). When IdleCore finds a chest
    ///   it is placed in that area, and the search moves to another free area (random).
    /// - Opening a chest frees its area. IdleCore.chestsWaiting stays the source of truth; Sync() repairs
    ///   the spots after a load / offline settle.
    /// - Tutorial chest: placed by onboarding in a free area, opened with fixed contents, not an island slot.
    /// </summary>
    public class IslandMap
    {
        /// <summary>Keep chests this far inside their area edge (fraction of the area size).</summary>
        public const float SpotMargin = 0.15f;

        private readonly IdleCore core;
        private readonly CardSystem cards;
        private readonly IGameRandom random;

        public IReadOnlyList<MapArea> Areas { get; }
        public IslandState State { get; private set; }

        /// <summary>(area index) a chest appeared (found by search, offline settle or tutorial).</summary>
        public event Action<int> OnChestPlaced;
        /// <summary>(area index, result) a chest was opened.</summary>
        public event Action<int, ChestOpenResult> OnChestOpened;
        /// <summary>(area index or -1) the character moved to a new search area.</summary>
        public event Action<int> OnSearchAreaChanged;

        /// <summary>Default layout of the 3 chest areas: left, right, bottom-center. Tune with the map art.</summary>
        public static MapArea[] DefaultAreas() => new[]
        {
            new MapArea(0.08f, 0.50f, 0.30f, 0.38f),
            new MapArea(0.62f, 0.50f, 0.30f, 0.38f),
            new MapArea(0.35f, 0.08f, 0.30f, 0.36f),
        };

        public IslandMap(IdleCore core, CardSystem cards, IslandState state, IGameRandom random, IReadOnlyList<MapArea> areas = null)
        {
            this.core = core ?? throw new ArgumentNullException(nameof(core));
            this.cards = cards ?? throw new ArgumentNullException(nameof(cards));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            Areas = areas ?? DefaultAreas();
            core.OnChestFound += _ => Sync();
            ReplaceState(state);
        }

        public void ReplaceState(IslandState state)
        {
            State = state ?? new IslandState();
            while (State.spots.Count < Areas.Count) State.spots.Add(new ChestSpot());
            Sync();
        }

        public int NormalChestCount()
        {
            int n = 0;
            foreach (var s in State.spots) if (s.hasChest && !s.isTutorial) n++;
            return n;
        }

        /// <summary>
        /// Makes the spots match IdleCore.chestsWaiting: places missing chests (search area first),
        /// removes extras, then picks the next search area. Safe to call any time.
        /// </summary>
        public void Sync()
        {
            int target = core.State.chestsWaiting;
            while (NormalChestCount() < target)
            {
                int area = State.searchArea >= 0 && !State.spots[State.searchArea].hasChest ? State.searchArea : RandomFreeArea();
                if (area < 0) break; // more chests than areas: cannot show them (should not happen, slots = areas)
                Place(area, tutorial: false);
                State.searchArea = -1;
            }
            while (NormalChestCount() > target)
            {
                for (int i = State.spots.Count - 1; i >= 0; i--)
                    if (State.spots[i].hasChest && !State.spots[i].isTutorial) { State.spots[i].hasChest = false; break; }
            }
            UpdateSearchArea();
        }

        private void UpdateSearchArea()
        {
            int before = State.searchArea;
            bool searching = core.State.chestSearchActive && !core.ChestSlotsFull;
            if (!searching) State.searchArea = -1;
            else if (State.searchArea < 0 || State.spots[State.searchArea].hasChest) State.searchArea = RandomFreeArea();
            if (State.searchArea != before) OnSearchAreaChanged?.Invoke(State.searchArea);
        }

        /// <summary>A random area without a chest, or -1 (onboarding uses it for the tutorial search).</summary>
        public int PickFreeArea() => RandomFreeArea();

        private int RandomFreeArea()
        {
            var free = new List<int>();
            for (int i = 0; i < State.spots.Count; i++) if (!State.spots[i].hasChest) free.Add(i);
            return free.Count == 0 ? -1 : free[random.NextInt(free.Count)];
        }

        private void Place(int area, bool tutorial)
        {
            var a = Areas[area];
            var spot = State.spots[area];
            spot.hasChest = true;
            spot.isTutorial = tutorial;
            spot.x = a.x + a.width * (SpotMargin + (float)random.Next01() * (1 - 2 * SpotMargin));
            spot.y = a.y + a.height * (SpotMargin + (float)random.Next01() * (1 - 2 * SpotMargin));
            OnChestPlaced?.Invoke(area);
        }

        /// <summary>Onboarding: drop the tutorial chest into a free area. Returns the area or -1.</summary>
        public int PlaceTutorialChest(int preferredArea = -1)
        {
            foreach (var s in State.spots) if (s.hasChest && s.isTutorial) return State.spots.IndexOf(s);
            int area = preferredArea >= 0 && preferredArea < State.spots.Count && !State.spots[preferredArea].hasChest ? preferredArea
                     : State.searchArea >= 0 ? State.searchArea : RandomFreeArea();
            if (area < 0) return -1;
            Place(area, tutorial: true);
            UpdateSearchArea();
            return area;
        }

        /// <summary>Opens the chest at an area (focus + tap). Null if there is no chest there.</summary>
        public ChestOpenResult OpenAt(int area)
        {
            if (area < 0 || area >= State.spots.Count || !State.spots[area].hasChest) return null;
            var spot = State.spots[area];

            ChestOpenResult result = spot.isTutorial ? cards.OpenTutorialChest() : cards.OpenChest();
            if (result == null) return null;

            spot.hasChest = false;
            spot.isTutorial = false;
            OnChestOpened?.Invoke(area, result);
            UpdateSearchArea();
            return result;
        }

        /// <summary>Call after IdleCore.StartChestSearch() so the search area shows at once.</summary>
        public void OnSearchStarted() => UpdateSearchArea();
    }
}
