using System;

namespace Fuzzman.Core
{
    /// <summary>
    /// Simple byte range type.
    /// </summary>
    public struct ByteRange
    {
        /// <summary>
        /// Starting position (inclusive).
        /// </summary>
        public uint Start { get; set; }

        /// <summary>
        /// Ending position (inclusive).
        /// </summary>
        public uint End { get; set; }

        /// <summary>
        /// Check whether an offset falls within the range.
        /// </summary>
        /// <param name="offset">Offset to test</param>
        /// <returns></returns>
        public bool Contains(uint offset)
        {
            return offset >= this.Start && offset <= this.End;
        }
    }
}
