using Fuzzman.Core.Platform.Mmap;

namespace Fuzzman.Core
{
    public interface IMutator
    {
        void Process(MappedFileView view);
    }
}
