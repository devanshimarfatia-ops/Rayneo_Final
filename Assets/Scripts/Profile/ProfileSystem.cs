using System;
using System.Collections.Generic;
using RayneoIdle.Idle;

namespace RayneoIdle.Profile
{
    public enum CosmeticKind { Outfit, Color, Pet }

    /// <summary>One cosmetic: outfit, clothing color or pet. Price 0 = free (always unlocked).</summary>
    public class Cosmetic
    {
        public CosmeticKind kind;
        public string id;
        public string name;
        public double price;

        public Cosmetic(CosmeticKind kind, string id, string name, double price)
        {
            this.kind = kind;
            this.id = id;
            this.name = name;
            this.price = price;
        }

        public string Key => $"{kind}:{id}";
    }

    /// <summary>Saved profile: what is equipped and what was bought.</summary>
    [Serializable]
    public class ProfileState
    {
        public string outfit = "explorer";
        public string color = "cyan";
        public string pet = "none";
        /// <summary>Bought items as "Kind:id" keys (free items are never stored).</summary>
        public List<string> unlocked = new List<string>();
    }

    /// <summary>
    /// Profile shop (device test 4, Yaxin): character looks (outfit styles incl. male / female clothes, clothing
    /// colors) and pets. Shop skins only, no stat effect (GDD: island themes, pets and skin colors are shop
    /// skins). Some items are free, the rest are bought with coins. Prices are placeholders until the economy
    /// sheet gets a cosmetics section. Plain C#.
    /// </summary>
    public class ProfileSystem
    {
        public static readonly Cosmetic[] Outfits =
        {
            new Cosmetic(CosmeticKind.Outfit, "explorer", "Explorer", 0),  // hat + trousers
            new Cosmetic(CosmeticKind.Outfit, "dress", "Long dress", 0),   // long hair + dress
            new Cosmetic(CosmeticKind.Outfit, "overalls", "Overalls", 1000),
            new Cosmetic(CosmeticKind.Outfit, "cloak", "Hooded cloak", 2500),
        };

        public static readonly Cosmetic[] Colors =
        {
            new Cosmetic(CosmeticKind.Color, "cyan", "Cyan", 0),
            new Cosmetic(CosmeticKind.Color, "green", "Green", 0),
            new Cosmetic(CosmeticKind.Color, "gold", "Gold", 500),
            new Cosmetic(CosmeticKind.Color, "silver", "Silver", 800),
            new Cosmetic(CosmeticKind.Color, "purple", "Purple", 1200),
            new Cosmetic(CosmeticKind.Color, "pink", "Pink", 2000),
        };

        public static readonly Cosmetic[] Pets =
        {
            new Cosmetic(CosmeticKind.Pet, "none", "No pet", 0),
            new Cosmetic(CosmeticKind.Pet, "cat", "Cat", 1500),
            new Cosmetic(CosmeticKind.Pet, "dog", "Dog", 1500),
            new Cosmetic(CosmeticKind.Pet, "bird", "Bird", 3000),
            new Cosmetic(CosmeticKind.Pet, "turtle", "Turtle", 5000),
        };

        private readonly IdleCore core;
        public ProfileState State { get; private set; }

        /// <summary>Equipped look or pet changed (map explorer and portraits redraw).</summary>
        public event Action OnChanged;

        public ProfileSystem(IdleCore core, ProfileState state)
        {
            this.core = core ?? throw new ArgumentNullException(nameof(core));
            ReplaceState(state);
        }

        public void ReplaceState(ProfileState state)
        {
            State = state ?? new ProfileState();
            State.unlocked ??= new List<string>();
            OnChanged?.Invoke();
        }

        public static Cosmetic[] List(CosmeticKind kind) =>
            kind == CosmeticKind.Outfit ? Outfits : kind == CosmeticKind.Color ? Colors : Pets;

        public static Cosmetic Find(CosmeticKind kind, string id) => Array.Find(List(kind), c => c.id == id) ?? List(kind)[0];

        public string Equipped(CosmeticKind kind) =>
            kind == CosmeticKind.Outfit ? State.outfit : kind == CosmeticKind.Color ? State.color : State.pet;

        public bool IsUnlocked(Cosmetic c) => c.price <= 0 || State.unlocked.Contains(c.Key);

        /// <summary>Equip an unlocked item. False if it is still locked.</summary>
        public bool Equip(Cosmetic c)
        {
            if (!IsUnlocked(c)) return false;
            switch (c.kind)
            {
                case CosmeticKind.Outfit: State.outfit = c.id; break;
                case CosmeticKind.Color: State.color = c.id; break;
                default: State.pet = c.id; break;
            }
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Buy (if needed) and equip. False when there are not enough coins.</summary>
        public bool BuyAndEquip(Cosmetic c)
        {
            if (!IsUnlocked(c))
            {
                if (!core.TrySpend(c.price)) return false;
                State.unlocked.Add(c.Key);
            }
            return Equip(c);
        }

        public int UnlockedCount(CosmeticKind kind)
        {
            int n = 0;
            foreach (var c in List(kind)) if (IsUnlocked(c)) n++;
            return n;
        }
    }
}
