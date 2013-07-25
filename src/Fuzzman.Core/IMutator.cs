using Fuzzman.Core.System.Mmap;

namespace Fuzzman.Core
{
    public interface IMutator
    {
        void Process(MappedFileView view);
    }
}
