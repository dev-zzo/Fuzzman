using System.IO;
using Fuzzman.Core;
using Fuzzman.Core.Mutator;
using Fuzzman.Core.System.Mmap;

namespace Fuzzman.Agent
{
    class DumbFuzzer : IFuzzer
    {
        public DumbFuzzer(IRandom rng)
        {
            this.rng = rng;
            this.bitFlipper = new BitFlipper(rng);
            this.valueSetter = new ValueSetter(rng);
        }

        public void Process(string target)
        {
            using (MappedFile mapped = new MappedFile(target, FileMode.Open, FileAccess.ReadWrite))
            using (MappedFileView view = mapped.CreateView(0, 0))
            {
                int max;
                max = (int)this.rng.GetNext(0, 51);
                for (int i = 0; i < max; i++)
                    bitFlipper.Process(view);

                max = (int)this.rng.GetNext(0, 6);
                for (int i = 0; i < max; i++)
                    valueSetter.Process(view);
            }
        }

        private readonly IRandom rng;
        private readonly IMutator bitFlipper;
        private readonly IMutator valueSetter;

    }
}
