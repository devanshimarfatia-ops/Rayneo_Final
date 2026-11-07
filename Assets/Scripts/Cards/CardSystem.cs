using System;
using System.Collections.Generic;
using RayneoIdle.Idle;

namespace RayneoIdle.Cards
{
    /// <summary>
    /// W4: card draws, blind packs, chest rewards, duplicate conversion. Plain C#.
    /// Coins go through IdleCore so every credit fires the same events.
    ///
    /// Rules (economy draft section 8, MVP values in IdleConfig):
    /// - Chest: rewardsPerChest slots, each rolled coins / card / pack by slot weights.
    /// - Card draw: rarity by rarity weights, then a uniform card of that rarity.
    /// - Special pity: after specialPityDraws draws without a NEW Special, the next draw is a Special
    ///   the player does not own yet. No pity once all Specials are owned.
    /// - Duplicate: converted to coins by rarity (duplicateCoins*); the copy is still counted.
    /// - Pack: packSize draws. Packs from chests are stored and opened later with OpenPack().
    /// - Tutorial chest: fixed tutorialChestCoins + 1 random Common + 1 pack.
    /// </summary>
    public class CardSystem
    {
        private readonly IdleCore core;
        private readonly IGameRandom random;

        public CardCatalog Catalog { get; }
        public CardCollectionState State { get; private set; }
        private IdleConfig Config => core.Config;

        /// <summary>Fired once per card drawn (chest card, pack card, tutorial card).</summary>
        public event Action<CardDrawResult> OnCardDrawn;
        /// <summary>Fired after a chest is opened and all its rewards are applied.</summary>
        public event Action<ChestOpenResult> OnChestOpened;
        /// <summary>(cards drawn) after a pack is opened.</summary>
        public event Action<List<CardDrawResult>> OnPackOpened;
        /// <summary>(unopened packs) whenever the pack count changes.</summary>
        public event Action<int> OnPacksChanged;

        public CardSystem(IdleCore core, CardCatalog catalog, CardCollectionState state, IGameRandom random)
        {
            this.core = core ?? throw new ArgumentNullException(nameof(core));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            ReplaceState(state);
        }

        public void ReplaceState(CardCollectionState state)
        {
            State = state ?? new CardCollectionState();
            State.EnsureSize(Catalog.Count);
        }

        // =====================================================================
        // Chests
        // =====================================================================

        /// <summary>Takes one waiting chest from IdleCore and opens it. Null if no chest is waiting.</summary>
        public ChestOpenResult OpenChest()
        {
            if (!core.TryTakeChest()) return null;

            var result = new ChestOpenResult();
            for (int i = 0; i < Config.rewardsPerChest; i++)
            {
                var kind = (ChestSlotKind)WeightedPick.Pick(random,
                    Config.slotWeightCoins, Config.slotWeightCard, Config.slotWeightPack);
                result.slots.Add(ApplySlot(kind));
            }
            OnChestOpened?.Invoke(result);
            return result;
        }

        /// <summary>Onboarding chest (GDD 2.1): fixed contents, does not use an island chest slot.</summary>
        public ChestOpenResult OpenTutorialChest()
        {
            var result = new ChestOpenResult { isTutorial = true };

            var coinSlot = new ChestSlotReward { kind = ChestSlotKind.Coins, coins = Config.tutorialChestCoins };
            core.AddCoins(coinSlot.coins, CoinSource.Tutorial);
            result.slots.Add(coinSlot);

            var cardSlot = new ChestSlotReward { kind = ChestSlotKind.Card };
            cardSlot.cards.Add(DrawOfRarity(CardRarity.Common, preferUnowned: true));
            result.slots.Add(cardSlot);

            result.slots.Add(ApplySlot(ChestSlotKind.Pack));

            OnChestOpened?.Invoke(result);
            return result;
        }

        private ChestSlotReward ApplySlot(ChestSlotKind kind)
        {
            var slot = new ChestSlotReward { kind = kind };
            switch (kind)
            {
                case ChestSlotKind.Coins:
                    slot.coins = Config.chestCoinReward;
                    core.AddCoins(slot.coins, CoinSource.Chest);
                    break;
                case ChestSlotKind.Card:
                    slot.cards.Add(DrawCard());
                    break;
                case ChestSlotKind.Pack:
                    State.unopenedPacks++;
                    OnPacksChanged?.Invoke(State.unopenedPacks);
                    break;
            }
            return slot;
        }

        // =====================================================================
        // Packs
        // =====================================================================

        /// <summary>Opens one stored pack: packSize draws. Null if there is no pack.</summary>
        public List<CardDrawResult> OpenPack()
        {
            if (State.unopenedPacks <= 0) return null;
            State.unopenedPacks--;
            OnPacksChanged?.Invoke(State.unopenedPacks);

            var cards = new List<CardDrawResult>();
            for (int i = 0; i < Config.packSize; i++) cards.Add(DrawCard());
            OnPackOpened?.Invoke(cards);
            return cards;
        }

        // =====================================================================
        // Draws
        // =====================================================================

        /// <summary>One normal draw: pity check, rarity roll, uniform card.</summary>
        public CardDrawResult DrawCard()
        {
            // Trigger: specialPityDraws draws in a row without a NEW Special, and a Special is still missing.
            var missingSpecials = Unowned(CardRarity.Special);
            if (State.drawsSinceNewSpecial >= Config.specialPityDraws && missingSpecials.Count > 0)
            {
                int forced = missingSpecials[random.NextInt(missingSpecials.Count)];
                return Grant(forced, fromPity: true);
            }

            var rarity = (CardRarity)WeightedPick.Pick(random,
                Config.rarityWeightCommon, Config.rarityWeightTreasured, Config.rarityWeightSpecial);
            return DrawOfRarity(rarity, preferUnowned: false);
        }

        private CardDrawResult DrawOfRarity(CardRarity rarity, bool preferUnowned)
        {
            var pool = preferUnowned ? Unowned(rarity) : null;
            if (pool == null || pool.Count == 0) pool = Catalog.IndicesOf(rarity);
            int index = pool[random.NextInt(pool.Count)];
            return Grant(index, fromPity: false);
        }

        private CardDrawResult Grant(int index, bool fromPity)
        {
            var def = Catalog.Cards[index];
            bool isNew = State.ownedCounts[index] == 0;

            State.ownedCounts[index]++;
            State.totalDraws++;

            if (def.rarity == CardRarity.Special && isNew) State.drawsSinceNewSpecial = 0;
            else State.drawsSinceNewSpecial++;

            double dupCoins = 0;
            if (isNew)
            {
                State.isNew[index] = true;
            }
            else
            {
                dupCoins = DuplicateCoins(def.rarity);
                core.AddCoins(dupCoins, CoinSource.Duplicate);
            }

            var result = new CardDrawResult
            {
                cardIndex = index, card = def, isNew = isNew, duplicateCoins = dupCoins, fromPity = fromPity,
            };
            OnCardDrawn?.Invoke(result);
            return result;
        }

        public double DuplicateCoins(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return Config.duplicateCoinsCommon;
                case CardRarity.Treasured: return Config.duplicateCoinsTreasured;
                default: return Config.duplicateCoinsSpecial;
            }
        }

        private List<int> Unowned(CardRarity rarity)
        {
            var list = new List<int>();
            foreach (int i in Catalog.IndicesOf(rarity)) if (!State.Owns(i)) list.Add(i);
            return list;
        }

        /// <summary>Clears the NEW badge once the card was viewed in the collection.</summary>
        public void MarkSeen(int index)
        {
            if (index >= 0 && index < State.isNew.Length) State.isNew[index] = false;
        }
    }
}
