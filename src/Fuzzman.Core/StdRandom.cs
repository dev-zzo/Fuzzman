using System;

namespace Fuzzman.Core
{
    /// <summary>
    /// Dumb implementation of random using a system-provided class.
    /// </summary>
    public class StdRandom : IRandom
    {
        public StdRandom(int seed)
        {
            this.random = new Random(seed);
        }

        public uint GetNext(uint min = 0, uint max = uint.MaxValue)
        {
            return min + (uint)(random.NextDouble() * (max - min));
        }

        private Random random;
    }
}
