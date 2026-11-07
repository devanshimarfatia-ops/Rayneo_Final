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

        public static CardCatalog CreateMvp() => new CardCatalog(new[]
        {
            new CardDef("common_1", "Common 1", CardRarity.Common),
            new CardDef("common_2", "Common 2", CardRarity.Common),
            new CardDef("common_3", "Common 3", CardRarity.Common),
            new CardDef("treasured_1", "Treasured 1", CardRarity.Treasured),
            new CardDef("treasured_2", "Treasured 2", CardRarity.Treasured),
            new CardDef("treasured_3", "Treasured 3", CardRarity.Treasured),
            new CardDef("special_1", "Special 1", CardRarity.Special),
            new CardDef("special_2", "Special 2", CardRarity.Special),
            new CardDef("special_3", "Special 3", CardRarity.Special),
        });

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
