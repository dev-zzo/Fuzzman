using System.IO;
using Fuzzman.Core;
using Fuzzman.Core.Mutator;
using Fuzzman.Core.System.Mmap;

namespace Fuzzman.Agent
{
    class DumbFuzzer : IFuzzer
    {
        public DumbFuzzer(int seed = 12345)
        {
            this.rng = new StdRandom(12345);
            this.bitFlipper = new BitFlipper(rng);
            this.valueSetter = new ValueSetter(rng);
        }

        public void Process(string target)
        {
            using (MappedFile mapped = new MappedFile(target, FileMode.Open, FileAccess.ReadWrite))
            using (MappedFileView view = mapped.CreateView(0, 0))
            {
                for (int i = 0; i < 10; i++)
                    bitFlipper.Process(view);
                for (int i = 0; i < 2; i++)
                    valueSetter.Process(view);
            }
        }

        private readonly IRandom rng;
        private readonly IMutator bitFlipper;
        private readonly IMutator valueSetter;

    }
}
