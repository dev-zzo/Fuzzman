using System.Collections.Generic;
using System.IO;
using Fuzzman.Core;
using Fuzzman.Core.Mutator;
using Fuzzman.Core.Platform.Mmap;

namespace Fuzzman.Agent.Fuzzers.TTF
{
    public class TTFFuzzer : IFuzzer
    {
        public TTFFuzzer(IRandom rng)
        {
            this.rng = rng;
        }

        public Difference[] Diffs
        {
            get { return this.diffs; }
        }

        public void Populate(string source)
        {
            using (MappedFile mapped = new MappedFile(source, FileMode.Open, FileAccess.Read))
            using (MappedFileView view = mapped.CreateView(0, 0))
            {
                // Protecting the OffsetTable and TableDir entries seems to be a good idea.
                TtfOffsetTable offsetTable;
                view.Read<TtfOffsetTable>(0, out offsetTable);
                this.restricted = new ByteRange[1];
                this.restricted[0].Start = 0;
                this.restricted[0].End = 12U + ReverseBytes(offsetTable.NumTables) * 16U - 1U;

                List<Difference> newDiffs = new List<Difference>();
                //Generate(view, new ValueSetter(this.rng), 5, newDiffs);
                Generate(view, new BitFlipper(this.rng), 50, newDiffs);
                this.diffs = newDiffs.ToArray();
            }
        }

        public void Apply(string sourcePath, string targetPath)
        {
            FuzzerHelper.ApplyDifferences(this.diffs, sourcePath, targetPath);
            using (MappedFile mapped = new MappedFile(targetPath, FileMode.Open, FileAccess.ReadWrite))
            using (MappedFileView view = mapped.CreateView(0, 0))
            {
                // Protecting the OffsetTable and TableDir entries seems to be a good idea.
                ushort entryCount;
                view.Read<ushort>(4, out entryCount);
                entryCount = ReverseBytes(entryCount);

                uint offset = 12;
                while (entryCount > 0)
                {
                    TtfTableDirectoryEntry entry;
                    view.Read<TtfTableDirectoryEntry>(offset, out entry);
                    uint tableOffset = ReverseBytes(entry.Offset);
                    uint tableLength = ReverseBytes(entry.Length);
                    uint newCheckSum = CalculateCheckSum(view, tableOffset, tableLength);
                    entry.CheckSum = ReverseBytes(newCheckSum);
                    view.Write<TtfTableDirectoryEntry>(offset, entry);
                    offset += 16;
                    entryCount -= 1;
                }
            }
        }

        private readonly IRandom rng;
        private Difference[] diffs;
        private ByteRange[] restricted;

        private void Generate(MappedFileView view, IMutator mutator, uint maxMutations, List<Difference> newDiffs)
        {
            uint actualMutations = this.rng.GetNext(0, maxMutations + 1U);
            while (actualMutations > 0)
            {
                List<Difference> candidates = mutator.Process(view);
                bool anyAdded = false;
                foreach (Difference diff in candidates)
                {
                    if (FuzzerHelper.IsValidDifference(diff, newDiffs, this.restricted))
                    {
                        newDiffs.Add(diff);
                        anyAdded = true;
                    }
                }
                if (anyAdded)
                {
                    --actualMutations;
                }
            }
        }

        private static uint CalculateCheckSum(MappedFileView view, uint offset, uint length)
        {
            uint checksum = 0;

            length = (length + 3) >> 2;
            while (length > 0)
            {
                uint value;
                view.Read<uint>(offset, out value);
                checksum += ReverseBytes(value);
                offset += 4;
                length -= 1;
            }

            return checksum;
        }

        private static ushort ReverseBytes(ushort value)
        {
            return (ushort)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        private static uint ReverseBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }
    }
}
