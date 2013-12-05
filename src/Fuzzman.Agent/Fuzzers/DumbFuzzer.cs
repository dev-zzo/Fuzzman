using System.Collections.Generic;
using System.IO;
using Fuzzman.Core;
using Fuzzman.Core.Mutator;
using Fuzzman.Core.Platform.Mmap;

namespace Fuzzman.Agent.Fuzzers
{
    /// <summary>
    /// Quick, dirty, and effective!
    /// </summary>
    class DumbFuzzer : IFuzzer
    {
        public DumbFuzzer(IRandom rng)
        {
            this.rng = rng;
        }

        public Difference[] Diffs
        {
            get { return this.diffs; }
        }

        public void Populate(string source)
        {
            List<Difference> diffs = new List<Difference>();

            using (MappedFile mapped = new MappedFile(source, FileMode.Open, FileAccess.Read))
            using (MappedFileView view = mapped.CreateView(0, 0))
            {
                IMutator bitFlipper = new BitFlipper(this.rng);
                IMutator valueSetter = new ValueSetter(this.rng);
                int max;
                max = (int)this.rng.GetNext(0, 51);
                for (int i = 0; i < max; i++)
                    diffs.AddRange(bitFlipper.Process(view));

                max = (int)this.rng.GetNext(0, 6);
                for (int i = 0; i < max; i++)
                    diffs.AddRange(valueSetter.Process(view));
            }

            this.diffs = diffs.ToArray();
        }

        public void Apply(string sourcePath, string targetPath)
        {
            FuzzerHelper.ApplyDifferences(this.diffs, sourcePath, targetPath);
        }

        private readonly IRandom rng;
        private Difference[] diffs;
    }
}
