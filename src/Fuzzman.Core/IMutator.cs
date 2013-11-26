using System.Collections.Generic;
using Fuzzman.Core.Platform.Mmap;

namespace Fuzzman.Core
{
    public interface IMutator
    {
        void Process(MappedFileView view, List<Difference> diffs);
    }
}
