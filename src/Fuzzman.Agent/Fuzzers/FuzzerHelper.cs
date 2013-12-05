using System.IO;
using Fuzzman.Core;
using Fuzzman.Core.Platform.Mmap;
using System.Collections.Generic;

namespace Fuzzman.Agent.Fuzzers
{
    static class FuzzerHelper
    {
        public static void ApplyDifferences(Difference[] diffs, string sourcePath, string targetPath)
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Copy(sourcePath, targetPath);

            using (MappedFile mapped = new MappedFile(targetPath, FileMode.Open, FileAccess.ReadWrite))
            using (MappedFileView view = mapped.CreateView(0, 0))
            {
                foreach (Difference diff in diffs)
                {
                    if (diff.Ignored)
                        continue;

                    view.Write((uint)diff.Offset, diff.NewValue);
                }
            }
        }

        public static bool IsValidDifference(Difference subject, List<Difference> existing, ByteRange[] restricted)
        {
            // No point in patching the same place twice.
            if (existing != null)
            {
                foreach (Difference diff in existing)
                {
                    if (subject.Offset == diff.Offset)
                        return false;
                }
            }

            // No point in getting into the restricted areas.
            if (restricted != null)
            {
                foreach (ByteRange range in restricted)
                {
                    if (range.Contains(subject.Offset))
                        return false;
                }
            }

            return true;
        }
    }
}
