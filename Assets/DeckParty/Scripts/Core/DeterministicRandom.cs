namespace DeckParty.Core
{
    /// <summary>
    /// SplitMix64 generator operating on caller-owned state. All randomness is stored inside the
    /// PlayerState, so a saved or networked game resumes with exactly the same future draws.
    /// </summary>
    public static class DeterministicRandom
    {
        public static ulong Seed(uint seed)
        {
            ulong state = seed;
            state = state * 0x9E3779B97F4A7C15UL + 0xD1B54A32D192ED03UL;
            return state;
        }

        public static ulong Next(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Uniform integer in [0, maxExclusive).</summary>
        public static int Range(ref ulong state, int maxExclusive)
        {
            if (maxExclusive <= 1) return 0;
            return (int)(Next(ref state) % (ulong)maxExclusive);
        }
    }
}
