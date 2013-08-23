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

        public void Process(MappedFileView view)
        {
            uint offset = rng.GetNext(0, view.Length);
            byte bit = (byte)(1 << (int)rng.GetNext(0, 8));
            view[offset] ^= bit;
        }

        private IRandom rng;
    }
}
