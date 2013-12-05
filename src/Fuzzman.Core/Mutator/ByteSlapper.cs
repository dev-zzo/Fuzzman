using System.Collections.Generic;
using Fuzzman.Core.Platform.Mmap;

namespace Fuzzman.Core.Mutator
{
    /// <summary>
    /// Changes a random byte in the view.
    /// </summary>
    public class ByteSlapper : IMutator
    {
        public ByteSlapper(IRandom rng)
        {
            this.rng = rng;
        }

        public List<Difference> Process(MappedFileView view)
        {
            List<Difference> diffs = new List<Difference>();

            uint offset = rng.GetNext(0, view.Length);
            diffs.Add(new Difference()
            {
                Offset = offset,
                OldValue = view[offset],
                NewValue = (byte)rng.GetNext(0, byte.MaxValue + 1),
            });

            return diffs;
        }

        private IRandom rng;
    }
}
