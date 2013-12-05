using System.Collections.Generic;
using Fuzzman.Core.Platform.Mmap;

namespace Fuzzman.Core
{
    public interface IMutator
    {
        List<Difference> Process(MappedFileView view);
    }
}
