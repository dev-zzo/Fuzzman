using System.Collections.Generic;
using Fuzzman.Core.Platform.Mmap;

namespace Fuzzman.Core.Mutator
{
    /// <summary>
    /// Flips a single bit in the view.
    /// </summary>
    public class BitFlipper : IMutator
    {
        public BitFlipper(IRandom rng)
        {
            this.rng = rng;
        }

        public List<Difference> Process(MappedFileView view)
        {
            List<Difference> diffs = new List<Difference>();

            uint offset = rng.GetNext(0, view.Length);
            byte bit = (byte)(1 << (int)rng.GetNext(0, 8));
            diffs.Add(new Difference()
            {
                Offset = offset,
                OldValue = view[offset],
                NewValue = (byte)(view[offset] ^ bit),
            });

            return diffs;
        }

        private IRandom rng;
    }
}
