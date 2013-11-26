using System.Collections.Generic;
using System.IO;
using Fuzzman.Core;
using Fuzzman.Core.Mutator;
using Fuzzman.Core.Platform.Mmap;

namespace Fuzzman.Agent.Fuzzers
{
    class DumbFuzzer : IFuzzer
    {
        public DumbFuzzer(IRandom rng)
        {
            this.rng = rng;
            this.bitFlipper = new BitFlipper(rng);
            this.valueSetter = new ValueSetter(rng);
        }

        public Difference[] Process(string target)
        {
            List<Difference> diffs = new List<Difference>();

            using (MappedFile mapped = new MappedFile(target, FileMode.Open, FileAccess.Read))
            using (MappedFileView view = mapped.CreateView(0, 0))
            {
                int max;
                max = (int)this.rng.GetNext(0, 51);
                for (int i = 0; i < max; i++)
                    bitFlipper.Process(view, diffs);

                max = (int)this.rng.GetNext(0, 6);
                for (int i = 0; i < max; i++)
                    valueSetter.Process(view, diffs);
            }

            return diffs.ToArray();
        }

        private readonly IRandom rng;
        private readonly IMutator bitFlipper;
        private readonly IMutator valueSetter;

    }
}
