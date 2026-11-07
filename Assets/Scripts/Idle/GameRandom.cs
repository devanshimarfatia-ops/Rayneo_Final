using System;

namespace RayneoIdle.Idle
{
    /// <summary>Random source for rolls, injectable so tests can use a fixed seed.</summary>
    public interface IGameRandom
    {
        /// <summary>Uniform double in [0, 1).</summary>
        double Next01();

        /// <summary>Uniform int in [0, maxExclusive).</summary>
        int NextInt(int maxExclusive);
    }

    public sealed class SystemGameRandom : IGameRandom
    {
        private readonly Random random;

        public SystemGameRandom(int? seed = null) => random = seed.HasValue ? new Random(seed.Value) : new Random();

        public double Next01() => random.NextDouble();
        public int NextInt(int maxExclusive) => random.Next(maxExclusive);
    }

    public static class WeightedPick
    {
        /// <summary>Returns the index picked by weight. Weights may be any non-negative scale.</summary>
        public static int Pick(IGameRandom random, params double[] weights)
        {
            double total = 0;
            foreach (double w in weights) total += Math.Max(0, w);
            double roll = random.Next01() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= Math.Max(0, weights[i]);
                if (roll < 0) return i;
            }
            return weights.Length - 1;
        }
    }
}
