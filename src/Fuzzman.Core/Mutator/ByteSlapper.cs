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

        public void Process(MappedFileView view)
        {
            uint offset = rng.GetNext(0, view.Length);
            view[offset] = (byte)rng.GetNext(0, byte.MaxValue + 1);
        }

        private IRandom rng;
    }
}
