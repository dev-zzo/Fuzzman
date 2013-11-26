using System.Collections.Generic;
using Fuzzman.Core.Platform.Mmap;

namespace Fuzzman.Core.Mutator
{
    /// <summary>
    /// Sets a random byte/short/int/long to either 0x00 or 0xFF.
    /// </summary>
    public class ValueSetter : IMutator
    {
        public ValueSetter(IRandom rng)
        {
            this.rng = rng;
        }

        public void Process(MappedFileView view, List<Difference> diffs)
        {
            uint size = (uint)(1 << (int)rng.GetNext(0, 4)) - 1;
            uint offset = rng.GetNext(0, view.Length - size);
            byte value = (byte)(rng.GetNext(0, 256) > 128 ? 0xFF : 0x00);
            for (uint i = 0; i <= size; ++i)
            {
                diffs.Add(new Difference()
                {
                    Offset = offset + i,
                    OldValue = view[offset],
                    NewValue = value,
                });
            }
        }

        private IRandom rng;
    }
}
