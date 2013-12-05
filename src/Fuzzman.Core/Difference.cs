using System;

namespace Fuzzman.Core
{
    /// <summary>
    /// A modification that is performed to a fuzzed file.
    /// </summary>
    public struct Difference
    {
        public uint Offset { get; set; }

        public byte OldValue { get; set; }

        public byte NewValue { get; set; }

        public bool Ignored { get; set; }

        public override string ToString()
        {
            return String.Format("{0:X8} {1:X2} -> {2:X2}", this.Offset, this.OldValue, this.NewValue);
        }
    }
}
