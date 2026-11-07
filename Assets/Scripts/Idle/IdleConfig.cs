using System;

namespace RayneoIdle.Idle
{
    /// <summary>
    /// Economy numbers for the idle loop. Defaults = MVP column of the economy workbook
    /// (Economy_Draft.xlsx, sheet Params, "MVP value"). Change numbers there first, then copy here.
    /// Plain serializable class so it can be edited on a MonoBehaviour / ScriptableObject in the Inspector.
    /// </summary>
    [Serializable]
    public class IdleConfig
    {
        // ---------- Passive income ----------
        /// <summary>Coins per game hour at Coin Rate level 0 (economy anchor: 1 h = 100 coins).</summary>
        public double baseCoinsPerHour = 100;

        /// <summary>Real time away that is still paid, in hours (offline and pause settle both use it).</summary>
        public double offlineCapHours = 24;

        // ---------- Coin Rate upgrade (the only MVP sink) ----------
        /// <summary>Extra coins per hour for each Coin Rate level.</summary>
        public double coinRateBonusPerLevel = 5;

        public int coinRateMaxLevel = 20;

        /// <summary>Cost of level L = round(baseCost * growth^(L-1), to the nearest coinRateCostRounding).</summary>
        public double coinRateBaseCost = 250;
        public double coinRateCostGrowth = 1.21;
        public double coinRateCostRounding = 10;

        // ---------- To-do ----------
        /// <summary>Coins for finishing one To-do.</summary>
        public double todoReward = 320;

        // ---------- Chests (island search) ----------
        /// <summary>Max chests waiting on the island (one per chest area). Search stops when all are full.</summary>
        public int chestSlots = 3;

        /// <summary>Game hours of searching to find one chest.</summary>
        public double chestDiscoveryHours = 5;

        /// <summary>Rewards rolled per chest.</summary>
        public int rewardsPerChest = 3;

        /// <summary>Roll weights per reward slot: coins / one card / blind pack (percent, any scale).</summary>
        public double slotWeightCoins = 73;
        public double slotWeightCard = 22;
        public double slotWeightPack = 5;

        /// <summary>Coins for a "coins" reward slot.</summary>
        public double chestCoinReward = 100;

        // ---------- Tutorial chest (onboarding only, fixed contents) ----------
        public double tutorialChestCoins = 500;

        /// <summary>
        /// Real seconds the tutorial chest search takes (fixed onboarding delay, not the 5 h discovery time,
        /// not affected by Demo Mode). 10 s keeps demo steps 7-8 within about one minute.
        /// </summary>
        public double tutorialChestSearchSeconds = 10;

        // ---------- Cards ----------
        /// <summary>Cards in one blind pack.</summary>
        public int packSize = 5;

        /// <summary>
        /// Shop price of one blind pack (full-design value; the MVP draft had no shop, added after device test 4).
        /// Rule "packs never pay for packs": 5 Special duplicates (1,250) stay below this.
        /// </summary>
        public double packPrice = 3000;

        /// <summary>Rarity weights: Common / Treasured / Special.</summary>
        public double rarityWeightCommon = 60;
        public double rarityWeightTreasured = 30;
        public double rarityWeightSpecial = 10;

        /// <summary>After this many draws without a new Special, the next draw is a Special the player lacks.</summary>
        public int specialPityDraws = 25;

        /// <summary>Coins for a duplicate card: Common / Treasured / Special.</summary>
        public double duplicateCoinsCommon = 50;
        public double duplicateCoinsTreasured = 100;
        public double duplicateCoinsSpecial = 250;

        // ---------- Demo Mode (9.2b) ----------
        /// <summary>Income multipliers per demo tier (passive coins + chest search only, never time itself).</summary>
        public double[] demoIncomeMultipliers = { 1, 60, 1800 };

        /// <summary>Character animation speed per demo tier (capped so it stays readable).</summary>
        public double[] demoAnimationSpeeds = { 1, 2, 3 };

        // ---------- Derived values ----------
        public double CoinsPerHourAt(int coinRateLevel) => baseCoinsPerHour + coinRateBonusPerLevel * coinRateLevel;

        /// <summary>Cost to buy Coin Rate level <paramref name="level"/> (1-based). Matches ROUND(250*1.21^(L-1), -1).</summary>
        public double CoinRateCost(int level)
        {
            double raw = coinRateBaseCost * Math.Pow(coinRateCostGrowth, level - 1);
            return Math.Round(raw / coinRateCostRounding, MidpointRounding.AwayFromZero) * coinRateCostRounding;
        }
    }
}
