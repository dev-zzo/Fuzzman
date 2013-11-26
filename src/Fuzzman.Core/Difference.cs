using System;

namespace Fuzzman.Core
{
    /// <summary>
    /// A modification that is performed to a fuzzed file.
    /// </summary>
    public struct Difference
    {
        public long Offset { get; set; }

        public byte OldValue { get; set; }

        public byte NewValue { get; set; }

        public bool Ignored { get; set; }
    }
}
