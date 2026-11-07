using System;
using System.Collections.Generic;

namespace RayneoIdle.Cards
{
    public enum CardRarity { Common = 0, Treasured = 1, Special = 2 }

    /// <summary>Static card data. Artwork is looked up by <see cref="id"/> on the Unity side.</summary>
    [Serializable]
    public class CardDef
    {
        public string id;
        public string displayName;
        public CardRarity rarity;

        public CardDef(string id, string displayName, CardRarity rarity)
        {
            this.id = id;
            this.displayName = displayName;
            this.rarity = rarity;
        }
    }

    /// <summary>
    /// The card pool. MVP (D6): 9 cards, 3 per rarity, one 3x3 gallery row per rarity.
    /// Index in <see cref="Cards"/> is the save index, so only ever append new cards.
    /// Names are placeholders until the artwork-to-rarity mapping is decided.
    /// </summary>
    public class CardCatalog
    {
        public IReadOnlyList<CardDef> Cards { get; }

        public CardCatalog(IReadOnlyList<CardDef> cards) => Cards = cards;

        /// <summary>Cards per rarity (Yaxin 2026-11-07: 18 per rarity, 54 in total; was 3 per rarity in D6).</summary>
        public const int CardsPerRarity = 18;

        /// <summary>
        /// The card pool: 18 Common / 18 Treasured / 18 Special. The first 9 entries are the original D6 cards
        /// (save indices 0..8 must not move); cards 4..18 of each rarity are appended after them.
        /// The economy (rarity weights, pity, collection pace) was tuned for 9 cards and still needs a redo.
        /// </summary>
        public static CardCatalog CreateMvp()
        {
            var list = new List<CardDef>();
            var rarities = new[] { CardRarity.Common, CardRarity.Treasured, CardRarity.Special };
            foreach (var r in rarities)
                for (int n = 1; n <= 3; n++) list.Add(Make(r, n));
            foreach (var r in rarities)
                for (int n = 4; n <= CardsPerRarity; n++) list.Add(Make(r, n));
            return new CardCatalog(list);
        }

        private static CardDef Make(CardRarity r, int n)
        {
            string name = r == CardRarity.Common ? "Common" : r == CardRarity.Treasured ? "Treasured" : "Special";
            return new CardDef($"{name.ToLowerInvariant()}_{n}", $"{name} {n}", r);
        }

        /// <summary>0-based position of a card inside its rarity (display order, art variant).</summary>
        public int NumberInRarity(int index)
        {
            var list = IndicesOf(Cards[index].rarity);
            return list.IndexOf(index);
        }

        public int Count => Cards.Count;

        public List<int> IndicesOf(CardRarity rarity)
        {
            var list = new List<int>();
            for (int i = 0; i < Cards.Count; i++) if (Cards[i].rarity == rarity) list.Add(i);
            return list;
        }
    }

    /// <summary>Saved collection progress. Arrays are indexed like <see cref="CardCatalog.Cards"/> (JsonUtility has no Dictionary).</summary>
    [Serializable]
    public class CardCollectionState
    {
        /// <summary>Copies owned per card (duplicates are converted to coins but still counted here).</summary>
        public int[] ownedCounts = new int[0];

        /// <summary>True until the player has looked at the card in the collection (NEW badge).</summary>
        public bool[] isNew = new bool[0];

        /// <summary>Draws since the last NEW Special. Pity fires when it reaches IdleConfig.specialPityDraws.</summary>
        public int drawsSinceNewSpecial;

        public int totalDraws;

        /// <summary>Blind packs won from chests and not opened yet (opened one by one, demo steps 9-10).</summary>
        public int unopenedPacks;

        /// <summary>Grows the arrays when the catalog has more cards than the save (old save, new cards).</summary>
        public void EnsureSize(int count)
        {
            if (ownedCounts.Length < count) Array.Resize(ref ownedCounts, count);
            if (isNew.Length < count) Array.Resize(ref isNew, count);
        }

        public bool Owns(int index) => index < ownedCounts.Length && ownedCounts[index] > 0;

        public int OwnedDistinct()
        {
            int n = 0;
            foreach (int c in ownedCounts) if (c > 0) n++;
            return n;
        }
    }

    /// <summary>One card coming out of a chest or pack.</summary>
    public struct CardDrawResult
    {
        public int cardIndex;
        public CardDef card;
        /// <summary>First copy: shows NEW, fills the gallery slot.</summary>
        public bool isNew;
        /// <summary>Coins paid for a duplicate (0 when new).</summary>
        public double duplicateCoins;
        /// <summary>This draw was forced by the Special pity rule.</summary>
        public bool fromPity;
    }

    public enum ChestSlotKind { Coins = 0, Card = 1, Pack = 2 }

    /// <summary>One of the rewards inside a chest.</summary>
    public class ChestSlotReward
    {
        public ChestSlotKind kind;
        public double coins;
        /// <summary>1 card for Card. Empty for Coins and Pack (a pack goes to unopenedPacks).</summary>
        public List<CardDrawResult> cards = new List<CardDrawResult>();
    }

    public class ChestOpenResult
    {
        public bool isTutorial;
        public List<ChestSlotReward> slots = new List<ChestSlotReward>();

        /// <summary>Coin slots + duplicate coins, everything this chest paid.</summary>
        public double TotalCoins
        {
            get
            {
                double sum = 0;
                foreach (var s in slots)
                {
                    sum += s.coins;
                    foreach (var c in s.cards) sum += c.duplicateCoins;
                }
                return sum;
            }
        }
    }
}
